namespace StockSharp.Tests;

using System.ComponentModel.DataAnnotations;
using System.Text;

using Ecng.Reflection;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Gpu;
using StockSharp.Algo.Gpu.Indicators;

[TestClass]
public class IndicatorTests
{
	private static IIndicatorValue CreateValue(IndicatorType type, IIndicator indicator, SecurityId secId, DateTimeOffset now, int idx, TimeSpan tf, bool isFinal, bool isEmpty, int diffLimit = 10)
	{
		var time = now + tf.Multiply(idx);

		int getRnd()
			=> diffLimit > 0 ? RandomGen.GetInt(1, diffLimit) : RandomGen.GetInt(diffLimit, 0);

		ICandleMessage createCandle()
		{
			var candle = new TimeFrameCandleMessage
			{
				OpenPrice = (100 + getRnd()).Max(1),
				HighPrice = (101 + getRnd()).Max(1),
				LowPrice = (99 - getRnd()).Max(1),
				ClosePrice = (100.5m + getRnd()).Max(1),
				OpenTime = time,
				CloseTime = time + tf,
				TotalVolume = RandomGen.GetInt(1, 1000),
				SecurityId = secId,
				TypedArg = tf,
				State = CandleStates.Finished,
			};

			if (candle.HighPrice < candle.OpenPrice)
				(candle.OpenPrice, candle.HighPrice) = (candle.HighPrice, candle.OpenPrice);

			if (candle.HighPrice < candle.ClosePrice)
				(candle.ClosePrice, candle.HighPrice) = (candle.HighPrice, candle.ClosePrice);

			return candle;
		}

		var input = type.InputValue;

		if (input == typeof(DecimalIndicatorValue))
			return isEmpty ? new DecimalIndicatorValue(indicator, time) : new DecimalIndicatorValue(indicator, (100 + getRnd()).Max(1), time) { IsFinal = isFinal };
		else if (input == typeof(CandleIndicatorValue))
			return isEmpty ? new CandleIndicatorValue(indicator, time) : new CandleIndicatorValue(indicator, createCandle()) { IsFinal = isFinal };
		else
			throw new InvalidOperationException(input.ToString());
	}

	private static TimeFrameCandleMessage[] LoadCandles(SecurityId secId, DateTimeOffset time, TimeSpan tf)
	{
		var path = Path.Combine(Helper.ResFolder, "ohlcv.txt");
		using var reader = new StreamReader(path, Encoding.UTF8);
		var csv = new FastCsvReader(reader, Environment.NewLine) { ColumnSeparator = ',' };

		var list = new List<TimeFrameCandleMessage>();
		var t = time;

		while (csv.NextLine())
		{
			var open = csv.ReadDecimal();
			var high = csv.ReadDecimal();
			var low = csv.ReadDecimal();
			var close = csv.ReadDecimal();
			var volume = csv.ReadDecimal();

			list.Add(new()
			{
				TypedArg = tf,
				SecurityId = secId,
				OpenTime = t,
				CloseTime = t + tf,
				OpenPrice = open,
				HighPrice = high,
				LowPrice = low,
				ClosePrice = close,
				TotalVolume = volume,
				State = CandleStates.Finished,
			});

			t += tf;
		}

		return [.. list];
	}

	private static void CompareValue(IIndicatorValue actual, IIndicatorValue expected, string indName, bool checkExtended)
	{
		if (checkExtended)
			actual.IsFinal.AssertEqual(expected.IsFinal, indName);

		if (!actual.IsFormed)
		{
			if (checkExtended)
				expected.IsFormed.AssertFalse(indName);
		}
		else
		{
			static void compare(IEnumerable<object> a, IEnumerable<object> e, string indName)
			{
				var aArr = a.ToArray();
				var eArr = e.ToArray();

				aArr.Length.AssertEqual(eArr.Length);

				for (var i = 0; i < aArr.Length; i++)
				{
					var av = aArr[i];
					var ev = eArr[i];

					if (av is IEnumerable<object> ae)
						compare(ae, (IEnumerable<object>)ev, indName);
					else if (av is bool b1)
						b1.AssertEqual((bool)ev, indName);
					else if (av is int i1)
						i1.AssertEqual((int)ev, indName);
					else
						(((decimal)av - (decimal)ev) < 0.001m).AssertTrue(indName);
				}
			}

			compare(actual.ToValues(), expected.ToValues(), indName);
		}
	}

