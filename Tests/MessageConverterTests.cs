namespace StockSharp.Tests;

using System.Text;

using StockSharp.Fix;
using StockSharp.Fix.Native;

using FixExtensions = StockSharp.Fix.Native.Extensions;

/// <summary>
/// Round-trip conversion tests for MessageConverter.
/// Tests: Message -> FixXxx -> Message, then compare with original.
/// </summary>
[TestClass]
public partial class MessageConverterTests : BaseTestClass
{
	protected IMessageConverter Converter { get; } = new MessageConverter();

	protected static SecurityId CreateTestSecurityId(string code = "AAPL", string board = "NYSE")
		=> new() { SecurityCode = code, BoardCode = board };

	protected static DateTime CreateTestDateTime()
		=> new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

	#region Text FIX wire (TextFixReader / TextFixWriter)

	// The wire fixtures below are written by hand from the FIX specification, never by asking the
	// writer, so that an encoder and a decoder mistake which cancel each other cannot pass unnoticed.
	// '|' stands for the SOH delimiter, as the specification's own examples print it.
	private const char _sohMark = '|';

	private static string ToWire(string text)
		=> text.Replace(_sohMark, (char)AsciiSymbols.Soh);

	private static string FromWire(string text)
		=> text.Replace((char)AsciiSymbols.Soh, _sohMark);

	private static IFixReader CreateFixReader(string text)
		=> CreateFixReader(text, Encoding.ASCII);

	private static IFixReader CreateFixReader(string text, Encoding encoding)
		=> new TextFixReader(new MemoryStream(encoding.GetBytes(ToWire(text))), encoding, ownsStream: true);

	private static FastDateTimeParser CreateTimeStampParser()
		=> new(FixExtensions.TimeStampFormat);

	private static async Task<string> WriteFixAsync(Func<IFixWriter, ValueTask> handler, CancellationToken cancellationToken)
	{
		var stream = new MemoryStream();

		using (var writer = new TextFixWriter(stream, Encoding.ASCII))
		{
			await handler(writer);
			await writer.FlushAsync(cancellationToken);
		}

		return FromWire(Encoding.ASCII.GetString(stream.ToArray()));
	}

	private static async Task<byte[]> WriteFixBytesAsync(Func<IFixWriter, ValueTask> handler, Encoding encoding, CancellationToken cancellationToken)
	{
		var stream = new MemoryStream();

		using (var writer = new TextFixWriter(stream, encoding))
		{
			await handler(writer);
			await writer.FlushAsync(cancellationToken);
		}

		return stream.ToArray();
	}

	/// <summary>
	/// Pins the text wire format field by field against a message typed out from the specification
	/// (BodyLength 105 and CheckSum 244 counted over these very bytes).
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_WellFormedMessage_ReadsEveryFieldInOrder()
	{
		using var reader = CreateFixReader("8=FIX.4.4|9=105|35=D|34=12|49=SENDER|56=TARGET|52=20240615-10:30:00.000|11=ORD-1|55=AAPL|54=1|38=100|40=2|44=150.25|59=0|10=244|");

		AreEqual(FixTags.BeginString, await reader.ReadTagAsync(CancellationToken));
		AreEqual("FIX.4.4", await reader.ReadStringAsync(CancellationToken));

		// The sequence number of the message being read is not known until its own tag arrives.
		IsNull(reader.MsgSeqNum);

		AreEqual(FixTags.BodyLength, await reader.ReadTagAsync(CancellationToken));
		AreEqual(105, await reader.ReadIntAsync(CancellationToken));

		AreEqual(FixTags.MsgType, await reader.ReadTagAsync(CancellationToken));
		AreEqual("D", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));
		AreEqual(12L, await reader.ReadLongAsync(CancellationToken));
		IsNotNull(reader.MsgSeqNum);
		AreEqual(12L, reader.MsgSeqNum.Value);

		AreEqual(FixTags.SenderCompID, await reader.ReadTagAsync(CancellationToken));
		AreEqual("SENDER", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.TargetCompID, await reader.ReadTagAsync(CancellationToken));
		AreEqual("TARGET", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.SendingTime, await reader.ReadTagAsync(CancellationToken));
		AreEqual(CreateTestDateTime(), await reader.ReadDateTimeAsync(CreateTimeStampParser(), CancellationToken));

		AreEqual(FixTags.ClOrdID, await reader.ReadTagAsync(CancellationToken));
		AreEqual("ORD-1", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.Symbol, await reader.ReadTagAsync(CancellationToken));
		AreEqual("AAPL", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.Side, await reader.ReadTagAsync(CancellationToken));
		AreEqual('1', await reader.ReadCharAsync(CancellationToken));

