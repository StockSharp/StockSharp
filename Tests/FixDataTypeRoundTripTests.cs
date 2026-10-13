namespace StockSharp.Tests;

using System.Text;

using StockSharp.Fix.Native;

/// <summary>
/// Round-trip tests for all FIX protocol data types.
/// Verifies that what is written can be read back correctly.
/// </summary>
[TestClass]
public class FixDataTypeRoundTripTests : BaseTestClass
{
	private readonly FastDateTimeParser _dateTimeParser = new("yyyyMMdd-HH:mm:ss");
	private readonly FastTimeSpanParser _timeParser = new("HH:mm:ss");

	private static (MemoryStream stream, IFixReader reader, IFixWriter writer) CreateStreams()
	{
		var stream = new MemoryStream();
		var encoding = Encoding.ASCII;

		var textWriter = new TextFixWriter(stream, encoding);
		return (stream, new TextFixReader(stream, encoding), textWriter);
	}

	#region Bool Round-Trip Tests

	[TestMethod]
	public async Task Bool_True_RoundTrip()
	{
		var (stream, reader, writer) = CreateStreams();
		var tag = FixTags.PossDupFlag;

		await writer.WriteAsync(tag, CancellationToken);
		await writer.WriteAsync(true, CancellationToken);
		await writer.FlushAsync(CancellationToken);

		stream.Position = 0;
		var readTag = await reader.ReadTagAsync(CancellationToken);
		var readValue = await reader.ReadBoolAsync(CancellationToken);

		readTag.AssertEqual(tag);
		readValue.AssertTrue();
	}

	[TestMethod]
	public async Task Bool_False_RoundTrip()
	{
		var (stream, reader, writer) = CreateStreams();
		var tag = FixTags.PossDupFlag;

		await writer.WriteAsync(tag, CancellationToken);
		await writer.WriteAsync(false, CancellationToken);
		await writer.FlushAsync(CancellationToken);

		stream.Position = 0;
		var readTag = await reader.ReadTagAsync(CancellationToken);
		var readValue = await reader.ReadBoolAsync(CancellationToken);

		readTag.AssertEqual(tag);
		readValue.AssertFalse();
	}

	#endregion

	#region TickDirection Round-Trip Tests

	[TestMethod]
	public void TickDirection_RoundTrip()
	{
		true.ToTickDir().FromTickDir().AssertTrue();
		false.ToTickDir().FromTickDir().AssertFalse();
	}

	#endregion

	#region Int Round-Trip Tests