	private static void CompareValues(IIndicatorValue[] actual, IIndicatorValue[] expected, string indName, bool checkExtended)
	{
		ArgumentNullException.ThrowIfNull(actual);
		ArgumentNullException.ThrowIfNull(expected);

		actual.Length.AssertEqual(expected.Length);

		for (var i = 0; i < expected.Length; i++)
			CompareValue(actual[i], expected[i], indName, checkExtended);
	}

	private static IEnumerable<IndicatorType> GetIndicatorTypes()
	{
		IIndicatorProvider provider = new IndicatorProvider();
		provider.Init();
		return provider.All.Where(t => t.Indicator != typeof(CandlePatternIndicator));
	}

	[TestMethod]
	public void StateNonFinalInput()
	{
		var now = DateTimeOffset.UtcNow;
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
					Assert.Fail();
				else if (a.GetType() != b.GetType())
					Assert.Fail();
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
						Assert.Fail();

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
		var now = DateTimeOffset.UtcNow;
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
				indicator.NumValuesToInitialize.AssertGreater(0);
				indicator.IsFormed.AssertFalse();

				var finalCount = 0;
				var i = 0;

				while (!indicator.IsFormed)
				{
					var isFinal = RandomGen.GetBool();

					if (isFinal)
						finalCount++;

					var value = CreateValue(type, indicator, secId, now, i, tf, isFinal, false);
					indicator.Process(value).ValidateValue();

					finalCount.AssertLess(1000);

					i++;
				}

				finalCount.AssertEqual(indicator.NumValuesToInitialize, indicator.ToString());

				for (var n = 0; n < 100; n++)
				{
					var value = CreateValue(type, indicator, secId, now, i + n, tf, RandomGen.GetBool(), false);
					indicator.Process(value).ValidateValue();

					indicator.IsFormed.AssertTrue();
				}

				// test 5 times to ensure the same final count
				for (var j = 0; j < 5; j++)
				{
					// Reset
					indicator.Reset();
					indicator.IsFormed.AssertFalse();

					indicator.NumValuesToInitialize.AssertEqual(finalCount);

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

					finalCount.AssertEqual(finalCount2);
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
		var now = DateTimeOffset.UtcNow;
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
		};

		foreach (var type in GetIndicatorTypes().Where(t => !skipTypes.Contains(t.Indicator)))
		{
			var indicator = type.CreateIndicator();

			IIndicatorValue lastFinal = null;

			var i = 0;
			var extra = 10;

			while (!indicator.IsFormed || extra > 0)
			{
				var value = CreateValue(type, indicator, secId, now, i++, tf, true, false);
				lastFinal = indicator.Process(value);
				lastFinal.ValidateValue();

				if (indicator.IsFormed)
					extra--;
			}

			var wasChanged = false;

			for (int k = 0; k < 200; k++)
			{
				var nonFinalValue = CreateValue(type, indicator, secId, now, i + k * 1000, tf, false, false, (RandomGen.GetBool() ? -1 : 1) * k * 10);
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
							var minI = Convert.ToInt32(min);
							var maxI = Convert.ToInt32(max);
							value = RandomGen.GetInt(minI, maxI).To(propType);
						}
						else if (propType == typeof(long))
						{
							var minL = Convert.ToInt64(min);
							var maxL = Convert.ToInt64(max);
							var rnd = RandomGen.GetDouble();
							var v = minL + (long)Math.Round((maxL - minL) * rnd);
							value = v;
						}
						else if (propType == typeof(double))
						{
							var minD = Convert.ToDouble(min);
							var maxD = Convert.ToDouble(max);
							value = minD + (maxD - minD) * RandomGen.GetDouble();
						}
						else if (propType == typeof(float))
						{
							var minF = Convert.ToSingle(min);
							var maxF = Convert.ToSingle(max);
							value = (float)(minF + (maxF - minF) * RandomGen.GetDouble());
						}
						else if (propType == typeof(decimal))
						{
							var minM = Convert.ToDecimal(min);
							var maxM = Convert.ToDecimal(max);
							value = minM + (decimal)RandomGen.GetDouble() * (maxM - minM);
						}
						else
						{
							// fallback to numeric conversion if possible
							if (propType.IsNumeric())
							{
								var minD = Convert.ToDouble(min);
								var maxD = Convert.ToDouble(max);
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

					if (obj1 is not null && obj2 is not null)
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
	public void Process()
	{
		var time = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = LoadCandles(secId, time, tf);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			var inputType = type.InputValue;

			if (inputType == typeof(DecimalIndicatorValue))
				indicator.Check(candles, data => data.ClosePrice);
			else if (inputType == typeof(CandleIndicatorValue))
				indicator.Check(candles, data => data);
			else
				throw new InvalidOperationException(inputType.To<string>());
		}
	}

	[TestMethod]
	public void DocUrlUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.DocUrl)
			.Where(url => !url.IsEmpty())
			.Select(url => url.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Assert.Fail($"Duplicate DocUrl(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void NameUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.Name)
			.Select(n => n.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Assert.Fail($"Duplicate Names(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void DescriptionUnique()
	{
		var duplicates = GetIndicatorTypes()
			.Select(t => t.Description)
			.Where(n => !n.IsEmpty())
			.Select(n => n.ToLowerInvariant())
			.GroupBy(x => x)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToArray();

		if (duplicates.Any())
			Assert.Fail($"Duplicate Descriptions(s) found: {duplicates.JoinCommaSpace()}");
	}

	[TestMethod]
	public void RequiredAttributes()
	{
		foreach (var type in GetIndicatorTypes())
		{
			var indicatorType = type.Indicator;

			// Check [IndicatorIn]
			var inAttr = indicatorType.GetAttribute<IndicatorInAttribute>();
			inAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [IndicatorIn] attribute.");

			// Check [IndicatorOut]
			var outAttr = indicatorType.GetAttribute<IndicatorOutAttribute>();
			outAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [IndicatorOut] attribute.");

			// Check [Doc]
			var docAttr = indicatorType.GetAttribute<DocAttribute>();
			docAttr.AssertNotNull($"Indicator {indicatorType.Name} missing [Doc] attribute.");
		}
	}

	[TestMethod]
	public void GpuIndicators()
	{
		static ICandleMessage[][] loadCandles()
		{
			var start = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var baseTf = TimeSpan.FromMinutes(1);
			var secId = Helper.CreateSecurityId();

			// 1m base candles from storage
			var baseCandles = LoadCandles(secId, start, baseTf)
				.Cast<ICandleMessage>()
				.ToArray();

			var result = new List<ICandleMessage[]> { baseCandles };

			// Build bigger TF series from 1m via compressor
			var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
			var builder = provider.Get(typeof(TimeFrameCandleMessage));
			var biggerTfs = new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1) };

			foreach (var tf in biggerTfs)
			{
				var subBig = new Subscription(tf.TimeFrame(), new SecurityMessage { SecurityId = secId });
				var mdBig = subBig.MarketData;
				mdBig.IsFinishedOnly = false;
				mdBig.AllowBuildFromSmallerTimeFrame = true;

				var compressor = new BiggerTimeFrameCandleCompressor(mdBig, builder, baseTf.TimeFrame());
				var list = new List<ICandleMessage>();

				foreach (var c in baseCandles)
				{
					var messages = compressor.Process((CandleMessage)c);

					foreach (var m in messages)
					{
						if (m is TimeFrameCandleMessage tfMsg && tfMsg.State == CandleStates.Finished)
							list.Add(tfMsg);
					}
				}

				result.Add([.. list]);
			}

			return [.. result];
		}

		static IIndicatorValue[] runCpu(IIndicator indicator, ICandleMessage[] candles)
		{
			var res = new IIndicatorValue[candles.Length];

			for (var i = 0; i < candles.Length; i++)
				res[i] = indicator.Process(candles[i]);

			return res;
		}

		static IGpuIndicatorParams[] randomIndicators(IIndicator[] indicators, Type paramType)
		{
			var parameters = new IGpuIndicatorParams[indicators.Length];

			for (var i = 0; i < indicators.Length; i++)
			{
				var prm = paramType.CreateInstance<IGpuIndicatorParams>();
				prm.FromIndicator(indicators[i]);

				parameters[i] = prm;
			}

			return parameters;
		}

		var msgSeries = loadCandles(); // multiple TF series
		var gpuSeries = msgSeries
			.Select(series => series.Select(c => new GpuCandle(c.OpenTime, c.OpenPrice, c.HighPrice, c.LowPrice, c.ClosePrice, c.TotalVolume)).ToArray())
			.ToArray();

		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		var (ctx, acc) = GpuAcceleratorFactory.CreateBestAccelerator();

		using (ctx)
		using (acc)
		{
			foreach (var (indicatorType, calculatorType) in provider.All)
			{
				var calculator = provider.Create(ctx, acc, calculatorType);
				calculator.AssertNotNull();

				// build N parameter variations from randomized indicators
				const int variations = 10;
				var indicators = new IIndicator[variations];

				for (var i = 0; i < indicators.Length; i++)
				{
					var indicator = indicatorType.CreateInstance<IIndicator>();
					// Randomize indicator settings using existing helper
					SetRandom(indicator, () => { });

					indicators[i] = indicator;
				}

				var parameters = randomIndicators(indicators, calculator.ParameterType);

				// calculate via interface for all TF series and all params
				var gpuAll = calculator.Calculate(gpuSeries, parameters); // [series][param][bar]

				for (var s = 0; s < msgSeries.Length; s++)
				{
					for (var p = 0; p < indicators.Length; p++)
					{
						var gpuOut = gpuAll[s][p];

						// fresh indicator instance for CPU with same settings
						var indCpu = indicators[p].TypedClone();
						var cpu = runCpu(indCpu, msgSeries[s]);

						CompareValues(gpuOut.Select(r => r.ToValue(indCpu)).ToArray(), cpu, indCpu.ToString(), true);
					}
				}
			}
		}
	}

	[TestMethod]
	public void GpuProviderInit()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		// Must discover at least the built-in GPU calculators
		provider.All.ContainsKey(typeof(SimpleMovingAverage)).AssertTrue("SMA calculator not discovered");
		provider.All.ContainsKey(typeof(AverageDirectionalIndex)).AssertTrue("ADX calculator not discovered");
	}

	[TestMethod]
	public void GpuProviderTryGet()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		var indType = typeof(SimpleMovingAverage);

		provider.TryGetCalculatorType(indType, out var calcType).AssertTrue();
		calcType.Is<IGpuIndicatorCalculator>().AssertTrue();

		var (ctx, acc) = GpuAcceleratorFactory.CreateBestAccelerator();
		using (ctx)
		using (acc)
		{
			var calc = provider.Create(ctx, acc, calcType);
			calc.AssertNotNull();
			calc.IndicatorType.AssertEqual(indType);
		}
	}

	[TestMethod]
	public void GpuProviderRegisterUnregister()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		var unkIndicator = typeof(CandlePatternIndicator); // Assume no built-in GPU calculator for this indicator
		var unkCalcType = typeof(GpuSmaCalculator);

		// Unknown indicator should not exist initially
		provider.TryGetCalculatorType(unkIndicator, out _).AssertFalse();

		// Register a mapping (for test purposes, map Acceleration -> GpuSmaCalculator)
		provider.Register(unkIndicator, unkCalcType);
		provider.TryGetCalculatorType(unkIndicator, out var calcType).AssertTrue();
		calcType.AssertEqual(unkCalcType);

		// Create should return a calculator instance
		var (ctx, acc) = GpuAcceleratorFactory.CreateBestAccelerator();
		using (ctx)
		using (acc)
		{
			var calc = provider.Create(ctx, acc, unkCalcType);
			calc.AssertNotNull();
		}

		// Unregister
		provider.Unregister(unkIndicator).AssertTrue();
		provider.TryGetCalculatorType(unkIndicator, out _).AssertFalse();

		// Clear
		provider.Clear();
		provider.All.Count.AssertEqual(0);
	}

	[TestMethod]
	public void IndicatorValues_Roundtrip()
	{
		var time = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = LoadCandles(secId, time, tf);

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

			// roundtrip each produced value
			for (var i = 0; i < outputs.Count; i++)
			{
				var original = outputs[i];
				
				var factory = type.CreateIndicator();

				var restored = factory.CreateValue(original.Time, [.. original.ToValues()]);

				CompareValue(restored, original, factory.ToString(), false);
			}
		}
	}

