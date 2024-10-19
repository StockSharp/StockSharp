namespace StockSharp.Messages;

/// <summary>
/// Type of the changes in <see cref="PositionChangeMessage"/>.
/// </summary>
[DataContract]
[Serializable]
public enum PositionChangeTypes
{
	/// <summary>
	/// Initial value.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BeginValueKey)]
	BeginValue,

	/// <summary>
	/// Current value.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrentValueKey)]
	CurrentValue,

	/// <summary>
	/// Blocked.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BlockedKey)]
	BlockedValue,

	/// <summary>
	/// Position price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PosPriceKey)]
	CurrentPrice,

	/// <summary>
	/// Average price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AveragePriceKey)]
	AveragePrice,

	/// <summary>
	/// Unrealized profit.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnrealizedProfitKey)]
	UnrealizedPnL,

	/// <summary>
	/// Realized profit.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RealizedProfitKey)]
	RealizedPnL,

	/// <summary>
	/// Variation margin.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VariationMarginKey)]
	VariationMargin,

	/// <summary>
	/// Currency.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrencyKey)]
	Currency,

	/// <summary>
	/// Extended information.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExtendedInfoKey)]
	[Obsolete]
	ExtensionInfo,

	/// <summary>
	/// Margin leverage.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarginLeverageKey)]
	Leverage,

	/// <summary>
	/// Total commission.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalCommissionKey)]
	Commission,

	/// <summary>
	/// Current value (in lots).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrValueInLotsKey)]
	CurrentValueInLots,

	/// <summary>
	/// The depositary where the physical security.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DepoKey)]
	[Obsolete]
	DepoName,

	/// <summary>
	/// Portfolio state.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StateKey)]
	State,

	/// <summary>
	/// Expiration date.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExpiryDateKey)]
	ExpirationDate,

	/// <summary>
	/// Commission (taker).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionTakerKey)]
	CommissionTaker,

	/// <summary>
	/// Commission (maker).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionMakerKey)]
	CommissionMaker,

	/// <summary>
	/// Settlement price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementPriceKey)]
	SettlementPrice,

	/// <summary>
	/// Orders (bids).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersBidsKey)]
	BuyOrdersCount,
	
	/// <summary>
	/// Orders (asks).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersAsksKey)]
	SellOrdersCount,
	
	/// <summary>
	/// Margin (buy).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarginBuyKey)]
	BuyOrdersMargin,
	
	/// <summary>
	/// Margin (sell).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarginSellKey)]
	SellOrdersMargin,
	
	/// <summary>
	/// Orders (margin).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersMarginKey)]
	OrdersMargin,

	/// <summary>
	/// Orders.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrdersCountKey)]
	OrdersCount,

	/// <summary>
	/// Trades.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TradesKey)]
	TradesCount,

	/// <summary>
	/// Liquidation price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LiquidationPriceKey)]
	LiquidationPrice,
}

/// <summary>
/// The message contains information about the position changes.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionKey,
	Description = LocalizedStrings.PositionDescKey)]
public class PositionChangeMessage : BaseChangeMessage<PositionChangeMessage,
	PositionChangeTypes>, IPortfolioNameMessage, ISecurityIdMessage, IStrategyIdMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.PortfolioNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string PortfolioName { get; set; }

	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClientCodeKey,
		Description = LocalizedStrings.ClientCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string ClientCode { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// The depositary where the physical security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DepoKey,
		Description = LocalizedStrings.DepoNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string DepoName { get; set; }

	/// <summary>
	/// Limit type for Т+ market.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LimitTypeKey,
		Description = LocalizedStrings.PosLimitKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public TPlusLimits? LimitType { get; set; }

	/// <summary>
	/// Text position description.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DescriptionKey,
		Description = LocalizedStrings.PosTextKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Description { get; set; }

	/// <summary>
	/// Electronic board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BoardCode { get; set; }

	/// <inheritdoc />
	[DataMember]
	public string StrategyId { get; set; }

	/// <summary>
	/// Side.
	/// </summary>
	[DataMember]
	public Sides? Side { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
	/// </summary>
	public PositionChangeMessage()
		: this(MessageTypes.PositionChange)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected PositionChangeMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.PositionChanges;

	/// <summary>
	/// Create a copy of <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new PositionChangeMessage();

		CopyTo(clone);

		return clone;
	}

	/// <inheritdoc />
	public override void CopyTo(PositionChangeMessage destination)
	{
		base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.DepoName = DepoName;
		destination.LimitType = LimitType;
		destination.Description = Description;
		destination.PortfolioName = PortfolioName;
		destination.ClientCode = ClientCode;
		destination.BoardCode = BoardCode;
		destination.StrategyId = StrategyId;
		destination.Side = Side;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Sec={SecurityId},P={PortfolioName},CL={ClientCode},L={LimitType},Changes={Changes.Select(c => c.ToString()).JoinComma()}";

		if (!StrategyId.IsEmpty())
			str += $",Strategy={StrategyId}";

		if (!Description.IsEmpty())
			str += $",Description={Description}";

		if (Side != null)
			str += $",Side={Side}";

		return str;
	}
}