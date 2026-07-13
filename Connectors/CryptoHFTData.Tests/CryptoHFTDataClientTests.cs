namespace StockSharp.CryptoHFTData.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Parquet.Serialization;

using StockSharp.CryptoHFTData.Native;

using StockSharp.Messages;

using ZstdSharp;

[TestClass]
public class CryptoHFTDataClientTests
{
	[TestMethod]
	public async Task ListsSymbolsFromMetadataEndpoint()
	{
		var handler = new StubHandler(request =>
		{
			Assert.AreEqual("https://example.test/symbols?exchange=binance_futures&data_type=trades", request.RequestUri.ToString());
			return Json("{\"symbols\":[\"BTCUSDT\",\"ETHUSDT\"]}");
		});
		using var client = new CryptoHFTDataClient("https://example.test", "key".Secure(), handler);

		var symbols = await client.GetSymbols("binance_futures", "trades", CancellationToken.None);

		CollectionAssert.AreEqual(new[] { "BTCUSDT", "ETHUSDT" }, symbols);
	}

	[TestMethod]
	public async Task DownloadsAndDeserializesCompressedTrades()
	{
		var expected = new[]
		{
			new TradeRow
			{
				ReceivedTime = 1_783_728_004_885_015_486,
				EventTime = 1_783_728_004_749,
				TradeTime = 1_783_728_004_749,
				Symbol = "KAVAUSDT",
				TradeId = 404_761_086,
				Price = "0.044570",
				Quantity = "1353.4",
				IsBuyerMaker = true,
			},
		};
		var payload = await CompressParquet(expected);
		var handler = new StubHandler(request =>
		{
			Assert.AreEqual("key", request.Headers.GetValues("X-API-Key").Single());
			StringAssert.Contains(request.RequestUri.Query, Uri.EscapeDataString("binance_futures/2026-07-11/00/KAVAUSDT_trades.parquet.zst"));
			return Bytes(payload);
		});
		using var client = new CryptoHFTDataClient("https://example.test", "key".Secure(), handler);

		var rows = await client.GetTrades(
			"binance_futures",
			"KAVAUSDT",
			new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
			new DateTime(2026, 7, 11, 0, 59, 59, DateTimeKind.Utc),
			CancellationToken.None);

		Assert.HasCount(1, rows);
		Assert.AreEqual(expected[0].TradeId, rows[0].TradeId);
		Assert.AreEqual(expected[0].Price, rows[0].Price);
		Assert.IsTrue(rows[0].IsBuyerMaker);
	}

	[TestMethod]
	public async Task MissingHourlyFilesProduceNoRows()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		using var client = new CryptoHFTDataClient("https://example.test", "key".Secure(), handler);

		var rows = await client.GetOrderBook(
			"bybit",
			"BTCUSDT",
			new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
			new DateTime(2026, 7, 11, 0, 59, 59, DateTimeKind.Utc),
			CancellationToken.None);

		Assert.IsEmpty(rows);
	}

	[TestMethod]
	public void MapsProviderRowsToStockSharpMessages()
	{
		var securityId = new SecurityId { SecurityCode = "KAVAUSDT", BoardCode = "binance_futures" };
		var trade = new TradeRow
		{
			ReceivedTime = 1_783_728_004_885_015_486,
			TradeTime = 1_783_728_004_749,
			TradeId = 404_761_086,
			Price = "0.044570",
			Quantity = "1353.4",
			IsBuyerMaker = true,
		};

		var tick = CryptoHFTDataMessageAdapter.ToExecutionMessage(trade, securityId, 42);

		Assert.AreEqual(DataType.Ticks, tick.DataTypeEx);
		Assert.AreEqual(0.044570m, tick.TradePrice);
		Assert.AreEqual(1353.4m, tick.TradeVolume);
		Assert.AreEqual(Sides.Sell, tick.OriginSide);
		Assert.AreEqual(42, tick.OriginalTransactionId);

		var depth = CryptoHFTDataMessageAdapter.ToQuoteChangeMessage(
			[
				new OrderBookRow { EventTime = 1_783_728_000_848, EventType = "update", FinalUpdateId = 100, Side = "bid", Price = "0.042360", Quantity = "70718.9" },
				new OrderBookRow { EventTime = 1_783_728_000_848, EventType = "update", FinalUpdateId = 100, Side = "ask", Price = "0.044000", Quantity = "0" },
			],
			securityId,
			43);

		Assert.AreEqual(QuoteChangeStates.Increment, depth.State);
		Assert.AreEqual(100, depth.SeqNum);
		Assert.HasCount(1, depth.Bids);
		Assert.HasCount(1, depth.Asks);
		Assert.AreEqual(0m, depth.Asks[0].Volume);

		var grouped = CryptoHFTDataMessageAdapter.GroupOrderBookRows(
			[
				new OrderBookRow { EventTime = 1, EventType = "update", FinalUpdateId = 1 },
				new OrderBookRow { EventTime = 2, EventType = "snapshot", LastUpdateId = 2 },
				new OrderBookRow { EventTime = 3, EventType = "update", FinalUpdateId = 3 },
			]).ToArray();

		Assert.HasCount(3, grouped);
		Assert.AreEqual("update", grouped[0][0].EventType);
	}

	[TestMethod]
	[TestCategory("Integration")]
	public async Task DownloadsLiveTradesAndOrderBook()
	{
		if (!Environment.GetEnvironmentVariable("RUN_CRYPTOHFTDATA_LIVE_TESTS").EqualsIgnoreCase("true"))
			Assert.Inconclusive("Set RUN_CRYPTOHFTDATA_LIVE_TESTS=true to run the live API test.");

		using var client = new CryptoHFTDataClient(
			"https://api.cryptohftdata.com",
			Environment.GetEnvironmentVariable("CRYPTOHFTDATA_API_KEY")?.Secure());
		var from = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
		var to = from.AddHours(1).AddTicks(-1);

		var trades = await client.GetTrades("binance_futures", "KAVAUSDT", from, to, CancellationToken.None);
		var orderBook = await client.GetOrderBook("binance_futures", "KAVAUSDT", from, to, CancellationToken.None);

		Assert.IsNotEmpty(trades);
		Assert.IsNotEmpty(orderBook);
		Assert.IsTrue(trades.All(t => t.TradeTime >= new DateTimeOffset(from).ToUnixTimeMilliseconds()));
		Assert.IsTrue(orderBook.All(q => q.EventTime >= new DateTimeOffset(from).ToUnixTimeMilliseconds()));
	}

	private static async Task<byte[]> CompressParquet<T>(IEnumerable<T> rows)
		where T : class, new()
	{
		await using var parquet = new MemoryStream();
		await ParquetSerializer.SerializeAsync(rows, parquet);
		using var compressor = new Compressor();
		return compressor.Wrap(parquet.ToArray()).ToArray();
	}

	private static HttpResponseMessage Json(string value)
		=> new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Bytes(byte[] value)
		=> new(HttpStatusCode.OK) { Content = new ByteArrayContent(value) };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(responseFactory(request));
	}
}