	[TestMethod]
	public void Preload()
	{
		var time = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = LoadCandles(secId, time, tf);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator1 = type.CreateIndicator();
			var indicator2 = type.CreateIndicator();

			var preloadData = new List<(IIndicatorValue input, IIndicatorValue output)>();

			// Process first half with indicator1 and collect data for preloading
			var halfCount = candles.Length / 2;
			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];
				IIndicatorValue input = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator1, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator1, c) { IsFinal = true };

				var output = indicator1.Process(input);
				preloadData.Add((input, output));
			}

			// Preload indicator2 with collected data
			indicator2.Preload(preloadData);

			// Verify that indicator2 is in the same state as indicator1
			indicator1.IsFormed.AssertEqual(indicator2.IsFormed, type.Name);
			indicator2.IsPreloaded.AssertTrue(type.Name);

			// Process second half with both indicators and compare results
			for (var i = halfCount; i < candles.Length; i++)
			{
				var c = candles[i];

				IIndicatorValue input1 = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator1, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator1, c) { IsFinal = true };

				IIndicatorValue input2 = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator2, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator2, c) { IsFinal = true };

				var output1 = indicator1.Process(input1);
				var output2 = indicator2.Process(input2);

				CompareValue(output2, output1, type.Name, true);
			}
		}
	}

	[TestMethod]
	public void Preload_WithValues()
	{
		var time = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var tf = TimeSpan.FromMinutes(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = LoadCandles(secId, time, tf);

		foreach (var type in GetIndicatorTypes())
		{
			var indicator1 = type.CreateIndicator();
			var indicator2 = type.CreateIndicator();

			var preloadData = new List<(DateTimeOffset time, object[] values)>();

			// Process first half with indicator1 and collect output values
			var halfCount = candles.Length / 2;
			for (var i = 0; i < halfCount; i++)
			{
				var c = candles[i];
				IIndicatorValue input = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator1, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator1, c) { IsFinal = true };

				var output = indicator1.Process(input);
				preloadData.Add((output.Time, [.. output.ToValues()]));
			}

			// Preload indicator2 with collected values
			indicator2.Preload(preloadData);

			// Verify that indicator2 is in the same state as indicator1
			indicator1.IsFormed.AssertEqual(indicator2.IsFormed, type.Name);
			indicator2.IsPreloaded.AssertTrue(type.Name);

			// Process second half with both indicators and compare results
			for (var i = halfCount; i < candles.Length; i++)
			{
				var c = candles[i];

				IIndicatorValue input1 = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator1, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator1, c) { IsFinal = true };

				IIndicatorValue input2 = type.InputValue == typeof(DecimalIndicatorValue)
					? new DecimalIndicatorValue(indicator2, c.ClosePrice, c.OpenTime) { IsFinal = true }
					: new CandleIndicatorValue(indicator2, c) { IsFinal = true };

				var output1 = indicator1.Process(input1);
				var output2 = indicator2.Process(input2);

				CompareValue(output2, output1, type.Name, true);
			}
		}
	}

	[TestMethod]
	public void Preload_AlreadyPreloaded()
	{
		var type = GetIndicatorTypes().First();
		var indicator = type.CreateIndicator();

		var preloadData = new List<(DateTimeOffset time, object[] values)>
		{
			(DateTimeOffset.UtcNow, new object[] { 100m })
		};

		indicator.Preload(preloadData);
		indicator.IsPreloaded.AssertTrue();

		try
		{
			indicator.Preload(preloadData);
			Assert.Fail("Expected InvalidOperationException");
		}
		catch (InvalidOperationException ex)
		{
			ex.Message.Contains("already preloaded").AssertTrue();
		}
	}
}

