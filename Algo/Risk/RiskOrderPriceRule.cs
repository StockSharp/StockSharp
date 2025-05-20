namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking order price.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderPrice2Key,
	Description = LocalizedStrings.RiskOrderPriceKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class RiskOrderPriceRule : RiskRule
{
	private decimal _price;

	/// <summary>
	/// Order price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.OrderPriceKey,
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
		switch (message.Type)
		{
			case MessageTypes.OrderRegister:
			{
				var orderReg = (OrderRegisterMessage)message;
				return orderReg.Price >= Price;
			}

			case MessageTypes.OrderReplace:
			{
				var orderReplace = (OrderReplaceMessage)message;
				return orderReplace.Price > 0 && orderReplace.Price >= Price;
			}

			default:
				return false;
		}
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
