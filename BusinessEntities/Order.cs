namespace StockSharp.BusinessEntities;

/// <summary>
/// Order.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderKey,
	Description = LocalizedStrings.InfoAboutOrderKey)]
public class Order : NotifiableObject, IOrderMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Order"/>.
	/// </summary>
	public Order()
	{
	}

	private SecurityId? _securityId;

	[BasicSetting]
	SecurityId ISecurityIdMessage.SecurityId
	{
		get => _securityId ??= Security?.Id.ToSecurityId() ?? default;
		set => throw new NotSupportedException();
	}

	DataType IGeneratedMessage.BuildFrom { get; set; }

	private TimeSpan? _latencyRegistration;

	/// <summary>
	/// Time taken to register an order.
	/// </summary>
	//[TimeSpan]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RegistrationKey,
		Description = LocalizedStrings.OrderRegLatencyKey,
		GroupName = LocalizedStrings.LatencyKey,
		Order = 1000)]
	public TimeSpan? LatencyRegistration
	{
		get => _latencyRegistration;
		set
		{
			if (_latencyRegistration == value)
				return;

			_latencyRegistration = value;
			NotifyChanged();
		}
	}

	private TimeSpan? _latencyCancellation;

	/// <summary>
	/// Time taken to cancel an order.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CancellationKey,
		Description = LocalizedStrings.OrderCancelLatencyKey,
		GroupName = LocalizedStrings.LatencyKey,
		Order = 1001)]
	public TimeSpan? LatencyCancellation
	{
		get => _latencyCancellation;
		set
		{
			if (_latencyCancellation == value)
				return;

			_latencyCancellation = value;
			NotifyChanged();
		}
	}

	private TimeSpan? _latencyEdition;

	/// <summary>
	/// Time taken to edit an order.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EditionKey,
		Description = LocalizedStrings.EditionLatencyKey,
		GroupName = LocalizedStrings.LatencyKey,
		Order = 1002)]
	public TimeSpan? LatencyEdition
	{
		get => _latencyEdition;
		set
		{
			if (_latencyEdition == value)
				return;

			_latencyEdition = value;
			NotifyChanged();
		}
	}

	private long? _id;

	/// <summary>
	/// Order ID.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdentifierKey,
		Description = LocalizedStrings.IdStringKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	[BasicSetting]
	public long? Id
	{
		get => _id;
		set
		{
			if (_id == value)
				return;

			_id = value;
			NotifyChanged();
		}
	}

	private string _stringId;

	/// <summary>
	/// Order ID (as string, if electronic board does not use numeric order ID representation).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdStringKey,
		Description = LocalizedStrings.OrderIdStringDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public string StringId
	{
		get => _stringId;
		set
		{
			_stringId = value;
			NotifyChanged();
		}
	}

	private string _boardId;

	/// <summary>
	/// Board order id. Uses in case of <see cref="Id"/> and <see cref="StringId"/> is a brokerage system ids.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderBoardIdKey,
		Description = LocalizedStrings.OrderBoardIdDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public string BoardId
	{
		get => _boardId;
		set
		{
			_boardId = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset _time;

	/// <summary>
	/// Order placing time on exchange.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RegTimeKey,
		Description = LocalizedStrings.RegTimeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public DateTimeOffset Time
	{
		get => _time;
		set
		{
			if (_time == value)
				return;

			_time = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Transaction ID. Automatically set when the <see cref="ITransactionProvider.RegisterOrder"/> method called.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TransactionKey,
		Description = LocalizedStrings.TransactionIdKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public long TransactionId { get; set; }

	/// <summary>
	/// Security, for which an order is being placed.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.OrderSecurityKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	public Security Security { get; set; }

	private OrderStates _state;

	/// <summary>
	/// Order state.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StateKey,
		Description = LocalizedStrings.OrderStateDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	[BasicSetting]
	public OrderStates State
	{
		get => _state;
		set
		{
			if (_state == value)
				return;

			_state = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Portfolio, in which the order is being traded.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfolioKey,
		Description = LocalizedStrings.OrderPortfolioKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	public Portfolio Portfolio { get; set; }

	private DateTimeOffset _serverTime;

	/// <inheritdoc/>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChangedKey,
		Description = LocalizedStrings.OrderLastChangeTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 8)]
	[BasicSetting]
	public DateTimeOffset ServerTime
	{
		get => _serverTime;
		set
		{
			if (_serverTime == value)
				return;

			_serverTime = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset? _cancelledTime;

	/// <summary>
	/// Cancelled time.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CancelKey,
		Description = LocalizedStrings.CancelledTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public DateTimeOffset? CancelledTime
	{
		get => _cancelledTime;
		set
		{
			if (_cancelledTime == value)
				return;

			_cancelledTime = value;
			NotifyChanged();
		}
	}

	private DateTimeOffset? _matchedTime;

	/// <summary>
	/// Cancelled time.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MatchKey,
		Description = LocalizedStrings.MatchedTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	public DateTimeOffset? MatchedTime
	{
		get => _matchedTime;
		set
		{
			if (_matchedTime == value)
				return;

			_matchedTime = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Time of last order change (Cancellation, Fill).
	/// </summary>
	[Obsolete("Use ServerTime property.")]
	[Browsable(false)]
	public DateTimeOffset LastChangeTime
	{
		get => ServerTime;
		set => ServerTime = value;
	}

	private DateTimeOffset _localTime;

	/// <summary>
	/// Last order change local time (Cancellation, Fill).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LocalTimeKey,
		Description = LocalizedStrings.LocalTimeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 11)]
	public DateTimeOffset LocalTime
	{
		get => _localTime;
		set
		{
			if (_localTime == value)
				return;

			_localTime = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Order price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.OrderPriceKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 12)]
	[BasicSetting]
	public decimal Price { get; set; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.OrderVolumeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 13)]
	[BasicSetting]
	public decimal Volume { get; set; }

	/// <summary>
	/// Visible quantity of contracts in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VisibleVolumeKey,
		Description = LocalizedStrings.VisibleVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 14)]
	public decimal? VisibleVolume { get; set; }

	/// <inheritdoc/>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DirectionKey,
		Description = LocalizedStrings.OrderSideDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 14)]
	[BasicSetting]
	public Sides Side { get; set; }

	/// <summary>
	/// Order side (buy or sell).
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use Side property.")]
	public Sides Direction
	{
		get => Side;
		set => Side = value;
	}

	private decimal _balance;

	/// <summary>
	/// Order contracts balance.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BalanceKey,
		Description = LocalizedStrings.OrderBalanceKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 15)]
	[BasicSetting]
	public decimal Balance
	{
		get => _balance;
		set
		{
			if (_balance == value)
				return;

			_balance = value;
			NotifyChanged();
		}
	}

	private long? _status;

	/// <summary>
	/// System order status.
	/// </summary>
	[DataMember]
	[Browsable(false)]
	public long? Status
	{
		get => _status;
		set
		{
			if (_status == value)
				return;

			_status = value;
			NotifyChanged();
		}
	}

	private bool? _isSystem;

	/// <summary>
	/// Is a system trade.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SystemKey,
		Description = LocalizedStrings.IsSystemTradeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 16)]
	public bool? IsSystem
	{
		get => _isSystem;
		set
		{
			if (_isSystem == value)
				return;

			_isSystem = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Placed order comment.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommentKey,
		Description = LocalizedStrings.OrderCommentKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 17)]
	[BasicSetting]
	public string Comment { get; set; }

	/// <summary>
	/// Order type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderTypeKey,
		Description = LocalizedStrings.OrderTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 18)]
	[BasicSetting]
	public OrderTypes? Type { get; set; }

	private DateTimeOffset? _expiryDate;

	/// <summary>
	/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
	/// </summary>
	/// <remarks>
	/// If the value is <see langword="null"/>, then the order is registered until cancel. Otherwise, the period is specified.
	/// </remarks>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpirationKey,
		Description = LocalizedStrings.OrderExpirationTimeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 19)]
	[BasicSetting]
	public DateTimeOffset? ExpiryDate
	{
		get => _expiryDate;
		set
		{
			if (_expiryDate == value)
				return;

			_expiryDate = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Order condition (e.g., stop- and algo- orders parameters).
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ConditionKey,
		Description = LocalizedStrings.OrderConditionDescKey,
		GroupName = LocalizedStrings.ConditionalOrderKey,
		Order = 20)]
	public OrderCondition Condition { get; set; }

	/// <summary>
	/// Limit order time in force.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeInForceKey,
		Description = LocalizedStrings.LimitOrderTifKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 21)]
	[BasicSetting]
	public TimeInForce? TimeInForce { get; set; }

	private Order _derivedOrder;

	/// <summary>
	/// Exchange order that was created by the stop-order when the condition is activated (<see langword="null" /> if a stop condition has not been activated).
	/// </summary>
	//[DataMember]
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LinkedOrderKey,
		Description = LocalizedStrings.LinkedOrderDescKey,
		GroupName = LocalizedStrings.ConditionalOrderKey)]
	[Browsable(false)]
	[Obsolete("No longer used.")]
	public Order DerivedOrder
	{
		get => _derivedOrder;
		set
		{
			if (_derivedOrder == value)
				return;

			_derivedOrder = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Commission (broker, exchange etc.).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionDescKey,
		GroupName = LocalizedStrings.CommissionKey,
		Order = 100)]
	public decimal? Commission { get; set; }

	/// <summary>
	/// Commission currency. Can be <see langword="null"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.CommissionKey,
		Order = 101)]
	public string CommissionCurrency { get; set; }

	/// <summary>
	/// User's order ID.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UserIdKey,
		Description = LocalizedStrings.UserOrderIdKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 23)]
	public string UserOrderId { get; set; }

	/// <summary>
	/// Strategy id.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StrategyKey,
		Description = LocalizedStrings.IdentifierKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 24)]
	public string StrategyId { get; set; }

	/// <summary>
	/// Broker firm code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BrokerKey,
		Description = LocalizedStrings.BrokerCodeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 24)]
	public string BrokerCode { get; set; }

	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClientCodeKey,
		Description = LocalizedStrings.ClientCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 25)]
	public string ClientCode { get; set; }

	/// <summary>
	/// Trading security currency.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 26)]
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// Is the order of market-maker.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarketMakerKey,
		Description = LocalizedStrings.MarketMakerOrderKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 27)]
	public bool? IsMarketMaker { get; set; }

	/// <summary>
	/// Margin mode.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarginKey,
		Description = LocalizedStrings.MarginModeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 28)]
	public MarginModes? MarginMode { get; set; }

	/// <summary>
	/// Slippage in trade price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.SlippageTradeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 29)]
	public decimal? Slippage { get; set; }

	/// <summary>
	/// Is order manual.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ManualKey,
		Description = LocalizedStrings.IsOrderManualKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 30)]
	public bool? IsManual { get; set; }

	/// <summary>
	/// Average execution price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AveragePriceKey,
		Description = LocalizedStrings.AveragePriceDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 31)]
	public decimal? AveragePrice { get; set; }

	/// <summary>
	/// Yield.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.YieldKey,
		Description = LocalizedStrings.YieldKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 32)]
	public decimal? Yield { get; set; }

	/// <summary>
	/// Minimum quantity of an order to be executed.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MinVolumeKey,
		Description = LocalizedStrings.MinVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 33)]
	public decimal? MinVolume { get; set; }

	/// <summary>
	/// Position effect.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionEffectKey,
		Description = LocalizedStrings.PositionEffectDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 34)]
	public OrderPositionEffects? PositionEffect { get; set; }

	/// <summary>
	/// Post-only order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PostOnlyKey,
		Description = LocalizedStrings.PostOnlyOrderKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 35)]
	public bool? PostOnly { get; set; }

	/// <summary>
	/// Sequence number.
	/// </summary>
	/// <remarks>Zero means no information.</remarks>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SeqNumKey,
		Description = LocalizedStrings.SequenceNumberKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 36)]
	public long SeqNum { get; set; }

	/// <summary>
	/// Margin leverage.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LeverageKey,
		Description = LocalizedStrings.MarginLeverageKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 37)]
	public int? Leverage { get; set; }

	OrderStates? IOrderMessage.State => State;
	decimal? IOrderMessage.Balance => Balance;
	decimal? IOrderMessage.Volume => Volume;

	/// <inheritdoc />
	public override string ToString()
	{
		var str = LocalizedStrings.OrderDetails
			.Put(TransactionId, Id == null ? StringId : Id.To<string>(), Security?.Id, Portfolio?.Name, Side == Sides.Buy ? LocalizedStrings.Buy2 : LocalizedStrings.Sell2, Price, Volume, State, Balance, Type);

		if (!ClientCode.IsEmpty())
			str += $" ClCode={ClientCode}";

		if (!UserOrderId.IsEmpty())
			str += $" UID={UserOrderId}";

		if (!StrategyId.IsEmpty())
			str += $" Strategy={StrategyId}";

		if (Condition != null)
			str += $" Condition={Condition}";

		if (AveragePrice != null)
			str += $" AvgPrice={AveragePrice}";

		if (MinVolume != null)
			str += $" MinVolume={MinVolume}";

		if (PositionEffect != null)
			str += $" PosEffect={PositionEffect.Value}";

		if (PostOnly != null)
			str += $",PostOnly={PostOnly.Value}";

		if (SeqNum != 0)
			str += $",SeqNum={SeqNum}";

		if (Leverage != null)
			str += $",Leverage={Leverage.Value}";

		return str;
	}
}
