namespace StockSharp.Algo;

/// <summary>
/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
/// </summary>
public class OrderBookIncrementMessageAdapter : MessageAdapterWrapper
{
	private readonly IOrderBookIncrementManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookIncrementMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public OrderBookIncrementMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new OrderBookIncrementManager(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookIncrementMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="manager">Order book increment manager.</param>
	public OrderBookIncrementMessageAdapter(IMessageAdapter innerAdapter, IOrderBookIncrementManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = _manager.ProcessInMessage(message);

		if (toInner.Length > 0)
		{
			foreach (var msg in toInner)
				await base.OnSendInMessageAsync(msg, cancellationToken);
		}

		if (toOut.Length > 0)
		{
			foreach (var sendOutMsg in toOut)
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut) = _manager.ProcessOutMessage(message);

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="OrderBookIncrementMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new OrderBookIncrementMessageAdapter(InnerAdapter.TypedClone());
	}
}
