namespace StockSharp.Tests;

using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Text.Json;

using DataType = StockSharp.Messages.DataType;

/// <summary>
/// Series, parameters and names an indicator offers, and how a parameter path is applied.
/// </summary>
partial class IndicatorTests
{
	[TestMethod]
	public void IndicatorValues_Standard()
	{
		var ind = new PassThroughIndicator();
		var t = DateTime.UtcNow;
		var tf = TimeSpan.FromMinutes(1);

		// DecimalIndicatorValue
		{
			var v1 = new DecimalIndicatorValue(ind, 123.45m, t) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new DecimalIndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();
			v2.Value.AssertEqual(123.45m);

			// empty
			var vEmpty = new DecimalIndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new DecimalIndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// CandleIndicatorValue
		{
			var c = new TimeFrameCandleMessage
			{
				OpenTime = t,
				CloseTime = t + tf,
				OpenPrice = 100m,
				HighPrice = 105m,
				LowPrice = 95m,
				ClosePrice = 102m,
				TotalVolume = 1000m,
				State = CandleStates.Finished,
				TypedArg = tf,
			};

			var v1 = new CandleIndicatorValue(ind, c);
			var arr = v1.ToValues().ToArray();
			var v2 = new CandleIndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();

			var c2 = v2.Value;
			c2.OpenPrice.AssertEqual(c.OpenPrice);
			c2.HighPrice.AssertEqual(c.HighPrice);
			c2.LowPrice.AssertEqual(c.LowPrice);
			c2.ClosePrice.AssertEqual(c.ClosePrice);
			c2.TotalVolume.AssertEqual(c.TotalVolume);

			// empty
			var vEmpty = new CandleIndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new CandleIndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// MarketDepthIndicatorValue
		{
			var depth = new QuoteChangeMessage
			{
				ServerTime = t,
				Bids = [new QuoteChange(100m, 10m)],
				Asks = [new QuoteChange(101m, 11m)]
			};

			var v1 = new MarketDepthIndicatorValue(ind, depth) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new MarketDepthIndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();

			// Use explicit presence checks before comparing: a conditional `?.AssertEqual`
			// would silently pass if the round-trip dropped the bids/asks (GetBestXxx() => null).
			var bestBid = v2.Value.GetBestBid();
			var bestAsk = v2.Value.GetBestAsk();
			bestBid.HasValue.AssertTrue();
			bestAsk.HasValue.AssertTrue();
			bestBid.Value.Price.AssertEqual(100m);
			bestAsk.Value.Price.AssertEqual(101m);

			// empty
			var vEmpty = new MarketDepthIndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new MarketDepthIndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// Level1IndicatorValue
		{
			var l1 = new Level1ChangeMessage { ServerTime = t };
			l1.Add(Level1Fields.LastTradePrice, 77m);
			l1.Add(Level1Fields.Volume, 555m);

			var v1 = new Level1IndicatorValue(ind, l1) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new Level1IndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();
			((decimal?)v2.Value.TryGet(Level1Fields.LastTradePrice)).AssertEqual(77m);
			((decimal?)v2.Value.TryGet(Level1Fields.Volume)).AssertEqual(555m);

			// empty
			var vEmpty = new Level1IndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new Level1IndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// TickIndicatorValue
		{
			var tick = new ExecutionMessage
			{
				ServerTime = t,
				TradePrice = 12.34m,
				TradeVolume = 9.87m,
				DataTypeEx = DataType.Ticks
			};

			var v1 = new TickIndicatorValue(ind, tick) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new TickIndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();
			v2.Value.Price.AssertEqual(12.34m);
			v2.Value.Volume.AssertEqual(9.87m);

			// empty
			var vEmpty = new TickIndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new TickIndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// PairIndicatorValue<decimal>
		{
			var p = (1.23m, 4.56m);
			var v1 = new PairIndicatorValue<decimal>(ind, p, t) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new PairIndicatorValue<decimal>(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();
			v2.Value.Item1.AssertEqual(1.23m);
			v2.Value.Item2.AssertEqual(4.56m);

			// empty
			var vEmpty = new PairIndicatorValue<decimal>(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new PairIndicatorValue<decimal>(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}

		// ShiftedIndicatorValue (extends SingleIndicatorValue<decimal> with extra Shift)
		{
			var v1 = new ShiftedIndicatorValue(ind, 999m, 5, t) { IsFinal = true };
			var arr = v1.ToValues().ToArray();
			var v2 = new ShiftedIndicatorValue(ind, t);
			v2.FromValues(arr);
			v2.IsEmpty.AssertFalse();
			v2.Value.AssertEqual(999m);
			v2.Shift.AssertEqual(5);

			// empty
			var vEmpty = new ShiftedIndicatorValue(ind, t);
			var arrEmpty = vEmpty.ToValues().ToArray();
			var vEmpty2 = new ShiftedIndicatorValue(ind, t);
			vEmpty2.FromValues(arrEmpty);
			vEmpty2.IsEmpty.AssertTrue();
		}
	}

	[TestMethod]
	public void PercentagePriceOscillatorCompositesUsePercentMeasure()
	{
		new PercentagePriceOscillatorSignal().Measure.AssertEqual(IndicatorMeasures.Percent);
		new PercentagePriceOscillatorHistogram().Measure.AssertEqual(IndicatorMeasures.Percent);
	}

	/// <summary>
	/// Extensions over an indicator belong in one place. A second helper beside the first is
	/// invisible at the call site - extension syntax never names the class - so it is only found by
	/// looking, which is how two of them came to exist in the same namespace.
	/// </summary>
	[TestMethod]
	public void IndicatorExtensionsLiveInOneClass()
	{
		Type[] contracts = [typeof(IIndicator), typeof(IIndicatorValue), typeof(IndicatorType)];

		static bool IsExtensionOver(MethodInfo m, Type[] contracts)
		{
			if (!m.IsDefined(typeof(ExtensionAttribute), false))
				return false;

			var first = m.GetParameters().FirstOrDefault()?.ParameterType;

			if (first is null)
				return false;

			var element = first.IsGenericType && first.GetGenericTypeDefinition() == typeof(IEnumerable<>)
				? first.GetGenericArguments()[0]
				: first;

			return contracts.Any(c => c.IsAssignableFrom(element));
		}

		// The namespace spans two assemblies - the contracts live below the implementations.
		var holders = new[] { typeof(IIndicator).Assembly, typeof(BaseIndicator).Assembly }
			.SelectMany(a => a.GetTypes())
			.Where(t => t.Namespace == typeof(BaseIndicator).Namespace && t.IsAbstract && t.IsSealed)
			.Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(m => IsExtensionOver(m, contracts)))
			.Select(t => t.Name)
			.OrderBy(n => n)
			.ToArray();

		holders.AssertEqual([nameof(IndicatorHelper)]);
	}

	[TestMethod]
	public void EveryOfferedIndicatorAnswersToItsOwnNameAndHasSeries()
	{
		var provider = new IndicatorProvider();
		provider.Init();

		var unreachable = new List<string>();

		foreach (var offered in provider.All)
		{
			var name = offered.Indicator.Name;
			var type = IndicatorProvider.TryFind(name);

			if (type != offered.Indicator)
			{
				unreachable.Add($"{name} -> {type?.Name ?? "nothing"}");
				continue;
			}

			// A plan with no series is one nobody can read.
			if (type.CreateInstance<IIndicator>().GetOutputs().Count == 0)
				unreachable.Add($"{name}: no series");
		}

		unreachable.Count.AssertEqual(0, unreachable.Take(25).JoinN());
	}

	[TestMethod]
	public void AHiddenPartAnswersToNoName()
	{
		// The signal line exists to sit inside the relative vigor index, and the provider does not
		// offer it. Resolving it by name would hand out an indicator the catalog never listed.
		IndicatorProvider.TryFind(nameof(RelativeVigorIndexSignal)).AssertNull();
		IndicatorProvider.TryFind("NoSuchIndicatorAnywhere").AssertNull();
	}

	[TestMethod]
	public void SeriesAreNamedAfterTheCodeAndNotTheDisplayText()
	{
		// An inner indicator's Name is its localized caption. Using it as a series key would make
		// the name depend on the language the process happens to be running in; these are the C#
		// property names, which are the same everywhere.
		new BollingerBands().GetOutputs().Select(o => o.Name).JoinComma().AssertEqual("MovingAverage,UpBand,LowBand");
		new MovingAverageConvergenceDivergenceHistogram().GetOutputs().Select(o => o.Name).JoinComma().AssertEqual("Macd,SignalMa");
	}

	[TestMethod]
	public void ACompositeReportsThePartsNestedInsideIt()
	{
		// The directional index sits inside the average directional index, and DI+ and DI- sit
		// inside that. Reading only the first level returns two series instead of three and drops
		// both directional lines without saying so.
		new AverageDirectionalIndex().GetOutputs().Select(o => o.Name).JoinComma().AssertEqual("Dx.Plus,Dx.Minus,MovingAverage");
	}

	[TestMethod]
	public void AParameterIsWhatTheIndicatorDeclaresItself()
	{
		var parameters = typeof(SimpleMovingAverage).GetParameters().Select(p => p.Name).ToArray();

		parameters.Contains(nameof(SimpleMovingAverage.Length)).AssertTrue();

		// Inherited identity and drawing hints are not something a caller may set.
		parameters.Contains(nameof(IIndicator.Name)).AssertFalse();
		parameters.Contains(nameof(IIndicator.Source)).AssertFalse();

		var sma = new SimpleMovingAverage();
		sma.ApplyParameters(new Dictionary<string, object> { ["length"] = 7, ["Name"] = "renamed", ["nosuch"] = 1 });

		sma.Length.AssertEqual(7);
		sma.Name.AssertNotEqual("renamed");
	}

	[TestMethod]
	public void APartOfACompositeIsTunedByNamingThePathToIt()
	{
		// The stochastic oscillator holds its periods on the parts, and exposes those parts through
		// get-only properties. Without a path there is no way to reach them at all.
		var stoch = new StochasticOscillator();
		stoch.ApplyParameters(new Dictionary<string, object> { ["K.Length"] = 21, ["d.length"] = 5 });

		stoch.K.Length.AssertEqual(21);
		stoch.D.Length.AssertEqual(5);
	}

	[TestMethod]
	public void AParameterThatArrivesAsJsonStillLands()
	{
		// System.Text.Json hands values over as JsonElement, which is not IConvertible: converting
		// it directly throws, and a swallowed failure leaves every indicator on its default - two
		// moving averages of different periods drawing the same line.
		using var json = JsonDocument.Parse("{\"length\":5}");

		var sma = new SimpleMovingAverage();
		sma.ApplyParameters(new Dictionary<string, object> { ["length"] = json.RootElement.GetProperty("length") });

		sma.Length.AssertEqual(5);
	}

	[TestMethod]
	public void ARefusedValueDoesNotUndoWhatCameBeforeIt()
	{
		// The parameters are applied one by one, so a bad one arrives after good ones have already
		// landed. Length 0 is refused by the indicator (a period is at least 1), and refusing it must
		// cost nothing else: %K keeps the 21 it was given, %D keeps the 3 its constructor chose.
		var stoch = new StochasticOscillator();
		stoch.ApplyParameters([new("K.Length", (object)21), new("D.Length", (object)0)]);

		stoch.K.Length.AssertEqual(21);
		stoch.D.Length.AssertEqual(3);
	}

	[TestMethod]
	public void ARefusedValueLeavesTheOneAlreadyAcceptedForThatProperty()
	{
		// Same property twice: the second value is out of range, so it is not applied - which leaves
		// the accepted 7 standing. Rolling back to the indicator's own default of 32 would silently
		// hand back a different moving average than the caller was told took effect.
		var sma = new SimpleMovingAverage();
		sma.ApplyParameters([new("Length", (object)7), new("Length", (object)(-1))]);

		sma.Length.AssertEqual(7);
	}

	[TestMethod]
	public void APathThroughAPartThatDoesNotExistIsSkippedWholesale()
	{
		// Nothing named NoSuchPart sits inside the stochastic oscillator, so there is no property to
		// set at the end of that path. The request is dropped, not applied to the root indicator and
		// not allowed to abandon the rest of the batch.
		var stoch = new StochasticOscillator();
		stoch.ApplyParameters([new("NoSuchPart.Length", (object)99), new("K.Length", (object)21)]);

		stoch.K.Length.AssertEqual(21);
		stoch.D.Length.AssertEqual(3);
	}

	[TestMethod]
	public void AValueThatIsNotANumberAtAllLeavesTheDefault()
	{
		// Text that no conversion can turn into a period must not reach Length. The indicator stays
		// on the 32 its constructor chose rather than on some coerced value.
		var sma = new SimpleMovingAverage();
		sma.ApplyParameters(new Dictionary<string, object> { ["Length"] = "not a number" });

		sma.Length.AssertEqual(32);
	}

	[TestMethod]
	public void EverySeriesOfACompositeCarriesAValue()
	{
		var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		foreach (var indicator in new IIndicator[] { new SimpleMovingAverage(), new BollingerBands(), new AverageDirectionalIndex(), new MovingAverageConvergenceDivergenceHistogram() })
		{
			var outputs = indicator.GetOutputs();
			var parts = outputs.Select(o => o.Part).ToArray();
			decimal?[] values = null;

			for (var i = 0; i < 200; i++)
			{
				// A wave rather than a ramp: the directional index needs moves in both directions
				// before either of its lines forms.
				var price = 100m + (decimal)Math.Sin(i / 5.0) * 10m;

				var candle = new TimeFrameCandleMessage
				{
					OpenTime = start.AddMinutes(i),
					CloseTime = start.AddMinutes(i + 1),
					OpenPrice = price,
					HighPrice = price + 1m,
					LowPrice = price - 1m,
					ClosePrice = price,
					TotalVolume = 100m,
					State = CandleStates.Finished,
				};

				values = indicator.Process(candle).GetOutputValues(parts).Values;
			}

			values.Length.AssertEqual(outputs.Count, indicator.GetType().Name);

			for (var i = 0; i < values.Length; i++)
				values[i].AssertNotNull($"{indicator.GetType().Name}.{outputs[i].Name}");
		}
	}

	[TestMethod]
	public void AShortAliasCoversEveryPartOfItsFamily()
	{
		// A short alias is a subclass that renames its base, so a caller can ask for "PPO" instead of
		// spelling the type out. Line, signal and histogram are separate indicators with different
		// output counts, so naming only some of them leaves the short vocabulary pointing at a member
		// the caller did not mean - and the answer comes back with fewer series rather than refused.
		var indicators = typeof(BaseIndicator)
			.Assembly
			.GetTypes()
			.Where(t => !t.IsAbstract && typeof(IIndicator).IsAssignableFrom(t))
			.ToArray();

		var concrete = indicators.ToHashSet();

		var aliasedBases = indicators
			.Where(t => t.GetAttribute<BrowsableAttribute>()?.Browsable == false && t.BaseType is not null && concrete.Contains(t.BaseType))
			.Select(t => t.BaseType)
			.ToHashSet();

		var unaliased = new List<string>();

		foreach (var type in aliasedBases)
		{
			foreach (var suffix in new[] { "Signal", "Histogram" })
			{
				var part = indicators.FirstOrDefault(t => t.Name == type.Name + suffix);

				// A hidden part is not addressable by name at all, so it needs no short name either.
				if (part is null || part.GetAttribute<IndicatorHiddenAttribute>() is not null)
					continue;

				if (!aliasedBases.Contains(part))
					unaliased.Add(part.Name);
			}
		}

		unaliased.Count.AssertEqual(0, $"aliased families whose parts have no alias: {unaliased.JoinCommaSpace()}");
	}
}
