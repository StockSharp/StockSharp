namespace StockSharp.Algo;

/// <summary>
/// Re-injects loopback (IsBack) messages emitted by the inner pipeline back into it
/// as input messages, instead of surfacing them to external consumers.
/// </summary>
/// <remarks>
/// Without this wrapper, consumers that subscribe to <c>NewOutMessageAsync</c>
/// on a <see cref="BasketMessageAdapter"/> directly must re-dispatch loopback
/// messages themselves — otherwise <see cref="HeartbeatMessageAdapter"/> heartbeats and other
/// loopback signals are silently dropped.
/// </remarks>
public sealed class LoopBackMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	/// <inheritdoc />
	protected override ValueTask InnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.IsBack())
			return InnerAdapter.SendInMessageAsync(message, cancellationToken);

		return base.InnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public override IMessageAdapter Clone()
		=> new LoopBackMessageAdapter(InnerAdapter.Clone());
}
