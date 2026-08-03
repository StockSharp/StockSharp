namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;

using Ecng.Reflection;

using ILGPU;
using ILGPU.Runtime;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Gpu;
using StockSharp.Algo.Gpu.Indicators;
using DataType = StockSharp.Messages.DataType;

[TestClass]
public class IndicatorTests : BaseTestClass
{
	// Creating an ILGPU context plus accelerator costs seconds (this box selects CUDA), and the object
	// carries no per-test state: it is the device, not a fixture. One is created for the whole class and
	// shared by every GPU test instead of one per test.
	private static Context _gpuContext;
	private static Accelerator _gpuAccelerator;

	// The class runs its methods in parallel, and ILGPU drives buffer allocation, kernel launches and
	// Synchronize() through the accelerator's default stream - which is not meant to be used from several
	// threads at once. Every stretch of code that touches the shared accelerator takes this lock.
	private static readonly Lock _gpuLock = new();

	/// <summary>
	/// Creates the shared device on first use. Doing it in <see cref="ClassInitializeAttribute"/> instead
	/// would fail all tests in the class - including the majority that never touch the GPU - on a box
	/// where no accelerator can be created.
	/// </summary>
	private static (Context, Accelerator) GetGpu()
	{
		using (_gpuLock.EnterScope())
		{
			if (_gpuAccelerator is null)
				(_gpuContext, _gpuAccelerator) = GpuAcceleratorFactory.CreateBestAccelerator();

			return (_gpuContext, _gpuAccelerator);
		}
	}

	[ClassCleanup]
	public static void ClassUnInit()
	{
		_gpuAccelerator?.Dispose();
		_gpuContext?.Dispose();

		_gpuAccelerator = null;
		_gpuContext = null;
	}

	private static IIndicatorValue CreateValue(IndicatorType type, IIndicator indicator, SecurityId secId, DateTime now, int idx, TimeSpan tf, bool isFinal, bool isEmpty, int diffLimit = 10, Random rnd = null)
	{
		var time = now + tf.Multiply(idx);

		// When a seeded Random is supplied the value stream becomes reproducible; otherwise fall
		// back to the global RandomGen. RandomGen.GetInt(min, max) has an inclusive upper bound,
		// so the local path adds 1 to match it exactly.
		int getRnd()
			=> rnd is null
				? (diffLimit > 0 ? RandomGen.GetInt(1, diffLimit) : RandomGen.GetInt(diffLimit, 0))
				: (diffLimit > 0 ? rnd.Next(1, diffLimit + 1) : rnd.Next(diffLimit, 1));

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
				// From the seeded stream when one is given, so the whole series is reproducible.
				// Volume was the one field still drawn from the global generator, which left the
				// indicators that read it - NVI moves only when volume falls - deciding by chance
				// whether a run had anything to react to.
				TotalVolume = rnd is null ? RandomGen.GetInt(1, 1000) : rnd.Next(1, 1000),
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

	// Seven tests in this class run over the same Resources/ohlcv.txt, so the file is read and parsed once
	// and the rows are then shared. Only the candle messages are rebuilt per call: they carry a per-test
	// security id, start time and time frame, and every test keeps its own instances - nothing mutable is
	// handed between tests.
	private static Task<(decimal open, decimal high, decimal low, decimal close, decimal volume)[]> _ohlcvRows;
	private static readonly Lock _ohlcvLock = new();

	private static async Task<(decimal open, decimal high, decimal low, decimal close, decimal volume)[]> ReadOhlcvRows(CancellationToken cancellationToken)
	{
		var path = Path.Combine(Helper.ResFolder, "ohlcv.txt");
		using var reader = new StreamReader(path, Encoding.UTF8);
		var csv = new FastCsvReader(reader, Environment.NewLine) { ColumnSeparator = ',' };

		var list = new List<(decimal open, decimal high, decimal low, decimal close, decimal volume)>();

		while (await csv.NextLineAsync(cancellationToken))
		{
			var open = csv.ReadDecimal();
			var high = csv.ReadDecimal();
			var low = csv.ReadDecimal();
			var close = csv.ReadDecimal();
			var volume = csv.ReadDecimal();

			list.Add((open, high, low, close, volume));
		}

		// Guard the shared parse: every test in this class depends on it, and an empty read would
		// otherwise turn them into silent no-ops instead of failures.
		list.Count.AssertGreater(0, path);

		return [.. list];
	}

	private async ValueTask<TimeFrameCandleMessage[]> LoadCandles(SecurityId secId, DateTime time, TimeSpan tf)
	{
		Task<(decimal open, decimal high, decimal low, decimal close, decimal volume)[]> rowsTask;

		using (_ohlcvLock.EnterScope())
			rowsTask = _ohlcvRows ??= ReadOhlcvRows(CancellationToken);

		var rows = await rowsTask;
		var candles = new TimeFrameCandleMessage[rows.Length];
		var t = time;

		for (var i = 0; i < rows.Length; i++)
		{
			var (open, high, low, close, volume) = rows[i];

			candles[i] = new()
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
			};

			t += tf;
		}

		return candles;
	}

