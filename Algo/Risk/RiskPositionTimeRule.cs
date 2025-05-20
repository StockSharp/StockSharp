namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking position lifetime.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionTimeKey,
	Description = LocalizedStrings.RulePositionTimeKey,
	GroupName = LocalizedStrings.PositionsKey)]
public class RiskPositionTimeRule : RiskRule
{
	private readonly Dictionary<Tuple<SecurityId, string>, DateTimeOffset> _posOpenTime = [];
	private TimeSpan _time;

	/// <summary>
	/// Position lifetime.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeKey,
		Description = LocalizedStrings.PositionTimeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public TimeSpan Time
	{
		get => _time;
		set
		{
			if (_time == value)
				return;

			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_time = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _time.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_posOpenTime.Clear();
	}

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.PositionChange:
			{
				var posMsg = (PositionChangeMessage)message;
				var currValue = posMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

				if (currValue == null)
					return false;

				var key = Tuple.Create(posMsg.SecurityId, posMsg.PortfolioName);

				if (currValue == 0)
				{
					_posOpenTime.Remove(key);
					return false;
				}

				if (!_posOpenTime.TryGetValue(key, out var openTime))
				{
					_posOpenTime.Add(key, posMsg.LocalTime);
					return false;
				}

				var diff = posMsg.LocalTime - openTime;

				if (diff < Time)
					return false;

				_posOpenTime.Remove(key);
				return true;
			}

			case MessageTypes.Time:
			{
				List<Tuple<SecurityId, string>> removingPos = null;

				foreach (var pair in _posOpenTime)
				{
					var diff = message.LocalTime - pair.Value;

					if (diff < Time)
						continue;

					removingPos ??= [];

					removingPos.Add(pair.Key);
				}

				removingPos?.ForEach(t => _posOpenTime.Remove(t));

				return removingPos != null;
			}
		}

		return false;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Time), Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Time = storage.GetValue<TimeSpan>(nameof(Time));
	}
}
