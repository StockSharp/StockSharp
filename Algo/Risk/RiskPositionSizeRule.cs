namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking position size.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionKey,
	Description = LocalizedStrings.RulePositionKey,
	GroupName = LocalizedStrings.PositionsKey)]
public class RiskPositionSizeRule : RiskRule
{
	private decimal _position;

	/// <summary>
	/// Position size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionKey,
		Description = LocalizedStrings.PositionSizeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Position
	{
		get => _position;
		set
		{
			if (_position == value)
				return;

			_position = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _position.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.PositionChange)
			return false;

		var posMsg = (PositionChangeMessage)message;
		var currValue = posMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

		if (currValue == null)
			return false;

		if (Position > 0)
			return currValue >= Position;
		else
			return currValue <= Position;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Position), Position);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Position = storage.GetValue<decimal>(nameof(Position));
	}
}