	private static void CompareValue(IIndicatorValue actual, IIndicatorValue expected, string indName, bool checkExtended, bool gpuTolerance = false)
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
			void compare(IEnumerable<object> a, IEnumerable<object> e, string indName)
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
					{
						// GPU float32 precision can flip boolean trend direction near thresholds
						if (!gpuTolerance)
							b1.AssertEqual((bool)ev, indName);
					}
					else if (av is int i1)
						i1.AssertEqual((int)ev, indName);
					else
					{
						var dA = (decimal)av;
						var dE = (decimal)ev;
						var diff = (dA - dE).Abs();

						if (gpuTolerance)
						{
							// GPU uses float32 (~7 sig digits), use relative tolerance
							var maxAbs = dA.Abs().Max(dE.Abs());
							var tol = 1.01m.Max(maxAbs * 0.025m);
							(diff <= tol).AssertTrue($"{indName} GPU={dA} CPU={dE} diff={diff} tol={tol}");
						}
						else
						{
							(diff < 0.001m).AssertTrue(indName);
						}
					}
				}
			}

			compare(actual.ToValues(), expected.ToValues(), indName);
		}
	}

	private static void CompareValues(IIndicatorValue[] actual, IIndicatorValue[] expected, string indName, bool checkExtended, bool gpuTolerance = false)
	{
		ArgumentNullException.ThrowIfNull(actual);
		ArgumentNullException.ThrowIfNull(expected);

		actual.Length.AssertEqual(expected.Length);

		for (var i = 0; i < expected.Length; i++)
			CompareValue(actual[i], expected[i], indName, checkExtended, gpuTolerance);
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
	public async Task Process()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		// Every indicator is an independent run over the same read-only candle array (Check() clones the
		// candles it feeds), so the sweep is parallelised. Failures are collected rather than thrown so
		// that one broken indicator does not hide the others, and each entry names its indicator - the
		// asserts inside Check() carry no name of their own.
		var invalid = new ConcurrentBag<(Type type, Exception error)>();

		Parallel.ForEach(GetIndicatorTypes(), type =>
		{
			try
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
			catch (Exception ex)
			{
				invalid.Add((type.Indicator, ex));
			}
		});

		if (!invalid.IsEmpty)
		{
			var msg = invalid.OrderBy(x => x.type.Name).Select(x => $"{x.type.Name}: {x.error.Message}").JoinN();
			Fail($"Indicators failed ({invalid.Count}):{Environment.NewLine}{msg}");
		}
	}

	// ---------------------------------------------------------------------------------------------------------
	// Reference-vector generator for Resources/IndicatorsData/<Indicator>.txt.
	//
	// Those files are the pinned expected output of Process(), and are otherwise only ever read. This is the
	// single place that knows how to write them, and it shares Render() with Process()'s own Check(), so the
	// written format cannot drift away from the parsed one.
	//
	// A file is produced with the same feed Process() uses - the one the indicator declares through
	// [IndicatorIn]. Changing that declaration therefore changes the reference data, and the file has to be
	// regenerated in the same commit as the change.
	//
	// It never runs by accident: it is opt-in through the SS_INDICATORS_REGEN environment variable, and with the
	// variable unset it does nothing at all.
	//
	//   SS_INDICATORS_REGEN=verify   Re-render every indicator and report how a fresh run compares to the
	//                                committed data. Writes nothing. ALWAYS run this first. It must report no
	//                                VALIDATED difference at all: that is the proof that the generator produces
	//                                the same numbers Process() pins today, so a later rewrite only changes what
	//                                was meant to change. If it does report one, the generator is wrong - fix
	//                                the generator, never the data.
	//   SS_INDICATORS_REGEN=A,B,C    Rewrite the files of these indicators only (by class name).
	//   SS_INDICATORS_REGEN=*        Rewrite every file.
	//
	// The report separates two kinds of difference:
	//
	//   VALIDATED  a value Process() actually asserts on has changed. Never acceptable without an intended
	//              change of behaviour, and the only thing that makes verify fail.
	//   cosmetic   the bytes differ somewhere Check() never looks - a warm-up row before the indicator is
	//              formed, or a reference row that stops short of today's column count. The committed files
	//              carry a good deal of this: they are older than several engine changes (complex values now
	//              back-fill an empty entry for every inner, some warm-up formulas were rewritten), and Check()
	//              deliberately tolerates it. Regenerating a file also normalises its cosmetic drift, which is
	//              why files are regenerated one by one rather than wholesale.
	//
	// Line ending is a hard-coded CRLF and the encoding is UTF-8 without BOM, matching the committed files, so
	// the output is identical on every platform.
	//
	//   set SS_INDICATORS_REGEN=verify && dotnet test StockSharp_Tests.slnx --filter GenerateReferenceData
	// ---------------------------------------------------------------------------------------------------------
	[TestMethod]
	public async Task GenerateReferenceData()
	{
		const string modeVar = "SS_INDICATORS_REGEN";
		const string verifyMode = "verify";
		const string allMode = "*";

		var mode = Environment.GetEnvironmentVariable(modeVar);

		// Inconclusive rather than a bare return: this is a maintenance tool, not a check, and a
		// tool that reports Passed while doing nothing is indistinguishable from one that ran and
		// found nothing wrong. Run it deliberately, and only when a new indicator is added or the
		// logic of an existing one changes - it REWRITES committed reference data.
		if (mode.IsEmpty())
			Inconclusive($"Reference-data maintenance tool; not part of the regression suite. Set {modeVar}=verify to compare without writing, {modeVar}=<Name>[,<Name>...] to rewrite specific files, or {modeVar}=* to rewrite every out-of-date one. Only needed when adding an indicator or changing an existing one's logic.");

		var verify = mode.EqualsIgnoreCase(verifyMode);
		var requested = verify || mode == allMode
			? null
			: mode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		var encoding = new UTF8Encoding(false);
		var identical = new List<string>();
		var cosmetic = new List<string>();
		var validated = new List<string>();
		var written = new List<string>();

		foreach (var type in GetIndicatorTypes())
		{
			var name = type.Indicator.Name;

			if (requested?.Remove(name) == false)
				continue;

			var indicator = type.CreateIndicator();
			var inputType = type.InputValue;

			IndicatorDataRunner.RenderedSeries rendered;

			if (inputType == typeof(DecimalIndicatorValue))
				rendered = indicator.Render(candles, data => data.ClosePrice);
			else if (inputType == typeof(CandleIndicatorValue))
				rendered = indicator.Render(candles, data => data);
			else
				throw new InvalidOperationException(inputType.To<string>());

			var path = Path.Combine(Helper.ResFolder, "IndicatorsData", $"{name}.txt");
			var content = encoding.GetBytes(string.Concat(rendered.Rows.Select(r => r + "\r\n")));
			var current = File.Exists(path) ? File.ReadAllBytes(path) : null;

			if (current is not null && current.SequenceEqual(content))
			{
				identical.Add(name);
				continue;
			}

			if (current is null)
			{
				validated.Add($"{name}: no reference file yet");
			}
			else
			{
				var diffs = rendered.ValidatedDiffs(Do.Invariant(() => File.ReadAllLines(path)));

				if (diffs.Length == 0)
					cosmetic.Add($"{name} (formed from line {rendered.FormedFrom + 1})");
				else
					validated.Add($"{name}: {diffs.Length} validated difference(s), first {diffs.First()}");
			}

			if (verify)
				continue;

			File.WriteAllBytes(path, content);
			written.Add(name);
		}

		if (requested?.Count > 0)
			Fail($"Unknown indicator name(s) in {modeVar}: {requested.JoinCommaSpace()}");

		if (verify)
		{
			var report = $"byte-identical: {identical.Count}, cosmetic drift only: {cosmetic.Count}, VALIDATED differences: {validated.Count}" +
				$"{Environment.NewLine}cosmetic: {cosmetic.JoinCommaSpace()}" +
				$"{Environment.NewLine}{validated.JoinN()}";

			if (validated.Count > 0)
				Fail(report);

			Console.WriteLine(report);
			return;
		}

		written.Count.AssertGreater(0, $"{modeVar}={mode} matched no out-of-date reference file.");
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
			Fail($"Duplicate DocUrl(s) found: {duplicates.JoinCommaSpace()}");
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
			Fail($"Duplicate Names(s) found: {duplicates.JoinCommaSpace()}");
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
			Fail($"Duplicate Descriptions(s) found: {duplicates.JoinCommaSpace()}");
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

	/// <summary>
	/// Edges where the outer indicator holds another indicator but never hands it its own input, so that
	/// inner's input requirement says nothing about what the outer must be fed. Every entry has to name the
	/// exact reason, and <see cref="InputTypeCoversDelegation"/> fails on an entry that no longer matches a
	/// real edge, so the list cannot quietly turn into a blanket suppression.
	/// </summary>
	private static readonly (Type outer, Type inner)[] _nonForwardingDelegations =
	[
		// Both combine two lines' already computed values and never process them - the lines are driven by the
		// owning complex indicator instead: GatorHistogram.OnProcess reads Line1/Line2.GetNullableCurrentValue()
		// and IchimokuSenkouALine.OnProcessDecimal reads Tenkan/Kijun.GetCurrentValue().
		(typeof(GatorHistogram), typeof(AlligatorLine)),
		(typeof(IchimokuSenkouALine), typeof(IchimokuLine)),

		// Substitutes a derived scalar for the input, so StochasticK is used as a plain aggregator over the
		// MACD histogram: SchaffTrendCycle.OnProcessDecimal calls
		// StochasticK.Process(input, (macdHist - _buffer.Min.Value) / den), and that overload builds a brand
		// new DecimalIndicatorValue instead of passing the outer input on.
		(typeof(SchaffTrendCycle), typeof(StochasticK)),
	];

	/// <summary>
	/// An indicator hands its own input straight to the indicators it delegates to:
	/// <see cref="BaseComplexIndicator{TValue}.OnProcess"/> passes <c>input</c> to every inner, and the
	/// hand-rolled delegations (an indicator kept in a field, e.g. <c>AverageTrueRange._trueRange</c>) do the
	/// same. So the outer declaration has to satisfy every inner declaration. An outer that declares
	/// <see cref="DecimalIndicatorValue"/> while an inner requires <see cref="CandleIndicatorValue"/> quietly
	/// feeds that inner a degenerate candle whose open/high/low/close are all the same number - the inner keeps
	/// computing, just over bars that never existed.
	/// </summary>
	[TestMethod]
	public void InputTypeCoversDelegation()
	{
		var errors = new SortedSet<string>(StringComparer.Ordinal);
		var usedExceptions = new HashSet<(Type, Type)>();

		foreach (var indicator in ReachableIndicators())
		{
			var outerType = indicator.GetType();

			if (outerType.GetValueType(true) != typeof(DecimalIndicatorValue))
				continue;

			foreach (var inner in Delegates(indicator))
			{
				var innerType = inner.GetType();

				if (innerType.GetValueType(true) != typeof(CandleIndicatorValue))
					continue;

				if (_nonForwardingDelegations.Contains((outerType, innerType)))
				{
					usedExceptions.Add((outerType, innerType));
					continue;
				}

				errors.Add($"{outerType.Name} declares {nameof(DecimalIndicatorValue)} but delegates to {innerType.Name}, which requires {nameof(CandleIndicatorValue)}.");
			}
		}

		var stale = _nonForwardingDelegations.Where(e => !usedExceptions.Contains(e)).ToArray();

		if (stale.Length > 0)
			Fail($"Stale {nameof(_nonForwardingDelegations)} entries (no such delegation any more): {stale.Select(e => $"{e.outer.Name}->{e.inner.Name}").JoinCommaSpace()}");

		if (errors.Count > 0)
			Fail($"Indicators whose declared input does not cover what they delegate to:{Environment.NewLine}{errors.JoinN()}");
	}

	/// <summary>
	/// Indicators whose entire contract is to aggregate whatever stream they are given, so being fed a decimal
	/// instead of a candle is intended polymorphism rather than starvation: fed candles they aggregate the bar
	/// extremes, fed plain numbers they aggregate the numbers. They read a candle field, but they do not require
	/// one, so they stay on <see cref="DecimalIndicatorValue"/>.
	/// </summary>
	private static readonly Type[] _feedPolymorphic =
	[
		typeof(Highest),
		typeof(Lowest),
	];

	/// <summary>
	/// Declaring <see cref="DecimalIndicatorValue"/> is a promise that the close price is all the indicator
	/// looks at. Running the same series twice - once as candles, once as bare close prices - has to produce the
	/// same numbers for such an indicator. If it does not, the implementation reads open/high/low/volume and the
	/// declaration is wrong. This catches what <see cref="InputTypeCoversDelegation"/> cannot: an indicator that
	/// reads bar fields without going through an inner that declares candles (e.g. Donchian Channels, whose
	/// inners are the deliberately feed-polymorphic <see cref="Highest"/>/<see cref="Lowest"/>).
	/// </summary>
	[TestMethod]
	public async Task InputTypeCoversFeedSensitivity()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurity().ToSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		var errors = new List<string>();
		var usedExceptions = new HashSet<Type>();

		foreach (var type in GetIndicatorTypes())
		{
			if (type.InputValue != typeof(DecimalIndicatorValue))
				continue;

			var byClose = type.CreateIndicator().Render(candles, c => c.ClosePrice).Rows;
			var byCandle = type.CreateIndicator().Render(candles, c => c).Rows;

			var diff = -1;

			for (var i = 0; i < byClose.Length; i++)
			{
				if (byClose[i] != byCandle[i])
				{
					diff = i;
					break;
				}
			}

			// The exemption is applied AFTER measuring rather than as an early skip, so an entry
			// that has stopped being feed-sensitive is reported as stale instead of silently
			// exempting an indicator that no longer needs exempting.
			if (_feedPolymorphic.Contains(type.Indicator))
			{
				if (diff >= 0)
					usedExceptions.Add(type.Indicator);

				continue;
			}

			if (diff < 0)
				continue;

			errors.Add($"{type.Indicator.Name} declares {nameof(DecimalIndicatorValue)} but reacts to the bar fields: line {diff + 1} is '{byClose[diff]}' fed the close and '{byCandle[diff]}' fed the candle.");
		}

		if (errors.Count > 0)
			Fail($"Indicators whose declared input does not match what they read:{Environment.NewLine}{errors.JoinN()}");

		var stale = _feedPolymorphic.Where(t => !usedExceptions.Contains(t)).ToArray();

		if (stale.Length > 0)
			Fail($"Stale {nameof(_feedPolymorphic)} entries (no longer feed-sensitive, or no longer registered as declaring {nameof(DecimalIndicatorValue)}): {stale.Select(t => t.Name).JoinCommaSpace()}");
	}

	/// <summary>
	/// Every indicator instance reachable from the registered ones through delegation, including the
	/// <see cref="IndicatorHiddenAttribute"/> building blocks that never appear in the provider on their own
	/// (e.g. <see cref="AlligatorLine"/>) - the invariant applies to them just the same.
	/// </summary>
	private static IEnumerable<IIndicator> ReachableIndicators()
	{
		var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
		var pending = new Queue<IIndicator>(GetIndicatorTypes().Select(t => t.CreateIndicator()));

		while (pending.Count > 0)
		{
			var indicator = pending.Dequeue();

			if (!visited.Add(indicator))
				continue;

			yield return indicator;

			foreach (var inner in Delegates(indicator))
				pending.Enqueue(inner);
		}
	}

	/// <summary>
	/// The indicators <paramref name="indicator"/> drives: the inner ones of a complex indicator plus anything
	/// of an indicator type kept in an instance field, private and inherited ones included (auto-property
	/// backing fields are covered by that, which is how the hand-rolled delegations are declared).
	/// </summary>
	private static IEnumerable<IIndicator> Delegates(IIndicator indicator)
	{
		if (indicator is IComplexIndicator complex)
		{
			foreach (var inner in complex.InnerIndicators)
				yield return inner;
		}

		for (var type = indicator.GetType(); type is not null && type != typeof(object); type = type.BaseType)
		{
			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (!field.FieldType.Is<IIndicator>())
					continue;

				if (field.GetValue(indicator) is IIndicator inner)
					yield return inner;
			}
		}
	}

	[TestMethod]
	public async Task GpuIndicators()
	{
		async Task<ICandleMessage[][]> loadCandles()
		{
			var start = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
			var baseTf = TimeSpan.FromMinutes(1);
			var secId = Helper.CreateSecurityId();

			// 1m base candles from storage
			var baseCandles = (await LoadCandles(secId, start, baseTf))
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

		var msgSeries = await loadCandles(); // multiple TF series
		var gpuSeries = msgSeries
			.Select(series => series.Select(c => new GpuCandle(c.OpenTime, c.OpenPrice, c.HighPrice, c.LowPrice, c.ClosePrice, c.TotalVolume)).ToArray())
			.ToArray();

		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		var invalid = new ConcurrentBag<(Type type, Exception error)>();

		// No context/accelerator is created or disposed here: both belong to the class (see ClassInit)
		// and are shared with the provider tests.
		foreach (var (indicatorType, calculatorType) in provider.All)
		{
			// build N parameter variations from randomized indicators.
			// Four instead of ten lowers the randomisation density only: every calculator, every time
			// frame series and every bar is still compared GPU against CPU, just with fewer random
			// parameter draws per run.
			const int variations = 4;
			var indicators = new IIndicator[variations];

			for (var i = 0; i < indicators.Length; i++)
			{
				var indicator = indicatorType.CreateInstance<IIndicator>();
				// Randomize indicator settings using existing helper
				SetRandom(indicator, () => { });

				indicators[i] = indicator;
			}

			IGpuIndicatorResult[][][] gpuAll;

			// Only the accelerator-bound part is serialised; the CPU reference matrix below runs outside
			// the lock so the provider tests are not held up for the whole sweep. The kernels the
			// calculator constructor JITs must be compiled one at a time on the shared accelerator.
			var (gpuContext, gpuAccelerator) = GetGpu();

			using (_gpuLock.EnterScope())
			{
				var calculator = provider.Create(gpuContext, gpuAccelerator, calculatorType);
				calculator.AssertNotNull();

				var parameters = randomIndicators(indicators, calculator.ParameterType);

				// calculate via interface for all TF series and all params
				gpuAll = calculator.Calculate(gpuSeries, parameters); // [series][param][bar]
			}

			// Every (series, parameter set) cell runs its own freshly cloned CPU indicator over a
			// read-only candle array and touches no GPU state, so the whole matrix is compared in
			// parallel - this is the bulk of the test's time.
			var cells =
				from s in Enumerable.Range(0, msgSeries.Length)
				from p in Enumerable.Range(0, indicators.Length)
				select (series: s, param: p);

			try
			{
				Parallel.ForEach(cells, cell =>
				{
					var gpuOut = gpuAll[cell.series][cell.param];

					// fresh indicator instance for CPU with same settings
					var indCpu = indicators[cell.param].TypedClone();
					var cpu = runCpu(indCpu, msgSeries[cell.series]);

					CompareValues([.. gpuOut.Select(r => r.ToValue(indCpu))], cpu, indCpu.ToString(), true, gpuTolerance: true);
				});
			}
			catch (AggregateException ex)
			{
				// Unwrap so each failing cell keeps its own message instead of hiding behind the
				// aggregate, and every one of them still names the indicator it came from.
				foreach (var inner in ex.Flatten().InnerExceptions)
					invalid.Add((indicatorType, inner));
			}
			catch (Exception ex)
			{
				invalid.Add((indicatorType, ex));
			}
		}

		if (!invalid.IsEmpty)
		{
			var msg = invalid.OrderBy(x => x.type.Name).Select(x => $"{x.type.Name}: {x.error.Message}").JoinN();
			Fail($"GPU indicators failed ({invalid.Count}):{Environment.NewLine}{msg}");
		}
	}

	[TestMethod]
	public void GpuProviderInit()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		// Must discover at least the built-in GPU calculators
		provider.All.Keys.Count(k => k == typeof(SimpleMovingAverage)).AssertEqual(1, "SMA calculator not discovered");
		provider.All.Keys.Count(k => k == typeof(AverageDirectionalIndex)).AssertEqual(1, "ADX calculator not discovered");
	}

	[TestMethod]
	public void GpuProviderTryGet()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		var indType = typeof(SimpleMovingAverage);

		provider.TryGetCalculatorType(indType, out var calcType).AssertTrue();
		calcType.Is<IGpuIndicatorCalculator>().AssertTrue();

		// What this test is about is the provider lookup and the calculator it builds, not the device:
		// it uses the accelerator the class created once instead of paying seconds for its own.
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = provider.Create(gpuContext, gpuAccelerator, calcType);
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

		// Create should return a calculator instance (on the accelerator the class created once)
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = provider.Create(gpuContext, gpuAccelerator, unkCalcType);
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
}

