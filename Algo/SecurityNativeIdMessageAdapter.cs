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
	private readonly Lock _syncRoot = new();

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
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var nativeIds = Storage.Get(StorageName);

				using (_syncRoot.EnterScope())
				{
					foreach (var (securityId, nativeId) in nativeIds)
					{
						_securityIds[nativeId] = securityId;
					}
				}

				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				await ProcessAllSuspendedAsync(cancellationToken);

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

						using (_syncRoot.EnterScope())
							_securityIds[nativeSecurityId] = noNative;
					}
				}
				else
				{
					// TODO
				}

				// external code shouldn't receive native ids
				secMsg.SecurityId = noNative;

				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				await ProcessOutSuspendedAsync(securityId, cancellationToken);

				break;
			}

			case MessageTypes.PositionChange:
			{
				var positionMsg = (PositionChangeMessage)message;

				await ProcessMessageAsync(positionMsg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}

					return curr;
				}, cancellationToken);
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				var secId = execMsg.SecurityId;
				if (execMsg.TransactionId != 0 && secId != default && (!secId.SecurityCode.IsEmpty() || secId.Native != null))
				{
					using (_syncRoot.EnterScope())
						_transToSec[execMsg.TransactionId] = secId;
				}

				var noSecInfo = secId == default || (secId.SecurityCode.IsEmpty() && secId.Native == null);
				if (noSecInfo && execMsg.TransactionId == 0 && execMsg.OriginalTransactionId != 0)
				{
					using (_syncRoot.EnterScope())
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

				await ProcessMessageAsync(execMsg, null, cancellationToken);
				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;

				await ProcessMessageAsync(level1Msg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}

					return curr;
				}, cancellationToken);
				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quotesMsg = (QuoteChangeMessage)message;

				await ProcessMessageAsync(quotesMsg, (prev, curr) => curr, cancellationToken);
				break;
			}

			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (newsMsg.SecurityId != null)
					await ProcessMessageAsync(newsMsg.SecurityId.Value, newsMsg, null, cancellationToken);
				else
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}

			default:
			{
				if (message is ISecurityIdMessage secIdMsg)
					await ProcessMessageAsync(secIdMsg.SecurityId, (Message)secIdMsg, null, cancellationToken);
				else
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}
		}
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_syncRoot.EnterScope())
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
					return ProcessInSuspended(secId, cancellationToken);

				return default;

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
						return default;

					securityId.Native = native;
					message.ReplaceSecurityId(securityId);
				}

				break;
			}
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private object GetNativeId(ISecurityIdMessage message, SecurityId securityId)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		using (_syncRoot.EnterScope())
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
	public override IMessageAdapter Clone()
	{
		return new SecurityNativeIdMessageAdapter(InnerAdapter.TypedClone(), Storage);
	}

	private ValueTask ProcessMessageAsync<TMessage>(TMessage message, Func<TMessage, TMessage, TMessage> processSuspend, CancellationToken cancellationToken)
		where TMessage : Message, ISecurityIdMessage
	{
		return ProcessMessageAsync(message.SecurityId, message, processSuspend, cancellationToken);
	}

	private async ValueTask ProcessMessageAsync<TMessage>(SecurityId securityId, TMessage message, Func<TMessage, TMessage, TMessage> processSuspend, CancellationToken cancellationToken)
		where TMessage : Message
	{
		var native = securityId.Native;

		if (native != null)
		{
			SecurityId? fullSecurityId;

			using (_syncRoot.EnterScope())
				fullSecurityId = _securityIds.TryGetValue2(native);

			if (fullSecurityId == null)
			{
				using (_syncRoot.EnterScope())
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
		else if (!securityId.IsSpecial)
		{
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			var isSecCodeEmpty = securityCode.IsEmpty();

			//if (isSecCodeEmpty && throwIfSecIdEmpty)
			//	throw new InvalidOperationException();

			if (!isSecCodeEmpty && boardCode.IsEmpty())
			{
				SecurityId? foundId = null;

				using (_syncRoot.EnterScope())
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

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessAllSuspendedAsync(CancellationToken cancellationToken)
	{
		List<Message> inMsgs;
		List<Message> outMsgs;

		using (_syncRoot.EnterScope())
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
			await base.OnSendInMessageAsync(msg, cancellationToken);
		}

		foreach (var msg in outMsgs)
			await base.OnInnerAdapterNewOutMessageAsync(msg, cancellationToken);
	}

	private async ValueTask ProcessInSuspended(SecurityId securityId, CancellationToken cancellationToken)
	{
		var noNativeId = securityId.Native == null ? (SecurityId?)null : securityId;

		if (noNativeId != null)
		{
			var t = noNativeId.Value;
			t.Native = null;
			noNativeId = t;
		}

		List<ISecurityIdMessage> msgs = null;

		using (_syncRoot.EnterScope())
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
			await base.OnSendInMessageAsync((Message)msg, cancellationToken);
		}
	}

	private async ValueTask ProcessOutSuspendedAsync(SecurityId securityId, CancellationToken cancellationToken)
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

		using (_syncRoot.EnterScope())
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
			await base.OnInnerAdapterNewOutMessageAsync(msg.ReplaceSecurityId(securityId), cancellationToken);
	}

	private async void OnStorageNewIdentifierAdded(string storageName, SecurityId securityId, object nativeId)
	{
		if (!StorageName.EqualsIgnoreCase(storageName))
			return;

		bool needMessage;

		using (_syncRoot.EnterScope())
		{
			var added = _securityIds.TryAdd(nativeId, securityId);

			needMessage = added && _suspendedInMessages.ContainsKey(securityId);
		}

		if (needMessage)
		{
			var temp = securityId;
			temp.Native = nativeId;
			RaiseNewOutMessageAsync(new ProcessSuspendedMessage(this, temp), default);
		}
	}
}