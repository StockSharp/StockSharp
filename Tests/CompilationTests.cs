namespace StockSharp.Tests;

using System.ComponentModel;
using System.Drawing;

using Ecng.Compilation;
using Ecng.Drawing;
using Ecng.Reflection;

using IronPython.Runtime.Types;

using Microsoft.FSharp.Control;

using StockSharp.Algo.Analytics;
using StockSharp.Algo.Compilation;
using StockSharp.Diagram;

[TestClass]
public class CompilationTests : BaseTestClass
{
	private static readonly string _analyticsFolder = "../../../../Algo.Analytics.{0}";
	private static readonly (string name, byte[] body)[] _noReferenceImages = [];

	private static readonly ReferenceImageCache _defaultReferenceImages = new(static () => CodeExtensions.DefaultReferences);
	private static readonly ReferenceImageCache _analyticsReferenceImages = new(static () => CodeExtensions.CreateAssemblyReferences(
	[
		"StockSharp.Algo.Analytics",
		"MathNet.Numerics"
	]));
	private static readonly ReferenceImageCache _fSharpReferenceImages = new(static () => CodeExtensions.FSharpReferences);

	private sealed class ReferenceImageCache(Func<IEnumerable<AssemblyReference>> getReferences)
	{
		private static readonly Task<(string name, byte[] body)[]> _emptyImages = Task.FromResult(Array.Empty<(string name, byte[] body)>());

		private readonly Lock _sync = new();
		private Task<(string name, byte[] body)[]> _images = _emptyImages;
		private bool _isInitialized;

		public async Task<(string name, byte[] body)[]> GetImages(CancellationToken token)
		{
			Task<(string name, byte[] body)[]> images;
			var observeFailure = false;

			using (_sync.EnterScope())
			{
				if (!_isInitialized)
				{
					_images = LoadImages(getReferences(), token);
					_isInitialized = true;
					observeFailure = true;
				}

				images = _images;
			}

			if (observeFailure)
				_ = ResetOnFailure(images);

			return await images.WaitAsync(token);
		}

		private async Task ResetOnFailure(Task<(string name, byte[] body)[]> images)
		{
			try
			{
				await images;
			}
			catch
			{
				using (_sync.EnterScope())
				{
					if (!ReferenceEquals(_images, images))
						return;

					_images = _emptyImages;
					_isInitialized = false;
				}
			}
		}

		private static async Task<(string name, byte[] body)[]> LoadImages(IEnumerable<AssemblyReference> references, CancellationToken token)
			=> (await references.ToValidRefImages(token)).ToArray();
	}

	private static async Task<(string name, byte[] body)[]> GetReferenceImages(bool includeAnalytics, bool includeFSharp, CancellationToken token)
	{
		var defaultReferences = await _defaultReferenceImages.GetImages(token);

		if (!includeAnalytics && !includeFSharp)
			return defaultReferences;

		var analyticsReferences = Array.Empty<(string name, byte[] body)>();
		var fSharpReferences = Array.Empty<(string name, byte[] body)>();

		if (includeAnalytics)
			analyticsReferences = await _analyticsReferenceImages.GetImages(token);

		if (includeFSharp)
			fSharpReferences = await _fSharpReferenceImages.GetImages(token);

		return [.. defaultReferences, .. analyticsReferences, .. fSharpReferences];
	}

	#region Toolchain

	/// <summary>
	/// Most of this class drives toolchains that are not the product: IronPython, the F# compiler
	/// service, and a writable folder on the real file system for them to unpack into. A machine that
	/// cannot host one of those says nothing about whether StockSharp is correct, and a suite that
	/// shows a missing dependency as failure teaches its readers to ignore red. The distinction is
	/// drawn here rather than left to each test: an absent toolchain is reported inconclusive and
	/// named, while a toolchain that is present has to serve every language compiled below.
	/// </summary>
	[TestMethod]
	public void MissingScriptingToolchainIsReportedAsInconclusiveNotFailure()
	{
		if (AsmInit.ScriptingUnavailableReason is string reason)
			Inconclusive(reason);

		var provider = ServicesRegistry.TryCompilerProvider;

		IsNotNull(provider, $"{nameof(CompilationExtensions)}.{nameof(CompilationExtensions.Init)} registered no compiler provider, so nothing in this class can compile anything.");

		foreach (var fileExtension in new[] { FileExts.CSharp, FileExts.FSharp, FileExts.Python })
			IsTrue(provider.ContainsKey(fileExtension), $"No compiler is registered for '{fileExtension}', so every test below that compiles it fails for a reason that has nothing to do with the code it compiles.");
	}

	/// <summary>
	/// The initializer is given the file system it writes the Python common modules into and the
	/// extra modules to write there. Neither has a sensible stand-in: without them a script that
	/// imports a common module fails at run time with an import error that names nothing, so both are
	/// refused by name instead of quietly defaulted.
	/// </summary>
	[TestMethod]
	public async Task CompilationInitRefusesTheArgumentsItCannotWorkWithout()
	{
		await ThrowsAsync<ArgumentNullException>(() => CompilationExtensions.Init(null, Helper.LogManager.Application, [], CancellationToken));
		await ThrowsAsync<ArgumentNullException>(() => CompilationExtensions.Init(Paths.FileSystem, Helper.LogManager.Application, null, CancellationToken));
	}

	#endregion