static class IndicatorDataRunner
{
	private class TestIndicatorValue<TInner> : IIndicatorValue
	{
		private readonly TInner _value;

		public TestIndicatorValue(IIndicator indicator, DateTime time, TInner value, TInner initFrom = default)
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
		public DateTime Time { get; }
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
					OpenTime = DateTime.UtcNow,
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

	/// <summary>
	/// An indicator run rendered in the on-disk shape of Resources/IndicatorsData/&lt;Indicator&gt;.txt.
	/// </summary>
	public class RenderedSeries
	{
		/// <summary>
		/// One line per candle.
		/// </summary>
		public string[] Rows { get; init; }

		/// <summary>
		/// Index of the first row from which <see cref="IIndicator.IsFormed"/> held. Rows before it are the
		/// warm-up: <see cref="Check{T}"/> returns before looking at them, so nothing there is validated.
		/// </summary>
		public int FormedFrom { get; init; }

		/// <summary>
		/// Comparison tolerance <see cref="Check{T}"/> applies, derived from <see cref="IIndicator.Measure"/>.
		/// </summary>
		public decimal Epsilon { get; init; }
	}

	/// <summary>
	/// Runs <paramref name="indicator"/> over <paramref name="candles"/> with final values only and renders one
	/// line per candle in the exact on-disk shape of Resources/IndicatorsData/&lt;Indicator&gt;.txt: every plain
	/// (flattened) component of the produced value, rounded to two decimals and formatted with the invariant
	/// culture, joined by comma; an empty component renders as an empty field. This is the single definition of
	/// that format - the reference-data generator and the feed-comparison test both go through it, so neither
	/// can drift away from what <see cref="Check{T}"/> parses.
	/// </summary>
	public static RenderedSeries Render<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(candles);
		ArgumentNullException.ThrowIfNull(getValue);