static class IndicatorDataRunner
{
	private class TestIndicatorValue<TInner> : IIndicatorValue
	{
		private readonly TInner _value;

		public TestIndicatorValue(IIndicator indicator, DateTimeOffset time, TInner value, TInner initFrom = default)
		{
			Indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
			_value = value is ICloneable cl ? (TInner)cl.Clone() : value;

			if (initFrom is ICandleMessage initCandle && _value is ICandleMessage candle)
			{
				candle.OpenTime = initCandle.OpenTime;
				candle.CloseTime = initCandle.CloseTime;
			}

			Time = time;
		}

		public IIndicator Indicator { get; }
		public bool IsFinal { get; set; }
		public DateTimeOffset Time { get; }
		bool IIndicatorValue.IsFormed { get; set; }
		bool IIndicatorValue.IsEmpty => false;

		T IIndicatorValue.GetValue<T>(Level1Fields? field)
		{
			if (_value is T t)
				return t;
			else if (typeof(T).Is<ICandleMessage>())
			{
				var dec = _value.To<decimal>();

				return new TimeFrameCandleMessage
				{
					OpenPrice = dec,
					HighPrice = dec,
					LowPrice = dec,
					ClosePrice = dec,
					OpenTime = DateTimeOffset.UtcNow,
				}.To<T>();
			}
			else if (typeof(T) == typeof(decimal))
			{
				var c = _value.To<ICandleMessage>();
				return c.ClosePrice.To<T>();
			}
			else
				throw new NotSupportedException();
		}

