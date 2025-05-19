namespace StockSharp.Algo.Statistics;

/// <summary>
/// The interface, describing statistic parameter, calculated based on the profit-loss value (maximal contraction, Sharp coefficient etc.).
/// </summary>
public interface IPnLStatisticParameter : IStatisticParameter
{
	/// <summary>
	/// To add new data to the parameter.
	/// </summary>
	/// <param name="marketTime">The exchange time.</param>
	/// <param name="pnl">The profit-loss value.</param>
	/// <param name="commission">Commission.</param>
	void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission);
}

/// <summary>
/// The base profit-loss statistics parameter.
/// </summary>
/// <typeparam name="TValue">The type of the parameter value.</typeparam>
/// <remarks>
/// Initialize <see cref="BasePnLStatisticParameter{TValue}"/>.
/// </remarks>
/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
public abstract class BasePnLStatisticParameter<TValue>(StatisticParameterTypes type) : BaseStatisticParameter<TValue>(type), IPnLStatisticParameter
	where TValue : IComparable<TValue>
{
	/// <inheritdoc />
	public virtual void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
		=> throw new NotSupportedException();
}

/// <summary>
/// Net profit for whole time period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NetProfitKey,
	Description = LocalizedStrings.NetProfitWholeTimeKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 0
)]
public class NetProfitParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="NetProfitParameter"/>.
	/// </summary>
	public NetProfitParameter()
		: base(StatisticParameterTypes.NetProfit)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		Value = pnl;
	}
}

/// <summary>
/// Net profit for whole time period in percent.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NetProfitPercentKey,
	Description = LocalizedStrings.NetProfitPercentDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 1
)]
public class NetProfitPercentParameter : BasePnLStatisticParameter<decimal>
{
	private decimal _beginValue;

	/// <summary>
	/// Initialize <see cref="NetProfitPercentParameter"/>.
	/// </summary>
	public NetProfitPercentParameter()
		: base(StatisticParameterTypes.NetProfitPercent)
	{
	}

	/// <inheritdoc />
	public override void Init(decimal beginValue)
	{
		base.Init(beginValue);

		_beginValue = beginValue;
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_beginValue == 0)
			return;

		Value = pnl * 100m / _beginValue;
	}
}

/// <summary>
/// The maximal profit value for the entire period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxProfitKey,
	Description = LocalizedStrings.MaxProfitWholePeriodKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 2
)]
public class MaxProfitParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxProfitParameter"/>.
	/// </summary>
	public MaxProfitParameter()
		: base(StatisticParameterTypes.MaxProfit)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		Value = Math.Max(Value, pnl);
	}
}

/// <summary>
/// Date of maximum profit value for the entire period.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxProfitDateParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxProfitParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxProfitDateKey,
	Description = LocalizedStrings.MaxProfitDateDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 3
)]
public class MaxProfitDateParameter(MaxProfitParameter underlying) : BasePnLStatisticParameter<DateTimeOffset>(StatisticParameterTypes.MaxProfitDate)
{
	private readonly MaxProfitParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	private decimal _prevValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevValue = default;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_prevValue < _underlying.Value)
		{
			_prevValue = _underlying.Value;
			Value = marketTime;
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("PrevValue", _prevValue);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_prevValue = storage.GetValue<decimal>("PrevValue");
		base.Load(storage);
	}
}

/// <summary>
/// Maximum absolute drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownKey,
	Description = LocalizedStrings.MaxDrawdownDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 4
)]
public class MaxDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxDrawdownParameter"/>.
	/// </summary>
	public MaxDrawdownParameter()
		: base(StatisticParameterTypes.MaxDrawdown)
	{
	}

	internal decimal MaxEquity = decimal.MinValue;

	/// <inheritdoc />
	public override void Reset()
	{
		MaxEquity = decimal.MinValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		MaxEquity = Math.Max(MaxEquity, pnl);
		Value = Math.Max(Value, MaxEquity - pnl);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MaxEquity", MaxEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		MaxEquity = storage.GetValue<decimal>("MaxEquity");
		base.Load(storage);
	}
}

