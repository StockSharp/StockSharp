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
	private readonly AsyncLock _sync = new();
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
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		async ValueTask ProcessOrderReplaceMessage(OrderReplaceMessage replaceMsg)
		{
			if (!_pendingRegistration.TryGetAndRemove(replaceMsg.OriginalTransactionId, out var originOrderMsg))
				_suspendedIn.Add(replaceMsg);
			else
			{
				_suspendedIn.Remove(originOrderMsg);

				await RaiseNewOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					HasOrderInfo = true,
					OriginalTransactionId = replaceMsg.OriginalTransactionId,
					ServerTime = CurrentTimeUtc,
					OrderState = OrderStates.Done,
					OrderType = originOrderMsg.OrderType,
				}, cancellationToken);

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
				using (await _sync.LockAsync(cancellationToken))
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
				using (await _sync.LockAsync(cancellationToken))
				{
					if (!_connected)
					{
						var timeMsg = (TimeMessage)message;

						if (timeMsg.OfflineMode == MessageOfflineModes.Ignore)
							break;

						return;
					}
				}

				break;
			}
			case MessageTypes.OrderRegister:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					if (!_connected)
					{
						var orderMsg = (OrderRegisterMessage)message.Clone();

						_pendingRegistration.Add(orderMsg.TransactionId, orderMsg);
						StoreMessage(orderMsg);

						return;
					}
				}

				break;
			}
			case MessageTypes.OrderCancel:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					if (!_connected)
					{
						var cancelMsg = (OrderCancelMessage)message.Clone();

						if (!_pendingRegistration.TryGetAndRemove(cancelMsg.OriginalTransactionId, out var originOrderMsg))
							_suspendedIn.Add(cancelMsg);
						else
						{
							_suspendedIn.Remove(originOrderMsg);

							await RaiseNewOutMessageAsync(new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								OriginalTransactionId = cancelMsg.TransactionId,
								ServerTime = CurrentTimeUtc,
								OrderState = OrderStates.Done,
								OrderType = originOrderMsg.OrderType,
							}, cancellationToken);
						}

						return;
					}
				}

				break;
			}
			case MessageTypes.OrderReplace:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					if (!_connected)
					{
						await ProcessOrderReplaceMessage((OrderReplaceMessage)message.Clone());
						return;
					}
				}

				break;
			}
			case MessageTypes.ProcessSuspended:
			{
				Message[] msgs;

				using (await _sync.LockAsync(cancellationToken))
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

				await msgs.Select(msg => base.OnSendInMessageAsync(msg, cancellationToken)).WhenAll();
				return;
			}
			default:
			{
				switch (message.OfflineMode)
				{
					case MessageOfflineModes.None:
						using (await _sync.LockAsync(cancellationToken))
						{
							if (!_connected)
							{
								if (message is ISubscriptionMessage subscrMsg)
									await ProcessSubscriptionMessage(subscrMsg, cancellationToken);
								else
									StoreMessage(message.Clone());

								return;
							}
						}

						break;
					case MessageOfflineModes.Ignore:
						break;
					case MessageOfflineModes.Cancel:
					{
						if (message is ISubscriptionMessage subscrMsg)
							await RaiseNewOutMessageAsync(subscrMsg.CreateResult(), cancellationToken);

						return;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
				}

				break;
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessSubscriptionMessage(ISubscriptionMessage subscrMsg, CancellationToken cancellationToken)
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

					await RaiseNewOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = subscrMsg.TransactionId }, cancellationToken);
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
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
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
				using (await _sync.LockAsync(cancellationToken))
					_connected = false;

				break;
			}

			case MessageTypes.ConnectionLost:
			{
				using (await _sync.LockAsync(cancellationToken))
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
			using (await _sync.LockAsync(cancellationToken))
			{
				_connected = true;

				processMsg = new ProcessSuspendedMessage(this);
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (processMsg != null)
			await base.OnInnerAdapterNewOutMessageAsync(processMsg, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new OfflineMessageAdapter(InnerAdapter.TypedClone());
	}
}