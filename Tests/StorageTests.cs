namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Storages.Binary.Snapshot;
using StockSharp.Algo.Testing.Generation;

[TestClass]
[DoNotParallelize]
public class StorageTests : BaseTestClass
{
	private const int _tickCount = 5000;
	private const int _maxRenkoSteps = 100;
	private const int _depthCount1 = 10;
	private const int _depthCount2 = 1000;
	private const int _depthCount3 = 10000;
	private static readonly int[] _sourceArray = [01, 02, 03, 06, 07, 08, 09, 10, 13, 14, 15, 16, 17, 20, 21, 22, 23, 24, 27, 28, 29, 30];

	private static IStorageRegistry GetStorageRegistry()
	{
		var fs = Helper.FileSystem;
		return fs.GetStorage(fs.GetSubTemp());
	}

	private static IMarketDataStorage<ExecutionMessage> GetTradeStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetTickMessageStorage(security, null, format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена сделки.")]
	public async Task TickNegativePrice(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		await GetTradeStorage(secId, format).SaveAsync([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = -10,
			SecurityId = secId,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow
		}], token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickEmptySecurityBinary(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		await GetTradeStorage(secId, format).SaveAsync([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow,
		}], token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task TickInvalidSecurity2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetTradeStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentException>(async () => { await storage.SaveAsync(
		[
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 10,
				TradeVolume = 10,
				ServerTime = DateTime.UtcNow,
				SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux }
			}
		], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task TickInvalidSecurity3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetTradeStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentException>(async () => { await storage.SaveAsync([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			SecurityId = secId,
		}], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickRandom(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickStringId(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			foreach (var trade in trades)
			{
				if (!RandomGen.GetBool())
					continue;

				trade.TradeStringId = trade.TradeId.To<string>();

				if (RandomGen.GetBool())
					trade.TradeId = null;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickRandomLocalTime(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			foreach (var trade in trades)
			{
				trade.LocalTime = trade.ServerTime;
				trade.ServerTime = trade.ServerTime.AddYears(-1);
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickNanosec(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, interval: TimeSpan.FromTicks(16546));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickHighPrice(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = i * byte.MaxValue + 1;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickLowPrice(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			var priceStep = /*trades.First().Security.PriceStep = */0.00001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = (i + 1) * priceStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickExtremePrice(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			//trades.First().Security.PriceStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickExtremePrice2(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			//trades.First().Security.PriceStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickExtremeVolume(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			//trades.First().Security.VolumeStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradeVolume = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickExtremeVolume2(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			//trades.First().Security.VolumeStep = 0.0001m;

			foreach (var t in trades)
				t.TradeVolume = RandomGen.GetBool() ? decimal.MinValue : decimal.MaxValue;
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickNonSystem(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			for (var i = 0; i < trades.Length; i++)
			{
				if (i > 0 && RandomGen.GetInt(1000) % 10 == 0)
				{
					trades[i].IsSystem = false;
					trades[i].TradePrice = Math.Round(trades[i].TradePrice.Value / i, 10);
				}
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickFractionalVolume(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			var volumeStep = /*trades.First().Security.VolumeStep = */0.00001m;

			foreach (var trade in trades)
			{
				trade.TradeVolume *= volumeStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickFractionalVolume2(StorageFormats format)
	{
		await TickRandomSaveLoad(format, _tickCount, trades =>
		{
			var volumeStep = /*trades.First().Security.VolumeStep = */0.00001m;

			foreach (var trade in trades)
			{
				trade.TradeVolume *= (volumeStep * 0.1m);
			}
		});
	}

	private async Task TickRandomSaveLoad(StorageFormats format, int count, Action<ExecutionMessage[]> modify = null, TimeSpan? interval = null)
	{
		var security = Helper.CreateStorageSecurity();
		var trades = security.RandomTicks(count, false, interval);
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		modify?.Invoke(trades);

		var storage = GetTradeStorage(secId, format);
		await storage.SaveAsync(trades, token);
		await LoadTradesAndCompare(storage, trades);
		await storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickPartSave(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var trades = security.RandomTicks(_tickCount, false);

		const int halfTicks = _tickCount / 2;

		var tradeStorage = GetTradeStorage(secId, format);

		await tradeStorage.SaveAsync(trades.Take(halfTicks), token);
		await LoadTradesAndCompare(tradeStorage, [.. trades.Take(halfTicks)]);

		await tradeStorage.SaveAsync([.. trades.Skip(halfTicks)], token);
		await LoadTradesAndCompare(tradeStorage, [.. trades.Skip(halfTicks)]);

		await LoadTradesAndCompare(tradeStorage, trades);
		await tradeStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickRandomDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		await tradeStorage.SaveAsync(trades, token);

		var randomDeleteTrades = trades.Select(t => RandomGen.GetInt(5) == 2 ? null : t).WhereNotNull().ToList();
		await tradeStorage.DeleteAsync(randomDeleteTrades, token);

		await LoadTradesAndCompare(tradeStorage, [.. trades.Except(randomDeleteTrades)]);
		await tradeStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickFullDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		await tradeStorage.SaveAsync(trades, token);

		await tradeStorage.DeleteAsync(trades.First().ServerTime, trades.Last().ServerTime, token);

		var loadedTrades = await tradeStorage.LoadAsync(trades.First().ServerTime, trades.Last().ServerTime).ToArrayAsync(token);
		loadedTrades.Length.AssertEqual(0);

		await tradeStorage.SaveAsync(trades, token);

		await LoadTradesAndCompare(tradeStorage, trades);

		await tradeStorage.DeleteAsync(trades, token);

		loadedTrades = await tradeStorage.LoadAsync(trades.First().ServerTime, trades.Last().ServerTime).ToArrayAsync(token);
		loadedTrades.Length.AssertEqual(0);

		loadedTrades = await tradeStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loadedTrades.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickWrongDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		await GetTradeStorage(secId, format).DeleteAsync(new DateTime(2005, 1, 1), new DateTime(2005, 1, 10), token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickRandomDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		await tradeStorage.SaveAsync(trades, token);

		var minTime = DateTime.MaxValue;
		var maxTime = DateTime.MinValue;

		foreach (var t in trades)
		{
			minTime = t.ServerTime < minTime ? t.ServerTime : minTime;
			maxTime = t.ServerTime > maxTime ? t.ServerTime : maxTime;
		}

		var diff = maxTime - minTime;
		var third = TimeSpan.FromTicks(diff.Ticks / 3);

		var from = minTime + third;
		var to = maxTime - third;
		await tradeStorage.DeleteAsync(from, to, token);

		await LoadTradesAndCompare(tradeStorage, [.. trades.Where(t => t.ServerTime < from || t.ServerTime > to)]);

		await tradeStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var dt = DateTime.UtcNow;
		var token = CancellationToken;

		var tradeStorage = GetTradeStorage(secId, format);

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradeId = 1,
				TradePrice = 10,
				TradeVolume = 10,
				ServerTime = dt,
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradeId = 2,
				TradePrice = 10,
				TradeVolume = 10,
				ServerTime = dt,
			}
		};

		await tradeStorage.SaveAsync([trades[0]], token);
		await tradeStorage.SaveAsync([trades[1]], token);

		await LoadTradesAndCompare(tradeStorage, trades);
		await tradeStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadTradesAndCompare(IMarketDataStorage<ExecutionMessage> tradeStorage, ExecutionMessage[] trades)
	{
		var token = CancellationToken;
		
		var loadedTrades = await tradeStorage.LoadAsync(trades.First().ServerTime, trades.Last().ServerTime).ToArrayAsync(token);

		loadedTrades.CompareMessages(trades);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthAdaptivePriceStep(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		security.PriceStep = 0.0001m;

		var depths = security.RandomDepths(_depthCount2);

		security.PriceStep = 0.1m;

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format);

		var token = CancellationToken;

		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);

		await storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthLowPriceStep(StorageFormats format)
	{
		var token = CancellationToken;
		
		var security = Helper.CreateSecurity();
		security.PriceStep = 0.00000001m;

		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount2);

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format: format);

		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);

		await storage.DeleteWithCheckAsync(token);
	}

	private static IMarketDataStorage<QuoteChangeMessage> GetDepthStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetQuoteMessageStorage(security, null, format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthInvalidVolume(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.UtcNow,
			SecurityId = secId,
			Bids = [new(1, -1)],
		};

		var storage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => { await storage.SaveAsync([depth], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthInvalidSecurity(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux },
			Bids = [new(1, 1)],
		};

		var storage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentException>(async () => { await storage.SaveAsync([depth], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthInvalidOrder(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var storage = GetDepthStorage(secId, format);
		storage.AppendOnlyNew = false;
		await storage.SaveAsync([depth2], token);
		await storage.SaveAsync([depth1], token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthInvalidOrder2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth2], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);

		await depthStorage.SaveAsync([depth1], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthInvalidOrder3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.AppendOnlyNew = false;
		await depthStorage.SaveAsync([depth2], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);

		try
		{
			await depthStorage.SaveAsync([depth1], token);
		}
		catch
		{
			await depthStorage.DeleteWithCheckAsync(token);
			throw;
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Все переданные стаканы является пустыми.")]
	public async Task DepthInvalidEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.UtcNow,
			SecurityId = secId,
		};

		var depths = new[] { depth };

		var storage = GetDepthStorage(secId, format);
		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Переданный стакан является пустым.")]
	public async Task DepthEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
			Bids = [new(2, 2)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthNegativePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(-10, 1)],
			Asks = [new(-0.1m, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Asks = [new(-0.1m, 1), new(1, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
			Bids = [new(-10, 1)],
			Asks = [new(-0.1m, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthZeroPrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			Bids = [new(-0.1m, 1)],
			Asks = [new(0, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			Asks = [new(0, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			Bids = [new(-10, 1)],
			Asks = [new(0, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthEmpty2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthEmpty3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0) };
		var depth2 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1) };
		var depth3 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2) };

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthPartSave(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount2);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths.Take(500), token);
		await LoadDepthsAndCompare(depthStorage, [.. depths.Take(500)]);

		await depthStorage.SaveAsync([.. depths.Skip(500)], token);
		await LoadDepthsAndCompare(depthStorage, [.. depths.Skip(000)]);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthHalfFilled(StorageFormats format, int count)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depthStorage = GetDepthStorage(secId, format);

		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();

		var secMsg = security.ToMessage();

		generator.Process(secMsg);
		generator.Process(security.Board.ToMessage());

		var time = DateTime.UtcNow;

		var depths = new List<QuoteChangeMessage>();

		var token = CancellationToken;

		for (var x = 0; x < count; x++)
		{
			var isBids = RandomGen.GetBool();
			var maxDepth = RandomGen.GetInt(1, 5);

			generator.MaxBidsDepth = isBids ? maxDepth : 0;
			generator.MaxAsksDepth = isBids ? 0 : maxDepth;

			if (generator.MaxBidsDepth == 0 && generator.MaxAsksDepth == 0)
				continue;

			depths.Add((QuoteChangeMessage)generator.Process(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = RandomGen.GetInt(100, 120),
				TradeVolume = RandomGen.GetInt(1, 20),
				ServerTime = time.AddDays(x),
				SecurityId = secId,
			}));
		}

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		var from = time;
		var to = from.AddDays(count + 1);

		await depthStorage.DeleteAsync(from, to, token);

		var loadedDepths = await depthStorage.LoadAsync(from, to).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthHalfFilled(StorageFormats format)
	{
		await DepthHalfFilled(format, _depthCount1);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandom(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomOrdersCount(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, ordersCount: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomConditions(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, conditions: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremePrice(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.PriceStep = 0.0001m;

			foreach (var depth in depths)
			{
				var prices = Enumerable
					.Repeat(0, RandomGen.GetInt(20))
					.Select(i => RandomGen.GetDecimal())
					.OrderBy(v => v)
					.Distinct()
					.ToArray();

				var bidCount = RandomGen.GetInt(prices.Length);

				var bids = Enumerable
					.Repeat(0, bidCount)
					.Select((i, ind) => new QuoteChange(prices[ind], RandomGen.GetInt()))
					.OrderBy(q => 0 - q.Price)
					.ToArray();

				var asks = Enumerable
					.Repeat(0, prices.Length - bidCount)
					.Select((i, ind) => new QuoteChange(prices[bidCount + ind], RandomGen.GetInt()))
					.OrderBy(q => q.Price)
					.ToArray();

				depth.Bids = bids;
				depth.Asks = asks;
				//depth.ServerTime;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremeVolume(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.VolumeStep = 0.0001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume = RandomGen.GetDecimal();

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremeVolume2(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.VolumeStep = 0.0001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume = RandomGen.GetBool() ? 1 : decimal.MaxValue;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume = RandomGen.GetBool() ? 1 : decimal.MaxValue;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomNanosec(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, interval: TimeSpan.FromTicks(14465));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFractionalVolume(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			var volumeStep = /*depths.First().Security.VolumeStep = */0.00001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume *= volumeStep;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume *= volumeStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFractionalVolume2(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			var volumeStep = /*depths.First().Security.VolumeStep = */0.00001m;

			foreach (var depth in depths)
			{
				var volume = volumeStep * 0.1m;

				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume *= volume;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume *= volume;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var dt = DateTime.UtcNow;

		var depthStorage = GetDepthStorage(secId, format);

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				ServerTime = dt,
				SecurityId = secId,
				Bids = [new(10, 1)],
			},
			new QuoteChangeMessage
			{
				ServerTime = dt,
				SecurityId = secId,
				Bids = [new(11, 1)],
			},
		};

		await depthStorage.SaveAsync([depths[0]], token);
		await depthStorage.SaveAsync([depths[1]], token);

		await LoadDepthsAndCompare(depthStorage, depths);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthRandom(StorageFormats format, int count, Action<QuoteChangeMessage[]> modify = null, TimeSpan? interval = null, bool ordersCount = false, bool conditions = false)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(count, interval, null, ordersCount);

		if (conditions)
		{
			foreach (var depth in depths)
			{
				if (!RandomGen.GetBool())
					continue;

				if (depth.Bids.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Bids.Length - 1);
					var q = depth.Bids[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Bids[idx] = q;
				}

				if (depth.Asks.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Asks.Length - 1);
					var q = depth.Asks[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Asks[idx] = q;
				}
			}
		}

		modify?.Invoke(depths);

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(depthStorage, depths);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3, TimeSpan.FromSeconds(2));

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		var randomDeleteDepths = depths.Select(d => RandomGen.GetInt(5) == 2 ? null : d).WhereNotNull().ToList();
		await depthStorage.DeleteAsync(randomDeleteDepths, token);

		await LoadDepthsAndCompare(depthStorage, [.. depths.Except(randomDeleteDepths).OrderBy(d => d.ServerTime)]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFullDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);

		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteAsync(depths, token);

		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		loadedDepths = await depthStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		var now = DateTime.UtcNow;

		var from = now + TimeSpan.FromMinutes(_depthCount3 / 2);
		var to = now + TimeSpan.FromMinutes(3 * _depthCount3 / 2);
		await depthStorage.DeleteAsync(from, to, token);

		await LoadDepthsAndCompare(depthStorage, [.. depths.Where(d => d.ServerTime < from || d.ServerTime > to).OrderBy(d => d.ServerTime)]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadDepthsAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var token = CancellationToken;
		
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomLessMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		const int depthSize = 20;

		var depths = security.RandomDepths(_depthCount3, new TrendMarketDepthGenerator(secId)
		{
			MaxBidsDepth = depthSize,
			MaxAsksDepth = depthSize,
		});

		//storage.MarketDepthMaxDepth = depthSize / 2;

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);
		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomMoreMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		//storage.MarketDepthMaxDepth = 20;

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);
		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthRandomIncrement(StorageFormats format, bool ordersCount, bool conditions)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount2, null, null, ordersCount);

		if (conditions)
		{
			foreach (var depth in depths)
			{
				if (!RandomGen.GetBool())
					continue;

				if (depth.Bids.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Bids.Length - 1);
					var q = depth.Bids[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Bids[idx] = q;
				}

				if (depth.Asks.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Asks.Length - 1);
					var q = depth.Asks[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Asks[idx] = q;
				}
			}
		}

		var diffQuotes = new List<QuoteChangeMessage>();

		for (var i = depths.Length - 1; i > 0; i--)
		{
			diffQuotes.Add(depths[i - 1].GetDelta(depths[i]));
		}

		diffQuotes.Reverse();

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(diffQuotes, token);
		await LoadQuotesAndCompare(depthStorage, diffQuotes);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadQuotesAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var token = CancellationToken;
		
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrement(StorageFormats format)
	{
		return DepthRandomIncrement(format, false, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementOrders(StorageFormats format)
	{
		return DepthRandomIncrement(format, true, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementOrdersConditions(StorageFormats format)
	{
		return DepthRandomIncrement(format, true, true);
	}

	private Task DepthRandomIncrementNonIncrement(StorageFormats format, bool isStateFirst)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? QuoteChangeStates.SnapshotComplete : null,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? null : QuoteChangeStates.SnapshotComplete,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<InvalidOperationException>(async () => { await depthStorage.SaveAsync(depths, token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementNonIncrement(StorageFormats format)
	{
		return DepthRandomIncrementNonIncrement(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementNonIncrement2(StorageFormats format)
	{
		return DepthRandomIncrementNonIncrement(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomIncrementNonIncrement3(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = QuoteChangeStates.SnapshotComplete,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = null,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(depths.Take(1), token);
		await ThrowsExactlyAsync<ArgumentException>(async () => await depthStorage.SaveAsync(depths.Skip(1), token));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesExtremePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		var trades = security.RandomTicks(_tickCount, false);

		foreach (var trade in trades)
		{
			trade.TradePrice = RandomGen.GetDecimal();
			trade.TradeVolume = RandomGen.GetDecimal();
		}

		return CandlesRandom(format, trades, security, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesNoProfile(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		return CandlesRandom(format, security.RandomTicks(_tickCount, false), security, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public Task CandlesWithProfile(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		return CandlesRandom(format, security.RandomTicks(_tickCount, true), security, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesActive(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = TimeSpan.FromMinutes(5);
		var time = new DateTime(2017, 10, 02, 15, 30, 00).UtcKind();

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 101,
				HighPrice = 104.4m,
				LowPrice = 99,
				ClosePrice = 99.3m,
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 101,
				HighPrice = 104.4m,
				LowPrice = 99,
				ClosePrice = 99.3m,
				State = CandleStates.Finished,
				BuildFrom = DataType.Ticks,
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf + tf,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 101,
				HighPrice = 104.4m,
				LowPrice = 99,
				ClosePrice = 99.3m,
				State = CandleStates.Active,
			},
		};

		var candleStorage = GetStorageRegistry().GetTimeFrameCandleMessageStorage(secId, tf);

		await candleStorage.SaveAsync(candles, token);

		var loadedCandles = await candleStorage.LoadAsync(candles.First().OpenTime, candles.Last().OpenTime).ToArrayAsync(token);
		loadedCandles.CompareCandles([.. candles.Where(c => c.State != CandleStates.Active)], format);
		await candleStorage.DeleteAsync(loadedCandles, token);

		foreach (var candle in candles)
		{
			await candleStorage.SaveAsync([candle], token);
		}

		loadedCandles = await candleStorage.LoadAsync(candles.First().OpenTime, candles.Last().OpenTime).ToArrayAsync(token);
		loadedCandles.CompareCandles([.. candles.Where(c => c.State != CandleStates.Active)], format);
		await candleStorage.DeleteAsync(loadedCandles, token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesDuplicate(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = TimeSpan.FromMinutes(5);
		var time = new DateTime(2017, 10, 02, 15, 30, 00).UtcKind();

		var candles = new CandleMessage[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 101,
				HighPrice = 104.4m,
				LowPrice = 99,
				ClosePrice = 99.3m,
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time,
				SecurityId = secId,
				TypedArg = tf,
				OpenPrice = 101,
				HighPrice = 104.4m,
				LowPrice = 99,
				ClosePrice = 99.3m,
				State = CandleStates.Finished,
			},
		};

		var candleStorage = GetStorageRegistry().GetTimeFrameCandleMessageStorage(secId, tf);

		await candleStorage.SaveAsync(candles, token);

		var loadedCandles = await candleStorage.LoadAsync(candles.First().OpenTime, candles.Last().OpenTime).ToArrayAsync(token);
		loadedCandles.CompareCandles([.. candles.Take(1)], format);
		await candleStorage.DeleteAsync(loadedCandles, token);

		foreach (var candle in candles)
		{
			await candleStorage.SaveAsync([candle], token);
		}

		loadedCandles = await candleStorage.LoadAsync(candles.First().OpenTime, candles.Last().OpenTime).ToArrayAsync(token);
		loadedCandles.CompareCandles([.. candles.Take(1)], format);
		await candleStorage.DeleteAsync(loadedCandles, token);
	}

	private static ExecutionMessage[] GenerateFactalVolumeTrades(Security security, decimal modifier)
	{
		var secMsg = security.ToMessage();

		var trades = new List<ExecutionMessage>();

		var tradeGenerator = new RandomWalkTradeGenerator(secMsg.SecurityId);
		tradeGenerator.Init();

		var now = DateTime.UtcNow;

		tradeGenerator.Process(secMsg);
		tradeGenerator.Process(new Level1ChangeMessage
		{
			SecurityId = secMsg.SecurityId,
			ServerTime = now,
		}.TryAdd(Level1Fields.LastTradeTime, now));

		for (var i = 0; i < _tickCount; i++)
		{
			var msg = (ExecutionMessage)tradeGenerator.Process(new TimeMessage
			{
				ServerTime = now + TimeSpan.FromSeconds(i + 1)
			});

			msg.TradeVolume *= security.VolumeStep * modifier;

			trades.Add(msg);
		}

		return [.. trades];
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesFractionalVolume(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		return CandlesRandom(format, GenerateFactalVolumeTrades(security, 1), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesFractionalVolume2(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		return CandlesRandom(format, GenerateFactalVolumeTrades(security, 0.1m), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	private async Task CandlesRandom(
		StorageFormats format,
		ExecutionMessage[] trades,
		Security security, bool isCalcVolumeProfile,
		bool resetPriceStep = false,
		decimal volumeRange = CandleTests.VolumeRange,
		decimal boxSize = CandleTests.BoxSize)
	{
		var minPrice = decimal.MaxValue;
		var maxPrice = decimal.MinValue;

		foreach (var t in trades)
		{
			minPrice = Math.Min(minPrice, t.TradePrice.Value);
			maxPrice = Math.Max(maxPrice, t.TradePrice.Value);
		}

		decimal numSteps;
		try
		{
			numSteps = (maxPrice - minPrice) / boxSize;
		}
		catch (OverflowException)
		{
			numSteps = _maxRenkoSteps;
		}

		if (numSteps > _maxRenkoSteps)
			boxSize = (maxPrice - minPrice) / _maxRenkoSteps;

		var tfArg = CandleTests.TimeFrame;
		var ticksArg = CandleTests.TotalTicks;
		var rangeArg = CandleTests.PriceRange.Pips(security);
		var renkoArg = boxSize.Pips(security);
		var pnfArg = CandleTests.PnF(security, boxSize);

		var candles = CandleTests.GenerateCandles(trades, security, rangeArg, ticksArg, tfArg, volumeRange, renkoArg, pnfArg, isCalcVolumeProfile);

		var storage = GetStorageRegistry();

		if (resetPriceStep)
			security.PriceStep = 1;

		var secId = security.ToSecurityId();

		await CheckCandles<TimeFrameCandleMessage, TimeSpan>(storage, secId, candles, tfArg, format);
		await CheckCandles<VolumeCandleMessage, decimal>(storage, secId, candles, volumeRange, format);
		await CheckCandles<TickCandleMessage, int>(storage, secId, candles, ticksArg, format);
		await CheckCandles<RangeCandleMessage, Unit>(storage, secId, candles, rangeArg, format);
		await CheckCandles<RenkoCandleMessage, Unit>(storage, secId, candles, renkoArg, format);
		await CheckCandles<PnFCandleMessage, PnFArg>(storage, secId, candles, pnfArg, format);
	}

	private async Task CheckCandles<TCandle, TArg>(IStorageRegistry storage, SecurityId security, IEnumerable<CandleMessage> candles, TArg arg, StorageFormats format)
		where TCandle : CandleMessage
	{
		var token = CancellationToken;
		
		var candleStorage = storage.GetCandleMessageStorage(security, DataType.Create<TCandle>(arg), null, format);
		var typedCandle = candles.OfType<TCandle>().ToArray();

		await candleStorage.SaveAsync(typedCandle, token);
		var loadedCandles = await candleStorage.LoadAsync(typedCandle.First().OpenTime, typedCandle.Last().OpenTime).ToArrayAsync(token);
		loadedCandles.CompareCandles([.. typedCandle.Where(c => c.State != CandleStates.Active)], format);
		await candleStorage.DeleteAsync(loadedCandles, token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesInvalid(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), null, format);

		var candles = new[] { new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId } };

		try
		{
			await ThrowsExactlyAsync<ArgumentException>(async () => { await tfStorage.SaveAsync(candles, token); });
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesInvalid2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), null, format);

		var candles = new[]
		{
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(5), SecurityId = secId },
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId }
		};

		try
		{
			await ThrowsExactlyAsync<ArgumentException>(async () => { await tfStorage.SaveAsync(candles, token); });
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesInvalid4(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		ThrowsExactly<ArgumentNullException>(() => storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(0), null, format));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = TimeSpan.FromMinutes(5);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = DateTime.UtcNow;

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},
		};

		try
		{
			await tfStorage.SaveAsync(candles, token);
			await tfStorage.SaveAsync(candles, token);

			var loaded = (await tfStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Cast<TimeFrameCandleMessage>().ToArray();
			loaded.CompareCandles(candles, format);
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	private async Task CandlesTimeFrame(StorageFormats format, TimeSpan tf, DateTime? time = null)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		time ??= DateTime.UtcNow;

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time.Value + tf,
				CloseTime = time.Value + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time.Value + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},
		};

		foreach (var candle in candles)
		{
			candle.OpenTime = candle.OpenTime;
			candle.CloseTime = candle.CloseTime;

			candle.LowTime = candle.OpenTime;
			candle.HighTime = candle.CloseTime;
		}

		try
		{
			await tfStorage.SaveAsync(candles, token);
			(await tfStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).CompareCandles(candles, format);
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesMiniTimeFrame(StorageFormats format)
	{
		return CandlesTimeFrame(format, TimeSpan.FromMilliseconds(100));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesMiniTimeFrame2(StorageFormats format)
	{
		return CandlesTimeFrame(format, TimeSpan.FromMinutes(1.0456));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesBigTimeFrame(StorageFormats format)
	{
		return CandlesTimeFrame(format, TimeSpan.FromHours(100));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesBigTimeFrame2(StorageFormats format)
	{
		return CandlesTimeFrame(format, TimeSpan.FromHours(100.4570456));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesDiffDates(StorageFormats format)
	{
		return CandlesTimeFrame(format, TimeSpan.FromHours(3), new DateTime(2019, 1, 1, 20, 00, 00).UtcKind());
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesDiffDaysOffsets(StorageFormats format)
	{
		await CandlesDiffDaysOffsets(format, false);
		await CandlesDiffDaysOffsets(format, true);
	}

	private async Task CandlesDiffDaysOffsets(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTime(2019, 05, 06, 17, 1, 1).UtcKind();

		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = (time + tf),//.AddTicks(-1),
			OpenPrice = 10,
			HighPrice = 20,
			LowPrice = 9,
			ClosePrice = 9,
			TypedArg = tf,
			SecurityId = secId
		};

		candle.OpenTime = candle.OpenTime;
		candle.CloseTime = candle.CloseTime;

		if (initHighLow)
		{
			candle.LowTime = candle.OpenTime;
			candle.HighTime = candle.CloseTime;
		}

		var candles = new[] { candle };

		try
		{
			await tfStorage.SaveAsync(candles, token);
			(await tfStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).CompareCandles(candles, format);
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesDiffOffsets(StorageFormats format)
	{
		return CandlesDiffOffsets(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task CandlesDiffOffsetsIntraday(StorageFormats format)
	{
		return CandlesDiffOffsets(format, true);
	}

	private async Task CandlesDiffOffsets(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = initHighLow ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTime(2019, 05, 06, 17, 1, 1);

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time,
				CloseTime = time + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf,
				CloseTime = time + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf + tf,
				CloseTime = time + tf + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},
		};

		foreach (var candle in candles)
		{
			candle.CloseTime = candle.CloseTime.AddTicks(-1);

			candle.OpenTime = candle.OpenTime;
			candle.CloseTime = candle.CloseTime;

			if (initHighLow)
			{
				candle.LowTime = candle.OpenTime;
				candle.HighTime = candle.CloseTime;
			}
		}

		try
		{
			foreach (var candle in candles)
			{
				await tfStorage.SaveAsync([candle], token);
			}

			(await tfStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).CompareCandles(candles, format);
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task CandlesDiffOffsets2(StorageFormats format)
	{
		await CandlesDiffOffsets2(format, true);
		await CandlesDiffOffsets2(format, false);
	}

	private async Task CandlesDiffOffsets2(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var tf = initHighLow ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTime(2019, 05, 06, 17, 1, 1).UtcKind();

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time,
				CloseTime = time + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf,
				CloseTime = time + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = time + tf + tf,
				CloseTime = time + tf + tf + tf,
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},
		};

		foreach (var candle in candles)
		{
			candle.CloseTime = candle.CloseTime.AddTicks(-1);

			candle.OpenTime = candle.OpenTime;
			candle.CloseTime = candle.CloseTime;

			if (initHighLow)
			{
				candle.LowTime = candle.OpenTime;
				candle.HighTime = candle.CloseTime;
			}
		}

		try
		{
			foreach (var candle in candles)
			{
				await tfStorage.SaveAsync([candle], token);
			}

			(await tfStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).CompareCandles(candles, format);
		}
		finally
		{
			await tfStorage.DeleteAsync(candles, token);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogRandom(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _depthCount3);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogFractionalVolume(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _depthCount3, items =>
		{
			var volumeStep = /*items.First().Order.Security.VolumeStep = */0.00001m;

			foreach (var item in items)
			{
				item.OrderVolume *= volumeStep;

				if (item.TradeVolume is not null)
					item.TradeVolume *= volumeStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogFractionalVolume2(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _depthCount3, items =>
		{
			var volumeStep = /*items.First().Order.Security.VolumeStep = */0.00001m;

			foreach (var item in items)
			{
				item.OrderVolume *= volumeStep * 0.1m;

				if (item.TradeVolume is not null)
					item.TradeVolume *= volumeStep * 0.1m;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogExtreme(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _depthCount3, items =>
		{
			foreach (var item in items)
			{
				item.OrderPrice = RandomGen.GetDecimal();
				item.OrderVolume = RandomGen.GetBool() || item.OrderState == OrderStates.Active ? decimal.MaxValue : 0;

				if (item.TradePrice is not null)
				{
					item.TradePrice = RandomGen.GetDecimal();
					//item.TradeVolume = RandomGen.GetBool() ? decimal.MaxValue : null;
				}
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task OrderLogNonSystem(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var quotes = security.RandomOrderLog(_depthCount3);

		for (var i = 0; i < quotes.Length; i++)
		{
			if (i > 0 && RandomGen.GetInt(1000) % 10 == 0)
			{
				var item = quotes[i];

				item.IsSystem = false;
				item.OrderPrice = Math.Round(item.OrderPrice / i, 10);

				if (item.TradePrice is not null)
				{
					item.IsSystem = false;

					if (RandomGen.GetInt(1000) % 20 == 0)
						item.TradePrice = Math.Round(item.TradePrice.Value / i, 10);
				}
			}
		}

		var storage = GetStorageRegistry();

		var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
		await logStorage.SaveAsync(quotes, token);
		await LoadOrderLogAndCompare(logStorage, quotes);
		await logStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task OrderLogSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var dt = DateTime.UtcNow;
		var token = CancellationToken;

		var olStorage = GetStorageRegistry().GetOrderLogMessageStorage(secId, null, format);

		var ol = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 1,
				OrderPrice = 100,
				OrderState = OrderStates.Active,
				SecurityId = secId,
				LocalTime = dt,
				ServerTime = dt,
				OrderVolume = 1,
				TransactionId = 1,
				PortfolioName = Messages.Extensions.AnonymousPortfolioName,
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 1,
				OrderPrice = 100,
				OrderState = OrderStates.Done,
				SecurityId = secId,
				LocalTime = dt,
				ServerTime = dt,
				OrderVolume = 1,
				TransactionId = 2,
				PortfolioName = Messages.Extensions.AnonymousPortfolioName,
			},
		}.ToArray();

		await olStorage.SaveAsync([ol[0]], token);
		await olStorage.SaveAsync([ol[1]], token);

		await LoadOrderLogAndCompare(olStorage, ol);
		await olStorage.DeleteWithCheckAsync(token);
	}

	private async Task OrderLogRandomSaveLoad(StorageFormats format, int count, Action<IEnumerable<ExecutionMessage>> modify = null)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var items = security.RandomOrderLog(count);

		modify?.Invoke(items);

		var storage = GetStorageRegistry();

		var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
		await logStorage.SaveAsync(items, token);
		await LoadOrderLogAndCompare(logStorage, items);
		await logStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadOrderLogAndCompare(IMarketDataStorage<ExecutionMessage> storage, IList<ExecutionMessage> items)
	{
		var token = CancellationToken;
		var loadedItems = await storage.LoadAsync(items.First().ServerTime, items.Last().ServerTime).ToArrayAsync(token);
		loadedItems.CompareMessages(items);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task News(StorageFormats format)
	{
		var newsStorage = GetStorageRegistry().GetNewsMessageStorage(null, format);
		var token = CancellationToken;

		var news = Helper.RandomNews();

		await newsStorage.SaveAsync(news, token);

		var loaded = await newsStorage.LoadAsync(news.First().ServerTime, news.Last().ServerTime).ToArrayAsync(token);

		loaded.CompareMessages(news);

		await newsStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task BoardState(StorageFormats format)
	{
		var storage = GetStorageRegistry().GetBoardStateMessageStorage(null, format);
		var token = CancellationToken;

		var data = Helper.RandomBoardStates();

		await storage.SaveAsync(data, token);

		var loaded = await storage.LoadAsync(data.First().ServerTime, data.Last().ServerTime).ToArrayAsync(token);

		loaded.CompareMessages(data);

		await storage.DeleteWithCheckAsync(token);
	}

	private async Task Level1(StorageFormats format, bool isFractional, bool diffDays = false)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = security.RandomLevel1(isFractional, diffDays, _depthCount3);

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.CompareMessages(testValues);

		var loadedItems = await l1Storage.LoadAsync(testValues.First().ServerTime, testValues.Last().ServerTime).ToArrayAsync(token);
		loadedItems.CompareMessages(testValues);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1(StorageFormats format)
	{
		return Level1(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1Empty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = DateTime.UtcNow,
			}
		};

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.Count().AssertEqual(0);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	//[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1DiffDays(StorageFormats format)
	{
		return Level1(format, false, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1Fractional(StorageFormats format)
	{
		return Level1(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1MinMax(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var token = CancellationToken;

		security.PriceStep = security.MinPrice = 0.0000001m;
		security.MaxPrice = 100000000m;

		var securityId = security.ToSecurityId();
		var serverTime = DateTime.UtcNow;

		var testValues = new List<Level1ChangeMessage>
		{
			new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = serverTime,
			}.TryAdd(Level1Fields.MinPrice, security.MinPrice)
		};

		serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

		testValues.Add(new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
		}.TryAdd(Level1Fields.MaxPrice, security.MaxPrice));

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.CompareMessages(testValues);

		var loadedItems = await l1Storage.LoadAsync(testValues.First().ServerTime, testValues.Last().ServerTime).ToArrayAsync(token);
		loadedItems.CompareMessages(testValues);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	//[DataTestMethod]
	//[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	//public void Level1Duplicates(StorageFormats format)
	//{
	//	var security = Helper.CreateSecurity();

	//	var securityId = security.ToSecurityId();
	//	var serverTime = DateTime.UtcNow;

	//	var testValues = new List<Level1ChangeMessage>();

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}.TryAdd(Level1Fields.LastTradePrice, 1000m));

	//	serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}.TryAdd(Level1Fields.LastTradePrice, 1000m));

	//	serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}
	//	.TryAdd(Level1Fields.LastTradePrice, 1000m)
	//	.TryAdd(Level1Fields.BestBidPrice, 999m)
	//	);

	//	var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

	//	await l1Storage.SaveAsync(testValues, token);
	//	var loaded = await l1Storage.LoadAsync(DateTime.MinValue, token).ToArrayAsync(token);

	//	testValues.RemoveAt(0);
	//	testValues[1].Changes.Remove(Level1Fields.LastTradePrice);

	//	var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
	//	loaded.CompareMessages(testValues);

	//	await l1Storage.DeleteWithCheckAsync(token);
	//}

	[TestMethod]
	public async Task Securities()
	{
		var exchangeProvider = ServicesRegistry.ExchangeInfoProvider;
		var securities = Helper.RandomSecurities().Select(s => s.ToSecurity(exchangeProvider)).ToArray();
		var token = CancellationToken;
		var fs = Helper.MemorySystem;
		var executor = TimeSpan.FromSeconds(5).CreateExecutorAndRun(err => { }, token);
		var registry = fs.GetEntityRegistry(executor);

		var storage = registry.Securities;

		foreach (var security in securities)
		{
			await storage.SaveAsync(security, true, token);
		}

		storage = registry.Securities;
		var loaded = await storage.LookupAllAsync().ToArrayAsync(token);

		loaded.Length.AssertEqual(securities.Length);

		for (var i = 0; i < loaded.Length; i++)
		{
			Helper.CheckEqual(securities[i], loaded[i]);
		}

		await storage.DeleteAllAsync(token);
		(await storage.LookupAllAsync().ToArrayAsync(token)).Count().AssertEqual(0);

		await registry.DisposeAsync();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Transaction(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var transactions = security.RandomTransactions(1000);

		var storage = GetStorageRegistry().GetTransactionStorage(secId, null, format);

		await storage.SaveAsync(transactions, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.CompareMessages(transactions);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Position(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var testValues = security.RandomPositionChanges();
		var token = CancellationToken;

		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		await storage.SaveAsync(testValues, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		testValues = [.. testValues.Where(t => t.HasChanges())];

		loaded.CompareMessages(testValues);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task PositionEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = new[]
		{
			new PositionChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
			},
		};

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		await storage.SaveAsync(testValues, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.Length.AssertEqual(0);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	[TestMethod]
	public void FolderNames()
	{
		var secIds = new[]
		{
			"USD/EUR@DUCAS",
			"AAPL@NASDAQ",
			"AAPL@NASDAQ",
			"A:A:PL@NASDAQ",
			":AAPL:@NASDAQ",
			"::AAPL:@NASDAQ",
			"*AAPL*@NASDAQ",
			"*AA*PL*@NASDAQ",
			"AA*PL@NASDAQ",
			"NUL@NASDAQ",
			"NULL@NASDAQ",
			"SCOM5@NASDAQ",
			"COM5S@NASDAQ",
			"LPT9*@NASDAQ",
			":LPT9*@NASDAQ",
			"COM|5S@NASDAQ",
			"LPT9|@NASDAQ",
			"|LPT9*@NASDAQ",
			".LPT9@NASDAQ",
			"..LPT9@NASDAQ",
			".LPT9.@NASDAQ",
			"...LPT9.@NASDAQ",
			".?9.@NASDAQ",
			"?@NASDAQ",
			"?@?",
			"USD\\EUR@DUCAS",
			"USD\\EUR@DUCAS\\GLOBAL",
		};

		const string namesFolder = "FolderNames";
		var fs = Helper.FileSystem;

		foreach (var secId in secIds)
		{
			var folderName = secId.SecurityIdToFolderName();
			folderName.FolderNameToSecurityId().AssertEqual(secId);

			var di = Directory.CreateDirectory(Path.Combine(fs.GetSubTemp(namesFolder), folderName));
			di.Parent.Name.AssertEqual(namesFolder);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Bounds(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var storage = GetStorageRegistry().GetTickMessageStorage(secId, null, format);

		var now = DateTime.UtcNow;

		await storage.SaveAsync(
		[
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradePrice = 150,
				ServerTime = now,
				TradeVolume = 1,
				TradeId = 10
			}
		], token);

		now = now.ToUniversalTime().Date;

		(await storage.LoadAsync(now, default).ToArrayAsync(token)).Length.AssertEqual(1);
		(await storage.LoadAsync(now, DateTime.MaxValue).ToArrayAsync(token)).Length.AssertEqual(1);
		(await storage.LoadAsync(now.EndOfDay(), DateTime.MaxValue).ToArrayAsync(token)).Length.AssertEqual(0);
		(await storage.LoadAsync(now.EndOfDay(), DateTime.Today).ToArrayAsync(token)).Length.AssertEqual(0);
		(await storage.LoadAsync(now.AddDays(10), DateTime.MaxValue).ToArrayAsync(token)).Length.AssertEqual(0);

		await storage.DeleteAsync(now, default, token);
		(await storage.LoadAsync(now, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	[TestMethod]
	public void Snapshot()
	{
		static void Check<TKey, TMessage>(ISnapshotSerializer<TKey, TMessage> serializer, TMessage message)
			where TMessage : Message
		{
			Helper.CheckEqual(message, serializer.Deserialize(serializer.Version, serializer.Serialize(serializer.Version, message)));
		}

		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var books = security.RandomDepths(100);

		foreach (var book in books)
		{
			Check(new QuotesBinarySnapshotSerializer(), book);
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new Level1BinarySnapshotSerializer(), Helper.RandomLevel1(security, secId, DateTime.UtcNow, RandomGen.GetBool(), RandomGen.GetBool(), () => 1m));
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new PositionBinarySnapshotSerializer(), Helper.RandomPositionChange(secId));
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new TransactionBinarySnapshotSerializer(), Helper.RandomTransaction(secId, i));
		}
	}

	private static readonly DateTime _regressionFrom = DateTime.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture).ApplyMoscow().UtcDateTime;
	private static readonly DateTime _regressionTo = DateTime.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture).ApplyMoscow().UtcDateTime;

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildFromSmallerTimeframes(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes

		var token = CancellationToken;
		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var expectedDates = _sourceArray.Select(d => new DateTime(2021, 12, d)).ToHashSet();
		var dates = (await buildableStorage.GetDatesAsync(CancellationToken)).ToHashSet();

		expectedDates.SetEquals(dates).AssertTrue();

		var candles = await buildableStorage.LoadAsync(_regressionFrom, _regressionTo).ToArrayAsync(token);
		candles.Length.AssertEqual(expectedDates.Count);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildFromSmallerTimeframesCandleOrder(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes using original issue data, ensure candle updates are ordered in time and not doubled

		var token = CancellationToken;
		var secId = "SBER1@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var candles = await buildableStorage.LoadAsync(_regressionFrom, _regressionTo).ToArrayAsync(token);

		CandleMessage prevCandle = null;

		foreach (var c in candles)
		{
			if(prevCandle == null)
			{
				prevCandle = c.TypedClone();
				continue;
			}

			(c.OpenTime > prevCandle.OpenTime ||
				c.OpenTime == prevCandle.OpenTime && prevCandle.State == CandleStates.Active).AssertTrue();

			prevCandle = c.TypedClone();
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildableRange(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/SS-192

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();
		var token = CancellationToken;

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var range = await buildableStorage.GetRangeAsync(_regressionFrom, _regressionTo, token);

		range.Min.AssertEqual(range.Min.Date);
		range.Max.AssertEqual(range.Max.Date);
	}

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

	[TestMethod]
	public void Index_AddAndGetDates()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add dates
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);

		// Get dates
		var dates = index.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(2);
		dates[0].AssertEqual(date1);
		dates[1].AssertEqual(date2);
	}

	[TestMethod]
	public void Index_RemoveDate()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add dates
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);

		// Remove one date
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, true);

		// Check remaining date
		var dates = index.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(date2);
	}

	[TestMethod]
	public void Index_AvailableSecurities()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId1 = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var secId2 = new SecurityId { SecurityCode = "GAZP", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		index.ChangeDate(secId1, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId2, StorageFormats.Binary, DataType.Ticks, date, false);

		var securities = index.AvailableSecurities.ToArray();
		securities.Length.AssertEqual(2);
		securities.Contains(secId1).AssertTrue();
		securities.Contains(secId2).AssertTrue();
	}

	[TestMethod]
	public void Index_GetAvailableDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date, false);
		index.ChangeDate(secId, StorageFormats.Csv, DataType.Level1, date, false);

		// Get data types for Binary format
		var dataTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.MarketDepth).AssertTrue();

		// Get data types for Csv format
		var dataTypesCsv = index.GetAvailableDataTypes(secId, StorageFormats.Csv).ToArray();
		dataTypesCsv.Length.AssertEqual(1);
		dataTypesCsv[0].AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void Index_SaveAndLoad()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add data
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date1, false);

		// Save to stream
		using var stream = new MemoryStream();
		index.Save(stream);

		// Load from stream
		var loadedIndex = new LocalMarketDataDrive.Index();
		loadedIndex.Load(stream.ToArray());

		// Verify loaded data
		var dates = loadedIndex.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(2);
		dates[0].AssertEqual(date1);
		dates[1].AssertEqual(date2);

		var dataTypes = loadedIndex.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.MarketDepth).AssertTrue();
	}

	[TestMethod]
	public void Index_NeedSave()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		// Initially should not need save
		index.NeedSave(TimeSpan.Zero).AssertFalse();

		// Add data
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);

		// Should need save immediately
		index.NeedSave(TimeSpan.Zero).AssertTrue();

		// Should not need save with large delay
		index.NeedSave(TimeSpan.FromDays(1)).AssertFalse();
	}

	[TestMethod]
	public void Index_MultipleFormatsAndDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		// Add same date for different formats and data types
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Csv, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date, false);

		// Verify Binary format has both data types
		var binaryTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		binaryTypes.Length.AssertEqual(2);

		// Verify Csv format has only Ticks
		var csvTypes = index.GetAvailableDataTypes(secId, StorageFormats.Csv).ToArray();
		csvTypes.Length.AssertEqual(1);
		csvTypes[0].AssertEqual(DataType.Ticks);
	}

	[TestMethod]
	public void Index_CandleDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		var tf5min = TimeSpan.FromMinutes(5).TimeFrame();
		var tf1hour = TimeSpan.FromHours(1).TimeFrame();

		index.ChangeDate(secId, StorageFormats.Binary, tf5min, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, tf1hour, date, false);

		var dataTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Contains(tf5min).AssertTrue();
		dataTypes.Contains(tf1hour).AssertTrue();

		// Save and reload to test candle serialization
		using var stream = new MemoryStream();
		index.Save(stream);

		var loadedIndex = new LocalMarketDataDrive.Index();
		loadedIndex.Load(stream.ToArray());

		var loadedTypes = loadedIndex.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		loadedTypes.Length.AssertEqual(2);
	}

	private static LocalMarketDataDrive CreateDrive(string path = null)
	{
		var fs = Helper.MemorySystem;
		return new(fs, path ?? fs.GetSubTemp());
	}

	private async Task SetupTestDataAsync(LocalMarketDataDrive drive, SecurityId securityId, DataType dataType, StorageFormats format, DateTime[] dates)
	{
		var token = CancellationToken;
		var storageDrive = drive.GetStorageDrive(securityId, dataType, format);

		foreach (var date in dates)
		{
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			// Write minimal valid data
			stream.Position = 0;
			await storageDrive.SaveStreamAsync(date, stream, token);
		}
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_EmptyDrive_ReturnsEmpty()
	{
		var drive = CreateDrive();
		var token = CancellationToken;

		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		securities.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_WithSecurities_ReturnsSecurities()
	{
		var drive = CreateDrive();
		var security1 = new SecurityId { SecurityCode = "TEST1", BoardCode = BoardCodes.Test };
		var security2 = new SecurityId { SecurityCode = "TEST2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, security1, DataType.Ticks, StorageFormats.Binary, dates);
		await SetupTestDataAsync(drive, security2, DataType.Ticks, StorageFormats.Binary, dates);

		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		(securities.Length >= 2).AssertTrue();
		securities.Any(s => s.SecurityCode == "TEST1" && s.BoardCode == BoardCodes.Test).AssertTrue();
		securities.Any(s => s.SecurityCode == "TEST2" && s.BoardCode == BoardCodes.Test).AssertTrue();
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		var drive = CreateDrive();
		var security = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, security, DataType.Ticks, StorageFormats.Binary, dates);

		var cts = new CancellationTokenSource();
		var token = cts.Token;
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_EmptySecurity_ReturnsEmpty(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "EMPTY", BoardCode = BoardCodes.Test };
		var token = CancellationToken;

		var dataTypes = await drive.GetAvailableDataTypesAsync(securityId, format, token);

		dataTypes.AssertNotNull();
		dataTypes.Count().AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_WithData_ReturnsDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, securityId, DataType.Level1, format, dates);

		var dataTypes = (await drive.GetAvailableDataTypesAsync(securityId, format, token)).ToArray();

		(dataTypes.Length >= 2).AssertTrue();
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.Level1).AssertTrue();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_DefaultSecurityId_ReturnsAllDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var security1 = new SecurityId { SecurityCode = "TEST1", BoardCode = BoardCodes.Test };
		var security2 = new SecurityId { SecurityCode = "TEST2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, security1, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, security2, DataType.Level1, format, dates);
		await SetupTestDataAsync(drive, security2, DataType.MarketDepth, format, dates);

		var dataTypes = (await drive.GetAvailableDataTypesAsync(default, format, token)).ToArray();

		(dataTypes.Length >= 3).AssertTrue();
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.Level1).AssertTrue();
		dataTypes.Contains(DataType.MarketDepth).AssertTrue();
	}

	[TestMethod]
	public async Task VerifyAsync_ExistingPath_DoesNotThrow()
	{
		var drive = CreateDrive();

		await drive.VerifyAsync(CancellationToken);

		// Should not throw
		true.AssertTrue();
	}

	[TestMethod]
	public Task VerifyAsync_NonExistingPath_ThrowsInvalidOperationException()
	{
		var drive = new LocalMarketDataDrive(Helper.MemorySystem, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
		var token = CancellationToken;

		return ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await drive.VerifyAsync(token);
		});
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_EmptyDrive_ReturnsEmpty()
	{
		var drive = CreateDrive();
		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var securityProvider = new CollectionSecurityProvider([]);
		var token = CancellationToken;

		var securities = await drive.LookupSecuritiesAsync(criteria, securityProvider).ToArrayAsync(token);

		securities.Length.AssertEqual(0);
	}


	[TestMethod]
	public Task LookupSecuritiesAsync_NullCriteria_ThrowsArgumentNullException()
	{
		var drive = CreateDrive();
		var securityProvider = new CollectionSecurityProvider([]);
		var token = CancellationToken;

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			await drive.LookupSecuritiesAsync(null, securityProvider).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task LookupSecuritiesAsync_NullSecurityProvider_ThrowsArgumentNullException()
	{
		var drive = CreateDrive();
		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var token = CancellationToken;

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			await drive.LookupSecuritiesAsync(criteria, null).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, StorageFormats.Binary, dates);

		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var securityProvider = new CollectionSecurityProvider([]);

		var cts = new CancellationTokenSource();
		var token = cts.Token;
		cts.Cancel();

		await ThrowsExactlyAsync<OperationCanceledException>(async () =>
		{
			await drive.LookupSecuritiesAsync(criteria, securityProvider).ToArrayAsync(token);
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_MultipleDates_ReturnsDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[]
		{
			DateTime.UtcNow.Date.AddDays(-2),
			DateTime.UtcNow.Date.AddDays(-1),
			DateTime.UtcNow.Date
		};
		var token = CancellationToken;

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, format, dates);

		var dataTypes = (await drive.GetAvailableDataTypesAsync(securityId, format, token)).ToArray();

		(dataTypes.Length >= 1).AssertTrue();
		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_MultipleSecurities_ReturnsAllSecurities()
	{
		var drive = CreateDrive();
		var securities = new[]
		{
			new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			new SecurityId { SecurityCode = "MSFT", BoardCode = BoardCodes.Test },
			new SecurityId { SecurityCode = "GOOGL", BoardCode = BoardCodes.Test }
		};
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		foreach (var secId in securities)
		{
			await SetupTestDataAsync(drive, secId, DataType.Ticks, StorageFormats.Binary, dates);
		}

		var result = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		(result.Length >= 3).AssertTrue();
		foreach (var secId in securities)
		{
			result.Any(s => s.SecurityCode == secId.SecurityCode && s.BoardCode == secId.BoardCode).AssertTrue();
		}
	}

	[TestMethod]
	public Task VerifyAsync_PathWithNoAccess_ThrowsInvalidOperationException()
	{
		var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nested", "path");
		var drive = new LocalMarketDataDrive(Helper.MemorySystem, invalidPath);
		var token = CancellationToken;

		return ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await drive.VerifyAsync(token);
		});
	}
}
