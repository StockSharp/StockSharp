namespace StockSharp.BusinessEntities;

/// <summary>
/// Tick trade.
/// </summary>
[Serializable]
[DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeKey,
	Description = LocalizedStrings.TickTradeKey)]
[Obsolete("Use ITickTradeMessage.")]
public class Trade : Cloneable<Trade>, ITickTradeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Trade"/>.
	/// </summary>
	public Trade()
	{
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdentifierKey,
		Description = LocalizedStrings.TradeIdKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	[BasicSetting]
	public long? Id { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdStringKey,
		Description = LocalizedStrings.TradeIdStringKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public string StringId { get; set; }

	private SecurityId? _securityId;

	/// <inheritdoc />
	[BasicSetting]
	SecurityId ISecurityIdMessage.SecurityId
	{
		get => _securityId ??= Security?.Id.ToSecurityId() ?? default;
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// The instrument, on which the trade was completed.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public Security Security { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeKey,
		Description = LocalizedStrings.TradeTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	[BasicSetting]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	[Browsable(false)]
	[Obsolete("Use ServerTime property.")]
	public DateTimeOffset Time
	{
		get => ServerTime;
		set => ServerTime = value;
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LocalTimeKey,
		Description = LocalizedStrings.TradeLocalTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public DateTimeOffset LocalTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.TradeVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	[BasicSetting]
	public decimal Volume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.TradePriceDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	[BasicSetting]
	public decimal Price { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.InitiatorKey,
		Description = LocalizedStrings.DirectionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	[BasicSetting]
	public Sides? OriginSide { get; set; }

	/// <inheritdoc />
	[Browsable(false)]
	[Obsolete("Use OriginSide property.")]
	public Sides? OrderDirection
	{
		get => OriginSide;
		set => OriginSide = value;
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SystemTradeKey,
		Description = LocalizedStrings.IsSystemTradeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	public bool? IsSystem { get; set; }

	/// <inheritdoc />
	[Browsable(false)]
	public long? Status { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenInterestKey,
		Description = LocalizedStrings.OpenInterestDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 10)]
	public decimal? OpenInterest { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UpTrendKey,
		Description = LocalizedStrings.UpTrendDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 11)]
	public bool? IsUpTick { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	public CurrencyTypes? Currency { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc />
	public Messages.DataType BuildFrom { get; set; }

	/// <inheritdoc />
	[DataMember]
	public decimal? Yield { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? OrderBuyId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? OrderSellId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="Trade"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Trade Clone()
	{
		return new Trade
		{
			Id = Id,
			StringId = StringId,
			Volume = Volume,
			Price = Price,
			ServerTime = ServerTime,
			LocalTime = LocalTime,
			OriginSide = OriginSide,
			Security = Security,
			IsSystem = IsSystem,
			Status = Status,
			OpenInterest = OpenInterest,
			IsUpTick = IsUpTick,
			Currency = Currency,
			SeqNum = SeqNum,
			BuildFrom = BuildFrom,
			Yield = Yield,
			OrderBuyId = OrderBuyId,
			OrderSellId = OrderSellId,
		};
	}

	/// <summary>
	/// Get the hash code of the object <see cref="Trade"/>.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return (Security?.GetHashCode() ?? 0) ^ (Id ?? default).GetHashCode();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var idStr = Id is null ? StringId : Id.To<string>();
		return $"{ServerTime} {idStr} {Price} {Volume}";
	}
}