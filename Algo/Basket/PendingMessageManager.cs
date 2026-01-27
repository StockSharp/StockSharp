namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IPendingMessageManager"/>.
/// Buffers messages until basket is connected.
/// </summary>
/// <param name="state">State storage.</param>
public class PendingMessageManager(IPendingMessageState state) : IPendingMessageManager
{
	private readonly IPendingMessageState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public IPendingMessageState State => _state;

	/// <inheritdoc />
	public bool TryEnqueue(Message message, ConnectionStates currentState, bool hasPendingAdapters, int totalAdapters)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		// Buffer if there are pending adapters, or no adapters at all,
		// or all adapters are disconnected/failed
		if (hasPendingAdapters || totalAdapters == 0)
		{
			_state.Add(message.Clone());
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public Message[] DequeueAll()
	{
		return _state.GetAndClear();
	}

	/// <inheritdoc />
	public MarketDataMessage TryRemoveMarketData(long transactionId)
	{
		return _state.TryRemoveMarketData(transactionId);
	}

	/// <inheritdoc />
	public bool HasPending => _state.Count > 0;
}
