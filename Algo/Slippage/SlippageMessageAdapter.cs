namespace StockSharp.Algo.Slippage;

/// <summary>
/// The message adapter, automatically calculating slippage.
/// </summary>
public class SlippageMessageAdapter : MessageAdapterWrapper
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SlippageMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	public SlippageMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

	private ISlippageManager _slippageManager = new SlippageManager();

	/// <summary>
	/// Slippage manager.
	/// </summary>
	public ISlippageManager SlippageManager
	{
		get => _slippageManager;
		set => _slippageManager = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		SlippageManager.ProcessMessage(message);
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message.Type != MessageTypes.Reset)
		{
			var slippage = SlippageManager.ProcessMessage(message);

			if (slippage != null)
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.Slippage == null)
					execMsg.Slippage = slippage;
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
		return new SlippageMessageAdapter(InnerAdapter.TypedClone());
	}
}