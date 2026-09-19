namespace StockSharp.Tests;

/// <summary>
/// Zero values in every storage type: they must survive the round-trip as zero, not as null.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTime.UtcNow;
		var token = CancellationToken;
		var storage = GetTradeStorage(secId, format);

		var ticks = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 0,
				TradeVolume = 10,
				SecurityId = secId,
				ServerTime = now
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 2,
				TradePrice = 10,
				TradeVolume = 0,
				SecurityId = secId,
				ServerTime = now.AddSeconds(1)
			}
		};

		foreach (var tick in ticks)
		{
			await storage.SaveAsync([tick], token);
			var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
			loaded.CompareMessages([tick]);
			await storage.DeleteAsync([tick], token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task OrderLogZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTime.UtcNow;
		var token = CancellationToken;
		var storage = GetStorageRegistry().GetOrderLogMessageStorage(secId, null, format);

		var logs = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 1,
				OrderPrice = 0,
				OrderVolume = 10,
				OrderState = OrderStates.Active,
				Side = Sides.Buy,
				SecurityId = secId,
				ServerTime = now
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 2,
				OrderPrice = 10,
				OrderVolume = 0,
				OrderState = OrderStates.Done,
				Side = Sides.Sell,
				SecurityId = secId,
				ServerTime = now.AddSeconds(1)
			}
		};

		foreach (var log in logs)
		{
			await storage.SaveAsync([log], token);
			var loaded = await storage.LoadAsync(log.ServerTime, log.ServerTime).ToArrayAsync(token);
			loaded.CompareMessages([log]);
			await storage.DeleteAsync([log], token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1ZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTime.UtcNow;
		var token = CancellationToken;
		var storage = GetStorageRegistry().GetLevel1MessageStorage(secId, null, format);

		var l1 = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = now
			}.TryAdd(Level1Fields.LastTradePrice, 0m, true),
			new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = now.AddSeconds(1)
			}.TryAdd(Level1Fields.LastTradeVolume, 0m, true)
		};

		foreach (var msg in l1)
		{
			await storage.SaveAsync([msg], token);
			var loaded = await storage.LoadAsync(msg.ServerTime, msg.ServerTime).ToArrayAsync(token);
			loaded.CompareMessages([msg]);
			await storage.DeleteAsync([msg], token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var now = DateTime.UtcNow;
		var token = CancellationToken;
		var storage = GetStorageRegistry().GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = now,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 0m,
				HighPrice = 10m,
				LowPrice = 5m,
				ClosePrice = 7m,
				TotalVolume = 100m
			},
			new TimeFrameCandleMessage
			{
				OpenTime = now.AddMinutes(1),
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 1m,
				HighPrice = 0m,
				LowPrice = 0.5m,
				ClosePrice = 0.7m,
				TotalVolume = 100m
			},
			new TimeFrameCandleMessage
			{
				OpenTime = now.AddMinutes(2),
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 1m,
				HighPrice = 2m,
				LowPrice = 0m,
				ClosePrice = 1.5m,
				TotalVolume = 100m
			},
			new TimeFrameCandleMessage
			{
				OpenTime = now.AddMinutes(3),
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 1m,
				HighPrice = 2m,
				LowPrice = 0.5m,
				ClosePrice = 0m,
				TotalVolume = 100m
			},
			new TimeFrameCandleMessage
			{
				OpenTime = now.AddMinutes(4),
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 1m,
				HighPrice = 2m,
				LowPrice = 0.5m,
				ClosePrice = 1.5m,
				TotalVolume = 0m
			}
		};

		foreach (var candle in candles)
		{
			await storage.SaveAsync([candle], token);
			var loaded = await storage.LoadAsync(candle.OpenTime, candle.OpenTime).ToArrayAsync(token);
			loaded.CompareCandles([candle], format);
			await storage.DeleteAsync([candle], token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTime.UtcNow;
		var token = CancellationToken;
		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format);

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = now,
				Bids = [new(0, 1)],
				Asks = [new(0, 1)],
			},
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = now.AddSeconds(1),
				Bids = [new(1, 0)],
				Asks = [new(1, 0)],
			}
		};

		foreach (var depth in depths)
		{
			await storage.SaveAsync([depth], token);
			var loaded = await storage.LoadAsync(depth.ServerTime, depth.ServerTime).ToArrayAsync(token);
			loaded.CompareMessages([depth]);
			await storage.DeleteAsync([depth], token);
		}
	}
}