		int IComparable<IIndicatorValue>.CompareTo(IIndicatorValue other)
			=> throw new NotSupportedException();

		int IComparable.CompareTo(object obj)
			=> throw new NotSupportedException();

		IEnumerable<object> IIndicatorValue.ToValues()
			=> throw new NotSupportedException();

		void IIndicatorValue.FromValues(object[] values)
			=> throw new NotSupportedException();
	}

	private class IndicatorData
	{
		public int Line { get; init; }
		public CandleMessage Candle { get; init; }
		public decimal?[] Values { get; init; }
	}

	public static void Check<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(getValue);

		var values = new List<IndicatorData>();

		var epsilon = indicator.Measure switch
		{
			IndicatorMeasures.MinusOnePlusOne => 0.001m,
			IndicatorMeasures.Percent or IndicatorMeasures.Price or IndicatorMeasures.Volume => 0.1m,
			_ => throw new NotSupportedException(indicator.Measure.ToString()),
		};

		var data = Do.Invariant(() => File.ReadAllLines(Path.Combine(Helper.ResFolder, "IndicatorsData", $"{indicator.GetType().Name}.txt")).Select((line, idx) =>
		{
			var parts = line.SplitByComma();

			return new IndicatorData
			{
				Line = idx,
				Candle = candles[idx],
				Values = [.. parts.Select(p => p.To<decimal?>())],
			};
		}).ToArray());

