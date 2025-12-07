namespace StockSharp.Messages;

/// <summary>
/// Synchronous message adapter.
/// </summary>
/// <remarks>
/// Initialize <see cref="SyncMessageAdapter"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
public abstract class SyncMessageAdapter(IdGenerator transactionIdGenerator) : AsyncMessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		OnSendInMessage(message);
		return default;
	}

	/// <summary>
	/// Sends message to adapter.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns><see langword="true"/> if message was processed successfully; otherwise, <see langword="false"/>.</returns>
	protected abstract bool OnSendInMessage(Message message);
}