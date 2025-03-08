namespace StockSharp.Messages;

/// <summary>
/// A message containing info about the order.
/// </summary>
/// <remarks>
/// Initialize <see cref="OrderMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class OrderMessage(MessageTypes type) : SecurityMessage(type),
	ITransactionIdMessage, IPortfolioNameMessage, IStrategyIdMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TransactionKey,
		Description = LocalizedStrings.TransactionIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public long TransactionId { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfolioKey,
		Description = LocalizedStrings.OrderPortfolioNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string PortfolioName { get; set; }

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
	/// User's order ID.
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
	/// Broker firm code.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BrokerKey,
		Description = LocalizedStrings.BrokerCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BrokerCode { get; set; }

	/// <summary>
	/// Client code assigned by the broker.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClientCodeKey,
		Description = LocalizedStrings.ClientCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string ClientCode { get; set; }

	/// <summary>
	/// Order condition (e.g., stop- and algo- orders parameters).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ConditionKey,
		Description = LocalizedStrings.OrderConditionDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[XmlIgnore]
	public OrderCondition Condition { get; set; }

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
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(OrderMessage destination)
	{
		base.CopyTo(destination);

		destination.TransactionId = TransactionId;
		destination.PortfolioName = PortfolioName;
		destination.OrderType = OrderType;
		destination.UserOrderId = UserOrderId;
		destination.StrategyId = StrategyId;
		destination.BrokerCode = BrokerCode;
		destination.ClientCode = ClientCode;
		destination.Condition = Condition?.Clone();
		destination.Comment = Comment;
		destination.MarginMode = MarginMode;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",TransId={TransactionId},OrdType={OrderType},Pf={PortfolioName}(ClCode={ClientCode}),Cond={Condition},MR={MarginMode}";

		if (!Comment.IsEmpty())
			str += $",Comment={Comment}";

		if (!UserOrderId.IsEmpty())
			str += $",UID={UserOrderId}";

		if (!StrategyId.IsEmpty())
			str += $",Strategy={StrategyId}";

		if (!BrokerCode.IsEmpty())
			str += $",BrID={BrokerCode}";

		return str;
	}
}