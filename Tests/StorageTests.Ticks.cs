namespace StockSharp.Tests;

/// <summary>
/// Tick (trade) storage: round-trip, extreme values, partial save, delete and date bounds.
/// </summary>
partial class StorageTests
{
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
		var storage = GetTradeStorage(secId, format);

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = -10,
			SecurityId = secId,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow
		};

		await storage.SaveAsync([tick], token);
		await LoadTradesAndCompare(storage, [tick], format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TickEmptySecurity_RoundTrips(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;
		var storage = GetTradeStorage(secId, format);
		var serverTime = DateTime.UtcNow;

		await storage.SaveAsync([new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeId = 1,
			TradePrice = 10,
			TradeVolume = 10,
			ServerTime = serverTime,
		}], token);

		var loaded = await storage.LoadAsync(serverTime, serverTime).ToArrayAsync(token);
		loaded.Length.AssertEqual(1);
		loaded[0].SecurityId.AssertEqual(secId);
		loaded[0].TradeId.AssertEqual(1L);
		loaded[0].TradePrice.AssertEqual(10m);
		loaded[0].TradeVolume.AssertEqual(10m);
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
					trades[i].TradePrice = (trades[i].TradePrice.Value / i).Round(10);
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
		await LoadTradesAndCompare(storage, trades, format);
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
		await LoadTradesAndCompare(tradeStorage, [.. trades.Take(halfTicks)], format);

		await tradeStorage.SaveAsync([.. trades.Skip(halfTicks)], token);
		await LoadTradesAndCompare(tradeStorage, [.. trades.Skip(halfTicks)], format);

		await LoadTradesAndCompare(tradeStorage, trades, format);
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

		await LoadTradesAndCompare(tradeStorage, [.. trades.Except(randomDeleteTrades)], format);
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

		await LoadTradesAndCompare(tradeStorage, trades, format);

		await tradeStorage.DeleteAsync(trades, token);

		loadedTrades = await tradeStorage.LoadAsync(trades.First().ServerTime, trades.Last().ServerTime).ToArrayAsync(token);
		loadedTrades.Length.AssertEqual(0);

		loadedTrades = await tradeStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loadedTrades.Length.AssertEqual(0);
	}

	/// <summary>
	/// A delete names the records to remove. Records the caller never saved are not in the storage,
	/// so nothing is removed and the day they fall on comes back whole - a day dropped because the
	/// request happened to carry as many items as the day holds is silent, unrecoverable data loss.
	/// </summary>
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DeletingTicksThatWereNeverStoredLeavesTheDayWhole(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var date = new DateTime(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);

		ExecutionMessage CreateTick(DateTime serverTime, long tradeId) => new()
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = serverTime,
			TradeId = tradeId,
			TradePrice = 100 + tradeId,
			TradeVolume = 1,
		};

		var stored = new[]
		{
			CreateTick(date.AddMinutes(0), 1),
			CreateTick(date.AddMinutes(1), 2),
			CreateTick(date.AddMinutes(2), 3),
		};

		var tradeStorage = GetTradeStorage(secId, format);

		await tradeStorage.SaveAsync(stored, token);

		// The same day and the same number of records, but not one of them was ever saved.
		var foreign = new[]
		{
			CreateTick(date.AddHours(1), 11),
			CreateTick(date.AddHours(1).AddMinutes(1), 12),
			CreateTick(date.AddHours(1).AddMinutes(2), 13),
		};

		await tradeStorage.DeleteAsync(foreign, token);

		await LoadTradesAndCompare(tradeStorage, stored, format);
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

		await LoadTradesAndCompare(tradeStorage, [.. trades.Where(t => t.ServerTime < from || t.ServerTime > to)], format);

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

		await LoadTradesAndCompare(tradeStorage, trades, format);
		await tradeStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadTradesAndCompare(IMarketDataStorage<ExecutionMessage> tradeStorage, ExecutionMessage[] trades, StorageFormats format)
	{
		var token = CancellationToken;

		var loadedTrades = await tradeStorage.LoadAsync(trades.First().ServerTime, trades.Last().ServerTime).ToArrayAsync(token);

		loadedTrades.CompareMessages(trades, skipLocalTime: format == StorageFormats.Csv);
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

}
