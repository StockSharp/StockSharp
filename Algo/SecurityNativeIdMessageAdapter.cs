namespace StockSharp.Algo;

/// <summary>
/// Security native id message adapter.
/// </summary>
public class SecurityNativeIdMessageAdapter : MessageAdapterWrapper
{
	private readonly PairSet<object, SecurityId> _securityIds = [];
	private readonly Dictionary<SecurityId, List<ISecurityIdMessage>> _suspendedInMessages = [];
	private readonly Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>> _suspendedOutMessages = [];
	private readonly Dictionary<long, SecurityId> _transToSec = [];
	private readonly SyncObject _syncRoot = new();

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public INativeIdStorage Storage { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityNativeIdMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="storage">Security native identifier storage.</param>
	public SecurityNativeIdMessageAdapter(IMessageAdapter innerAdapter, INativeIdStorage storage)
		: base(innerAdapter)
	{
		Storage = storage ?? throw new ArgumentNullException(nameof(storage));
		Storage.Added += OnStorageNewIdentifierAdded;
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		Storage.Added -= OnStorageNewIdentifierAdded;
		base.Dispose();
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var nativeIds = Storage.Get(StorageName);

				lock (_syncRoot)
				{
					foreach (var (securityId, nativeId) in nativeIds)
					{
						_securityIds[nativeId] = securityId;
					}
				}

				base.OnInnerAdapterNewOutMessage(message);

				ProcessAllSuspended();

				break;
			}

			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;
				var securityId = secMsg.SecurityId;

				var nativeSecurityId = securityId.Native;
				var securityCode = securityId.SecurityCode;
				var boardCode = securityId.BoardCode;

				if (securityCode.IsEmpty())
					throw new InvalidOperationException("Sec code is missed.");

				var noNative = securityId;
				noNative.Native = null;

				if (!boardCode.IsEmpty())
				{
					if (nativeSecurityId != null)
					{
						var storageName = StorageName;

						if (!Storage.TryAdd(storageName, noNative, nativeSecurityId, IsNativeIdentifiersPersistable))
						{
							var prevId = Storage.TryGetByNativeId(storageName, nativeSecurityId);

							if (prevId != null)
							{
								if (noNative != prevId.Value)
								{
									LogWarning(LocalizedStrings.DuplicateSystemId.Put(noNative, prevId.Value, nativeSecurityId));

									Storage.RemoveBySecurityId(storageName, prevId.Value);
									Storage.TryAdd(storageName, noNative, nativeSecurityId, IsNativeIdentifiersPersistable);
								}
							}
							else
							{
								LogWarning(LocalizedStrings.DuplicateSystemId.Put(Storage.TryGetBySecurityId(storageName, noNative), nativeSecurityId, noNative));

								Storage.RemoveByNativeId(storageName, nativeSecurityId);
								Storage.TryAdd(storageName, noNative, nativeSecurityId, IsNativeIdentifiersPersistable);
							}
						}

						lock (_syncRoot)
							_securityIds[nativeSecurityId] = noNative;
					}
				}
				else
				{
					// TODO
				}

				// external code shouldn't receive native ids
				secMsg.SecurityId = noNative;

				base.OnInnerAdapterNewOutMessage(message);

				ProcessOutSuspended(securityId);

				break;
			}

