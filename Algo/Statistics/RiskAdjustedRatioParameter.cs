namespace StockSharp.Algo.Statistics;

/// <summary>
/// Base class for risk-adjusted ratios (Sharpe/Sortino).
/// </summary>
public abstract class RiskAdjustedRatioParameter : BasePnLStatisticParameter<decimal>, IRiskFreeRateStatisticParameter, IBeginValueStatisticParameter
{
	private DateTime? _gridStart;
	private decimal? _initialPnL;
	private decimal _periodStartPnL;
	private decimal _lastPnL;
	private long _closedPeriods;

	private double _periodsPerYear;

	private decimal _sumReturn; // Sum of returns
	private long _count;        // Number of returns

	private decimal _riskFreeRate;
	private TimeSpan _period;

	/// <inheritdoc />
	public decimal BeginValue { get; set; }

	/// <inheritdoc />
	public decimal RiskFreeRate
	{
		get => _riskFreeRate;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_riskFreeRate = value;
		}
	}

	/// <summary>
	/// Return calculation period.
	/// </summary>
	/// <remarks>
	/// Returns are measured on a grid of periods of this length, opened by the first reported PnL. The value of
	/// a period is the last PnL reported at or before its closing boundary, so a period in which nothing was
	/// reported closes on the level it opened with and yields a return of zero. A trailing period that the run
	/// ends in the middle of is not a period and is not measured. The same length is what the annualization
	/// assumes, so it is also what the reported ratio is a per-year figure of. Every boundary of one grid is
	/// measured at one length, so the length can only be set before the first PnL is reported or after
	/// <see cref="Reset"/>.
	/// </remarks>
	/// <exception cref="InvalidOperationException">The grid is already open and the new length differs from the current one.</exception>
	public TimeSpan Period
	{
		get => _period;
		set
		{
			if (value != _period && _gridStart is not null)
				throw new InvalidOperationException("The return period cannot be changed while the grid is open.");

			SetPeriod(value);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskAdjustedRatioParameter"/> class.
	/// </summary>
	/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
	protected RiskAdjustedRatioParameter(StatisticParameterTypes type)
		: base(type)
	{
		Period = TimeSpan.FromDays(1);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_gridStart = null;
		_initialPnL = null;
		_periodStartPnL = 0;
		_lastPnL = 0;
		_closedPeriods = 0;
		_sumReturn = 0;
		_count = 0;

		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		// A report that carries no time says nothing about when anything happened, so it is not an observation:
		// it neither opens the grid nor closes a period nor becomes the level a period closes on. A strategy
		// reset produces exactly such a report.
		if (marketTime == default)
			return;

		if (_gridStart is null)
		{
			// The first report opens the grid and fixes the baseline that every later equity is measured from.
			_gridStart = marketTime;
			_initialPnL = pnl;
			_periodStartPnL = pnl;
			_lastPnL = pnl;

			Value = 0;
			return;
		}

		var elapsed = (marketTime - _gridStart.Value).Ticks;

		long held = 0;
		long reached = 0;

		if (elapsed > 0)
		{
			var length = _period.Ticks;

			// A boundary strictly before this report closes on the level held since the previous one; a
			// boundary the report lands on exactly closes on the report itself.
			reached = elapsed / length;
			held = (elapsed - 1) / length;
		}

		var measured = ClosePeriods(held, _lastPnL);

		_lastPnL = pnl;

		if (!ClosePeriods(reached, pnl))
			measured = false;

		if (!measured || _count < 2 || !HasEnoughRiskSamples(_count))
		{
			Value = 0;
			return;
		}

		var avgReturn = _sumReturn / _count;
		var annualizedReturn = avgReturn * (decimal)_periodsPerYear;
		var risk = GetRisk(_count, _sumReturn);
		var annualizedRisk = risk * (decimal)Math.Sqrt(_periodsPerYear);

		var excessReturn = annualizedReturn - RiskFreeRate;

		Value = annualizedRisk != 0
			? excessReturn / annualizedRisk
			: excessReturn.Sign() switch
			{
				1 => decimal.MaxValue,
				-1 => decimal.MinValue,
				_ => 0,
			};
	}

	/// <summary>
	/// Sets <see cref="Period"/> without the open-grid check, for restoring a state that was saved with it.
	/// </summary>
	/// <param name="value">Return calculation period.</param>
	private void SetPeriod(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

		_period = value;
		_periodsPerYear = 1.0 / (value.TotalDays / 365.25);
	}

	/// <summary>
	/// Closes every grid boundary up to <paramref name="target"/> on the given PnL level.
	/// </summary>
	/// <param name="target">The number of boundaries the grid is to have closed.</param>
	/// <param name="level">The PnL level those boundaries close on.</param>
	/// <returns><see langword="true"/> if every closed period produced a return; <see langword="false"/> if any of them had no capital to measure a return against.</returns>
	private bool ClosePeriods(long target, decimal level)
	{
		var periods = target - _closedPeriods;

		if (periods <= 0)
			return true;

		var start = _periodStartPnL;

		// _closedPeriods counts boundaries the grid has crossed, not returns recorded: time passes whether or
		// not there was capital to measure it against, and the next report is measured from here either way.
		_closedPeriods = target;
		_periodStartPnL = level;

		var measured = AddSample(start, level, 1);

		// The boundaries after the first were crossed without a report of their own, so they open and close on
		// the same level: the strategy stood still for that long, which is a return of zero. Whether that zero
		// can be recorded depends on the equity at that level, not on the equity the first period opened with:
		// capital lost by one boundary can be back by the next.
		if (periods > 1 && !AddSample(level, level, periods - 1))
			measured = false;

		return measured;
	}

	/// <summary>
	/// Records <paramref name="count"/> periods that opened on <paramref name="start"/> and closed on <paramref name="level"/>.
	/// </summary>
	/// <param name="start">The PnL level the periods opened on.</param>
	/// <param name="level">The PnL level they closed on.</param>
	/// <param name="count">How many periods earned that return.</param>
	/// <returns><see langword="true"/> if the return was recorded; <see langword="false"/> if there was no capital to measure it against.</returns>
	private bool AddSample(decimal start, decimal level, long count)
	{
		if (BeginValue <= 0)
			return false;

		var startEquity = BeginValue + start - _initialPnL.Value;

		// A return is a fraction of the capital its period opened with, so a period that opened with none has
		// no return to record - it is not a return of zero.
		if (startEquity <= 0)
			return false;

		var ret = (level - start) / startEquity;

		_sumReturn += ret * count;
		_count += count;

		AddRiskSample(ret, count);

		return true;
	}

	/// <summary>
	/// Adds consecutive periods of the same return to the risk accumulator.
	/// </summary>
	/// <param name="ret">The return value.</param>
	/// <param name="count">How many periods earned it.</param>
	protected abstract void AddRiskSample(decimal ret, long count);

	/// <summary>
	/// Gets the risk value (e.g., stddev or downside deviation).
	/// </summary>
	/// <param name="count">Count of samples.</param>
	/// <param name="sumReturn">Sum of all returns.</param>
	/// <returns>Risk value.</returns>
	protected abstract decimal GetRisk(long count, decimal sumReturn);

	/// <summary>
	/// Checks if enough risk samples accumulated for calculation.
	/// </summary>
	/// <param name="count">Count of samples.</param>
	/// <returns>Check result.</returns>
	protected abstract bool HasEnoughRiskSamples(long count);

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(BeginValue), BeginValue);
		storage.SetValue("GridStart", _gridStart);
		storage.SetValue("InitialPnL", _initialPnL);
		storage.SetValue("PeriodStartPnL", _periodStartPnL);
		storage.SetValue("LastPnL", _lastPnL);
		storage.SetValue("ClosedPeriods", _closedPeriods);
		storage.SetValue("RiskFreeRate", RiskFreeRate);
		storage.SetValue("Period", Period);
		storage.SetValue("SumReturn", _sumReturn);
		storage.SetValue("Count", _count);

		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		BeginValue = storage.GetValue<decimal>(nameof(BeginValue));
		_gridStart = storage.GetValue<DateTime?>("GridStart");
		_initialPnL = storage.GetValue<decimal?>("InitialPnL");
		_periodStartPnL = storage.GetValue<decimal>("PeriodStartPnL");
		_lastPnL = storage.GetValue<decimal>("LastPnL");
		_closedPeriods = storage.GetValue<long>("ClosedPeriods");
		RiskFreeRate = storage.GetValue<decimal>("RiskFreeRate");
		SetPeriod(storage.GetValue<TimeSpan>("Period"));
		_sumReturn = storage.GetValue<decimal>("SumReturn");
		_count = storage.GetValue<long>("Count");

		base.Load(storage);
	}
}
