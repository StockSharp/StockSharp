namespace StockSharp.Algo.Latency;

/// <summary>
/// Orders registration delay calculation manager.
/// </summary>
public class LatencyManager : ILatencyManager
{
	private readonly SyncObject _syncObject = new();
	private readonly Dictionary<long, DateTimeOffset> _register = [];
	private readonly Dictionary<long, DateTimeOffset> _cancel = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="LatencyManager"/>.
	/// </summary>
	public LatencyManager()
	{
	}

	/// <inheritdoc />
	public TimeSpan LatencyRegistration { get; private set; }

	/// <inheritdoc />
	public TimeSpan LatencyCancellation { get; private set; }

	/// <inheritdoc />
	public TimeSpan? ProcessMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Reset();
				break;
			}

			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;

				lock (_syncObject)
				{
					AddRegister(regMsg.TransactionId, regMsg.LocalTime);
				}

				break;
			}
			case MessageTypes.OrderReplace:
			{
				var replaceMsg = (OrderReplaceMessage)message;

				lock (_syncObject)
				{
					AddCancel(replaceMsg.OriginalTransactionId, replaceMsg.LocalTime);
					AddRegister(replaceMsg.TransactionId, replaceMsg.LocalTime);
				}

				break;
			}
			case MessageTypes.OrderCancel:
			{
				var cancelMsg = (OrderCancelMessage)message;

				lock (_syncObject)
					AddCancel(cancelMsg.TransactionId, cancelMsg.LocalTime);

				break;
			}
			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (!execMsg.HasOrderInfo())
					break;

				if (execMsg.OrderState == OrderStates.Pending)
					break;

				var transId = execMsg.OriginalTransactionId;

				lock (_syncObject)
				{
					if (!_register.TryGetValue(transId, out var time))
					{
						if (_cancel.TryGetValue(transId, out time))
						{
							_cancel.Remove(transId);

							if (execMsg.OrderState == OrderStates.Failed)
								break;

							var latency = execMsg.LocalTime - time;
							LatencyCancellation += latency;
							return latency;
						}
					}
					else
					{
						_register.Remove(transId);

						if (execMsg.OrderState == OrderStates.Failed)
							break;

						var latency = execMsg.LocalTime - time;
						LatencyRegistration += latency;
						return latency;
					}
				}

				break;
			}
		}

		return null;
	}

	private void AddRegister(long transactionId, DateTimeOffset localTime)
	{
		if (transactionId == 0)
			throw new ArgumentNullException(nameof(transactionId));

		if (localTime == default)
			throw new ArgumentNullException(nameof(localTime));

		if (_register.ContainsKey(transactionId))
			throw new ArgumentException(LocalizedStrings.TransactionRegAlreadyAdded.Put(transactionId), nameof(transactionId));

		_register.Add(transactionId, localTime);
	}

	private void AddCancel(long transactionId, DateTimeOffset localTime)
	{
		if (transactionId <= 0)
			throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.InvalidValue);

		if (localTime == default)
			throw new ArgumentNullException(nameof(localTime));

		if (_cancel.ContainsKey(transactionId))
			throw new ArgumentException(LocalizedStrings.TransactionCancelAlreadyAdded.Put(transactionId), nameof(transactionId));

		_cancel.Add(transactionId, localTime);
	}

	/// <inheritdoc />
	public void Reset()
	{
		lock (_syncObject)
		{
			LatencyRegistration = LatencyCancellation = default;

			_register.Clear();
			_cancel.Clear();
		}
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Load(SettingsStorage storage)
	{
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Save(SettingsStorage storage)
	{
	}
}