		AreEqual(FixTags.OrderQty, await reader.ReadTagAsync(CancellationToken));
		AreEqual(100m, await reader.ReadDecimalAsync(CancellationToken));

		AreEqual(FixTags.OrdType, await reader.ReadTagAsync(CancellationToken));
		AreEqual('2', await reader.ReadCharAsync(CancellationToken));

		AreEqual(FixTags.Price, await reader.ReadTagAsync(CancellationToken));
		AreEqual(150.25m, await reader.ReadDecimalAsync(CancellationToken));

		AreEqual(FixTags.TimeInForce, await reader.ReadTagAsync(CancellationToken));
		AreEqual('0', await reader.ReadCharAsync(CancellationToken));

		AreEqual(FixTags.CheckSum, await reader.ReadTagAsync(CancellationToken));
		AreEqual("244", await reader.ReadStringAsync(CancellationToken));
	}

	/// <summary>
	/// Pins the field boundary: only SOH closes a value, so the first '=' delimits the tag and any
	/// later one belongs to the value and must survive intact.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_ValueKeepsEqualsSign_FieldEndsOnlyAtSoh()
	{
		using var reader = CreateFixReader("58=a=b=c|55=AAPL|");

		AreEqual(FixTags.Text, await reader.ReadTagAsync(CancellationToken));
		AreEqual("a=b=c", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixTags.Symbol, await reader.ReadTagAsync(CancellationToken));
		AreEqual("AAPL", await reader.ReadStringAsync(CancellationToken));
	}

	/// <summary>
	/// Pins the group boundary: entries arrive in wire order, each with its own values, and the
	/// field following the group is not swallowed by it.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_RepeatingGroup_KeepsEntryOrderAndNextFieldIntact()
	{
		using var reader = CreateFixReader("268=2|269=0|270=1.5|271=10|269=1|270=1.6|271=20|55=AAPL|");

		AreEqual(FixTags.NoMDEntries, await reader.ReadTagAsync(CancellationToken));
		AreEqual(2, await reader.ReadIntAsync(CancellationToken));

		AreEqual(FixTags.MDEntryType, await reader.ReadTagAsync(CancellationToken));
		AreEqual('0', await reader.ReadCharAsync(CancellationToken));
		AreEqual(FixTags.MDEntryPx, await reader.ReadTagAsync(CancellationToken));
		AreEqual(1.5m, await reader.ReadDecimalAsync(CancellationToken));
		AreEqual(FixTags.MDEntrySize, await reader.ReadTagAsync(CancellationToken));
		AreEqual(10m, await reader.ReadDecimalAsync(CancellationToken));

		AreEqual(FixTags.MDEntryType, await reader.ReadTagAsync(CancellationToken));
		AreEqual('1', await reader.ReadCharAsync(CancellationToken));
		AreEqual(FixTags.MDEntryPx, await reader.ReadTagAsync(CancellationToken));
		AreEqual(1.6m, await reader.ReadDecimalAsync(CancellationToken));
		AreEqual(FixTags.MDEntrySize, await reader.ReadTagAsync(CancellationToken));
		AreEqual(20m, await reader.ReadDecimalAsync(CancellationToken));

		AreEqual(FixTags.Symbol, await reader.ReadTagAsync(CancellationToken));
		AreEqual("AAPL", await reader.ReadStringAsync(CancellationToken));
	}

	/// <summary>
	/// Pins that input stopping inside a value is reported, never handed back as a value read from
	/// the half of the field that did arrive.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_TruncatedField_IsReported()
	{
		using var reader = CreateFixReader("8=FIX.4.4|9=105|35=D|34=1");

		AreEqual(FixTags.BeginString, await reader.ReadTagAsync(CancellationToken));
		AreEqual("FIX.4.4", await reader.ReadStringAsync(CancellationToken));
		AreEqual(FixTags.BodyLength, await reader.ReadTagAsync(CancellationToken));
		AreEqual(105, await reader.ReadIntAsync(CancellationToken));
		AreEqual(FixTags.MsgType, await reader.ReadTagAsync(CancellationToken));
		AreEqual("D", await reader.ReadStringAsync(CancellationToken));
		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));

		// The exception type is not part of any written contract; being told at all is.
		await ThrowsAsync<Exception>(async () => await reader.ReadLongAsync(CancellationToken), "Truncated sequence number accepted.");
	}

	/// <summary>
	/// Pins the documented end-of-data answer of <see cref="IFixReader.ReadTagAsync"/>: the empty
	/// tag, which is what the message loops in Extensions test for to stop.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_EndOfData_ReturnsEmptyTag()
	{
		using var reader = CreateFixReader("8=FIX.4.4|");

		AreEqual(FixTags.BeginString, await reader.ReadTagAsync(CancellationToken));
		AreEqual("FIX.4.4", await reader.ReadStringAsync(CancellationToken));

		AreEqual(FixExtensions.EmptyTag, await reader.ReadTagAsync(CancellationToken));
	}

	/// <summary>
	/// A tag is a positive number followed by an equals sign. Anything else is not a field, and
	/// reading it as one would name a field nobody sent and carry the bytes that followed into it -
	/// so the frame is refused rather than half understood.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_MalformedTag_IsRefused()
	{
		using (var reader = CreateFixReader("0=FIX.4.4|"))
			await ThrowsAsync<Exception>(async () => await reader.ReadTagAsync(CancellationToken), "A tag of zero names no field.");

		using (var reader = CreateFixReader("-8=FIX.4.4|"))
			await ThrowsAsync<Exception>(async () => await reader.ReadTagAsync(CancellationToken), "A negative tag names no field.");

		using (var reader = CreateFixReader("8|"))
			await ThrowsAsync<Exception>(async () => await reader.ReadTagAsync(CancellationToken), "A tag with no value is not a field.");
	}

	/// <summary>
	/// Pins that a value which is not the number the field promises is reported rather than turned
	/// into a plausible one.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_MalformedNumber_IsReported()
	{
		using (var reader = CreateFixReader("38=abc|"))
		{
			AreEqual(FixTags.OrderQty, await reader.ReadTagAsync(CancellationToken));
			await ThrowsAsync<Exception>(async () => await reader.ReadIntAsync(CancellationToken), "Non numeric quantity accepted.");
		}

		using (var reader = CreateFixReader("38=100X|"))
		{
			AreEqual(FixTags.OrderQty, await reader.ReadTagAsync(CancellationToken));
			await ThrowsAsync<Exception>(async () => await reader.ReadIntAsync(CancellationToken), "Trailing garbage after quantity accepted.");
		}

		using (var reader = CreateFixReader("44=1.2.3|"))
		{
			AreEqual(FixTags.Price, await reader.ReadTagAsync(CancellationToken));
			await ThrowsAsync<Exception>(async () => await reader.ReadDecimalAsync(CancellationToken), "Price with two decimal points accepted.");
		}
	}

	/// <summary>
	/// Pins that a timestamp which cannot be parsed is reported. Returning the default date instead
	/// makes an unreadable field indistinguishable from 0001-01-01 sent on purpose.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_UnparsableDateTime_IsReported()
	{
		// Right shape for UTCTimestamp, impossible values: month 13, day 32, hour 99.
		using var reader = CreateFixReader("52=20241332-99:99:99.999|");

		AreEqual(FixTags.SendingTime, await reader.ReadTagAsync(CancellationToken));

		await ThrowsAsync<Exception>(async () => await reader.ReadDateTimeAsync(CreateTimeStampParser(), CancellationToken), "Unparsable timestamp silently defaulted.");
	}

	/// <summary>
	/// Pins the char and boolean field shapes from the specification: exactly one character, and
	/// 'Y' or 'N' only.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_MalformedBoolAndChar_AreReported()
	{
		using (var reader = CreateFixReader("54=12|"))
		{
			AreEqual(FixTags.Side, await reader.ReadTagAsync(CancellationToken));
			await ThrowsAsync<Exception>(async () => await reader.ReadCharAsync(CancellationToken), "Two character value accepted as char.");
		}

		using (var reader = CreateFixReader("43=T|"))
		{
			AreEqual(FixTags.PossDupFlag, await reader.ReadTagAsync(CancellationToken));
			await ThrowsAsync<Exception>(async () => await reader.ReadBoolAsync(CancellationToken), "Boolean other than Y/N accepted.");
		}

		using (var reader = CreateFixReader("43=Y|43=N|"))
		{
			AreEqual(FixTags.PossDupFlag, await reader.ReadTagAsync(CancellationToken));
			IsTrue(await reader.ReadBoolAsync(CancellationToken));
			AreEqual(FixTags.PossDupFlag, await reader.ReadTagAsync(CancellationToken));
			IsFalse(await reader.ReadBoolAsync(CancellationToken));
		}
	}

	/// <summary>
	/// Pins that the whole range the int reader offers is readable off the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_Int_ReadsWholeRange()
	{
		using var reader = CreateFixReader("9=2147483647|9=-2147483648|");

		AreEqual(FixTags.BodyLength, await reader.ReadTagAsync(CancellationToken));
		AreEqual(int.MaxValue, await reader.ReadIntAsync(CancellationToken));

		AreEqual(FixTags.BodyLength, await reader.ReadTagAsync(CancellationToken));
		AreEqual(int.MinValue, await reader.ReadIntAsync(CancellationToken));
	}

	/// <summary>
	/// Pins that the whole range the long reader offers is readable off the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_Long_ReadsWholeRange()
	{
		using var reader = CreateFixReader("34=9223372036854775807|34=-9223372036854775808|");

		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));
		AreEqual(long.MaxValue, await reader.ReadLongAsync(CancellationToken));

		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));
		AreEqual(long.MinValue, await reader.ReadLongAsync(CancellationToken));
	}

	/// <summary>
	/// Pins that the whole range the decimal reader offers is readable off the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_Decimal_ReadsWholeRange()
	{
		using var reader = CreateFixReader("44=79228162514264337593543950335|44=-79228162514264337593543950335|");

		AreEqual(FixTags.Price, await reader.ReadTagAsync(CancellationToken));
		AreEqual(decimal.MaxValue, await reader.ReadDecimalAsync(CancellationToken));

		AreEqual(FixTags.Price, await reader.ReadTagAsync(CancellationToken));
		AreEqual(decimal.MinValue, await reader.ReadDecimalAsync(CancellationToken));
	}

	/// <summary>
	/// Pins the written bytes against text typed out from the specification: every field is
	/// tag=value closed by SOH, and no field is padded, quoted or reordered.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_WritesEachFieldAsTagEqualsValueSoh()
	{
		var text = await WriteFixAsync(async w =>
		{
			await w.WriteAsync(FixTags.MsgType, CancellationToken);
			await w.WriteAsync("D", CancellationToken);
			await w.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
			await w.WriteAsync(12L, CancellationToken);
			await w.WriteAsync(FixTags.SenderCompID, CancellationToken);
			await w.WriteAsync("SENDER", CancellationToken);
			await w.WriteAsync(FixTags.Symbol, CancellationToken);
			await w.WriteAsync("AAPL", CancellationToken);
			await w.WriteAsync(FixTags.Side, CancellationToken);
			await w.WriteAsync('1', CancellationToken);
			await w.WriteAsync(FixTags.OrderQty, CancellationToken);
			await w.WriteAsync(100m, CancellationToken);
			await w.WriteAsync(FixTags.Price, CancellationToken);
			await w.WriteAsync(150.25m, CancellationToken);
			await w.WriteAsync(FixTags.PossDupFlag, CancellationToken);
			await w.WriteAsync(true, CancellationToken);
		}, CancellationToken);

		AreEqual("35=D|34=12|49=SENDER|55=AAPL|54=1|38=100|44=150.25|43=Y|", text);
	}

	/// <summary>
	/// Pins the specification's float shape: plain digits with an optional point, never exponent
	/// notation and never a bare '.5', which a counterparty would refuse.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_Decimal_FormatsWithoutExponent()
	{
		var text = await WriteFixAsync(async w =>
		{
			foreach (var value in new[] { 0m, 150.25m, -0.5m, 0.0001m, 1000000m, -123.456m })
			{
				await w.WriteAsync(FixTags.Price, CancellationToken);
				await w.WriteAsync(value, CancellationToken);
			}
		}, CancellationToken);

		AreEqual("44=0|44=150.25|44=-0.5|44=0.0001|44=1000000|44=-123.456|", text);
	}

	/// <summary>
	/// Pins the UTCTimestamp field shape - yyyyMMdd-HH:mm:ss.fff - written out by hand rather than
	/// taken from the parser.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_DateTime_WritesUtcTimestampFormat()
	{
		var text = await WriteFixAsync(async w =>
		{
			await w.WriteAsync(FixTags.SendingTime, CancellationToken);
			await w.WriteAsync(CreateTestDateTime(), CreateTimeStampParser(), CancellationToken);
		}, CancellationToken);

		AreEqual("52=20240615-10:30:00.000|", text);
	}

	/// <summary>
	/// Pins that the whole range the int writer accepts reaches the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_Int_WritesWholeRange()
	{
		var text = await WriteFixAsync(async w =>
		{
			await w.WriteAsync(FixTags.BodyLength, CancellationToken);
			await w.WriteAsync(int.MaxValue, CancellationToken);
			await w.WriteAsync(FixTags.BodyLength, CancellationToken);
			await w.WriteAsync(int.MinValue, CancellationToken);
		}, CancellationToken);

		AreEqual("9=2147483647|9=-2147483648|", text);
	}

	/// <summary>
	/// Pins that the whole range the long writer accepts reaches the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_Long_WritesWholeRange()
	{
		var text = await WriteFixAsync(async w =>
		{
			await w.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
			await w.WriteAsync(long.MaxValue, CancellationToken);
			await w.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
			await w.WriteAsync(long.MinValue, CancellationToken);
		}, CancellationToken);

		AreEqual("34=9223372036854775807|34=-9223372036854775808|", text);
	}

	/// <summary>
	/// Pins that the whole range the decimal writer accepts reaches the wire, both ends.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_Decimal_WritesWholeRange()
	{
		var text = await WriteFixAsync(async w =>
		{
			await w.WriteAsync(FixTags.Price, CancellationToken);
			await w.WriteAsync(decimal.MaxValue, CancellationToken);
			await w.WriteAsync(FixTags.Price, CancellationToken);
			await w.WriteAsync(decimal.MinValue, CancellationToken);
		}, CancellationToken);

		AreEqual("44=79228162514264337593543950335|44=-79228162514264337593543950335|", text);
	}

	/// <summary>
	/// Pins the unknown field path: a tag this build does not know is skipped whole - the '=' inside
	/// its value included - and the field standing after it reads normally.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_UnknownTag_SkipValueLeavesTheNextFieldReadable()
	{
		using var reader = CreateFixReader("9999=a=b|55=AAPL|");

		AreEqual((FixTags)9999, await reader.ReadTagAsync(CancellationToken));
		IsFalse(reader.IsValueRead);

		await reader.SkipValueAsync(CancellationToken);
		IsTrue(reader.IsValueRead);

		AreEqual(FixTags.Symbol, await reader.ReadTagAsync(CancellationToken));
		AreEqual("AAPL", await reader.ReadStringAsync(CancellationToken));
	}

	/// <summary>
	/// Pins that a field the input stops inside is not skipped as if it had arrived whole: the caller
	/// is told, rather than left believing the message continues at the next byte.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_SkipValueWithoutTerminator_IsReported()
	{
		using var reader = CreateFixReader("58=unterminated");

		AreEqual(FixTags.Text, await reader.ReadTagAsync(CancellationToken));

		await ThrowsAsync<Exception>(async () => await reader.SkipValueAsync(CancellationToken), "Truncated field skipped as if complete.");
	}

	/// <summary>
	/// Pins that one reader serves message after message: the sequence number it reports belongs to
	/// the message being read, so the second message does not inherit the first one's number.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_SecondMessage_DoesNotInheritTheFirstSequenceNumber()
	{
		using var reader = CreateFixReader("8=FIX.4.4|34=12|8=FIX.4.4|34=13|");

		AreEqual(FixTags.BeginString, await reader.ReadTagAsync(CancellationToken));
		AreEqual("FIX.4.4", await reader.ReadStringAsync(CancellationToken));
		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));
		AreEqual(12L, await reader.ReadLongAsync(CancellationToken));
		AreEqual(12L, reader.MsgSeqNum.Value);

		AreEqual(FixTags.BeginString, await reader.ReadTagAsync(CancellationToken));
		IsNull(reader.MsgSeqNum, "The second message kept the first one's sequence number.");

		AreEqual("FIX.4.4", await reader.ReadStringAsync(CancellationToken));
		AreEqual(FixTags.MsgSeqNum, await reader.ReadTagAsync(CancellationToken));
		AreEqual(13L, await reader.ReadLongAsync(CancellationToken));
		AreEqual(13L, reader.MsgSeqNum.Value);
	}

	/// <summary>
	/// Pins what a reused reader forgets between messages: the running checksum, the byte count that
	/// caps a message, the last tag and its read flag. A carried over checksum would refuse the next
	/// message's own CheckSum field.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_ClearState_ForgetsTheMessageJustRead()
	{
		using var reader = CreateFixReader("35=D|35=F|");

		reader.IsDump = true;

		AreEqual(FixTags.MsgType, await reader.ReadTagAsync(CancellationToken));
		AreEqual("D", await reader.ReadStringAsync(CancellationToken));

		// The field occupies five bytes: '3' 51 + '5' 53 + '=' 61 + 'D' 68 + SOH 1 = 234.
		AreEqual(234U, reader.CheckSum);
		AreEqual(5, reader.BytesCount);
		AreEqual(FixTags.MsgType, reader.LastTag);
		IsTrue(reader.IsValueRead);
		AreEqual("35=D|", reader.FlushDump());

		reader.ClearState();

		AreEqual(0U, reader.CheckSum);
		AreEqual(0, reader.BytesCount);
		AreEqual(FixExtensions.EmptyTag, reader.LastTag);
		IsFalse(reader.IsValueRead);

		AreEqual(FixTags.MsgType, await reader.ReadTagAsync(CancellationToken));
		AreEqual("F", await reader.ReadStringAsync(CancellationToken));

		// '3' 51 + '5' 53 + '=' 61 + 'F' 70 + SOH 1 = 236, counted from zero rather than from 234.
		AreEqual(236U, reader.CheckSum);
		AreEqual(5, reader.BytesCount);
		AreEqual("35=F|", reader.FlushDump(), "The dump of the finished message came back with the next one.");
	}

	/// <summary>
	/// Pins that a value is decoded with the reader's encoding: the six Cyrillic letters below are
	/// carried as twelve UTF-8 bytes, and what comes back is the six characters.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_Utf8Value_ReadsCharactersNotBytes()
	{
		const string text = "\u041F\u0440\u0438\u0432\u0435\u0442";

		AreEqual(12, Encoding.UTF8.GetByteCount(text));

		using var reader = CreateFixReader($"58={text}|55=AAPL|", Encoding.UTF8);

		AreEqual(FixTags.Text, await reader.ReadTagAsync(CancellationToken));

		var value = await reader.ReadStringAsync(CancellationToken);

		AreEqual(text, value);
		AreEqual(6, value.Length);

		// The multi byte value ends where its SOH is, not where its first high byte is.
		AreEqual(FixTags.Symbol, await reader.ReadTagAsync(CancellationToken));
		AreEqual("AAPL", await reader.ReadStringAsync(CancellationToken));
	}

	/// <summary>
	/// Pins that text reaches the wire in the writer's encoding: the tag and separators as ASCII, the
	/// value as its twelve UTF-8 bytes, never narrowed to one byte per character.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_Utf8Text_WritesEncodedBytes()
	{
		const string text = "\u041F\u0440\u0438\u0432\u0435\u0442";

		var written = await WriteFixBytesAsync(async w =>
		{
			await w.WriteAsync(FixTags.Text, CancellationToken);
			await w.WriteAsync(text, CancellationToken);
		}, Encoding.UTF8, CancellationToken);

		var expected = new List<byte>();
		expected.AddRange(Encoding.ASCII.GetBytes("58="));
		expected.AddRange(Encoding.UTF8.GetBytes(text));
		expected.Add((byte)AsciiSymbols.Soh);

		AreEqual(16, expected.Count);
		IsTrue(expected.SequenceEqual(written), "Text was not written as its encoded bytes.");
	}

	/// <summary>
	/// Pins who closes the stream. A session hands the same socket to reader after reader, so only
	/// the one told it owns the stream may close it.
	/// </summary>
	[TestMethod]
	public async Task TextFixReaderAndWriter_CloseTheStreamOnlyWhenTheyOwnIt()
	{
		var borrowedRead = new MemoryStream(Encoding.ASCII.GetBytes(ToWire("55=AAPL|")));

		using (var reader = new TextFixReader(borrowedRead, Encoding.ASCII, ownsStream: false))
			AreEqual(FixTags.Symbol, await ((IFixReader)reader).ReadTagAsync(CancellationToken));

		IsTrue(borrowedRead.CanRead, "The reader closed a stream it does not own.");

		var ownedRead = new MemoryStream(Encoding.ASCII.GetBytes(ToWire("55=AAPL|")));

		using (var reader = new TextFixReader(ownedRead, Encoding.ASCII, ownsStream: true))
			AreEqual(FixTags.Symbol, await ((IFixReader)reader).ReadTagAsync(CancellationToken));

		IsFalse(ownedRead.CanRead, "The reader left open a stream it owns.");

		var borrowedWrite = new MemoryStream();

		using (var writer = new TextFixWriter(borrowedWrite, Encoding.ASCII, ownsStream: false))
			await ((IFixWriter)writer).WriteAsync(FixTags.Symbol, CancellationToken);

		IsTrue(borrowedWrite.CanWrite, "The writer closed a stream it does not own.");

		var ownedWrite = new MemoryStream();

		using (var writer = new TextFixWriter(ownedWrite, Encoding.ASCII, ownsStream: true))
			await ((IFixWriter)writer).WriteAsync(FixTags.Symbol, CancellationToken);

		IsFalse(ownedWrite.CanWrite, "The writer left open a stream it owns.");
	}

	/// <summary>
	/// Pins that a cancelled read is refused rather than served: nothing is taken out of the stream,
	/// so the field is still there for whoever reads next.
	/// </summary>
	[TestMethod]
	public async Task TextFixReader_CancelledToken_TakesNothingFromTheStream()
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var stream = new MemoryStream(Encoding.ASCII.GetBytes(ToWire("55=AAPL|")));

		using var reader = new TextFixReader(stream, Encoding.ASCII, ownsStream: true);

		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () => await ((IFixReader)reader).ReadTagAsync(cts.Token), "A cancelled read was served from the stream.");
		AreEqual(0L, stream.Position);
	}

	/// <summary>
	/// Pins that a cancelled write puts nothing on the wire: half a field would be indistinguishable
	/// from a corrupted message to the counterparty.
	/// </summary>
	[TestMethod]
	public async Task TextFixWriter_CancelledToken_PutsNothingOnTheWire()
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var stream = new MemoryStream();

		using var writer = new TextFixWriter(stream, Encoding.ASCII);

		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () => await ((IFixWriter)writer).WriteAsync(FixTags.Symbol, cts.Token), "A cancelled write reached the stream.");
		AreEqual(0L, stream.Length);
	}

	#endregion

	#region ScaledNumber

	// A ScaledNumber promises one thing: its value is mantissa * 10^exponent. Every expected number
	// below is worked out from that promise by hand, never read back from the struct's own properties.
	// The constructor takes the exponent first, then the mantissa.

	/// <summary>
	/// Pins that adding two numbers of the same scale adds their values: 1.00 + 2.00 = 3.00.
	/// Both operands are 10^-2 scaled, so the sum is worth 3, whatever representation carries it.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_EqualExponents_AddsValues()
	{
		var one = new ScaledNumber(-2, 100);
		var two = new ScaledNumber(-2, 200);

		AreEqual(1d, one.AsDouble);
		AreEqual(2d, two.AsDouble);
		AreEqual(3d, (one + two).AsDouble);
	}

	/// <summary>
	/// Pins that operands of different scale still add by value: 100 * 10^-2 is 1, 2 * 10^1 is 20,
	/// so the sum is 21. The exponent describes the scale of a number, it is not a quantity to add.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_DifferentExponents_AddsValues()
	{
		var one = new ScaledNumber(-2, 100);
		var twenty = new ScaledNumber(1, 2);

		AreEqual(1d, one.AsDouble);
		AreEqual(20d, twenty.AsDouble);
		AreEqual(21d, (one + twenty).AsDouble);
	}

	/// <summary>
	/// Pins that any number worth zero is an additive identity: 0 * 10^0 and 0 * 10^5 are both zero,
	/// so adding either to 125 * 10^-2 = 1.25 must leave 1.25.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_ZeroIsIdentity()
	{
		var value = new ScaledNumber(-2, 125);

		AreEqual(1.25d, value.AsDouble);
		AreEqual(1.25d, (value + new ScaledNumber(0, 0)).AsDouble);
		AreEqual(1.25d, (value + new ScaledNumber(5, 0)).AsDouble);
	}

	/// <summary>
	/// Pins addition across zero: -150 * 10^-2 = -1.50 plus 50 * 10^-2 = 0.50 is -1.00.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_Negatives_AddsValues()
	{
		var minusOneAndAHalf = new ScaledNumber(-2, -150);
		var half = new ScaledNumber(-2, 50);

		AreEqual(-1.5d, minusOneAndAHalf.AsDouble);
		AreEqual(0.5d, half.AsDouble);
		AreEqual(-1d, (minusOneAndAHalf + half).AsDouble);
	}

	/// <summary>
	/// Pins that a mantissa too large to hold does not silently turn the value round: adding 1 to
	/// long.MaxValue at scale 10^0 may not produce a result smaller than what it started from.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_MantissaOverflow_DoesNotGoBackwards()
	{
		var max = new ScaledNumber(0, long.MaxValue);
		var one = new ScaledNumber(0, 1);

		IsGreaterOrEqual((max + one).AsDouble, max.AsDouble);
	}

	/// <summary>
	/// Pins the same for the exponent: two numbers of 10^2000000000 each are past the top of double,
	/// so their sum is past it too and may not come back as a small number.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_Add_ExponentOverflow_DoesNotGoBackwards()
	{
		var huge = new ScaledNumber(2_000_000_000, 1);

		IsGreaterOrEqual((huge + huge).AsDouble, huge.AsDouble);
	}

	/// <summary>
	/// Pins that the decimal value carries every digit of the mantissa. 9007199254740993 is 2^53 + 1,
	/// the first whole number a double cannot hold; a decimal has 28 digits and holds all 16 of these.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_AsDecimal_KeepsDigitsDoubleCannotHold()
	{
		var value = new ScaledNumber(0, 9007199254740993L);

		AreEqual(9007199254740993m, value.AsDecimal);
	}

	/// <summary>
	/// Pins the scaling side of the decimal value: 12345 * 10^-2 is 12345 / 100, that is 123.45.
	/// </summary>
	[TestMethod]
	public void ScaledNumber_AsDecimal_AppliesNegativeExponent()
	{
		var value = new ScaledNumber(-2, 12345);

		AreEqual(123.45m, value.AsDecimal);
	}

	#endregion

	#region FixId

	// A FixId carries one order identifier in the two shapes the two channels need: the text a FIX
	// message puts in ClOrdID, and the number an SBE frame carries. What the type owes a caller is
	// that neither shape is invented, lost or quietly turned into the other.

	/// <summary>
	/// Pins that an identifier built from a number and the same identifier arriving as text answer
	/// the same on both channels: 7 reads as the number 7 and prints as "7" either way.
	/// </summary>
	[TestMethod]
	public void FixId_BuiltFromNumberOrText_AnswersTheSameOnBothChannels()
	{
		var fromLong = FixId.FromLong(7);
		FixId fromText = "7";

		AreEqual("7", (string)fromLong);
		AreEqual(7L, fromLong.ToLong());
		AreEqual(7L, fromLong.ToLongN().Value);
		IsFalse(fromLong.IsEmpty());

		AreEqual("7", (string)fromText);
		AreEqual(7L, fromText.ToLong());
		AreEqual(7L, fromText.ToLongN().Value);
		IsFalse(fromText.IsEmpty());
	}

	/// <summary>
	/// Pins that the whole numeric range survives the trip through the text shape: long.MaxValue is
	/// nineteen digits, more than an int holds, and it must come back as itself.
	/// </summary>
	[TestMethod]
	public void FixId_LargeNumber_IsNeitherTruncatedNorReformatted()
	{
		var id = FixId.FromLong(long.MaxValue);

		AreEqual("9223372036854775807", (string)id);
		AreEqual(long.MaxValue, id.ToLong());

		var negative = FixId.FromLong(-5);

		AreEqual("-5", (string)negative);
		AreEqual(-5L, negative.ToLong());
	}

	/// <summary>
	/// Pins that the text shape is echoed exactly: FIX ClOrdID is a string, so "007" and "7" are two
	/// different identifiers to a counterparty even though both read as the number 7.
	/// </summary>
	[TestMethod]
	public void FixId_LeadingZeros_KeepTheTextTheyArrivedWith()
	{
		FixId id = "007";

		AreEqual("007", (string)id);
		AreEqual("007", id.ToString());
		AreEqual(7L, id.ToLong());
	}

	/// <summary>
	/// Pins that an identifier which is not a number keeps its text and is never handed back as one:
	/// a caller asking for the numeric shape of "ORD-1" is told, not given zero.
	/// </summary>
	[TestMethod]
	public void FixId_NonNumericText_IsKeptAndNeverReadAsANumber()
	{
		FixId id = "ORD-1";

		AreEqual("ORD-1", (string)id);
		IsFalse(id.IsEmpty());

		Throws<Exception>(() => id.ToLong(), "A non numeric identifier was read as a number.");
	}

	/// <summary>
	/// Pins that an absent identifier says so instead of naming order zero: an empty ClOrdID is not
	/// the order whose id is 0.
	/// </summary>
	[TestMethod]
	public void FixId_Empty_HasNoNumberAtAll()
	{
		FixId fromEmpty = string.Empty;
		FixId fromNull = (string)null;

		foreach (var id in new[] { default(FixId), fromEmpty, fromNull })
		{
			IsTrue(id.IsEmpty());
			IsNull(id.ToLongN());

			Throws<Exception>(() => id.ToLong(), "An absent identifier was read as a number.");
		}
	}

	/// <summary>
	/// Pins that the two shapes are kept as given when they are not each other's transcription - the
	/// text ClOrdID "ORD-7" alongside the binary id 7 is the point of the type - and that such an
	/// identifier is neither of the two identifiers built from one shape alone.
	/// </summary>
	[TestMethod]
	public void FixId_TextAndNumberThatDiffer_AreBothKept()
	{
		var id = FixId.From("ORD-7", 7);

		AreEqual("ORD-7", (string)id);
		AreEqual(7L, id.ToLong());
		AreEqual(7L, id.ToLongN().Value);

		AreNotEqual(FixId.FromLong(7), id);
		AreNotEqual((FixId)"ORD-7", id);
	}

	#endregion
}
