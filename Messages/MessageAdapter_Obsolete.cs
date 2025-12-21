namespace StockSharp.Messages;

abstract partial class MessageAdapter
{
	/// <inheritdoc />
	[Obsolete("Use SendOutMessageAsync method.")]
	public virtual void SendOutMessage(Message message)
	{
		InitMessageLocalTime(message);

		message.Adapter ??= this;

		if (message is DataTypeInfoMessage dtim && dtim.FileDataType is DataType dt && dt.IsMarketData)
			this.AddSupportedMarketDataType(dt);

#pragma warning disable CS0618 // Type or member is obsolete
		NewOutMessage?.Invoke(message);
#pragma warning restore CS0618
	}

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="expected">Is disconnect expected.</param>
	[Obsolete("Use SendOutDisconnectMessageAsync method.")]
	protected void SendOutDisconnectMessage(bool expected)
		=> AsyncHelper.Run(() => SendOutDisconnectMessageAsync(expected, default));

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> disconnect message.
	/// </summary>
	/// <param name="error">Error info. Can be <see langword="null"/>.</param>
	[Obsolete("Use SendOutDisconnectMessageAsync method.")]
	protected void SendOutDisconnectMessage(Exception error)
		=> AsyncHelper.Run(() => SendOutDisconnectMessageAsync(error, default));

	/// <summary>
	/// Send to <see cref="SendOutMessage"/> connection state message.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/></param>
	[Obsolete("Use SendOutConnectionStateAsync method.")]
	protected void SendOutConnectionState(ConnectionStates state)
		=> AsyncHelper.Run(() => SendOutConnectionStateAsync(state, default));

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="description">Error details.</param>
	[Obsolete("Use SendOutErrorAsync method.")]
	protected void SendOutError(string description)
		=> AsyncHelper.Run(() => SendOutErrorAsync(description, default));

	/// <summary>
	/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="error">Error details.</param>
	[Obsolete("Use SendOutErrorAsync method.")]
	protected void SendOutError(Exception error)
		=> AsyncHelper.Run(() => SendOutErrorAsync(error, default));

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="error">Subscribe or unsubscribe error info. To be set if the answer.</param>
	[Obsolete("Use SendSubscriptionReplyAsync method.")]
	protected void SendSubscriptionReply(long originalTransactionId, Exception error = null)
		=> AsyncHelper.Run(() => SendSubscriptionReplyAsync(originalTransactionId, default, error));

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionResponseMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	[Obsolete("Use SendSubscriptionNotSupportedAsync method.")]
	protected void SendSubscriptionNotSupported(long originalTransactionId)
		=> AsyncHelper.Run(() => SendSubscriptionNotSupportedAsync(originalTransactionId, default));

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	/// <param name="nextFrom"><see cref="SubscriptionFinishedMessage.NextFrom"/>.</param>
	[Obsolete("Use SendSubscriptionFinishedAsync method.")]
	protected void SendSubscriptionFinished(long originalTransactionId, DateTime? nextFrom = null)
		=> AsyncHelper.Run(() => SendSubscriptionFinishedAsync(originalTransactionId, default, nextFrom));

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
	[Obsolete("Use SendSubscriptionOnlineAsync method.")]
	protected void SendSubscriptionOnline(long originalTransactionId)
		=> AsyncHelper.Run(() => SendSubscriptionOnlineAsync(originalTransactionId, default));

	/// <summary>
	/// Initialize a new message <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
	/// </summary>
	/// <param name="message">Subscription.</param>
	[Obsolete("Use SendSubscriptionResultAsync method.")]
	protected void SendSubscriptionResult(ISubscriptionMessage message)
		=> AsyncHelper.Run(() => SendSubscriptionResultAsync(message, default));
}
