namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking orders error count.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderErrorKey,
	Description = LocalizedStrings.RiskOrderErrorKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class RiskOrderErrorRule : RiskRule
{
	private int _current;

	private int _count;

	/// <summary>
	/// Error count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ErrorsKey,
		Description = LocalizedStrings.ErrorsCountKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public int Count
	{
		get => _count;
		set
		{
			if (_count == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_count = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => Count.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_current = 0;
	}

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Execution)
			return false;

		var execMsg = (ExecutionMessage)message;

		if (execMsg.IsOk())
		{
			if (execMsg.HasOrderInfo() && execMsg.OrderState == OrderStates.Active)
				_current = 0;

			return false;
		}

		return ++_current >= Count;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Count), Count);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Count = storage.GetValue<int>(nameof(Count));
	}
}
