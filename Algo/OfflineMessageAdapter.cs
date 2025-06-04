namespace StockSharp.Algo;

/// <summary>
/// The messages adapter keeping message until connection will be done.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class OfflineMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private bool _connected;
	private readonly SyncObject _syncObject = new();
	private readonly List<Message> _suspendedIn = [];
	private readonly PairSet<long, ISubscriptionMessage> _pendingSubscriptions = [];
	private readonly PairSet<long, OrderRegisterMessage> _pendingRegistration = [];
	private int _maxMessageCount = 10000;

	/// <summary>
	/// Max message queue count. The default value is 10000.
	/// </summary>
	/// <remarks>
	/// Value set to -1 corresponds to the size without limitations.
	/// </remarks>
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

	/// <inheritdoc />
	protected override bool SendInBackFurther => false;

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		void ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			if (!_pendingRegistration.TryGetAndRemove(replaceMsg.OriginalTransactionId, out var originOrderMsg))
				_suspendedIn.Add(replaceMsg);
			else
			{
				_suspendedIn.Remove(originOrderMsg);

				RaiseNewOutMessage(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					HasOrderInfo = true,
					OriginalTransactionId = replaceMsg.OriginalTransactionId,
					ServerTime = CurrentTime,
					OrderState = OrderStates.Done,
					OrderType = originOrderMsg.OrderType,
				});

				var orderMsg = new OrderRegisterMessage();

				replaceMsg.CopyTo(orderMsg);

				_pendingRegistration.Add(replaceMsg.TransactionId, orderMsg);
				StoreMessage(orderMsg);
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				lock (_syncObject)
				{
					_connected = false;

					_suspendedIn.Clear();
					_pendingSubscriptions.Clear();
				}

				break;
			}
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case ExtendedMessageTypes.Reconnect:
				break;
			case MessageTypes.Time:
			{
				lock (_syncObject)
				{
					if (!_connected)
					{
						var timeMsg = (TimeMessage)message;

						if (timeMsg.OfflineMode == MessageOfflineModes.Ignore)
							break;

						return true;
					}
				}

				break;
			}
			case MessageTypes.OrderRegister:
			{
				lock (_syncObject)
				{
					if (!_connected)
					{
						var orderMsg = (OrderRegisterMessage)message.Clone();

						_pendingRegistration.Add(orderMsg.TransactionId, orderMsg);
						StoreMessage(orderMsg);

						return true;
					}
				}

				break;
			}
			case MessageTypes.OrderCancel:
			{
				lock (_syncObject)
				{
					if (!_connected)
					{
						var cancelMsg = (OrderCancelMessage)message.Clone();

						if (!_pendingRegistration.TryGetAndRemove(cancelMsg.OriginalTransactionId, out var originOrderMsg))
							_suspendedIn.Add(cancelMsg);
						else
						{
							_suspendedIn.Remove(originOrderMsg);

							RaiseNewOutMessage(new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								OriginalTransactionId = cancelMsg.TransactionId,
								ServerTime = CurrentTime,
								OrderState = OrderStates.Done,
								OrderType = originOrderMsg.OrderType,
							});
						}

						return true;
					}
				}

				break;
			}
			case MessageTypes.OrderReplace:
			{
				lock (_syncObject)
				{
					if (!_connected)
					{
						ProcessOrderReplaceMessage((OrderReplaceMessage)message.Clone());
						return true;
					}
				}

				break;
			}
			case MessageTypes.ProcessSuspended:
			{
				Message[] msgs;

				lock (_syncObject)
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

				foreach (var msg in msgs)
					base.OnSendInMessage(msg);

				return true;
			}
			default:
			{
				switch (message.OfflineMode)
				{
					case MessageOfflineModes.None:
						lock (_syncObject)
						{
							if (!_connected)
							{
								if (message is ISubscriptionMessage subscrMsg)
									ProcessSubscriptionMessage(subscrMsg);
								else
									StoreMessage(message.Clone());

								return true;
							}
						}

						break;
					case MessageOfflineModes.Ignore:
						break;
					case MessageOfflineModes.Cancel:
					{
						if (message is ISubscriptionMessage subscrMsg)
							RaiseNewOutMessage(subscrMsg.CreateResult());

						return true;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	private void ProcessSubscriptionMessage(ISubscriptionMessage subscrMsg)
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

					RaiseNewOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = subscrMsg.TransactionId });
					return;
				}
			}
							
			StoreMessage((Message)subscrMsg.Clone());
		}
	}

	private void StoreMessage(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (_maxMessageCount > 0 && _suspendedIn.Count == _maxMessageCount)
			throw new InvalidOperationException(LocalizedStrings.MaxMessageCountExceed);

		_suspendedIn.Add(message);

		LogInfo("Message {0} stored in offline.", message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		ConnectMessage connectMessage = null;

		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				connectMessage = (ConnectMessage)message;
				break;
			}

			case MessageTypes.Disconnect:
			{
				lock (_syncObject)
					_connected = false;

				break;
			}

			case MessageTypes.ConnectionLost:
			{
				lock (_syncObject)
					_connected = false;

				var lostMsg = (ConnectionLostMessage)message;

				if (lostMsg.IsResetState)
					return;

				break;
			}

			case MessageTypes.ConnectionRestored:
			{
				break;
			}
		}

		ProcessSuspendedMessage processMsg = null;

		if ((connectMessage != null && connectMessage.IsOk()) || message.Type == MessageTypes.ConnectionRestored)
		{
			lock (_syncObject)
			{
				_connected = true;

				processMsg = new ProcessSuspendedMessage(this);
			}
		}

		base.OnInnerAdapterNewOutMessage(message);

		if (processMsg != null)
			base.OnInnerAdapterNewOutMessage(processMsg);
	}

	/// <summary>
	/// Create a copy of <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new OfflineMessageAdapter(InnerAdapter.TypedClone());
	}
}