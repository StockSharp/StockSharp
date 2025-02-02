namespace StockSharp.BusinessEntities;

/// <summary>
/// The position by the instrument.
/// </summary>
[Serializable]
[DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionKey,
	Description = LocalizedStrings.PositionDescKey)]
public class Position : NotifiableObject, ILocalTimeMessage, IServerTimeMessage
{
	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => LastChangeTime;
		set => LastChangeTime = value;
	}

	/// <summary>
	/// Portfolio name.
	/// </summary>
	[Browsable(false)]
	public virtual string PortfolioName => Portfolio?.Name;

	/// <summary>
	/// Initializes a new instance of the <see cref="Position"/>.
	/// </summary>
	public Position()
	{
	}

	private decimal? _beginValue;

	/// <summary>
	/// Position size at the beginning of the trading session.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BeginValueKey,
		Description = LocalizedStrings.PosBeginValueKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	[Browsable(false)]
	public decimal? BeginValue
	{
		get => _beginValue;
		set
		{
			if (_beginValue == value)
				return;

			_beginValue = value;
			NotifyChanged();
		}
	}

	private decimal? _currentValue;

	/// <summary>
	/// Current position size.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrentValueKey,
		Description = LocalizedStrings.CurrentPosSizeKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	//[Browsable(false)]
	[BasicSetting]
	public decimal? CurrentValue
	{
		get => _currentValue;
		set
		{
			if (_currentValue == value)
				return;

			_currentValue = value;
			NotifyChanged();
		}
	}

	private decimal? _blockedValue;

	/// <summary>
	/// Position size, registered for active orders.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BlockedKey,
		Description = LocalizedStrings.PosBlockedSizeKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	[Browsable(false)]
	public decimal? BlockedValue
	{
		get => _blockedValue;
		set
		{
			if (_blockedValue == value)
				return;

			_blockedValue = value;
			NotifyChanged();
		}
	}

	private decimal? _currentPrice;

	/// <summary>
	/// Position price.
	/// </summary>
	//[Ignore]
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosPriceKey,
		Description = LocalizedStrings.PosPriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public decimal? CurrentPrice
	{
		get => _currentPrice;
		set
		{
			if (_currentPrice == value)
				return;

			_currentPrice = value;
			NotifyChanged();
		}
	}

	private decimal? _averagePrice;

	/// <summary>
	/// Average price.
	/// </summary>
	//[Ignore]
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AveragePriceKey,
		Description = LocalizedStrings.AveragePriceCalcTradesKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public decimal? AveragePrice
	{
		get => _averagePrice;
		set
		{
			if (_averagePrice == value)
				return;

			_averagePrice = value;
			NotifyChanged();
		}
	}

	private decimal? _unrealizedPnL;

	/// <summary>
	/// Unrealized profit.
	/// </summary>
	//[Ignore]
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnrealizedProfitKey,
		Description = LocalizedStrings.UnrealizedProfitDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public decimal? UnrealizedPnL
	{
		get => _unrealizedPnL;
		set
		{
			if (_unrealizedPnL == value)
				return;

			_unrealizedPnL = value;
			NotifyChanged();
		}
	}

	private decimal? _realizedPnL;

	/// <summary>
	/// Realized profit.
	/// </summary>
	//[Ignore]
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RealizedProfitKey,
		Description = LocalizedStrings.RealizedProfitDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public decimal? RealizedPnL
	{
		get => _realizedPnL;
		set
		{
			if (_realizedPnL == value)
				return;

			_realizedPnL = value;
			NotifyChanged();
		}
	}

	private decimal? _variationMargin;

	/// <summary>
	/// Variation margin.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VariationMarginKey,
		Description = LocalizedStrings.VariationMarginDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	[Browsable(false)]
	public decimal? VariationMargin
	{
		get => _variationMargin;
		set
		{
			if (_variationMargin == value)
				return;

			_variationMargin = value;
			NotifyChanged();
		}
	}

	private decimal? _commission;

	/// <summary>
	/// Total commission.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.TotalCommissionDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	[Browsable(false)]
	public decimal? Commission
	{
		get => _commission;
		set
		{
			if (_commission == value)
				return;

			_commission = value;
			NotifyChanged();
		}
	}

	private decimal? _settlementPrice;

	/// <summary>
	/// Settlement price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettlementPriceKey,
		Description = LocalizedStrings.SettlementPriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	//[Nullable]
	[Browsable(false)]
	public decimal? SettlementPrice
	{
		get => _settlementPrice;
		set
		{
			if (_settlementPrice == value)
				return;

			_settlementPrice = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset _lastChangeTime;

	/// <summary>
	/// Time of last position change.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChangedKey,
		Description = LocalizedStrings.TimePosLastChangeKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public DateTimeOffset LastChangeTime
	{
		get => _lastChangeTime;
		set
		{
			_lastChangeTime = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset _localTime;

	/// <summary>
	/// Local time of the last position change.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LocalTimeKey,
		Description = LocalizedStrings.LocalTimeDescKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public DateTimeOffset LocalTime
	{
		get => _localTime;
		set
		{
			_localTime = value;
			NotifyChanged();
		}
	}

	private string _description;

	/// <summary>
	/// Text position description.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DescriptionKey,
		Description = LocalizedStrings.PosTextKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Description
	{
		get => _description;
		set
		{
			_description = value;
			NotifyChanged();
		}
	}

	private CurrencyTypes? _currency;

	/// <summary>
	/// Portfolio currency.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.PortfolioCurrencyKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public CurrencyTypes? Currency
	{
		get => _currency;
		set
		{
			_currency = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset? _expirationDate;

	/// <summary>
	/// Expiration date.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpiryDateKey,
		Description = LocalizedStrings.ExpiryDateKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? ExpirationDate
	{
		get => _expirationDate;
		set
		{
			_expirationDate = value;
			NotifyChanged();
		}
	}

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

	/// <summary>
	/// Portfolio, in which position is created.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfolioKey,
		Description = LocalizedStrings.PosPortfolioKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Portfolio Portfolio { get; set; }

	/// <summary>
	/// Security, for which a position was created.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.PosSecurityKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Security Security { get; set; }

	/// <summary>
	/// The depositary where the physical security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DepoKey,
		Description = LocalizedStrings.DepoNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[DataMember]
	public string DepoName { get; set; }

	/// <summary>
	/// Limit type for Т+ market.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LimitKey,
		Description = LocalizedStrings.PosLimitKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	[DataMember]
	public TPlusLimits? LimitType { get; set; }

	/// <summary>
	/// Strategy id.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StrategyKey,
		Description = LocalizedStrings.IdentifierKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 100)]
	public virtual string StrategyId { get; set; }

	/// <summary>
	/// Side.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SideKey,
		Description = LocalizedStrings.PosSideKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 101)]
	public virtual Sides? Side { get; set; }

	private decimal? _leverage;

	/// <summary>
	/// Margin leverage.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LeverageKey,
		Description = LocalizedStrings.MarginLeverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Leverage
	{
		get => _leverage;
		set
		{
			if (_leverage == value)
				return;

			//if (value < 0)
			//	throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_leverage = value;
			NotifyChanged();
		}
	}

	private decimal? _commissionTaker;

	/// <summary>
	/// Commission (taker).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? CommissionTaker
	{
		get => _commissionTaker;
		set
		{
			_commissionTaker = value;
			NotifyChanged();
		}
	}

	private decimal? _commissionMaker;

	/// <summary>
	/// Commission (maker).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? CommissionMaker
	{
		get => _commissionMaker;
		set
		{
			_commissionMaker = value;
			NotifyChanged();
		}
	}

	private int? _buyOrdersCount;

	/// <summary>
	/// Orders (bids).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public int? BuyOrdersCount
	{
		get => _buyOrdersCount;
		set
		{
			_buyOrdersCount = value;
			NotifyChanged();
		}
	}

	private int? _sellOrdersCount;

	/// <summary>
	/// Orders (asks).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public int? SellOrdersCount
	{
		get => _sellOrdersCount;
		set
		{
			_sellOrdersCount = value;
			NotifyChanged();
		}
	}

	private decimal? _buyOrdersMargin;

	/// <summary>
	/// Margin (buy).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? BuyOrdersMargin
	{
		get => _buyOrdersMargin;
		set
		{
			_buyOrdersMargin = value;
			NotifyChanged();
		}
	}

	private decimal? _sellOrdersMargin;

	/// <summary>
	/// Margin (sell).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? SellOrdersMargin
	{
		get => _sellOrdersMargin;
		set
		{
			_sellOrdersMargin = value;
			NotifyChanged();
		}
	}

	private decimal? _ordersMargin;

	/// <summary>
	/// Orders (margin).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? OrdersMargin
	{
		get => _ordersMargin;
		set
		{
			_ordersMargin = value;
			NotifyChanged();
		}
	}

	private int? _ordersCount;

	/// <summary>
	/// Orders.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public int? OrdersCount
	{
		get => _ordersCount;
		set
		{
			_ordersCount = value;
			NotifyChanged();
		}
	}

	private int? _tradesCount;

	/// <summary>
	/// Trades.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public int? TradesCount
	{
		get => _tradesCount;
		set
		{
			_tradesCount = value;
			NotifyChanged();
		}
	}

	private decimal? _liquidationPrice;

	/// <summary>
	/// Liquidation price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LiquidationPriceKey,
		Description = LocalizedStrings.LiquidationPriceKey,
		GroupName = LocalizedStrings.StatisticsKey)]
	[Browsable(false)]
	public decimal? LiquidationPrice
	{
		get => _liquidationPrice;
		set
		{
			if (_liquidationPrice == value)
				return;

			_liquidationPrice = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Create a copy of <see cref="Position"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public virtual Position Clone()
	{
		var clone = new Position();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// To copy fields of the current position to <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The position in which you should to copy fields.</param>
	public void CopyTo(Position destination)
	{
		if (destination == null)
			throw new ArgumentNullException(nameof(destination));

		destination.CurrentValue = CurrentValue;
		destination.BeginValue = BeginValue;
		destination.BlockedValue = BlockedValue;
		destination.Commission = Commission;
		destination.VariationMargin = VariationMargin;
		destination.RealizedPnL = RealizedPnL;
		destination.UnrealizedPnL = UnrealizedPnL;
		destination.AveragePrice = AveragePrice;
		destination.CurrentPrice = CurrentPrice;
		destination.SettlementPrice = SettlementPrice;
		destination.Description = Description;
		destination.Currency = Currency;
		destination.ExpirationDate = ExpirationDate;
		destination.ClientCode = ClientCode;
		//destination.LastChangeTime = LastChangeTime;
		//destination.LocalTime = LocalTime;

		destination.Portfolio = Portfolio;
		destination.Security = Security;
		destination.DepoName = DepoName;
		destination.LimitType = LimitType;
		destination.StrategyId = StrategyId;
		destination.Side = Side;

		destination.Leverage = Leverage;
		destination.CommissionMaker = CommissionMaker;
		destination.CommissionTaker = CommissionTaker;

		destination.BuyOrdersCount = BuyOrdersCount;
		destination.SellOrdersCount = SellOrdersCount;
		destination.BuyOrdersMargin = BuyOrdersMargin;
		destination.SellOrdersMargin = SellOrdersMargin;
		destination.OrdersCount = OrdersCount;
		destination.TradesCount = TradesCount;

		destination.LiquidationPrice = LiquidationPrice;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = $"{Portfolio}-{Security}";

		if (!StrategyId.IsEmpty())
			str += $"-{StrategyId}";

		if (Side != null)
			str += $"-{Side.Value}";

		return str;
	}
}
