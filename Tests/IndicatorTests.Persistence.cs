namespace StockSharp.Tests;

using System.ComponentModel.DataAnnotations;

using Ecng.Reflection;

/// <summary>
/// Saving and loading: settings round-trip, value round-trip and preloading a container.
/// </summary>
partial class IndicatorTests
{
	private static readonly HashSet<string> _ignoreProps =
	[
		nameof(IIndicator.Name),
		nameof(IIndicator.Container),
		nameof(IIndicator.Source),
		nameof(IComplexIndicator.InnerIndicators),
	];

	private static IEnumerable<PropertyInfo> GetProps(Type type)
		=> [..
		type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => !_ignoreProps.Contains(p.Name))
			.Where(p => p.IsBrowsable())
		];

	private static void SetRandom(IIndicator indicator, Action check)
	{
		if (indicator is AwesomeOscillator ao)
		{
			ao.ShortMa.Length = RandomGen.GetInt(5, 20);
			ao.LongMa.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is OscillatorOfMovingAverage oma)
		{
			oma.ShortPeriod = RandomGen.GetInt(5, 20);
			oma.LongPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is KasePeakOscillator kpo)
		{
			kpo.ShortPeriod = RandomGen.GetInt(5, 20);
			kpo.LongPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageRibbon mar)
		{
			mar.ShortPeriod = RandomGen.GetInt(5, 20);
			mar.LongPeriod = RandomGen.GetInt(20, 50);
			mar.RibbonCount = RandomGen.GetInt(2, 10);
			check();
		}
		else if (indicator is RangeActionVerificationIndex ravi)
		{
			ravi.ShortSma.Length = RandomGen.GetInt(5, 20);
			ravi.LongSma.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is KaufmanAdaptiveMovingAverage kama)
		{
			kama.FastSCPeriod = RandomGen.GetInt(5, 20);
			kama.SlowSCPeriod = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is Ichimoku i)
		{
			i.Tenkan.Length = RandomGen.GetInt(5, 10);
			i.Kijun.Length = RandomGen.GetInt(10, 20);
			i.SenkouB.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageConvergenceDivergence macd)
		{
			macd.ShortMa.Length = RandomGen.GetInt(5, 20);
			macd.LongMa.Length = RandomGen.GetInt(20, 50);
			check();
		}
		else if (indicator is MovingAverageConvergenceDivergenceHistogram hist)
		{
			hist.Macd.ShortMa.Length = RandomGen.GetInt(5, 20);
			hist.Macd.LongMa.Length = RandomGen.GetInt(20, 50);
			hist.SignalMa.Length = RandomGen.GetInt(5, 20);
			check();
		}
		else if (indicator is RainbowCharts rc)
		{
			rc.Lines = RandomGen.GetInt(5, 20);
			check();
		}
		else
			SetRandomPropsRecursive(indicator, check);
	}

	private static void SetRandomPropsRecursive(IIndicator indicator, Action check)
	{
		ArgumentNullException.ThrowIfNull(indicator);

		var type = indicator.GetType();
		var props = GetProps(type);

		foreach (var prop in props)
		{
			var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

			if (propType.Is<IIndicator>())
			{
				var nested = (IIndicator)prop.GetValue(indicator);

				if (nested is not null)
				{
					SetRandom(nested, check);
				}
			}
			else
			{
				if (!prop.IsModifiable())
					continue;

				object value;

				if (indicator is Fractals f && prop.Name == nameof(f.Length))
				{
					f.Length = 39;
					continue;
				}
				else
				{
					var rangeAttr = prop.GetAttribute<RangeAttribute>();

					if (rangeAttr is not null)
					{
						var minObj = rangeAttr.Minimum;
						var maxObj = rangeAttr.Maximum;

						// convert to target type
						var min = minObj.To(propType);
						var max = maxObj.To(propType);

						// choose random within [min; max]
						if (propType == typeof(int) || propType == typeof(short) || propType == typeof(sbyte) || propType == typeof(byte) || propType == typeof(ushort) || propType == typeof(uint))
						{
							var minI = min.To<int>();
							var maxI = max.To<int>();
							value = RandomGen.GetInt(minI, maxI).To(propType);
						}
						else if (propType == typeof(long))
						{
							var minL = min.To<long>();
							var maxL = max.To<long>();
							var rnd = RandomGen.GetDouble();
							var v = minL + (long)((maxL - minL) * rnd).Round();
							value = v;
						}
						else if (propType == typeof(double))
						{
							var minD = min.To<double>();
							var maxD = max.To<double>();
							value = minD + (maxD - minD) * RandomGen.GetDouble();
						}
						else if (propType == typeof(float))
						{
							var minF = min.To<float>();
							var maxF = max.To<float>();
							value = (float)(minF + (maxF - minF) * RandomGen.GetDouble());
						}
						else if (propType == typeof(decimal))
						{
							var minM = min.To<decimal>();
							var maxM = max.To<decimal>();
							value = minM + (decimal)RandomGen.GetDouble() * (maxM - minM);
						}
						else
						{
							// fallback to numeric conversion if possible
							if (propType.IsNumeric())
							{
								var minD = min.To<double>();
								var maxD = max.To<double>();
								var d = minD + (maxD - minD) * RandomGen.GetDouble();
								value = d.To(propType);
							}
							else
							{
								// if not numeric, skip
								continue;
							}
						}

						prop.SetValue(indicator, value);
						check();
						continue;
					}

					if (propType == typeof(int))
						value = RandomGen.GetInt(10, 100);
					else if (propType == typeof(decimal))
						value = (decimal)RandomGen.GetInt(1, 100) / 10;
					else if (propType == typeof(bool))
						value = RandomGen.GetBool();
					else if (propType == typeof(string))
						value = RandomGen.GetString(5, 10);
					else if (propType.IsEnum)
						value = RandomGen.GetEnum(propType);
					else if (propType == typeof(Unit))
						value = new Unit { Value = RandomGen.GetInt(1, 100), Type = RandomGen.GetEnum<UnitTypes>() };
					else if (propType.IsNumeric())
						value = RandomGen.GetInt(1, 100).To(propType);
					else
						continue;
				}

				prop.SetValue(indicator, value);
				
				check();
			}
		}
	}

	[TestMethod]
	public void SaveLoad()
	{
		void ComparePropsRecursive(IIndicator obj1, IIndicator obj2)
		{
			ArgumentNullException.ThrowIfNull(obj1);
			ArgumentNullException.ThrowIfNull(obj2);

			var props = GetProps(obj1.GetType());

			foreach (var prop in props)
			{
				var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

				if (propType.Is<IIndicator>())
				{
					var nested1 = (IIndicator)prop.GetValue(obj1);
					var nested2 = (IIndicator)prop.GetValue(obj2);

					// Guard on the nested values actually being compared (obj1/obj2 were a
					// copy-paste and are already non-null via ThrowIfNull above, so that check
					// was always true). Both nested indicators must be present or both absent;
					// a one-sided null means save/load dropped a nested indicator.
					(nested1 is null).AssertEqual(nested2 is null, prop.Name);

					if (nested1 is not null && nested2 is not null)
						ComparePropsRecursive(nested1, nested2);
				}
				else
				{
					var v1 = prop.GetValue(obj1);
					var v2 = prop.GetValue(obj2);
					v1.AssertEqual(v2);
				}
			}
		}

		foreach (var type in GetIndicatorTypes())
		{
			for (var i = 0; i < 100; i++)
			{
				var reseted = false;
				void OnReseted() => reseted = true;

				var indicator = type.CreateIndicator();
				indicator.Reseted += OnReseted;

				SetRandom(indicator, () =>
				{
					reseted.AssertTrue();
					reseted = false;
				});

				var storage = indicator.Save();

				var restoredIndicator = type.CreateIndicator();
				restoredIndicator.Load(storage);

				ComparePropsRecursive(indicator, restoredIndicator);
			}
		}
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task IndicatorValues_Roundtrip()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			var outputs = new List<IIndicatorValue>(candles.Length);

			// feed all candles
			foreach (var c in candles)
			{
				IIndicatorValue input = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator, c) { IsFinal = true };

				var outVal = indicator.Process(input);
				outputs.Add(outVal);
			}

			// One factory for the whole loop. CreateValue() only reads the indicator (it news up a value
			// and fills it from the supplied objects) and never advances its state, so a per-bar instance
			// bought nothing while costing an indicator construction per bar - display name lookup, a
			// Guid and every inner indicator, for each of ~265k values.
			var factory = type.CreateIndicator();

			// roundtrip each produced value
			for (var i = 0; i < outputs.Count; i++)
			{
				var original = outputs[i];

				var restored = factory.CreateValue(original.Time, [.. original.ToValues()]);

				// The restored value is produced by a freshly created (never formed) indicator,
				// so it inherits IsFormed == false (see BaseIndicatorValue.IsFormed = indicator.IsFormed).
				// CompareValue with checkExtended:false skips ALL asserts while !actual.IsFormed,
				// which turned the whole round-trip comparison into a no-op. Mirror the original
				// formed state onto the restored value so the numeric comparison actually runs and
				// the round-trip (ToValues -> CreateValue) is verified for formed values.
				restored.IsFormed = original.IsFormed;

				CompareValue(restored, original, factory.ToString(), false);
			}
		}
	}

	[TestMethod]
	public async Task Preload()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);
		var halfCount = candles.Length / 2;

		foreach (var type in GetIndicatorTypes())
		{
			var indicator1 = type.CreateIndicator();
			var indicator2 = type.CreateIndicator();

			var name = indicator1.ToString();

			var preloadData = new List<(IIndicatorValue input, IIndicatorValue output)>();

			// Process first half with indicator1 and collect data for preloading
			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];

				indicator1.Process(c);

				var (input, output) = indicator1.Container.GetValue(0);

				if (!input.IsFinal || !output.IsFinal)
					continue;

				var outputClone = indicator2.CreateValue(output.Time, [.. output.ToValues()]);
				outputClone.IsFinal = true;

				CompareValue(output, outputClone, name, true);

				preloadData.Add((input, outputClone));
			}

			// Preload indicator2 with collected data
			indicator2.Preload(preloadData);

			// Verify that indicator2 is in the same state as indicator1
			indicator1.IsFormed.AssertEqual(indicator2.IsFormed, name);
			indicator2.IsPreloaded.AssertTrue(name);

			indicator1.Reset();
			indicator1.IsFormed.AssertFalse(name);
			indicator1.IsPreloaded.AssertFalse(name);

			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];

				var output1 = indicator1.Process(c);
				var output2 = indicator2.Process(c);

				CompareValue(output2, output1, name, true);
			}

			{
				var c = candles[halfCount];
				var output1 = indicator1.Process(c);
				ThrowsExactly<NotSupportedException>(() => indicator2.Process(c));
			}
		}
	}

	[TestMethod]
	public async Task Preload_WithValues()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator1 = type.CreateIndicator();
			var indicator2 = type.CreateIndicator();

			var name = indicator1.ToString();

			var preloadData = new List<(DateTime, object[])>();

			// Process first half with indicator1 and collect output values
			var halfCount = candles.Length / 2;
			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];

				var output = indicator1.Process(c);
				preloadData.Add((output.Time, [.. output.ToValues()]));
			}

			// Preload indicator2 with collected values
			indicator2.Preload(preloadData);

			// Verify that indicator2 is in the same state as indicator1
			indicator1.IsFormed.AssertEqual(indicator2.IsFormed, name);
			indicator2.IsPreloaded.AssertTrue(name);

			indicator1.Reset();
			indicator1.IsFormed.AssertFalse(name);
			indicator1.IsPreloaded.AssertFalse(name);

			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];

				var output1 = indicator1.Process(c);
				var output2 = indicator2.Process(c);

				CompareValue(output2, output1, name, true);
			}
		}
	}

	[TestMethod]
	public void Preload_AlreadyPreloaded()
	{
		var type = GetIndicatorTypes().First();
		var indicator = type.CreateIndicator();

		var preloadData = new List<(DateTime time, object[] values)>
		{
			(DateTime.UtcNow, new object[] { 100m })
		};

		indicator.Preload(preloadData);
		indicator.IsPreloaded.AssertTrue();

		try
		{
			indicator.Preload(preloadData);
			Fail("Expected InvalidOperationException");
		}
		catch (InvalidOperationException ex)
		{
			ex.Message.AssertEqual("Indicator is already preloaded.");
		}
	}
}
