namespace StockSharp.Algo.Statistics;

/// <summary>
/// Base class for risk-adjusted ratios (Sharpe/Sortino).
/// </summary>
public abstract class RiskAdjustedRatioParameter : BasePnLStatisticParameter<decimal>, IRiskFreeRateStatisticParameter, IBeginValueStatisticParameter
{
	private decimal? _initialPnL;
	private decimal? _previousPnL;
	private double _periodsPerYear;

	private decimal _sumReturn; // Sum of returns
	private int _count;         // Number of returns

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
	public TimeSpan Period
	{
		get => _period;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_period = value;
			_periodsPerYear = 1.0 / (value.TotalDays / 365.25);
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
		_initialPnL = null;
		_previousPnL = null;
		_sumReturn = 0;
		_count = 0;

		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		if (_previousPnL != null)
		{
			if (BeginValue <= 0)
			{
				_previousPnL = pnl;
				Value = 0;
				return;
			}

			var previousEquity = BeginValue + _previousPnL.Value - _initialPnL.Value;

			// A run that has lost its capital has no return to speak of: a return is a fraction of what
			// was there to risk, and there was nothing. This arrives on the path that reports a position
			// change, so a ratio that cannot be worked out reports no ratio rather than stopping the run
			// that is reporting to it.
			if (previousEquity <= 0)
			{
				_previousPnL = pnl;
				Value = 0;
				return;
			}

			var ret = (pnl - _previousPnL.Value) / previousEquity;

			_sumReturn += ret;
			_count++;

			AddRiskSample(ret);
		}
		else
			_initialPnL = pnl;

		_previousPnL = pnl;

		if (_count < 2 || !HasEnoughRiskSamples(_count))
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
	/// Adds a new sample to the risk accumulator.
	/// </summary>
	/// <param name="ret">The return value.</param>
	protected abstract void AddRiskSample(decimal ret);

	/// <summary>
	/// Gets the risk value (e.g., stddev or downside deviation).
	/// </summary>
	/// <param name="count">Count of samples.</param>
	/// <param name="sumReturn">Sum of all returns.</param>
	/// <returns>Risk value.</returns>
	protected abstract decimal GetRisk(int count, decimal sumReturn);

	/// <summary>
	/// Checks if enough risk samples accumulated for calculation.
	/// </summary>
	/// <param name="count">Count of samples.</param>
	/// <returns>Check result.</returns>
	protected abstract bool HasEnoughRiskSamples(int count);

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(BeginValue), BeginValue);
		storage.SetValue("InitialPnL", _initialPnL);
		storage.SetValue("PreviousPnL", _previousPnL);
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
		_initialPnL = storage.GetValue<decimal?>("InitialPnL");
		RiskFreeRate = storage.GetValue<decimal>("RiskFreeRate");
		Period = storage.GetValue<TimeSpan>("Period");
		_previousPnL = storage.GetValue<decimal?>("PreviousPnL");
		_sumReturn = storage.GetValue<decimal>("SumReturn");
		_count = storage.GetValue<int>("Count");

		base.Load(storage);
	}
}
