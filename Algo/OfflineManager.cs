namespace StockSharp.Algo;

/// <summary>
/// Offline message processing logic.
/// </summary>
public interface IOfflineManager
{
	/// <summary>
	/// Max message queue count.
	/// </summary>
	int MaxMessageCount { get; set; }

	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result: toInner messages to forward, toOut messages to emit, shouldForward indicates if original should be sent.</returns>
	(Message[] toInner, Message[] toOut, bool shouldForward) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result: forward indicates if message should be forwarded, extraOut additional messages to emit, suppressOriginal if original should not be forwarded.</returns>
	(bool suppressOriginal, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Offline message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OfflineManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="createProcessSuspendedMessage">Create a message that resumes processing after connection is restored.</param>
public sealed class OfflineManager(ILogReceiver logReceiver, Func<Message> createProcessSuspendedMessage) : IOfflineManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<Message> _createProcessSuspendedMessage = createProcessSuspendedMessage ?? throw new ArgumentNullException(nameof(createProcessSuspendedMessage));
	private readonly Lock _sync = new();

	private bool _connected;
	private readonly List<Message> _suspendedIn = [];
	private readonly PairSet<long, ISubscriptionMessage> _pendingSubscriptions = [];
	private readonly PairSet<long, OrderRegisterMessage> _pendingRegistration = [];
	private int _maxMessageCount = 10000;

	/// <inheritdoc />
	public int MaxMessageCount
	{
		get => _maxMessageCount;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_maxMessageCount = value;
		}
	}

