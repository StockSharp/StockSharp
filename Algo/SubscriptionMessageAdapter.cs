namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private sealed class SubscriptionInfo<TMessage>
			where TMessage : Message
		{
			public TMessage Message { get; }

			// subscribe/unsubscribe requests set
			public List<TMessage> Requests { get; } = new List<TMessage>();

			public CachedSynchronizedSet<long> Subscribers { get; } = new CachedSynchronizedSet<long>();

			public bool IsSubscribed { get; set; }

			public SubscriptionInfo(TMessage message)
			{
				Message = message ?? throw new ArgumentNullException(nameof(message));
			}
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<Helper.SubscriptionKey, SubscriptionInfo<MarketDataMessage>> _mdSubscribers = new Dictionary<Helper.SubscriptionKey, SubscriptionInfo<MarketDataMessage>>();
		private readonly Dictionary<string, SubscriptionInfo<MarketDataMessage>> _newsBoardSubscribers = new Dictionary<string, SubscriptionInfo<MarketDataMessage>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, SubscriptionInfo<PortfolioMessage>> _pfSubscribers = new Dictionary<string, SubscriptionInfo<PortfolioMessage>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<long, SubscriptionInfo<MarketDataMessage>> _mdSubscribersById = new Dictionary<long, SubscriptionInfo<MarketDataMessage>>();
		private readonly Dictionary<long, SubscriptionInfo<OrderStatusMessage>> _orderStatusSubscribers = new Dictionary<long, SubscriptionInfo<OrderStatusMessage>>();
		private readonly Dictionary<long, SubscriptionInfo<PortfolioLookupMessage>> _pfLookupSubscribers = new Dictionary<long, SubscriptionInfo<PortfolioLookupMessage>>();
		private readonly HashSet<long> _onlyHistorySubscriptions = new HashSet<long>();
		private readonly List<Message> _subscriptionRequests = new List<Message>();
		private readonly HashSet<long> _passThroughtIds = new HashSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		/// <remarks>
		/// Error case like connection lost etc.
		/// </remarks>
		public bool IsRestoreOnErrorReconnect { get; set; }

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		/// <remarks>
		/// Normal case connect/disconnect.
		/// </remarks>
		public bool IsRestoreOnNormalReconnect { get; set; }

		/// <summary>
		/// Support multiple subscriptions with duplicate parameters.
		/// </summary>
		public bool SupportMultipleSubscriptions { get; set; }

		/// <summary>
		/// Send back reply for non existing unsubscription requests with filled <see cref="MarketDataMessage.Error"/> property.
		/// </summary>
		public bool NonExistSubscriptionAsError { get; set; }

		private void ClearSubscribers()
		{
			_mdSubscribers.Clear();
			_newsBoardSubscribers.Clear();
			_pfSubscribers.Clear();
			_mdSubscribersById.Clear();
			_orderStatusSubscribers.Clear();
			_pfLookupSubscribers.Clear();
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			if (message.IsBack)
			{
				if (message.Adapter == this)
				{
					message.Adapter = null;
					message.IsBack = false;
				}
				else
				{
					base.OnSendInMessage(message);
					return;
				}
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_sync)
					{
						if (!IsRestoreOnErrorReconnect)
							ClearSubscribers();
					
						_subscriptionRequests.Clear();
						_passThroughtIds.Clear();
					}

					base.OnSendInMessage(message);
					break;
				}

				case MessageTypes.Disconnect:
				{
					var messages = new List<Message>();

					lock (_sync)
					{
						FillSubscriptionList(messages);

						if (IsRestoreOnNormalReconnect)
							_subscriptionRequests.AddRange(messages.Select(m => m.Clone()));
						else
							ClearSubscribers();
					}

					foreach (var msg in messages)
					{
						if (msg is MarketDataMessage mdMsg)
						{
							mdMsg.OriginalTransactionId = mdMsg.TransactionId;
							mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
							mdMsg.IsSubscribe = false;

							if (IsRestoreOnNormalReconnect)
								_passThroughtIds.Add(mdMsg.TransactionId);
						}
						else if (msg is OrderStatusMessage statusMsg)
						{
							statusMsg.OriginalTransactionId = statusMsg.TransactionId;
							statusMsg.TransactionId = TransactionIdGenerator.GetNextId();
							statusMsg.IsSubscribe = false;

							if (IsRestoreOnNormalReconnect)
								_passThroughtIds.Add(statusMsg.TransactionId);
						}
						else if (msg is PortfolioMessage pfMsg)
						{
							pfMsg.OriginalTransactionId = pfMsg.TransactionId;
							pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
							pfMsg.IsSubscribe = false;

							if (IsRestoreOnNormalReconnect)
								_passThroughtIds.Add(pfMsg.TransactionId);
						}

						base.OnSendInMessage(msg);
					}

					base.OnSendInMessage(message);
					break;
				}

				case MessageTypes.MarketData:
					ProcessInMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.Portfolio:
					ProcessInPortfolioMessage((PortfolioMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessInPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;

				case MessageTypes.OrderStatus:
					ProcessInOrderStatusMessage((OrderStatusMessage)message);
					break;

				default:
					base.OnSendInMessage(message);
					break;
			}
		}

		private void FillSubscriptionList(List<Message> messages)
		{
			messages.AddRange(_mdSubscribers.Values.Select(p => p.Message.Clone()));
			messages.AddRange(_newsBoardSubscribers.Values.Select(p => p.Message.Clone()));
			messages.AddRange(_pfSubscribers.Values.Select(p => p.Message.Clone()));
			messages.AddRange(_orderStatusSubscribers.Values.Select(p => p.Message.Clone()));
			messages.AddRange(_pfLookupSubscribers.Values.Select(p => p.Message.Clone()));
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			List<Message> messages = null;

			void FillSubscriptions()
			{
				messages = new List<Message>();

				lock (_sync)
				{
					FillSubscriptionList(messages);

					//ClearSubscribers();
				}

				if (messages.Count == 0)
					messages = null;
			}

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var connectMsg = (ConnectMessage)message;

					if (connectMsg.Error == null)
					{
						if (IsRestoreOnErrorReconnect)
							FillSubscriptions();
						else if (IsRestoreOnNormalReconnect)
						{
							lock (_sync)
							{
								if (_subscriptionRequests.Count > 0)
								{
									messages = new List<Message>(_subscriptionRequests);
									_subscriptionRequests.Clear();
								}
							}
						}
					}

					break;
				}

				case ExtendedMessageTypes.ReconnectingFinished:
				{
					if (IsRestoreOnErrorReconnect)
						FillSubscriptions();

					break;
				}

				case MessageTypes.MarketData:
				{
					if (ProcessOutMarketDataMessage((MarketDataMessage)message))
						return;
					
					break;
				}

				case MessageTypes.PortfolioLookupResult:
				{
					if (ProcessOutPortfolioLookupResultMessage((PortfolioLookupResultMessage)message))
						return;
					
					break;
				}

				case MessageTypes.Portfolio:
				case MessageTypes.PortfolioChange:
				case MessageTypes.PositionChange:

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:

				case MessageTypes.News:
				case MessageTypes.BoardState:
				case MessageTypes.Execution:
				{
					ApplySubscriptionIds((ISubscriptionIdMessage)message);
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (messages != null)
			{
				foreach (var msg in messages)
				{
					msg.IsBack = true;
					msg.Adapter = this;

					if (msg is MarketDataMessage mdMsg)
					{
						//mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
						_passThroughtIds.Add(mdMsg.TransactionId);
					}
					else if (msg is PortfolioMessage pfMsg)
					{
						//pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
						_passThroughtIds.Add(pfMsg.TransactionId);
					}
					else if (msg is OrderStatusMessage statusMsg)
					{
						//statusMsg.TransactionId = TransactionIdGenerator.GetNextId();
						_passThroughtIds.Add(statusMsg.TransactionId);
					}

					base.OnInnerAdapterNewOutMessage(msg);
				}
			}
		}

		private void ApplySubscriptionIds(ISubscriptionIdMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			long originTransId;

			switch (message)
			{
				case CandleMessage candleMsg:
					originTransId = candleMsg.OriginalTransactionId;
					break;
				case ExecutionMessage execMsg:
					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.OrderLog:
							originTransId = execMsg.OriginalTransactionId;
							break;
						default:
							ApplyTransactionalSubscriptionIds(execMsg, _orderStatusSubscribers);
							return;
					}

					break;
				case NewsMessage newsMsg:
					originTransId = newsMsg.OriginalTransactionId;
					break;

				case BoardMessage boardStateMsg:
					originTransId = boardStateMsg.OriginalTransactionId;
					break;

				case BoardStateMessage boardStateMsg:
					originTransId = boardStateMsg.OriginalTransactionId;
					break;

				case PortfolioMessage _:
				case BasePositionChangeMessage _:
					ApplyTransactionalSubscriptionIds(message, _pfLookupSubscribers);
					return;

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.ToString());
			}

			lock (_sync)
			{
				if (!_mdSubscribersById.TryGetValue(originTransId, out var info))
					return;

				//if (info.Message.TransactionId == originTransId && info.Subscribers.Count > 1)
				message.SubscriptionIds = info.Subscribers.Cache;
			}
		}

		private void ApplyTransactionalSubscriptionIds<TMessage>(ISubscriptionIdMessage message, Dictionary<long, SubscriptionInfo<TMessage>> lookupSubscribers)
			where TMessage : Message, ISubscriptionIdMessage
		{
			lock (_sync)
			{
				if (message.OriginalTransactionId > 0 && lookupSubscribers.ContainsKey(message.OriginalTransactionId))
					message.SubscriptionId = message.OriginalTransactionId;
					
				if (_pfLookupSubscribers.Count == 0)
					return;

				message.SubscriptionIds = lookupSubscribers.First().Value.Subscribers.Cache;
			}
		}

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			MarketDataMessage CreateSendOut(bool isSubscribe, long transId)
			{
				return new MarketDataMessage
				{
					//DataType = message.DataType,
					IsSubscribe = isSubscribe,
					//SecurityId = message.SecurityId,
					//Arg = message.Arg,
					OriginalTransactionId = transId,
				};
			}

			MarketDataMessage CreateNonExist(long transId)
			{
				if (!NonExistSubscriptionAsError)
					this.AddInfoLog(LocalizedStrings.SubscriptionNonExist);

				return new MarketDataMessage
				{
					OriginalTransactionId = transId,
					Error = NonExistSubscriptionAsError ? new InvalidOperationException(LocalizedStrings.SubscriptionNonExist) : null,
				};
			}

			if (message.DataType.IsSecurityRequired())
				ProcessInSubscriptionMessage(message, message.CreateKey(GetSecurityId(message.SecurityId)), _mdSubscribers, _mdSubscribersById, CreateSendOut, CreateNonExist);
			else
				ProcessInSubscriptionMessage(message, GetNewsBoardKey(message), _newsBoardSubscribers, _mdSubscribersById, CreateSendOut, CreateNonExist);
		}

		private static string GetNewsBoardKey(MarketDataMessage message)
			=> (message.DataType == MarketDataTypes.News ? message.NewsId : message.BoardCode) ?? string.Empty;

		private void ProcessInOrderStatusMessage(OrderStatusMessage message)
		{
			ProcessInSubscriptionMessage(message, message.TransactionId, _orderStatusSubscribers, null, null, null);
		}

		private void ProcessInPortfolioLookupMessage(PortfolioLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, message.TransactionId, _pfLookupSubscribers, null, null, null);
		}

		private void ProcessInPortfolioMessage(PortfolioMessage message)
		{
			ProcessInSubscriptionMessage(message, message.PortfolioName, _pfSubscribers, null, null, null);
		}

		private SecurityId GetSecurityId(SecurityId securityId) => IsSupportSubscriptionBySecurity ? securityId : default;

		private void ProcessInSubscriptionMessage<TKey, TMessage>(TMessage message, TKey key,
			Dictionary<TKey, SubscriptionInfo<TMessage>> subscriptions,
			Dictionary<long, SubscriptionInfo<TMessage>> subscribersById,
			Func<bool, long, TMessage> createSendOut, Func<long, TMessage> createNotExist)
			where TMessage : Message, ISubscriptionMessage
		{
			TMessage sendInMsg = null;
			TMessage sendOutMsg = null;

			try
			{
				lock (_sync)
				{
					if (_passThroughtIds.Contains(message.TransactionId))
					{
						sendInMsg = message;
						return;
					}

					var sendIn = false;
					var isOnlyHistory = false;

					var info = ProcessInSubscription(subscriptions, subscribersById, key, message,
						ref sendIn, ref isOnlyHistory, ref sendOutMsg, createSendOut, createNotExist);

					if (sendIn)
					{
						if (!message.IsSubscribe && message.OriginalTransactionId == 0)
							message.OriginalTransactionId = info.Message.TransactionId;
						else
						{
							message.IsHistory = isOnlyHistory;

							if (isOnlyHistory)
								_onlyHistorySubscriptions.Add(message.TransactionId);
						}

						sendInMsg = message;
					}
				}
			}
			finally
			{
				if (sendInMsg != null)
					base.OnSendInMessage(sendInMsg);
			}

			if (sendOutMsg != null)
				RaiseNewOutMessage(sendOutMsg);
		}

		private SubscriptionInfo<TMessage> ProcessInSubscription<TKey, TMessage>(
			Dictionary<TKey, SubscriptionInfo<TMessage>> subscriptions,
			Dictionary<long, SubscriptionInfo<TMessage>> subscribersById,
			TKey key, TMessage message, ref bool sendIn, ref bool isOnlyHistory, ref TMessage sendOutMsg,
			Func<bool, long, TMessage> createSendOut, Func<long, TMessage> createNotExist)
			where TMessage : Message, ISubscriptionMessage
		{
			TMessage clone = null;
			var info = subscriptions.TryGetValue(key) ?? new SubscriptionInfo<TMessage>(clone = (TMessage)message.Clone());
			var subscribers = info.Subscribers;
			var isSubscribe = message.IsSubscribe;
			var transId = message.TransactionId;

			if (isSubscribe)
			{
				subscribers.Add(transId);
				sendIn = subscribers.Count == 1;

				if (SupportMultipleSubscriptions)
				{
					if (!sendIn)
					{
						isOnlyHistory = true;
						sendIn = true;
					}
				}
			}
			else
			{
				if (subscribers.Count > 0)
				{
					subscribers.Remove(message.OriginalTransactionId);
					sendIn = subscribers.Count == 0;
				}
				else
				{
					if (createNotExist == null)
						return info;

					sendOutMsg = createNotExist(transId);
				}
			}

			if (sendOutMsg != null)
				return info;

			//if (isSubscribe)
			info.Requests.Add(clone ?? (TMessage)message.Clone());

			subscribersById?.Add(transId, info);

			if (!sendIn && info.IsSubscribed)
			{
				sendOutMsg = createSendOut?.Invoke(isSubscribe, transId);
			}

			if (subscribers.Count > 0)
				subscriptions[key] = info;
			else
				subscriptions.Remove(key);

			return info;
		}

		private bool ProcessOutMarketDataMessage(MarketDataMessage message)
		{
			return ProcessOutSubscriptionMessage(message.OriginalTransactionId, _mdSubscribersById, info =>
				info.Message.DataType.IsSecurityRequired()
					? ProcessSubscriptionResult(_mdSubscribers, info.Message.CreateKey(GetSecurityId(info.Message.SecurityId)), info, message)
					: ProcessSubscriptionResult(_newsBoardSubscribers, GetNewsBoardKey(info.Message), info, message));
		}

		private bool ProcessOutPortfolioLookupResultMessage(PortfolioLookupResultMessage message)
		{
			return ProcessOutSubscriptionMessage(message.OriginalTransactionId, _pfLookupSubscribers, null);
		}

		private bool ProcessOutSubscriptionMessage<TMessage>(long originId, Dictionary<long, SubscriptionInfo<TMessage>> subscribersById, Func<SubscriptionInfo<TMessage>, IEnumerable<TMessage>> createReply)
			where TMessage : Message, ISubscriptionMessage
		{
			IEnumerable<TMessage> replies;

			lock (_sync)
			{
				if (_onlyHistorySubscriptions.Remove(originId) || _passThroughtIds.Remove(originId))
					return false;

				var info = subscribersById.TryGetValue(originId);

				if (info == null)
					return false;

				replies = createReply?.Invoke(info);
			}

			if (replies == null)
				return false;

			foreach (var reply in replies)
			{
				base.OnInnerAdapterNewOutMessage(reply);
			}

			return true;
		}

		private IEnumerable<MarketDataMessage> ProcessSubscriptionResult<T>(Dictionary<T, SubscriptionInfo<MarketDataMessage>> subscriptions, T key, SubscriptionInfo<MarketDataMessage> info, MarketDataMessage message)
		{
			//var info = subscriptions.TryGetValue(key);

			if (!subscriptions.ContainsKey(key))
				return null;

			//var isSubscribe = info.Message.IsSubscribe;
			//var removeInfo = !isSubscribe || !message.IsOk();

			info.IsSubscribed = info.Message.IsSubscribe && message.IsOk();

			var replies = new List<MarketDataMessage>();

			// TODO только нужная подписка
			foreach (var requests in info.Requests)
			{
				var reply = (MarketDataMessage)requests.Clone();
				reply.OriginalTransactionId = requests.TransactionId;
				//reply.TransactionId = message.TransactionId;
				reply.Error = message.Error;
				reply.IsNotSupported = message.IsNotSupported;

				replies.Add(reply);
			}

			if (!info.IsSubscribed)
			{
				subscriptions.Remove(key);
				_mdSubscribersById.RemoveWhere(p => p.Value == info);
			}

			return replies;
		}

		/// <summary>
		/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionMessageAdapter(InnerAdapter)
			{
				IsRestoreOnErrorReconnect = IsRestoreOnErrorReconnect,
				IsRestoreOnNormalReconnect = IsRestoreOnNormalReconnect,
				SupportMultipleSubscriptions = SupportMultipleSubscriptions,
				NonExistSubscriptionAsError = NonExistSubscriptionAsError,
			};
		}
	}
}