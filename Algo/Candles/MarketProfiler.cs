namespace StockSharp.Algo.Candles;

/// <summary>
/// What the measured candles cover.
/// </summary>
/// <param name="Candles">Number of candles measured.</param>
/// <param name="From">Open time of the first candle.</param>
/// <param name="To">Open time of the last candle.</param>
/// <param name="Sessions">Distinct trading days covered.</param>
/// <param name="MedianVolume">Volume of the middle candle.</param>
/// <param name="HighVolumeSharePercent">Share of candles whose volume exceeded twice the average of the thirty before, in percent.</param>
public sealed record MarketCoverage(
	int Candles,
	DateTime From,
	DateTime To,
	int Sessions,
	decimal MedianVolume,
	decimal HighVolumeSharePercent);

/// <summary>
/// How much, and how violently, the instrument moves.
/// </summary>
/// <param name="MedianCandleRangePercent">High-to-low range of the middle candle, as a percentage of its close.</param>
/// <param name="UpperCandleRangePercent">Range of the widest candle in ten, as a percentage of its close.</param>
/// <param name="MedianDailyRangePercent">High-to-low range of the middle day, as a percentage of its close.</param>
/// <param name="AnnualisedVolatilityPercent">Standard deviation of daily returns, annualised over 252 days.</param>
public sealed record MarketMovement(
	decimal MedianCandleRangePercent,
	decimal UpperCandleRangePercent,
	decimal MedianDailyRangePercent,
	decimal AnnualisedVolatilityPercent);

/// <summary>
/// Whether moves tend to continue or to come back.
/// </summary>
/// <param name="Autocorrelation1">Correlation between a candle's return and the next one's.</param>
/// <param name="Autocorrelation5">Correlation between a candle's return and the one five candles later.</param>
/// <param name="VarianceRatio5">Variance of five-candle returns against five times the one-candle variance.</param>
/// <param name="VarianceRatio20">Variance of twenty-candle returns against twenty times the one-candle variance.</param>
/// <param name="DirectionalDaySharePercent">Share of days that closed near an extreme of their range rather than mid-range, in percent.</param>
/// <remarks>
/// Above one, a variance ratio says moves extend; below one, that they come back; around one, that the
/// instrument has no memory at that horizon.
/// </remarks>
public sealed record MarketPersistence(
	decimal Autocorrelation1,
	decimal Autocorrelation5,
	decimal VarianceRatio5,
	decimal VarianceRatio20,
	decimal DirectionalDaySharePercent);

/// <summary>
/// What followed the events a strategy would trade, five candles on.
/// </summary>
/// <param name="BreakoutCount">Candles that closed above the high of the twenty before.</param>
/// <param name="BreakoutFollowThroughPercent">Average return over the five candles after such a close.</param>
/// <param name="BreakoutPositiveSharePercent">Share of those still positive five candles later, in percent.</param>
/// <param name="BreakdownCount">Candles that closed below the low of the twenty before.</param>
/// <param name="BreakdownFollowThroughPercent">Average return over the five candles after such a close.</param>
/// <param name="StretchCount">Candles that closed more than two standard deviations of return from the average of the twenty before.</param>
/// <param name="StretchReversionPercent">Average return over the five candles after such a close, signed towards the average.</param>
public sealed record MarketEdge(
	int BreakoutCount,
	decimal BreakoutFollowThroughPercent,
	decimal BreakoutPositiveSharePercent,
	int BreakdownCount,
	decimal BreakdownFollowThroughPercent,
	int StretchCount,
	decimal StretchReversionPercent);

/// <summary>
/// Parts of the trading day.
/// </summary>
public enum SessionParts
{
	/// <summary>Outside the active session.</summary>
	Outside,

	/// <summary>The first thirty minutes of the active session.</summary>
	FirstHalfHour,

	/// <summary>Between the first and the last thirty minutes.</summary>
	Middle,

	/// <summary>The last thirty minutes of the active session.</summary>
	LastHalfHour,
}

/// <summary>
/// How one part of the day is shaped.
/// </summary>
/// <param name="Part">The part of the day.</param>
/// <param name="RangeSharePercent">Share of the total candle range that happens in it, in percent.</param>
/// <param name="VolumeSharePercent">Share of the total volume that happens in it, in percent.</param>
/// <param name="AverageReturnPercent">Average open-to-close return of a candle in it, in percent.</param>
public sealed record SessionShare(SessionParts Part, decimal RangeSharePercent, decimal VolumeSharePercent, decimal AverageReturnPercent);

