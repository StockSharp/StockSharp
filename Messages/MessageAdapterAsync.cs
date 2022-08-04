namespace StockSharp.Messages;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Async version of <see cref="MessageAdapter"/>.
/// </summary>
public abstract class MessageAdapterAsync : MessageAdapter
{
	/// <summary>
	/// Initialize <see cref="MessageAdapterAsync"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected MessageAdapterAsync(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
		=> ThreadingHelper.Run(() => OnSendInMessageAsync(message, default));

	/// <summary>
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
	protected abstract ValueTask<bool> OnSendInMessageAsync(Message message, CancellationToken cancellationToken);
}