namespace StockSharp.Algo.Server
{
	using System;
	using System.Collections.Generic;

	using Ecng.Security;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing a message listening component.
	/// </summary>
	public interface IMessageListener : IMessageChannel, ILogSource
	{
		/// <summary>
		/// The customer authentication.
		/// </summary>
		IAuthorization Authorization { get; }

		/// <summary>
		/// Transaction and request identifiers storage.
		/// </summary>
		ITransactionIdStorage TransactionIdStorage { get; }

		/// <summary>
		/// Keep subscriptions on disconnect.
		/// </summary>
		bool KeepSubscriptionsOnDisconnect { get; set; }

		/// <summary>
		/// New message event.
		/// </summary>
		new event Action<IMessageListenerSession, Message> NewOutMessage;

		/// <summary>
		/// Session disconnected event.
		/// </summary>
		event Action<IMessageListenerSession> SessionDisconnected;

		/// <summary>
		/// Subscriptions.
		/// </summary>
		IEnumerable<Subscription> Subscriptions { get; }

		/// <summary>
		/// Client subscription changed event.
		/// </summary>
		event Action<IMessageListenerSession, Subscription> SubscriptionChanged;

		/// <summary>
		/// Remove subscription.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <returns><see langword="true"/> if subscription was found, otherwise <see langword="false"/>.</returns>
		bool RemoveSubscription(Subscription subscription);

		/// <summary>
		/// Disconnect session.
		/// </summary>
		/// <param name="session">Session.</param>
		void Disconnect(IMessageListenerSession session);

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="session">Session.</param>
		/// <param name="subscriptionId">Subscription id.</param>
		/// <param name="message">Message.</param>
		/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
		bool SendInMessage(IMessageListenerSession session, long? subscriptionId, Message message);
	}
}