/// <summary>
/// What limits how far a profile can be trusted.
/// </summary>
[Flags]
public enum MarketProfileCaveats
{
	/// <summary>Nothing.</summary>
	None = 0,

	/// <summary>Fewer than twenty trading days are covered, so anything measured per day rests on few observations.</summary>
	FewSessions = 1 << 0,

	/// <summary>Fewer than a thousand candles were measured, so the event counts are indicative rather than settled.</summary>
	FewCandles = 1 << 1,
}

/// <summary>
/// What was measured in one instrument's candles.
/// </summary>
/// <param name="Coverage">What the measurements rest on.</param>
/// <param name="Movement">How much it moves.</param>
/// <param name="Persistence">Whether moves continue or come back.</param>
/// <param name="Edge">What followed the events a strategy would trade.</param>
/// <param name="Session">How the day is shaped, for the parts of it that hold any candles.</param>
/// <param name="Caveats">What limits how far the numbers can be trusted.</param>
public sealed record MarketProfile(
	MarketCoverage Coverage,
	MarketMovement Movement,
	MarketPersistence Persistence,
	MarketEdge Edge,
	IReadOnlyList<SessionShare> Session,
	MarketProfileCaveats Caveats);

/// <summary>
/// Measures what an instrument's candle history contains: how it moves, whether moves persist, and what
/// followed breakouts and stretched closes.
/// </summary>
/// <remarks>
/// Measurements, not recommendations: nothing here says what to trade.
/// </remarks>
public static class MarketProfiler
{
	private const int _breakoutWindow = 20;
	private const int _horizon = 5;

	/// <summary>Fewest candles worth measuring anything on.</summary>
	public const int MinimumCandles = 200;

	/// <summary>
	/// Measures one instrument.
	/// </summary>
	/// <param name="candles">Finished candles of one instrument and one time frame, oldest first.</param>
	/// <returns>What was measured.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Fewer than <see cref="MinimumCandles"/> candles were given.</exception>
	public static MarketProfile Measure(IReadOnlyList<ICandleMessage> candles)
	{
		if (candles is null)
			throw new ArgumentNullException(nameof(candles));

		if (candles.Count < MinimumCandles)
			throw new ArgumentOutOfRangeException(nameof(candles), candles.Count, LocalizedStrings.InvalidValue);

		var returns = Returns(candles);
		var days = candles.GroupBy(c => c.OpenTime.Date).OrderBy(g => g.Key).ToArray();

		var caveats = MarketProfileCaveats.None;

		if (days.Length < 20)
			caveats |= MarketProfileCaveats.FewSessions;

		if (candles.Count < 1000)
			caveats |= MarketProfileCaveats.FewCandles;

		return new(
			MeasureCoverage(candles, days.Length),
			MeasureMovement(candles, days),
			MeasurePersistence(returns, days),
			MeasureEdge(candles, returns),
			MeasureSession(candles),
			caveats);
	}

	/// <summary>
	/// Finds the part of the day the instrument actually trades in: the narrowest window of the day that
	/// holds ninety percent of its volume.
	/// </summary>
	/// <param name="candles">Candles to look at.</param>
	/// <returns>Start and end of the active session, as times of day.</returns>
	/// <remarks>
	/// The first and last candle of the day would make the session as long as the extended hours the feed
	/// carries; where the volume is, is a property of the instrument rather than of an exchange schedule.
	/// </remarks>
	public static (TimeSpan Open, TimeSpan Close) ActiveSession(IReadOnlyList<ICandleMessage> candles)
	{
		if (candles is null)
			throw new ArgumentNullException(nameof(candles));

		var byTime = candles
			.GroupBy(c => c.OpenTime.TimeOfDay)
			.ToDictionary(g => g.Key, g => g.Sum(c => c.TotalVolume));

		var times = byTime.Keys.OrderBy(t => t).ToArray();

		if (times.Length == 0)
			return (TimeSpan.Zero, TimeSpan.FromHours(24));

		var total = byTime.Values.Sum();

		if (total <= 0)
			return (times[0], times[^1]);

		var target = total * 0.90m;
		var best = (Start: 0, End: times.Length - 1);
		var narrowest = TimeSpan.MaxValue;
		var running = 0m;
		var start = 0;

		for (var end = 0; end < times.Length; end++)
		{
			running += byTime[times[end]];

			while (running - byTime[times[start]] >= target && start < end)
			{
				running -= byTime[times[start]];
				start++;
			}

			if (running < target)
				continue;

			var width = times[end] - times[start];

			if (width >= narrowest)
				continue;

			narrowest = width;
			best = (start, end);
		}

		return (times[best.Start], times[best.End]);
	}

