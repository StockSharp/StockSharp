namespace StockSharp.Algo.Slippage;

/// <summary>
/// The message adapter, automatically calculating slippage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SlippageMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="slippageManager">Slippage manager.</param>
public class SlippageMessageAdapter(IMessageAdapter innerAdapter, ISlippageManager slippageManager) : MessageAdapterWrapper(innerAdapter)
{
	private readonly ISlippageManager _slippageManager = slippageManager ?? throw new ArgumentNullException(nameof(slippageManager));

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		_slippageManager.ProcessMessage(message);
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message.Type != MessageTypes.Reset)
		{
			var slippage = _slippageManager.ProcessMessage(message);

			if (slippage != null)
			{
				var execMsg = (ExecutionMessage)message;

				execMsg.Slippage ??= slippage;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="SlippageMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new SlippageMessageAdapter(InnerAdapter.TypedClone(), _slippageManager.Clone());
	}
}