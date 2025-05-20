namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking commission size.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CommissionKey,
	Description = LocalizedStrings.RiskCommissionKey,
	GroupName = LocalizedStrings.PnLKey)]
public class RiskCommissionRule : RiskRule
{
	private decimal _commission;

	/// <summary>
	/// Commission size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Commission
	{
		get => _commission;
		set
		{
			if (_commission == value)
				return;

			_commission = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _commission.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.PositionChange)
			return false;

		var pfMsg = (PositionChangeMessage)message;

		if (!pfMsg.IsMoney())
			return false;

		var currValue = pfMsg.TryGetDecimal(PositionChangeTypes.Commission);

		if (currValue == null)
			return false;

		return currValue >= Commission;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Commission), Commission);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Commission = storage.GetValue<decimal>(nameof(Commission));
	}
}
