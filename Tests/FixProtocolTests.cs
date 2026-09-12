namespace StockSharp.Tests;

using System.Text;

using StockSharp.Fix;
using StockSharp.Fix.Native;

[TestClass]
public class FixProtocolTests : BaseTestClass
{
	private static (IFixReader reader, IFixWriter writer, MemoryStream stream) Create()
	{
		var stream = new MemoryStream();
		var encoding = Encoding.ASCII;

		var writer = new TextFixWriter(stream, encoding);
		stream.Position = 0;
		var reader = new TextFixReader(stream, encoding);
		return (reader, writer, stream);
	}

	#region Basic Type Tests

	[TestMethod]
	public async Task Int()
	{
		var (reader, writer, stream) = Create();
		var expected = 12345;

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadIntAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task NegativeInt()
	{
		var (reader, writer, stream) = Create();
		var expected = -9876;

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadIntAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Long()
	{
		var (reader, writer, stream) = Create();

		// The text format cannot round-trip long.MinValue exactly.
		var minValue = long.MinValue + 1;

		await writer.WriteAsync(long.MaxValue, CancellationToken);
		await writer.WriteAsync(minValue, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadLongAsync(CancellationToken);
		actual.AssertEqual(long.MaxValue);

		actual = await reader.ReadLongAsync(CancellationToken);
		actual.AssertEqual(minValue);
	}

	[TestMethod]
	public async Task Decimal()
	{
		var (reader, writer, stream) = Create();
		var expected = 123.456m;

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;
		var actual = await reader.ReadDecimalAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task NegativeDecimal()
	{
		var (reader, writer, stream) = Create();
		var expected = -2920000.00m;

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadDecimalAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task String()
	{
		var (reader, writer, stream) = Create();
		var expected = "Hello World";

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;
		var actual = await reader.ReadStringAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Char()
	{
		var (reader, writer, stream) = Create();
		var expected = 'X';

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadCharAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task Bool()
	{
		var (reader, writer, stream) = Create();
		var expected = true;

		await writer.WriteAsync(expected, CancellationToken);
		stream.Position = 0;
		var actual = await reader.ReadBoolAsync(CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task DateTime()
	{
		var (reader, writer, stream) = Create();
		var parser = new FastDateTimeParser("yyyyMMdd-HH:mm:ss");
		var expected = new DateTime(2023, 10, 15, 14, 30, 0).UtcKind();

		await writer.WriteAsync(expected, parser, CancellationToken);
		stream.Position = 0;
		DateTime actual = await reader.ReadDateTimeAsync(parser, CancellationToken);

		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task TimeSpan()
	{
		var (reader, writer, stream) = Create();
		var parser = new FastTimeSpanParser("HH:mm:ss");
		var expected = new TimeSpan(14, 30, 0);

		await writer.WriteAsync(expected, parser, CancellationToken);
		stream.Position = 0;

		var actual = await reader.ReadTimeSpanAsync(parser, CancellationToken);

		actual.AssertEqual(expected);
	}

	/// <summary>
	/// The most negative long has no positive counterpart, so writing it by negating it overflows.
	/// A sequence number or a quantity that arrives at that value has to survive the wire like any
	/// other, rather than taking the session down on the way out.
	/// </summary>
	[TestMethod]
	public async Task LongMinValueIsWrittenAndReadBack()
	{
		var (reader, writer, stream) = Create();

		await writer.WriteAsync(long.MinValue, CancellationToken);
		stream.Position = 0;

		(await reader.ReadLongAsync(CancellationToken)).AssertEqual(long.MinValue,
			"the most negative long is a number like any other on the wire");
	}

	#endregion

	#region Text Format Specific Tests

	[TestMethod]
	public async Task TextReadTag()
	{
		var (reader, _, stream) = Create();
		var tagData = "8=FIX.4.2\u0001";
		var bytes = tagData.ASCII();

		stream.Write(bytes, 0, bytes.Length);
		stream.Position = 0;

		var tag = await reader.ReadTagAsync(CancellationToken);
		var value = await reader.ReadStringAsync(CancellationToken);

		tag.AssertEqual(FixTags.BeginString);
		value.AssertEqual("FIX.4.2");
	}

	[TestMethod]
	public async Task TextTags()
	{
		var (reader, writer, stream) = Create();

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix42, CancellationToken);
		await writer.WriteAsync(FixTags.BodyLength, CancellationToken);
		await writer.WriteAsync(123, CancellationToken);
		stream.Position = 0;

		var tag1 = await reader.ReadTagAsync(CancellationToken);
		var value1 = await reader.ReadStringAsync(CancellationToken);
		var tag2 = await reader.ReadTagAsync(CancellationToken);
		var value2 = await reader.ReadIntAsync(CancellationToken);

		tag1.AssertEqual(FixTags.BeginString);
		value1.AssertEqual(FixVersions.Fix42);
		tag2.AssertEqual(FixTags.BodyLength);
		value2.AssertEqual(123);
	}

	[TestMethod]
	public async Task TextDecimalZero()
	{
		var (_, writer, stream) = Create();
		var value = 0m;

		await writer.WriteAsync(value, CancellationToken);

		stream.Position = 0;
		var bytes = stream.ToArray();

		bytes.Length.AssertEqual(2);
		((byte)'0').AssertEqual(bytes[0]);
		(0x01).AssertEqual(bytes[1]); // SOH character
	}

	[TestMethod]
	public async Task TextNegativeDecimalFormat()
	{
		var (_, writer, stream) = Create();
		var value = -123.45m;

		await writer.WriteAsync(value, CancellationToken);

		stream.Position = 0;

		var result = stream.ToArray().ASCII().Replace("\u0001", "");

		result.AssertEqual("-123.45");
	}

	[TestMethod]
	public async Task TextRandomMixedTypes()
	{
		var (reader, writer, stream) = Create();
		var count = RandomGen.GetInt(5, 15); // Random number of messages
		var messages = new List<(int type, object value)>();
		var parser = new FastDateTimeParser("yyyyMMdd-HH:mm:ss");

		for (var i = 0; i < count; i++)
		{
			var typeChoice = RandomGen.GetInt(0, 4);

			object value;

			switch (typeChoice)
			{
				case 0: // Int
					var intValue = RandomGen.GetInt(-1000, 1000);
					await writer.WriteAsync(intValue, CancellationToken);
					value = intValue;
					break;

				case 1: // Decimal
					var decValue = RandomGen.GetDecimal(3, 2); // 3 integer digits, 2 fractional
					await writer.WriteAsync(decValue, CancellationToken);
					value = decValue;
					break;

				case 2: // String
					var strValue = RandomGen.GetString(1, 20);
					await writer.WriteAsync(strValue, CancellationToken);
					value = strValue;
					break;

				case 3: // DateTime
					var dtValue = RandomGen.GetDate(
						new(2000, 1, 1),
						new(2023, 12, 31)
					).UtcKind();
					dtValue = new DateTime(dtValue.Year, dtValue.Month, dtValue.Day, dtValue.Hour, dtValue.Minute, dtValue.Second).UtcKind(); // Remove milliseconds
					await writer.WriteAsync(dtValue, parser, CancellationToken);
					value = dtValue;
					break;

				case 4: // Boolean
					var boolValue = RandomGen.GetBool();
					await writer.WriteAsync(boolValue, CancellationToken);
					value = boolValue;
					break;

				default:
					throw new InvalidOperationException();
			}

			messages.Add((typeChoice, value));
		}

		stream.Position = 0;

		for (var i = 0; i < count; i++)
		{
			var (type, expectedValue) = messages[i];

			var actualValue = type switch
			{
				0 => await reader.ReadIntAsync(CancellationToken),
				1 => await reader.ReadDecimalAsync(CancellationToken),
				2 => await reader.ReadStringAsync(CancellationToken),
				3 => await reader.ReadDateTimeAsync(parser, CancellationToken),
				4 => (object)await reader.ReadBoolAsync(CancellationToken),
				_ => throw new InvalidOperationException(),
			};

			actualValue.AssertNotNull();
			actualValue.AssertEqual(expectedValue);
		}
	}

	#endregion

	#region Dump Tests

	[TestMethod]
	public async Task Dump()
	{
		var (reader, writer, stream) = Create();

		writer.IsDump = true;

		// Use deterministic values to avoid formatting differences
		var sender = "SENDER123";
		var target = "TARGET456";
		var msgSeqNum = 42;
		var msgType = "D"; // New Order Single
		var clOrdId = "abc123def456ghij";
		var symbol = "AAPL";
		var orderQty = 100;
		var price = 123.45m; // Use exact decimal to avoid formatting issues

		var dump = $"8={FixVersions.Fix44}|49={sender}|56={target}|34={msgSeqNum}|35={msgType}|11={clOrdId}|55={symbol}|38={orderQty}|44={price}|";

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		await writer.WriteAsync(FixTags.SenderCompID, CancellationToken);
		await writer.WriteAsync(sender, CancellationToken);

		await writer.WriteAsync(FixTags.TargetCompID, CancellationToken);
		await writer.WriteAsync(target, CancellationToken);

		await writer.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
		await writer.WriteAsync(msgSeqNum, CancellationToken);

		await writer.WriteAsync(FixTags.MsgType, CancellationToken);
		await writer.WriteAsync(msgType, CancellationToken);

		await writer.WriteAsync(FixTags.ClOrdID, CancellationToken);
		await writer.WriteAsync(clOrdId, CancellationToken);

		await writer.WriteAsync(FixTags.Symbol, CancellationToken);
		await writer.WriteAsync(symbol, CancellationToken);

		await writer.WriteAsync(FixTags.OrderQty, CancellationToken);
		await writer.WriteAsync(orderQty, CancellationToken);

		await writer.WriteAsync(FixTags.Price, CancellationToken);
		await writer.WriteAsync(price, CancellationToken);

		writer.FlushDump().AssertEqual(dump);

		stream.Position = 0;

		reader.IsDump = true;
		await reader.SkipMessageAsync(CancellationToken);

		reader.FlushDump().AssertEqual(dump);

		writer.IsDump = false;

		stream.Position = 0;

		await writer.WriteAsync(FixTags.Price, CancellationToken);
		await writer.WriteAsync(price, CancellationToken);

		writer.FlushDump().IsEmpty().AssertTrue();
	}

	#endregion

	#region Random Data Tests

	[TestMethod]
	public async Task RandomIntegers()
	{
		var (reader, writer, stream) = Create();
		var values = new List<int>();
		var count = RandomGen.GetInt(10, 50); // Random number of values to write

		for (var i = 0; i < count; i++)
		{
			var value = RandomGen.GetInt(-10000, 10000);
			values.Add(value);
			await writer.WriteAsync(value, CancellationToken);
		}

		stream.Position = 0;
		var readValues = new List<int>();

		for (var i = 0; i < count; i++)
		{
			readValues.Add(await reader.ReadIntAsync(CancellationToken));
		}

		readValues.Count.AssertEqual(values.Count);

		for (var i = 0; i < values.Count; i++)
		{
			readValues[i].AssertEqual(values[i]);
		}
	}

	[TestMethod]
	public async Task RandomStrings()
	{
		var (reader, writer, stream) = Create();
		var strings = new List<string>();
		var count = RandomGen.GetInt(5, 20);

		for (var i = 0; i < count; i++)
		{
			var value = RandomGen.GetString(1, 30);

			strings.Add(value);
			await writer.WriteAsync(value, CancellationToken);
		}

		stream.Position = 0;
		var readValues = new List<string>();

		for (var i = 0; i < count; i++)
		{
			readValues.Add(await reader.ReadStringAsync(CancellationToken));
		}

		readValues.Count.AssertEqual(strings.Count);

		for (var i = 0; i < strings.Count; i++)
		{
			readValues[i].AssertEqual(strings[i]);
		}
	}

	[TestMethod]
	public async Task RandomDecimals()
	{
		var (reader, writer, stream) = Create();
		var values = new List<decimal>();
		var count = RandomGen.GetInt(10, 30);

		for (var i = 0; i < count; i++)
		{
			decimal value;

			if (RandomGen.GetBool())
			{
				value = RandomGen.GetDecimal(5, RandomGen.GetInt(0, 8));
			}
			else
			{
				if (RandomGen.GetBool())
					value = RandomGen.GetDecimal(1, 6) / 1000000m; // Very small
				else
					value = RandomGen.GetDecimal(5, 2) * 1000000m; // Very large
			}

			if (RandomGen.GetBool())
				value = -value;

			values.Add(value);
			await writer.WriteAsync(value, CancellationToken);
		}

		stream.Position = 0;

		var readValues = new List<decimal>();

		for (var i = 0; i < count; i++)
		{
			readValues.Add(await reader.ReadDecimalAsync(CancellationToken));
		}

		readValues.Count.AssertEqual(values.Count);

		for (var i = 0; i < values.Count; i++)
		{
			readValues[i].AssertEqual(values[i]);
		}
	}

	[TestMethod]
	public async Task RandomDateTime()
	{
		var (reader, writer, stream) = Create();
		var values = new List<DateTime>();
		var count = RandomGen.GetInt(10, 30);
		var parser = new FastDateTimeParser("yyyyMMdd-HH:mm:ss");

		for (int i = 0; i < count; i++)
		{
			var value = RandomGen.GetDate(
				new(1970, 1, 1),
				new(2050, 12, 31)).UtcKind();

			// Text format only preserves second precision, truncate for comparison
			value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, DateTimeKind.Utc);

			values.Add(value);
			await writer.WriteAsync(value, parser, CancellationToken);
		}

		stream.Position = 0;

		var readValues = new List<DateTime>();

		for (var i = 0; i < count; i++)
		{
			readValues.Add(await reader.ReadDateTimeAsync(parser, CancellationToken));
		}

		readValues.Count.AssertEqual(values.Count);

		for (var i = 0; i < values.Count; i++)
		{
			readValues[i].AssertEqual(values[i].UtcKind());
		}
	}

	[TestMethod]
	public async Task RandomFullFixMessage()
	{
		var (reader, writer, stream) = Create();

		var sender = "SENDER" + RandomGen.GetInt(100, 999);
		var target = "TARGET" + RandomGen.GetInt(100, 999);
		var msgSeqNum = RandomGen.GetInt(1, 10000);
		var msgType = "D"; // New Order Single
		var clOrdId = Guid.NewGuid().ToString("N")[..16];
		var symbol = "AAPL";
		var orderQty = RandomGen.GetInt(1, 1000);
		var price = RandomGen.GetDecimal(1, 1000, 2);

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		await writer.WriteAsync(FixTags.SenderCompID, CancellationToken);
		await writer.WriteAsync(sender, CancellationToken);

		await writer.WriteAsync(FixTags.TargetCompID, CancellationToken);
		await writer.WriteAsync(target, CancellationToken);

		await writer.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
		await writer.WriteAsync(msgSeqNum, CancellationToken);

		await writer.WriteAsync(FixTags.MsgType, CancellationToken);
		await writer.WriteAsync(msgType, CancellationToken);

		await writer.WriteAsync(FixTags.ClOrdID, CancellationToken);
		await writer.WriteAsync(clOrdId, CancellationToken);

		await writer.WriteAsync(FixTags.Symbol, CancellationToken);
		await writer.WriteAsync(symbol, CancellationToken);

		await writer.WriteAsync(FixTags.OrderQty, CancellationToken);
		await writer.WriteAsync(orderQty, CancellationToken);

		await writer.WriteAsync(FixTags.Price, CancellationToken);
		await writer.WriteAsync(price, CancellationToken);

		stream.Position = 0;

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.BeginString);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(FixVersions.Fix44);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.SenderCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(sender);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.TargetCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(target);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.MsgSeqNum);
		(await reader.ReadIntAsync(CancellationToken)).AssertEqual(msgSeqNum);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.MsgType);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(msgType);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.ClOrdID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(clOrdId);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.Symbol);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(symbol);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.OrderQty);
		(await reader.ReadIntAsync(CancellationToken)).AssertEqual(orderQty);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.Price);
		(await reader.ReadDecimalAsync(CancellationToken)).AssertEqual(price);
	}

	#endregion

	#region Checksum Tests

	[TestMethod]
	public async Task Checksum()
	{
		var (reader, writer, stream) = Create();

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		await writer.WriteAsync(FixTags.SenderCompID, CancellationToken);
		await writer.WriteAsync("SENDER123", CancellationToken);

		await writer.WriteAsync(FixTags.TargetCompID, CancellationToken);
		await writer.WriteAsync("TARGET456", CancellationToken);

		var checksum = CalculateChecksum(stream.ToArray());

		writer.CalcCheckSum().AssertEqual(checksum);

		await writer.WriteAsync(FixTags.CheckSum, CancellationToken);
		await writer.WriteAsync($"{checksum:000}", CancellationToken); // Format as 3 digits with leading zeros

		stream.Position = 0;

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.BeginString);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(FixVersions.Fix44);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.SenderCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual("SENDER123");

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.TargetCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual("TARGET456");

		var calcCheckSum = reader.CalcCheckSum();

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.CheckSum);
		var readChecksum = (await reader.ReadStringAsync(CancellationToken)).To<int>();

		readChecksum.AssertEqual(checksum);

		readChecksum.AssertEqual(calcCheckSum);
	}

