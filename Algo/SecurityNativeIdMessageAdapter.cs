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
	/// Security native id message adapter.
	/// </summary>
	public class SecurityNativeIdMessageAdapter : MessageAdapterWrapper
	{
		private sealed class InMemoryStorage : INativeIdStorage
		{
			private readonly SynchronizedPairSet<SecurityId, object> _nativeIds = new SynchronizedPairSet<SecurityId, object>();

			void INativeIdStorage.Init()
			{
			}

			bool INativeIdStorage.TryAdd(string name, SecurityId securityId, object nativeId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				if (nativeId == null)
					throw new ArgumentNullException(nameof(nativeId));

				return _nativeIds.TryAdd(securityId, nativeId);
			}

			object INativeIdStorage.TryGetBySecurityId(string name, SecurityId securityId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				return _nativeIds.TryGetValue(securityId);
			}

			SecurityId? INativeIdStorage.TryGetByNativeId(string name, object nativeId)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				SecurityId securityId;

				if (!_nativeIds.TryGetKey(nativeId, out securityId))
					return null;

				return securityId;
			}

			Tuple<SecurityId, object>[] INativeIdStorage.Get(string name)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));

				return _nativeIds.SyncGet(c => c.Select(p => Tuple.Create(p.Key, p.Value)).ToArray());
			}
		}

		private readonly PairSet<object, SecurityId> _securityIds = new PairSet<object, SecurityId>();
		private readonly Dictionary<SecurityId, List<Message>> _suspendedInMessages = new Dictionary<SecurityId, List<Message>>();
		private readonly Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>> _suspendedOutMessages = new Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>>();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly string _storageName;

		/// <summary>
		/// Security native identifier storage.
		/// </summary>
		public INativeIdStorage Storage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityNativeIdMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public SecurityNativeIdMessageAdapter(IMessageAdapter innerAdapter)
			: this(innerAdapter, new InMemoryStorage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityNativeIdMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="storage">Security native identifier storage.</param>
		public SecurityNativeIdMessageAdapter(IMessageAdapter innerAdapter, INativeIdStorage storage)
			: base(innerAdapter)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			Storage = storage;

			_storageName = innerAdapter.NativeIdStorageName;

			if (_storageName.IsEmpty())
				throw new ArgumentException(nameof(innerAdapter));
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

					lock (_syncRoot)
					{
						foreach (var tuple in nativeIds)
						{
							var securityId = tuple.Item1;
							var nativeId = tuple.Item2;

							_securityIds[nativeId] = securityId;
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
						_suspendedOutMessages.Clear();
						_suspendedInMessages.Clear();
					}

					base.OnInnerAdapterNewOutMessage(message);
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

					// external code shouldn't receive native ids
					securityId.Native = null;
					
					if (!boardCode.IsEmpty())
					{
						if (nativeSecurityId != null)
						{
							if (!Storage.TryAdd(_storageName, securityId, nativeSecurityId))
							{
								var prevId = Storage.TryGetByNativeId(_storageName, nativeSecurityId);

								if (prevId != null)
								{
									if (securityId != prevId.Value)
										throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(securityId, prevId.Value, nativeSecurityId));
								}
								else
									throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(Storage.TryGetBySecurityId(_storageName, securityId), nativeSecurityId, securityId));
							}

							lock (_syncRoot)
								_securityIds[nativeSecurityId] = securityId;
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

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleRange:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;
					ProcessMessage(candleMsg.SecurityId, candleMsg, null);
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

					if ((secMsg as MarketDataMessage)?.DataType == MarketDataTypes.News && securityId.IsDefault())
						break;

					object native;

					lock (_syncRoot)
					{
						native = _securityIds.TryGetKey(securityId);

						if (native == null)
						{
							_suspendedInMessages.SafeAdd(securityId).Add(message.Clone());
							return;
						}
					}

					securityId.Native = native;
					ReplaceSecurityId(message, securityId);

					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;
					// TODO
					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityNativeIdMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SecurityNativeIdMessageAdapter(InnerAdapter);
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
					SecurityId? foundId = null;

					lock (_syncRoot)
					{
						foreach (var id in _securityIds.Values)
						{
							if (!id.SecurityCode.CompareIgnoreCase(securityCode))
								continue;

							if (securityId.SecurityType != null && securityId.SecurityType != id.SecurityType)
								continue;

							foundId = id;
						}

						if (foundId == null)
						{
							var tuple = _suspendedOutMessages.SafeAdd(securityId, key => RefTuple.Create(new List<Message>(), (Dictionary<MessageTypes, Message>)null));
							tuple.First.Add(message.Clone());
							return;
						}
					}

					ReplaceSecurityId(message, foundId.Value);

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
				var tuple = _suspendedOutMessages.TryGetValue(securityId);

				if (tuple != null)
				{
					msgs = GetMessages(tuple);
					_suspendedOutMessages.Remove(securityId);
				}

				// find association by code and code + type
				var pair = _suspendedOutMessages
					.FirstOrDefault(p =>
						p.Key.SecurityCode.CompareIgnoreCase(securityId.SecurityCode) &&
						p.Key.BoardCode.IsEmpty() &&
						(securityId.SecurityType == null || p.Key.SecurityType == securityId.SecurityType));

				var value = pair.Value;

				if (value != null)
				{
					_suspendedOutMessages.Remove(pair.Key);

					if (msgs == null)
						msgs = GetMessages(value);
					else
						msgs.AddRange(GetMessages(value));
				}

				var inMsgs = _suspendedInMessages.TryGetValue(securityId);

				if (inMsgs != null)
				{
					_suspendedInMessages.Remove(securityId);

					if (msgs == null)
						msgs = inMsgs;
					else
						msgs.AddRange(inMsgs);
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

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleRange:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var msg = (CandleMessage)message;
					msg.SecurityId = securityId;
					break;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.Str2770);
			}
		}
	}
}