		for (var i = 0; i < data.Length; i++)
		{
			values.Add(data[i]);

			var inputValues = new List<TestIndicatorValue<T>>
			{
				new(indicator, data[i].Candle.OpenTime, getValue(data[i].Candle)) { IsFinal = true }
			};

			var numNonFinals = RandomGen.GetInt(10);
			for (var j = 0; j < numNonFinals; ++j)
			{
				var i2 = Math.Max(0, Math.Min(data.Length - 1, i + RandomGen.GetInt(-5, 5)));
				inputValues.Add(new(indicator, data[i2].Candle.OpenTime, getValue(data[i2].Candle), i < data.Length - 1 ? getValue(data[i+1].Candle) : default) { IsFinal = false });
			}

			void CheckValue(IIndicatorValue value, int column)
			{
				if (!indicator.IsFormed)
					return;

				var data = values[values.Count - 1];

				if (value.IsEmpty)
				{
					//testValue.AssertNull();
				}
				else
				{
					var testValue = data.Values[column];

					testValue.AssertNotNull();

					var indValue = value.ToDecimal().Round(2);

					((testValue.Value - indValue).Abs() < epsilon).AssertTrue();
				}
			}

			foreach (var inputValue in inputValues)
			{
				var value = indicator.Process(inputValue);

				ValidateValue(value);

				if (!inputValue.IsFinal)
					continue;

				value
					.Plain()
					.Select((sv, idx) => (v: sv, column: idx))
					.ForEach(p => CheckValue(p.v, p.column));
			}
		}

		indicator.IsFormed.AssertTrue();
	}

	private static readonly SynchronizedDictionary<IndicatorMeasures, Range<decimal>> _validators = [];

	public static void ValidateValue(this IIndicatorValue value)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (value is IComplexIndicatorValue complex)
		{
			if (complex.InnerValues.Count > 0)
			{
				var allFinal = complex.InnerValues.Values.All(v => v.IsFinal);
				complex.IsFinal.AssertEqual(allFinal, $"IComplexIndicatorValue.IsFinal={complex.IsFinal}, but inner values: [{complex.InnerValues.Values.Select(v => v.IsFinal.ToString()).JoinCommaSpace()}]");
			}
		}

		value.Plain().ForEach(v =>
		{
			if (v.IsEmpty)
				return;

			var dec = v.ToDecimal();
			var range = _validators.SafeAdd(v.Indicator.Measure);
			range.Contains(dec).AssertTrue();
		});
	}

	public static IEnumerable<IIndicatorValue> Plain(this IIndicatorValue val)
	{
		if (val is not IComplexIndicatorValue civ)
		{
			yield return val;
		}
		else
		{
			foreach (var v in civ.InnerValues.SelectMany(kv => Plain(kv.Value)))
				yield return v;
		}
	}
}