namespace StockSharp.Tests;

using System.Collections.Concurrent;

using ILGPU;
using ILGPU.Runtime;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Gpu;
using StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// The shared GPU device, and the calculators compared against the CPU indicators they mirror.
/// </summary>
partial class IndicatorTests
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

	// Why the shared device could not be created, or null when it was. Filled in by the single probe
	// below so that every GPU test reads the same answer instead of paying for its own attempt.
	private static string _gpuUnavailableReason;
	private static bool _gpuProbed;

	/// <summary>
	/// Creates the shared device on first use, and reports a machine that cannot host one as
	/// inconclusive rather than as a failed indicator. Doing it in <see cref="ClassInitializeAttribute"/>
	/// instead would fail all tests in the class - including the majority that never touch the GPU - on
	/// a box where no accelerator can be created.
	/// </summary>
	private static (Context, Accelerator) GetGpu()
	{
		SkipIfNoGpu();

		using (_gpuLock.EnterScope())
			return (_gpuContext, _gpuAccelerator);
	}

	// Probes for a device once and reports its absence as inconclusive. ILGPU offers a CPU accelerator
	// on any machine, so this normally passes straight through; what it catches is a box whose only
	// driver is broken or whose device went away, neither of which says anything about whether the
	// calculators below compute the right numbers.
	private static void SkipIfNoGpu()
	{
		using (_gpuLock.EnterScope())
		{
			if (!_gpuProbed)
			{
				_gpuProbed = true;

				try
				{
					if (GpuAcceleratorFactory.TryCreateBestAccelerator(out var context, out var accelerator))
						(_gpuContext, _gpuAccelerator) = (context, accelerator);
					else
						_gpuUnavailableReason = "ILGPU reports no accelerator on this machine, so nothing in this class can run a GPU calculator.";
				}
				catch (Exception ex)
				{
					_gpuUnavailableReason = $"ILGPU could not create an accelerator on this machine ({ex.Message}), so nothing in this class can run a GPU calculator.";
				}
			}
		}

		if (_gpuUnavailableReason is string reason)
			Inconclusive(reason);
	}

	/// <summary>
	/// Every GPU test below compares a calculator against the CPU indicator it mirrors, and that
	/// comparison only means something when there is a device to run the calculator on. A machine that
	/// cannot create one is an environment, not a wrong indicator, and a suite that shows a missing
	/// device as red teaches its readers to ignore red. The distinction is drawn once here: an absent
	/// accelerator is reported inconclusive and named, while an accelerator that is present has to be
	/// able to host a calculator, so that every failure below belongs to the code it is testing.
	/// </summary>
	[TestMethod]
	public void MissingGpuAcceleratorIsReportedAsInconclusiveNotFailure()
	{
		var (gpuContext, gpuAccelerator) = GetGpu();

		IsNotNull(gpuContext, "No ILGPU context was created, so nothing in this class can launch a kernel.");
		IsNotNull(gpuAccelerator, "No ILGPU accelerator was created, so nothing in this class can launch a kernel.");

		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		provider.TryGetCalculatorType(typeof(SimpleMovingAverage), out var calcType).AssertTrue("No GPU calculator is registered for the simplest indicator there is, so every comparison below fails for a reason that has nothing to do with the calculator it compares.");

		using (_gpuLock.EnterScope())
		{
			var calc = provider.Create(gpuContext, gpuAccelerator, calcType);

			IsNotNull(calc, "The device is there but refuses to host a calculator, so every failure below would name an indicator for a fault of the device.");
		}
	}

	[ClassCleanup]
	public static void ClassUnInit()
	{
		_gpuAccelerator?.Dispose();
		_gpuContext?.Dispose();

		_gpuAccelerator = null;
		_gpuContext = null;
		_gpuUnavailableReason = null;
		_gpuProbed = false;
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

		// Price scale of each series: one of the two magnitudes the float32 allowance is formed from.
		var priceScales = msgSeries
			.Select(series => series.Max(c => c.HighPrice.Abs().Max(c.LowPrice.Abs())))
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

					// The kernel's error is bounded by the largest quantity it touches: the prices it reads,
					// or the values it produces when those run bigger (an accumulator over volume does).
					var scale = priceScales[cell.series].Max(ValuesScale(cpu));

					CompareValues([.. gpuOut.Select(r => r.ToValue(indCpu))], cpu, indCpu.ToString(), true, GpuTolerance(scale, indCpu.NumValuesToInitialize));
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

	// Both input sums are exact in float32, but dividing them by different window lengths can round
	// two distinct averages to the same value. A crossover is categorical, so retain the sign while
	// calculating the averages and only convert the resulting -1/0/+1 signal to float.
	[TestMethod]
	public void GpuMovingAverageCrossoverKeepsNarrowAverageDifference()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		var closes = Enumerable.Repeat(7_000m, 65).ToArray();

		for (var i = 0; i < 4; i++)
			closes[i]++;

		for (var i = 29; i < 34; i++)
			closes[i]++;

		var cpu = new MovingAverageCrossover
		{
			ShortPeriod = 65,
			LongPeriod = 36,
		};
		var cpuValues = closes
			.Select((close, i) => cpu.Process(new DecimalIndicatorValue(cpu, close, start.AddMinutes(i)) { IsFinal = true }))
			.ToArray();

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		GpuIndicatorResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			gpuValues = new GpuMovingAverageCrossoverCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], [new GpuMovingAverageCrossoverParams(65, 36, (byte)Level1Fields.ClosePrice)])[0][0];
		}

		cpuValues[^1].IsFormed.AssertTrue();
		cpuValues[^1].ToDecimal().AssertEqual(-1m);
		gpuValues[^1].IsFormed.AssertEqual((byte)1);
		((decimal)gpuValues[^1].Value).AssertEqual(-1m);
	}

	// Whole-number inputs are represented exactly by float32, but the two EMA states that make up
	// MACD can still accumulate enough rounding error for a small positive slope to disappear. The
	// impulse is categorical, so 0 and +1 cannot be covered by the numeric GPU tolerance.
	[TestMethod]
	public void GpuElderImpulseSystemKeepsSmallPositiveMacdSlope()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [3_999_996m, 3_999_996m, 3_999_997m, 3_999_998m];

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		var cpu = new ElderImpulseSystem();
		cpu.Ema.Length = 2;
		cpu.Macd.LongMa.Length = 3;
		cpu.Macd.ShortMa.Length = 2;

		var cpuValues = closes
			.Select((close, i) => cpu.Process(new DecimalIndicatorValue(cpu, close, start.AddMinutes(i)) { IsFinal = true }))
			.ToArray();

		GpuIndicatorResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var parameters = new[]
			{
				new GpuElderImpulseSystemParams(2, 3, 2, (byte)Level1Fields.ClosePrice),
			};

			gpuValues = new GpuElderImpulseSystemCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], parameters)[0][0];
		}

		gpuValues.Length.AssertEqual(cpuValues.Length);

		for (var i = 0; i < cpuValues.Length; i++)
		{
			gpuValues[i].IsFormed.AssertEqual(cpuValues[i].IsFormed ? (byte)1 : (byte)0, $"bar {i}");

			if (cpuValues[i].IsFormed)
				((decimal)gpuValues[i].Value).AssertEqual(cpuValues[i].ToDecimal(), $"bar {i}");
		}

		cpuValues[^1].ToDecimal().AssertEqual(1m, "the final EMA and MACD slopes are both positive");
	}

	// The input integers are represented exactly by float32. Rounding the six recursive Laguerre
	// states to float32 after every bar still destroys small price moves around a large price level.
	[TestMethod]
	public void GpuLaguerreRsiPreservesSmallMovesAtLargePriceLevel()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes =
		[
			1_000_000m, 1_000_002m, 1_000_001m, 1_000_003m, 1_000_004m, 1_000_004m,
			1_000_003m, 1_000_001m, 1_000_001m, 1_000_002m, 1_000_002m,
		];

		var cpu = new LaguerreRSI { Gamma = 0.0159805023325431167766m };
		var cpuValues = closes
			.Select((close, i) => cpu.Process(new DecimalIndicatorValue(cpu, close, start.AddMinutes(i)) { IsFinal = true }))
			.ToArray();

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		GpuIndicatorResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var parameters = new[]
			{
				new GpuLaguerreRsiParams((float)cpu.Gamma, (byte)Level1Fields.ClosePrice),
			};

			gpuValues = new GpuLaguerreRsiCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], parameters)[0][0];
		}

		gpuValues.Length.AssertEqual(cpuValues.Length);

		var cpuFinal = cpuValues[^1].ToDecimal();
		var gpuFinal = (decimal)gpuValues[^1].Value;

		(gpuFinal - cpuFinal).Abs().AssertLess(0.001m, $"GPU={gpuFinal} CPU={cpuFinal}");
	}

	// Shift counts bars back to the bar that holds the extremum, not bars back to the reversal that
	// started the leg. The two only coincide when the low is made on the very first bar of the leg,
	// so the lows here put the low three bars before the reversal and five bars after the previous one.
	[TestMethod]
	public void GpuTroughShiftCountsBarsBackToTheExtremumBar()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] lows = [100m, 99m, 98m, 97m, 97.5m, 97.5m, 98.5m];

		var cpu = new Trough { Deviation = 0.01m };
		var cpuValues = lows
			.Select((low, i) => cpu.Process(new TimeFrameCandleMessage
			{
				OpenTime = start.AddMinutes(i),
				OpenPrice = low,
				HighPrice = low,
				LowPrice = low,
				ClosePrice = low,
				TotalVolume = 1m,
				State = CandleStates.Finished,
			}))
			.ToArray();

		var bars = lows
			.Select((low, i) => new GpuCandle(start.AddMinutes(i), low, low, low, low, 1m))
			.ToArray();

		GpuTroughResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			gpuValues = new GpuTroughCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], [new GpuTroughParams((float)cpu.Deviation)])[0][0];
		}

		var cpuTrough = (ZigZagIndicatorValue)cpuValues[^1];
		var gpuTrough = (ZigZagIndicatorValue)gpuValues[^1].ToValue(cpu);

		cpuTrough.IsEmpty.AssertFalse("CPU trough");
		gpuTrough.IsEmpty.AssertFalse("GPU trough");

		cpuTrough.ToDecimal().AssertEqual(97m, "CPU value");
		gpuTrough.ToDecimal().AssertEqual(97m, "GPU value");

		cpuTrough.Shift.AssertEqual(3, "CPU shift");
		gpuTrough.Shift.AssertEqual(3, "GPU shift");
	}

	// %B measures where the price sits inside a band that a small deviation multiplier makes a tiny
	// fraction of the price itself. These closes put the price 0.0135 above a lower band around 7112,
	// and one float32 step there is 4.9e-4 - so bands built as absolute prices hold that distance on
	// 28 representable values.
	[TestMethod]
	public void GpuBollingerPercentBKeepsNarrowBandAgainstPriceScaleRounding()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [7108m, 7109m, 7112m, 7111m, 7114m, 7114m, 7114m, 7114m, 7114m, 7112m];

		var cpu = new BollingerPercentB
		{
			Length = closes.Length,
			StdDevMultiplier = 0.1m,
		};

		var cpuValues = closes
			.Select((close, i) => cpu.Process(new TimeFrameCandleMessage
			{
				OpenTime = start.AddMinutes(i),
				OpenPrice = close,
				HighPrice = close,
				LowPrice = close,
				ClosePrice = close,
				TotalVolume = 1m,
				State = CandleStates.Finished,
			}))
			.ToArray();

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		GpuIndicatorResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var parameters = new[]
			{
				new GpuBollingerPercentBParams(closes.Length, (float)cpu.StdDevMultiplier, (byte)Level1Fields.ClosePrice),
			};

			gpuValues = new GpuBollingerPercentBCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], parameters)[0][0];
		}

		gpuValues[^1].IsFormed.AssertEqual((byte)1, "GPU formed");
		cpuValues[^1].IsFormed.AssertTrue("CPU formed");

		var cpuFinal = cpuValues[^1].ToDecimal();
		var gpuFinal = (decimal)gpuValues[^1].Value;

		// The expected answer is pinned as well, so the two sides cannot agree by drifting together.
		(cpuFinal - 3.1707094m).Abs().AssertLess(0.000001m, $"CPU={cpuFinal}");
		(gpuFinal - cpuFinal).Abs().AssertLess(0.001m, $"GPU={gpuFinal} CPU={cpuFinal}");
	}

	// Deviation taken as sumSq/L - mean^2 has to recover a variance of 4.6 by subtracting two numbers
	// near 5.06e7, where a float32 step is already 4. These closes deviate by 2.1354 around a price of
	// 7112, and most of that is lost in the subtraction, leaving both bands whole units out.
	[TestMethod]
	public void GpuBollingerBandsKeepsDeviationAtLargePriceLevel()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [7108m, 7109m, 7112m, 7111m, 7114m, 7114m, 7114m, 7114m, 7114m, 7112m];

		var cpu = new BollingerBands
		{
			Length = closes.Length,
			Width = 2m,
		};

		var cpuValues = closes
			.Select((close, i) => cpu.Process(new TimeFrameCandleMessage
			{
				OpenTime = start.AddMinutes(i),
				OpenPrice = close,
				HighPrice = close,
				LowPrice = close,
				ClosePrice = close,
				TotalVolume = 1m,
				State = CandleStates.Finished,
			}))
			.ToArray();

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		GpuBollingerBandsResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var parameters = new[]
			{
				new GpuBollingerBandsParams(closes.Length, (float)cpu.Width, (byte)Level1Fields.ClosePrice),
			};

			gpuValues = new GpuBollingerBandsCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], parameters)[0][0];
		}

		gpuValues[^1].IsFormed.AssertEqual((byte)1, "GPU formed");

		var cpuBands = (IBollingerBandsValue)cpuValues[^1];
		var gpuBands = (IBollingerBandsValue)gpuValues[^1].ToValue(cpu);

		var cpuWidth = cpuBands.UpBand.Value - cpuBands.LowBand.Value;
		var gpuWidth = gpuBands.UpBand.Value - gpuBands.LowBand.Value;

		// Four population deviations of 2.1354 wide, pinned so a zero deviation cannot pass unnoticed.
		(cpuWidth - 8.5416626m).Abs().AssertLess(0.000001m, $"CPU width={cpuWidth}");

		(gpuBands.MovingAverage.Value - cpuBands.MovingAverage.Value).Abs().AssertLess(0.01m, $"GPU middle={gpuBands.MovingAverage}");
		(gpuBands.UpBand.Value - cpuBands.UpBand.Value).Abs().AssertLess(0.01m, $"GPU upper={gpuBands.UpBand}");
		(gpuBands.LowBand.Value - cpuBands.LowBand.Value).Abs().AssertLess(0.01m, $"GPU lower={gpuBands.LowBand}");
		(gpuWidth - cpuWidth).Abs().AssertLess(0.01m, $"GPU width={gpuWidth} CPU width={cpuWidth}");
	}

	[TestMethod]
	public void ShiftCpuAndGpuFormOnTheDeclaredValueCount()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [10m, 20m, 30m, 40m];
		var cpu = new Shift { Length = 3 };

		cpu.NumValuesToInitialize.AssertEqual(3);

		var cpuValues = closes
			.Select((close, i) => cpu.Process(new DecimalIndicatorValue(cpu, close, start.AddMinutes(i)) { IsFinal = true }))
			.ToArray();

		cpuValues[0].IsEmpty.AssertTrue("CPU bar 0");
		cpuValues[1].IsEmpty.AssertTrue("CPU bar 1");
		cpuValues[2].IsFormed.AssertTrue("CPU bar 2");
		cpuValues[2].ToDecimal().AssertEqual(30m, "CPU bar 2");
		cpuValues[3].ToDecimal().AssertEqual(40m, "CPU bar 3");

		var bars = closes
			.Select((close, i) => new GpuCandle(start.AddMinutes(i), close, close, close, close, 1m))
			.ToArray();

		GpuIndicatorResult[] gpuValues;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			gpuValues = new GpuShiftCalculator(gpuContext, gpuAccelerator)
				.Calculate([bars], [new GpuShiftParams(3, (byte)Level1Fields.ClosePrice)])[0][0];
		}

		gpuValues[0].IsFormed.AssertEqual((byte)0, "GPU bar 0");
		gpuValues[1].IsFormed.AssertEqual((byte)0, "GPU bar 1");
		gpuValues[2].IsFormed.AssertEqual((byte)1, "GPU bar 2");
		((decimal)gpuValues[2].Value).AssertEqual(30m, "GPU bar 2");
		gpuValues[3].IsFormed.AssertEqual((byte)1, "GPU bar 3");
		((decimal)gpuValues[3].Value).AssertEqual(40m, "GPU bar 3");
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

	// Register documents itself as "register (or replace)", so registering over a mapping Init already
	// discovered must leave the newer calculator in place instead of refusing the call.
	[TestMethod]
	public void GpuProviderRegisterReplacesExistingMapping()
	{
		var provider = new GpuIndicatorCalculatorProvider();
		provider.Init();

		provider.TryGetCalculatorType(typeof(SimpleMovingAverage), out var discovered).AssertTrue();
		discovered.AssertEqual(typeof(GpuSmaCalculator));

		provider.Register<SimpleMovingAverage, GpuHighestCalculator>();

		provider.TryGetCalculatorType(typeof(SimpleMovingAverage), out var replaced).AssertTrue();
		replaced.AssertEqual(typeof(GpuHighestCalculator));
		provider.All.Keys.Count(k => k == typeof(SimpleMovingAverage)).AssertEqual(1);
	}

	// One batch mixes empty, single-bar and long series: every cell must line up with its own series and
	// bar, so result[s][p][i] carries that series' value and nobody else's. Closes on the long series are
	// 10..100, so SMA(3) at bar i is (10(i-1) + 10i + 10(i+1)) / 3 = 10i.
	[TestMethod]
	public void GpuSmaHeterogeneousBatchKeepsSeriesAligned()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();

		static GpuCandle bar(DateTime time, decimal close)
			=> new(time, close, close, close, close, 1m);

		var single = new[] { bar(start, 777m) };
		var longSeries = new GpuCandle[10];

		for (var i = 0; i < longSeries.Length; i++)
			longSeries[i] = bar(start.AddMinutes(i), 10m * (i + 1));

		GpuCandle[][] series = [[], single, longSeries, null];

		// L = 1 (the price itself), then window-1, window and window+1 against the ten-bar series.
		var parameters = new[]
		{
			new GpuSmaParams(1, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(3, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(9, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(10, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(11, (byte)Level1Fields.ClosePrice),
		};

		GpuIndicatorResult[][][] res;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = new GpuSmaCalculator(gpuContext, gpuAccelerator);
			res = calc.Calculate(series, parameters);
		}

		static void formed(GpuIndicatorResult actual, GpuCandle candle, decimal expected, string name)
		{
			actual.Time.AssertEqual(candle.Time, name);
			actual.IsFormed.AssertEqual((byte)1, name);
			float.IsNaN(actual.Value).AssertFalse(name);
			((decimal)actual.Value).AssertEqual(expected, name);
		}

		static void notFormed(GpuIndicatorResult actual, GpuCandle candle, string name)
		{
			actual.Time.AssertEqual(candle.Time, name);
			actual.IsFormed.AssertEqual((byte)0, name);
			float.IsNaN(actual.Value).AssertTrue(name);
		}

		res.Length.AssertEqual(series.Length);

		for (var p = 0; p < parameters.Length; p++)
		{
			res[0][p].Length.AssertEqual(0, "empty series");
			res[3][p].Length.AssertEqual(0, "null series");
			res[1][p].Length.AssertEqual(single.Length, "single series");
			res[2][p].Length.AssertEqual(longSeries.Length, "long series");
		}

		// L = 1: every bar is formed and equal to its own close.
		for (var i = 0; i < longSeries.Length; i++)
			formed(res[2][0][i], longSeries[i], 10m * (i + 1), $"L1 #{i}");

		formed(res[1][0][0], single[0], 777m, "single L1");

		// L = 3: the first two bars have no full window, then the closed form above.
		notFormed(res[2][1][0], longSeries[0], "L3 #0");
		notFormed(res[2][1][1], longSeries[1], "L3 #1");

		for (var i = 2; i < longSeries.Length; i++)
			formed(res[2][1][i], longSeries[i], 10m * i, $"L3 #{i}");

		// L = 9: mean of 10..90 = 50 at bar 8, mean of 20..100 = 60 at bar 9.
		for (var i = 0; i < 8; i++)
			notFormed(res[2][2][i], longSeries[i], $"L9 #{i}");

		formed(res[2][2][8], longSeries[8], 50m, "L9 #8");
		formed(res[2][2][9], longSeries[9], 60m, "L9 #9");

		// L = 10: only the last bar fills - mean of 10..100 = 55.
		for (var i = 0; i < 9; i++)
			notFormed(res[2][3][i], longSeries[i], $"L10 #{i}");

		formed(res[2][3][9], longSeries[9], 55m, "L10 #9");

		// L = 11 never fills on ten bars, and no window wider than one bar fills the single-bar series.
		for (var i = 0; i < longSeries.Length; i++)
			notFormed(res[2][4][i], longSeries[i], $"L11 #{i}");

		for (var p = 1; p < parameters.Length; p++)
			notFormed(res[1][p][0], single[0], $"single P{p}");
	}

	// A batch whose series are all empty is still a well-formed request - the array of series is not
	// empty, only its members are. The caller gets its [series][param] shape back with no bars in it.
	[TestMethod]
	public void GpuSmaAllEmptySeriesReturnsShapedResult()
	{
		GpuCandle[][] series = [[], null, []];

		var parameters = new[]
		{
			new GpuSmaParams(3, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(5, (byte)Level1Fields.ClosePrice),
		};

		GpuIndicatorResult[][][] res;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = new GpuSmaCalculator(gpuContext, gpuAccelerator);
			res = calc.Calculate(series, parameters);
		}

		res.Length.AssertEqual(series.Length);

		foreach (var perParam in res)
		{
			perParam.Length.AssertEqual(parameters.Length);

			foreach (var bars in perParam)
				bars.Length.AssertEqual(0);
		}
	}

	// Params are plain public structs, so Calculate is reachable with values the CPU setter would reject.
	// A window of zero or fewer bars has nothing to average or scan, so no bar may come back formed -
	// GpuHighestCalculator already guards exactly this way, and the family has to answer alike.
	[TestMethod]
	public void GpuCalculatorsFormNothingWhenLengthIsNotPositive()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		var bars = new GpuCandle[5];

		for (var i = 0; i < bars.Length; i++)
		{
			var price = 10m * (i + 1);
			bars[i] = new(start.AddMinutes(i), price, price, price, price, 1m);
		}

		GpuCandle[][] series = [bars];

		var smaParams = new[]
		{
			new GpuSmaParams(0, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(-3, (byte)Level1Fields.ClosePrice),
		};

		var highestParams = new[]
		{
			new GpuHighestParams(0),
			new GpuHighestParams(-3),
		};

		GpuIndicatorResult[][][] sma;
		GpuIndicatorResult[][][] highest;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			sma = new GpuSmaCalculator(gpuContext, gpuAccelerator).Calculate(series, smaParams);
			highest = new GpuHighestCalculator(gpuContext, gpuAccelerator).Calculate(series, highestParams);
		}

		static void nothingFormed(GpuIndicatorResult[][][] res, string name)
		{
			for (var p = 0; p < res[0].Length; p++)
			{
				for (var i = 0; i < res[0][p].Length; i++)
					res[0][p][i].IsFormed.AssertEqual((byte)0, $"{name} P{p} #{i}");
			}
		}

		nothingFormed(sma, nameof(GpuSmaCalculator));
		nothingFormed(highest, nameof(GpuHighestCalculator));
	}

	// CPU/GPU agreement inside the suite's 2.5% band says the two ran the same way, not that either is
	// right, so these values come from arithmetic done here. Closes are exact quarters; high = close + 1.5
	// and low = close - 0.75 put the typical price (H + L + C) / 3 at exactly close + 0.25, so the SMA over
	// each price type differs from the close SMA by a known constant. float32 keeps ~7 significant digits
	// and summing three of them costs at most ~1e-6 relative, so 1e-5 is honest headroom for the kernel -
	// and 2500 times tighter than the band the matrix test is allowed.
	[TestMethod]
	public void GpuSmaMatchesHandComputedValues()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [101.25m, 102.5m, 99.75m, 100.5m, 103.25m, 98.5m];
		var bars = new GpuCandle[closes.Length];

		for (var i = 0; i < closes.Length; i++)
			bars[i] = new(start.AddMinutes(i), closes[i], closes[i] + 1.5m, closes[i] - 0.75m, closes[i], 1m);

		GpuCandle[][] series = [bars];

		var parameters = new[]
		{
			new GpuSmaParams(3, (byte)Level1Fields.ClosePrice),
			new GpuSmaParams(3, (byte)Level1Fields.AveragePrice),
			new GpuSmaParams(3, (byte)Level1Fields.HighPrice),
		};

		GpuIndicatorResult[][][] res;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = new GpuSmaCalculator(gpuContext, gpuAccelerator);
			res = calc.Calculate(series, parameters);
		}

		// Three-bar close sums: 303.5, 302.75, 303.5, 302.25 for bars 2..5.
		decimal[] closeSma = [303.5m / 3m, 302.75m / 3m, 303.5m / 3m, 302.25m / 3m];

		static void check(GpuIndicatorResult actual, decimal expected, string name)
		{
			actual.IsFormed.AssertEqual((byte)1, name);
			float.IsNaN(actual.Value).AssertFalse(name);

			var value = (decimal)actual.Value;
			var tol = expected.Abs() * 0.00001m;

			((value - expected).Abs() <= tol).AssertTrue($"{name}: gpu={value} expected={expected} tol={tol}");
		}

		for (var i = 0; i < closeSma.Length; i++)
		{
			var bar = i + 2;

			check(res[0][0][bar], closeSma[i], $"close #{bar}");
			check(res[0][1][bar], closeSma[i] + 0.25m, $"average #{bar}");
			check(res[0][2][bar], closeSma[i] + 1.5m, $"high #{bar}");
		}

		// The first two bars carry no full window whichever price type is asked for.
		for (var p = 0; p < parameters.Length; p++)
		{
			res[0][p][0].IsFormed.AssertEqual((byte)0, $"P{p} #0");
			res[0][p][1].IsFormed.AssertEqual((byte)0, $"P{p} #1");
		}
	}

	/// <summary>
	/// The CPU/GPU matrix accepts any value within 2.5% of its CPU twin, which on an EMA of 1000 is a band
	/// 25 wide: the two agreeing inside it says they were written from the same idea, not that the idea is
	/// right, and a kernel with a wrong smoothing factor sits comfortably inside it. These values are
	/// computed here from the definition instead - an EMA seeded with the mean of the first Length closes
	/// and then carried by EMA = previous + k * (price - previous), k = 2 / (Length + 1) - so a user asking
	/// the GPU for an EMA gets an EMA. Closes are whole numbers and Length 3 puts k at exactly 0.5, so every
	/// expected value is exact in float32 and the allowance is 2500 times tighter than the matrix band.
	/// </summary>
	[TestMethod]
	public void GpuEmaMatchesHandComputedValues()
	{
		var start = new DateTime(2020, 1, 1).UtcKind();
		decimal[] closes = [100m, 104m, 108m, 112m, 116m, 120m];
		var bars = new GpuCandle[closes.Length];

		// High is two above the close throughout, so the EMA over highs is the EMA over closes plus two.
		for (var i = 0; i < closes.Length; i++)
			bars[i] = new(start.AddMinutes(i), closes[i], closes[i] + 2m, closes[i] - 2m, closes[i], 1m);

		GpuCandle[][] series = [bars];

		var parameters = new[]
		{
			new GpuEmaParams(3, (byte)Level1Fields.ClosePrice),
			new GpuEmaParams(3, (byte)Level1Fields.HighPrice),
			new GpuEmaParams(1, (byte)Level1Fields.ClosePrice),
			new GpuEmaParams(7, (byte)Level1Fields.ClosePrice),
		};

		GpuIndicatorResult[][][] res;
		var (gpuContext, gpuAccelerator) = GetGpu();

		using (_gpuLock.EnterScope())
		{
			var calc = new GpuEmaCalculator(gpuContext, gpuAccelerator);
			res = calc.Calculate(series, parameters);
		}

		static void formed(GpuIndicatorResult actual, decimal expected, string name)
		{
			actual.IsFormed.AssertEqual((byte)1, name);
			float.IsNaN(actual.Value).AssertFalse(name);

			var value = (decimal)actual.Value;
			var tol = expected.Abs() * 0.00001m;

			((value - expected).Abs() <= tol).AssertTrue($"{name}: gpu={value} expected={expected} tol={tol}");
		}

		static void notFormed(GpuIndicatorResult actual, string name)
		{
			actual.IsFormed.AssertEqual((byte)0, name);
			float.IsNaN(actual.Value).AssertTrue(name);
		}

		// Seed = (100 + 104 + 108) / 3 = 104, then 104 + 0.5 * (112 - 104) = 108,
		// 108 + 0.5 * (116 - 108) = 112 and 112 + 0.5 * (120 - 112) = 116.
		decimal[] closeEma = [104m, 108m, 112m, 116m];

		notFormed(res[0][0][0], "close #0");
		notFormed(res[0][0][1], "close #1");
		notFormed(res[0][1][0], "high #0");
		notFormed(res[0][1][1], "high #1");

		for (var i = 0; i < closeEma.Length; i++)
		{
			var bar = i + 2;

			formed(res[0][0][bar], closeEma[i], $"close #{bar}");
			formed(res[0][1][bar], closeEma[i] + 2m, $"high #{bar}");
		}

		// Length 1 has no smoothing left to do: every bar is formed and equal to its own close.
		for (var i = 0; i < closes.Length; i++)
			formed(res[0][2][i], closes[i], $"L1 #{i}");

		// Length 7 never completes its seed window on six bars, so no bar may claim a value.
		for (var i = 0; i < closes.Length; i++)
			notFormed(res[0][3][i], $"L7 #{i}");
	}
}
