namespace StockSharp.Algo.Storages.Remote;

/// <summary>
/// <see cref="RemoteStorageClient"/> cache.
/// </summary>
public class RemoteStorageCache
{
	private readonly SyncObject _sync = new();
	private readonly Dictionary<object, (Message[] messages, DateTime till)> _state = [];
	private readonly TimeSpan _timeout;

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteStorageCache"/>.
	/// </summary>
	/// <param name="timeout">Cache expiration timeout.</param>
	public RemoteStorageCache(TimeSpan timeout)
	{
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout));

		_timeout = timeout;
	}

	/// <summary>
	/// Store values into cache.
	/// </summary>
	/// <param name="key">Cache key.</param>
	/// <param name="messages">Messages.</param>
	public void Set(object key, Message[] messages)
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		if (messages is null)
			throw new ArgumentNullException(nameof(messages));

		lock (_sync)
			_state[key] = (messages, DateTime.UtcNow + _timeout);
	}

	/// <summary>
	/// Try load values from cache.
	/// </summary>
	/// <param name="key">Cache key.</param>
	/// <param name="messages">Messages.</param>
	/// <returns>Operation result.</returns>
	public bool TryGet(object key, out Message[] messages)
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		messages = null;

		lock (_sync)
		{
			if (_state.TryGetValue(key, out var t))
			{
				if (t.till > DateTime.UtcNow)
				{
					messages = t.messages;
					return true;
				}
				else
					_state.Remove(key);
			}
		}

		return false;
	}
}