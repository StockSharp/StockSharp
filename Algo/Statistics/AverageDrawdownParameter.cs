namespace StockSharp.Algo.Statistics;

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
	private decimal _maxEquity;
	private decimal _drawdownStart;
	private bool _inDrawdown;

	private decimal _minEquityDuringDrawdown;
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
		_maxEquity = 0m;
		_drawdownStart = 0;
		_inDrawdown = false;

		_minEquityDuringDrawdown = decimal.MaxValue;
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
				var drawdown = _drawdownStart - _minEquityDuringDrawdown;
				if (drawdown > 0)
				{
					_drawdownSum += drawdown;
					_drawdownCount++;
				}
				_inDrawdown = false;
			}
			_maxEquity = equity;
			_drawdownStart = equity;
			_minEquityDuringDrawdown = equity;
		}
		else if (equity < _maxEquity)
		{
			if (!_inDrawdown)
			{
				_drawdownStart = _maxEquity;
				_minEquityDuringDrawdown = equity;
				_inDrawdown = true;
			}
			else
			{
				if (equity < _minEquityDuringDrawdown)
					_minEquityDuringDrawdown = equity;
			}
		}
		else
		{
			// equity == _maxEquity: if we were in a drawdown, it ends here
			if (_inDrawdown)
			{
				var drawdown = _drawdownStart - _minEquityDuringDrawdown;
				if (drawdown > 0)
				{
					_drawdownSum += drawdown;
					_drawdownCount++;
				}
				_inDrawdown = false;
			}
			_drawdownStart = equity;
			_minEquityDuringDrawdown = equity;
		}

		// For current value: include unfinished drawdown if any
		var tempSum = _drawdownSum;
		var tempCount = _drawdownCount;

		if (_inDrawdown)
		{
			var currDrawdown = _drawdownStart - _minEquityDuringDrawdown;
			if (currDrawdown > 0)
			{
				tempSum += currDrawdown;
				tempCount++;
			}
		}
		else if (_maxEquity == 0m && equity < 0)
		{
			// Special case: starting in negative zone, treat baseline as zero
			var currDrawdown = 0m - equity;
			if (currDrawdown > 0)
			{
				tempSum += currDrawdown;
				tempCount = Math.Max(1, tempCount + 1);
			}
		}

		Value = tempCount > 0 ? (tempSum / tempCount) : 0;
	}

	/// <inheritdoc/>
	public override void Save(SettingsStorage storage)
	{
		storage
			.Set("MaxEquity", _maxEquity)
			.Set("DrawdownStart", _drawdownStart)
			.Set("InDrawdown", _inDrawdown)
			.Set("MinEquityDuringDrawdown", _minEquityDuringDrawdown)
			.Set("DrawdownSum", _drawdownSum)
			.Set("DrawdownCount", _drawdownCount)
			;

		base.Save(storage);
	}

	/// <inheritdoc/>
	public override void Load(SettingsStorage storage)
	{
		_maxEquity = storage.GetValue<decimal>("MaxEquity");
		_drawdownStart = storage.GetValue<decimal>("DrawdownStart");
		_inDrawdown = storage.GetValue<bool>("InDrawdown");
		_minEquityDuringDrawdown = storage.GetValue<decimal>("MinEquityDuringDrawdown");
		_drawdownSum = storage.GetValue<decimal>("DrawdownSum");
		_drawdownCount = storage.GetValue<int>("DrawdownCount");

		base.Load(storage);
	}
}