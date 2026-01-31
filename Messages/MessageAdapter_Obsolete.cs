namespace StockSharp.Messages;

partial class MessageAdapter
{
	/// <inheritdoc />
	[Obsolete("Use async version instead.")]
	public virtual void SendOutMessage(Message message)
		=> SendOutMessageAsync(message, default);

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="expected">Is disconnect expected.</param>
	[Obsolete("Use async version instead.")]
	protected void SendOutDisconnectMessage(bool expected)
		=> SendOutDisconnectMessageAsync(expected, default);

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="error">Error info. Can be <see langword="null"/>.</param>
	[Obsolete("Use async version instead.")]
	protected void SendOutDisconnectMessage(Exception error)
		=> SendOutDisconnectMessageAsync(error, default);

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> connection state message.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/></param>
	[Obsolete("Use async version instead.")]
	protected void SendOutConnectionState(ConnectionStates state)
		=> SendOutConnectionStateAsync(state, default);

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="description">Error details.</param>
	[Obsolete("Use async version instead.")]
	protected void SendOutError(string description)
		=> SendOutErrorAsync(description, default);

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="error">Error details.</param>
	[Obsolete("Use async version instead.")]
	protected void SendOutError(Exception error)
		=> SendOutErrorAsync(error, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="error">Subscribe or unsubscribe error info. To be set if the answer.</param>
	[Obsolete("Use async version instead.")]
	protected void SendSubscriptionReply(long originalTransactionId, Exception error = null)
		=> SendSubscriptionReplyAsync(originalTransactionId, default, error);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	[Obsolete("Use async version instead.")]
	protected void SendSubscriptionNotSupported(long originalTransactionId)
		=> SendSubscriptionNotSupportedAsync(originalTransactionId, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="nextFrom"><see cref="SubscriptionFinishedMessage.NextFrom"/>.</param>
	[Obsolete("Use async version instead.")]
	protected void SendSubscriptionFinished(long originalTransactionId, DateTime? nextFrom = null)
		=> SendSubscriptionFinishedAsync(originalTransactionId, default, nextFrom);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	[Obsolete("Use async version instead.")]
	protected void SendSubscriptionOnline(long originalTransactionId)
		=> SendSubscriptionOnlineAsync(originalTransactionId, default);

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="message">Subscription.</param>
	[Obsolete("Use async version instead.")]
	protected void SendSubscriptionResult(ISubscriptionMessage message)
		=> SendSubscriptionResultAsync(message, default);
}
