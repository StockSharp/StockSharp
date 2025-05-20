namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The interface, describing statistic parameter, calculated based on trade.
/// </summary>
public interface ITradeStatisticParameter : IStatisticParameter
{
	/// <summary>
	/// To add information about new trade to the parameter.
	/// </summary>
	/// <param name="info">Information on new trade.</param>
	void Add(PnLInfo info);
}

/// <summary>
/// Base class for calculating trade statistic parameters by aggregating trades over aligned time periods.
/// </summary>
/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
public abstract class PerPeriodBaseTradeParameter(StatisticParameterTypes type) : BaseStatisticParameter<decimal>(type), ITradeStatisticParameter
{
	private DateTime _currStart;
	private int _currCount;

	private int _periodsCount;

	/// <inheritdoc />
	public override void Reset()
	{
		_currStart = default;
		_currCount = default;
		_periodsCount = default;

		base.Reset();
	}

	/// <summary>
	/// Align the specified date for exact period start.
	/// </summary>
	/// <param name="date">Trade date.</param>
	/// <returns>Aligned value.</returns>
	protected abstract DateTime Align(DateTime date);

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		var date = Align(info.ServerTime.UtcDateTime);

		if (_currStart == default)
		{
			_currStart = date;

			_periodsCount = 1;
			_currCount = 1;

			Value = _currCount;
		}
		else if (_currStart == date)
		{
			Value = ((Value * _periodsCount - _currCount) + ++_currCount) / _periodsCount;
		}
		else
		{
			_currStart = date;

			_currCount = 1;

			Value = (Value * _periodsCount + _currCount) / ++_periodsCount;
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage
			.Set("CurrStart", _currStart)
			.Set("PeriodsCount", _periodsCount)
			.Set("CurrCount", _currCount)
		;

		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_currStart = storage.GetValue<DateTime>("CurrStart");
		_periodsCount = storage.GetValue<int>("PeriodsCount");
		_currCount = storage.GetValue<int>("CurrCount");

		base.Load(storage);
	}
}