/// <summary>
/// Maximum absolute drawdown during the whole period in percent.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxDrawdownPercentParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownPercentKey,
	Description = LocalizedStrings.MaxDrawdownPercentKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 5
)]
public class MaxDrawdownPercentParameter(MaxDrawdownParameter underlying) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.MaxDrawdownPercent)
{
	private readonly MaxDrawdownParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var maxEquity = _underlying.MaxEquity;

		Value = maxEquity != 0 ? (_underlying.Value * 100m / maxEquity) : 0;
	}
}

/// <summary>
/// Date of maximum absolute drawdown during the whole period.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxDrawdownDateParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownDateKey,
	Description = LocalizedStrings.MaxDrawdownDateDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 6
)]
public class MaxDrawdownDateParameter(MaxDrawdownParameter underlying) : BasePnLStatisticParameter<DateTimeOffset>(StatisticParameterTypes.MaxDrawdownDate)
{
	private readonly MaxDrawdownParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	private decimal _prevValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevValue = default;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_prevValue < _underlying.Value)
		{
			_prevValue = _underlying.Value;
			Value = marketTime;
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("PrevValue", _prevValue);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_prevValue = storage.GetValue<decimal>("PrevValue");
		base.Load(storage);
	}
}

/// <summary>
/// Maximum relative equity drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RelativeDrawdownKey,
	Description = LocalizedStrings.MaxRelativeDrawdownKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 7
)]
public class MaxRelativeDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxRelativeDrawdownParameter"/>.
	/// </summary>
	public MaxRelativeDrawdownParameter()
		: base(StatisticParameterTypes.MaxRelativeDrawdown)
	{
	}

	private decimal _maxEquity = decimal.MinValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_maxEquity = decimal.MinValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		_maxEquity = Math.Max(_maxEquity, pnl);

		var drawdown = _maxEquity - pnl;
		Value = Math.Max(Value, _maxEquity != 0 ? drawdown / _maxEquity : 0);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MaxEquity", _maxEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_maxEquity = storage.GetValue<decimal>("MaxEquity");
		base.Load(storage);
	}
}

/// <summary>
/// Relative income for the whole time period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RelativeIncomeKey,
	Description = LocalizedStrings.RelativeIncomeWholePeriodKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 8
)]
public class ReturnParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="ReturnParameter"/>.
	/// </summary>
	public ReturnParameter()
		: base(StatisticParameterTypes.Return)
	{
	}

	private decimal _minEquity = decimal.MaxValue;

	/// <inheritdoc />
	public override void Reset()
	{
		_minEquity = decimal.MaxValue;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		_minEquity = Math.Min(_minEquity, pnl);

		var profit = pnl - _minEquity;
		Value = Math.Max(Value, _minEquity != 0 ? profit / _minEquity : 0);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("MinEquity", _minEquity);
		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_minEquity = storage.GetValue<decimal>("MinEquity");
		base.Load(storage);
	}
}

/// <summary>
/// Recovery factor (net profit / maximum drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="RecoveryFactorParameter"/>.
/// </remarks>
/// <param name="maxDrawdown"><see cref="MaxDrawdownParameter"/></param>
/// <param name="netProfit"><see cref="NetProfitParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RecoveryFactorKey,
	Description = LocalizedStrings.RecoveryFactorDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 9
)]
public class RecoveryFactorParameter(MaxDrawdownParameter maxDrawdown, NetProfitParameter netProfit) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.RecoveryFactor)
{
	private readonly MaxDrawdownParameter _maxDrawdown = maxDrawdown ?? throw new ArgumentNullException(nameof(maxDrawdown));
	private readonly NetProfitParameter _netProfit = netProfit ?? throw new ArgumentNullException(nameof(netProfit));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_maxDrawdown.Value != 0)
			Value = _netProfit.Value / _maxDrawdown.Value;
	}
}

/// <summary>
/// Total commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CommissionKey,
	Description = LocalizedStrings.TotalCommissionDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 10
)]
public class CommissionParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="CommissionParameter"/>.
	/// </summary>
	public CommissionParameter()
		: base(StatisticParameterTypes.Commission)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (commission is not null)
			Value = commission.Value;
	}
}

/// <summary>
/// Base class for risk-adjusted ratios (Sharpe/Sortino).
/// </summary>
public abstract class RiskAdjustedRatioParameter : BasePnLStatisticParameter<decimal>
{
	private decimal? _previousPnL;
	private double _periodsPerYear;

