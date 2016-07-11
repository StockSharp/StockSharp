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
	public class SecurityAdapter : MessageAdapterWrapper
	{
		private readonly Dictionary<SecurityId, SecurityId> _securityIds = new Dictionary<SecurityId, SecurityId>();
		private readonly PairSet<SecurityId, object> _nativeIds = new PairSet<SecurityId, object>();
		private readonly Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>> _suspendedMessages = new Dictionary<SecurityId, RefPair<List<Message>, Dictionary<MessageTypes, Message>>>();
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public SecurityAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncRoot)
					{
						_securityIds.Clear();
						_suspendedMessages.Clear();
						_nativeIds.Clear();
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
						lock (_syncRoot)
						{
							_securityIds[securityId] = securityId;

							var temp = securityId;
							// GetHashCode shouldn't calc based on native id
							temp.Native = null;

							if (!isNativeIdNull && !_nativeIds.TryAdd(temp, nativeSecurityId))
							{
								SecurityId prevId;

								if (_nativeIds.TryGetKey(nativeSecurityId, out prevId))
									throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(securityId, prevId, nativeSecurityId));
								else
									throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(nativeSecurityId, _nativeIds[temp], temp));
							}
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
		/// Create a copy of <see cref="SecurityAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SecurityAdapter(InnerAdapter);
		}

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		public object GetNativeId(SecurityId securityId)
		{
			return _nativeIds.TryGetValue(securityId);
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

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.Str2770);
			}
		}
	}
}