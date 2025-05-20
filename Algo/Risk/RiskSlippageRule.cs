namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking slippage size.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SlippageKey,
	Description = LocalizedStrings.RiskSlippageKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class RiskSlippageRule : RiskRule
{
	private decimal _slippage;

	/// <summary>
	/// Slippage size.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.SlippageSizeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Slippage
	{
		get => _slippage;
		set
		{
			if (_slippage == value)
				return;

			_slippage = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _slippage.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Execution)
			return false;

		var execMsg = (ExecutionMessage)message;
		var currValue = execMsg.Slippage;

		if (currValue == null)
			return false;

		if (Slippage > 0)
			return currValue > Slippage;
		else
			return currValue < Slippage;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Slippage), Slippage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Slippage = storage.GetValue<decimal>(nameof(Slippage));
	}
}