	private decimal _sumReturn; // Sum of all returns
	private int _count;         // Number of returns

	private decimal _riskFreeRate;
	private TimeSpan _period;

	/// <summary>
	/// Annual risk-free rate (e.g., 0.03 = 3%).
	/// </summary>
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
		_previousPnL = null;
		_sumReturn = 0;
		_count = 0;

		base.Reset();
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_previousPnL != null)
		{
			var ret = pnl - _previousPnL.Value;

			_sumReturn += ret;
			_count++;

			AddRiskSample(ret);
		}

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

		Value = annualizedRisk != 0
			? (annualizedReturn - RiskFreeRate) / annualizedRisk
			: 0;
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
		RiskFreeRate = storage.GetValue<decimal>("RiskFreeRate");
		Period = storage.GetValue<TimeSpan>("Period");
		_previousPnL = storage.GetValue<decimal?>("PreviousPnL");
		_sumReturn = storage.GetValue<decimal>("SumReturn");
		_count = storage.GetValue<int>("Count");

		base.Load(storage);
	}
}

/// <summary>
/// Sharpe ratio (annualized return - risk-free rate / annualized standard deviation).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SharpeRatioKey,
	Description = LocalizedStrings.SharpeRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 11
)]
public class SharpeRatioParameter : RiskAdjustedRatioParameter
{
	private decimal _sumSq; // Sum of squared returns

	/// <summary>
	/// Initialize a new instance of the <see cref="SharpeRatioParameter"/> class.
	/// </summary>
	public SharpeRatioParameter()
		: base(StatisticParameterTypes.SharpeRatio)
	{
	}

	/// <inheritdoc />
	protected override void AddRiskSample(decimal ret)
	{
		_sumSq += ret * ret;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(int count, decimal sumReturn)
	{
		if (count < 2)
			return 0;

		var avg = sumReturn / count;
		return (decimal)Math.Sqrt((double)((_sumSq - avg * avg * count) / (count - 1)));
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(int count)
		=> count >= 2;

	/// <inheritdoc />
	public override void Reset()
	{
		_sumSq = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue("SumSq", _sumSq);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_sumSq = storage.GetValue<decimal>("SumSq");
	}
}

/// <summary>
/// Sortino ratio (annualized return - risk-free rate / annualized downside deviation).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SortinoRatioKey,
	Description = LocalizedStrings.SortinoRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 12
)]
public class SortinoRatioParameter : RiskAdjustedRatioParameter
{
	private decimal _downsideSumSq;
	private int _downsideCount;

	/// <summary>
	/// Initialize a new instance of the <see cref="SortinoRatioParameter"/> class.
	/// </summary>
	public SortinoRatioParameter()
		: base(StatisticParameterTypes.SortinoRatio)
	{
	}

	/// <inheritdoc />
	protected override void AddRiskSample(decimal ret)
	{
		if (ret >= 0)
			return;

		_downsideSumSq += ret * ret;
		_downsideCount++;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(int count, decimal sumReturn)
	{
		return _downsideCount > 0
			? (decimal)Math.Sqrt((double)(_downsideSumSq / _downsideCount))
			: 0;
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(int count)
		=> _downsideCount > 0;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_downsideSumSq = 0;
		_downsideCount = 0;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set("DownsideSumSq", _downsideSumSq);
		storage.Set("DownsideCount", _downsideCount);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_downsideSumSq = storage.GetValue<decimal>("DownsideSumSq");
		_downsideCount = storage.GetValue<int>("DownsideCount");
	}
}

/// <summary>
/// Calmar ratio (annualized net profit / max drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="CalmarRatioParameter"/>.
/// </remarks>
/// <param name="profit"><see cref="NetProfitParameter"/></param>
/// <param name="maxDrawdown"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CalmarRatioKey,
	Description = LocalizedStrings.CalmarRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 13
)]
public class CalmarRatioParameter(NetProfitParameter profit, MaxDrawdownParameter maxDrawdown) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.CalmarRatio)
{
	private readonly NetProfitParameter _profit = profit ?? throw new ArgumentNullException(nameof(profit));
	private readonly MaxDrawdownParameter _maxDrawdown = maxDrawdown ?? throw new ArgumentNullException(nameof(maxDrawdown));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var annualizedProfit = _profit.Value;
		var maxDrawdown = _maxDrawdown.Value;

