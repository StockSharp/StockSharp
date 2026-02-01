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
/// <param name="state">State storage.</param>
public sealed class OfflineManager(ILogReceiver logReceiver, Func<Message> createProcessSuspendedMessage, IOfflineManagerState state) : IOfflineManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<Message> _createProcessSuspendedMessage = createProcessSuspendedMessage ?? throw new ArgumentNullException(nameof(createProcessSuspendedMessage));
	private readonly IOfflineManagerState _state = state ?? throw new ArgumentNullException(nameof(state));
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

	private DateTime CurrentTime => _logReceiver.CurrentTime;

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut, bool shouldForward) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.Clear();
				return ([], [], true);
			}
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case ExtendedMessageTypes.Reconnect:
				return ([], [], true);

			case MessageTypes.Time:
			{
				if (!_state.IsConnected)
				{
					var timeMsg = (TimeMessage)message;

					if (timeMsg.OfflineMode == MessageOfflineModes.Ignore)
						return ([], [], true);

					return ([], [], false);
				}

				return ([], [], true);
			}
			case MessageTypes.OrderRegister:
			{
				if (!_state.IsConnected)
				{
					var orderMsg = (OrderRegisterMessage)message.Clone();

					_state.AddPendingRegistration(orderMsg.TransactionId, orderMsg);
					StoreMessage(orderMsg);

					return ([], [], false);
				}

				return ([], [], true);
			}
			case MessageTypes.OrderCancel:
			{
				Message outMsg = null;
				var handled = false;

				if (!_state.IsConnected)
				{
					handled = true;
					var cancelMsg = (OrderCancelMessage)message.Clone();

					if (!_state.TryGetAndRemovePendingRegistration(cancelMsg.OriginalTransactionId, out var originOrderMsg))
						_state.AddSuspended(cancelMsg);
					else
					{
						_state.RemoveSuspended(originOrderMsg);

						outMsg = new ExecutionMessage
						{
							DataTypeEx = DataType.Transactions,
							HasOrderInfo = true,
							OriginalTransactionId = cancelMsg.TransactionId,
							ServerTime = CurrentTime,
							OrderState = OrderStates.Done,
							OrderType = originOrderMsg.OrderType,
						};
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

				if (!_state.IsConnected)
					outMsg = ProcessOrderReplaceMessage((OrderReplaceMessage)message.Clone());

				if (outMsg != null)
					return ([], [outMsg], false);

				return ([], [], true);
			}
			case MessageTypes.ProcessSuspended:
			{
				var msgs = _state.GetAndClearSuspended();

				foreach (var msg in msgs)
				{
					if (msg is ISubscriptionMessage subscrMsg)
						_state.RemovePendingSubscriptionByValue(subscrMsg);
					else if (msg is OrderRegisterMessage orderMsg)
						_state.RemovePendingRegistrationByValue(orderMsg);
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

						if (!_state.IsConnected)
						{
							if (message is ISubscriptionMessage subscrMsg)
								outMsg = ProcessSubscriptionMessage(subscrMsg);
							else
								StoreMessage(message.Clone());

							handled = true;
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
							return ([], [subscrMsg.CreateResponse()], false);

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
					_state.SetConnected(true);
					return (false, [_createProcessSuspendedMessage()]);
				}

				return (false, []);
			}

			case MessageTypes.Disconnect:
			{
				_state.SetConnected(false);
				return (false, []);
			}

			case MessageTypes.ConnectionLost:
			{
				_state.SetConnected(false);

				var lostMsg = (ConnectionLostMessage)message;

				if (lostMsg.IsResetState)
					return (true, []);

				return (false, []);
			}

			case MessageTypes.ConnectionRestored:
			{
				_state.SetConnected(true);
				return (false, [_createProcessSuspendedMessage()]);
			}
		}

		return (false, []);
	}

	private Message ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
	{
		if (!_state.TryGetAndRemovePendingRegistration(replaceMsg.OriginalTransactionId, out var originOrderMsg))
		{
			_state.AddSuspended(replaceMsg);
			return null;
		}

		_state.RemoveSuspended(originOrderMsg);

		var outMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = replaceMsg.TransactionId,
			ServerTime = CurrentTime,
			OrderState = OrderStates.Done,
			OrderType = originOrderMsg.OrderType,
		};

		var orderMsg = new OrderRegisterMessage();

		replaceMsg.CopyTo(orderMsg);

		_state.AddPendingRegistration(replaceMsg.TransactionId, orderMsg);
		StoreMessage(orderMsg);

		return outMsg;
	}

	private Message ProcessSubscriptionMessage(ISubscriptionMessage subscrMsg)
	{
		if (subscrMsg.IsSubscribe)
		{
			var clone = subscrMsg.TypedClone();

			if (subscrMsg.TransactionId != 0)
				_state.AddPendingSubscription(subscrMsg.TransactionId, clone);

			StoreMessage((Message)clone);
		}
		else
		{
			if (subscrMsg.OriginalTransactionId != 0)
			{
				if (_state.TryGetAndRemovePendingSubscription(subscrMsg.OriginalTransactionId, out var originMsg))
				{
					_state.RemoveSuspended((Message)originMsg);

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

		if (_maxMessageCount > 0 && _state.SuspendedCount == _maxMessageCount)
			throw new InvalidOperationException(LocalizedStrings.MaxMessageCountExceed);

		_state.AddSuspended(message);

		_logReceiver.AddInfoLog("Message {0} stored in offline.", message);
	}
}
