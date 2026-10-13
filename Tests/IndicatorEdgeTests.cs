namespace StockSharp.Tests;

[TestClass]
public class IndicatorEdgeTests : BaseTestClass
{
	private static readonly DateTime _start = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	[TestMethod]
	public void CciPreviewMatchesSubsequentFinal()
	{
		var cci = new CommodityChannelIndex { Length = 2 };

		Process(cci, 1m, 0, true);
		Process(cci, 2m, 1, true);

		var preview = Process(cci, 3m, 2, false).GetValue<decimal>();
		var final = Process(cci, 3m, 2, true).GetValue<decimal>();

		preview.AssertEqual(final);
		((preview - 200m / 3m).Abs() < 0.0000001m).AssertTrue($"Unexpected CCI value {preview}.");
	}

	[TestMethod]
	public void CciUsesOneWindowForAverageAndDeviation()
	{
		var cci = new CommodityChannelIndex { Length = 2 };

		Process(cci, decimal.MaxValue / 10m, 0, true);
		Process(cci, 0.1m, 1, true);

		var nonFlat = Process(cci, 0m, 2, true);
		var flat = Process(cci, 0m, 3, true);

		((nonFlat.GetValue<decimal>() + 200m / 3m).Abs() < 0.0000001m).AssertTrue($"Unexpected CCI value {nonFlat}.");
		flat.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void CciPreviewUsesFinalSummationOrder()
	{
		var cci = new CommodityChannelIndex { Length = 3 };
		var large = decimal.MaxValue / 10m;

		Process(cci, 0m, 0, true);
		Process(cci, large, 1, true);
		Process(cci, -large, 2, true);

		var preview = Process(cci, 0.1m, 3, false);
		var final = Process(cci, 0.1m, 3, true);

		preview.GetValue<decimal>().AssertEqual(final.GetValue<decimal>());
	}

	[TestMethod]
	public void FlatCciDoesNotOverflowWhileAveraging()
	{
		var cci = new CommodityChannelIndex { Length = 4 };
		var price = decimal.MaxValue / 3m;
		IIndicatorValue result = null;

		for (var i = 0; i < cci.Length; i++)
			result = Process(cci, price, i, true);

		result.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void FramaFirstFormedValueIsNotAnchoredToZero()
	{
		var frama = new FractalAdaptiveMovingAverage { Length = 6 };
		var prices = new[] { 100m, 110m, 100m, 110m, 100m, 105m };
		IIndicatorValue result = null;

		for (var i = 0; i < prices.Length; i++)
			result = Process(frama, prices[i], i, true);

		result.IsEmpty.AssertFalse();
		(result.GetValue<decimal>() >= prices.Min()).AssertTrue($"First formed FRAMA value {result} was pulled below the observed price range.");
	}

	[TestMethod]
	public void FramaPreviewMatchesFinalWithoutMutatingState()
	{
		static FractalAdaptiveMovingAverage warmUp()
		{
			var frama = new FractalAdaptiveMovingAverage { Length = 6 };
			var prices = new[] { 100m, 110m, 100m, 110m, 100m, 105m };

			for (var i = 0; i < prices.Length; i++)
				Process(frama, prices[i], i, true);

			return frama;
		}

		var previewed = warmUp();
		var finalOnly = warmUp();

		var preview = Process(previewed, 120m, 6, false).GetValue<decimal>();
		var finalAfterPreview = Process(previewed, 120m, 6, true).GetValue<decimal>();
		var finalWithoutPreview = Process(finalOnly, 120m, 6, true).GetValue<decimal>();

		preview.AssertEqual(finalWithoutPreview);
		finalAfterPreview.AssertEqual(finalWithoutPreview);
	}

	[TestMethod]
	public void RollingWindowRemovesOldestDirectionChange()
	{
		var indicator = new MarketMeannessIndex { Length = 4 };

		Process(indicator, 0, 0m, true);
		Process(indicator, 1, 1m, true);
		Process(indicator, 2, 0m, true);

		var formed = Process(indicator, 3, 1m, true);
		var rolled = Process(indicator, 4, 2m, true);

		AreEqual(200m / 3m, formed.GetValue<decimal>());
		AreEqual(100m / 3m, rolled.GetValue<decimal>());
	}

	[TestMethod]
	public void PreviewMatchesFinalWithoutMutatingRollingState()
	{
		var previewed = new MarketMeannessIndex { Length = 4 };
		var parity = new MarketMeannessIndex { Length = 4 };
		var control = new MarketMeannessIndex { Length = 4 };

		foreach (var (minute, price) in new[] { (0, 0m), (1, 1m), (2, 0m), (3, 1m) })
		{
			Process(previewed, minute, price, true);
			Process(parity, minute, price, true);
			Process(control, minute, price, true);
		}

		var preview = Process(previewed, 4, 2m, false);
		var parityFinal = Process(parity, 4, 2m, true);

		AreEqual(100m / 3m, preview.GetValue<decimal>());
		AreEqual(parityFinal.GetValue<decimal>(), preview.GetValue<decimal>());

		var previewedFinal = Process(previewed, 4, -1m, true);
		var controlFinal = Process(control, 4, -1m, true);

		AreEqual(200m / 3m, previewedFinal.GetValue<decimal>());
		AreEqual(controlFinal.GetValue<decimal>(), previewedFinal.GetValue<decimal>());
	}

	[TestMethod]
	public void MassIndexRecoversAfterFlatRangesWithoutCommittingPreview()
	{
		var actual = new MassIndex { Length = 2, EmaLength = 2 };
		var expected = new MassIndex { Length = 2, EmaLength = 2 };

		for (var i = 0; i < 2; i++)
		{
			IsTrue(Process(actual, i, 0m, true).IsEmpty);
			IsTrue(Process(expected, i, 0m, true).IsEmpty);
		}

		IsTrue(Process(actual, 2, 8m, false).IsEmpty);

		var actualFirstRange = Process(actual, 2, 4m, true);
		var expectedFirstRange = Process(expected, 2, 4m, true);
		AreEqual(expectedFirstRange.IsEmpty, actualFirstRange.IsEmpty);

		var actualRecovered = Process(actual, 3, 4m, true);
		var expectedRecovered = Process(expected, 3, 4m, true);

		IsFalse(actualRecovered.IsEmpty);
		AreEqual(expectedRecovered.GetValue<decimal>(), actualRecovered.GetValue<decimal>());
		IsTrue(actualRecovered.GetValue<decimal>() > 0m);
	}

	[TestMethod]
	public void McGinleyRecoversFromZeroWarmUpWithoutCommittingPreview()
	{
		var actual = new McGinleyDynamic { Length = 2 };
		var expected = new McGinleyDynamic { Length = 2 };

		for (var i = 0; i < 2; i++)
		{
			Process(actual, i, 0m, true);
			Process(expected, i, 0m, true);
		}

		var zero = Process(actual, 2, 0m, true);
		Process(expected, 2, 0m, true);
		IsFalse(zero.IsEmpty);
		AreEqual(0m, zero.GetValue<decimal>());

		var preview = Process(actual, 3, 20m, false);
		IsFalse(preview.IsEmpty);
		AreEqual(20m, preview.GetValue<decimal>());

		var actualReseed = Process(actual, 3, 10m, true);
		var expectedReseed = Process(expected, 3, 10m, true);
		AreEqual(10m, actualReseed.GetValue<decimal>());
		AreEqual(expectedReseed.GetValue<decimal>(), actualReseed.GetValue<decimal>());

		var actualRecovered = Process(actual, 4, 12m, true);
		var expectedRecovered = Process(expected, 4, 12m, true);
		var recovered = actualRecovered.GetValue<decimal>();

		AreEqual(expectedRecovered.GetValue<decimal>(), recovered);
		IsTrue(recovered > 10m && recovered < 12m);
	}

	[TestMethod]
	public void PreviewIncludesTheWholeWarmUpWindow()
	{
		var sum = new Sum { Length = 3 };

		Process(sum, 1m, 0, true);

		var preview = Process(sum, 2m, 1, false);
		var final = Process(sum, 2m, 1, true);

		AreEqual(3m, preview.GetValue<decimal>());
		AreEqual(preview.GetValue<decimal>(), final.GetValue<decimal>());
	}

	[TestMethod]
	public void FinalAndPreviewDoNotAccumulateDecimalDrift()
	{
		var sum = new Sum { Length = 2 };
		var large = decimal.MaxValue / 10m;

		Process(sum, large, 0, true);
		Process(sum, 0.1m, 1, true);

		var retainedFraction = Process(sum, 0m, 2, true);
		var preview = Process(sum, 0m, 3, false);
		var final = Process(sum, 0m, 3, true);

		AreEqual(0.1m, retainedFraction.GetValue<decimal>());
		AreEqual(0m, preview.GetValue<decimal>());
		AreEqual(preview.GetValue<decimal>(), final.GetValue<decimal>());
	}

	[TestMethod]
	public void ExtremeValueCanLeaveTheWindow()
	{
		var sum = new Sum { Length = 1 };

		Process(sum, decimal.MinValue, 0, true);

		var preview = Process(sum, 0m, 1, false);
		var final = Process(sum, 0m, 1, true);

		AreEqual(0m, preview.GetValue<decimal>());
		AreEqual(preview.GetValue<decimal>(), final.GetValue<decimal>());
	}

	[TestMethod]
	public void ReplacementAvoidsTransientOverflow()
	{
		var sum = new Sum { Length = 3 };

		Process(sum, decimal.MinValue, 0, true);
		Process(sum, decimal.MaxValue, 1, true);
		Process(sum, decimal.MaxValue, 2, true);

		var preview = Process(sum, decimal.MinValue, 3, false);
		var final = Process(sum, decimal.MinValue, 3, true);

		AreEqual(decimal.MaxValue, preview.GetValue<decimal>());
		AreEqual(preview.GetValue<decimal>(), final.GetValue<decimal>());
	}

	[TestMethod]
	public void ResetHandlerCanStartANewWindow()
	{
		var sum = new Sum { Length = 2 };

		Process(sum, 1m, 0, true);
		Process(sum, 2m, 1, true);
		sum.Reseted += () => Process(sum, 4m, 2, true);

		sum.Reset();
		var result = Process(sum, 6m, 3, true);

		AreEqual(10m, result.GetValue<decimal>());
	}

	[TestMethod]
	public void MoneyFlowPreviewRecognizesAnEmptyNegativeWindow()
	{
		var moneyFlow = new MoneyFlowIndex { Length = 2 };
		var large = decimal.MaxValue / 10m;

		Process(moneyFlow, 3m, 1m, 0, true);
		Process(moneyFlow, 2m, large / 2m, 1, true);
		Process(moneyFlow, 1m, 0.1m, 2, true);
		Process(moneyFlow, 2m, 1m, 3, true);

		var preview = Process(moneyFlow, 3m, 1m, 4, false);
		var final = Process(moneyFlow, 3m, 1m, 4, true);

		AreEqual(100m, preview.GetValue<decimal>());
		AreEqual(preview.GetValue<decimal>(), final.GetValue<decimal>());
	}

	[TestMethod]
	public void ZeroNormalizersProduceEmptyValuesAndRecover()
	{
		IIndicator[] indicators =
		[
			new CenterOfGravityOscillator(),
			new DisparityIndex(),
			new ForecastOscillator(),
		];

		foreach (var indicator in indicators)
		{
			IIndicatorValue flat = null;

			for (var i = 0; i < indicator.NumValuesToInitialize; i++)
				flat = Process(indicator, 0m, i, true);

			IsTrue(flat.IsEmpty, indicator.ToString());

			var recovered = Process(indicator, 1m, indicator.NumValuesToInitialize, true);
			IsFalse(recovered.IsEmpty, indicator.ToString());
		}
	}

	[TestMethod]
	public void EmptyRocPipelinesRecoverWhenTheirBaselineBecomesDefined()
	{
		var composite = new CompositeMomentum();
		ICompositeMomentumValue compositeValue = null;

		for (var i = 0; i < composite.NumValuesToInitialize; i++)
			compositeValue = (ICompositeMomentumValue)Process(composite, 0m, i, true);

		IsTrue(compositeValue.CompositeLineValue.IsEmpty);
		IsTrue(compositeValue.SmaValue.IsEmpty);

		var index = composite.NumValuesToInitialize;
		var recoveryCount = composite.LongRoc.NumValuesToInitialize + composite.Sma.NumValuesToInitialize;

		for (var i = 0; i < recoveryCount; i++)
			compositeValue = (ICompositeMomentumValue)Process(composite, i + 1m, index++, true);

		IsFalse(compositeValue.CompositeLineValue.IsEmpty);
		IsFalse(compositeValue.SmaValue.IsEmpty);

		var trix = new Trix();
		IIndicatorValue trixValue = null;

		for (var i = 0; i < trix.NumValuesToInitialize; i++)
			trixValue = Process(trix, 0m, i, true);

		IsTrue(trixValue.IsEmpty);
		IsTrue(Process(trix, 1m, trix.NumValuesToInitialize, true).IsEmpty);
		IsFalse(Process(trix, 2m, trix.NumValuesToInitialize + 1, true).IsEmpty);
	}

	[TestMethod]
	public void WoodiesCciKeepsInnerFinalityAlignedForEmptySequenceValues()
	{
		var woodies = new WoodiesCCI();
		IWoodiesCCIValue value = null;

		for (var i = 0; i < woodies.Length; i++)
			value = (IWoodiesCCIValue)Process(woodies, 0m, i, true);

		IsTrue(value.IsFinal);
		IsTrue(value.CciValue.IsEmpty);
		IsTrue(value.SmaValue.IsEmpty);
		IsTrue(value.InnerValues.Values.All(inner => inner.IsFinal));

		var preview = (IWoodiesCCIValue)Process(woodies, 1m, woodies.Length, false);
		IsFalse(preview.IsFinal);
		IsTrue(preview.InnerValues.Values.All(inner => !inner.IsFinal));

		var final = (IWoodiesCCIValue)Process(woodies, 2m, woodies.Length, true);
		IsTrue(final.IsFinal);
		IsTrue(final.InnerValues.Values.All(inner => inner.IsFinal));
	}

	[TestMethod]
	public void PreloadRejectedMidStreamLoadsNothing()
	{
		var sma = new SimpleMovingAverage { Length = 3 };

		(IIndicatorValue input, IIndicatorValue output)[] batch =
		[
			PreloadPair(sma, 0, 1m, true),
			PreloadPair(sma, 1, 2m, true),
			PreloadPair(sma, 2, 3m, false),
		];

		// The third pair is not final, so the batch is rejected. A bulk load that reports failure
		// must have taken no effect: nothing loaded, nothing accumulated in the container.
		Throws<ArgumentException>(() => sma.Preload(batch));

		IsFalse(sma.IsPreloaded);
		IsFalse(sma.IsFormed);
		AreEqual(0, sma.Container.Count);
	}

	[TestMethod]
	public void PreloadCanBeRetriedAfterRejection()
	{
		var sma = new SimpleMovingAverage { Length = 3 };

		(IIndicatorValue input, IIndicatorValue output)[] rejected =
		[
			PreloadPair(sma, 0, 1m, true),
			PreloadPair(sma, 1, 2m, true),
			PreloadPair(sma, 2, 3m, false),
		];

		Throws<ArgumentException>(() => sma.Preload(rejected));

		(IIndicatorValue input, IIndicatorValue output)[] corrected =
		[
			PreloadPair(sma, 0, 1m, true),
			PreloadPair(sma, 1, 2m, true),
			PreloadPair(sma, 2, 3m, true),
		];

		// The rejected attempt claimed nothing, so the corrected batch is the first load that
		// counts and the indicator serves the values it was given.
		sma.Preload(corrected);

		IsTrue(sma.IsPreloaded);
		IsTrue(sma.IsFormed);
		AreEqual(3m, Process(sma, 3m, 2, true).GetValue<decimal>());
	}

	[TestMethod]
	public void PreloadRejectedForDuplicateTimeLeavesOrdinaryProcessingAvailable()
	{
		var sma = new SimpleMovingAverage { Length = 3 };

		(IIndicatorValue input, IIndicatorValue output)[] batch =
		[
			PreloadPair(sma, 0, 1m, true),
			PreloadPair(sma, 1, 2m, true),
			PreloadPair(sma, 1, 3m, true),
		];

		// Two pairs share a timestamp, so the batch cannot be indexed by time and is rejected.
		// A caller who never got a preload is entitled to feed the indicator the ordinary way.
		Throws<ArgumentException>(() => sma.Preload(batch));

		IsFalse(sma.IsPreloaded);

		// SMA(3) over 1, 2, 3 = 6 / 3 = 2.
		Process(sma, 1m, 0, true);
		Process(sma, 2m, 1, true);
		AreEqual(2m, Process(sma, 3m, 2, true).GetValue<decimal>());
	}

	[TestMethod]
	public void TwapWeightsEachCandleByItsDuration()
	{
		var twap = new TimeWeightedAveragePrice();

		// One minute at 100 then three minutes at 200: (100 * 1 + 200 * 3) / (1 + 3) = 175.
		// An unweighted mean over the two bars gives 150, which ignores how long each price stood.
		AreEqual(100m, Process(twap, 0, 1, 100m, true).GetValue<decimal>());
		AreEqual(175m, Process(twap, 1, 3, 200m, true).GetValue<decimal>());
	}

	[TestMethod]
	public void TwapPreviewDoesNotEnterTheRunningAverage()
	{
		var previewed = new TimeWeightedAveragePrice();
		var control = new TimeWeightedAveragePrice();

		Process(previewed, 0, 1, 100m, true);
		Process(control, 0, 1, 100m, true);

		// The preview answers "what if the open bar closed here" — (100 + 400) / 2 = 250 — and
		// committing a different close must land where a series without the preview lands.
		AreEqual(250m, Process(previewed, 1, 1, 400m, false).GetValue<decimal>());

		var previewedFinal = Process(previewed, 1, 1, 200m, true).GetValue<decimal>();
		var controlFinal = Process(control, 1, 1, 200m, true).GetValue<decimal>();

		AreEqual(150m, previewedFinal);
		AreEqual(controlFinal, previewedFinal);
	}

	[TestMethod]
	public void TwapResetStartsANewSeries()
	{
		var twap = new TimeWeightedAveragePrice();

		Process(twap, 0, 1, 100m, true);
		AreEqual(150m, Process(twap, 1, 1, 200m, true).GetValue<decimal>());

		twap.Reset();
		IsFalse(twap.IsFormed);

		// Nothing from before the reset may leak in: the first candle of the new series is the
		// whole series, so the average equals its own typical price.
		AreEqual(300m, Process(twap, 2, 1, 300m, true).GetValue<decimal>());
	}

	[TestMethod]
	public void ZigZagReversalPointsBackToTheBarThatHeldTheExtremum()
	{
		var zigZag = new ZigZag { Deviation = 0.05m };
		var closes = new[] { 100m, 110m, 120m, 100m };

		for (var i = 0; i < closes.Length - 1; i++)
			IsTrue(Process(zigZag, closes[i], i, true).IsEmpty, $"Bar {i} announced a reversal before it could be known.");

		var reversal = (ZigZagIndicatorValue)Process(zigZag, closes[3], 3, true);

		// The peak of 120 stood on bar 2; bar 3 closes 20 below it, past the 5% deviation
		// (120 - 120 * 0.05 = 114), so the reversal is confirmed one bar later: Shift = 3 - 2 = 1.
		// Shift counts bars back from the announcing bar to the bar the value describes, the same
		// meaning FractalPart gives it.
		IsFalse(reversal.IsEmpty);
		AreEqual(120m, reversal.GetValue<decimal>(default));
		IsTrue(reversal.IsUp);
		AreEqual(_start.AddMinutes(3), reversal.Time);
		AreEqual(1, reversal.Shift);
		AreEqual(reversal.GetValue<decimal>(default), closes[3 - reversal.Shift]);
	}

	[TestMethod]
	public void ZigZagShiftCountsBarsBackWhenThePeakIsTheFirstFormedBar()
	{
		var zigZag = new ZigZag { Deviation = 0.05m };
		var closes = new[] { 100m, 120m, 115m, 100m };

		for (var i = 0; i < closes.Length - 1; i++)
			IsTrue(Process(zigZag, closes[i], i, true).IsEmpty, $"Bar {i} announced a reversal before it could be known.");

		var reversal = (ZigZagIndicatorValue)Process(zigZag, closes[3], 3, true);

		// Same threshold, but the peak of 120 stands on bar 1 and bar 2 (115) stays above 114,
		// so the reversal is confirmed two bars later: Shift = 3 - 1 = 2.
		AreEqual(120m, reversal.GetValue<decimal>(default));
		AreEqual(2, reversal.Shift);
		AreEqual(reversal.GetValue<decimal>(default), closes[3 - reversal.Shift]);
	}

	[TestMethod]
	public void FractalIsAnnouncedOnlyAfterTheBarsThatConfirmIt()
	{
		var fractals = new Fractals();
		var highs = new[] { 10m, 11m, 12m, 11m, 10m };
		var lows = new[] { 9m, 10m, 11m, 10m, 9m };

		for (var i = 0; i < highs.Length - 1; i++)
		{
			var pending = (IFractalsValue)Process(fractals, i, highs[i], lows[i], true);
			IsFalse(pending.HasPattern, $"Bar {i} announced a fractal that only bar 4 can confirm.");
		}

		var confirmed = (IFractalsValue)Process(fractals, 4, highs[4], lows[4], true);
		var up = (FractalPartIndicatorValue)confirmed.UpValue;

		// Length 5 puts two bars either side of the middle one, so the peak on bar 2 becomes
		// knowable only on bar 4: announced at bar 4, pointing back Shift = 4 - 2 = 2 bars.
		// The lows 9, 10, 11, 10, 9 rise into the middle, so no down fractal forms.
		IsTrue(confirmed.HasUp);
		IsFalse(confirmed.HasDown);
		AreEqual(12m, up.GetValue<decimal>(default));
		AreEqual(2, up.Shift);
		AreEqual(_start.AddMinutes(4), up.Time);
		AreEqual(up.GetValue<decimal>(default), highs[4 - up.Shift]);
	}

	[TestMethod]
	public void ParabolicSarUsesTheFirstCandleAsLookback()
	{
		var sar = new ParabolicSar();

		// The first bar establishes the initial extreme; the second bar is the first one for which
		// a stop can be published.
		IsTrue(Process(sar, 0, 10m, 8m, true).IsEmpty, "One candle produced a SAR value.");
		IsFalse(Process(sar, 1, 12m, 9m, true).IsEmpty, "The second candle did not produce the first SAR value.");
	}

	[TestMethod]
	public void ParabolicSarFirstValueReflectsTheSecondCandle()
	{
		var near = FirstSarValue([(10m, 8m), (12m, 9m)]);
		var far = FirstSarValue([(10m, 8m), (20m, 9m)]);

		AreNotEqual(near, far, $"Both streams seeded at {near}, so the first SAR value ignored the second candle.");
	}

	[TestMethod]
	public void ParabolicSarKeepsOnlyTheCandlesItReads()
	{
		var sar = new ParabolicSar();

		// No public surface exposes the buffer, so read the field the calculation walks.
		var candles = (List<ICandleMessage>)typeof(ParabolicSar)
			.GetField("_candles", BindingFlags.Instance | BindingFlags.NonPublic)
			.GetValue(sar);

		IsNotNull(candles);

		for (var i = 0; i < 50; i++)
			Process(sar, i, 100m + i, 99m + i, true);

		var afterFirstHalf = candles.Count;

		for (var i = 50; i < 100; i++)
			Process(sar, i, 100m + i, 99m + i, true);

		// The calculation reads candles[^1]..candles[^3] only, so the buffer must not hold more
		// candles than were fed, nor keep growing with the length of the history.
		IsTrue(afterFirstHalf <= 50, $"50 candles left {afterFirstHalf} entries in the buffer.");
		AreEqual(afterFirstHalf, candles.Count, "The buffer grows with every candle although only the last three are read.");
	}

	/// <summary>
	/// <see cref="IIndicator.Source"/> names which price of the input the indicator reads, so writing back
	/// the value it already holds asks for nothing to change. A settings round-trip and a bound editor both
	/// write every property, the untouched ones included, and a user who never went near the source is
	/// entitled to keep the history the indicator has already accumulated - a reset there silently restarts
	/// a live indicator and hands the strategy a warm-up value in place of a real one.
	/// </summary>
	[TestMethod]
	public void SettingTheSameSourceDoesNotResetTheIndicator()
	{
		var sma = new SimpleMovingAverage { Length = 3, Source = Level1Fields.ClosePrice };

		Process(sma, 10m, 0, true);
		Process(sma, 20m, 1, true);

		// SMA(3) over 10, 20, 30 = 60 / 3 = 20.
		AreEqual(20m, Process(sma, 30m, 2, true).GetValue<decimal>());
		IsTrue(sma.IsFormed);

		sma.Source = Level1Fields.ClosePrice;

		IsTrue(sma.IsFormed, "Writing back the source the indicator already had un-formed it.");
		AreEqual(3, sma.Container.Count, "Writing back the source the indicator already had dropped the accumulated values.");

		// SMA(3) over 20, 30, 40 = 90 / 3 = 30: the window still holds the two values fed before the write.
		AreEqual(30m, Process(sma, 40m, 3, true).GetValue<decimal>());
	}

	/// <summary>
	/// The committed series in Resources/IndicatorsData are produced by the very code they then pin, so they
	/// agree with the implementation whatever it computes - a formula that drifts drags its own reference
	/// along. These numbers come from outside the implementation: each is the published definition of the
	/// indicator worked through by hand on a short series, so a user reading "SMA", "EMA", "RSI" or "ATR"
	/// gets the quantity that name stands for and not merely a stable one.
	/// </summary>
	[TestMethod]
	public void SmaEmaRsiAtrMatchTheirTextbookFormulas()
	{
		// Every expected value below is a plain rational, while the indicators reach it through a chain of
		// decimal divisions, so the comparison allows the last few digits of a 28-digit decimal to differ -
		// and nothing more than that.
		static void near(decimal actual, decimal expected, string name)
			=> ((actual - expected).Abs() <= 0.000000000001m).AssertTrue($"{name}: expected {expected}, got {actual}.");

		// SMA(n) is the plain mean of the last n closes.
		var sma = new SimpleMovingAverage { Length = 4 };

		Process(sma, 10m, 0, true);
		Process(sma, 20m, 1, true);
		Process(sma, 30m, 2, true);

		// (10 + 20 + 30 + 40) / 4 = 25.
		near(Process(sma, 40m, 3, true).GetValue<decimal>(), 25m, "SMA(4) over 10, 20, 30, 40");
		IsTrue(sma.IsFormed, "SMA(4) was not formed after its fourth close.");

		// (20 + 30 + 40 + 50) / 4 = 35: the oldest close leaves the window.
		near(Process(sma, 50m, 4, true).GetValue<decimal>(), 35m, "SMA(4) over 20, 30, 40, 50");

		// EMA is seeded with the mean of the first n closes and then carried forward by
		// EMA = previous + k * (close - previous), k = 2 / (n + 1). n = 3 puts k at exactly 0.5.
		var ema = new ExponentialMovingAverage { Length = 3 };

		Process(ema, 10m, 0, true);
		Process(ema, 20m, 1, true);

		// Seed = (10 + 20 + 30) / 3 = 20.
		near(Process(ema, 30m, 2, true).GetValue<decimal>(), 20m, "EMA(3) seed");
		IsTrue(ema.IsFormed, "EMA(3) was not formed after its third close.");

		// 20 + 0.5 * (40 - 20) = 30.
		near(Process(ema, 40m, 3, true).GetValue<decimal>(), 30m, "EMA(3) after 40");

		// 30 + 0.5 * (50 - 30) = 40.
		near(Process(ema, 50m, 4, true).GetValue<decimal>(), 40m, "EMA(3) after 50");

		// Wilder's RSI: the average gain and the average loss are seeded with the mean of the first n gains
		// and losses and then smoothed by (previous * (n - 1) + current) / n, and
		// RSI = 100 - 100 / (1 + avgGain / avgLoss), which is the same number as
		// 100 * avgGain / (avgGain + avgLoss).
		var rsi = new RelativeStrengthIndex { Length = 3 };

		// Closes 100, 101, 103, 102, 104, 103 give changes +1, +2, -1, +2, -1,
		// so gains 1, 2, 0, 2, 0 and losses 0, 0, 1, 0, 1.
		Process(rsi, 100m, 0, true);
		Process(rsi, 101m, 1, true);
		Process(rsi, 103m, 2, true);

		IsFalse(rsi.IsFormed, "RSI(3) called itself formed before three price changes had been seen.");

		// Seeds: avgGain = (1 + 2 + 0) / 3 = 1, avgLoss = (0 + 0 + 1) / 3 = 1/3.
		// RSI = 100 * 1 / (1 + 1/3) = 75.
		near(Process(rsi, 102m, 3, true).GetValue<decimal>(), 75m, "RSI(3) first formed value");
		IsTrue(rsi.IsFormed, "RSI(3) was not formed once its averages were seeded.");

		// avgGain = (1 * 2 + 2) / 3 = 4/3, avgLoss = (1/3 * 2 + 0) / 3 = 2/9.
		// RSI = 100 * (4/3) / (4/3 + 2/9) = 600 / 7.
		near(Process(rsi, 104m, 4, true).GetValue<decimal>(), 600m / 7m, "RSI(3) after a rise");

		// avgGain = (4/3 * 2 + 0) / 3 = 8/9, avgLoss = (2/9 * 2 + 1) / 3 = 13/27.
		// RSI = 100 * (8/9) / (8/9 + 13/27) = 2400 / 37.
		near(Process(rsi, 103m, 5, true).GetValue<decimal>(), 2400m / 37m, "RSI(3) after a fall");

		// Wilder's ATR over the true range - max(high - low, |previous close - high|, |previous close - low|),
		// the first bar having no previous close and so contributing its own high - low. The first n ranges
		// are averaged plainly, and from there ATR = (previous * (n - 1) + range) / n.
		var atr = new AverageTrueRange { Length = 3 };

		// Ranges: bar 0 = 12 - 10 = 2; bar 1 = max(2, |11 - 13|, |11 - 11|) = 2;
		// bar 2 = max(2, |12 - 16|, |12 - 14|) = 4.
		Process(atr, 0, 12m, 10m, 11m, true);
		Process(atr, 1, 13m, 11m, 12m, true);

		IsFalse(atr.IsFormed, "ATR(3) called itself formed before three ranges had been seen.");

		// Seed = (2 + 2 + 4) / 3 = 8/3.
		near(Process(atr, 2, 16m, 14m, 15m, true).GetValue<decimal>(), 8m / 3m, "ATR(3) seed");
		IsTrue(atr.IsFormed, "ATR(3) was not formed once its seed window was full.");

		// Range = max(15 - 12, |15 - 15|, |15 - 12|) = 3, so (8/3 * 2 + 3) / 3 = 25/9.
		near(Process(atr, 3, 15m, 12m, 13m, true).GetValue<decimal>(), 25m / 9m, "ATR(3) after a wide bar");

		// Range = max(14 - 13, |13 - 14|, |13 - 13|) = 1, so (25/9 * 2 + 1) / 3 = 59/27.
		near(Process(atr, 4, 14m, 13m, 13.5m, true).GetValue<decimal>(), 59m / 27m, "ATR(3) after a narrow bar");
	}

	private static decimal FirstSarValue((decimal high, decimal low)[] candles)
	{
		var sar = new ParabolicSar();

		for (var i = 0; i < candles.Length; i++)
		{
			var value = Process(sar, i, candles[i].high, candles[i].low, true);

			if (!value.IsEmpty)
				return value.GetValue<decimal>();
		}

		throw new InvalidOperationException("No SAR value was published.");
	}

	private static IIndicatorValue Process(ParabolicSar indicator, int minute, decimal high, decimal low, bool isFinal)
	{
		var time = _start.AddMinutes(minute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time.AddMinutes(1),
			OpenPrice = low,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = high,
			TotalVolume = 1m,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static (IIndicatorValue input, IIndicatorValue output) PreloadPair(IIndicator indicator, int minute, decimal value, bool isFinal)
	{
		var time = _start.AddMinutes(minute);

		var input = indicator.CreateValue(time, [value]);
		var output = indicator.CreateValue(time, [value]);

		input.IsFinal = output.IsFinal = isFinal;

		return (input, output);
	}

	private static IIndicatorValue Process(Fractals indicator, int minute, decimal high, decimal low, bool isFinal)
	{
		var time = _start.AddMinutes(minute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time.AddMinutes(1),
			OpenPrice = low,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = high,
			TotalVolume = 1m,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static IIndicatorValue Process(TimeWeightedAveragePrice indicator, int startMinute, int minutes, decimal price, bool isFinal)
	{
		var time = _start.AddMinutes(startMinute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time.AddMinutes(minutes),
			OpenPrice = price,
			HighPrice = price,
			LowPrice = price,
			ClosePrice = price,
			TotalVolume = 1m,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static IIndicatorValue Process(IIndicator indicator, int minute, decimal high, decimal low, decimal close, bool isFinal)
	{
		var time = _start.AddMinutes(minute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time.AddMinutes(1),
			OpenPrice = close,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = close,
			TotalVolume = 1m,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static IIndicatorValue Process(IIndicator indicator, decimal price, int index, bool isFinal)
	{
		var time = _start.AddMinutes(index);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time.AddMinutes(1),
			OpenPrice = price,
			HighPrice = price,
			LowPrice = price,
			ClosePrice = price,
			TotalVolume = 1m,
			State = CandleStates.Finished,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static IIndicatorValue Process(MarketMeannessIndex indicator, int minute, decimal price, bool isFinal)
		=> indicator.Process(new DecimalIndicatorValue(indicator, price, _start.AddMinutes(minute)) { IsFinal = isFinal });

	private static IIndicatorValue Process(MassIndex indicator, int minute, decimal range, bool isFinal)
	{
		var middle = 100m;
		var time = _start.AddMinutes(minute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time,
			OpenPrice = middle,
			HighPrice = middle + range / 2,
			LowPrice = middle - range / 2,
			ClosePrice = middle,
			TotalVolume = 1m,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}

	private static IIndicatorValue Process(McGinleyDynamic indicator, int minute, decimal price, bool isFinal)
		=> indicator.Process(new DecimalIndicatorValue(indicator, price, _start.AddMinutes(minute)) { IsFinal = isFinal });

	private static IIndicatorValue Process(Sum indicator, decimal value, int minute, bool isFinal)
		=> indicator.Process(new DecimalIndicatorValue(indicator, value, _start.AddMinutes(minute)) { IsFinal = isFinal });

	private static IIndicatorValue Process(MoneyFlowIndex indicator, decimal price, decimal volume, int minute, bool isFinal)
	{
		var time = _start.AddMinutes(minute);
		var candle = new TimeFrameCandleMessage
		{
			OpenTime = time,
			CloseTime = time,
			OpenPrice = price,
			HighPrice = price,
			LowPrice = price,
			ClosePrice = price,
			TotalVolume = volume,
			State = isFinal ? CandleStates.Finished : CandleStates.Active,
		};

		return indicator.Process(new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal });
	}
}