	[TestMethod]
	public Task CSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("CSharp"), FileExts.CSharp, CancellationToken);

	[TestMethod]
	public Task FSharpAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("FSharp"), FileExts.FSharp, CancellationToken);

	[TestMethod]
	public Task PythonAnalyticsScripts() => TestAnalyticsScripts(_analyticsFolder.Put("Python"), FileExts.Python, CancellationToken);

	[TestMethod]
	public Task PythonAnalyticsScriptsParallel() => TestAnalyticsScriptsParallel(_analyticsFolder.Put("Python"), FileExts.Python, CancellationToken);

	private static readonly DateTime _analyticsStart = new(2024, 6, 3, 10, 0, 0, DateTimeKind.Utc);

	[TestMethod]
	public async Task PearsonCorrelationMatchesManualFormula()
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		// A = 100,101,102,103,104 (mean 102, dA = -2,-1,0,1,2, sum dA^2 = 10);
		// B = 200,199,202,201,204 (mean 201.2, dB = -1.2,-2.2,0.8,-0.2,2.8, sum dB^2 = 14.8);
		// sum dA*dB = 2.4+2.2+0-0.2+5.6 = 10, so r = 10 / sqrt(10*14.8) = 10 / sqrt(148) = 0.8219949.
		var panel = await RunPearsonCorrelation(FileExts.CSharp, new[]
		{
			(first, new[] { (0, 100m), (1, 101m), (2, 102m), (3, 103m), (4, 104m) }),
			(second, new[] { (0, 200m), (1, 199m), (2, 202m), (3, 201m), (4, 204m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);
		AreEqual(2, matrix.GetLength(0));
		AreEqual(2, matrix.GetLength(1));

		IsTrue(Math.Abs(matrix[0, 0] - 1d) < 1e-9, $"a series correlates with itself, was {matrix[0, 0]}");
		IsTrue(Math.Abs(matrix[1, 1] - 1d) < 1e-9, $"a series correlates with itself, was {matrix[1, 1]}");
		IsTrue(Math.Abs(matrix[0, 1] - 0.8219949d) < 1e-6, $"expected 10/sqrt(148) = 0.8219949, was {matrix[0, 1]}");
		IsTrue(Math.Abs(matrix[1, 0] - matrix[0, 1]) < 1e-12, "correlation is symmetric");
	}

	[TestMethod]
	public async Task PearsonCorrelationPairsCandlesByTime()
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		// Each series misses a different minute, so both hold five closes and no truncation happens;
		// only the moments they share may be paired. Those are minutes 0,1,3,5, where the second
		// series is exactly the first plus 100, so the correlation over matched moments is 1.
		// Pairing by position instead lines 103 up with 202 and 104 with 203 and gives
		// 15.4 / sqrt(17.2*14.8) = 0.96522, which is not a correlation of these instruments.
		var panel = await RunPearsonCorrelation(FileExts.CSharp, new[]
		{
			(first, new[] { (0, 100m), (1, 101m), (3, 103m), (4, 104m), (5, 105m) }),
			(second, new[] { (0, 200m), (1, 201m), (2, 202m), (3, 203m), (5, 205m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);
		AreEqual(2, matrix.GetLength(0));
		AreEqual(2, matrix.GetLength(1));

		IsTrue(Math.Abs(matrix[0, 1] - 1d) < 1e-9, $"closes of the shared minutes are exactly linear, so r must be 1, was {matrix[0, 1]}");
		IsTrue(Math.Abs(matrix[1, 0] - matrix[0, 1]) < 1e-12, "correlation is symmetric");
	}

	[TestMethod]
	public async Task PearsonCorrelationHeatmapTitlesNameSecurities()
	{
		var token = CancellationToken;

		var securities = new[] { Helper.CreateSecurityId(), Helper.CreateSecurityId(), Helper.CreateSecurityId() };

		// Every heatmap cell is read through its titles, so both axes must name the instruments
		// in the order they were passed in, and the matrix must be square over the same set.
		var panel = await RunPearsonCorrelation(FileExts.CSharp, new[]
		{
			(securities[0], new[] { (0, 100m), (1, 101m), (2, 103m) }),
			(securities[1], new[] { (0, 200m), (1, 204m), (2, 203m) }),
			(securities[2], new[] { (0, 300m), (1, 299m), (2, 305m) }),
		}, token);

		var expected = securities.Select(s => s.ToStringId()).ToArray();

		IsNotNull(panel.HeatmapXTitles);
		IsNotNull(panel.HeatmapYTitles);

		AreEqual(expected.Length, panel.HeatmapXTitles.Length);
		AreEqual(expected.Length, panel.HeatmapYTitles.Length);

		for (var i = 0; i < expected.Length; i++)
		{
			AreEqual(expected[i], panel.HeatmapXTitles[i], $"x title {i}");
			AreEqual(expected[i], panel.HeatmapYTitles[i], $"y title {i}");
		}

		AreEqual(expected.Length, panel.HeatmapData.GetLength(0));
		AreEqual(expected.Length, panel.HeatmapData.GetLength(1));
	}

	[TestMethod]
	public async Task PearsonCorrelationConstantSeriesIsUndefined()
	{
		var token = CancellationToken;

		var flat = Helper.CreateSecurityId();
		var rising = Helper.CreateSecurityId();

		// Pearson divides by the deviation of each series; a series that never moves has zero
		// deviation, so the coefficient is 0/0 and must not be reported as an actual number.
		var panel = await RunPearsonCorrelation(FileExts.CSharp, new[]
		{
			(flat, new[] { (0, 100m), (1, 100m), (2, 100m), (3, 100m), (4, 100m) }),
			(rising, new[] { (0, 100m), (1, 101m), (2, 102m), (3, 103m), (4, 104m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);

		IsTrue(double.IsNaN(matrix[0, 1]), $"correlation with a flat series is undefined, was {matrix[0, 1]}");
		IsTrue(double.IsNaN(matrix[1, 0]), $"correlation with a flat series is undefined, was {matrix[1, 0]}");
	}

	[TestMethod]
	public async Task PearsonCorrelationCancelledBeforeFirstSecurityReportsCancellation()
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		using var source = CancellationTokenSource.CreateLinkedTokenSource(token);
		await source.CancelAsync();

		var series = new[]
		{
			(first, new[] { (0, 100m), (1, 101m), (2, 102m) }),
			(second, new[] { (0, 200m), (1, 199m), (2, 202m) }),
		};

		// A run that stops because of its own token must say so: OperationCanceledException is the
		// only way its caller can tell "you cancelled me" from "I finished and there is nothing to
		// show". A token cancelled before the first instrument is read leaves the script with no
		// closes at all, and neither a silent return nor a failure about something else will do.
		await ThrowsAsync<OperationCanceledException>(() => RunPearsonCorrelation(FileExts.CSharp, series, Helper.LogManager.Application, token, source.Token));
	}

	[TestMethod]
	public async Task PearsonCorrelationCancelledBetweenSecuritiesReportsCancellation()
	{
		var token = CancellationToken;

		var securities = new[] { Helper.CreateSecurityId(), Helper.CreateSecurityId(), Helper.CreateSecurityId() };

		using var source = CancellationTokenSource.CreateLinkedTokenSource(token);

		// Every instrument here holds three candles of one date, so the script writes exactly two
		// messages per instrument: the announcement and one date. The third message is therefore
		// the announcement of the second instrument, a fixed point partway through the loop.
		var logs = new CancelOnLogCountReceiver(3, source);

		var series = new[]
		{
			(securities[0], new[] { (0, 100m), (1, 101m), (2, 102m) }),
			(securities[1], new[] { (0, 200m), (1, 199m), (2, 202m) }),
			(securities[2], new[] { (0, 300m), (1, 305m), (2, 301m) }),
		};

		// Cancelling partway must be reported, and must not leave a heatmap behind: a matrix over
		// the instruments that were read while both axes are titled with all three cannot be
		// addressed at all, so a partial correlation is worse than none.
		await ThrowsAsync<OperationCanceledException>(() => RunPearsonCorrelation(FileExts.CSharp, series, logs, token, source.Token));
	}

	[TestMethod]
	public async Task NormalizePriceRebasesEverySecurityOnItsOwnFirstClose()
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		// Instruments are put on one chart so their shapes can be compared, which only works if each
		// is divided by its own first close rather than by a shared one: 50,100,25 -> 1, 2, 0.5 and
		// 200,150 -> 1, 0.75. Series are titled by instrument and keep the candle open times.
		var panel = await RunAnalyticsScript(FileExts.CSharp, "NormalizePriceScript", new[]
		{
			(first, new[] { Flat(0, 50m), Flat(1, 100m), Flat(2, 25m) }),
			(second, new[] { Flat(0, 200m), Flat(1, 150m) }),
		}, Helper.LogManager.Application, token, token);

		AreEqual(1, panel.Charts.Count, "all instruments share one chart");

		var chart = panel.ChartAt<DateTime, decimal, VoidType>(0);

		AreEqual(2, chart.Series.Count);

		var firstSeries = chart.Series[0];
		AreEqual(first.ToStringId(), firstSeries.Title);
		AreSequenceEqual(new[] { _analyticsStart, _analyticsStart.AddMinutes(1), _analyticsStart.AddMinutes(2) }, firstSeries.XValues, "first x");
		AreSequenceEqual(new[] { 1m, 2m, 0.5m }, firstSeries.YValues, "first y");

		var secondSeries = chart.Series[1];
		AreEqual(second.ToStringId(), secondSeries.Title);
		AreSequenceEqual(new[] { _analyticsStart, _analyticsStart.AddMinutes(1) }, secondSeries.XValues, "second x");
		AreSequenceEqual(new[] { 1m, 0.75m }, secondSeries.YValues, "second y");
	}

	[TestMethod]
	public async Task NormalizePriceZeroBaseDoesNotDisturbTheNextSecurity()
	{
		var token = CancellationToken;

		var zeroBase = Helper.CreateSecurityId();
		var normal = Helper.CreateSecurityId();

		var panel = await RunAnalyticsScript(FileExts.CSharp, "NormalizePriceScript", new[]
		{
			(zeroBase, new[] { Flat(0, 0m), Flat(1, 10m), Flat(2, 20m) }),
			(normal, new[] { Flat(0, 50m), Flat(1, 100m), Flat(2, 25m) }),
		}, Helper.LogManager.Application, token, token);

		var chart = panel.ChartAt<DateTime, decimal, VoidType>(0);

		AreEqual(2, chart.Series.Count, "a series is drawn for every instrument asked for");

		// An instrument with no usable base carries no points (see
		// NormalizePriceReportsNothingOnZeroFirstClose), and above all no raw prices: a chart
		// showing 0, 10 or 20 here would be presenting unnormalized data under a normalized title.
		var zeroSeries = chart.Series[0];
		AreEqual(zeroBase.ToStringId(), zeroSeries.Title);
		AreEqual(0, zeroSeries.YValues.Length, $"a zero base defines no normalized value, plotted {string.Join(", ", zeroSeries.YValues)}");

		// The base is per instrument, so a rejected one stays the problem of its own series: the
		// next instrument is normalized exactly as if the bad one were not in the list.
		var normalSeries = chart.Series[1];
		AreEqual(normal.ToStringId(), normalSeries.Title);
		AreSequenceEqual(new[] { _analyticsStart, _analyticsStart.AddMinutes(1), _analyticsStart.AddMinutes(2) }, normalSeries.XValues, "normal x");
		AreSequenceEqual(new[] { 1m, 2m, 0.5m }, normalSeries.YValues, "normal y");
	}

	[TestMethod]
	public async Task TimeVolumeSumsVolumeByHourOfDayAcrossDays()
	{
		var token = CancellationToken;

		var security = Helper.CreateSecurityId();

		// The fixture starts at 10:00 UTC. Minutes 0 and 30 fall in hour 10 and minute 75 in hour
		// 11; minute 1445 is 10:05 of the next day and minute 1560 is 12:00 of it. Grouping by hour
		// of day collapses the two days, so the expected totals are 10:00 -> 5+3+4 = 12,
		// 11:00 -> 10 and 12:00 -> 1.
		var panel = await RunAnalyticsScript(FileExts.CSharp, "TimeVolumeScript", new[]
		{
			(security, new[]
			{
				Bar(0, 100m, 100m, 5m),
				Bar(30, 100m, 100m, 3m),
				Bar(75, 100m, 100m, 10m),
				Bar(1445, 100m, 100m, 4m),
				Bar(1560, 100m, 100m, 1m),
			}),
		}, Helper.LogManager.Application, token, token);

		var grid = panel.SingleGrid;

		AreSequenceEqual(new[] { "Time", "Volume" }, grid.Columns.ToArray(), "columns");
		AreEqual(3, grid.Rows.Count, "one row per hour of day that traded");

		var byHour = grid.Rows.ToDictionary(r => (TimeSpan)r[0], r => (decimal)r[1]);

		AreEqual(12m, byHour[TimeSpan.FromHours(10)]);
		AreEqual(10m, byHour[TimeSpan.FromHours(11)]);
		AreEqual(1m, byHour[TimeSpan.FromHours(12)]);

		// The grid is meant to show the busiest hours first, so it has to be told to sort on the
		// volume column, descending - the order the rows happen to be inserted in is not an answer.
		IsTrue(grid.IsSorted, "the grid was never told how to sort");
		AreEqual("Volume", grid.SortColumn);
		IsFalse(grid.SortAsc, "the biggest volume must come first");
	}

	[TestMethod]
	public async Task PriceVolumeAccumulatesVolumeAtCandleMidPrice()
	{
		var token = CancellationToken;

		var security = Helper.CreateSecurityId();

		// Mid price is low + (high - low) / 2: (100,104) and (101,103) both give 102, so their
		// volumes join into 5 + 7 = 12 on that one level; (110,110) is a zero range candle sitting
		// at 110 with 3; (200,210) gives 205 with 4. Three levels, told apart by price alone.
		var panel = await RunAnalyticsScript(FileExts.CSharp, "PriceVolumeScript", new[]
		{
			(security, new[]
			{
				Bar(0, 100m, 104m, 5m),
				Bar(1, 101m, 103m, 7m),
				Bar(2, 110m, 110m, 3m),
				Bar(3, 200m, 210m, 4m),
			}),
		}, Helper.LogManager.Application, token, token);

		var series = panel.ChartAt<decimal, decimal, VoidType>(0).SingleSeries;

		AreEqual(security.ToStringId(), series.Title);
		AreEqual(DrawStyles.Histogram, series.Style, "a volume distribution is drawn as bars");
		AreEqual(3, series.XValues.Length, "candles sharing a mid price are one level");

		var byLevel = series.XValues.Select((x, i) => (x, y: series.YValues[i])).ToDictionary(p => p.x, p => p.y);

		AreEqual(12m, byLevel[102m]);
		AreEqual(3m, byLevel[110m]);
		AreEqual(4m, byLevel[205m]);
	}

	[TestMethod]
	public async Task BiggestCandlePicksMaxLengthAndMaxVolumePerSecurity()
	{
		var token = CancellationToken;

		var withData = Helper.CreateSecurityId();
		var single = Helper.CreateSecurityId();
		var empty = Helper.CreateSecurityId();

		// For the first instrument the longest candle is minute 1 (112-100 = 12, against 10 and 1)
		// while the heaviest is minute 2 (9, against 5 and 2), so the two charts must point at
		// different candles. The second instrument has one candle, which is both. The third has no
		// data at all and can contribute no point.
		var panel = await RunAnalyticsScript(FileExts.CSharp, "BiggestCandleScript", new[]
		{
			(withData, new[]
			{
				Bar(0, 100m, 110m, 5m),
				Bar(1, 100m, 112m, 2m),
				Bar(2, 100m, 101m, 9m),
			}),
			(single, new[] { Bar(0, 50m, 58m, 4m) }),
			(empty, Array.Empty<FixtureCandle>()),
		}, Helper.LogManager.Application, token, token);

		// Price chart: x is the candle time, y its middle price (low + length / 2), z its length.
		var prices = panel.ChartAt<DateTime, decimal, decimal>(0).SingleSeries;

		AreSequenceEqual(new[] { _analyticsStart.AddMinutes(1), _analyticsStart }, prices.XValues, "price x");
		AreSequenceEqual(new[] { 106m, 54m }, prices.YValues, "price y");
		AreSequenceEqual(new[] { 12m, 8m }, prices.ZValues, "price z");

		// Volume chart: the same instruments in the same order, each at its heaviest candle.
		var volumes = panel.ChartAt<DateTime, decimal, decimal>(1).SingleSeries;

		AreSequenceEqual(new[] { _analyticsStart.AddMinutes(2), _analyticsStart }, volumes.XValues, "volume x");
		AreSequenceEqual(new[] { 9m, 4m }, volumes.YValues, "volume y");
		AreSequenceEqual(new[] { 9m, 4m }, volumes.ZValues, "volume z");
	}

	[TestMethod]
	public async Task BiggestCandleReportsTiedAndFlatSecurities()
	{
		var token = CancellationToken;

		var tied = Helper.CreateSecurityId();
		var flat = Helper.CreateSecurityId();

		// Both candles of the first instrument are 4 long and carry 7, so the maximum is a tie: the
		// answer is still exactly one candle, whichever of them, and its length and volume are the
		// maxima. The second instrument never moves and never trades, but the biggest candle of
		// nothing much is still a candle - it must not vanish the way one with no data does.
		var panel = await RunAnalyticsScript(FileExts.CSharp, "BiggestCandleScript", new[]
		{
			(tied, new[] { Bar(0, 20m, 24m, 7m), Bar(1, 30m, 34m, 7m) }),
			(flat, new[] { Bar(0, 50m, 50m, 0m) }),
		}, Helper.LogManager.Application, token, token);

		var prices = panel.ChartAt<DateTime, decimal, decimal>(0).SingleSeries;

		AreEqual(2, prices.XValues.Length, "one point per instrument, ties included");
		AreSequenceEqual(new[] { 4m, 0m }, prices.ZValues, "price z");

		// Whichever tied candle was chosen, the middle price plotted has to be that candle's:
		// 22 for the one at minute 0 (20 + 4/2), 32 for the one at minute 1 (30 + 4/2).
		var tiedIsFirst = prices.XValues[0] == _analyticsStart;
		IsTrue(tiedIsFirst || prices.XValues[0] == _analyticsStart.AddMinutes(1), $"a tie must resolve to one of the tied candles, was {prices.XValues[0]:O}");
		AreEqual(tiedIsFirst ? 22m : 32m, prices.YValues[0]);

		AreEqual(_analyticsStart, prices.XValues[1]);
		AreEqual(50m, prices.YValues[1], "a zero range candle sits on its own price");

		var volumes = panel.ChartAt<DateTime, decimal, decimal>(1).SingleSeries;

		AreEqual(2, volumes.XValues.Length, "one point per instrument, ties included");
		AreSequenceEqual(new[] { 7m, 0m }, volumes.YValues, "volume y");
		AreSequenceEqual(new[] { 7m, 0m }, volumes.ZValues, "volume z");
	}

	// Compares a produced series against values worked out by hand, naming the index that differs.
	private static void AreSequenceEqual<T>(T[] expected, T[] actual, string what)
	{
		AreEqual(expected.Length, actual.Length, $"{what}: expected {expected.Length} values, got {actual.Length} ({string.Join(", ", actual)})");

		for (var i = 0; i < expected.Length; i++)
			AreEqual(expected[i], actual[i], $"{what}[{i}]");
	}

	// A candle whose open, high, low and close are all the same price - enough for the scripts that
	// read closes only.
	private static FixtureCandle Flat(int minute, decimal close)
		=> new(minute, close, close, close, close, 1m);

	// A candle spanning low..high, opening at the low and closing at the high.
	private static FixtureCandle Bar(int minute, decimal low, decimal high, decimal volume)
		=> new(minute, low, high, low, high, volume);

	// The F# copy of the Pearson script is a separate implementation, so the number it reports has
	// to be demanded of it in its own right. Same closes and same hand-worked r as the C# test:
	// A = 100,101,102,103,104 (mean 102, dA = -2,-1,0,1,2, sum dA^2 = 10);
	// B = 200,199,202,201,204 (mean 201.2, dB = -1.2,-2.2,0.8,-0.2,2.8, sum dB^2 = 14.8);
	// sum dA*dB = 2.4+2.2+0-0.2+5.6 = 10, so r = 10 / sqrt(148) = 0.8219949.
	[TestMethod]
	public Task PearsonCorrelationMatchesManualFormulaFSharp() => PearsonCorrelationMatchesManualFormula(FileExts.FSharp);

	// The Python copy runs through NumpyDotNet and converts decimal closes to double on the way,
	// so it needs the same independent check as the compiled copies.
	[TestMethod]
	public Task PearsonCorrelationMatchesManualFormulaPython() => PearsonCorrelationMatchesManualFormula(FileExts.Python);

	private async Task PearsonCorrelationMatchesManualFormula(string fileExtension)
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		var panel = await RunPearsonCorrelation(fileExtension, new[]
		{
			(first, new[] { (0, 100m), (1, 101m), (2, 102m), (3, 103m), (4, 104m) }),
			(second, new[] { (0, 200m), (1, 199m), (2, 202m), (3, 201m), (4, 204m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);
		AreEqual(2, matrix.GetLength(0));
		AreEqual(2, matrix.GetLength(1));

		IsTrue(Math.Abs(matrix[0, 0] - 1d) < 1e-9, $"a series correlates with itself, was {matrix[0, 0]}");
		IsTrue(Math.Abs(matrix[1, 1] - 1d) < 1e-9, $"a series correlates with itself, was {matrix[1, 1]}");
		IsTrue(Math.Abs(matrix[0, 1] - 0.8219949d) < 1e-6, $"expected 10/sqrt(148) = 0.8219949, was {matrix[0, 1]}");
		IsTrue(Math.Abs(matrix[1, 0] - matrix[0, 1]) < 1e-12, "correlation is symmetric");
	}

	[TestMethod]
	public Task PearsonCorrelationPairsCandlesByTimeFSharp() => PearsonCorrelationPairsCandlesByTime(FileExts.FSharp);

	[TestMethod]
	public Task PearsonCorrelationPairsCandlesByTimePython() => PearsonCorrelationPairsCandlesByTime(FileExts.Python);

	private async Task PearsonCorrelationPairsCandlesByTime(string fileExtension)
	{
		var token = CancellationToken;

		var first = Helper.CreateSecurityId();
		var second = Helper.CreateSecurityId();

		// Each series misses a different minute, so both hold five closes and no truncation happens;
		// only the moments they share may be paired. Those are minutes 0,1,3,5, where the second
		// series is exactly the first plus 100, so the correlation over matched moments is 1.
		// Pairing by position instead lines 103 up with 202 and 104 with 203 and gives
		// 15.4 / sqrt(17.2*14.8) = 0.96522, which is not a correlation of these instruments.
		var panel = await RunPearsonCorrelation(fileExtension, new[]
		{
			(first, new[] { (0, 100m), (1, 101m), (3, 103m), (4, 104m), (5, 105m) }),
			(second, new[] { (0, 200m), (1, 201m), (2, 202m), (3, 203m), (5, 205m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);
		AreEqual(2, matrix.GetLength(0));
		AreEqual(2, matrix.GetLength(1));

		IsTrue(Math.Abs(matrix[0, 1] - 1d) < 1e-9, $"closes of the shared minutes are exactly linear, so r must be 1, was {matrix[0, 1]}");
		IsTrue(Math.Abs(matrix[1, 0] - matrix[0, 1]) < 1e-12, "correlation is symmetric");
	}

	// Correlation with a series that never moves divides by a zero deviation. NumpyDotNet is a
	// different engine from MathNet, so the Python copy has to be asked the same question: it must
	// not turn 0/0 into a number a reader would take for a real correlation.
	[TestMethod]
	public async Task PearsonCorrelationConstantSeriesIsUndefinedPython()
	{
		var token = CancellationToken;

		var flat = Helper.CreateSecurityId();
		var rising = Helper.CreateSecurityId();

		var panel = await RunPearsonCorrelation(FileExts.Python, new[]
		{
			(flat, new[] { (0, 100m), (1, 100m), (2, 100m), (3, 100m), (4, 100m) }),
			(rising, new[] { (0, 100m), (1, 101m), (2, 102m), (3, 103m), (4, 104m) }),
		}, token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);

		IsTrue(double.IsNaN(matrix[0, 1]), $"correlation with a flat series is undefined, was {matrix[0, 1]}");
		IsTrue(double.IsNaN(matrix[1, 0]), $"correlation with a flat series is undefined, was {matrix[1, 0]}");
	}

	[TestMethod]
	public Task NormalizePriceIsRatioToFirstCloseFSharp() => NormalizePriceIsRatioToFirstClose(FileExts.FSharp);

	[TestMethod]
	public Task NormalizePriceIsRatioToFirstClosePython() => NormalizePriceIsRatioToFirstClose(FileExts.Python);

	// The F# and Python copies are separate implementations of the algorithm the C# tests above
	// pin, so the same arithmetic has to be demanded of them directly. The normalized value of a
	// candle is its close over the first close of the series, so closes 100,110,90 are worth
	// exactly 1, 110/100 = 1.1 and 90/100 = 0.9, plotted at the minute each candle opened.
	private async Task NormalizePriceIsRatioToFirstClose(string fileExtension)
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		var panel = await RunNormalizePrice(fileExtension, secId, [Flat(0, 100m), Flat(1, 110m), Flat(2, 90m)], Helper.LogManager.Application, token, token);

		var series = panel.ChartAt<DateTime, decimal, VoidType>(0).SingleSeries;

		AreEqual(secId.ToStringId(), series.Title, "the series is named after the instrument it normalizes");

		AreSequenceEqual(new[] { _analyticsStart, _analyticsStart.AddMinutes(1), _analyticsStart.AddMinutes(2) }, series.XValues, "x");
		AreSequenceEqual(new[] { 1m, 1.1m, 0.9m }, series.YValues, "y");
	}

	// A first close of zero leaves the series without a base: neither 0/0 nor 10/0 defines a
	// normalized value. All three implementations therefore draw the instrument's empty series,
	// consistent with the multi-security C# case above, instead of inventing a ratio of one.
	[TestMethod]
	public async Task NormalizePriceZeroBaseAgreesAcrossLanguages()
	{
		var token = CancellationToken;

		var results = new List<(string language, DateTime[] x, decimal[] y)>();

		foreach (var fileExtension in new[] { FileExts.CSharp, FileExts.FSharp, FileExts.Python })
		{
			var secId = Helper.CreateSecurityId();

			var panel = await RunNormalizePrice(fileExtension, secId, [Flat(0, 0m), Flat(1, 10m)], Helper.LogManager.Application, token, token);

			var chart = panel.ChartAt<DateTime, decimal, VoidType>(0);

			// Drawing nothing and drawing an empty series say the same thing to a chart reader.
			var x = chart.Series.Count == 0 ? Array.Empty<DateTime>() : chart.Series[0].XValues;
			var y = chart.Series.Count == 0 ? Array.Empty<decimal>() : chart.Series[0].YValues;

			results.Add((fileExtension, x, y));
		}

		foreach (var (language, x, y) in results)
		{
			AreEqual(0, x.Length, $"{language} assigned a time to a value that cannot be normalized");
			AreEqual(0, y.Length, $"{language} plotted values without a non-zero normalization base: {string.Join(", ", y)}");
		}
	}

	[TestMethod]
	public Task NormalizePriceStopsReadingWhenCancelledCSharp() => NormalizePriceStopsReadingWhenCancelled(FileExts.CSharp);

	[TestMethod]
	public Task NormalizePriceStopsReadingWhenCancelledFSharp() => NormalizePriceStopsReadingWhenCancelled(FileExts.FSharp);

	[TestMethod]
	public Task NormalizePriceStopsReadingWhenCancelledPython() => NormalizePriceStopsReadingWhenCancelled(FileExts.Python);

	// Cancellation between instruments is already pinned above; this is the other half of the
	// promise. A caller who cancels while the candles of one instrument are being read is entitled
	// to have that read stop, so the run must come back as a cancellation and not as a finished
	// calculation over all four candles. The cancel is fired from the script's second log line -
	// the date of the candle it has just been handed - so it lands at the same point of the loop
	// on every run instead of racing a timer.
	private async Task NormalizePriceStopsReadingWhenCancelled(string fileExtension)
	{
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		using var cts = new CancellationTokenSource();

		// The scripts announce the instrument first and the date of each candle after it, and all
		// three format that date differently, so the message is counted rather than matched.
		var logs = new CancelOnLogCountReceiver(2, cts);

		await ThrowsAsync<OperationCanceledException>(() => RunNormalizePrice(fileExtension, secId, [Flat(0, 100m), Flat(1, 110m), Flat(2, 120m), Flat(3, 130m)], logs, token, cts.Token));
	}

	// The F# TaskSeq consumer and the synchronous Python bridge choose the token passed to
	// GetAsyncEnumerator. Checking the token only after a candle arrives cannot interrupt a reader
	// that is still waiting for that candle, so both consumers are exercised against a storage that
	// waits inside MoveNextAsync.
	[TestMethod]
	public Task FSharpNormalizeCancelsPendingStorageRead()
		=> AnalyticsCancelsPendingStorageRead(FileExts.FSharp, "NormalizePriceScript");

	[TestMethod]
	public Task FSharpPearsonCancelsPendingStorageRead()
		=> AnalyticsCancelsPendingStorageRead(FileExts.FSharp, "PearsonCorrelationScript");

	[TestMethod]
	public Task PythonNormalizeCancelsPendingStorageRead()
		=> AnalyticsCancelsPendingStorageRead(FileExts.Python, "NormalizePriceScript");

	[TestMethod]
	public Task PythonPearsonCancelsPendingStorageRead()
		=> AnalyticsCancelsPendingStorageRead(FileExts.Python, "PearsonCorrelationScript");

	private async Task AnalyticsCancelsPendingStorageRead(string fileExtension, string scriptName)
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();
		var dataType = TimeSpan.FromMinutes(1).TimeFrame();
		var candleStorage = new BlockingCandleStorage(secId, dataType);
		var registry = new Mock<IStorageRegistry>();

		registry
			.Setup(r => r.GetCandleMessageStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
			.Returns(candleStorage);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

		var run = RunAnalyticsScriptAgainstStorage(
			fileExtension,
			scriptName,
			[secId],
			registry.Object,
			Mock.Of<IMarketDataDrive>(),
			StorageFormats.Csv,
			dataType,
			_analyticsStart.Date,
			_analyticsStart.Date.AddDays(1),
			Helper.LogManager.Application,
			token,
			cts.Token);

		try
		{
			await candleStorage.ReadStarted.WaitAsync(TimeSpan.FromSeconds(30), token);
			await cts.CancelAsync();
			await ThrowsAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5), token));
		}
		finally
		{
			// Let an implementation that failed to pass the token leave its blocked read, so a
			// failing assertion does not also leak a compiler context or a background operation.
			candleStorage.Release();

			try
			{
				await run.WaitAsync(TimeSpan.FromSeconds(5), token);
			}
			catch (OperationCanceledException)
			{
			}
		}

		AreEqual(1, candleStorage.Released, "the pending storage reader must be disposed exactly once");
	}

	// Runs the shipped normalize script over one instrument, so the plotted values can be checked
	// against ratios worked out by hand.
	private static Task<TestAnalyticsPanel> RunNormalizePrice(string fileExtension, SecurityId secId, FixtureCandle[] candles, ILogReceiver logs, CancellationToken setupToken, CancellationToken runToken)
		=> RunAnalyticsScript(fileExtension, "NormalizePriceScript", [(secId, candles)], logs, setupToken, runToken);

	// Cancels the run once the script has written a given number of log messages, for the cases
	// where the text of the message differs between language copies.
	private sealed class CancelOnLogCountReceiver : TestLogReceiver
	{
		public CancelOnLogCountReceiver(int cancelAfter, CancellationTokenSource cts)
		{
			(cancelAfter > 0).AssertTrue();
			cts.AssertNotNull();

			var count = 0;

			Log += _ =>
			{
				if (++count == cancelAfter)
					cts.Cancel();
			};
		}
	}

	// Reads one candle out of the storage and walks away, so the test can see what the Python
	// bridge does with the reader it opened.
	private const string _pythonBreakAfterFirstSource = """
		import clr

		clr.AddReference("StockSharp.Algo.Analytics")

		from System.Threading.Tasks import Task
		from StockSharp.Algo.Analytics import IAnalyticsScript
		from storage_extensions import *

		class break_after_first_script(IAnalyticsScript):
		    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
		        candle_storage = get_candle_storage(storage, securities[0], time_frame, drive, format)
		        candles = iter_candles(candle_storage, from_date, to_date, cancellation_token)
		        try:
		            for candle in candles:
		                break
		        finally:
		            candles.close()
		        return Task.CompletedTask
		""";

	// Reads the storage to the end, so a storage that fails partway shows what the bridge does on
	// the way out.
	private const string _pythonReadAllSource = """
		import clr

		clr.AddReference("StockSharp.Algo.Analytics")

		from System.Threading.Tasks import Task
		from StockSharp.Algo.Analytics import IAnalyticsScript
		from storage_extensions import *

		class read_all_script(IAnalyticsScript):
		    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
		        candle_storage = get_candle_storage(storage, securities[0], time_frame, drive, format)
		        for candle in iter_candles(candle_storage, from_date, to_date, cancellation_token):
		            pass
		        return Task.CompletedTask
		""";

	// Hands nx.to2darray a rectangular but non-square block with a distinct value in every cell and
	// draws the result, so the test can read back where each value landed.
	private const string _pythonTo2dArraySource = """
		import clr

		clr.AddReference("StockSharp.Algo.Analytics")

		from System.Threading.Tasks import Task
		from StockSharp.Algo.Analytics import IAnalyticsScript
		from numpy_extensions import nx

		class to2darray_script(IAnalyticsScript):
		    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
		        rows = [[1.0, 2.0, float("nan")], [4.0, 5.0, 6.0]]
		        result = nx.to2darray(rows)
		        panel.DrawHeatmap([str(i) for i in range(result.GetLength(0))], [str(j) for j in range(result.GetLength(1))], result)
		        return Task.CompletedTask
		""";

	// The same, with a second row longer than the first.
	private const string _pythonRaggedTo2dArraySource = """
		import clr

		clr.AddReference("StockSharp.Algo.Analytics")

		from System.Threading.Tasks import Task
		from StockSharp.Algo.Analytics import IAnalyticsScript
		from numpy_extensions import nx

		class ragged_to2darray_script(IAnalyticsScript):
		    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
		        rows = [[1.0, 2.0], [3.0, 4.0, 5.0]]
		        result = nx.to2darray(rows)
		        panel.DrawHeatmap([str(i) for i in range(result.GetLength(0))], [str(j) for j in range(result.GetLength(1))], result)
		        return Task.CompletedTask
		""";

	// A first row with zero columns still defines the expected width. A later non-empty row is
	// ragged and must not disappear behind the helper's empty-first-row shortcut.
	private const string _pythonEmptyFirstRaggedTo2dArraySource = """
		import clr

		clr.AddReference("StockSharp.Algo.Analytics")

		from System.Threading.Tasks import Task
		from StockSharp.Algo.Analytics import IAnalyticsScript
		from numpy_extensions import nx

		class empty_first_ragged_to2darray_script(IAnalyticsScript):
		    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
		        nx.to2darray([[], [5.0]])
		        return Task.CompletedTask
		""";

	// The bridge between a Python script and a .NET IAsyncEnumerable opens an enumerator over the
	// storage, so it owns closing it once the caller closes the Python generator. Python does not
	// promise to close a generator merely because a for-loop breaks; the inline consumer therefore
	// uses the supported try/finally + close pattern and must not pull a second candle while doing so.
	[TestMethod]
	public async Task PythonIterCandlesReleasesTheReaderWhenTheConsumerStops()
	{
		var token = CancellationToken;

		var (secId, dataType, candleStorage) = CreateCountingCandles(4, -1);

		await RunInlinePythonScript("break_after_first_script", _pythonBreakAfterFirstSource, secId, dataType, candleStorage, token);

		AreEqual(1, candleStorage.Read, "stopping after the first candle must not pull a second one");
		AreEqual(1, candleStorage.Released, "the storage reader must be closed exactly once when the consumer stops");
	}

	// A storage that fails partway must not be left open either: the failure has to reach the
	// caller instead of being swallowed, and the reader has to be closed on the way out.
	[TestMethod]
	public async Task PythonIterCandlesReleasesTheReaderWhenTheStorageFails()
	{
		var token = CancellationToken;

		var (secId, dataType, candleStorage) = CreateCountingCandles(4, 1);

		Exception caught = null;

		try
		{
			await RunInlinePythonScript("read_all_script", _pythonReadAllSource, secId, dataType, candleStorage, token);
		}
		catch (Exception ex)
		{
			caught = ex;
		}

		IsNotNull(caught, "a storage that fails must not look to the caller like a storage that ran out of data");
		caught.ToString().AssertContains("the storage went away", "the storage failure must reach the caller; a later cleanup error is not an acceptable substitute");
		AreEqual(1, candleStorage.Read, "only the candle before the failure can be delivered");
		AreEqual(1, candleStorage.Released, "the storage reader must be closed exactly once when the read fails");
	}

	// nx.to2darray is what turns rows of numbers into the matrix a heatmap is drawn from, so every
	// value has to land at its own coordinate: [[1,2,NaN],[4,5,6]] is two rows of three read row by
	// row, not its transpose. NaN is a value in its own right here - it is how an undefined
	// correlation reaches the chart - so it has to survive the conversion rather than become zero.
	[TestMethod]
	public async Task NumpyTo2dArrayKeepsEveryValueAtItsCoordinate()
	{
		var token = CancellationToken;

		var panel = await RunInlinePythonScript("to2darray_script", _pythonTo2dArraySource, Helper.CreateSecurityId(), TimeSpan.FromMinutes(1).TimeFrame(), Mock.Of<IMarketDataStorage<CandleMessage>>(), token);

		var matrix = panel.HeatmapData;

		IsNotNull(matrix);
		AreEqual(2, matrix.GetLength(0), "two rows in, two rows out");
		AreEqual(3, matrix.GetLength(1), "three values per row in, three columns out");

		AreEqual(1d, matrix[0, 0]);
		AreEqual(2d, matrix[0, 1]);
		IsTrue(double.IsNaN(matrix[0, 2]), $"NaN must survive the conversion, was {matrix[0, 2]}");
		AreEqual(4d, matrix[1, 0]);
		AreEqual(5d, matrix[1, 1]);
		AreEqual(6d, matrix[1, 2]);
	}

	// A rectangular System.Array cannot represent ragged rows without inventing missing values.
	// The helper therefore rejects them explicitly; taking the width from the first row would
	// silently discard values while returning a matrix that looks complete.
	[TestMethod]
	public async Task NumpyTo2dArrayDoesNotDropRaggedValuesSilently()
	{
		var token = CancellationToken;

		Exception caught = null;

		try
		{
			await RunInlinePythonScript("ragged_to2darray_script", _pythonRaggedTo2dArraySource, Helper.CreateSecurityId(), TimeSpan.FromMinutes(1).TimeFrame(), Mock.Of<IMarketDataStorage<CandleMessage>>(), token);
		}
		catch (Exception ex)
		{
			caught = ex;
		}

		IsNotNull(caught, "ragged rows were accepted and their extra value was silently discarded");
		caught.ToString().AssertContains("Every row must have the same length", "the run must fail for the ragged shape itself, not for an unrelated Python bridge error");
	}

	[TestMethod]
	public async Task NumpyTo2dArrayDoesNotDropValuesAfterAnEmptyFirstRow()
	{
		var token = CancellationToken;
		Exception caught = null;

		try
		{
			await RunInlinePythonScript("empty_first_ragged_to2darray_script", _pythonEmptyFirstRaggedTo2dArraySource, Helper.CreateSecurityId(), TimeSpan.FromMinutes(1).TimeFrame(), Mock.Of<IMarketDataStorage<CandleMessage>>(), token);
		}
		catch (Exception ex)
		{
			caught = ex;
		}

		IsNotNull(caught, "a value after an empty first row was silently discarded");
		caught.ToString().AssertContains("Every row must have the same length", "the run must fail for the ragged shape itself");
	}

	// A candle storage the test drives directly: it counts the candles it hands over and the times
	// its reader is closed, and can fail on a chosen candle.
	private sealed class CountingCandleStorage(SecurityId securityId, DataType dataType, CandleMessage[] candles, int failAt) : IMarketDataStorage<CandleMessage>
	{
		private static readonly IMarketDataSerializer _serializer = Mock.Of<IMarketDataSerializer>(s => s.TimePrecision == TimeSpan.FromTicks(1));

		private int _read;
		private int _released;

		public int Read => _read;
		public int Released => _released;

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync()
			=> new[] { candles[0].OpenTime.Date }.ToAsyncEnumerable();

		DataType IMarketDataStorage.DataType => dataType;
		SecurityId IMarketDataStorage.SecurityId => securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => _serializer;
		IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => Mock.Of<IMarketDataSerializer<CandleMessage>>();

		public async IAsyncEnumerable<CandleMessage> LoadAsync(DateTime date)
		{
			await Task.Yield();

			try
			{
				for (var i = 0; i < candles.Length; i++)
				{
					if (i == failAt)
						throw new InvalidOperationException("the storage went away");

					_read++;
					yield return candles[i];
				}
			}
			finally
			{
				_released++;
			}
		}

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => LoadAsync(date);

		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
		{
		// The index expressions are hoisted: an expression tree cannot carry a from-end index.
		var first = candles[0].OpenTime;
		var last = candles[candles.Length - 1].OpenTime;

		return new(Mock.Of<IMarketDataMetaInfo>(i => i.FirstTime == first && i.LastTime == last));
	}

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => new(0);
		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<int> IMarketDataStorage<CandleMessage>.SaveAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken) => new(0);
		ValueTask IMarketDataStorage<CandleMessage>.DeleteAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken) => default;
	}

	private sealed class BlockingCandleStorage(SecurityId securityId, DataType dataType) : IMarketDataStorage<CandleMessage>
	{
		private static readonly IMarketDataSerializer _serializer = Mock.Of<IMarketDataSerializer>(s => s.TimePrecision == TimeSpan.FromTicks(1));

		private readonly TaskCompletionSource<bool> _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _released;

		public Task ReadStarted => _readStarted.Task;
		public int Released => _released;

		public void Release() => _release.TrySetResult(true);

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync()
			=> new[] { _analyticsStart.Date }.ToAsyncEnumerable();

		DataType IMarketDataStorage.DataType => dataType;
		SecurityId IMarketDataStorage.SecurityId => securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => _serializer;
		IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => Mock.Of<IMarketDataSerializer<CandleMessage>>();

		public IAsyncEnumerable<CandleMessage> LoadAsync(DateTime date) => new BlockingEnumerable(this);
		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => LoadAsync(date);

		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
			=> new(Mock.Of<IMarketDataMetaInfo>(i => i.FirstTime == _analyticsStart && i.LastTime == _analyticsStart));

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => new(0);
		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<int> IMarketDataStorage<CandleMessage>.SaveAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken) => new(0);
		ValueTask IMarketDataStorage<CandleMessage>.DeleteAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken) => default;

		private sealed class BlockingEnumerable(BlockingCandleStorage owner) : IAsyncEnumerable<CandleMessage>
		{
			IAsyncEnumerator<CandleMessage> IAsyncEnumerable<CandleMessage>.GetAsyncEnumerator(CancellationToken cancellationToken)
				=> new BlockingEnumerator(owner, cancellationToken);
		}

		private sealed class BlockingEnumerator(BlockingCandleStorage owner, CancellationToken cancellationToken) : IAsyncEnumerator<CandleMessage>
		{
			CandleMessage IAsyncEnumerator<CandleMessage>.Current => throw new InvalidOperationException();

			ValueTask<bool> IAsyncEnumerator<CandleMessage>.MoveNextAsync() => new(MoveNextCoreAsync());

			private async Task<bool> MoveNextCoreAsync()
			{
				owner._readStarted.TrySetResult(true);
				await owner._release.Task.WaitAsync(cancellationToken);
				return false;
			}

			ValueTask IAsyncDisposable.DisposeAsync()
			{
				Interlocked.Increment(ref owner._released);
				return default;
			}
		}
	}

	// Builds a storage of flat one-minute candles that reports what was read out of it.
	private static (SecurityId secId, DataType dataType, CountingCandleStorage storage) CreateCountingCandles(int count, int failAt)
	{
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var dataType = tf.TimeFrame();

		var candles = Enumerable.Range(0, count).Select(i => (CandleMessage)new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _analyticsStart.AddMinutes(i),
			OpenPrice = 100,
			HighPrice = 100,
			LowPrice = 100,
			ClosePrice = 100,
			TotalVolume = 1,
			State = CandleStates.Finished,
		}).ToArray();

		return (secId, dataType, new(secId, dataType, candles, failAt));
	}

	// Compiles a Python script written for the test against the shipped common modules and runs it,
	// so a wrapper in Algo.Analytics.Python/common can be exercised on its own.
	private static async Task<TestAnalyticsPanel> RunInlinePythonScript(string name, string source, SecurityId secId, DataType dataType, IMarketDataStorage<CandleMessage> candleStorage, CancellationToken token)
	{
		var registry = new Mock<IStorageRegistry>();

		registry
			.Setup(r => r.GetCandleMessageStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
			.Returns(candleStorage);

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.Python];

		var res = await compiler.Compile(name, [source], _noReferenceImages, token);
		Validate(res);

		using var context = compiler.CreateContext();

		var assembly = res.GetAssembly(context);
		assembly.AssertNotNull();

		var script = assembly.GetExportedTypes().First(t => t.IsRequiredType<IAnalyticsScript>()).CreateInstance<IAnalyticsScript>();
		script.AssertNotNull();

		var panel = new TestAnalyticsPanel();

		await script.Run(
			Helper.LogManager.Application,
			panel,
			[secId],
			_analyticsStart.Date,
			_analyticsStart.Date.AddDays(1),
			registry.Object,
			Mock.Of<IMarketDataDrive>(),
			StorageFormats.Csv,
			dataType,
			token);

		return panel;
	}

	// Runs the shipped Pearson script over one flat candle per listed minute, so its heatmap can be
	// checked against a correlation worked out by hand.
	private static Task<TestAnalyticsPanel> RunPearsonCorrelation(string fileExtension, (SecurityId secId, (int minute, decimal close)[] closes)[] series, CancellationToken token)
		=> RunPearsonCorrelation(fileExtension, series, Helper.LogManager.Application, token, token);

	private static Task<TestAnalyticsPanel> RunPearsonCorrelation(string fileExtension, (SecurityId secId, (int minute, decimal close)[] closes)[] series, ILogReceiver logs, CancellationToken setupToken, CancellationToken runToken)
		=> RunAnalyticsScript(
			fileExtension,
			"PearsonCorrelationScript",
			[.. series.Select(s => (s.secId, s.closes.Select(c => new FixtureCandle(c.minute, c.close, c.close, c.close, c.close, 1m)).ToArray()))],
			logs,
			setupToken,
			runToken);

	// One minute candle of an analytics fixture: offset in minutes from _analyticsStart, then the
	// prices and volume the script under test reads.
	private readonly record struct FixtureCandle(int Minute, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

	// Each shipped algorithm exists once per language. The compiled copies are named after their
	// type, the Python copy in snake_case, so the same logical script needs a name per language.
	private static readonly Dictionary<string, string> _pythonScriptNames = new()
	{
		{ "PearsonCorrelationScript", "pearson_correlation_script" },
		{ "NormalizePriceScript", "normalize_price_script" },
	};

	// Saves the listed one-minute candles into an in-memory storage, then compiles and runs the
	// shipped script over exactly that data, so its output can be checked against values worked out
	// by hand. setupToken builds the fixture and compiles and must stay live; runToken is the only
	// token the script itself observes, so a cancellation test can hand it an already cancelled one.
	private static async Task<TestAnalyticsPanel> RunAnalyticsScript(string fileExtension, string scriptName, (SecurityId secId, FixtureCandle[] candles)[] series, ILogReceiver logs, CancellationToken setupToken, CancellationToken runToken)
	{
		var tf = TimeSpan.FromMinutes(1);
		var dataType = tf.TimeFrame();
		var format = StorageFormats.Csv;
		var storage = Helper.MemorySystem.GetStorage(Helper.MemorySystem.GetSubTemp());
		var drive = storage.DefaultDrive;

		var lastMinute = 0;

		foreach (var (secId, fixture) in series)
		{
			if (fixture.Length == 0)
				continue;

			lastMinute = lastMinute.Max(fixture.Max(c => c.Minute));

			var candles = fixture.Select(c => new TimeFrameCandleMessage
			{
				SecurityId = secId,
				TypedArg = tf,
				OpenTime = _analyticsStart.AddMinutes(c.Minute),
				OpenPrice = c.Open,
				HighPrice = c.High,
				LowPrice = c.Low,
				ClosePrice = c.Close,
				TotalVolume = c.Volume,
				State = CandleStates.Finished,
			}).ToArray();

			await storage.GetCandleMessageStorage(secId, dataType, drive, format).SaveAsync(candles, setupToken);
		}

		return await RunAnalyticsScriptAgainstStorage(
			fileExtension,
			scriptName,
			[.. series.Select(s => s.secId)],
			storage,
			drive,
			format,
			dataType,
			_analyticsStart.Date,
			_analyticsStart.AddMinutes(lastMinute).Date.AddDays(1),
			logs,
			setupToken,
			runToken);
	}

	private static async Task<TestAnalyticsPanel> RunAnalyticsScriptAgainstStorage(
		string fileExtension,
		string scriptName,
		SecurityId[] securities,
		IStorageRegistry storage,
		IMarketDataDrive drive,
		StorageFormats format,
		DataType dataType,
		DateTime from,
		DateTime to,
		ILogReceiver logs,
		CancellationToken setupToken,
		CancellationToken runToken)
	{
		var folderPath = _analyticsFolder.Put(fileExtension switch
		{
			FileExts.CSharp => "CSharp",
			FileExts.FSharp => "FSharp",
			FileExts.Python => "Python",
			_ => throw new ArgumentOutOfRangeException(nameof(fileExtension), fileExtension, "Unknown analytics language."),
		});

		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];

		var fileName = fileExtension == FileExts.Python
			? _pythonScriptNames.TryGetValue(scriptName) ?? throw new ArgumentOutOfRangeException(nameof(scriptName), scriptName, "No Python file name known for the script.")
			: scriptName;

		var sources = new[] { await File.ReadAllTextAsync(Path.Combine(folderPath, fileName + fileExtension), setupToken) };

		// Only the C# copies split their namespace imports into a separate file.
		if (fileExtension == FileExts.CSharp)
			sources = sources.Concat([await File.ReadAllTextAsync(Path.Combine(folderPath, "Properties", "usings.cs"), setupToken)]);

		var refs = compiler.IsReferencesSupported
			? await GetReferenceImages(true, fileExtension == FileExts.FSharp, setupToken)
			: _noReferenceImages;

		var res = await compiler.Compile(fileName, sources, refs, setupToken);
		Validate(res);

		using var context = compiler.CreateContext();

		var assembly = res.GetAssembly(context);
		assembly.AssertNotNull();

		var script = assembly.GetExportedTypes().First(t => t.IsRequiredType<IAnalyticsScript>()).CreateInstance<IAnalyticsScript>();
		script.AssertNotNull();

		var panel = new TestAnalyticsPanel();

		await script.Run(
			logs,
			panel,
			securities,
			from,
			to,
			storage,
			drive,
			format,
			dataType,
			runToken);

		return panel;
	}

	private static async Task TestAnalyticsScriptsParallel(string folderPath, string fileExtension, CancellationToken token)
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];

		// Get all script files in the folder
		var scriptFiles = Directory.GetFiles(folderPath, $"*{fileExtension}");
		(scriptFiles.Length > 0).AssertTrue("Ensure there are scripts to test");

		var securities = new[]
		{
			"EUR/USD@DUKAS".ToSecurityId(),
			"EUR/AUD@DUKAS".ToSecurityId(),
			"GBP/AUD@DUKAS".ToSecurityId(),
		};
		var from = new DateTime(2025, 4, 1).UtcKind();
		var to = GetPeriodEnd(fileExtension);
		var storageRegistry = Helper.GetResourceStorage();
		var format = StorageFormats.Binary;
		var timeFrame = TimeSpan.FromMinutes(1).TimeFrame();

		await EnsureCandlesExist(securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);

		var refs = compiler.IsReferencesSupported
			? await GetReferenceImages(true, fileExtension == FileExts.FSharp, token)
			: _noReferenceImages;

		// Run all compile-and-execute pipelines concurrently. The compiler owns any
		// synchronization required by its underlying script engine.
		var tasks = scriptFiles.Select(async scriptFile =>
		{
			var scriptName = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				if (scriptName.StartsWithIgnoreCase("empty"))
					return;

				var sourceCode = await File.ReadAllTextAsync(scriptFile, token);

				// Compile the script
				var sources = new string[] { sourceCode };

				var res = await compiler.Compile(scriptName, sources, refs, token);

				Validate(res);

				using var context = compiler.CreateContext();
				var assembly = res.GetAssembly(context);
				assembly.AssertNotNull();

				var analyticsScriptType = assembly.GetExportedTypes().First(t => t.IsRequiredType<IAnalyticsScript>());
				var script = analyticsScriptType.CreateInstance<IAnalyticsScript>();
				script.AssertNotNull();

				await RunAnalyticsScript(script, securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error running script '{scriptName}'.", ex);
			}
		});

		await Task.WhenAll(tasks);
	}

	private static async Task TestAnalyticsScripts(string folderPath, string fileExtension, CancellationToken token)
	{
		ICompiler compiler = ServicesRegistry.CompilerProvider[fileExtension];

		var usings = fileExtension == FileExts.CSharp
			? await File.ReadAllTextAsync(Path.Combine(folderPath, "Properties", "usings.cs"), token)
			: null;

		// Get all script files in the folder
		var scriptFiles = Directory.GetFiles(folderPath, $"*{fileExtension}");
		(scriptFiles.Length > 0).AssertTrue("Ensure there are scripts to test");

		var securities = new[]
		{
			"EUR/USD@DUKAS".ToSecurityId(),
			"EUR/AUD@DUKAS".ToSecurityId(),
			"GBP/AUD@DUKAS".ToSecurityId(),
		};
		var from = new DateTime(2025, 4, 1).UtcKind();
		var to = GetPeriodEnd(fileExtension);
		var storageRegistry = Helper.GetResourceStorage();
		var format = StorageFormats.Binary;
		var timeFrame = TimeSpan.FromMinutes(1).TimeFrame();

		await EnsureCandlesExist(securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);

		var refs = compiler.IsReferencesSupported
			? await GetReferenceImages(true, fileExtension == FileExts.FSharp, token)
			: _noReferenceImages;

		foreach (var scriptFile in scriptFiles)
		{
			var scriptName = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				if (scriptName.StartsWithIgnoreCase("empty"))
					continue;

				var sourceCode = await File.ReadAllTextAsync(scriptFile, token);

				// Compile the script

				var sources = new string[] { sourceCode };

				if (usings is not null)
					sources = sources.Concat([usings]);

				var res = await compiler.Compile(
					scriptName,
					sources,
					refs,
					token);

				Validate(res);

				using var context = compiler.CreateContext();
				var assembly = res.GetAssembly(context);
				assembly.AssertNotNull();

				var types = assembly.GetExportedTypes();
				var analyticsScriptType = types.First(t => t.IsRequiredType<IAnalyticsScript>());

				// Create an instance of the script
				var script = analyticsScriptType.CreateInstance<IAnalyticsScript>();
				script.AssertNotNull();

				// Test script execution with mock data
				await RunAnalyticsScript(script, securities, from, to, storageRegistry, storageRegistry.DefaultDrive, format, timeFrame, token);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error running script '{scriptName}'.", ex);
			}
		}
	}

	// IronPython interprets every candle (~24 us against ~1.9 us for compiled C#/F#), so a whole
	// month of 1-min candles costs seconds per script there while nothing the scripts are checked
	// for (VerifyOutputProduced) depends on the data volume. Python scripts therefore get a few
	// days only; compiled languages keep the original month.
	private static DateTime GetPeriodEnd(string fileExtension)
		=> (fileExtension == FileExts.Python ? new DateTime(2025, 4, 5) : new DateTime(2025, 4, 30)).UtcKind();

	// Guard for the language dependent period (see GetPeriodEnd): if the storage has no candles
	// inside the window, scripts produce no output at all and the failure would surface far away
	// from its cause. Fail here instead, naming the security and the empty range.
	private static async Task EnsureCandlesExist(SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken token)
	{
		foreach (var security in securities)
		{
			var dates = await storage.GetCandleMessageStorage(security, dataType, drive, format).GetDatesAsync(from, to).ToArrayAsync(token);
			(dates.Length > 0).AssertTrue($"No {dataType} data for {security} in {from:yyyy-MM-dd}..{to:yyyy-MM-dd}.");
		}
	}

	private static async Task RunAnalyticsScript(IAnalyticsScript script, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken token)
	{
		// Create a test panel to capture output
		var testPanel = new TestAnalyticsPanel();

		var (_, t) = token.CreateChildToken(TimeSpan.FromSeconds(60)); // 60 second timeout

		// Execute the script
		await script.Run(
			Helper.LogManager.Application,
			testPanel,
			securities,
			from,
			to,
			storage,
			drive,
			format,
			dataType,
			t
		);

		// Verify that the script produced some output
		testPanel.VerifyOutputProduced();
	}

	// Test implementation of IAnalyticsPanel to verify script execution. It records everything it
	// is handed - grid columns, rows and sorting, chart series values, the heatmap - so a test can
	// check the meaning of a script's output and not only its shape.
	private class TestAnalyticsPanel : IAnalyticsPanel
	{
		private readonly List<TestAnalyticsGrid> _grids = [];
		private readonly List<ITestAnalyticsChart> _charts = [];
		private bool _heatmapHasData;
		private bool _chart3dHasData;

		public string[] HeatmapXTitles { get; private set; }
		public string[] HeatmapYTitles { get; private set; }
		public double[,] HeatmapData { get; private set; }

		public IReadOnlyList<TestAnalyticsGrid> Grids => _grids;
		public IReadOnlyList<ITestAnalyticsChart> Charts => _charts;

		// The single grid or chart a one-output script is expected to have created.
		public TestAnalyticsGrid SingleGrid
		{
			get
			{
				_grids.Count.AssertEqual(1);
				return _grids[0];
			}
		}

		public TestAnalyticsChart<X, Y, Z> ChartAt<X, Y, Z>(int index)
		{
			(index < _charts.Count).AssertTrue($"chart {index} was not created");
			return (TestAnalyticsChart<X, Y, Z>)_charts[index];
		}

		public IAnalyticsGrid CreateGrid(params string[] columns)
		{
			columns.AssertNotNull();
			(columns.Length > 0).AssertTrue("columns must not be empty");

			var grid = new TestAnalyticsGrid(columns);
			_grids.Add(grid);
			return grid;
		}

		public IAnalyticsChart<X, Y, Z> CreateChart<X, Y, Z>()
		{
			var chart = new TestAnalyticsChart<X, Y, Z>();
			_charts.Add(chart);
			return chart;
		}

		public IAnalyticsChart<X, Y, VoidType> CreateChart<X, Y>()
		{
			var chart = new TestAnalyticsChart<X, Y, VoidType>();
			_charts.Add(chart);
			return chart;
		}

		public void DrawHeatmap(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			HeatmapXTitles = [.. xTitles];
			HeatmapYTitles = [.. yTitles];
			HeatmapData = data;

			// Every cell is addressed by its titles, so a matrix that does not span them is unreadable.
			data.GetLength(0).AssertEqual(HeatmapXTitles.Length);
			data.GetLength(1).AssertEqual(HeatmapYTitles.Length);

			// Check that data has actual content
			var hasData = data.GetLength(0) > 0 && data.GetLength(1) > 0;
			if (hasData)
				_heatmapHasData = true;
		}

		public void Draw3D(IEnumerable<string> xTitles, IEnumerable<string> yTitles, double[,] data, string xTitle, string yTitle, string zTitle)
		{
			xTitles.AssertNotNull();
			yTitles.AssertNotNull();
			data.AssertNotNull();

			// Check that data has actual content
			var hasData = data.GetLength(0) > 0 && data.GetLength(1) > 0;
			if (hasData)
				_chart3dHasData = true;
		}

		public void VerifyOutputProduced()
		{
			// Check grids have rows
			var gridsHaveData = _grids.Count > 0 && _grids.Any(g => g.RowCount > 0);

			// Check charts have series with data points
			var chartsHaveData = _charts.Count > 0 && _charts.Any(c => c.SeriesCount > 0 && c.TotalDataPoints > 0);

			// At least one type of output should have been produced with actual data
			(gridsHaveData || chartsHaveData || _heatmapHasData || _chart3dHasData).AssertTrue();
		}

		public interface ITestAnalyticsChart
		{
			int SeriesCount { get; }
			int TotalDataPoints { get; }
		}

		public class TestAnalyticsGrid(string[] columns) : IAnalyticsGrid
		{
			private readonly List<object[]> _rows = [];

			public IReadOnlyList<string> Columns => columns;
			public IReadOnlyList<object[]> Rows => _rows;

			public string SortColumn { get; private set; }
			public bool SortAsc { get; private set; }
			public bool IsSorted { get; private set; }

			public void SetSort(string column, bool asc)
			{
				column.IsEmpty().AssertFalse();
				columns.Count(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)).AssertEqual(1);

				SortColumn = column;
				SortAsc = asc;
				IsSorted = true;
			}

			public void SetRow(params object[] row)
			{
				row.AssertNotNull();
				row.Length.AssertEqual(columns.Length);
				_rows.Add(row);
			}

			public int RowCount => _rows.Count;
		}

		public class TestAnalyticsChart<X, Y, Z> : IAnalyticsChart<X, Y, Z>, ITestAnalyticsChart
		{
			private readonly List<AppendedSeries> _series = [];
			private int _totalDataPoints;

			// One Append call: what was drawn, under which title and in which style.
			public sealed record AppendedSeries(string Title, X[] XValues, Y[] YValues, Z[] ZValues, DrawStyles Style, Color? Color);

			public IReadOnlyList<AppendedSeries> Series => _series;

			public AppendedSeries SingleSeries
			{
				get
				{
					_series.Count.AssertEqual(1);
					return _series[0];
				}
			}

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();

				var x = xValues.ToArray();
				var y = yValues.ToArray();
				x.Length.AssertEqual(y.Length);

				_series.Add(new(title, x, y, [], style, color));
				_totalDataPoints += x.Length;
			}

			public void Append(string title, IEnumerable<X> xValues, IEnumerable<Y> yValues, IEnumerable<Z> zValues, DrawStyles style, Color? color)
			{
				title.IsEmpty().AssertFalse();
				xValues.AssertNotNull();
				yValues.AssertNotNull();
				zValues.AssertNotNull();

				var x = xValues.ToArray();
				var y = yValues.ToArray();
				var z = zValues.ToArray();
				x.Length.AssertEqual(y.Length);
				x.Length.AssertEqual(z.Length);

				_series.Add(new(title, x, y, z, style, color));
				_totalDataPoints += x.Length;
			}

			public int SeriesCount => _series.Count;
			public int TotalDataPoints => _totalDataPoints;
		}
	}

	private static readonly string _designerFolder = "../../../../Designer.Templates/";

	private static void Validate(CompilationResult res)
	{
		ArgumentNullException.ThrowIfNull(res);

		foreach (var e in res.Errors)
		{
			if (e.Type == CompilationErrorTypes.Error)
				throw new InvalidOperationException(e.ToString());
		}
	}

	private static List<PropertyDescriptor> GetBrowsableProperties(ICustomTypeDescriptor customTypeDescriptor)
	{
		if (customTypeDescriptor == null)
			throw new ArgumentNullException(nameof(customTypeDescriptor));

		var allProperties = customTypeDescriptor.GetProperties();

		List<PropertyDescriptor> browsableProperties = [];

		foreach (PropertyDescriptor prop in allProperties)
		{
			if (prop.Attributes[typeof(BrowsableAttribute)] is not BrowsableAttribute browsableAttr ||
				!browsableAttr.Browsable)
				continue;

			if (prop.Attributes[typeof(EditorBrowsableAttribute)] is EditorBrowsableAttribute editorBrowsableAttr &&
				editorBrowsableAttr.State == EditorBrowsableState.Never)
				continue;

			if (prop.Attributes[typeof(DesignOnlyAttribute)] is DesignOnlyAttribute designOnlyAttr &&
				designOnlyAttr.IsDesignOnly)
				continue;

			browsableProperties.Add(prop);
		}

		return browsableProperties;
	}

	private void InvokeDiagramElem(Type type, DiagramExternalElement instance)
	{
		var evts = type.GetEvents()
			.Where(DiagramExternalAttribute.IsExternal)
			.ToArray();
		evts.Length.AssertEqual(2);

		var raisedCnt = 0;

		foreach (var evt in evts)
		{
			var handlerType = evt.EventHandlerType;
			var isFSharp = handlerType.IsFSharpHandler();

			if (handlerType != typeof(Action<Unit>) && !isFSharp)
				continue;

			var evtAttrs = evt.GetAttributes().ToArray();
			evtAttrs.Count(a => a is DiagramExternalAttribute).AssertEqual(1);

			Delegate dlg;

			if (isFSharp)
			{
				FSharpHandler<Unit> handler = (s, value) => raisedCnt++;
				dlg = handler;
			}
			else
			{
				Action<Unit> handler = value => raisedCnt++;
				dlg = handler;
			}

			evt.AddEventHandler(instance, dlg);
			evt.RemoveEventHandler(instance, dlg);

			evt.AddEventHandler(instance, dlg);
			evt.AddEventHandler(instance, dlg);
		}

		var methods = type.GetMethods().ToArray();
		methods.Count(m => m.Name == "Process").AssertEqual(1);

		foreach (var method in methods)
		{
			if (method.Name != "Process")
				continue;

			var methodAttrs = method.GetAttributes().ToArray();

			method.Invoke(instance,
			[
				new TimeFrameCandleMessage { ClosePrice = 100 },
			(Unit)10,
		]);
		}

		raisedCnt.AssertEqual(2);
	}

	[TestMethod]
	public Task CSharpEmptyStrategy()
		=> CSharpCompile<Strategy>("Backtest/EmptyStrategy.cs");

	[TestMethod]
	public Task CSharpSmaStrategy()
		=> CSharpCompile<Strategy>("Backtest/SmaStrategy.cs");

	// PairStrategy is a template a user starts from, and it is the only backtest template with no
	// addressed test at all. Beyond compiling, every tunable it declares has to be usable.
	[TestMethod]
	public Task CSharpPairStrategy()
		=> CSharpCompile<Strategy>("Backtest/PairStrategy.cs", CheckDeclaredParams);

	// Every public read/write property a strategy template declares is a knob the Designer shows and
	// the optimizer drives, so each one must be backed by a StrategyParam registered under the same
	// name and must survive a read followed by writing the same value back.
	private void CheckDeclaredParams(Type type, Strategy strategy)
	{
		var props = type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
			.ToArray();

		(props.Length > 0).AssertTrue($"{type.Name} declares no parameters.");

		foreach (var prop in props)
		{
			strategy.Parameters
				.TryGetById(prop.Name, out var param)
				.AssertTrue($"{type.Name}.{prop.Name} is not registered as a strategy parameter.");

			param.AssertNotNull($"{type.Name}.{prop.Name} resolved to a null parameter.");

			var value = prop.GetValue(strategy);
			prop.SetValue(strategy, value);
			prop.GetValue(strategy).AssertEqual(value, $"{type.Name}.{prop.Name} did not survive a write of its own value.");
		}
	}

	[TestMethod]
	public Task CSharpIndicator()
		=> CSharpCompile<IIndicator>("Indicator/EmptyIndicator.cs");

	[TestMethod]
	public Task CSharpDiagramElem()
		=> CSharpCompile<DiagramExternalElement>("Custom/EmptyDiagramElement.cs", InvokeDiagramElem);

	private async Task CSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.CSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var res = await compiler.Compile("test", [sourceCode], await GetReferenceImages(false, false, token), token);
		Validate(res);

		using var context = compiler.CreateContext();
		var type = res.GetAssembly(context).GetExportedTypes().First();
		type.IsRequiredType<T>().AssertTrue();
		
		var s = type.CreateInstance<T>();
		
		s.AssertNotNull();
		s.Load(s.Save());

		custom?.Invoke(type, s);
	}

	[TestMethod]
	public Task FSharpEmptyStrategy()
		=> FSharpCompile<Strategy>("Backtest/EmptyStrategy.fs");

	[TestMethod]
	public Task FSharpSmaStrategy()
		=> FSharpCompile<Strategy>("Backtest/SmaStrategy.fs");

	[TestMethod]
	public Task FSharpIndicator()
		=> FSharpCompile<IIndicator>("Indicator/EmptyIndicator.fs");

	[TestMethod]
	public Task FSharpDiagramElem()
		=> FSharpCompile<DiagramExternalElement>("Custom/EmptyDiagramElement.fs", InvokeDiagramElem);

	private async Task FSharpCompile<T>(string fileName, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.FSharp];

		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));

		var res = await compiler.Compile("test", [sourceCode], await GetReferenceImages(false, true, token), token);
		Validate(res);

		using var context = compiler.CreateContext();
		var type = res.GetAssembly(context).GetExportedTypes().First();
		type.IsRequiredType<T>().AssertTrue();
		
		var s = type.CreateInstance<T>();

		s.AssertNotNull();
		s.Load(s.Save());

		custom?.Invoke(type, s);
	}

	// Recompiling the same module into the same context is a compiler level check (see the
	// recompile branch in PythonCompile), so it runs once here instead of in every template test.
	[TestMethod]
	public Task PythonEmptyStrategy()
		=> PythonCompile<Strategy>("Backtest/empty_strategy.py", true);

	[TestMethod]
	public Task PythonSmaStrategy()
		=> PythonCompile<Strategy>("Backtest/sma_strategy.py", false);

	[TestMethod]
	public Task PythonIndicator()
		=> PythonCompile<IIndicator>("Indicator/empty_indicator.py", false);

	[TestMethod]
	public Task PythonDiagramElem()
		=> PythonCompile<DiagramExternalElement>("Custom/empty_diagram_element.py", false, InvokeDiagramElem);

	private async Task PythonCompile<T>(string fileName, bool recompile, Action<Type, T> custom = null)
		where T : IPersistable
	{
		var token = CancellationToken;

		ICompiler compiler = ServicesRegistry.CompilerProvider[FileExts.Python];

		using var context = compiler.CreateContext();
		
		var sourceCode = File.ReadAllText(Path.Combine(_designerFolder, fileName));
		
		var res = await compiler.Compile(typeof(T).Name, [sourceCode], _noReferenceImages, token);

		Validate(res);

		var asm = res.GetAssembly(context);
		asm.AssertNotNull();

		var types = asm.GetExportedTypes();

		if (recompile)
		{
			// Compiling the same module name into the same context again must succeed
			// and expose the same type, not clash with the already loaded module.
			var res2 = await compiler.Compile(typeof(T).Name, [sourceCode], _noReferenceImages, token);

			Validate(res2);

			var asm2 = res2.GetAssembly(context);
			asm2.AssertNotNull();

			asm2.GetExportedTypes().Any(t => t.IsRequiredType<T>()).AssertTrue("recompiled module must expose the same type");
		}

		var arrs = types.Where(t => t.IsRequiredType<T>());
		var type = arrs.First();
		var ns = type.Namespace;
		var fn = type.FullName;

		var attrs = type.GetAttributes().ToArray();
		var docUrl = type.GetDocUrl();

		type.IsRequiredType<T>().AssertTrue();

		var name = type.GetDisplayName();
		var desc = type.GetDescription();
		var iconUri = type.GetIconUrl();

		var instance = TypeHelper.CreateInstance<T>(type);

		if (instance is Strategy s)
			s.Connector = new Connector();

		var props = GetBrowsableProperties((ICustomTypeDescriptor)instance);

		var descriptor = TypeDescriptor.GetProvider(instance).GetTypeDescriptor(instance);

		(instance is IPythonObject).AssertTrue();

		var pythonClass = type.CreateInstance<T>();

		var properties = type.GetProperties().ToArray();
		var modifiableProperties = properties.Where(p => p.IsBrowsable() && p.IsModifiable()).ToArray();
		//foreach (var prop in modifiableProperties)
		//{
		//	Console.WriteLine($"{prop.Name}={prop.PropertyType}");
		//	Console.WriteLine(prop.GetValue(pythonClass));
		//	Console.WriteLine();
		//}

		custom?.Invoke(type, instance);

		pythonClass.Load(pythonClass.Save());
	}

	private static readonly string _pythonCommonFolder = "../../../../Algo.Analytics.Python/common";

	private const string _extraCommonName = "unit_test_common.py";
	private const string _extraCommonBody = "TEST_MARKER = 42\n";

	private sealed class RecordingLogReceiver : BaseLogReceiver
	{
		private readonly Lock _sync = new();
		private readonly List<LogMessage> _messages = [];

		public RecordingLogReceiver()
		{
			Log += m =>
			{
				using (_sync.EnterScope())
				{
					_messages.Add(m);
				}
			};
		}

		public LogMessage[] Messages
		{
			get
			{
				using (_sync.EnterScope())
				{
					return [.. _messages];
				}
			}
		}
	}

	// PythonStream is the stdout/stderr sink CompilationExtensions installs on the Python engine.
	// It has no public seam, so the test builds the very type the engine is handed.
	private static Stream CreatePythonOutputStream(ILogReceiver logs)
	{
		var type = typeof(CompilationExtensions).GetNestedType("PythonStream", BindingFlags.NonPublic)
			?? throw new InvalidOperationException("PythonStream is gone.");

		return (Stream)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [logs], null);
	}

	// A UTF-8 character split across two Write calls must still reach the log as that one character:
	// the engine picks the chunk sizes, the caller only ever promised a byte stream.
	[TestMethod]
	public void PythonOutputSurvivesMultiByteCharSplitBetweenWrites()
	{
		var logs = new RecordingLogReceiver();

		using var stream = CreatePythonOutputStream(logs);

		const string text = "тест 🚀";
		var bytes = text.UTF8();

		// Four two-byte Cyrillic letters plus a space is 9 bytes, the rocket is a four-byte
		// sequence, so 13 in total and byte 11 lands in the middle of that last character.
		bytes.Length.AssertEqual(13);

		stream.Write(bytes, 0, 11);
		stream.Write(bytes, 11, bytes.Length - 11);
		stream.Flush();

		string.Concat(logs.Messages.Select(m => m.Message)).AssertEqual(text);
	}

	// Chunk boundaries inside a line, and a buffer the writer only partly owns, must cost neither
	// order nor content: what Python printed is what the log holds.
	[TestMethod]
	public void PythonOutputKeepsOrderAcrossPartialLinesAndBufferOffsets()
	{
		var logs = new RecordingLogReceiver();

		using var stream = CreatePythonOutputStream(logs);

		var first = "first line\nsecond ".UTF8();
		stream.Write(first, 0, first.Length);

		// The second chunk sits inside a larger buffer: only [offset, offset + count) is output.
		var second = "line\n".UTF8();
		var padded = new byte[64];
		second.CopyTo(padded, 7);

		stream.Write(padded, 7, second.Length);
		stream.Flush();

		string.Concat(logs.Messages.Select(m => m.Message)).AssertEqual("first line\nsecond line\n");
	}

	// The python_common resources are embedded from this folder, so the folder is the oracle for
	// both the names Init must extract and the bodies it must write.
	private static (string name, string body)[] GetPythonCommonSources()
		=> [.. Directory
			.GetFiles(_pythonCommonFolder, $"*{FileExts.Python}")
			.Select(f => (name: Path.GetFileName(f), body: File.ReadAllText(f)))
			.OrderBy(t => t.name, StringComparer.Ordinal)];

	private static string[] FileNames(IFileSystem fs)
		=> [.. fs.EnumerateFiles(Paths.PythonUtilsPath).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal)];

	private static void AssertCompilersRegistered()
	{
		var provider = ConfigManager.TryGetService<CompilerProvider>();
		provider.AssertNotNull();

		foreach (var ext in new[] { FileExts.CSharp, FileExts.VisualBasic, FileExts.FSharp, FileExts.Python })
			provider.ContainsKey(ext).AssertTrue(ext);

		ConfigManager.TryGetService<ICustomTypeDescriptorProvider>().AssertNotNull();
	}

	private static void AssertNoErrors(RecordingLogReceiver logs)
	{
		var errors = logs.Messages.Where(m => m.Level == LogLevels.Error).ToArray();
		errors.Length.AssertEqual(0, errors.Select(m => m.Message).JoinNL());
	}

	// Init registers process-wide services; every Init test puts back what the assembly initializer
	// set up, so the rest of the suite keeps compiling against that provider.
	private static async Task WithRestoredServices(Func<Task> body)
	{
		var compilers = ConfigManager.TryGetService<CompilerProvider>();
		var descriptors = ConfigManager.TryGetService<ICustomTypeDescriptorProvider>();

		try
		{
			await body();
		}
		finally
		{
			if (compilers is not null)
				ConfigManager.RegisterService(compilers);

			if (descriptors is not null)
				ConfigManager.RegisterService(descriptors);
		}
	}

	// Init must leave every python_common resource on the file system it was handed, byte for byte,
	// alongside the caller's extras - that is the whole point of the call.
	[TestMethod]
	[DoNotParallelize] // Init replaces the process-wide compiler services.
	public Task InitExtractsPythonCommonFiles() => WithRestoredServices(async () =>
	{
		var fs = new MemoryFileSystem();
		var logs = new RecordingLogReceiver();

		await CompilationExtensions.Init(fs, logs, [(_extraCommonName, _extraCommonBody)], CancellationToken);

		fs.DirectoryExists(Paths.PythonUtilsPath).AssertTrue();

		var sources = GetPythonCommonSources();

		var expected = sources.Select(t => t.name).Append(_extraCommonName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		FileNames(fs).AssertEqual(expected);

		foreach (var (name, body) in sources)
			fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, name)).AssertEqual(body);

		fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, _extraCommonName)).AssertEqual(_extraCommonBody);

		AssertNoErrors(logs);
		AssertCompilersRegistered();
	});

	// A cancelled token must be answered with cancellation. Init that returns normally tells the
	// caller the environment is ready when the common files were never written.
	[TestMethod]
	[DoNotParallelize] // Init replaces the process-wide compiler services.
	public Task InitPropagatesCancellation() => WithRestoredServices(async () =>
	{
		var fs = new MemoryFileSystem();
		var logs = new RecordingLogReceiver();

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => CompilationExtensions.Init(fs, logs, [(_extraCommonName, _extraCommonBody)], cts.Token));
	});

	// One unwritable file must be reported and must not cost the other files or the compilers:
	// a silent skip leaves Python scripts failing later on a missing import with no clue why.
	[TestMethod]
	[DoNotParallelize] // Init replaces the process-wide compiler services.
	public Task InitReportsFailedCommonFileWrite() => WithRestoredServices(async () =>
	{
		var fs = new MemoryFileSystem();
		var logs = new RecordingLogReceiver();

		var sources = GetPythonCommonSources();
		var lockedName = sources[0].name;
		var lockedPath = Path.Combine(Paths.PythonUtilsPath, lockedName);

		const string lockedBody = "MARKER = 'not to be overwritten'\n";

		fs.CreateDirectory(Paths.PythonUtilsPath);
		fs.WriteAllText(lockedPath, lockedBody);
		fs.SetReadOnly(lockedPath, true);

		await CompilationExtensions.Init(fs, logs, [(_extraCommonName, _extraCommonBody)], CancellationToken);

		fs.ReadAllText(lockedPath).AssertEqual(lockedBody);

		var errors = logs.Messages.Where(m => m.Level == LogLevels.Error).ToArray();
		errors.Length.AssertEqual(1, errors.Select(m => m.Message).JoinNL());
		errors[0].Message.AssertContains(nameof(UnauthorizedAccessException));

		foreach (var (name, body) in sources.Skip(1))
			fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, name)).AssertEqual(body);

		fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, _extraCommonName)).AssertEqual(_extraCommonBody);

		AssertCompilersRegistered();
	});

	// Re-initialising over a folder that already holds an older extraction must replace each file,
	// not append to it and not leave the tail of a longer previous body behind.
	[TestMethod]
	[DoNotParallelize] // Init replaces the process-wide compiler services.
	public Task InitCanRunTwiceOverTheSameFolder() => WithRestoredServices(async () =>
	{
		var fs = new MemoryFileSystem();
		var logs = new RecordingLogReceiver();

		const string longBody = "MARKER = 'first run, deliberately longer than the second one'\n";
		const string shortBody = "MARKER = 'second'\n";

		await CompilationExtensions.Init(fs, logs, [(_extraCommonName, longBody)], CancellationToken);
		await CompilationExtensions.Init(fs, logs, [(_extraCommonName, shortBody)], CancellationToken);

		var sources = GetPythonCommonSources();

		var expected = sources.Select(t => t.name).Append(_extraCommonName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		FileNames(fs).AssertEqual(expected);

		fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, _extraCommonName)).AssertEqual(shortBody);

		foreach (var (name, body) in sources)
			fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, name)).AssertEqual(body);

		AssertNoErrors(logs);
		AssertCompilersRegistered();
	});

	// An extra that reuses a built-in name must end as one file holding one of the two bodies whole.
	// Which of the two wins is a contract question and is deliberately not asserted here.
	[TestMethod]
	[DoNotParallelize] // Init replaces the process-wide compiler services.
	public Task InitKeepsOneFilePerCommonName() => WithRestoredServices(async () =>
	{
		var fs = new MemoryFileSystem();
		var logs = new RecordingLogReceiver();

		var sources = GetPythonCommonSources();
		var (clashName, builtInBody) = sources[0];

		const string extraBody = "MARKER = 'caller supplied override'\n";

		await CompilationExtensions.Init(fs, logs, [(clashName, extraBody)], CancellationToken);

		var expected = sources.Select(t => t.name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		FileNames(fs).AssertEqual(expected);

		var content = fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, clashName));
		(content == builtInBody || content == extraBody).AssertTrue($"neither body survived whole: {content}");

		foreach (var (name, body) in sources.Skip(1))
			fs.ReadAllText(Path.Combine(Paths.PythonUtilsPath, name)).AssertEqual(body);

		AssertNoErrors(logs);
		AssertCompilersRegistered();
	});

	/// <summary>
	/// A mathematical formula is compiled into an assembly of its own and loaded into a context that
	/// belongs to whichever component asked for it - a Math element on a diagram, an index
	/// instrument, a fitness function - and that component unloads its context when it is closed.
	/// The text, however, is shared: two components can perfectly well use the same formula. Closing
	/// one of them must not take the formula away from the other, whose chart or optimisation is
	/// still running and would otherwise fail on its next value.
	/// </summary>
	[TestMethod]
	public async Task AFormulaKeepsWorkingAfterAnotherHolderOfTheSameFormulaIsClosed()
	{
		// Unique to this test: a compiled formula is remembered process-wide under its text, so an
		// expression shared with another test would be answered out of whatever that test left.
		const string expression = "(AlphaLeg + BetaLeg) * 3 - 7";

		var first = new AssemblyLoadContextTracker();

		var opened = await expression.CompileAsync<decimal>(Helper.FileSystem, first, CancellationToken);

		opened.Error.AssertNull($"the expression did not compile: {opened.Error}");
		opened.Calculate([2m, 3m]).AssertEqual(8m);

		// The component that compiled it is closed, and takes its load context with it.
		first.Dispose();

		using var second = new AssemblyLoadContextTracker();

		var shared = await expression.CompileAsync<decimal>(Helper.FileSystem, second, CancellationToken);

		shared.Error.AssertNull($"the second holder was given a formula that does not work: {shared.Error}");
		shared.Calculate([2m, 3m]).AssertEqual(8m, "the second holder's formula must still calculate after the first holder was closed");
		shared.Calculate([10m, 1m]).AssertEqual(26m, "and must go on calculating for values it has not been asked for before");
	}
}
