namespace StockSharp.Messages;

/// <summary>
/// Subscription.
/// </summary>
public class SubscriptionBase<TSubscription> : Cloneable<TSubscription>
	where TSubscription : SubscriptionBase<TSubscription>, ISubscriptionMessage
{
	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId? SecurityId => (SubscriptionMessage as ISecurityIdMessage)?.SecurityId;

	/// <summary>
	/// Data type info.
	/// </summary>
	public DataType DataType => SubscriptionMessage.DataType;

	/// <summary>
	/// Start date, from which data needs to be retrieved.
	/// </summary>
	public DateTimeOffset? From
	{
		get => SubscriptionMessage.From;
		set => SubscriptionMessage.From = value;
	}

	/// <summary>
	/// End date, until which data needs to be retrieved.
	/// </summary>
	public DateTimeOffset? To
	{
		get => SubscriptionMessage.To;
		set => SubscriptionMessage.To = value;
	}

	/// <summary>
	/// Skip count.
	/// </summary>
	public long? Skip
	{
		get => SubscriptionMessage.Skip;
		set => SubscriptionMessage.Skip = value;
	}

	/// <summary>
	/// Max count.
	/// </summary>
	public long? Count
	{
		get => SubscriptionMessage.Count;
		set => SubscriptionMessage.Count = value;
	}

	/// <summary>
	/// <see cref="FillGapsDays"/>.
	/// </summary>
	public FillGapsDays? FillGaps
	{
		get => SubscriptionMessage.FillGaps;
		set => SubscriptionMessage.FillGaps = value;
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
	/// Request identifier.
	/// </summary>
	public long TransactionId
	{
		get => SubscriptionMessage.TransactionId;
		set => SubscriptionMessage.TransactionId = value;
	}

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

	/// <inheritdoc />
	public override TSubscription Clone() => throw new NotSupportedException();
}