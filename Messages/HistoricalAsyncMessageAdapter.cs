using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

namespace StockSharp.Messages;

/// <summary>
/// Historical <see cref="AsyncMessageAdapter"/>.
/// </summary>
public abstract class HistoricalAsyncMessageAdapter : AsyncMessageAdapter
{
	/// <summary>
	/// Initialize <see cref="HistoricalAsyncMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected HistoricalAsyncMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
	}

	/// <inheritdoc />
	protected override ValueTask OnConnectAsync(ConnectMessage msg, CancellationToken token)
	{
		SendOutMessage(new ConnectMessage());
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnDisconnectAsync(DisconnectMessage msg, CancellationToken token)
	{
		SendOutMessage(new DisconnectMessage());
		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnResetAsync(ResetMessage msg, CancellationToken token)
	{
		SendOutMessage(new ResetMessage());
		return default;
	}
}