	private static MarketCoverage MeasureCoverage(IReadOnlyList<ICandleMessage> candles, int sessions)
	{
		var volumes = candles.Select(c => c.TotalVolume).ToArray();
		var high = 0;

		for (var i = 30; i < candles.Count; i++)
		{
			var average = candles.Skip(i - 30).Take(30).Average(c => c.TotalVolume);

			if (average > 0 && candles[i].TotalVolume > average * 2)
				high++;
		}

		return new(
			candles.Count,
			candles[0].OpenTime,
			candles[^1].OpenTime,
			sessions,
			Median(volumes),
			candles.Count > 30 ? Round((decimal)high / (candles.Count - 30) * 100) : 0);
	}

	private static MarketMovement MeasureMovement(IReadOnlyList<ICandleMessage> candles, IReadOnlyList<IGrouping<DateTime, ICandleMessage>> days)
	{
		var ranges = candles
			.Where(c => c.ClosePrice > 0)
			.Select(c => (c.HighPrice - c.LowPrice) / c.ClosePrice * 100)
			.OrderBy(r => r)
			.ToArray();

		var dailyRanges = days
			.Where(d => d.Last().ClosePrice > 0)
			.Select(d => (d.Max(c => c.HighPrice) - d.Min(c => c.LowPrice)) / d.Last().ClosePrice * 100)
			.OrderBy(r => r)
			.ToArray();

		var dailyReturns = days
			.Zip(days.Skip(1))
			.Where(p => p.First.Last().ClosePrice > 0)
			.Select(p => (double)((p.Second.Last().ClosePrice - p.First.Last().ClosePrice) / p.First.Last().ClosePrice))
			.ToArray();

		var annualised = dailyReturns.Length > 1
			? (decimal)(StandardDeviation(dailyReturns) * Math.Sqrt(252) * 100)
			: 0m;

		return new(
			Round(Median(ranges)),
			Round(Percentile(ranges, 0.9)),
			Round(Median(dailyRanges)),
			Round(annualised));
	}

	private static MarketPersistence MeasurePersistence(double[] returns, IReadOnlyList<IGrouping<DateTime, ICandleMessage>> days)
	{
		var directional = days.Count(d =>
		{
			var range = d.Max(c => c.HighPrice) - d.Min(c => c.LowPrice);

			return range > 0 && Math.Abs(d.Last().ClosePrice - d.First().OpenPrice) / range > 0.6m;
		});

		return new(
			Round((decimal)Autocorrelation(returns, 1)),
			Round((decimal)Autocorrelation(returns, 5)),
			Round((decimal)VarianceRatio(returns, 5)),
			Round((decimal)VarianceRatio(returns, 20)),
			days.Count > 0 ? Round((decimal)directional / days.Count * 100) : 0);
	}

	private static MarketEdge MeasureEdge(IReadOnlyList<ICandleMessage> candles, double[] returns)
	{
		var breakouts = new List<decimal>();
		var breakdowns = new List<decimal>();
		var stretches = new List<decimal>();

		var deviation = (decimal)StandardDeviation(returns);

		for (var i = _breakoutWindow; i < candles.Count - _horizon; i++)
		{
			var window = candles.Skip(i - _breakoutWindow).Take(_breakoutWindow).ToArray();
			var close = candles[i].ClosePrice;

			if (close <= 0)
				continue;

			var forward = (candles[i + _horizon].ClosePrice - close) / close * 100;

			if (close > window.Max(c => c.HighPrice))
				breakouts.Add(forward);

			if (close < window.Min(c => c.LowPrice))
				breakdowns.Add(forward);

			var mean = window.Average(c => c.ClosePrice);

			if (mean > 0 && deviation > 0)
			{
				var distance = (close - mean) / mean;

				// Signed towards the average, so a positive number means the move came back whichever way it was stretched.
				if (Math.Abs(distance) > deviation * 2)
					stretches.Add(distance > 0 ? -forward : forward);
			}
		}

		return new(
			breakouts.Count,
			breakouts.Count > 0 ? Round(breakouts.Average()) : 0,
			breakouts.Count > 0 ? Round((decimal)breakouts.Count(r => r > 0) / breakouts.Count * 100) : 0,
			breakdowns.Count,
			breakdowns.Count > 0 ? Round(breakdowns.Average()) : 0,
			stretches.Count,
			stretches.Count > 0 ? Round(stretches.Average()) : 0);
	}