	private DateTime CurrentTimeUtc => _logReceiver.CurrentTimeUtc;

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut, bool shouldForward) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_sync.EnterScope())
				{
					_connected = false;
					_suspendedIn.Clear();
					_pendingSubscriptions.Clear();
					_pendingRegistration.Clear();
				}

				return ([], [], true);
			}
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case ExtendedMessageTypes.Reconnect:
				return ([], [], true);

			case MessageTypes.Time:
			{
				using (_sync.EnterScope())
				{
					if (!_connected)
					{
						var timeMsg = (TimeMessage)message;

						if (timeMsg.OfflineMode == MessageOfflineModes.Ignore)
							return ([], [], true);

						return ([], [], false);
					}
				}

				return ([], [], true);
			}
			case MessageTypes.OrderRegister:
			{
				using (_sync.EnterScope())
				{
					if (!_connected)
					{
						var orderMsg = (OrderRegisterMessage)message.Clone();

						_pendingRegistration.Add(orderMsg.TransactionId, orderMsg);
						StoreMessage(orderMsg);

						return ([], [], false);
					}
				}

				return ([], [], true);
			}
			case MessageTypes.OrderCancel:
			{
				Message outMsg = null;
				var handled = false;

				using (_sync.EnterScope())
				{
					if (!_connected)
					{
						handled = true;
						var cancelMsg = (OrderCancelMessage)message.Clone();

						if (!_pendingRegistration.TryGetAndRemove(cancelMsg.OriginalTransactionId, out var originOrderMsg))
							_suspendedIn.Add(cancelMsg);
						else
						{
							_suspendedIn.Remove(originOrderMsg);

							outMsg = new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								OriginalTransactionId = cancelMsg.TransactionId,
								ServerTime = CurrentTimeUtc,
								OrderState = OrderStates.Done,
								OrderType = originOrderMsg.OrderType,
							};
						}
					}
				}

				if (handled)
				{
					if (outMsg != null)
						return ([], [outMsg], false);

					return ([], [], false);
				}

				return ([], [], true);
			}
			case MessageTypes.OrderReplace:
			{
				Message outMsg = null;

				using (_sync.EnterScope())
				{
					if (!_connected)
						outMsg = ProcessOrderReplaceMessage((OrderReplaceMessage)message.Clone());
				}

				if (outMsg != null)
					return ([], [outMsg], false);

				return ([], [], true);
			}
			case MessageTypes.ProcessSuspended:
			{
				Message[] msgs;

				using (_sync.EnterScope())
				{
					msgs = _suspendedIn.CopyAndClear();

					foreach (var msg in msgs)
					{
						if (msg is ISubscriptionMessage subscrMsg)
							_pendingSubscriptions.RemoveByValue(subscrMsg);
						else if (msg is OrderRegisterMessage orderMsg)
							_pendingRegistration.RemoveByValue(orderMsg);
					}
				}

				return (msgs, [], false);
			}
			default:
			{
				switch (message.OfflineMode)
				{
					case MessageOfflineModes.None:
					{
						Message outMsg = null;
						var handled = false;

						using (_sync.EnterScope())
						{
							if (!_connected)
							{
								if (message is ISubscriptionMessage subscrMsg)
									outMsg = ProcessSubscriptionMessage(subscrMsg);
								else
									StoreMessage(message.Clone());

								handled = true;
							}
						}

						if (handled)
						{
							if (outMsg != null)
								return ([], [outMsg], false);

							return ([], [], false);
						}

						return ([], [], true);
					}
					case MessageOfflineModes.Ignore:
						return ([], [], true);
					case MessageOfflineModes.Cancel:
					{
						if (message is ISubscriptionMessage subscrMsg)
							return ([], [subscrMsg.CreateResult()], false);

						return ([], [], false);
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
				}
			}
		}
	}

	/// <inheritdoc />
	public (bool suppressOriginal, Message[] extraOut) ProcessOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var connectMessage = (ConnectMessage)message;

				if (connectMessage.IsOk())
				{
					using (_sync.EnterScope())
						_connected = true;

					return (false, [_createProcessSuspendedMessage()]);
				}

				return (false, []);
			}

			case MessageTypes.Disconnect:
			{
				using (_sync.EnterScope())
					_connected = false;

				return (false, []);
			}

			case MessageTypes.ConnectionLost:
			{
				using (_sync.EnterScope())
					_connected = false;

				var lostMsg = (ConnectionLostMessage)message;

				if (lostMsg.IsResetState)
					return (true, []);

				return (false, []);
			}

			case MessageTypes.ConnectionRestored:
			{
				using (_sync.EnterScope())
					_connected = true;

				return (false, [_createProcessSuspendedMessage()]);
			}
		}

		return (false, []);
	}

	private Message ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
	{
		if (!_pendingRegistration.TryGetAndRemove(replaceMsg.OriginalTransactionId, out var originOrderMsg))
		{
			_suspendedIn.Add(replaceMsg);
			return null;
		}

		_suspendedIn.Remove(originOrderMsg);

		var outMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = replaceMsg.TransactionId,
			ServerTime = CurrentTimeUtc,
			OrderState = OrderStates.Done,
			OrderType = originOrderMsg.OrderType,
		};

		var orderMsg = new OrderRegisterMessage();

		replaceMsg.CopyTo(orderMsg);

		_pendingRegistration.Add(replaceMsg.TransactionId, orderMsg);
		StoreMessage(orderMsg);

		return outMsg;
	}

	private Message ProcessSubscriptionMessage(ISubscriptionMessage subscrMsg)
	{
		if (subscrMsg.IsSubscribe)
		{
			var clone = subscrMsg.TypedClone();

			if (subscrMsg.TransactionId != 0)
				_pendingSubscriptions.Add(subscrMsg.TransactionId, clone);

			StoreMessage((Message)clone);
		}
		else
		{
			if (subscrMsg.OriginalTransactionId != 0)
			{
				if (_pendingSubscriptions.TryGetAndRemove(subscrMsg.OriginalTransactionId, out var originMsg))
				{
					_suspendedIn.Remove((Message)originMsg);

					return new SubscriptionResponseMessage { OriginalTransactionId = subscrMsg.TransactionId };
				}
			}

			StoreMessage((Message)subscrMsg.Clone());
		}

		return null;
	}

	private void StoreMessage(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (_maxMessageCount > 0 && _suspendedIn.Count == _maxMessageCount)
			throw new InvalidOperationException(LocalizedStrings.MaxMessageCountExceed);

		_suspendedIn.Add(message);

		_logReceiver.AddInfoLog("Message {0} stored in offline.", message);
	}
}