		var rows = new string[candles.Length];
		var formedFrom = candles.Length;

		Do.Invariant(() =>
		{
			for (var i = 0; i < candles.Length; i++)
			{
				var candle = candles[i];
				var value = indicator.Process(new TestIndicatorValue<T>(indicator, candle.OpenTime, getValue(candle)) { IsFinal = true });

				rows[i] = value.Plain().Select(v => v.IsEmpty ? string.Empty : v.ToDecimal().Round(2).ToString()).JoinComma();

				if (indicator.IsFormed && formedFrom > i)
					formedFrom = i;
			}
		});

		return new()
		{
			Rows = rows,
			FormedFrom = formedFrom,
			Epsilon = Epsilon(indicator),
		};
	}

	private static decimal Epsilon(IIndicator indicator)
		=> indicator.Measure switch
		{
			IndicatorMeasures.MinusOnePlusOne => 0.001m,
			IndicatorMeasures.Percent or IndicatorMeasures.Price or IndicatorMeasures.Volume => 0.1m,
			_ => throw new NotSupportedException(indicator.Measure.ToString()),
		};

	/// <summary>
	/// Lists the differences between the committed reference lines and a fresh run that <see cref="Check{T}"/>
	/// would actually trip over, applying its own reading rules: a row before the indicator is formed is never
	/// looked at; a component that now comes out empty only has to face an empty or absent reference cell, and
	/// only when the row is partially formed; a component that now has a value has to face a reference value
	/// within the measure's epsilon; reference columns beyond the produced ones are never read.
	/// Everything else - stale warm-up numbers, reference rows that stop short of today's column count - is
	/// cosmetic drift that the pinned data is allowed to carry.
	/// </summary>
	public static string[] ValidatedDiffs(this RenderedSeries rendered, string[] committed)
	{
		ArgumentNullException.ThrowIfNull(rendered);
		ArgumentNullException.ThrowIfNull(committed);

		return [.. Do.Invariant(() =>
		{
			var diffs = new List<string>();

			for (var i = rendered.FormedFrom; i < rendered.Rows.Length; i++)
			{
				var produced = rendered.Rows[i].SplitByComma();

				if (i >= committed.Length)
				{
					diffs.Add($"line {i + 1}: reference data ends, indicator still produces '{rendered.Rows[i]}'");
					continue;
				}

				var reference = committed[i].SplitByComma();
				var rowHasValue = produced.Any(c => !c.IsEmpty());

				for (var col = 0; col < produced.Length; col++)
				{
					var now = produced[col];
					var was = col < reference.Length ? reference[col] : null;

					if (now.IsEmpty())
					{
						if (rowHasValue && !was.IsEmpty())
							diffs.Add($"line {i + 1} column {col}: reference '{was}', now empty");

						continue;
					}

					if (was.IsEmpty())
					{
						diffs.Add($"line {i + 1} column {col}: reference {(was is null ? "has no such column" : "empty")}, now '{now}'");
						continue;
					}

					if ((was.To<decimal>() - now.To<decimal>()).Abs() >= rendered.Epsilon)
						diffs.Add($"line {i + 1} column {col}: reference '{was}', now '{now}'");
				}
			}

			return diffs;
		})];
	}

