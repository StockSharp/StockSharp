namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.Text;

/// <summary>
/// Runs against the committed reference data under Resources/IndicatorsData.
/// </summary>
partial class IndicatorTests
{
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

	/// <summary>
	/// Both readers of a whole series - <c>IndicatorDataRunner.Check</c> and <c>CompareValue</c> - compare
	/// nothing at all on a bar the indicator reports itself unformed on, so a sweep proves only as much as
	/// the indicator forms early. That bound is what <see cref="IIndicator.NumValuesToInitialize"/> promises
	/// a user: feed it that many final values and it is ready, and everything after them is a real value
	/// that a reference run actually answers for. An indicator that quietly needs more leaves the front of
	/// the series unchecked, and one that never forms is swept through with nothing checked at all.
	/// </summary>
	[TestMethod]
	public async Task EveryIndicatorFormsWithinNumValuesToInitialize()
	{
		var time = new DateTime(2000, 1, 1, 0, 0, 0).UtcKind();
		var tf = TimeSpan.FromDays(1);
		var secId = Helper.CreateSecurityId();
		var candles = await LoadCandles(secId, time, tf);

		// These two decide they are formed from the values they see rather than from how many - the same
		// pair NumValuesToInitialize excludes. The count they publish cannot be held to, but they still owe
		// the weaker half of the promise: they have to become formed on a real series.
		var dataDependent = new HashSet<Type>
		{
			typeof(AdaptiveLaguerreFilter),
			typeof(DemandIndex),
		};

		var errors = new List<string>();

		foreach (var type in GetIndicatorTypes())
		{
			var indicator = type.CreateIndicator();
			var declared = indicator.NumValuesToInitialize;
			var inputType = type.InputValue;
			var formedAt = 0;

			for (var i = 0; i < candles.Length; i++)
			{
				var candle = candles[i];

				IIndicatorValue input;

				if (inputType == typeof(DecimalIndicatorValue))
					input = new DecimalIndicatorValue(indicator, candle.ClosePrice, candle.OpenTime) { IsFinal = true };
				else if (inputType == typeof(CandleIndicatorValue))
					input = new CandleIndicatorValue(indicator, candle) { IsFinal = true };
				else
					throw new InvalidOperationException(inputType.To<string>());

				indicator.Process(input);

				if (indicator.IsFormed)
				{
					formedAt = i + 1;
					break;
				}
			}

			var name = type.Indicator.Name;

			if (formedAt == 0)
				errors.Add($"{name}: never formed over {candles.Length} candles, so no value of it was ever compared.");
			else if (formedAt > declared && !dataDependent.Contains(type.Indicator))
				errors.Add($"{name}: asks for {declared} value(s) but was formed only after {formedAt}, leaving {formedAt - declared} bar(s) beyond the declared warm-up unchecked.");
		}

		if (errors.Count > 0)
		{
			var report = errors
				.OrderBy(error => error, StringComparer.Ordinal)
				.JoinN();

			Fail($"Indicators not formed within NumValuesToInitialize ({errors.Count}):{Environment.NewLine}{report}");
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

		// Verifying by default: left to itself this compares the committed reference data against
		// what the indicators produce now and fails on a real difference. The rewrite modes have to
		// be asked for, because they REWRITE committed data - set {modeVar} to a name, to a
		// comma-separated list, or to * for every out-of-date file.
		var verify = mode.IsEmpty() || mode.EqualsIgnoreCase(verifyMode);
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
}
