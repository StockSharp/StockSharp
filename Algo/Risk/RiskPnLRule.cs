namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking profit-loss.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PnLKey,
	Description = LocalizedStrings.RulePnLKey,
	GroupName = LocalizedStrings.PnLKey)]
public class RiskPnLRule : RiskRule
{
	private decimal? _initValue;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_initValue = null;
	}

	private Unit _pnL = new();

	/// <summary>
	/// Profit-loss.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PnLKey,
		Description = LocalizedStrings.PnLKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public Unit PnL
	{
		get => _pnL;
		set
		{
			if (_pnL == value)
				return;

			_pnL = value ?? throw new ArgumentNullException(nameof(value));
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _pnL?.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.PositionChange)
			return false;

		var pfMsg = (PositionChangeMessage)message;

		if (!pfMsg.IsMoney())
			return false;

		var currValue = pfMsg.TryGetDecimal(PositionChangeTypes.CurrentValue);

		if (currValue == null)
			return false;

		if (_initValue == null)
		{
			_initValue = currValue.Value;
			return false;
		}

		if (PnL.Type == UnitTypes.Limit)
		{
			if (PnL.Value > 0)
				return PnL.Value <= currValue.Value;
			else
				return PnL.Value >= currValue.Value;
		}

		if (PnL.Value > 0)
			return (_initValue + PnL) <= currValue.Value;
		else
			return (_initValue + PnL) >= currValue.Value;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(PnL), PnL);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		PnL = storage.GetValue<Unit>(nameof(PnL));
	}
}
