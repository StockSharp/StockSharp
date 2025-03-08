namespace StockSharp.Algo.Latency;

/// <summary>
/// The message adapter, automatically calculating network delays.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LatencyMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
public class LatencyMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private ILatencyManager _latencyManager = new LatencyManager();

	/// <summary>
	/// Orders registration delay calculation manager.
	/// </summary>
	public ILatencyManager LatencyManager
	{
		get => _latencyManager;
		set => _latencyManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		message.TryInitLocalTime(this);

		LatencyManager.ProcessMessage(message);

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
				var latency = LatencyManager.ProcessMessage(execMsg);

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
		return new LatencyMessageAdapter(InnerAdapter.TypedClone());
	}
}