namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Storages.Binary.Snapshot;

[TestClass]
[DoNotParallelize]
public class StorageTests
{
	private const int _tickCount = 5000;
	private const int _maxRenkoSteps = 100;
	private const int _depthCount1 = 10;
	private const int _depthCount2 = 1000;
	private const int _depthCount3 = 10000;
	private static readonly int[] _sourceArray = [01,02,03,06,07,08,09,10,13,14,15,16,17,20,21,22,23,24,27,28,29,30];

	private static IStorageRegistry GetStorageRegistry()
		=> Helper.GetStorage(Helper.GetSubTemp(Guid.NewGuid().ToString("N")));

	private static IMarketDataStorage<ExecutionMessage> GetTradeStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetTickMessageStorage(security, null, format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена сделки.")]
	public void TickNegativePrice(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, format).Save([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = -10,
			SecurityId = secId,
			TradeVolume = 10,
			ServerTime = DateTimeOffset.UtcNow
		}]);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickEmptySecurityBinary(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, format).Save([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			ServerTime = DateTimeOffset.UtcNow,
		}]);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickInvalidSecurity2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetTradeStorage(secId, format);
		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(
		[
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 10,
				TradeVolume = 10,
				ServerTime = DateTimeOffset.UtcNow,
				SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux }
			}
		]));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickInvalidSecurity3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetTradeStorage(secId, format);
		Assert.ThrowsExactly<ArgumentException>(() => storage.Save([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			SecurityId = secId,
		}]));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickRandom(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickStringId(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickRandomLocalTime(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickNanosec(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, interval: TimeSpan.FromTicks(16546));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickHighPrice(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickLowPrice(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickExtremePrice(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickExtremePrice2(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickExtremeVolume(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickExtremeVolume2(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
		{
			//trades.First().Security.VolumeStep = 0.0001m;

			foreach (var t in trades)
				t.TradeVolume = RandomGen.GetBool() ? decimal.MinValue : decimal.MaxValue;
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickNonSystem(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickFractionalVolume(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
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
	public void TickFractionalVolume2(StorageFormats format)
	{
		TickRandomSaveLoad(format, _tickCount, trades =>
		{
			var volumeStep = /*trades.First().Security.VolumeStep = */0.00001m;

			foreach (var trade in trades)
			{
				trade.TradeVolume *= (volumeStep * 0.1m);
			}
		});
	}

	private static void TickRandomSaveLoad(StorageFormats format, int count, Action<ExecutionMessage[]> modify = null, TimeSpan? interval = null)
	{
		var security = Helper.CreateStorageSecurity();
		var trades = security.RandomTicks(count, false, interval);
		var secId = security.ToSecurityId();

		modify?.Invoke(trades);

		var storage = GetTradeStorage(secId, format);
		storage.Save(trades);
		LoadTradesAndCompare(storage, trades);
		storage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickPartSave(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var trades = security.RandomTicks(_tickCount, false);

		const int halfTicks = _tickCount / 2;

		var tradeStorage = GetTradeStorage(secId, format);

		tradeStorage.Save(trades.Take(halfTicks));
		LoadTradesAndCompare(tradeStorage, [.. trades.Take(halfTicks)]);

		tradeStorage.Save([.. trades.Skip(halfTicks)]);
		LoadTradesAndCompare(tradeStorage, [.. trades.Skip(halfTicks)]);

		LoadTradesAndCompare(tradeStorage, trades);
		tradeStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickRandomDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		tradeStorage.Save(trades);

		var randomDeleteTrades = trades.Select(t => RandomGen.GetInt(5) == 2 ? null : t).WhereNotNull().ToList();
		tradeStorage.Delete(randomDeleteTrades);

		LoadTradesAndCompare(tradeStorage, [.. trades.Except(randomDeleteTrades)]);
		tradeStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickFullDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		tradeStorage.Save(trades);

		tradeStorage.Delete(trades.First().ServerTime, trades.Last().ServerTime);

		var loadedTrades = tradeStorage.Load(trades.First().ServerTime, trades.Last().ServerTime).ToArray();
		loadedTrades.Length.AssertEqual(0);

		tradeStorage.Save(trades);

		LoadTradesAndCompare(tradeStorage, trades);

		tradeStorage.Delete(trades);

		loadedTrades = [.. tradeStorage.Load(trades.First().ServerTime, trades.Last().ServerTime)];
		loadedTrades.Length.AssertEqual(0);

		loadedTrades = [.. tradeStorage.Load()];
		loadedTrades.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickWrongDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, format).Delete(new DateTime(2005, 1, 1), new DateTime(2005, 1, 10));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickRandomDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var trades = security.RandomTicks(_tickCount, false);

		var tradeStorage = GetTradeStorage(secId, format);

		tradeStorage.Save(trades);

		var minTime = DateTimeOffset.MaxValue;
		var maxTime = DateTimeOffset.MinValue;

		foreach (var t in trades)
		{
			minTime = t.ServerTime < minTime ? t.ServerTime : minTime;
			maxTime = t.ServerTime > maxTime ? t.ServerTime : maxTime;
		}

		var diff = maxTime - minTime;
		var third = TimeSpan.FromTicks(diff.Ticks / 3);

		var from = minTime + third;
		var to = maxTime - third;
		tradeStorage.Delete(from, to);

		LoadTradesAndCompare(tradeStorage, [.. trades.Where(t => t.ServerTime < from || t.ServerTime > to)]);

		tradeStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var dt = DateTimeOffset.UtcNow;

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

		tradeStorage.Save([trades[0]]);
		tradeStorage.Save([trades[1]]);

		LoadTradesAndCompare(tradeStorage, trades);
		tradeStorage.DeleteWithCheck();
	}

	private static void LoadTradesAndCompare(IMarketDataStorage<ExecutionMessage> tradeStorage, ExecutionMessage[] trades)
	{
		var loadedTrades = tradeStorage.Load(trades.First().ServerTime, trades.Last().ServerTime).ToArray();

		loadedTrades.CompareMessages(trades);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthAdaptivePriceStep(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		security.PriceStep = 0.0001m;

		var depths = security.RandomDepths(_depthCount2);

		security.PriceStep = 0.1m;

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format);

		storage.Save(depths);
		LoadDepthsAndCompare(storage, depths);

		storage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthLowPriceStep(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		security.PriceStep = 0.00000001m;

		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount2);

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format: format);

		storage.Save(depths);
		LoadDepthsAndCompare(storage, depths);

		storage.DeleteWithCheck();
	}

	private static IMarketDataStorage<QuoteChangeMessage> GetDepthStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetQuoteMessageStorage(security, null, format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthInvalidVolume(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTimeOffset.UtcNow,
			SecurityId = secId,
			Bids = [new(1, -1)],
		};

		var storage = GetDepthStorage(secId, format);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => storage.Save([depth]));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthInvalidSecurity(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux },
			Bids = [new(1, 1)],
		};

		var storage = GetDepthStorage(secId, format);
		Assert.ThrowsExactly<ArgumentException>(() => storage.Save([depth]));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		storage.Save([depth2]);
		storage.Save([depth1]);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth2]);
		LoadDepthsAndCompare(depthStorage, [depth2]);

		depthStorage.Save([depth1]);
		LoadDepthsAndCompare(depthStorage, [depth2]);
		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth2]);
		LoadDepthsAndCompare(depthStorage, [depth2]);

		try
		{
			depthStorage.Save([depth1]);
		}
		catch
		{
			depthStorage.DeleteWithCheck();
			throw;
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Все переданные стаканы является пустыми.")]
	public void DepthInvalidEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTimeOffset.UtcNow,
			SecurityId = secId,
		};

		var depths = new[] { depth };

		var storage = GetDepthStorage(secId, format);
		storage.Save(depths);
		LoadDepthsAndCompare(storage, depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Переданный стакан является пустым.")]
	public void DepthEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthNegativePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthZeroPrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthEmpty2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthEmpty3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0) };
		var depth2 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1) };
		var depth3 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2) };

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthPartSave(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount2);

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths.Take(500));
		LoadDepthsAndCompare(depthStorage, [.. depths.Take(500)]);

		depthStorage.Save([.. depths.Skip(500)]);
		LoadDepthsAndCompare(depthStorage, [.. depths.Skip(000)]);

		LoadDepthsAndCompare(depthStorage, depths);

		depthStorage.DeleteWithCheck();
	}

	private static void DepthHalfFilled(StorageFormats format, int count)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depthStorage = GetDepthStorage(secId, format);

		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();

		var secMsg = security.ToMessage();

		generator.Process(secMsg);
		generator.Process(security.Board.ToMessage());

		var time = DateTimeOffset.UtcNow;

		var depths = new List<QuoteChangeMessage>();

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

		depthStorage.Save(depths);

		LoadDepthsAndCompare(depthStorage, depths);

		var from = time;
		var to = from.AddDays(count + 1);

		depthStorage.Delete(from, to);

		var loadedDepths = depthStorage.Load(from, to).ToArray();
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthHalfFilled(StorageFormats format)
	{
		DepthHalfFilled(format, _depthCount1);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandom(StorageFormats format)
	{
		DepthRandom(format, _depthCount3);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomOrdersCount(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, ordersCount: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomConditions(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, conditions: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthExtremePrice(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, depths =>
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
	public void DepthExtremeVolume(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, depths =>
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
	public void DepthExtremeVolume2(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, depths =>
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
	public void DepthRandomNanosec(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, interval: TimeSpan.FromTicks(14465));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthFractionalVolume(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, depths =>
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
	public void DepthFractionalVolume2(StorageFormats format)
	{
		DepthRandom(format, _depthCount3, depths =>
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
	public void DepthSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var dt = DateTimeOffset.UtcNow;

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

		depthStorage.Save([depths[0]]);
		depthStorage.Save([depths[1]]);

		LoadDepthsAndCompare(depthStorage, depths);
		depthStorage.DeleteWithCheck();
	}

	private static void DepthRandom(StorageFormats format, int count, Action<QuoteChangeMessage[]> modify = null, TimeSpan? interval = null, bool ordersCount = false, bool conditions = false)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save(depths);
		LoadDepthsAndCompare(depthStorage, depths);
		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount3, TimeSpan.FromSeconds(2));

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths);

		var randomDeleteDepths = depths.Select(d => RandomGen.GetInt(5) == 2 ? null : d).WhereNotNull().ToList();
		depthStorage.Delete(randomDeleteDepths);

		LoadDepthsAndCompare(depthStorage, [.. depths.Except(randomDeleteDepths).OrderBy(d => d.ServerTime)]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthFullDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths);

		LoadDepthsAndCompare(depthStorage, depths);

		depthStorage.Delete(depths.First().ServerTime, depths.Last().ServerTime);

		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();
		loadedDepths.Length.AssertEqual(0);

		depthStorage.Save(depths);

		LoadDepthsAndCompare(depthStorage, depths);

		depthStorage.Delete(depths);

		loadedDepths = [.. depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime)];
		loadedDepths.Length.AssertEqual(0);

		loadedDepths = [.. depthStorage.Load()];
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths);

		var from = DateTime.Today + TimeSpan.FromMinutes(_depthCount3 / 2);
		var to = DateTime.Today + TimeSpan.FromMinutes(3 * _depthCount3 / 2);
		depthStorage.Delete(from, to);

		LoadDepthsAndCompare(depthStorage, [.. depths.Where(d => d.ServerTime < from || d.ServerTime > to).OrderBy(d => d.ServerTime)]);

		depthStorage.DeleteWithCheck();
	}

	private static void LoadDepthsAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomLessMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		const int depthSize = 20;

		var depths = security.RandomDepths(_depthCount3, new TrendMarketDepthGenerator(secId)
		{
			MaxBidsDepth = depthSize,
			MaxAsksDepth = depthSize,
		});

		//storage.MarketDepthMaxDepth = depthSize / 2;

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths);
		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();

		loadedDepths.CompareMessages(depths);

		depthStorage.Delete(depths.First().ServerTime, depths.Last().ServerTime);
		loadedDepths = [.. depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime)];
		loadedDepths.Length.AssertEqual(0);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomMoreMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount3);

		//storage.MarketDepthMaxDepth = 20;

		var depthStorage = GetDepthStorage(secId, format);

		depthStorage.Save(depths);
		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();

		loadedDepths.CompareMessages(depths);

		depthStorage.Delete(depths.First().ServerTime, depths.Last().ServerTime);
		loadedDepths = [.. depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime)];
		loadedDepths.Length.AssertEqual(0);

		depthStorage.DeleteWithCheck();
	}

	private static void DepthRandomIncrement(StorageFormats format, bool ordersCount, bool conditions)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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
		depthStorage.Save(diffQuotes);
		LoadQuotesAndCompare(depthStorage, diffQuotes);
		depthStorage.DeleteWithCheck();
	}

	private static void LoadQuotesAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrement(StorageFormats format)
	{
		DepthRandomIncrement(format, false, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrementOrders(StorageFormats format)
	{
		DepthRandomIncrement(format, true, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrementOrdersConditions(StorageFormats format)
	{
		DepthRandomIncrement(format, true, true);
	}

	private static void DepthRandomIncrementNonIncrement(StorageFormats format, bool isStateFirst)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? QuoteChangeStates.SnapshotComplete : null,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? null : QuoteChangeStates.SnapshotComplete,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		Assert.ThrowsExactly<InvalidOperationException>(() => depthStorage.Save(depths));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrementNonIncrement(StorageFormats format)
	{
		DepthRandomIncrementNonIncrement(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrementNonIncrement2(StorageFormats format)
	{
		DepthRandomIncrementNonIncrement(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthRandomIncrementNonIncrement3(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = QuoteChangeStates.SnapshotComplete,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = null,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save(depths.Take(1));
		Assert.ThrowsExactly<ArgumentException>(() => depthStorage.Save(depths.Skip(1)));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesExtremePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		var trades = security.RandomTicks(_tickCount, false);

		foreach (var trade in trades)
		{
			trade.TradePrice = RandomGen.GetDecimal();
			trade.TradeVolume = RandomGen.GetDecimal();
		}

		CandlesRandom(format, trades, security, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesNoProfile(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(format, security.RandomTicks(_tickCount, false), security, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public void CandlesWithProfile(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(format, security.RandomTicks(_tickCount, true), security, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesActive(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();

		var tf = TimeSpan.FromMinutes(5);
		var time = new DateTime(2017, 10, 02, 15, 30, 00).ApplyMoscow();

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

		candleStorage.Save(candles);

		var loadedCandles = candleStorage.Load(candles.First().OpenTime, candles.Last().OpenTime).ToArray();
		loadedCandles.CompareCandles([.. candles.Where(c => c.State != CandleStates.Active)], format);
		candleStorage.Delete(loadedCandles);

		foreach (var candle in candles)
		{
			candleStorage.Save([candle]);
		}

		loadedCandles = [.. candleStorage.Load(candles.First().OpenTime, candles.Last().OpenTime)];
		loadedCandles.CompareCandles([.. candles.Where(c => c.State != CandleStates.Active)], format);
		candleStorage.Delete(loadedCandles);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDuplicate(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();

		var tf = TimeSpan.FromMinutes(5);
		var time = new DateTime(2017, 10, 02, 15, 30, 00).ApplyMoscow();

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

		candleStorage.Save(candles);

		var loadedCandles = candleStorage.Load(candles.First().OpenTime, candles.Last().OpenTime).ToArray();
		loadedCandles.CompareCandles([.. candles.Take(1)], format);
		candleStorage.Delete(loadedCandles);

		foreach (var candle in candles)
		{
			candleStorage.Save([candle]);
		}

		loadedCandles = [.. candleStorage.Load(candles.First().OpenTime, candles.Last().OpenTime)];
		loadedCandles.CompareCandles([.. candles.Take(1)], format);
		candleStorage.Delete(loadedCandles);
	}

	private static ExecutionMessage[] GenerateFactalVolumeTrades(Security security, decimal modifier)
	{
		var secMsg = security.ToMessage();

		var trades = new List<ExecutionMessage>();

		var tradeGenerator = new RandomWalkTradeGenerator(secMsg.SecurityId);
		tradeGenerator.Init();

		tradeGenerator.Process(secMsg);
		tradeGenerator.Process(new Level1ChangeMessage
		{
			SecurityId = secMsg.SecurityId,
			ServerTime = DateTime.Today,
		}.TryAdd(Level1Fields.LastTradeTime, DateTime.Today));

		for (var i = 0; i < _tickCount; i++)
		{
			var msg = (ExecutionMessage)tradeGenerator.Process(new TimeMessage
			{
				ServerTime = DateTime.Today + TimeSpan.FromSeconds(i + 1)
			});

			msg.TradeVolume *= security.VolumeStep * modifier;

			trades.Add(msg);
		}

		return [.. trades];
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesFractionalVolume(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		CandlesRandom(format, GenerateFactalVolumeTrades(security, 1), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesFractionalVolume2(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		CandlesRandom(format, GenerateFactalVolumeTrades(security, 0.1m), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	private static void CandlesRandom(
		StorageFormats format,
		ExecutionMessage[] trades,
		Security security, bool isCalcVolumeProfile,
		bool resetPriceStep = false,
		decimal volumeRange = CandleTests.VolumeRange,
		decimal boxSize = CandleTests.BoxSize,
		bool diffOffset = false)
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

		CheckCandles<TimeFrameCandleMessage, TimeSpan>(storage, secId, candles, tfArg, format, diffOffset);
		CheckCandles<VolumeCandleMessage, decimal>(storage, secId, candles, volumeRange, format, diffOffset);
		CheckCandles<TickCandleMessage, int>(storage, secId, candles, ticksArg, format, diffOffset);
		CheckCandles<RangeCandleMessage, Unit>(storage, secId, candles, rangeArg, format, diffOffset);
		CheckCandles<RenkoCandleMessage, Unit>(storage, secId, candles, renkoArg, format, diffOffset);
		CheckCandles<PnFCandleMessage, PnFArg>(storage, secId, candles, pnfArg, format, diffOffset);
	}

	private static void CheckCandles<TCandle, TArg>(IStorageRegistry storage, SecurityId security, IEnumerable<CandleMessage> candles, TArg arg, StorageFormats format, bool diffOffset)
		where TCandle : CandleMessage
	{
		var candleStorage = storage.GetCandleMessageStorage(typeof(TCandle), security, arg, null, format);
		var typedCandle = candles.OfType<TCandle>().ToArray();

		if (diffOffset)
		{
			foreach (var candle in typedCandle)
			{
				TimeZoneInfo tz;

				switch (RandomGen.GetInt(4))
				{
					case 0:
						continue;

					case 1:
						tz = TimeHelper.Cst;
						break;

					case 2:
						tz = TimeHelper.Est;
						break;

					default:
						tz = TimeZoneInfo.Utc;
						break;
				}

				switch (RandomGen.GetInt(4))
				{
					case 0:
						break;

					case 1:
						candle.OpenTime = candle.OpenTime.Convert(tz);
						break;

					case 2:
						candle.CloseTime = candle.CloseTime.Convert(tz);
						break;

					case 3:
						candle.HighTime = candle.HighTime.Convert(tz);
						break;

					default:
						candle.LowTime = candle.LowTime.Convert(tz);
						break;
				}
			}
		}

		candleStorage.Save(typedCandle);
		var loadedCandles = candleStorage.Load(typedCandle.First().OpenTime, typedCandle.Last().OpenTime).ToArray();
		loadedCandles.CompareCandles([.. typedCandle.Where(c => c.State != CandleStates.Active)], format);
		candleStorage.Delete(loadedCandles);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesInvalid(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), null, format);

		var candles = new[] { new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId } };

		try
		{
			Assert.ThrowsExactly<ArgumentException>(() => tfStorage.Save(candles));
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesInvalid2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), null, format);

		var candles = new[]
		{
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(5), SecurityId = secId },
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId }
		};

		try
		{
			Assert.ThrowsExactly<ArgumentException>(() => tfStorage.Save(candles));
		}
		finally
		{
			tfStorage.Delete(candles);
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

		Assert.ThrowsExactly<ArgumentNullException>(() => storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(0), null, format));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var tf = TimeSpan.FromMinutes(5);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = DateTimeOffset.UtcNow;

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
			tfStorage.Save(candles);
			tfStorage.Save(candles);

			var loaded = tfStorage.Load().Cast<TimeFrameCandleMessage>().ToArray();
			loaded.CompareCandles(candles, format);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	private static void CandlesTimeFrame(StorageFormats format, TimeSpan tf, DateTimeOffset? time = null)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		time ??= DateTimeOffset.UtcNow;

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
			tfStorage.Save(candles);
			tfStorage.Load().ToArray().CompareCandles(candles, format);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesMiniTimeFrame(StorageFormats format)
	{
		CandlesTimeFrame(format, TimeSpan.FromMilliseconds(100));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesMiniTimeFrame2(StorageFormats format)
	{
		CandlesTimeFrame(format, TimeSpan.FromMinutes(1.0456));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesBigTimeFrame(StorageFormats format)
	{
		CandlesTimeFrame(format, TimeSpan.FromHours(100));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesBigTimeFrame2(StorageFormats format)
	{
		CandlesTimeFrame(format, TimeSpan.FromHours(100.4570456));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffDates(StorageFormats format)
	{
		CandlesTimeFrame(format, TimeSpan.FromHours(3), new DateTime(2019, 1, 1, 20, 00, 00).UtcKind());
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffDaysOffsets(StorageFormats format)
	{
		CandlesDiffDaysOffsets(format, false);
		CandlesDiffDaysOffsets(format, true);
	}

	private static void CandlesDiffDaysOffsets(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var tf = TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTimeOffset(2019, 05, 06, 17, 1, 1, TimeSpan.FromHours(3));

		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time.ConvertToUtc(),
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
			tfStorage.Save(candles);
			tfStorage.Load().ToArray().CompareCandles(candles, format);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffOffsets(StorageFormats format)
	{
		CandlesDiffOffsets(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffOffsetsIntraday(StorageFormats format)
	{
		CandlesDiffOffsets(format, true);
	}

	private static void CandlesDiffOffsets(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var tf = initHighLow ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTimeOffset(2019, 05, 06, 17, 1, 1, TimeSpan.FromHours(3));

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time.ConvertToUtc(),
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
				OpenTime = (time + tf).ConvertToUtc(),
				CloseTime = (time + tf + tf).ConvertToUtc(),
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = (time + tf + tf).ConvertToEst(),
				CloseTime = (time + tf + tf + tf).ConvertToEst(),
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
				tfStorage.Save([candle]);
			}

			tfStorage.Load().ToArray().CompareCandles(candles, format);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffOffsets2(StorageFormats format)
	{
		CandlesDiffOffsets2(format, true);
		CandlesDiffOffsets2(format, false);
	}

	private static void CandlesDiffOffsets2(StorageFormats format, bool initHighLow)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var tf = initHighLow ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);

		var storage = GetStorageRegistry();
		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, tf, format: format);

		var time = new DateTimeOffset(2019, 05, 06, 17, 1, 1, TimeSpan.FromHours(3));

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				OpenTime = time.Convert(TimeHelper.Gmt),
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
				OpenTime = (time + tf).ConvertToUtc(),
				CloseTime = (time + tf + tf).ConvertToMoscow(),
				OpenPrice = 10,
				HighPrice = 20,
				LowPrice = 9,
				ClosePrice = 9,
				TypedArg = tf,
				SecurityId = secId
			},

			new TimeFrameCandleMessage
			{
				OpenTime = (time + tf + tf).ConvertToEst(),
				CloseTime = (time + tf + tf + tf).Convert(TimeHelper.Cst),
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
				tfStorage.Save([candle]);
			}

			tfStorage.Load().ToArray().CompareCandles(candles, format);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesDiffOffsets3(StorageFormats format)
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(format, security.RandomTicks(_depthCount3, false), security, false, diffOffset: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void OrderLogRandom(StorageFormats format)
	{
		OrderLogRandomSaveLoad(format, _depthCount3);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void OrderLogFractionalVolume(StorageFormats format)
	{
		OrderLogRandomSaveLoad(format, _depthCount3, items =>
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
	public void OrderLogFractionalVolume2(StorageFormats format)
	{
		OrderLogRandomSaveLoad(format, _depthCount3, items =>
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
	public void OrderLogExtreme(StorageFormats format)
	{
		OrderLogRandomSaveLoad(format, _depthCount3, items =>
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
	public void OrderLogNonSystem(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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
		logStorage.Save(quotes);
		LoadOrderLogAndCompare(logStorage, quotes);
		logStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void OrderLogSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var dt = DateTimeOffset.UtcNow;

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
				PortfolioName = StockSharp.Messages.Extensions.AnonymousPortfolioName,
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
				PortfolioName = StockSharp.Messages.Extensions.AnonymousPortfolioName,
			},
		}.ToArray();

		olStorage.Save([ol[0]]);
		olStorage.Save([ol[1]]);

		LoadOrderLogAndCompare(olStorage, ol);
		olStorage.DeleteWithCheck();
	}

	private static void OrderLogRandomSaveLoad(StorageFormats format, int count, Action<IEnumerable<ExecutionMessage>> modify = null)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var items = security.RandomOrderLog(count);

		modify?.Invoke(items);

		var storage = GetStorageRegistry();

		var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
		logStorage.Save(items);
		LoadOrderLogAndCompare(logStorage, items);
		logStorage.DeleteWithCheck();
	}

	private static void LoadOrderLogAndCompare(IMarketDataStorage<ExecutionMessage> storage, IList<ExecutionMessage> items)
	{
		var loadedItems = storage.Load(items.First().ServerTime, items.Last().ServerTime).ToArray();
		loadedItems.CompareMessages(items);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void News(StorageFormats format)
	{
		var newsStorage = GetStorageRegistry().GetNewsMessageStorage(null, format);

		var news = Helper.RandomNews();

		newsStorage.Save(news);

		var loaded = newsStorage.Load(news.First().ServerTime, news.Last().ServerTime).ToArray();

		loaded.CompareMessages(news);

		newsStorage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void BoardState(StorageFormats format)
	{
		var storage = GetStorageRegistry().GetBoardStateMessageStorage(null, format);

		var data = Helper.RandomBoardStates();

		storage.Save(data);

		var loaded = storage.Load(data.First().ServerTime, data.Last().ServerTime).ToArray();

		loaded.CompareMessages(data);

		storage.DeleteWithCheck();
	}

	private static void Level1(StorageFormats format, bool isFractional, bool diffTimeZones = false, bool diffDays = false)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();

		var testValues = security.RandomLevel1(isFractional, diffTimeZones, diffDays, _depthCount3);

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		l1Storage.Save(testValues);
		var loaded = l1Storage.Load().ToArray();
		loaded.CompareMessages(testValues);

		var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
		loadedItems.CompareMessages(testValues);

		l1Storage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1(StorageFormats format)
	{
		Level1(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1Empty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();

		var testValues = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = DateTimeOffset.UtcNow,
			}
		};

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		l1Storage.Save(testValues);
		var loaded = l1Storage.Load();

		loaded.Count().AssertEqual(0);

		l1Storage.DeleteWithCheck();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1DiffOffset(StorageFormats format)
	{
		Level1(format, false, true);
	}

	[TestMethod]
	//[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1DiffDays(StorageFormats format)
	{
		Level1(format, false, true, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1Fractional(StorageFormats format)
	{
		Level1(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1MinMax(StorageFormats format)
	{
		var security = Helper.CreateSecurity();

		security.PriceStep = security.MinPrice = 0.0000001m;
		security.MaxPrice = 100000000m;

		var securityId = security.ToSecurityId();
		var serverTime = DateTimeOffset.UtcNow;

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

		l1Storage.Save(testValues);
		var loaded = l1Storage.Load().ToArray();
		loaded.CompareMessages(testValues);

		var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
		loadedItems.CompareMessages(testValues);

		l1Storage.DeleteWithCheck();
	}

	//[DataTestMethod]
	//[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	//public void Level1Duplicates(StorageFormats format)
	//{
	//	var security = Helper.CreateSecurity();

	//	var securityId = security.ToSecurityId();
	//	var serverTime = DateTimeOffset.UtcNow;

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

	//	l1Storage.Save(testValues);
	//	var loaded = l1Storage.Load().ToArray();

	//	testValues.RemoveAt(0);
	//	testValues[1].Changes.Remove(Level1Fields.LastTradePrice);

	//	var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
	//	loaded.CompareMessages(testValues);

	//	l1Storage.DeleteWithCheck();
	//}

	[TestMethod]
	public void Securities()
	{
		var exchangeProvider = ServicesRegistry.ExchangeInfoProvider;
		var securities = Helper.RandomSecurities().Select(s => s.ToSecurity(exchangeProvider)).ToArray();

		var registry = Helper.GetEntityRegistry();

		var storage = registry.Securities;

		foreach (var security in securities)
		{
			storage.Save(security, true);
		}

		storage = registry.Securities;
		var loaded = storage.LookupAll().ToArray();

		loaded.Length.AssertEqual(securities.Length);

		for (var i = 0; i < loaded.Length; i++)
		{
			Helper.CheckEqual(securities[i], loaded[i]);
		}

		storage.DeleteAll();
		storage.LookupAll().Count().AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Transaction(StorageFormats format)
	{
		var security = Helper.CreateSecurity();

		var secId = security.ToSecurityId();

		var transactions = security.RandomTransactions(1000);

		var storage = GetStorageRegistry().GetTransactionStorage(secId, null, format);

		storage.Save(transactions);
		var loaded = storage.Load().ToArray();

		loaded.CompareMessages(transactions);

		storage.Delete();
		storage.Load().Count().AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Position(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var testValues = security.RandomPositionChanges();

		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		storage.Save(testValues);
		var loaded = storage.Load().ToArray();

		testValues = [.. testValues.Where(t => t.HasChanges())];

		loaded.CompareMessages(testValues);

		storage.Delete();
		storage.Load().Count().AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void PositionEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var testValues = new[]
		{
			new PositionChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
			},
		};

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		storage.Save(testValues);
		var loaded = storage.Load().ToArray();

		loaded.Length.AssertEqual(0);

		storage.Delete();
		storage.Load().Count().AssertEqual(0);
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

		foreach (var secId in secIds)
		{
			var folderName = secId.SecurityIdToFolderName();
			folderName.FolderNameToSecurityId().AssertEqual(secId);

			var di = Directory.CreateDirectory(Path.Combine(Helper.GetSubTemp(namesFolder), folderName));
			di.Parent.Name.AssertEqual(namesFolder);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Bounds(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry().GetTickMessageStorage(secId, null, format);

		var now = DateTimeOffset.UtcNow;

		storage.Save(
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
		]);

		now = now.ToUniversalTime().Date;

		storage.Load(now).Count().AssertEqual(1);
		storage.Load(now, null).Count().AssertEqual(1);
		storage.Load(now.EndOfDay(), null).Count().AssertEqual(0);
		storage.Load(now.EndOfDay(), DateTime.Today).Count().AssertEqual(0);
		storage.Load(now.AddDays(10), null).Count().AssertEqual(0);

		storage.Delete(now);
		storage.Load(now).Count().AssertEqual(0);
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
			Check(new Level1BinarySnapshotSerializer(), Helper.RandomLevel1(security, secId, DateTimeOffset.UtcNow, RandomGen.GetBool(), RandomGen.GetBool(), RandomGen.GetBool(), () => 1m));
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

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public void RegressionBuildFromSmallerTimeframes(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var from = DateTimeOffset.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
		var to = DateTimeOffset.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);

		var expectedDates = _sourceArray.Select(d => new DateTime(2021, 12, d)).ToHashSet();
		var dates = buildableStorage.Dates.ToHashSet();

		expectedDates.SetEquals(dates).AssertTrue();

		var candles = buildableStorage.Load(from, to).ToArray();
		candles.Length.AssertEqual(expectedDates.Count);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public void RegressionBuildFromSmallerTimeframesCandleOrder(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes using original issue data, ensure candle updates are ordered in time and not doubled

		var secId = "SBER1@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var from = DateTimeOffset.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
		var to = DateTimeOffset.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);

		var candles = buildableStorage.Load(from, to);

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
	public void RegressionBuildableRange(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/SS-192

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var from = DateTimeOffset.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
		var to = DateTimeOffset.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);

		var range = buildableStorage.GetRange(from, to);

		range.Min.UtcDateTime.AssertEqual(range.Min.UtcDateTime.Date);
		range.Max.UtcDateTime.AssertEqual(range.Max.UtcDateTime.Date);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void TickZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTimeOffset.UtcNow;
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
			storage.Save([tick]);
			var loaded = storage.Load().ToArray();
			loaded.CompareMessages([tick]);
			storage.Delete([tick]);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void OrderLogZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTimeOffset.UtcNow;
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
			storage.Save([log]);
			var loaded = storage.Load(log.ServerTime, log.ServerTime).ToArray();
			loaded.CompareMessages([log]);
			storage.Delete([log]);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void Level1ZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTimeOffset.UtcNow;
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
			storage.Save([msg]);
			var loaded = storage.Load(msg.ServerTime, msg.ServerTime).ToArray();
			loaded.CompareMessages([msg]);
			storage.Delete([msg]);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void CandlesZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var now = DateTimeOffset.UtcNow;
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
			storage.Save([candle]);
			var loaded = storage.Load(candle.OpenTime, candle.OpenTime).ToArray();
			loaded.CompareCandles([candle], format);
			storage.Delete([candle]);
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public void DepthZeroValues(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var now = DateTimeOffset.UtcNow;
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
			storage.Save([depth]);
			var loaded = storage.Load(depth.ServerTime, depth.ServerTime).ToArray();
			loaded.CompareMessages([depth]);
			storage.Delete([depth]);
		}
	}
}
