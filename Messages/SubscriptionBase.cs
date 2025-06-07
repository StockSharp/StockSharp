namespace StockSharp.Messages;

/// <summary>
/// Subscription.
/// </summary>
/// <typeparam name="TSubscription">The type of the subscription message. It must implement <see cref="ISubscriptionMessage"/> interface.</typeparam>
public abstract class SubscriptionBase<TSubscription> : Cloneable<TSubscription>, ISubscriptionMessage
	where TSubscription : SubscriptionBase<TSubscription>
{
	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId? SecurityId => (SubscriptionMessage as ISecurityIdMessage)?.SecurityId;

	/// <inheritdoc />
	public DataType DataType => SubscriptionMessage.DataType;

	/// <inheritdoc />
	public DateTimeOffset? From
	{
		get => SubscriptionMessage.From;
		set => SubscriptionMessage.From = value;
	}

	/// <inheritdoc />
	public DateTimeOffset? To
	{
		get => SubscriptionMessage.To;
		set => SubscriptionMessage.To = value;
	}

	/// <inheritdoc />
	public long? Skip
	{
		get => SubscriptionMessage.Skip;
		set => SubscriptionMessage.Skip = value;
	}

	/// <inheritdoc />
	public long? Count
	{
		get => SubscriptionMessage.Count;
		set => SubscriptionMessage.Count = value;
	}

	/// <inheritdoc />
	public FillGapsDays? FillGaps
	{
		get => SubscriptionMessage.FillGaps;
		set => SubscriptionMessage.FillGaps = value;
	}

	/// <summary>
	/// Request identifier.
	/// </summary>
	public long TransactionId
	{
		get => SubscriptionMessage.TransactionId;
		set => SubscriptionMessage.TransactionId = value;
	}

	/// <inheritdoc />
	public bool FilterEnabled => SubscriptionMessage.FilterEnabled;

	/// <inheritdoc />
	public bool SpecificItemRequest => SubscriptionMessage.SpecificItemRequest;

	/// <inheritdoc />
	public bool IsSubscribe
	{
		get => SubscriptionMessage.IsSubscribe;
		set => SubscriptionMessage.IsSubscribe = value;
	}

	/// <inheritdoc />
	public long OriginalTransactionId
	{
		get => SubscriptionMessage.OriginalTransactionId;
		set => SubscriptionMessage.OriginalTransactionId = value;
	}

	/// <summary>
	/// Subscription message.
	/// </summary>
	public ISubscriptionMessage SubscriptionMessage { get; }

	/// <summary>
	/// <see cref="MarketDataMessage"/>
	/// </summary>
	public MarketDataMessage MarketData => (MarketDataMessage)SubscriptionMessage;

	/// <summary>
	/// <see cref="SecurityLookupMessage"/>
	/// </summary>
	public SecurityLookupMessage SecurityLookup => (SecurityLookupMessage)SubscriptionMessage;

	/// <summary>
	/// <see cref="PortfolioLookupMessage"/>
	/// </summary>
	public PortfolioLookupMessage PortfolioLookup => (PortfolioLookupMessage)SubscriptionMessage;

	/// <summary>
	/// <see cref="OrderStatusMessage"/>
	/// </summary>
	public OrderStatusMessage OrderStatus => (OrderStatusMessage)SubscriptionMessage;

	/// <summary>
	/// State.
	/// </summary>
	public SubscriptionStates State { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionBase{TSubscription}"/>.
	/// </summary>
	/// <param name="subscriptionMessage">Subscription message.</param>
	/// <param name="security">Security.</param>
	protected SubscriptionBase(ISubscriptionMessage subscriptionMessage, SecurityMessage security)
	{
		SubscriptionMessage = subscriptionMessage ?? throw new ArgumentNullException(nameof(subscriptionMessage));
		SubscriptionMessage.IsSubscribe = true;

		if (security == null)
			return;

		switch (subscriptionMessage)
		{
			case MarketDataMessage mdMsg:
				security.CopyTo(mdMsg, false);
				break;
			case ISecurityIdMessage secIdMsg:
				secIdMsg.SecurityId = security.SecurityId;
				break;
			case INullableSecurityIdMessage nullSecIdMsg:
				nullSecIdMsg.SecurityId = security.SecurityId == default ? null : security.SecurityId;
				break;
		}
	}

	/// <inheritdoc />
	public override string ToString() => SubscriptionMessage.ToString();

	MessageTypes IMessage.Type => SubscriptionMessage.Type;

	IMessageAdapter IMessage.Adapter
	{
		get => SubscriptionMessage.Adapter;
		set => SubscriptionMessage.Adapter = value;
	}

	MessageBackModes IMessage.BackMode
	{
		get => SubscriptionMessage.BackMode;
		set => SubscriptionMessage.BackMode = value;
	}

	DateTimeOffset ILocalTimeMessage.LocalTime => SubscriptionMessage.LocalTime;
}