	public static void Check<T>(this IIndicator indicator, CandleMessage[] candles, Func<ICandleMessage, T> getValue)
	{
		ArgumentNullException.ThrowIfNull(indicator);
		ArgumentNullException.ThrowIfNull(getValue);

		var values = new List<IndicatorData>();

		var epsilon = Epsilon(indicator);

		// Counts the non-final values actually injected below. They are what proves ValidateValue's
		// complex-value contract on non-final input and that non-final input leaves no residue in the
		// state the next final comparison reads, so the run must not silently end up feeding none.
		var nonFinalCount = 0;

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

			// 0..3 rather than 0..10 non-final values per bar. The injections stay - they are the point,
			// see nonFinalCount above - only their density drops, from ~5 extra values per bar to ~1.5,
			// which is what made this a ~6x input multiplier over the 1658 reference bars.
			var numNonFinals = RandomGen.GetInt(3);
			for (var j = 0; j < numNonFinals; ++j)
			{
				var i2 = 0.Max((data.Length - 1).Min(i + RandomGen.GetInt(-5, 5)));
				inputValues.Add(new(indicator, data[i2].Candle.OpenTime, getValue(data[i2].Candle), i < data.Length - 1 ? getValue(data[i+1].Candle) : default) { IsFinal = false });
			}

			nonFinalCount += numNonFinals;

			void CheckValue(IIndicatorValue value, int column, bool rowHasValue)
			{
				if (!indicator.IsFormed)
					return;

				var data = values[values.Count - 1];

				if (value.IsEmpty)
				{
					// Sound interior-empty contract: only when this final value is PARTIALLY
					// formed (rowHasValue: at least one of its plain components is non-empty) does
					// the reference row definitely span the non-empty columns, so an empty
					// component at a lower column must map to a null reference cell (a value->Empty
					// regression of one inner output would be caught here). For a fully empty
					// (warm-up) value we skip the check: the reference row may be a blank line, and
					// a few reference files (e.g. Shift.txt) carry a stale value on the warm-up bar
					// - a reference-data quirk, not an engine regression. The column<Length guard
					// also covers trailing-trimmed empties.
					if (rowHasValue && column < data.Values.Length)
						data.Values[column].AssertNull();
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

				var plain = value.Plain().ToArray();
				var rowHasValue = plain.Any(sv => !sv.IsEmpty);

				plain
					.Select((sv, idx) => (v: sv, column: idx))
					.ForEach(p => CheckValue(p.v, p.column, rowHasValue));
			}
		}

		indicator.IsFormed.AssertTrue();
		nonFinalCount.AssertGreater(0, indicator.ToString());
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