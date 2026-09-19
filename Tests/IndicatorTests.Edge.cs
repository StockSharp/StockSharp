namespace StockSharp.Tests;

/// <summary>
/// Edge cases: length bounds, degenerate series and flat input.
/// </summary>
partial class IndicatorTests
{
	[TestMethod]
	public void IndicatorLengthBoundsAreEnforced()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => new FractalAdaptiveMovingAverage { Length = 3 });
		ThrowsExactly<ArgumentOutOfRangeException>(() => new MassIndex { EmaLength = 1 });
		ThrowsExactly<ArgumentOutOfRangeException>(() => new MarketMeannessIndex { Length = 1 });
		ThrowsExactly<ArgumentOutOfRangeException>(() => new OptimalTracking { Length = 1 });
		ThrowsExactly<ArgumentOutOfRangeException>(() => new IchimokuLine { Length = 1 });

		new FractalAdaptiveMovingAverage { Length = 4 }.Length.AssertEqual(4);
		new MassIndex { EmaLength = 2 }.EmaLength.AssertEqual(2);
		new MarketMeannessIndex { Length = 2 }.Length.AssertEqual(2);
		new OptimalTracking { Length = 2 }.Length.AssertEqual(2);
		new IchimokuLine { Length = 2 }.Length.AssertEqual(2);
	}

	[TestMethod]
	public void DegenerateIndicatorSeriesRecover()
	{
		var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		static TimeFrameCandleMessage candle(DateTime time, decimal open, decimal close, decimal spread)
			=> new()
			{
				OpenTime = time,
				CloseTime = time,
				OpenPrice = open,
				HighPrice = open.Max(close) + spread,
				LowPrice = open.Min(close) - spread,
				ClosePrice = close,
				TotalVolume = 1500m,
				State = CandleStates.Finished,
			};

		var frama = new FractalAdaptiveMovingAverage();
		IIndicatorValue framaValue = null;
		for (var i = 0; i < 100; i++)
		{
			var input = candle(start.AddMinutes(i), 100m, 100m, 0m);
			framaValue = frama.Process(new CandleIndicatorValue(frama, input) { IsFinal = true });
		}
		framaValue.IsEmpty.AssertFalse();
		framaValue.GetValue<decimal>().AssertEqual(100m);

		var wave = new WaveTrendOscillator();
		var previous = 100m;
		IWaveTrendOscillatorValue flatWave = null;
		IWaveTrendOscillatorValue recoveredWave = null;
		for (var i = 0; i < 100; i++)
		{
			var close = i == 40 ? 300m : 100m;
			var spread = i == 40 ? 60m : 1m;
			var input = candle(start.AddMinutes(i), previous, close, spread);
			var value = (IWaveTrendOscillatorValue)wave.Process(new CandleIndicatorValue(wave, input) { IsFinal = true });
			if (i == 39)
				flatWave = value;
			if (i == 40)
				recoveredWave = value;
			previous = close;
		}
		flatWave.Wt1Value.IsEmpty.AssertTrue();
		flatWave.Wt2Value.IsEmpty.AssertTrue();
		recoveredWave.Wt1Value.IsEmpty.AssertFalse();
		recoveredWave.Wt2Value.IsEmpty.AssertFalse();

		var chaikin = new ChaikinVolatility();
		chaikin.Ema.Length = 2;
		chaikin.Roc.Length = 2;
		IIndicatorValue flatChaikin = null;
		IIndicatorValue chaikinValue = null;
		var spreads = new[] { 0m, 0m, 1m, 2m, 4m };
		for (var i = 0; i < spreads.Length; i++)
		{
			var input = candle(start.AddMinutes(i), 100m, 100m, spreads[i]);
			chaikinValue = chaikin.Process(new CandleIndicatorValue(chaikin, input) { IsFinal = true });
			if (i == 1)
				flatChaikin = chaikinValue;
		}
		flatChaikin.IsEmpty.AssertTrue();
		chaikinValue.IsEmpty.AssertFalse();
	}

	[TestMethod]
	public void FlatRsiInputPreservesPreviousValue()
	{
		var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var rsi = new RelativeStrengthIndex { Length = 3 };
		var prices = new[] { 100m, 110m, 105m, 107m };
		IIndicatorValue previous = null;

		for (var i = 0; i < prices.Length; i++)
		{
			previous = rsi.Process(new DecimalIndicatorValue(rsi, prices[i], start.AddMinutes(i)) { IsFinal = true });
		}

		var preview = rsi.Process(new DecimalIndicatorValue(rsi, prices[^1], start.AddMinutes(prices.Length)));
		var final = rsi.Process(new DecimalIndicatorValue(rsi, prices[^1], start.AddMinutes(prices.Length)) { IsFinal = true });

		previous.IsEmpty.AssertFalse();
		preview.GetValue<decimal>().AssertEqual(previous.GetValue<decimal>());
		final.GetValue<decimal>().AssertEqual(previous.GetValue<decimal>());
	}
}
