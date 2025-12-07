namespace StockSharp.Messages;

/// <summary>
/// Synchronous message adapter.
/// </summary>
/// <remarks>
/// Initialize <see cref="SyncMessageAdapter"/>.
/// </remarks>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
[Obsolete("Sync mode is obsolete.")]
public abstract class SyncMessageAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <summary>
	/// Support partial downloading.
	/// </summary>
	[Browsable(false)]
	public virtual bool IsSupportPartialDownloading => true;

	/// <summary>
	/// Get maximum size step allowed for historical download.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="iterationInterval">Interval between iterations.</param>
	/// <returns>Step.</returns>
	public virtual TimeSpan GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
		=> this.GetDefaultHistoryStepSize(securityId, dataType, out iterationInterval);

	/// <summary>
	/// Get maximum possible items count per single subscription request.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Max items count.</returns>
	public virtual int? GetMaxCount(DataType dataType)
		=> dataType.GetDefaultMaxCount();

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