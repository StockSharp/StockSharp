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
