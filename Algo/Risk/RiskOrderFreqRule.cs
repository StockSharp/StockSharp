namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking orders placing frequency.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderFreqKey,
	Description = LocalizedStrings.RiskOrderFreqKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class RiskOrderFreqRule : RiskRule
{
	private DateTimeOffset? _endTime;
	private int _current;

	/// <inheritdoc />
	protected override string GetTitle() => Count + " -> " + Interval;

	private int _count = 10;

	/// <summary>
	/// Order count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CountKey,
		Description = LocalizedStrings.OrdersCountKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public int Count
	{
		get => _count;
		set
		{
			if (_count == value)
				return;

			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_count = value;
			UpdateTitle();
		}
	}

	private TimeSpan _interval;


	/// <summary>
	/// Interval, during which orders quantity will be monitored.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IntervalKey,
		Description = LocalizedStrings.RiskIntervalDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public TimeSpan Interval
	{
		get => _interval;
		set
		{
			if (_interval == value)
				return;

			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_interval = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_current = 0;
		_endTime = null;
	}

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			{
				var time = message.LocalTime;

				if (time == default)
				{
					LogWarning("Time is null. Msg={0}", message);
					return false;
				}

				if (_endTime == null)
				{
					_endTime = time + Interval;
					_current = 1;

					LogDebug("EndTime={0}", _endTime);
					return false;
				}

				if (time < _endTime)
				{
					_current++;

					LogDebug("Count={0} Msg={1}", _current, message);

					if (_current >= Count)
					{
						LogInfo("Count={0} EndTime={1}", _current, _endTime);

						_endTime = null;
						return true;
					}
				}
				else
				{
					_endTime = time + Interval;
					_current = 1;

					LogDebug("EndTime={0}", _endTime);
				}

				return false;
			}
		}

		return false;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Count), Count);
		storage.SetValue(nameof(Interval), Interval);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Count = storage.GetValue<int>(nameof(Count));
		Interval = storage.GetValue<TimeSpan>(nameof(Interval));
	}
}
