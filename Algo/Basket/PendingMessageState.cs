namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IPendingMessageState"/>.
/// </summary>
public class PendingMessageState : IPendingMessageState
{
	private readonly Lock _sync = new();
	private readonly List<Message> _messages = [];

	/// <inheritdoc />
	public void Add(Message message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		using (_sync.EnterScope())
			_messages.Add(message);
	}

	/// <inheritdoc />
	public Message[] GetAndClear()
	{
		using (_sync.EnterScope())
			return _messages.CopyAndClear();
	}

	/// <inheritdoc />
	public MarketDataMessage TryRemoveMarketData(long transactionId)
	{
		using (_sync.EnterScope())
		{
			var msg = _messages.FirstOrDefault(m => m is MarketDataMessage mdMsg && mdMsg.TransactionId == transactionId);

			if (msg != null)
			{
				_messages.Remove(msg);
				return (MarketDataMessage)msg;
			}

			return null;
		}
	}

	/// <inheritdoc />
	public int Count
	{
		get
		{
			using (_sync.EnterScope())
				return _messages.Count;
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
			_messages.Clear();
	}
}
