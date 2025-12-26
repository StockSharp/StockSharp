namespace StockSharp.Algo;

/// <summary>
/// Security native id processing logic.
/// </summary>
public interface ISecurityNativeIdManager : IDisposable
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result: toInner - messages to send to inner adapter, toOut - messages to raise as output.</returns>
	ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result: forward - message to forward to outer, extraOut - additional messages to forward, loopbackIn - messages to send back to inner adapter.</returns>
	ValueTask<(Message forward, Message[] extraOut, Message[] loopbackIn)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Initialize from storage provider.
	/// </summary>
	/// <param name="storageName">Storage name.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask InitializeAsync(string storageName, CancellationToken cancellationToken);

	/// <summary>
	/// Event raised when a new native id is added from storage and suspended messages need processing.
	/// </summary>
	event Func<SecurityId, CancellationToken, ValueTask> ProcessSuspendedRequested;
}

/// <summary>
/// Security native id processing implementation.
/// </summary>
public sealed class SecurityNativeIdManager : ISecurityNativeIdManager
{
	private readonly PairSet<object, SecurityId> _securityIds = [];
	private readonly Dictionary<SecurityId, List<ISecurityIdMessage>> _suspendedInMessages = [];
	private readonly Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>> _suspendedOutMessages = [];
	private readonly Dictionary<long, SecurityId> _transToSec = [];
	private readonly Lock _syncRoot = new();

	private readonly ILogReceiver _logReceiver;
	private readonly INativeIdStorageProvider _storageProvider;
	private readonly bool _isNativeIdentifiersPersistable;

	private INativeIdStorage _storage;

	/// <inheritdoc />
	public event Func<SecurityId, CancellationToken, ValueTask> ProcessSuspendedRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityNativeIdManager"/>.
	/// </summary>
	/// <param name="logReceiver">Log receiver.</param>
	/// <param name="storageProvider">Security native identifier storage provider.</param>
	/// <param name="isNativeIdentifiersPersistable">Whether native identifiers are persistable.</param>
	public SecurityNativeIdManager(
		ILogReceiver logReceiver,
		INativeIdStorageProvider storageProvider,
		bool isNativeIdentifiersPersistable)
	{
		_logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
		_storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
		_isNativeIdentifiersPersistable = isNativeIdentifiersPersistable;
	}

	/// <inheritdoc />
	public async ValueTask InitializeAsync(string storageName, CancellationToken cancellationToken)
	{
		_storage = _storageProvider.GetStorage(storageName);
		_storage.Added += OnStorageNewIdentifierAddedAsync;

		var nativeIds = await _storage.GetAsync(cancellationToken);

		using (_syncRoot.EnterScope())
		{
			foreach (var (securityId, nativeId) in nativeIds)
			{
				_securityIds[nativeId] = securityId;
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_storage != null)
			_storage.Added -= OnStorageNewIdentifierAddedAsync;
	}

	/// <inheritdoc />
	public ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken)
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

