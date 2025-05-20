namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking trade price.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradePriceKey,
	Description = LocalizedStrings.RiskTradePriceKey,
	GroupName = LocalizedStrings.TradesKey)]
public class RiskTradePriceRule : RiskRule
{
	private decimal _price;

	/// <summary>
	/// Trade price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.TradePriceDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Price
	{
		get => _price;
		set
		{
			if (_price == value)
				return;

			_price = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _price.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Execution)
			return false;

		var execMsg = (ExecutionMessage)message;

		if (!execMsg.HasTradeInfo())
			return false;

		return execMsg.TradePrice >= Price;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Price), Price);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Price = storage.GetValue<decimal>(nameof(Price));
	}
}