	[TestMethod]
	public async Task Int_RoundTrip()
	{
		var testValues = new[] { 0, 1, -1, 42, -42, 1000000, -1000000, 999999, -999999 };

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.HeartBtInt;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadIntAsync(CancellationToken);

			readTag.AssertEqual(tag);
			readValue.AssertEqual(originalValue);
		}
	}

	#endregion

	#region Long Round-Trip Tests

	[TestMethod]
	public async Task Long_RoundTrip()
	{
		var testValues = new[] { 0L, 1L, -1L, 42L, -42L, 9999999999L, -9999999999L };

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.MsgSeqNum;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadLongAsync(CancellationToken);

			readTag.AssertEqual(tag);
			readValue.AssertEqual(originalValue);
		}
	}

	#endregion

	#region Decimal Round-Trip Tests

	[TestMethod]
	public async Task Decimal_RoundTrip()
	{
		var testValues = new[]
		{
			0m, 1m, -1m,
			123.456m, -123.456m,
			0.00001m, -0.00001m,
			99999.99999m, -99999.99999m,
		};

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.Price;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadDecimalAsync(CancellationToken);

			readTag.AssertEqual(tag);
			readValue.AssertEqual(originalValue);
		}
	}

	#endregion

	#region DateTime Round-Trip Tests

	[TestMethod]
	public async Task DateTime_RoundTrip()
	{
		var testValues = new[]
		{
			new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc),
			new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
			new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
		};

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.SendingTime;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, _dateTimeParser, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadDateTimeAsync(_dateTimeParser, CancellationToken);

			readTag.AssertEqual(tag);
			readValue.UtcKind().AssertEqual(originalValue);
		}
	}

	#endregion

	#region TimeSpan Round-Trip Tests

	[TestMethod]
	public async Task TimeSpan_RoundTrip()
	{
		var testValues = new[]
		{
			TimeSpan.Zero,
			TimeSpan.FromSeconds(1),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(12),
			new TimeSpan(23, 59, 59),
			new TimeSpan(10, 30, 45),
		};

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.TransactTime;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, _timeParser, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadTimeSpanAsync(_timeParser, CancellationToken);

			readTag.AssertEqual(tag);
			// Compare hours, minutes, seconds (parser precision)
			readValue.Hours.AssertEqual(originalValue.Hours);
			readValue.Minutes.AssertEqual(originalValue.Minutes);
			readValue.Seconds.AssertEqual(originalValue.Seconds);
		}
	}

	#endregion

	#region String Round-Trip Tests

	[TestMethod]
	public async Task String_RoundTrip()
	{
		var testValues = new[]
		{
			"Hello",
			"FIX44",
			"SENDER123",
			"A",
			"1234567890",
		};

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.Symbol;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadStringAsync(CancellationToken);

			readTag.AssertEqual(tag);
			readValue.AssertEqual(originalValue);
		}
	}

	#endregion

	#region Char Round-Trip Tests

	[TestMethod]
	public async Task Char_RoundTrip()
	{
		var testValues = new[] { 'A', 'Z', '0', '9', 'Y', 'N' };

		foreach (var originalValue in testValues)
		{
			var (stream, reader, writer) = CreateStreams();
			var tag = FixTags.Side;

			await writer.WriteAsync(tag, CancellationToken);
			await writer.WriteAsync(originalValue, CancellationToken);
			await writer.FlushAsync(CancellationToken);

			stream.Position = 0;
			var readTag = await reader.ReadTagAsync(CancellationToken);
			var readValue = await reader.ReadCharAsync(CancellationToken);

			readTag.AssertEqual(tag);
			readValue.AssertEqual(originalValue);
		}
	}

	#endregion

	#region Complex Message Round-Trip

	[TestMethod]
	public async Task ComplexMessage_RoundTrip()
	{
		var (stream, reader, writer) = CreateStreams();

		var symbol = "AAPL";
		var side = '1'; // Buy
		var quantity = 100L;
		var price = 150.50m;
		var sendTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

		await writer.WriteAsync(FixTags.Symbol, CancellationToken);
		await writer.WriteAsync(symbol, CancellationToken);

		await writer.WriteAsync(FixTags.Side, CancellationToken);
		await writer.WriteAsync(side, CancellationToken);

		await writer.WriteAsync(FixTags.OrderQty, CancellationToken);
		await writer.WriteAsync(quantity, CancellationToken);

		await writer.WriteAsync(FixTags.Price, CancellationToken);
		await writer.WriteAsync(price, CancellationToken);

		await writer.WriteAsync(FixTags.SendingTime, CancellationToken);
		await writer.WriteAsync(sendTime, _dateTimeParser, CancellationToken);

		await writer.FlushAsync(CancellationToken);

		stream.Position = 0;

		var readSymbolTag = await reader.ReadTagAsync(CancellationToken);
		var readSymbol = await reader.ReadStringAsync(CancellationToken);

		var readSideTag = await reader.ReadTagAsync(CancellationToken);
		var readSide = await reader.ReadCharAsync(CancellationToken);

		var readQtyTag = await reader.ReadTagAsync(CancellationToken);
		var readQty = await reader.ReadLongAsync(CancellationToken);

		var readPriceTag = await reader.ReadTagAsync(CancellationToken);
		var readPrice = await reader.ReadDecimalAsync(CancellationToken);

		var readTimeTag = await reader.ReadTagAsync(CancellationToken);
		var readTime = await reader.ReadDateTimeAsync(_dateTimeParser, CancellationToken);

		readSymbolTag.AssertEqual(FixTags.Symbol);
		readSymbol.AssertEqual(symbol);

		readSideTag.AssertEqual(FixTags.Side);
		readSide.AssertEqual(side);

		readQtyTag.AssertEqual(FixTags.OrderQty);
		readQty.AssertEqual(quantity);

		readPriceTag.AssertEqual(FixTags.Price);
		readPrice.AssertEqual(price);

		readTimeTag.AssertEqual(FixTags.SendingTime);
		readTime.UtcKind().AssertEqual(sendTime);
	}

	#endregion
}