			case MessageTypes.PositionChange:
			{
				var positionMsg = (PositionChangeMessage)message;

				ProcessMessage(positionMsg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}

					return curr;
				});
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;
				
				var secId = execMsg.SecurityId;
				if (execMsg.TransactionId != 0 && secId != default && (!secId.SecurityCode.IsEmpty() || secId.Native != null))
				{
					lock (_syncRoot)
						_transToSec[execMsg.TransactionId] = secId;
				}

				var noSecInfo = secId == default || (secId.SecurityCode.IsEmpty() && secId.Native == null);
				if (noSecInfo && execMsg.TransactionId == 0 && execMsg.OriginalTransactionId != 0)
				{
					lock (_syncRoot)
					{
						if (
							_transToSec.TryGetValue(execMsg.OriginalTransactionId, out var suspendedSecId) &&

							// If original is suspended, suspend this related message to the same SecurityId key
							_suspendedOutMessages.TryGetValue(suspendedSecId, out var tuple)
						)
						{
							tuple.First ??= [];
							tuple.First.Add(execMsg.Clone());
							return;
						}
					}
				}

				ProcessMessage(execMsg, null);
				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;

				ProcessMessage(level1Msg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}

					return curr;
				});
				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quotesMsg = (QuoteChangeMessage)message;

				ProcessMessage(quotesMsg, (prev, curr) => curr);
				break;
			}

			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (newsMsg.SecurityId != null)
					ProcessMessage(newsMsg.SecurityId.Value, newsMsg, null);
				else
					base.OnInnerAdapterNewOutMessage(message);

				break;
			}

			default:
			{
				if (message is ISecurityIdMessage secIdMsg)
					ProcessMessage(secIdMsg.SecurityId, (Message)secIdMsg, null);
				else
					base.OnInnerAdapterNewOutMessage(message);

				break;
			}
		}
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				lock (_syncRoot)
				{
					_securityIds.Clear();
					_suspendedOutMessages.Clear();
					_suspendedInMessages.Clear();
					_transToSec.Clear();
				}

				break;
			}
			case MessageTypes.SecurityLookup:
				break;

			case MessageTypes.ProcessSuspended:
				var suspendMsg = (ProcessSuspendedMessage)message;

				if (suspendMsg.Arg is SecurityId secId)
					ProcessInSuspended(secId);

				return true;

			default:
			{
				if (message is ISecurityIdMessage secIdMsg)
				{
					if (secIdMsg.SecurityId == default)
						break;

					var securityId = secIdMsg.SecurityId;

					if (securityId.Native != null)
						break;

					var native = GetNativeId(secIdMsg, securityId);

					if (native == null)
						return true;

					securityId.Native = native;
					message.ReplaceSecurityId(securityId);
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	private object GetNativeId(ISecurityIdMessage message, SecurityId securityId)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		lock (_syncRoot)
		{
			var native = _securityIds.TryGetKey(securityId);

			if (native != null)
				return native;

			_suspendedInMessages.SafeAdd(securityId).Add((ISecurityIdMessage)((Message)message).Clone());
		}

		LogInfo("Suspended {0}.", message);
		return null;
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityNativeIdMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new SecurityNativeIdMessageAdapter(InnerAdapter.TypedClone(), Storage);
	}

	private void ProcessMessage<TMessage>(TMessage message, Func<TMessage, TMessage, TMessage> processSuspend)
		where TMessage : Message, ISecurityIdMessage
	{
		ProcessMessage(message.SecurityId, message, processSuspend);
	}

	private void ProcessMessage<TMessage>(SecurityId securityId, TMessage message, Func<TMessage, TMessage, TMessage> processSuspend)
		where TMessage : Message
	{
		var native = securityId.Native;

		if (native != null)
		{
			SecurityId? fullSecurityId;

			lock (_syncRoot)
				fullSecurityId = _securityIds.TryGetValue2(native);

			if (fullSecurityId == null)
			{
				lock (_syncRoot)
				{
					var tuple = _suspendedOutMessages.SafeAdd(securityId, key => RefTuple.Create((List<Message>)null, (Dictionary<MessageTypes, Message>)null));

					var clone = message.Clone();

					if (processSuspend == null)
					{
						tuple.First ??= [];

						tuple.First.Add(clone);
					}
					else
					{
						tuple.Second ??= [];

						var prev = tuple.Second.TryGetValue(clone.Type);

						tuple.Second[clone.Type] = prev == null
							? clone
							: processSuspend((TMessage)prev, (TMessage)clone);
					}
				}

				return;
			}

			message.ReplaceSecurityId(fullSecurityId.Value);
		}
		else
		{
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			var isSecCodeEmpty = securityCode.IsEmpty();

			//if (isSecCodeEmpty && throwIfSecIdEmpty)
			//	throw new InvalidOperationException();

			if (!isSecCodeEmpty && boardCode.IsEmpty())
			{
				SecurityId? foundId = null;

				lock (_syncRoot)
				{
					foreach (var id in _securityIds.Values)
					{
						if (!id.SecurityCode.EqualsIgnoreCase(securityCode))
							continue;

						//if (securityId.SecurityType != null && securityId.SecurityType != id.SecurityType)
						//	continue;

						foundId = id;
					}

					if (foundId == null)
					{
						var tuple = _suspendedOutMessages.SafeAdd(securityId, key => RefTuple.Create(new List<Message>(), (Dictionary<MessageTypes, Message>)null));
						tuple.First.Add(message.Clone());
						return;
					}
				}

				message.ReplaceSecurityId(foundId.Value);
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private void ProcessAllSuspended()
	{
		List<Message> inMsgs;
		List<Message> outMsgs;

		lock (_syncRoot)
		{
			inMsgs = [];
			outMsgs = [];

			foreach (var (secId, messages) in _suspendedInMessages.ToArray())
			{
				if (!_securityIds.TryGetKey(secId, out var nativeId))
					continue;

				inMsgs.AddRange(messages.Select(m =>
				{
					var tempId = m.SecurityId;
					tempId.Native = nativeId;
					m.SecurityId = tempId;
					return (Message)m;
				}));

				_suspendedInMessages.Remove(secId);
			}

			foreach (var (nativeId, secId) in _securityIds)
			{
				void processOut(RefPair<List<Message>, Dictionary<MessageTypes, Message>> messages)
				{
					if (messages.First is not null)
					{
						outMsgs.AddRange(messages.First.Select(m => m.ReplaceSecurityId(secId)));
					}

					if (messages.Second is not null)
					{
						outMsgs.AddRange(messages.Second.Select(p => p.Value.ReplaceSecurityId(secId)));
					}
				}

				if (_suspendedOutMessages.TryGetAndRemove(secId, out var messages1))
					processOut(messages1);

				var tempId = secId;
				tempId.Native = nativeId;

				if (_suspendedOutMessages.TryGetAndRemove(tempId, out var messages2))
					processOut(messages2);
			}

			foreach (var m in outMsgs)
			{
				if (m is ExecutionMessage em && em.TransactionId != 0)
					_transToSec.Remove(em.TransactionId);
			}
		}

		foreach (var msg in inMsgs)
		{
			msg.LoopBack(this);
			base.OnSendInMessage(msg);
		}

		foreach (var msg in outMsgs)
			base.OnInnerAdapterNewOutMessage(msg);
	}

	private void ProcessInSuspended(SecurityId securityId)
	{
		var noNativeId = securityId.Native == null ? (SecurityId?)null : securityId;

		if (noNativeId != null)
		{
			var t = noNativeId.Value;
			t.Native = null;
			noNativeId = t;
		}

		List<ISecurityIdMessage> msgs = null;

		lock (_syncRoot)
		{
			msgs = _suspendedInMessages.TryGetAndRemove(securityId);

			if (noNativeId != null)
			{
				var msgs2 = _suspendedInMessages.TryGetAndRemove(noNativeId.Value);

				if (msgs2 != null)
				{
					if (msgs == null)
						msgs = msgs2;
					else
						msgs.AddRange(msgs2);
				}
			}
		}

		if (msgs == null)
			return;

		foreach (var msg in msgs)
		{
			msg.SecurityId = securityId;
			base.OnSendInMessage((Message)msg);
		}
	}

	private void ProcessOutSuspended(SecurityId securityId)
	{
		static List<Message> GetMessages(RefPair<List<Message>, Dictionary<MessageTypes, Message>> tuple)
		{
			var retVal = tuple.First;

			if (retVal == null)
				retVal = [.. tuple.Second.Values];
			else if (tuple.Second != null)
				retVal.AddRange(tuple.Second.Values);

			return retVal;
		}

		var noNativeId = securityId.Native == null ? (SecurityId?)null : securityId;

		if (noNativeId != null)
		{
			var t = noNativeId.Value;
			t.Native = null;
			noNativeId = t;
		}

		List<Message> msgs = null;

		lock (_syncRoot)
		{
			var tuple = _suspendedOutMessages.TryGetAndRemove(securityId);

			if (tuple != null)
				msgs = GetMessages(tuple);

			if (noNativeId != null)
			{
				tuple = _suspendedOutMessages.TryGetAndRemove(noNativeId.Value);

				if (tuple != null)
				{
					if (msgs == null)
						msgs = GetMessages(tuple);
					else
						msgs.AddRange(GetMessages(tuple));
				}
			}

			// find association by code and code + type
			var pair = _suspendedOutMessages
				.FirstOrDefault(p =>
					p.Key.SecurityCode.EqualsIgnoreCase(securityId.SecurityCode) &&
					p.Key.BoardCode.IsEmpty() /*&&
					(securityId.SecurityType == null || p.Key.SecurityType == securityId.SecurityType)*/);

			var value = pair.Value;

			if (value != null)
			{
				_suspendedOutMessages.Remove(pair.Key);

				if (msgs == null)
					msgs = GetMessages(value);
				else
					msgs.AddRange(GetMessages(value));
			}

			if (msgs != null)
			{
				foreach (var m in msgs)
				{
					if (m is ExecutionMessage em && em.TransactionId != 0)
						_transToSec.Remove(em.TransactionId);
				}
			}
		}

		if (msgs == null)
			return;

		// external code shouldn't receive native ids
		securityId.Native = null;

		foreach (var msg in msgs)
			base.OnInnerAdapterNewOutMessage(msg.ReplaceSecurityId(securityId));
	}

	private void OnStorageNewIdentifierAdded(string storageName, SecurityId securityId, object nativeId)
	{
		if (!StorageName.EqualsIgnoreCase(storageName))
			return;

		bool needMessage;

		lock (_syncRoot)
		{
			var added = _securityIds.TryAdd(nativeId, securityId);

			needMessage = added && _suspendedInMessages.ContainsKey(securityId);
		}

		if (needMessage)
		{
			var temp = securityId;
			temp.Native = nativeId;
			RaiseNewOutMessage(new ProcessSuspendedMessage(this, temp));
		}
	}
}