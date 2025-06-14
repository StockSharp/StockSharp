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
	protected override bool OnSendInMessage(Message message)
	{
		message.TryInitLocalTime(this);

		_latencyManager.ProcessMessage(message);

		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
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

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="LatencyMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new LatencyMessageAdapter(InnerAdapter.TypedClone(), _latencyManager.Clone());
	}
}