namespace StockSharp.Messages;

/// <summary>
/// The types of data that contain information in <see cref="ExecutionMessage"/>.
/// </summary>
[DataContract]
[Serializable]
public enum ExecutionTypes
{
	/// <summary>
	/// Tick trade.
	/// </summary>
	[EnumMember]
	Tick,

	/// <summary>
	/// Transaction.
	/// </summary>
	[EnumMember]
	Transaction,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[EnumMember]
	[Obsolete]
	Obsolete,

	/// <summary>
	/// Order log.
	/// </summary>
	[EnumMember]
	OrderLog,
}

/// <summary>
/// The message contains information about the execution.
/// </summary>
[Serializable]
[DataContract]
public class ExecutionMessage : BaseSubscriptionIdMessage<ExecutionMessage>,
	ITransactionIdMessage, IServerTimeMessage, ISecurityIdMessage, ISeqNumMessage,
	IPortfolioNameMessage, IErrorMessage, IStrategyIdMessage, IGeneratedMessage,
	IOrderMessage, ITickTradeMessage, IOrderLogMessage, ISystemMessage
{
	OrderStates? IOrderMessage.State => OrderState;
	decimal? IOrderMessage.Balance => Balance;
	decimal? IOrderMessage.Volume => OrderVolume;

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId SecurityId { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfolioKey,
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

	/// <summary>
	/// Broker firm code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BrokerKey,
		Description = LocalizedStrings.BrokerCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BrokerCode { get; set; }

	/// <summary>
	/// The depositary where the physical security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DepoKey,
		Description = LocalizedStrings.DepoNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string DepoName { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ServerTimeKey,
		Description = LocalizedStrings.ServerTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TransactionKey,
		Description = LocalizedStrings.TransactionIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public long TransactionId { get; set; }

	/// <summary>
	/// Data type, information about which is contained in the <see cref="ExecutionMessage"/>.
	/// </summary>
	[DataMember]
	//[Nullable]
	[Obsolete("Use DataTypeEx property.")]
	[Browsable(false)]
	public ExecutionTypes? ExecutionType
	{
		get => DataTypeEx.ToExecutionType();
		set => DataTypeEx = value?.ToDataType();
	}

	/// <summary>
	/// Is the action an order cancellation.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CancelKey,
		Description = LocalizedStrings.IsActionOrderCancellationKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public bool IsCancellation { get; set; }

	/// <summary>
	/// Order ID.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderIdKey,
		Description = LocalizedStrings.OrderIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public long? OrderId { get; set; }

	/// <summary>
	/// Order ID (as string, if electronic board does not use numeric order ID representation).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdStringKey,
		Description = LocalizedStrings.OrderIdStringDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string OrderStringId { get; set; }

	/// <summary>
	/// Board order id. Uses in case of <see cref="OrderId"/> and <see cref="OrderStringId"/> is a brokerage system ids.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderBoardIdKey,
		Description = LocalizedStrings.OrderBoardIdDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string OrderBoardId { get; set; }

	/// <summary>
	/// Is the message contains order info.
	/// </summary>
	public bool HasOrderInfo { get; set; }

	/// <summary>
	/// Is the message contains trade info.
	/// </summary>
	public bool HasTradeInfo => TradePrice is not null || TradeId is not null || TradeVolume is not null || !TradeStringId.IsEmpty();

	/// <summary>
	/// Order price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.OrderPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal OrderPrice { get; set; }

	/// <summary>
	/// Number of contracts in the order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeOrderKey,
		Description = LocalizedStrings.OrderVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? OrderVolume { get; set; }

	/// <summary>
	/// Number of contracts in the trade.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeTradeKey,
		Description = LocalizedStrings.TradeVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? TradeVolume { get; set; }

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
	/// Order contracts balance.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BalanceKey,
		Description = LocalizedStrings.OrderBalanceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Balance { get; set; }

	/// <summary>
	/// Order type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderTypeKey,
		Description = LocalizedStrings.OrderTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public OrderTypes? OrderType { get; set; }

	/// <summary>
	/// System order status.
	/// </summary>
	[DataMember]
	[Browsable(false)]
	//[Nullable]
	public long? OrderStatus { get; set; }

	/// <summary>
	/// Order state.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StateKey,
		Description = LocalizedStrings.OrderStateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public OrderStates? OrderState { get; set; }

	/// <summary>
	/// Placed order comment.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommentKey,
		Description = LocalizedStrings.OrderCommentKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Comment { get; set; }

	/// <summary>
	/// Message for order (created by the trading system when registered, changed or cancelled).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SystemCommentKey,
		Description = LocalizedStrings.SystemCommentDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string SystemComment { get; set; }

	/// <summary>
	/// Is a system trade.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SystemKey,
		Description = LocalizedStrings.IsSystemTradeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public bool? IsSystem { get; set; }

	/// <inheritdoc/>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpirationKey,
		Description = LocalizedStrings.OrderExpirationTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? ExpiryDate { get; set; }

	/// <summary>
	/// Limit order execution condition.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExecutionConditionKey,
		Description = LocalizedStrings.ExecutionConditionDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public TimeInForce? TimeInForce { get; set; }

	/// <summary>
	/// Trade ID.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeIdKey,
		Description = LocalizedStrings.TradeIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public long? TradeId { get; set; }

	/// <summary>
	/// Trade ID (as string, if electronic board does not use numeric order ID representation).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdStringKey,
		Description = LocalizedStrings.TradeIdStringKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string TradeStringId { get; set; }

	/// <summary>
	/// Trade price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceKey,
		Description = LocalizedStrings.TradePriceDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? TradePrice { get; set; }

	/// <summary>
	/// System trade status.
	/// </summary>
	[DataMember]
	[Browsable(false)]
	//[Nullable]
	public long? TradeStatus { get; set; }

	/// <summary>
	/// Deal initiator (seller or buyer).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.InitiatorKey,
		Description = LocalizedStrings.DirectionDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public Sides? OriginSide { get; set; }

	/// <summary>
	/// Number of open positions (open interest).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenInterestKey,
		Description = LocalizedStrings.OpenInterestDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? OpenInterest { get; set; }

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ErrorKey,
		Description = LocalizedStrings.OrderErrorKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[XmlIgnore]
	public Exception Error { get; set; }

	/// <summary>
	/// Order condition (e.g., stop- and algo- orders parameters).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ConditionKey,
		Description = LocalizedStrings.OrderConditionDescKey,
		GroupName = LocalizedStrings.ConditionalOrderKey)]
	[XmlIgnore]
	public OrderCondition Condition { get; set; }

	/// <summary>
	/// Is tick uptrend or downtrend in price. Uses only <see cref="DataType"/> for <see cref="DataType.Ticks"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UpTrendKey,
		Description = LocalizedStrings.UpTrendDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public bool? IsUpTick { get; set; }

	/// <summary>
	/// Commission (broker, exchange etc.). Uses when <see cref="DataType"/> set to <see cref="DataType.Transactions"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Commission { get; set; }

	/// <summary>
	/// Commission currency. Can be <see langword=""/>.
	/// </summary>
	public string CommissionCurrency { get; set; }

	/// <summary>
	/// Network latency. Uses when <see cref="DataType"/> set to <see cref="DataType.Transactions"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LatencyKey,
		Description = LocalizedStrings.NetworkLatencyKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public TimeSpan? Latency { get; set; }

	/// <summary>
	/// Slippage in trade price. Uses when <see cref="DataType"/> set to <see cref="DataType.Transactions"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlippageKey,
		Description = LocalizedStrings.SlippageTradeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Slippage { get; set; }

	/// <summary>
	/// User order id. Uses when <see cref="DataType"/> set to <see cref="DataType.Transactions"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UserIdKey,
		Description = LocalizedStrings.UserOrderIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string UserOrderId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public string StrategyId { get; set; }

	/// <summary>
	/// Trading security currency.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// The profit, realized by trade.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PnLKey,
		Description = LocalizedStrings.PnLKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? PnL { get; set; }

	/// <summary>
	/// The position, generated by order or trade.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionKey,
		Description = LocalizedStrings.PositionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Position { get; set; }

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
	/// Margin mode.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarginKey,
		Description = LocalizedStrings.MarginModeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public MarginModes? MarginMode { get; set; }

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
	/// Average execution price.
	/// </summary>
	[DataMember]
	public decimal? AveragePrice { get; set; }

	/// <summary>
	/// Yield.
	/// </summary>
	[DataMember]
	public decimal? Yield { get; set; }

	/// <summary>
	/// Minimum quantity of an order to be executed.
	/// </summary>
	[DataMember]
	public decimal? MinVolume { get; set; }

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
	/// Used to identify whether the order initiator is an aggressor or not in the trade.
	/// </summary>
	[DataMember]
	public bool? Initiator { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <summary>
	/// Margin leverage.
	/// </summary>
	[DataMember]
	public int? Leverage { get; set; }

	/// <summary>
	/// Order id (buy).
	/// </summary>
	[DataMember]
	public long? OrderBuyId { get; set; }

	/// <summary>
	/// Order id (sell).
	/// </summary>
	[DataMember]
	public long? OrderSellId { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ExecutionMessage"/>.
	/// </summary>
	public ExecutionMessage()
		: base(MessageTypes.Execution)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataTypeEx;

	/// <inheritdoc />
	public DataType DataTypeEx { get; set; }

	long? IComplexIdMessage.Id => TradeId;
	string IComplexIdMessage.StringId => TradeStringId;
	decimal ITickTradeMessage.Price => TradePrice ?? default;
	decimal ITickTradeMessage.Volume => TradeVolume ?? default;
	long? ISystemMessage.Status => OrderStatus ?? TradeStatus;

	IOrderMessage IOrderLogMessage.Order => this;
	ITickTradeMessage IOrderLogMessage.Trade => HasTradeInfo ? this : null;

	OrderTypes? IOrderMessage.Type => OrderType;
	decimal IOrderMessage.Price => OrderPrice;

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff},({DataType}),Sec={SecurityId},O/T={HasOrderInfo}/{HasTradeInfo},Ord={OrderId}/{OrderStringId}/{TransactionId}/{OriginalTransactionId},Fail={Error},Price={OrderPrice},OrdVol={OrderVolume},TrVol={TradeVolume},Bal={Balance},TId={TradeId},Pf={PortfolioName},TPrice={TradePrice},UId={UserOrderId},Type={OrderType},State={OrderState},Cond={Condition}";

		if (!StrategyId.IsEmpty())
			str += $",Strategy={StrategyId}";

		if (PositionEffect != null)
			str += $",PosEffect={PositionEffect.Value}";

		if (PostOnly != null)
			str += $",PostOnly={PostOnly.Value}";

		if (Initiator != null)
			str += $",Initiator={Initiator.Value}";

		if (SeqNum != default)
			str += $",SeqNum={SeqNum}";

		if (Leverage != null)
			str += $",Leverage={Leverage.Value}";

		if (OrderBuyId != null)
			str += $",buy (id)={OrderBuyId.Value}";

		if (OrderSellId != null)
			str += $",sell (id)={OrderSellId.Value}";

		return str;
	}

	/// <inheritdoc />
	public override void CopyTo(ExecutionMessage destination)
	{
		base.CopyTo(destination);

		destination.Balance = Balance;
		destination.Comment = Comment;
		destination.Condition = Condition?.Clone();
		destination.ClientCode = ClientCode;
		destination.BrokerCode = BrokerCode;
		destination.Currency = Currency;
		destination.ServerTime = ServerTime;
		destination.DepoName = DepoName;
		destination.Error = Error;
		destination.ExpiryDate = ExpiryDate;
		destination.IsSystem = IsSystem;
		destination.OpenInterest = OpenInterest;
		destination.OrderId = OrderId;
		destination.OrderStringId = OrderStringId;
		destination.OrderBoardId = OrderBoardId;
		destination.DataTypeEx = DataTypeEx;
		destination.IsCancellation = IsCancellation;
		destination.OrderState = OrderState;
		destination.OrderStatus = OrderStatus;
		destination.OrderType = OrderType;
		destination.OriginSide = OriginSide;
		destination.PortfolioName = PortfolioName;
		destination.OrderPrice = OrderPrice;
		destination.SecurityId = SecurityId;
		destination.Side = Side;
		destination.SystemComment = SystemComment;
		destination.TimeInForce = TimeInForce;
		destination.TradeId = TradeId;
		destination.TradeStringId = TradeStringId;
		destination.TradePrice = TradePrice;
		destination.TradeStatus = TradeStatus;
		destination.TransactionId = TransactionId;
		destination.OrderVolume = OrderVolume;
		destination.TradeVolume = TradeVolume;
		destination.VisibleVolume = VisibleVolume;
		destination.IsUpTick = IsUpTick;
		destination.Commission = Commission;
		destination.Latency = Latency;
		destination.Slippage = Slippage;
		destination.UserOrderId = UserOrderId;
		destination.StrategyId = StrategyId;

		destination.PnL = PnL;
		destination.Position = Position;

		destination.HasOrderInfo = HasOrderInfo;

		destination.IsMarketMaker = IsMarketMaker;
		destination.MarginMode = MarginMode;
		destination.IsManual = IsManual;

		destination.CommissionCurrency = CommissionCurrency;

		destination.AveragePrice = AveragePrice;
		destination.Yield = Yield;
		destination.MinVolume = MinVolume;
		destination.PositionEffect = PositionEffect;
		destination.PostOnly = PostOnly;
		destination.Initiator = Initiator;
		destination.SeqNum = SeqNum;
		destination.BuildFrom = BuildFrom;
		destination.Leverage = Leverage;
		destination.OrderBuyId = OrderBuyId;
		destination.OrderSellId = OrderSellId;
	}
}