	[TestMethod]
	public async Task ChecksumValidation()
	{
		var (reader, writer, stream) = Create();

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		await writer.WriteAsync(FixTags.MsgType, CancellationToken);
		await writer.WriteAsync("D", CancellationToken); // New Order Single

		await writer.WriteAsync(FixTags.ClOrdID, CancellationToken);
		await writer.WriteAsync("ORDER12345", CancellationToken);

		await writer.WriteAsync(FixTags.Symbol, CancellationToken);
		await writer.WriteAsync("MSFT", CancellationToken);

		await writer.WriteAsync(FixTags.Side, CancellationToken);
		await writer.WriteAsync('1', CancellationToken); // Buy (char)

		await writer.WriteAsync(FixTags.OrdType, CancellationToken);
		await writer.WriteAsync('2', CancellationToken); // Limit (char)

		await writer.WriteAsync(FixTags.OrderQty, CancellationToken);
		await writer.WriteAsync(100, CancellationToken);

		await writer.WriteAsync(FixTags.Price, CancellationToken);
		await writer.WriteAsync(399.99m, CancellationToken);

		var messageContent = stream.ToArray();

		var checksum = CalculateChecksum(messageContent);

		writer.CalcCheckSum().AssertEqual(checksum);

		await writer.WriteAsync(FixTags.CheckSum, CancellationToken);
		await writer.WriteAsync($"{checksum:000}", CancellationToken);

		stream.Position = 0;

		var readTags = new List<FixTags>();

		while (true)
		{
			var tag = await reader.ReadTagAsync(CancellationToken);
			readTags.Add(tag);

			if (tag == FixTags.CheckSum)
			{
				var readChecksum = (await reader.ReadStringAsync(CancellationToken)).To<int>();

				readChecksum.AssertEqual(checksum);
				break;
			}

			switch (tag)
			{
				case FixTags.BeginString:
				case FixTags.MsgType:
				case FixTags.ClOrdID:
				case FixTags.Symbol:
					await reader.ReadStringAsync(CancellationToken);
					break;
				case FixTags.Side:
				case FixTags.OrdType:
					await reader.ReadCharAsync(CancellationToken);
					break;
				case FixTags.OrderQty:
					await reader.ReadIntAsync(CancellationToken);
					break;
				case FixTags.Price:
					await reader.ReadDecimalAsync(CancellationToken);
					break;
				default:
					await reader.ReadStringAsync(CancellationToken);
					break;
			}
		}

		// 8 message tags plus the checksum tag.
		readTags.Count.AssertEqual(9);
	}

