namespace StockSharp.Algo.Server
{
	using System;

	using StockSharp.Algo;
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
		IRemoteAuthorization Authorization { get; }

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
		/// Send message.
		/// </summary>
		/// <param name="session">Session.</param>
		/// <param name="requestId">Request identifier.</param>
		/// <param name="message">Message.</param>
		void SendInMessage(IMessageListenerSession session, string requestId, Message message);
	}
}