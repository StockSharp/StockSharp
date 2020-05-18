namespace StockSharp.Algo.Server
{
	using System;

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
		/// Send message.
		/// </summary>
		/// <param name="session">Session.</param>
		/// <param name="subscriptionId">Subscription id.</param>
		/// <param name="message">Message.</param>
		/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
		bool SendInMessage(IMessageListenerSession session, long? subscriptionId, Message message);
	}
}