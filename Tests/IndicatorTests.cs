namespace StockSharp.Tests;

using System.Text;

/// <summary>
/// Indicator tests. Split by theme into IndicatorTests.*.cs; this part holds the value and comparison helpers they share.
/// </summary>
[TestClass]
public partial class IndicatorTests : BaseTestClass
{
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

	// float32 keeps 24 significant bits, so a single operation rounds at about 1.2e-7 of the magnitude it
	// works on.
	private const decimal _float32Epsilon = 1.1920929e-7m;

	// A windowed kernel rounds once per bar it accumulates, and each bar costs a few roundings beyond that
	// one: reading the candle, the running division, the store back to float32.
	private const decimal _gpuRoundingsPerBar = 8m;

	/// <summary>
	/// Absolute allowance for a GPU (float32) value compared against the CPU (decimal) reference.
	/// </summary>
	/// <param name="scale">Largest magnitude the kernel works on - the prices it reads, or the values it
	/// produces when those are larger.</param>
	/// <param name="window">Number of bars accumulated before a value is produced.</param>
	/// <returns>Largest difference float32 arithmetic can account for.</returns>
	private static decimal GpuTolerance(decimal scale, int window)
		=> _gpuRoundingsPerBar * _float32Epsilon * window.Max(1) * scale.Abs();

	/// <summary>
	/// Largest magnitude a series of indicator values reaches, across every column of every bar.
	/// </summary>
	/// <param name="values">Series to measure.</param>
	/// <returns>Largest absolute value found, or zero when the series carries none.</returns>
	private static decimal ValuesScale(IIndicatorValue[] values)
	{
		ArgumentNullException.ThrowIfNull(values);

		var max = 0m;

		void walk(IEnumerable<object> vals)
		{
			foreach (var v in vals)
			{
				if (v is IEnumerable<object> nested)
					walk(nested);
				else if (v is decimal d)
					max = max.Max(d.Abs());
			}
		}

		foreach (var value in values)
		{
			if (value?.IsFormed == true)
				walk(value.ToValues());
		}

		return max;
	}

	private static void CompareValue(IIndicatorValue actual, IIndicatorValue expected, string indName, bool checkExtended, decimal? gpuTolerance = null)
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
						// A flag is not an approximation: an inverted trend direction is wrong whatever the
						// arithmetic, so it is compared exactly in every tolerance mode.
						b1.AssertEqual((bool)ev, indName);
					}
					else if (av is int i1)
						i1.AssertEqual((int)ev, indName);
					else
					{
						var dA = (decimal)av;
						var dE = (decimal)ev;
						var diff = (dA - dE).Abs();

						if (gpuTolerance is decimal tol)
						{
							// The allowance is absolute and comes from the magnitudes the kernel works on,
							// not from the value in hand: a band taken from the compared value collapses to
							// nothing where a result crosses zero, and inflates without limit where the
							// result is a ratio with a small denominator.
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

	private static void CompareValues(IIndicatorValue[] actual, IIndicatorValue[] expected, string indName, bool checkExtended, decimal? gpuTolerance = null)
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
}