	[TestMethod]
	public async Task ChecksumDisabled()
	{
		var (reader, writer, stream) = Create();

		writer.CheckSumDisabled = reader.CheckSumDisabled = true;

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		await writer.WriteAsync(FixTags.SenderCompID, CancellationToken);
		await writer.WriteAsync("SENDER123", CancellationToken);

		await writer.WriteAsync(FixTags.TargetCompID, CancellationToken);
		await writer.WriteAsync("TARGET456", CancellationToken);

		var checksum = CalculateChecksum(stream.ToArray());

		writer.CalcCheckSum().AssertEqual(0);

		await writer.WriteAsync(FixTags.CheckSum, CancellationToken);
		await writer.WriteAsync($"{checksum:000}", CancellationToken); // Format as 3 digits with leading zeros

		stream.Position = 0;

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.BeginString);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual(FixVersions.Fix44);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.SenderCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual("SENDER123");

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.TargetCompID);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual("TARGET456");

		reader.CalcCheckSum().AssertEqual(0);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.CheckSum);
		var readChecksum = (await reader.ReadStringAsync(CancellationToken)).To<int>();

		readChecksum.AssertEqual(checksum);
	}

	/// <summary>
	/// A rejection has to name the message it refuses, and by then the tag carrying its number is
	/// long consumed - so the reader remembers it.
	/// </summary>
	[TestMethod]
	public async Task MsgSeqNumIsRemembered()
	{
		var (reader, writer, stream) = Create();

		reader.MsgSeqNum.AssertNull();

		await writer.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
		await writer.WriteAsync(42L, CancellationToken);

		await writer.WriteAsync(FixTags.Symbol, CancellationToken);
		await writer.WriteAsync("AAPL", CancellationToken);

		stream.Position = 0;

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.MsgSeqNum);
		(await reader.ReadLongAsync(CancellationToken)).AssertEqual(42L);

		reader.MsgSeqNum.AssertEqual(42L);

		// Still known once the rest of the message has been read past it.
		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.Symbol);
		(await reader.ReadStringAsync(CancellationToken)).AssertEqual("AAPL");

		reader.MsgSeqNum.AssertEqual(42L);
	}

	/// <summary>
	/// The number belongs to one message. A rejection written while reading the next one must not
	/// name the previous message, so the number is forgotten where a message begins.
	/// </summary>
	[TestMethod]
	public async Task MsgSeqNumIsForgottenBetweenMessages()
	{
		var (reader, writer, stream) = Create();

		await writer.WriteAsync(FixTags.MsgSeqNum, CancellationToken);
		await writer.WriteAsync(7L, CancellationToken);

		await writer.WriteAsync(FixTags.BeginString, CancellationToken);
		await writer.WriteAsync(FixVersions.Fix44, CancellationToken);

		stream.Position = 0;

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.MsgSeqNum);
		(await reader.ReadLongAsync(CancellationToken)).AssertEqual(7L);
		reader.MsgSeqNum.AssertEqual(7L);

		(await reader.ReadTagAsync(CancellationToken)).AssertEqual(FixTags.BeginString);

		reader.MsgSeqNum.AssertNull();
	}

	#endregion

	#region Helper Methods

	private static int CalculateChecksum(byte[] messageBytes)
	{
		// FIX checksum is the sum of all bytes modulo 256
		var sum = 0;

		foreach (byte b in messageBytes)
		{
			sum += b;
		}

		return sum % 256;
	}

	#endregion
}
