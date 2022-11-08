namespace StockSharp.Messages;

using System;

using Ecng.Common;

/// <summary>
/// Subscription.
/// </summary>
public class SubscriptionBase
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
	/// Subscription message.
	/// </summary>
	public ISubscriptionMessage SubscriptionMessage { get; }

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
	/// Initializes a new instance of the <see cref="SubscriptionBase"/>.
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
				nullSecIdMsg.SecurityId = security.SecurityId.IsDefault() ? null : security.SecurityId;
				break;
		}
	}

	/// <inheritdoc />
	public override string ToString() => SubscriptionMessage.ToString();
}