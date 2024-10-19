namespace StockSharp.Messages;

/// <summary>
/// The message containing the information for the order registration.
/// </summary>
[DataContract]
[Serializable]
public class OrderRegisterMessage : OrderMessage
{
	/// <summary>
	/// Order price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.OrderPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Price { get; set; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.OrderVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Volume { get; set; }

	/// <summary>
	/// Visible quantity of contracts in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VisibleVolumeKey,
		Description = LocalizedStrings.VisibleVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? VisibleVolume { get; set; }

	/// <summary>
	/// Order side (buy or sell).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DirectionKey,
		Description = LocalizedStrings.OrderSideDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Sides Side { get; set; }

	/// <summary>
	/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
	/// </summary>
	/// <remarks>
	/// If the value is equal <see langword="null" />, order will be GTC (good til cancel). Or uses exact date.
	/// </remarks>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpirationKey,
		Description = LocalizedStrings.OrderExpirationTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? TillDate { get; set; }

	/// <summary>
	/// Limit order time in force.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeInForceKey,
		Description = LocalizedStrings.LimitOrderTifKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public TimeInForce? TimeInForce { get; set; }

	/// <summary>
	/// Is the order of market-maker.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarketMakerKey,
		Description = LocalizedStrings.MarketMakerOrderKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public bool? IsMarketMaker { get; set; }

	/// <summary>
	/// Slippage in trade price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.SlippageTradeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? Slippage { get; set; }

	/// <summary>
	/// Is order manual.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ManualKey,
		Description = LocalizedStrings.IsOrderManualKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public bool? IsManual { get; set; }

	/// <summary>
	/// Minimum quantity of an order to be executed.
	/// </summary>
	[DataMember]
	public decimal? MinOrderVolume { get; set; }

	/// <summary>
	/// Position effect.
	/// </summary>
	[DataMember]
	public OrderPositionEffects? PositionEffect { get; set; }

	/// <summary>
	/// Post-only order.
	/// </summary>
	[DataMember]
	public bool? PostOnly { get; set; }

	/// <summary>
	/// Margin leverage.
	/// </summary>
	[DataMember]
	public int? Leverage { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderRegisterMessage"/>.
	/// </summary>
	public OrderRegisterMessage()
		: base(MessageTypes.OrderRegister)
	{
	}

	/// <summary>
	/// Initialize <see cref="OrderRegisterMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected OrderRegisterMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="OrderRegisterMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new OrderRegisterMessage(Type);
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(OrderRegisterMessage destination)
	{
		base.CopyTo(destination);

		destination.Price = Price;
		destination.Volume = Volume;
		destination.VisibleVolume = VisibleVolume;
		destination.Side = Side;
		destination.TillDate = TillDate;
		destination.TimeInForce = TimeInForce;
		destination.IsMarketMaker = IsMarketMaker;
		destination.Slippage = Slippage;
		destination.IsManual = IsManual;
		destination.MinOrderVolume = MinOrderVolume;
		destination.PositionEffect = PositionEffect;
		destination.PostOnly = PostOnly;
		destination.Leverage = Leverage;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Price={Price},Side={Side},Vol={Volume}/{VisibleVolume}/{MinOrderVolume},Till={TillDate},TIF={TimeInForce},MM={IsMarketMaker},SLP={Slippage},MN={IsManual}";

		if (PositionEffect != null)
			str += $",PosEffect={PositionEffect.Value}";

		if (PostOnly != null)
			str += $",PostOnly={PostOnly.Value}";

		if (Leverage != null)
			str += $",Leverage={Leverage.Value}";

		return str;
	}
}