namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// Candle storage: time frames, profiles, active candles, offsets and fractional volumes.
/// </summary>
partial class StorageTests
{
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
			minPrice = minPrice.Min(t.TradePrice.Value);
			maxPrice = maxPrice.Max(t.TradePrice.Value);
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
		// boxSize is already an absolute price delta (clamped to the (max - min) / _maxRenkoSteps
		// span above), so it must be passed through as absolute. Pips() would reinterpret it as a
		// count of price steps and multiply it by PriceStep, making the real brick 10x smaller
		// than what the clamp computed - and 10x more bricks to build and store.
		var renkoArg = new Unit(boxSize);
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
}
