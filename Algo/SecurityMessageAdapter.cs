namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Security message adapter.
	/// </summary>
	public class SecurityMessageAdapter : MessageAdapterWrapper
	{
		private sealed class InMemoryStorage : INativeIdStorage
		{
			private readonly PairSet<SecurityId, object> _nativeIds = new PairSet<SecurityId, object>();

			public bool TryAdd(string name, SecurityId securityId, object nativeId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				if (nativeId == null)
					throw new ArgumentNullException(nameof(nativeId));

				return _nativeIds.TryAdd(securityId, nativeId);
			}

			public object TryGetBySecurityId(string name, SecurityId securityId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				return _nativeIds.TryGetValue(securityId);
			}

			public SecurityId? TryGetByNativeId(string name, object nativeId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				SecurityId securityId;

				if (!_nativeIds.TryGetKey(nativeId, out securityId))
					return null;

				return securityId;
			}

			public IEnumerable<Tuple<SecurityId, object>> Get(string name)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				return _nativeIds.Select(p => Tuple.Create(p.Key, p.Value)).ToArray();
			}
		}

		private readonly Dictionary<SecurityId, SecurityId> _securityIds = new Dictionary<SecurityId, SecurityId>();
		private readonly Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>> _suspendedMessages = new Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>>();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly string _storageName;

		/// <summary>
		/// Native ids storage.
		/// </summary>
		public INativeIdStorage Storage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public SecurityMessageAdapter(IMessageAdapter innerAdapter)
			: this(innerAdapter, new InMemoryStorage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storage">Native ids storage.</param>
		public SecurityMessageAdapter(IMessageAdapter innerAdapter, INativeIdStorage storage)
			: base(innerAdapter)
		{
			Storage = storage;

			_storageName = GetStorageName(innerAdapter);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var nativeIds = Storage.Get(_storageName);

					foreach (var tuple in nativeIds)
					{
						var securityId = tuple.Item1;
						var nativeId = tuple.Item2;

						var fullSecurityId = securityId;
						fullSecurityId.Native = nativeId;

						lock (_syncRoot)
						{
							_securityIds[fullSecurityId] = fullSecurityId;
							_securityIds[securityId] = fullSecurityId;
						}
					}

					base.OnInnerAdapterNewOutMessage(message);
					break;
				}

				case MessageTypes.Reset:
				{
					lock (_syncRoot)
					{
						_securityIds.Clear();
						_suspendedMessages.Clear();
					}

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
						throw new InvalidOperationException();

					if (securityId.SecurityType == null)
						securityId.SecurityType = secMsg.SecurityType;

					var isNativeIdNull = nativeSecurityId == null;

					if (!boardCode.IsEmpty())
					{
						var temp = securityId;
						// GetHashCode shouldn't calc based on native id
						temp.Native = null;

						lock (_syncRoot)
						{
							_securityIds[securityId] = securityId;
							_securityIds[temp] = securityId;
						}

						if (!isNativeIdNull && !Storage.TryAdd(_storageName, temp, nativeSecurityId))
						{
							var prevId = Storage.TryGetByNativeId(_storageName, nativeSecurityId);

							if (prevId != null)
							{
								if (temp != prevId.Value)
									throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(temp, prevId.Value, nativeSecurityId));
							}
							else
								throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(nativeSecurityId, Storage.TryGetBySecurityId(_storageName, temp), temp));
						}
					}
					else
					{
						// TODO
					}

					base.OnInnerAdapterNewOutMessage(message);

					ProcessSuspendedSecurityMessages(securityId);

					break;
				}

				case MessageTypes.Position:
				{
					var positionMsg = (PositionMessage)message;
					ProcessMessage(positionMsg.SecurityId, positionMsg, null);
					break;
				}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;

					ProcessMessage(positionMsg.SecurityId, positionMsg, (prev, curr) =>
					{
						foreach (var pair in prev.Changes)
						{
							curr.Changes.TryAdd(pair.Key, pair.Value);
						}

						return curr;
					});
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					ProcessMessage(execMsg.SecurityId, execMsg, null);
					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					ProcessMessage(level1Msg.SecurityId, level1Msg, (prev, curr) =>
					{
						foreach (var pair in prev.Changes)
						{
							curr.Changes.TryAdd(pair.Key, pair.Value);
						}

						return curr;
					});
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteChangeMsg = (QuoteChangeMessage)message;
					ProcessMessage(quoteChangeMsg.SecurityId, quoteChangeMsg, (prev, curr) => curr);
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;

					if (newsMsg.SecurityId != null)
						ProcessMessage(newsMsg.SecurityId.Value, newsMsg, null);

					break;
				}

				default:
					base.OnInnerAdapterNewOutMessage(message);
					break;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.MarketData:
				{
					var secMsg = (SecurityMessage)message;

					var securityId = secMsg.SecurityId;
					securityId.Native = null;

					var fullSecurityId = _securityIds.TryGetValue2(securityId);

					if (fullSecurityId != null)
						ReplaceSecurityId(message, fullSecurityId.Value);

					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SecurityMessageAdapter(InnerAdapter);
		}

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		public object GetNativeId(SecurityId securityId)
		{
			return Storage.TryGetBySecurityId(_storageName, securityId);
		}

		private void ProcessMessage<TMessage>(SecurityId securityId, TMessage message, Func<TMessage, TMessage, TMessage> processSuspend)
			where TMessage : Message
		{
			if (securityId.Native != null)
			{
				SecurityId? fullSecurityId;

				lock (_syncRoot)
					fullSecurityId = _securityIds.TryGetValue2(securityId);

				if (fullSecurityId == null)
				{
					lock (_syncRoot)
					{
						var tuple = _suspendedMessages.SafeAdd(securityId, key => RefTuple.Create((List<Message>)null, (Dictionary<MessageTypes, Message>)null));

						var clone = message.Clone();

						if (processSuspend == null)
						{
							if (tuple.First == null)
								tuple.First = new List<Message>();

							tuple.First.Add(clone);
						}
						else
						{
							if (tuple.Second == null)
								tuple.Second = new Dictionary<MessageTypes, Message>();

							var prev = tuple.Second.TryGetValue(clone.Type);

							tuple.Second[clone.Type] = prev == null
								? clone
								: processSuspend((TMessage)prev, (TMessage)clone);
						}
					}

					return;
				}

				ReplaceSecurityId(message, fullSecurityId.Value);
			}
			else
			{
				var securityCode = securityId.SecurityCode;
				var boardCode = securityId.BoardCode;

				var isSecCodeEmpty = securityCode.IsEmpty();

				if (isSecCodeEmpty && message.Type != MessageTypes.Execution)
					throw new InvalidOperationException();

				if (!isSecCodeEmpty && boardCode.IsEmpty())
				{
					SecurityId? foundedId = null;

					lock (_syncRoot)
					{
						foreach (var id in _securityIds.Keys)
						{
							if (!id.SecurityCode.CompareIgnoreCase(securityCode))
								continue;

							if (securityId.SecurityType != null && securityId.SecurityType != id.SecurityType)
								continue;

							foundedId = id;
						}

						if (foundedId == null)
						{
							var tuple = _suspendedMessages.SafeAdd(securityId, key => RefTuple.Create(new List<Message>(), (Dictionary<MessageTypes, Message>)null));
							tuple.First.Add(message.Clone());
							return;
						}
					}

					ReplaceSecurityId(message, foundedId.Value);

					//// если указан код и тип инструмента, то пытаемся найти инструмент по ним
					//if (securityId.SecurityType != null)
					//{

					//}
					//else
					//	throw new ArgumentException(nameof(securityId), LocalizedStrings.Str682Params.Put(securityCode, securityId.SecurityType));
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessSuspendedSecurityMessages(SecurityId securityId)
		{
			List<Message> msgs = null;

			lock (_syncRoot)
			{
				var tuple = _suspendedMessages.TryGetValue(securityId);

				if (tuple != null)
				{
					msgs = GetMessages(tuple);
					_suspendedMessages.Remove(securityId);
				}

				// find association by code and code + type
				var pair = _suspendedMessages
					.FirstOrDefault(p =>
						p.Key.SecurityCode.CompareIgnoreCase(securityId.SecurityCode) &&
						p.Key.BoardCode.IsEmpty() &&
						(securityId.SecurityType == null || p.Key.SecurityType == securityId.SecurityType));

				var value = pair.Value;

				if (value != null)
				{
					_suspendedMessages.Remove(pair.Key);

					if (msgs == null)
						msgs = GetMessages(value);
					else
						msgs.AddRange(GetMessages(value));
				}
			}

			if (msgs == null)
				return;

			foreach (var msg in msgs)
			{
				ReplaceSecurityId(msg, securityId);
				base.OnInnerAdapterNewOutMessage(msg);
			}
		}

		private static List<Message> GetMessages(RefPair<List<Message>, Dictionary<MessageTypes, Message>> tuple)
		{
			var retVal = tuple.First;

			if (retVal == null)
				retVal = tuple.Second.Values.ToList();
			else if (tuple.Second != null)
				retVal.AddRange(tuple.Second.Values);

			return retVal;
		}

		private static void ReplaceSecurityId(Message message, SecurityId securityId)
		{
			switch (message.Type)
			{
				case MessageTypes.Position:
				{
					var positionMsg = (PositionMessage)message;
					positionMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;
					positionMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					execMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;
					level1Msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteChangeMsg = (QuoteChangeMessage)message;
					quoteChangeMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;
					newsMsg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var msg = (OrderRegisterMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderReplace:
				{
					var msg = (OrderReplaceMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var msg = (OrderCancelMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				case MessageTypes.MarketData:
				{
					var msg = (MarketDataMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.Str2770);
			}
		}

		private string GetStorageName(IMessageAdapter innerAdapter)
		{
			var adapter = innerAdapter;
			var wrapper = adapter as IMessageAdapterWrapper;

			while (wrapper != null)
			{
				adapter = wrapper.InnerAdapter;
				wrapper = adapter as IMessageAdapterWrapper;
			}

			return adapter.GetType().Name.Replace("MessageAdapter", string.Empty);
		}
	}
}