				return new(([message], []));
			}

			case MessageTypes.SecurityLookup:
				return new(([message], []));

			case MessageTypes.ProcessSuspended:
			{
				var suspendMsg = (ProcessSuspendedMessage)message;

				if (suspendMsg.Arg is SecurityId secId)
					return ProcessInSuspended(secId);

				return new(([], []));
			}

			default:
			{
				if (message is ISecurityIdMessage secIdMsg)
				{
					if (secIdMsg.SecurityId == default)
						return new(([message], []));

					var securityId = secIdMsg.SecurityId;

					if (securityId.Native != null)
						return new(([message], []));

					var native = GetNativeId(secIdMsg, securityId);

					if (native == null)
						return new(([], []));

					securityId.Native = native;
					message.ReplaceSecurityId(securityId);
				}

				return new(([message], []));
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask<(Message forward, Message[] extraOut, Message[] loopbackIn)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var (extraOut, loopbackIn) = await ProcessAllSuspendedAsync(cancellationToken);
				return (message, extraOut, loopbackIn);
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
						if (!await _storage.TryAddAsync(noNative, nativeSecurityId, _isNativeIdentifiersPersistable, cancellationToken))
						{
							var prevId = await _storage.TryGetByNativeIdAsync(nativeSecurityId, cancellationToken);

							if (prevId != null)
							{
								if (noNative != prevId.Value)
								{
									_logReceiver.AddWarningLog(LocalizedStrings.DuplicateSystemId.Put(noNative, prevId.Value, nativeSecurityId));

									await _storage.RemoveBySecurityIdAsync(prevId.Value, cancellationToken: cancellationToken);
									await _storage.TryAddAsync(noNative, nativeSecurityId, _isNativeIdentifiersPersistable, cancellationToken);
								}
							}
							else
							{
								_logReceiver.AddWarningLog(LocalizedStrings.DuplicateSystemId.Put(await _storage.TryGetBySecurityIdAsync(noNative, cancellationToken), nativeSecurityId, noNative));

								await _storage.RemoveByNativeIdAsync(nativeSecurityId, cancellationToken: cancellationToken);
								await _storage.TryAddAsync(noNative, nativeSecurityId, _isNativeIdentifiersPersistable, cancellationToken);
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

				var suspended = ProcessOutSuspended(securityId);
				return (message, suspended, []);
			}

			case MessageTypes.PositionChange:
			{
				var positionMsg = (PositionChangeMessage)message;
				return ProcessSecurityIdMessage(positionMsg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}
					return curr;
				});
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
							_suspendedOutMessages.TryGetValue(suspendedSecId, out var tuple)
						)
						{
							tuple.First ??= [];
							tuple.First.Add(execMsg.Clone());
							return (null, [], []);
						}
					}
				}

				return ProcessSecurityIdMessage(execMsg, null);
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;
				return ProcessSecurityIdMessage(level1Msg, (prev, curr) =>
				{
					foreach (var pair in prev.Changes)
					{
						curr.Changes.TryAdd2(pair.Key, pair.Value);
					}
					return curr;
				});
			}

			case MessageTypes.QuoteChange:
			{
				var quotesMsg = (QuoteChangeMessage)message;
				return ProcessSecurityIdMessage(quotesMsg, (prev, curr) => curr);
			}

			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (newsMsg.SecurityId != null)
					return ProcessMessage(newsMsg.SecurityId.Value, newsMsg, null);
				else
					return (message, [], []);
			}

			default:
			{
				if (message is ISecurityIdMessage secIdMsg)
					return ProcessMessage(secIdMsg.SecurityId, (Message)secIdMsg, null);
				else
					return (message, [], []);
			}
		}
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

		_logReceiver.AddInfoLog("Suspended {0}.", message);
		return null;
	}

	private (Message forward, Message[] extraOut, Message[] loopbackIn) ProcessSecurityIdMessage<TMessage>(
		TMessage message,
		Func<TMessage, TMessage, TMessage> processSuspend)
		where TMessage : Message, ISecurityIdMessage
	{
		return ProcessMessage(message.SecurityId, message, processSuspend);
	}

	private (Message forward, Message[] extraOut, Message[] loopbackIn) ProcessMessage<TMessage>(
		SecurityId securityId,
		TMessage message,
		Func<TMessage, TMessage, TMessage> processSuspend)
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

				return (null, [], []);
			}

			message.ReplaceSecurityId(fullSecurityId.Value);
		}
		else if (!securityId.IsSpecial)
		{
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			var isSecCodeEmpty = securityCode.IsEmpty();

			if (!isSecCodeEmpty && boardCode.IsEmpty())
			{
				SecurityId? foundId = null;

				using (_syncRoot.EnterScope())
				{
					foreach (var id in _securityIds.Values)
					{
						if (!id.SecurityCode.EqualsIgnoreCase(securityCode))
							continue;

						foundId = id;
					}

					if (foundId == null)
					{
						var tuple = _suspendedOutMessages.SafeAdd(securityId, key => RefTuple.Create(new List<Message>(), (Dictionary<MessageTypes, Message>)null));
						tuple.First.Add(message.Clone());
						return (null, [], []);
					}
				}

				message.ReplaceSecurityId(foundId.Value);
			}
		}

		return (message, [], []);
	}

	private async ValueTask<(Message[] extraOut, Message[] loopbackIn)> ProcessAllSuspendedAsync(CancellationToken cancellationToken)
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

		return (outMsgs.ToArray(), inMsgs.ToArray());
	}

	private ValueTask<(Message[] toInner, Message[] toOut)> ProcessInSuspended(SecurityId securityId)
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
			return new(([], []));

		var toInner = new List<Message>();
		foreach (var msg in msgs)
		{
			msg.SecurityId = securityId;
			toInner.Add((Message)msg);
		}

		return new((toInner.ToArray(), []));
	}

	private Message[] ProcessOutSuspended(SecurityId securityId)
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
					p.Key.BoardCode.IsEmpty());

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
			return [];

		// external code shouldn't receive native ids
		securityId.Native = null;

		foreach (var msg in msgs)
			msg.ReplaceSecurityId(securityId);

		return msgs.ToArray();
	}

	private async ValueTask OnStorageNewIdentifierAddedAsync(SecurityId securityId, object nativeId, CancellationToken cancellationToken)
	{
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

			var handler = ProcessSuspendedRequested;
			if (handler != null)
				await handler(temp, cancellationToken);
		}
	}
}
