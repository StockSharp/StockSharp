namespace StockSharp.Algo;

/// <summary>
/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
/// </summary>
public class OrderBookTruncateMessageAdapter : MessageAdapterWrapper
{
	private readonly IOrderBookTruncateManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookTruncateMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public OrderBookTruncateMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new OrderBookTruncateManager(this, InnerAdapter.NearestSupportedDepth);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookTruncateMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="manager">Order book truncate manager.</param>
	public OrderBookTruncateMessageAdapter(IMessageAdapter innerAdapter, IOrderBookTruncateManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, _) = _manager.ProcessInMessage(message);

		return base.OnSendInMessageAsync(toInner, cancellationToken);
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
	/// Create a copy of <see cref="OrderBookTruncateMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new OrderBookTruncateMessageAdapter(InnerAdapter.TypedClone());
}