		Value = maxDrawdown != 0 ? annualizedProfit / Math.Abs(maxDrawdown) : 0;
	}
}

/// <summary>
/// Sterling ratio (annualized net profit / average drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="SterlingRatioParameter"/>.
/// </remarks>
/// <param name="profit"><see cref="NetProfitParameter"/></param>
/// <param name="avgDrawdown"><see cref="AverageDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SterlingRatioKey,
	Description = LocalizedStrings.SterlingRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 14
)]
public class SterlingRatioParameter(NetProfitParameter profit, AverageDrawdownParameter avgDrawdown) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.SterlingRatio)
{
	private readonly NetProfitParameter _profit = profit ?? throw new ArgumentNullException(nameof(profit));
	private readonly AverageDrawdownParameter _avgDrawdown = avgDrawdown ?? throw new ArgumentNullException(nameof(avgDrawdown));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var annualizedProfit = _profit.Value;
		var avgDrawdown = _avgDrawdown.Value;

		Value = avgDrawdown != 0 ? annualizedProfit / Math.Abs(avgDrawdown) : 0;
	}
}

/// <summary>
/// Average drawdown during the whole period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AverageDrawdownKey,
	Description = LocalizedStrings.AverageDrawdownDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 15
)]
public class AverageDrawdownParameter : BasePnLStatisticParameter<decimal>
{
	private decimal _lastEquity;
	private decimal _maxEquity = decimal.MinValue;
	private decimal _drawdownStart;
	private bool _inDrawdown;

	private int _drawdownCount;
	private decimal _drawdownSum;

	/// <summary>
	/// Initialize a new instance of the <see cref="AverageDrawdownParameter"/> class.
	/// </summary>
	public AverageDrawdownParameter()
		: base(StatisticParameterTypes.AverageDrawdown)
	{
	}

	/// <inheritdoc/>
	public override void Reset()
	{
		_lastEquity = 0;
		_maxEquity = decimal.MinValue;
		_drawdownStart = 0;
		_inDrawdown = false;

		_drawdownCount = 0;
		_drawdownSum = 0;

		base.Reset();
	}

	/// <inheritdoc/>
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var equity = pnl;

		if (equity > _maxEquity)
		{
			if (_inDrawdown)
			{
				var drawdown = _drawdownStart - _lastEquity;

				if (drawdown > 0)
				{
					_drawdownSum += drawdown;
					_drawdownCount++;
				}

				_inDrawdown = false;
			}

			_maxEquity = equity;
			_drawdownStart = equity;
		}
		else if (equity < _maxEquity)
		{
			if (!_inDrawdown)
			{
				_drawdownStart = _maxEquity;
				_inDrawdown = true;
			}
		}

		_lastEquity = equity;

		// Compute average including current unfinished drawdown if any
		var tempSum = _drawdownSum;
		var tempCount = _drawdownCount;

		if (_inDrawdown)
		{
			var currDrawdown = _drawdownStart - _lastEquity;

			if (currDrawdown > 0)
			{
				tempSum += currDrawdown;
				tempCount++;
			}
		}

		Value = tempCount > 0 ? (tempSum / tempCount) : 0;
	}

	/// <inheritdoc/>
	public override void Save(SettingsStorage storage)
	{
		storage
			.Set("LastEquity", _lastEquity)
			.Set("MaxEquity", _maxEquity)
			.Set("DrawdownStart", _drawdownStart)
			.Set("InDrawdown", _inDrawdown)
			.Set("DrawdownSum", _drawdownSum)
			.Set("DrawdownCount", _drawdownCount)
		;

		base.Save(storage);
	}

	/// <inheritdoc/>
	public override void Load(SettingsStorage storage)
	{
		_lastEquity = storage.GetValue<decimal>("LastEquity");
		_maxEquity = storage.GetValue<decimal>("MaxEquity");
		_drawdownStart = storage.GetValue<decimal>("DrawdownStart");
		_inDrawdown = storage.GetValue<bool>("InDrawdown");
		_drawdownSum = storage.GetValue<decimal>("DrawdownSum");
		_drawdownCount = storage.GetValue<int>("DrawdownCount");

		base.Load(storage);
	}
}