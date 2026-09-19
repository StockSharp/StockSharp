namespace StockSharp.Tests;

/// <summary>
/// Indicator values: non-final input, composite values, formation count and decimal extremes.
/// </summary>
partial class IndicatorTests
{
	[TestMethod]
	public void StateNonFinalInput()
	{
		var now = DateTime.UtcNow;
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromDays(1);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			indicator.IsFormed.AssertFalse();

			static void stateEquals(object a, object b)
			{
				if (a == null && b == null)
					return;
				else if (a == null || b == null)
					Fail();
				else if (a.GetType() != b.GetType())
					Fail();
				else if (a is IIndicator indA && b is IIndicator indB)
				{
					foreach (var field in a.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						var va = field.GetValue(indA);
						var vb = field.GetValue(indB);

						stateEquals(va, vb);
					}

					return;
				}
				else if (a is System.Collections.IEnumerable ea && b is System.Collections.IEnumerable eb && a is not string)
				{
					var enumA = ea.Cast<object>().ToArray();
					var enumB = eb.Cast<object>().ToArray();

					if (enumA.Length != enumB.Length)
						Fail();

					for (var i = 0; i < enumA.Length; i++)
					{
						stateEquals(enumA[i], enumB[i]);
					}

					return;
				}

				a.AssertEqual(b);
			}

			var before = indicator.TypedClone();

			for (var i = 0; i < 100; i++)
			{
				var value = CreateValue(type, indicator, secId, now, i, tf, false, RandomGen.GetBool());

				indicator.Process(value).ValidateValue();
				indicator.IsFormed.AssertFalse();

				stateEquals(before, indicator);
			}
		}
	}

	[TestMethod]
	public void ComplexValues()
	{
		foreach (var type in GetIndicatorTypes())
			type.OutputValue.Is<IComplexIndicatorValue>().AssertEqual(type.IsComplex);
	}

	[TestMethod]
	public void NumValuesToInitialize()
	{
		var now = DateTime.UtcNow;
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromDays(1);

		foreach (var type in GetIndicatorTypes())
		{
			// non deterministic indicators
			if (type.Indicator == typeof(AdaptiveLaguerreFilter) ||
				type.Indicator == typeof(DemandIndex))
				continue;

			var indicator = type.CreateIndicator();

			var k = 0;

			do
			{
				indicator.NumValuesToInitialize.AssertGreater(0, indicator.ToString());
				indicator.IsFormed.AssertFalse(indicator.ToString());

				var finalCount = 0;
				var i = 0;

				while (!indicator.IsFormed)
				{
					var isFinal = RandomGen.GetBool();

					if (isFinal)
						finalCount++;

					var value = CreateValue(type, indicator, secId, now, i, tf, isFinal, false);
					indicator.Process(value).ValidateValue();

					finalCount.AssertLess(1000, indicator.ToString());

					i++;
				}

				finalCount.AssertEqual(indicator.NumValuesToInitialize, indicator.ToString());

				for (var n = 0; n < 100; n++)
				{
					var value = CreateValue(type, indicator, secId, now, i + n, tf, RandomGen.GetBool(), false);
					indicator.Process(value).ValidateValue();

					indicator.IsFormed.AssertTrue(indicator.ToString());
				}

				// test 5 times to ensure the same final count
				for (var j = 0; j < 5; j++)
				{
					// Reset
					indicator.Reset();
					indicator.IsFormed.AssertFalse(indicator.ToString());

					indicator.NumValuesToInitialize.AssertEqual(finalCount, indicator.ToString());

					var finalCount2 = 0;

					while (!indicator.IsFormed)
					{
						var isFinal = RandomGen.GetBool();

						if (isFinal)
							finalCount2++;

						var value = CreateValue(type, indicator, secId, now, i, tf, isFinal, false);
						indicator.Process(value).ValidateValue();

						i++;
					}

					finalCount.AssertEqual(finalCount2, indicator.ToString());
				}

				var isAnySet = false;
				SetRandom(indicator, () => isAnySet = true);

				if (!isAnySet)
					indicator.Reset();
			}
			while (++k < 5);
		}
	}

	[TestMethod]
	public void NonFinalValueChanges()
	{
		var now = DateTime.UtcNow;
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromDays(1);

		var invalid = new List<Type>();

		// hard to test
		var skipTypes = new List<Type>
		{
			typeof(VolumeProfileIndicator),
			typeof(Peak),
			typeof(Trough),
			typeof(ParabolicSar),
			typeof(Median),
			typeof(Fractals),

			// reads only the direction, so with the last two prices equal no preview moves it
			typeof(MarketMeannessIndex),
		};

		// Use a seeded RNG so the perturbation stream is reproducible and the test is deterministic
		// across workers; an unseeded global RNG could, by chance, never break an extreme.
		var rnd = new Random(12345);

		foreach (var type in GetIndicatorTypes().Where(t => !skipTypes.Contains(t.Indicator)))
		{
			var indicator = type.CreateIndicator();

			IIndicatorValue lastFinal = null;

			var i = 0;
			var extra = 10;

			while (!indicator.IsFormed || extra > 0)
			{
				var value = CreateValue(type, indicator, secId, now, i++, tf, true, false, rnd: rnd);
				lastFinal = indicator.Process(value);
				lastFinal.ValidateValue();

				if (indicator.IsFormed)
					extra--;
			}

			var wasChanged = false;

			for (int k = 0; k < 200; k++)
			{
				var nonFinalValue = CreateValue(type, indicator, secId, now, i + k * 1000, tf, false, false, (rnd.Next(2) == 0 ? -1 : 1) * k * 10, rnd);
				var nonFinalResult = indicator.Process(nonFinalValue);
				nonFinalResult.ValidateValue();

				if (!lastFinal.ToValues().SequenceEqual(nonFinalResult.ToValues()))
				{
					wasChanged = true;
					break;
				}
			}

			wasChanged.AssertTrue(indicator.ToString());
		}
	}

	[TestMethod]
	public void ExtremeDecimalProfiles()
	{
		const int maxWarmup = 4096;
		const decimal offsetBudget = 1_000_000_000_000m;

		var errors = new List<string>();
		var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var nonDeterministic = new HashSet<Type>
		{
			typeof(AdaptiveLaguerreFilter),
			typeof(DemandIndex),
		};

		static string errorText(Exception error)
			=> $"{error.GetType().Name}: {error.Message.ReplaceLineEndings(" ")}";

		static IIndicatorValue createInput(
			IndicatorType type,
			IIndicator indicator,
			SecurityId secId,
			DateTime time,
			TimeSpan tf,
			decimal value,
			int index,
			bool isFinal,
			bool isHighOffset)
		{
			if (type.InputValue == typeof(DecimalIndicatorValue))
				return new DecimalIndicatorValue(indicator, value, time) { IsFinal = isFinal };

			if (type.InputValue == typeof(CandleIndicatorValue))
			{
				decimal open;
				decimal close;
				decimal high;
				decimal low;
				decimal volume;

				if (isHighOffset)
				{
					open = value;
					close = value + index % 3 - 1;
					var spread = 2m + index % 3;
					high = open.Max(close) + spread;
					low = open.Min(close) - spread;
					volume = 1m + index % 3;
				}
				else if (value == 0m)
				{
					open = close = high = low = volume = 0m;
				}
				else
				{
					open = close = value;
					high = value + 1m;
					low = (value - 1m).Max(0m);
					volume = 1m;
				}

				var candle = new TimeFrameCandleMessage
				{
					SecurityId = secId,
					TypedArg = tf,
					OpenTime = time,
					CloseTime = time + tf,
					OpenPrice = open,
					HighPrice = high,
					LowPrice = low,
					ClosePrice = close,
					TotalVolume = volume,
					State = isFinal ? CandleStates.Finished : CandleStates.Active,
				};

				return new CandleIndicatorValue(indicator, candle) { IsFinal = isFinal };
			}

			throw new InvalidOperationException($"Unsupported input value {type.InputValue} for {type.Indicator}.");
		}

		foreach (var type in GetIndicatorTypes().OrderBy(t => t.Indicator.FullName, StringComparer.Ordinal))
		{
			var name = type.Indicator.FullName ?? type.Indicator.Name;

			if (type.InputValue != typeof(DecimalIndicatorValue) &&
				type.InputValue != typeof(CandleIndicatorValue))
			{
				errors.Add($"{name} | input | Unsupported input value {type.InputValue}.");
				continue;
			}

			foreach (var profile in new[] { "high-offset", "zero-recovery" })
			{
				var isHighOffset = profile == "high-offset";
				IIndicator actual;
				IIndicator control;

				try
				{
					actual = type.CreateIndicator();
					control = type.CreateIndicator();
				}
				catch (Exception error)
				{
					errors.Add($"{name} | {profile} | create | {errorText(error)}");
					continue;
				}

				var warmup = actual.NumValuesToInitialize.Max(control.NumValuesToInitialize).Max(1);

				if (warmup > maxWarmup)
				{
					errors.Add($"{name} | {profile} | warmup | {warmup} exceeds the {maxWarmup} value safety limit.");
					continue;
				}

				var finalCount = warmup + 3;
				var highOffset = offsetBudget / (warmup + 8m);

				bool process(IIndicator indicator, decimal value, int index, bool isFinal, string phase, out IIndicatorValue result)
				{
					try
					{
						var input = createInput(type, indicator, secId, now + tf.Multiply(index), tf, value, index, isFinal, isHighOffset);
						result = indicator.Process(input);
						result.ValidateValue();
						return true;
					}
					catch (Exception error)
					{
						result = null;
						errors.Add($"{name} | {profile} | {phase} | {errorText(error)}");
						return false;
					}
				}

				var initialized = true;

				for (var i = 0; i < finalCount; i++)
				{
					var value = isHighOffset ? highOffset + i % 5 - 2 : 0m;

					if (!process(actual, value, i, true, $"actual final {i}", out _) ||
						!process(control, value, i, true, $"control final {i}", out _))
					{
						initialized = false;
						break;
					}
				}

				if (!initialized)
					continue;

				var nextIndex = finalCount;
				var previewValue = isHighOffset ? highOffset + 11m : 1m;
				var finalValue = isHighOffset ? highOffset - 7m : 2m;

				if (!process(actual, previewValue, nextIndex, false, "preview", out _))
					continue;

				if (!process(actual, finalValue, nextIndex, true, "final after preview", out var actualFinal) ||
					!process(control, finalValue, nextIndex, true, "control final", out var controlFinal))
					continue;

				if (nonDeterministic.Contains(type.Indicator))
					continue;

				try
				{
					actual.IsFormed.AssertEqual(control.IsFormed, name);
					CompareValue(actualFinal, controlFinal, name, true);
				}
				catch (Exception error)
				{
					errors.Add($"{name} | {profile} | preview state | {errorText(error)}");
				}
			}
		}

		if (errors.Count > 0)
		{
			var report = errors
				.OrderBy(error => error, StringComparer.Ordinal)
				.JoinN();

			Fail($"Extreme indicator profiles failed ({errors.Count}):{Environment.NewLine}{report}");
		}
	}
}
