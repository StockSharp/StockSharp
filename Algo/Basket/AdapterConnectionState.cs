namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IAdapterConnectionState"/>.
/// </summary>
public class AdapterConnectionState : IAdapterConnectionState
{
	private readonly Lock _sync = new();
	private readonly Dictionary<IMessageAdapter, (ConnectionStates state, Exception error)> _adapterStates = [];
	private ConnectionStates _currentState = ConnectionStates.Disconnected;

	/// <inheritdoc />
	public void SetAdapterState(IMessageAdapter adapter, ConnectionStates state, Exception error)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		using (_sync.EnterScope())
			_adapterStates[adapter] = (state, error);
	}

	/// <inheritdoc />
	public bool TryGetAdapterState(IMessageAdapter adapter, out ConnectionStates state, out Exception error)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		using (_sync.EnterScope())
		{
			if (_adapterStates.TryGetValue(adapter, out var tuple))
			{
				state = tuple.state;
				error = tuple.error;
				return true;
			}
		}

		state = default;
		error = default;
		return false;
	}

	/// <inheritdoc />
	public bool RemoveAdapter(IMessageAdapter adapter)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		using (_sync.EnterScope())
			return _adapterStates.Remove(adapter);
	}

	/// <inheritdoc />
	public ConnectionStates CurrentState
	{
		get
		{
			using (_sync.EnterScope())
				return _currentState;
		}
		set
		{
			using (_sync.EnterScope())
				_currentState = value;
		}
	}

	/// <inheritdoc />
	public IEnumerable<(IMessageAdapter adapter, ConnectionStates state, Exception error)> GetAllStates()
	{
		using (_sync.EnterScope())
			return _adapterStates.Select(p => (p.Key, p.Value.state, p.Value.error)).ToArray();
	}

	/// <inheritdoc />
	public int ConnectedCount
	{
		get
		{
			using (_sync.EnterScope())
				return _adapterStates.Count(p => p.Value.state == ConnectionStates.Connected);
		}
	}

	/// <inheritdoc />
	public int TotalCount
	{
		get
		{
			using (_sync.EnterScope())
				return _adapterStates.Count;
		}
	}

	/// <inheritdoc />
	public bool HasPendingAdapters
	{
		get
		{
			using (_sync.EnterScope())
				return _adapterStates.Any(p => p.Value.state == ConnectionStates.Connecting);
		}
	}

	/// <inheritdoc />
	public bool AllFailed
	{
		get
		{
			using (_sync.EnterScope())
				return _adapterStates.Count > 0 && _adapterStates.All(p => p.Value.state == ConnectionStates.Failed);
		}
	}

	/// <inheritdoc />
	public bool AllDisconnectedOrFailed
	{
		get
		{
			using (_sync.EnterScope())
				return _adapterStates.All(p => p.Value.state is ConnectionStates.Disconnected or ConnectionStates.Failed);
		}
	}

	/// <inheritdoc />
	public Exception[] GetErrors()
	{
		using (_sync.EnterScope())
			return _adapterStates.Select(p => p.Value.error).Where(e => e != null).ToArray();
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_adapterStates.Clear();
			_currentState = ConnectionStates.Disconnected;
		}
	}
}
