namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IAdapterConnectionManager"/>.
/// Manages connection states for multiple adapters and aggregates basket state.
/// </summary>
/// <param name="state">State storage.</param>
public class AdapterConnectionManager(IAdapterConnectionState state) : IAdapterConnectionManager
{
	private readonly IAdapterConnectionState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public ConnectionStates CurrentState => _state.CurrentState;

	/// <inheritdoc />
	public bool ConnectDisconnectEventOnFirstAdapter { get; set; } = true;

	/// <inheritdoc />
	public IAdapterConnectionState State => _state;

	/// <inheritdoc />
	public bool HasPendingAdapters => _state.HasPendingAdapters;

	/// <inheritdoc />
	public void InitializeAdapter(IMessageAdapter adapter)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		_state.SetAdapterState(adapter, ConnectionStates.Connecting, null);
	}

	/// <inheritdoc />
	public void BeginConnect()
	{
		_state.CurrentState = ConnectionStates.Connecting;
	}

	/// <inheritdoc />
	public void BeginDisconnect()
	{
		_state.CurrentState = ConnectionStates.Disconnecting;
	}

	/// <inheritdoc />
	public Message[] ProcessConnect(IMessageAdapter adapter, Exception error)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		if (error == null)
		{
			_state.SetAdapterState(adapter, ConnectionStates.Connected, null);

			if (_state.CurrentState == ConnectionStates.Connecting)
			{
				if (ConnectDisconnectEventOnFirstAdapter)
				{
					// raise Connected event only one time for the first adapter
					_state.CurrentState = ConnectionStates.Connected;
					return [new ConnectMessage()];
				}
				else
				{
					// wait until all adapters leave Connecting state
					if (!_state.HasPendingAdapters)
					{
						_state.CurrentState = ConnectionStates.Connected;
						return [new ConnectMessage()];
					}
				}
			}
		}
		else
		{
			_state.SetAdapterState(adapter, ConnectionStates.Failed, error);

			if (_state.CurrentState is ConnectionStates.Connecting or ConnectionStates.Connected)
			{
				if (_state.AllFailed)
				{
					var errors = _state.GetErrors();
					_state.CurrentState = ConnectionStates.Failed;
					return [new ConnectMessage { Error = errors.SingleOrAggr() }];
				}

				if (!ConnectDisconnectEventOnFirstAdapter && !_state.HasPendingAdapters && _state.ConnectedCount > 0)
				{
					_state.CurrentState = ConnectionStates.Connected;
					return [new ConnectMessage()];
				}
			}
		}

		return [];
	}

	/// <inheritdoc />
	public Message[] ProcessDisconnect(IMessageAdapter adapter, Exception error)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		if (error == null)
			_state.SetAdapterState(adapter, ConnectionStates.Disconnected, null);
		else
			_state.SetAdapterState(adapter, ConnectionStates.Failed, error);

		if (_state.AllDisconnectedOrFailed)
		{
			_state.CurrentState = ConnectionStates.Disconnected;
			return [new DisconnectMessage()];
		}

		return [];
	}

	/// <inheritdoc />
	public void Reset()
	{
		_state.Clear();
	}
}
