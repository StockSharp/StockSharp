namespace StockSharp.Algo.Latency;

/// <summary>
/// The message adapter, automatically calculating network delays.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LatencyMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="latencyManager">Orders registration delay calculation manager.</param>
public class LatencyMessageAdapter(IMessageAdapter innerAdapter, ILatencyManager latencyManager) : MessageAdapterWrapper(innerAdapter)
{
	private readonly ILatencyManager _latencyManager = latencyManager ?? throw new ArgumentNullException(nameof(latencyManager));

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		message.TryInitLocalTime(this);

		_latencyManager.ProcessMessage(message);

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type == MessageTypes.Execution)
		{
			var execMsg = (ExecutionMessage)message;

			if (execMsg.HasOrderInfo())
			{
				var latency = _latencyManager.ProcessMessage(execMsg);

				if (latency != null)
					execMsg.Latency = latency;
			}
		}

		return base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="LatencyMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new LatencyMessageAdapter(InnerAdapter.TypedClone(), _latencyManager.Clone());
	}
}