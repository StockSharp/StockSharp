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

	private static IStorageRegistry GetStorageRegistry()
		=> Helper.GetStorage(Helper.GetSubTemp(Guid.NewGuid().ToString("N")));

	private static IMarketDataStorage<ExecutionMessage> GetTradeStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetTickMessageStorage(security, null, format);
	}

	private static void TickNegativePrice(StorageFormats format)
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
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена сделки.")]
	public void TickNegativePriceBinary()
	{
		TickNegativePrice(StorageFormats.Binary);
	}

	[TestMethod]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена сделки.")]
	public void TickNegativePriceCsv()
	{
		TickNegativePrice(StorageFormats.Csv);
	}

	// нулевые номер сделок имеют индексы
	//[TestMethod]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильный идентификатор сделки.")]
	//public void TickInvalidId()
	//{
	//    var security = Helper.CreateSecurity();
	//    Helper.GetDatabaseStorage().GetTradeStorage(security).Save(new[] { new Trade
	//    {
	//        Price = 10,
	//        Security = security,
	//        Volume = 10
	//    }});
	//}

	// http://stocksharp.com/forum/yaf_postsm6450_Oshibka-pri-importie-instrumientov-s-Finama.aspx#post6450
	//[TestMethod]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильный объем сделки.")]
	//public void TickInvalidVolume()
	//{
	//    var security = Helper.CreateSecurity();
	//    Helper.GetDatabaseStorage().GetTradeStorage(security).Save(new[] { new Trade
	//    {
	//        Id = 1,
	//        Price = 10,
	//        Security = security,
	//    }});
	//}

	[TestMethod]
	public void TickEmptySecurityBinary()
	{
		static void TickEmptySecurity(StorageFormats format)
		{
			var security = Helper.CreateSecurity();
			var secId = security.ToSecurityId();

			GetTradeStorage(secId, format).Save([ new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 10,
				TradeVolume = 10,
				ServerTime = DateTimeOffset.UtcNow,
			}]);
		}

		TickEmptySecurity(StorageFormats.Binary);
		TickEmptySecurity(StorageFormats.Csv);
	}

	private static void TickInvalidSecurity2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, format).Save(
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
		]);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент для Trade равен , а должен быть TestId.")]
	public void TickInvalidSecurity2Binary()
	{
		TickInvalidSecurity2(StorageFormats.Binary);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент для Trade равен , а должен быть TestId.")]
	public void TickInvalidSecurity2Csv()
	{
		TickInvalidSecurity2(StorageFormats.Csv);
	}

	private static void TickInvalidSecurity3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, format).Save([ new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			SecurityId = secId,
		}]);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент TestId2 имеет нулевой шаг цены.")]
	public void TickInvalidSecurity3Binary()
	{
		TickInvalidSecurity3(StorageFormats.Binary);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент TestId2 имеет нулевой шаг цены.")]
	public void TickInvalidSecurity3Csv()
	{
		TickInvalidSecurity3(StorageFormats.Csv);
	}

	[TestMethod]
	public void TickRandom()
	{
		TickRandomSaveLoad(_tickCount);
	}

	[TestMethod]
	public void TickStringId()
	{
		TickRandomSaveLoad(_tickCount, trades =>
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
	public void TickRandomLocalTime()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			foreach (var trade in trades)
			{
				trade.LocalTime = trade.ServerTime;
				trade.ServerTime = trade.ServerTime.AddYears(-1);
			}
		});
	}

	[TestMethod]
	public void TickNanosec()
	{
		TickRandomSaveLoad(_tickCount, interval: TimeSpan.FromTicks(16546));
	}

	[TestMethod]
	public void TickHighPrice()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = i * byte.MaxValue + 1;
			}
		});
	}

	[TestMethod]
	public void TickLowPrice()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			var priceStep = /*trades.First().Security.PriceStep = */0.00001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = (i + 1) * priceStep;
			}
		});
	}

	[TestMethod]
	public void TickExtremePrice()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			//trades.First().Security.PriceStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	public void TickExtremePrice2()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			//trades.First().Security.PriceStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradePrice = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	public void TickExtremeVolume()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			//trades.First().Security.VolumeStep = 0.0001m;

			for (var i = 0; i < trades.Length; i++)
			{
				trades[i].TradeVolume = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	public void TickExtremeVolume2()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			//trades.First().Security.VolumeStep = 0.0001m;

			foreach (var t in trades)
				t.TradeVolume = RandomGen.GetBool() ? decimal.MinValue : decimal.MaxValue;
		});
	}

	[TestMethod]
	public void TickNonSystem()
	{
		TickRandomSaveLoad(_tickCount, trades =>
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
	public void TickFractionalVolume()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			var volumeStep = /*trades.First().Security.VolumeStep = */0.00001m;

			foreach (var trade in trades)
			{
				trade.TradeVolume *= volumeStep;
			}
		});
	}

	[TestMethod]
	public void TickFractionalVolume2()
	{
		TickRandomSaveLoad(_tickCount, trades =>
		{
			var volumeStep = /*trades.First().Security.VolumeStep = */0.00001m;

			foreach (var trade in trades)
			{
				trade.TradeVolume *= (volumeStep * 0.1m);
			}
		});
	}

	//[TestMethod]
	//public void TickPerformance()
	//{
	//	var time = Watch.Do(() => TickRandomSaveLoad(1000000));
	//	(time < TimeSpan.FromMinutes(1)).AssertTrue();
	//}

	private static void TickRandomSaveLoad(int count, Action<ExecutionMessage[]> modify = null, TimeSpan? interval = null)
	{
		var security = Helper.CreateStorageSecurity();
		var trades = security.RandomTicks(count, false, interval);
		var secId = security.ToSecurityId();

		modify?.Invoke(trades);

		void SaveAndLoad(StorageFormats format)
		{
			var storage = GetTradeStorage(secId, format);
			storage.Save(trades);
			LoadTradesAndCompare(storage, trades);
			storage.DeleteWithCheck();
		}

		SaveAndLoad(StorageFormats.Binary);
		SaveAndLoad(StorageFormats.Csv);
	}

	private static void TickPartSave(StorageFormats format)
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
	public void TickPartSaveBinary()
	{
		TickPartSave(StorageFormats.Binary);
	}

	[TestMethod]
	public void TickPartSaveCsv()
	{
		TickPartSave(StorageFormats.Csv);
	}

	private static void TickRandomDelete(StorageFormats format)
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
	public void TickRandomDeleteBinary()
	{
		TickRandomDelete(StorageFormats.Binary);
	}

	[TestMethod]
	public void TickRandomDeleteCsv()
	{
		TickRandomDelete(StorageFormats.Csv);
	}

	private static void TickFullDelete(StorageFormats format)
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
	public void TickFullDelete()
	{
		TickFullDelete(StorageFormats.Binary);
		TickFullDelete(StorageFormats.Csv);
	}

	[TestMethod]
	public void TickWrongDateDelete()
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		GetTradeStorage(secId, StorageFormats.Binary).Delete(new DateTime(2005, 1, 1), new DateTime(2005, 1, 10));
		GetTradeStorage(secId, StorageFormats.Csv).Delete(new DateTime(2005, 1, 1), new DateTime(2005, 1, 10));
	}

	[TestMethod]
	public void TickRandomDateDelete()
	{
		static void delete(StorageFormats format)
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

		delete(StorageFormats.Binary);
		delete(StorageFormats.Csv);
	}

	private static void TickSameTime(StorageFormats format)
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

	[TestMethod]
	public void TickSameTime()
	{
		TickSameTime(StorageFormats.Binary);
		TickSameTime(StorageFormats.Csv);
	}

	private static void LoadTradesAndCompare(IMarketDataStorage<ExecutionMessage> tradeStorage, ExecutionMessage[] trades)
	{
		var loadedTrades = tradeStorage.Load(trades.First().ServerTime, trades.Last().ServerTime).ToArray();

		loadedTrades.CompareMessages(trades);
	}

	private static void DepthAdaptivePriceStep(StorageFormats format)
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
	public void DepthAdaptivePriceStepBinary()
	{
		DepthAdaptivePriceStep(StorageFormats.Binary);
	}

	[TestMethod]
	public void DepthAdaptivePriceStepCsv()
	{
		DepthAdaptivePriceStep(StorageFormats.Csv);
	}

	private static void DepthLowPriceStep(StorageFormats format)
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

	[TestMethod]
	public void DepthLowPriceStepBinary()
	{
		DepthLowPriceStep(StorageFormats.Binary);
	}

	[TestMethod]
	public void DepthLowPriceStepCsv()
	{
		DepthLowPriceStep(StorageFormats.Csv);
	}

	private static IMarketDataStorage<QuoteChangeMessage> GetDepthStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetQuoteMessageStorage(security, null, format);
	}

	//[TestMethod]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена котировки.")]
	//public void DepthInvalidPriceBin()
	//{
	//	var security = Helper.CreateSecurity();

	//	var depth = new MarketDepth(security);
	//	depth.AddQuote(new Quote
	//	{
	//		Volume = 1,
	//		Price = -1,
	//	});

	//	GetDepthStorage(security, StorageFormats.Binary).Save(new[] { depth });
	//}

	//[TestMethod]
	//[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильная цена котировки.")]
	//public void DepthInvalidPriceCsv()
	//{
	//	var security = Helper.CreateSecurity();

	//	var depth = new MarketDepth(security);
	//	depth.AddQuote(new Quote
	//	{
	//		Volume = 1,
	//		Price = -1,
	//	});

	//	GetDepthStorage(security, StorageFormats.Csv).Save(new[] { depth });
	//}

	[TestMethod]
	[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильный объем котировки.")]
	public void DepthInvalidVolumeBin()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTimeOffset.UtcNow,
			SecurityId = secId,
			Bids = [new QuoteChange(1, -1)],
		};

		GetDepthStorage(secId, StorageFormats.Binary).Save([depth]);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentOutOfRangeException), "Неправильный объем котировки.")]
	public void DepthInvalidVolumeCsv()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTimeOffset.UtcNow,
			SecurityId = secId,
			Bids = [new QuoteChange(1, -1)],
		};

		GetDepthStorage(secId, StorageFormats.Csv).Save([depth]);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент для MarketDepth равен , а должен быть TestId.")]
	public void DepthInvalidSecurityBin()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux },
			Bids = [new QuoteChange(1, 1)],
		};

		GetDepthStorage(secId, StorageFormats.Binary).Save([depth]);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Инструмент для MarketDepth равен , а должен быть TestId.")]
	public void DepthInvalidSecurityCsv()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth = new QuoteChangeMessage
		{
			SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux },
			Bids = [new QuoteChange(1, 1)],
		};

		GetDepthStorage(secId, StorageFormats.Csv).Save([depth]);
	}

	[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
		};

		var binStorage = GetDepthStorage(secId, StorageFormats.Binary);
		binStorage.AppendOnlyNew = false;
		binStorage.Save([depth2]);
		binStorage.Save([depth1]);

		var csvStorage = GetDepthStorage(secId, StorageFormats.Csv);
		csvStorage.AppendOnlyNew = false;
		csvStorage.Save([depth2]);
		csvStorage.Save([depth1]);
	}

	private static void DepthInvalidOrder2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save([depth2]);
		LoadDepthsAndCompare(depthStorage, [depth2]);

		depthStorage.Save([depth1]);
		LoadDepthsAndCompare(depthStorage, [depth2]);
		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder2()
	{
		DepthInvalidOrder2(StorageFormats.Binary);
		DepthInvalidOrder2(StorageFormats.Csv);
	}

	private static void DepthInvalidOrder3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
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
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public void DepthInvalidOrder3()
	{
		DepthInvalidOrder3(StorageFormats.Binary);
		DepthInvalidOrder3(StorageFormats.Csv);
	}

	[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Все переданные стаканы является пустыми.")]
	public void DepthInvalidEmpty()
	{
		static void Do(StorageFormats format)
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

		Do(StorageFormats.Binary);
		Do(StorageFormats.Csv);
	}

	private static void DepthEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new QuoteChange(1, 1)],
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
			Bids = [new QuoteChange(2, 2)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Переданный стакан является пустым.")]
	public void DepthEmpty()
	{
		DepthEmpty(StorageFormats.Binary);
		DepthEmpty(StorageFormats.Csv);
	}

	private static void DepthEmpty2(StorageFormats format)
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
			Bids = [new QuoteChange(1, 1)],
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
	public void DepthNegativePrices()
	{
		DepthNegativePrices(StorageFormats.Binary);
		DepthNegativePrices(StorageFormats.Csv);
	}

	private static void DepthNegativePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new QuoteChange(-10, 1)],
			Asks = [new QuoteChange(-0.1m, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Asks = [new QuoteChange(-0.1m, 1), new QuoteChange(1, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
			Bids = [new QuoteChange(-10, 1)],
			Asks = [new QuoteChange(-0.1m, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	public void DepthZeroPrices()
	{
		DepthZeroPrices(StorageFormats.Binary);
		DepthZeroPrices(StorageFormats.Csv);
	}

	private static void DepthZeroPrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var depth1 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			Bids = [new QuoteChange(-0.1m, 1)],
			Asks = [new QuoteChange(0, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			Asks = [new QuoteChange(0, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			Bids = [new QuoteChange(-10, 1)],
			Asks = [new QuoteChange(0, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save([depth1, depth2, depth3]);
		LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	public void DepthEmpty2()
	{
		DepthEmpty2(StorageFormats.Binary);
		DepthEmpty2(StorageFormats.Csv);
	}

	private static void DepthEmpty3(StorageFormats format)
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
	public void DepthEmpty3()
	{
		DepthEmpty3(StorageFormats.Binary);
		DepthEmpty3(StorageFormats.Csv);
	}

	private static void DepthPartSave(StorageFormats format)
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

	[TestMethod]
	public void DepthPartSave()
	{
		DepthPartSave(StorageFormats.Binary);
		DepthPartSave(StorageFormats.Csv);
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
	public void DepthHalfFilled()
	{
		DepthHalfFilled(StorageFormats.Binary, _depthCount1);
		DepthHalfFilled(StorageFormats.Csv, _depthCount1);
	}

	[TestMethod]
	public void DepthRandom()
	{
		DepthRandom(_depthCount3);
	}

	[TestMethod]
	public void DepthRandomOrdersCount()
	{
		DepthRandom(_depthCount3, ordersCount: true);
	}

	[TestMethod]
	public void DepthRandomConditions()
	{
		DepthRandom(_depthCount3, conditions: true);
	}

	[TestMethod]
	public void DepthExtremePrice()
	{
		DepthRandom(_depthCount3, depths =>
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
	public void DepthExtremeVolume()
	{
		DepthRandom(_depthCount3, depths =>
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
	public void DepthExtremeVolume2()
	{
		DepthRandom(_depthCount3, depths =>
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
	public void DepthRandomNanosec()
	{
		DepthRandom(_depthCount3, interval: TimeSpan.FromTicks(14465));
	}

	[TestMethod]
	public void DepthFractionalVolume()
	{
		DepthRandom(_depthCount3, depths =>
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
	public void DepthFractionalVolume2()
	{
		DepthRandom(_depthCount3, depths =>
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

	private static void DepthSameTime(StorageFormats format)
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
				Bids = [new QuoteChange(10, 1)],
			},
			new QuoteChangeMessage
			{
				ServerTime = dt,
				SecurityId = secId,
				Bids = [new QuoteChange(11, 1)],
			},
		};

		depthStorage.Save([depths[0]]);
		depthStorage.Save([depths[1]]);

		LoadDepthsAndCompare(depthStorage, depths);
		depthStorage.DeleteWithCheck();
	}

	[TestMethod]
	public void DepthSameTime()
	{
		DepthSameTime(StorageFormats.Binary);
		DepthSameTime(StorageFormats.Csv);
	}

	//[TestMethod]
	//public void DepthPerformance()
	//{
	//	var time = Watch.Do(() => DepthRandom(1000000));
	//	(time < TimeSpan.FromMinutes(1)).AssertTrue();
	//}

	private static void DepthRandom(int count, Action<List<QuoteChangeMessage>> modify = null, TimeSpan? interval = null, bool ordersCount = false, bool conditions = false)
	{
		foreach (var format in Enumerator.GetValues<StorageFormats>())
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
	}

	private static void DepthRandomDelete(StorageFormats format)
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
	public void DepthRandomDelete()
	{
		DepthRandomDelete(StorageFormats.Binary);
		DepthRandomDelete(StorageFormats.Csv);
	}

	private static void DepthFullDelete(StorageFormats format)
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
	public void DepthFullDelete()
	{
		DepthFullDelete(StorageFormats.Binary);
		DepthFullDelete(StorageFormats.Csv);
	}

	private static void DepthRandomDateDelete(StorageFormats format)
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

	[TestMethod]
	public void DepthRandomDateDelete()
	{
		DepthRandomDateDelete(StorageFormats.Binary);
		DepthRandomDateDelete(StorageFormats.Csv);
	}

	private static void LoadDepthsAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var loadedDepths = depthStorage.Load(depths.First().ServerTime, depths.Last().ServerTime).ToArray();

		loadedDepths.CompareMessages(depths);
	}

	private static void DepthRandomLessMaxDepth(StorageFormats format)
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
	public void DepthRandomLessMaxDepth()
	{
		DepthRandomLessMaxDepth(StorageFormats.Binary);
		DepthRandomLessMaxDepth(StorageFormats.Csv);
	}

	private static void DepthRandomMoreMaxDepth(StorageFormats format)
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

	[TestMethod]
	public void DepthRandomMoreMaxDepth()
	{
		DepthRandomMoreMaxDepth(StorageFormats.Binary);
		DepthRandomMoreMaxDepth(StorageFormats.Csv);
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

		for (var i = depths.Count - 1; i > 0; i--)
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
	public void DepthRandomIncrement()
	{
		DepthRandomIncrement(StorageFormats.Binary, false, false);
		DepthRandomIncrement(StorageFormats.Csv, false, false);
	}

	[TestMethod]
	public void DepthRandomIncrementOrders()
	{
		DepthRandomIncrement(StorageFormats.Binary, true, false);
		DepthRandomIncrement(StorageFormats.Csv, true, false);
	}

	[TestMethod]
	public void DepthRandomIncrementOrdersConditions()
	{
		DepthRandomIncrement(StorageFormats.Binary, true, true);
		DepthRandomIncrement(StorageFormats.Csv, true, true);
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
				Bids = [new QuoteChange(101, 1)],
				Asks = [new QuoteChange(102, 2)],
				State = isStateFirst ? QuoteChangeStates.SnapshotComplete : null,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new QuoteChange(101, 1)],
				Asks = [new QuoteChange(102, 2)],
				State = isStateFirst ? null : QuoteChangeStates.SnapshotComplete,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save(depths);
		LoadQuotesAndCompare(depthStorage, depths);
		depthStorage.DeleteWithCheck();
	}

	private static void DepthRandomIncrementNonIncrement3(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new QuoteChange(101, 1)],
				Asks = [new QuoteChange(102, 2)],
				State = QuoteChangeStates.SnapshotComplete,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTimeOffset.UtcNow,
				Bids = [new QuoteChange(101, 1)],
				Asks = [new QuoteChange(102, 2)],
				State = null,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.Save(depths.Take(1));
		depthStorage.Save(depths.Skip(1));
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DepthRandomIncrementNonIncrementBinary()
	{
		DepthRandomIncrementNonIncrement(StorageFormats.Binary, true);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DepthRandomIncrementNonIncrementCsv()
	{
		DepthRandomIncrementNonIncrement(StorageFormats.Csv, true);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DepthRandomIncrementNonIncrementBinary2()
	{
		DepthRandomIncrementNonIncrement(StorageFormats.Binary, false);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DepthRandomIncrementNonIncrementCsv2()
	{
		DepthRandomIncrementNonIncrement(StorageFormats.Csv, false);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void DepthRandomIncrementNonIncrementBinary3()
	{
		DepthRandomIncrementNonIncrement3(StorageFormats.Binary);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void DepthRandomIncrementNonIncrementCsv3()
	{
		DepthRandomIncrementNonIncrement3(StorageFormats.Csv);
	}

	[TestMethod]
	public void CandlesExtremePrices()
	{
		var security = Helper.CreateSecurity(100);

		var trades = security.RandomTicks(_tickCount, false);

		foreach (var trade in trades)
		{
			trade.TradePrice = RandomGen.GetDecimal();
			trade.TradeVolume = RandomGen.GetDecimal();
		}

		CandlesRandom(trades, security, false);
	}

	[TestMethod]
	public void CandlesNoProfile()
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(security.RandomTicks(_tickCount, false), security, false);
	}

	[TestMethod]
	public void CandlesWithProfile()
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(security.RandomTicks(_tickCount, true), security, true);
	}

	[TestMethod]
	public void CandlesActive()
	{
		CandlesActive(StorageFormats.Binary);
		CandlesActive(StorageFormats.Csv);
	}

	private static void CandlesActive(StorageFormats format)
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
	public void CandlesDuplicate()
	{
		CandlesDuplicate(StorageFormats.Binary);
		CandlesDuplicate(StorageFormats.Csv);
	}

	private static void CandlesDuplicate(StorageFormats format)
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
	public void CandlesFractionalVolume()
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		CandlesRandom(GenerateFactalVolumeTrades(security, 1), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	[TestMethod]
	public void CandlesFractionalVolume2()
	{
		var security = Helper.CreateSecurity(100);
		security.VolumeStep = 0.00001m;

		CandlesRandom(GenerateFactalVolumeTrades(security, 0.1m), security, false, volumeRange: 0.0003m, boxSize: 0.0003m);
	}

	private static void CandlesRandom(ExecutionMessage[] trades,
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

		var formats = isCalcVolumeProfile ? [StorageFormats.Binary] : Enumerator.GetValues<StorageFormats>().ToArray();

		if (resetPriceStep)
			security.PriceStep = 1;

		var secId = security.ToSecurityId();

		CheckCandles<TimeFrameCandleMessage, TimeSpan>(storage, secId, candles, tfArg, formats, diffOffset);
		CheckCandles<VolumeCandleMessage, decimal>(storage, secId, candles, volumeRange, formats, diffOffset);
		CheckCandles<TickCandleMessage, int>(storage, secId, candles, ticksArg, formats, diffOffset);
		CheckCandles<RangeCandleMessage, Unit>(storage, secId, candles, rangeArg, formats, diffOffset);
		CheckCandles<RenkoCandleMessage, Unit>(storage, secId, candles, renkoArg, formats, diffOffset);
		CheckCandles<PnFCandleMessage, PnFArg>(storage, secId, candles, pnfArg, formats, diffOffset);
	}

	private static void CheckCandles<TCandle, TArg>(IStorageRegistry storage, SecurityId security, IEnumerable<CandleMessage> candles, TArg arg, IEnumerable<StorageFormats> formats, bool diffOffset)
		where TCandle : CandleMessage
	{
		foreach (var format in formats)
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
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Неправильный параметр свечи.")]
	public void CandlesInvalid()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5));

		var candles = new[] { new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId } };

		try
		{
			tfStorage.Save(candles);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException), "Неправильный параметр свечи.")]
	public void CandlesInvalid2()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5));

		var candles = new[]
		{
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(5), SecurityId = secId },
			new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(1), SecurityId = secId }
		};

		try
		{
			tfStorage.Save(candles);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	//[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Неправильный параметр свечи.")]
	//public void CandlesInvalid3()
	//{
	//	var security = Helper.CreateSecurity();

	//	var storage = GetStorageRegistry();

	//	var tfStorage = storage.GetCandleStorage<TimeFrameCandle, TimeSpan>(security, TimeSpan.FromMinutes(5));

	//	var candles = new[]
	//	{
	//		new TimeFrameCandle
	//		{
	//			TimeFrame = TimeSpan.FromMinutes(5),
	//			Security = security,
	//			OpenTime = new DateTime(2005, 01, 01, 00, 01, 00),
	//			CloseTime = new DateTime(2005, 01, 01, 00, 06, 00)
	//		},
	//		new TimeFrameCandle
	//		{
	//			TimeFrame = TimeSpan.FromMinutes(5),
	//			Security = security,
	//			OpenTime = new DateTime(2005, 01, 01, 00, 02, 00),
	//			CloseTime = new DateTime(2005, 01, 01, 00, 07, 00)
	//		}
	//	};

	//	try
	//	{
	//		tfStorage.Save(candles);
	//	}
	//	finally
	//	{
	//		tfStorage.Delete(candles);
	//	}
	//}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException), "Неправильный параметр свечи.")]
	public void CandlesInvalid4()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry();

		var tfStorage = storage.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(0));

		var candles = new[] { new TimeFrameCandleMessage { TypedArg = TimeSpan.FromMinutes(0), OpenTime = DateTimeOffset.UtcNow, SecurityId = secId } };

		try
		{
			tfStorage.Save(candles);
		}
		finally
		{
			tfStorage.Delete(candles);
		}
	}

	private static void CandlesSameTime(StorageFormats format)
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

	[TestMethod]
	public void CandlesSameTime()
	{
		CandlesSameTime(StorageFormats.Binary);
		CandlesSameTime(StorageFormats.Csv);
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
	public void CandlesMiniTimeFrame()
	{
		CandlesTimeFrame(StorageFormats.Binary, TimeSpan.FromMilliseconds(100));
		CandlesTimeFrame(StorageFormats.Csv, TimeSpan.FromMilliseconds(100));
	}

	[TestMethod]
	public void CandlesMiniTimeFrame2()
	{
		CandlesTimeFrame(StorageFormats.Binary, TimeSpan.FromMinutes(1.0456));
		CandlesTimeFrame(StorageFormats.Csv, TimeSpan.FromMinutes(1.0456));
	}

	[TestMethod]
	public void CandlesBigTimeFrame()
	{
		CandlesTimeFrame(StorageFormats.Binary, TimeSpan.FromHours(100));
		CandlesTimeFrame(StorageFormats.Csv, TimeSpan.FromHours(100));
	}

	[TestMethod]
	public void CandlesBigTimeFrame2()
	{
		CandlesTimeFrame(StorageFormats.Binary, TimeSpan.FromHours(100.4570456));
		CandlesTimeFrame(StorageFormats.Csv, TimeSpan.FromHours(100.4570456));
	}

	[TestMethod]
	public void CandlesDiffDates()
	{
		CandlesTimeFrame(StorageFormats.Binary, TimeSpan.FromHours(3), new DateTime(2019, 1, 1, 20, 00, 00).UtcKind());
		CandlesTimeFrame(StorageFormats.Csv, TimeSpan.FromHours(3), new DateTime(2019, 1, 1, 20, 00, 00).UtcKind());
	}

	[TestMethod]
	public void CandlesDiffDaysOffsets()
	{
		CandlesDiffDaysOffsets(StorageFormats.Binary, false);
		CandlesDiffDaysOffsets(StorageFormats.Csv, false);

		CandlesDiffDaysOffsets(StorageFormats.Binary, true);
		CandlesDiffDaysOffsets(StorageFormats.Csv, true);
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
	public void CandlesDiffOffsets()
	{
		CandlesDiffOffsets(StorageFormats.Binary, false);
		CandlesDiffOffsets(StorageFormats.Csv, false);
	}

	[TestMethod]
	public void CandlesDiffOffsetsIntraday()
	{
		CandlesDiffOffsets(StorageFormats.Binary, true);
		CandlesDiffOffsets(StorageFormats.Csv, true);
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
	public void CandlesDiffOffsets2()
	{
		CandlesDiffOffsets2(StorageFormats.Binary, true);
		CandlesDiffOffsets2(StorageFormats.Csv, true);

		CandlesDiffOffsets2(StorageFormats.Binary, false);
		CandlesDiffOffsets2(StorageFormats.Csv, false);
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
	public void CandlesDiffOffsets3()
	{
		var security = Helper.CreateSecurity(100);

		CandlesRandom(security.RandomTicks(_depthCount3, false), security, false, diffOffset: true);
	}

	[TestMethod]
	public void OrderLogRandom()
	{
		OrderLogRandomSaveLoad(_depthCount3);
	}

	[TestMethod]
	public void OrderLogFractionalVolume()
	{
		OrderLogRandomSaveLoad(_depthCount3, items =>
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
	public void OrderLogFractionalVolume2()
	{
		OrderLogRandomSaveLoad(_depthCount3, items =>
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
	public void OrderLogExtreme()
	{
		OrderLogRandomSaveLoad(_depthCount3, items =>
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

	private static void OrderLogNonSystem(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var quotes = security.RandomOrderLog(_depthCount3);

		for (var i = 0; i < quotes.Count; i++)
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
	public void OrderLogNonSystem()
	{
		OrderLogNonSystem(StorageFormats.Binary);
		OrderLogNonSystem(StorageFormats.Csv);
	}

	private static void OrderLogSameTime(StorageFormats format)
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

	[TestMethod]
	public void OrderLogSameTime()
	{
		OrderLogSameTime(StorageFormats.Binary);
		OrderLogSameTime(StorageFormats.Csv);
	}

	private static void OrderLogRandomSaveLoad(int count, Action<IEnumerable<ExecutionMessage>> modify = null)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var items = security.RandomOrderLog(count);

		modify?.Invoke(items);

		foreach (var format in Enumerator.GetValues<StorageFormats>())
		{
			var storage = GetStorageRegistry();

			var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
			logStorage.Save(items);
			LoadOrderLogAndCompare(logStorage, items);
			logStorage.DeleteWithCheck();
		}
	}

	private static void LoadOrderLogAndCompare(IMarketDataStorage<ExecutionMessage> storage, IList<ExecutionMessage> items)
	{
		var loadedItems = storage.Load(items.First().ServerTime, items.Last().ServerTime).ToArray();
		loadedItems.CompareMessages(items);
	}

	private static void News(StorageFormats format)
	{
		var newsStorage = GetStorageRegistry().GetNewsMessageStorage(null, format);

		var news = Helper.RandomNews();

		newsStorage.Save(news);

		var loaded = newsStorage.Load(news.First().ServerTime, news.Last().ServerTime).ToArray();

		loaded.CompareMessages(news);

		newsStorage.DeleteWithCheck();
	}

	[TestMethod]
	public void News()
	{
		News(StorageFormats.Binary);
		News(StorageFormats.Csv);
	}

	private static void BoardState(StorageFormats format)
	{
		var storage = GetStorageRegistry().GetBoardStateMessageStorage(null, format);

		var data = new[]
		{
			new BoardStateMessage
			{
				State = SessionStates.Active,
				BoardCode = ExchangeBoard.Forts.Code,
				ServerTime = DateTimeOffset.UtcNow,
			},

			new BoardStateMessage
			{
				State = SessionStates.Paused,
				ServerTime = DateTimeOffset.UtcNow,
			},
		};

		storage.Save(data);

		var loaded = storage.Load(data.First().ServerTime, data.Last().ServerTime).ToArray();

		loaded.CompareMessages(data);

		storage.DeleteWithCheck();
	}

	[TestMethod]
	public void BoardState()
	{
		BoardState(StorageFormats.Binary);
		BoardState(StorageFormats.Csv);
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
	public void Level1Binary()
	{
		Level1(StorageFormats.Binary, false);
	}

	[TestMethod]
	public void Level1Csv()
	{
		Level1(StorageFormats.Csv, false);
	}

	private static void Level1Empty(StorageFormats format)
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
	public void Level1EmptyBinary()
	{
		Level1Empty(StorageFormats.Binary);
	}

	[TestMethod]
	public void Level1EmptyCsv()
	{
		Level1Empty(StorageFormats.Csv);
	}

	[TestMethod]
	public void Level1BinaryDiffOffset()
	{
		Level1(StorageFormats.Binary, false, true);
	}

	[TestMethod]
	public void Level1CsvDiffOffset()
	{
		Level1(StorageFormats.Csv, false, true);
	}

	// [TestMethod]
	// public void Level1BinaryDiffDays()
	// {
	// 	Level1(StorageFormats.Binary, false, true, true);
	// }

	[TestMethod]
	public void Level1CsvDiffDays()
	{
		Level1(StorageFormats.Csv, false, true, true);
	}

	[TestMethod]
	public void Level1BinaryFractional()
	{
		Level1(StorageFormats.Binary, true);
	}

	[TestMethod]
	public void Level1CsvFractional()
	{
		Level1(StorageFormats.Csv, true);
	}

	private static void Level1MinMax(StorageFormats format)
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

	[TestMethod]
	public void Level1BinaryMinMax()
	{
		Level1MinMax(StorageFormats.Binary);
	}

	[TestMethod]
	public void Level1CsvMinMax()
	{
		Level1MinMax(StorageFormats.Csv);
	}

	//private void Level1Duplicates(StorageFormats format)
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
	//	}.TryAdd(Level1Fields.LastTradePrice, 1000m)
	//	 .TryAdd(Level1Fields.BestBidPrice, 999m));

	//	var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

	//	l1Storage.Save(testValues);
	//	var loaded = l1Storage.Load().ToArray();

	//	testValues.RemoveAt(0);
	//	testValues[1].Changes.Remove(Level1Fields.LastTradePrice);

	//	var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
	//	loaded.CompareMessages(testValues, format);

	//	l1Storage.DeleteWithCheck();
	//}

	//[TestMethod]
	//public void Level1Duplicates()
	//{
	//	Level1Duplicates(StorageFormats.Csv);
	//	Level1Duplicates(StorageFormats.Binary);
	//}

	[TestMethod]
	public void Securities()
	{
		var securities = new List<Security>();

		for (var i = 0; i < 10000; i++)
		{
			var s = new Security
			{
				Code = "TestSecurity" + Guid.NewGuid().GetFileNameWithoutExtension(null),
				Name = "TestName",
				PriceStep = RandomGen.GetBool() ? (decimal)RandomGen.GetInt(1, 100) / RandomGen.GetInt(1, 100) : null,
				Volume = RandomGen.GetBool() ? (decimal)RandomGen.GetInt(1, 100) / RandomGen.GetInt(1, 100) : null,
				Decimals = RandomGen.GetBool() ? RandomGen.GetInt(1, 100) : null,
				Multiplier = RandomGen.GetBool() ? (decimal)RandomGen.GetInt(1, 100) / RandomGen.GetInt(1, 100) : null,
				Type = RandomGen.GetBool() ? RandomGen.GetEnum<SecurityTypes>() : null,
				Currency = RandomGen.GetBool() ? RandomGen.GetEnum<CurrencyTypes>() : null,
				Board = ExchangeBoard.Test
			};

			s.Id = s.Code + "@Test";

			if (s.Type == SecurityTypes.Option)
			{
				s.OptionType = RandomGen.GetEnum<OptionTypes>();
				s.Strike = (decimal)RandomGen.GetInt(1, 100) / RandomGen.GetInt(1, 100);
			}

			securities.Add(s);
		}

		var registry = Helper.GetEntityRegistry();

		var storage = registry.Securities;

		foreach (var security in securities)
		{
			storage.Save(security, true);
		}

		storage = registry.Securities;
		var loaded = storage.LookupAll().ToArray();

		loaded.Length.AssertEqual(securities.Count);

		for (var i = 0; i < loaded.Length; i++)
		{
			Helper.CheckEqual(securities[i], loaded[i]);
		}

		storage.DeleteAll();
		storage.LookupAll().Count().AssertEqual(0);
	}

	private static void TransactionTest(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var transactions = new List<ExecutionMessage>();

		var secId = security.ToSecurityId();

		for (var i = 0; i < 10000; i++)
		{
			transactions.Add(Helper.RandomTransaction(secId, i));
		}

		var storage = GetStorageRegistry().GetTransactionStorage(secId, null, format);

		storage.Save(transactions);
		var loaded = storage.Load().ToArray();

		loaded.CompareMessages(transactions);

		storage.Delete();
		storage.Load().Count().AssertEqual(0);
	}

	[TestMethod]
	public void TransactionBinary()
	{
		TransactionTest(StorageFormats.Binary);
	}

	[TestMethod]
	public void TransactionCsv()
	{
		TransactionTest(StorageFormats.Csv);
	}

	private static void PositionTest(StorageFormats format)
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
	public void PositionBinary()
	{
		PositionTest(StorageFormats.Binary);
	}

	[TestMethod]
	public void PositionCsv()
	{
		PositionTest(StorageFormats.Csv);
	}

	private static void PositionEmptyTest(StorageFormats format)
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
	public void PositionEmptyBinary()
	{
		PositionEmptyTest(StorageFormats.Binary);
	}

	[TestMethod]
	public void PositionEmptyCsv()
	{
		PositionEmptyTest(StorageFormats.Csv);
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
	public void Bounds()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry().GetTickMessageStorage(secId, null, StorageFormats.Binary);

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
	public void DefaultCredentialsProvider()
	{
		ICredentialsProvider prov1 = new DefaultCredentialsProvider();
		ServerCredentials orig = null;

		try
		{
			prov1.TryLoad(out orig).AssertTrue();

			var c = new ServerCredentials
			{
				Email = "email",
				Password = "pwd".Secure(),
				Token = "token".Secure()
			};

			prov1.Save(c.Clone(), true);

			prov1.TryLoad(out var c2).AssertTrue();

			Helper.CheckEqual(c.Save(), c2.Save());

			ICredentialsProvider prov2 = new DefaultCredentialsProvider();

			prov2.TryLoad(out var c3).AssertTrue();

			Helper.CheckEqual(c.Save(), c3.Save());
		}
		finally
		{
			prov1.Save(orig, true);
		}
	}

	[TestMethod]
	public void RegressionBuildFromSmallerTimeframes()
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf);

		var from = DateTimeOffset.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
		var to = DateTimeOffset.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);

		var expectedDates = new[] { 01,02,03,06,07,08,09,10,13,14,15,16,17,20,21,22,23,24,27,28,29,30 }.Select(d => new DateTime(2021, 12, d)).ToHashSet();
		var dates = buildableStorage.Dates.ToHashSet();

		expectedDates.SetEquals(dates).AssertTrue();

		var candles = buildableStorage.Load(from, to).ToArray();
		candles.Length.AssertEqual(expectedDates.Count);
	}

	[TestMethod]
	public void RegressionBuildFromSmallerTimeframesCandleOrder()
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes using original issue data, ensure candle updates are ordered in time and not doubled

		var secId = "SBER1@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf);

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
	public void RegressionBuildableRange()
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/SS-192

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf);

		var from = DateTimeOffset.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
		var to = DateTimeOffset.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);

		var range = buildableStorage.GetRange(from, to);

		range.Min.UtcDateTime.AssertEqual(range.Min.UtcDateTime.Date);
		range.Max.UtcDateTime.AssertEqual(range.Max.UtcDateTime.Date);
	}

	private static IMessageAdapter CreateAdapter<T>(Action<T> init = null, INativeIdStorage nativeIdStorage = null)
		where T : IMessageAdapter
	{
		var adapter = typeof(T).CreateAdapter(new IncrementalIdGenerator());

		init?.Invoke((T)adapter);

		if (adapter.IsNativeIdentifiers)
			adapter = new SecurityNativeIdMessageAdapter(adapter, nativeIdStorage ?? Helper.CreateNativeIdStorage());

		LogManager.Instance.Sources.Add(adapter);

		return adapter;
	}
}