	private static IReadOnlyList<SessionShare> MeasureSession(IReadOnlyList<ICandleMessage> candles)
	{
		var shares = new List<SessionShare>();

		var totalRange = candles.Sum(c => c.HighPrice - c.LowPrice);
		var totalVolume = candles.Sum(c => c.TotalVolume);

		foreach (var (part, test) in Parts(candles))
		{
			var inside = candles.Where(test).ToArray();

			if (inside.Length == 0)
				continue;

			var returns = inside.Where(c => c.OpenPrice > 0).Select(c => (c.ClosePrice - c.OpenPrice) / c.OpenPrice * 100).ToArray();

			shares.Add(new(
				part,
				totalRange > 0 ? Round(inside.Sum(c => c.HighPrice - c.LowPrice) / totalRange * 100) : 0,
				totalVolume > 0 ? Round(inside.Sum(c => c.TotalVolume) / totalVolume * 100) : 0,
				returns.Length > 0 ? Round(returns.Average()) : 0));
		}

		return shares;
	}

	private static IReadOnlyList<(SessionParts Part, Func<ICandleMessage, bool> Test)> Parts(IReadOnlyList<ICandleMessage> candles)
	{
		var (open, close) = ActiveSession(candles);

		var early = open.Add(TimeSpan.FromMinutes(30));
		var late = close.Subtract(TimeSpan.FromMinutes(30));

		// A session shorter than an hour holds no two separate half hours; unclamped, a candle between them
		// would fall in both and the shares would add up to more than the day.
		if (late < early)
			late = early;

		return
		[
			(SessionParts.Outside, c => c.OpenTime.TimeOfDay < open || c.OpenTime.TimeOfDay > close),
			(SessionParts.FirstHalfHour, c => c.OpenTime.TimeOfDay >= open && c.OpenTime.TimeOfDay < early),
			(SessionParts.Middle, c => c.OpenTime.TimeOfDay >= early && c.OpenTime.TimeOfDay < late),
			(SessionParts.LastHalfHour, c => c.OpenTime.TimeOfDay >= late && c.OpenTime.TimeOfDay <= close),
		];
	}

	private static double[] Returns(IReadOnlyList<ICandleMessage> candles)
		=> [.. candles
			.Zip(candles.Skip(1))
			.Where(p => p.First.ClosePrice > 0)
			.Select(p => (double)((p.Second.ClosePrice - p.First.ClosePrice) / p.First.ClosePrice))];

	private static double Autocorrelation(double[] values, int lag)
	{
		if (values.Length <= lag + 1)
			return 0;

		var mean = values.Average();
		var numerator = 0d;
		var denominator = 0d;

		for (var i = 0; i < values.Length; i++)
		{
			denominator += (values[i] - mean) * (values[i] - mean);

			if (i + lag < values.Length)
				numerator += (values[i] - mean) * (values[i + lag] - mean);
		}

		return denominator == 0 ? 0 : numerator / denominator;
	}

	private static double VarianceRatio(double[] values, int horizon)
	{
		if (values.Length <= horizon * 2)
			return 1;

		var single = Variance(values);

		if (single == 0)
			return 1;

		var aggregated = new List<double>();

		for (var i = 0; i + horizon <= values.Length; i += horizon)
			aggregated.Add(values.Skip(i).Take(horizon).Sum());

		return Variance([.. aggregated]) / (single * horizon);
	}

	private static double Variance(double[] values)
	{
		if (values.Length < 2)
			return 0;

		var mean = values.Average();

		return values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1);
	}

	private static double StandardDeviation(double[] values)
		=> Math.Sqrt(Variance(values));

	private static decimal Median(decimal[] values)
	{
		if (values.Length == 0)
			return 0;

		var ordered = values.OrderBy(v => v).ToArray();

		return ordered[ordered.Length / 2];
	}

	private static decimal Percentile(decimal[] ordered, double share)
		=> ordered.Length == 0 ? 0 : ordered[Math.Min(ordered.Length - 1, (int)(ordered.Length * share))];

	private static decimal Round(decimal value)
		=> Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
