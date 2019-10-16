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

		private readonly Dictionary<Helper.SubscriptionKey, SubscriptionInfo<MarketDataMessage>> _subscribers = new Dictionary<Helper.SubscriptionKey, SubscriptionInfo<MarketDataMessage>>();
		private readonly Dictionary<string, SubscriptionInfo<MarketDataMessage>> _newsSubscribers = new Dictionary<string, SubscriptionInfo<MarketDataMessage>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, SubscriptionInfo<PortfolioMessage>> _pfSubscribers = new Dictionary<string, SubscriptionInfo<PortfolioMessage>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<long, SubscriptionInfo<MarketDataMessage>> _subscribersById = new Dictionary<long, SubscriptionInfo<MarketDataMessage>>();
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
			_subscribers.Clear();
			_newsSubscribers.Clear();
			_pfSubscribers.Clear();
			_subscribersById.Clear();
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
						messages.AddRange(_newsSubscribers.Values.Select(p => p.Message.Clone()));
						messages.AddRange(_subscribers.Values.Select(p => p.Message.Clone()));
						messages.AddRange(_pfSubscribers.Values.Select(p => p.Message.Clone()));

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

				default:
					base.OnSendInMessage(message);
					break;
			}
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
					messages.AddRange(_subscribers.Values.Select(p => p.Message.Clone()));
					messages.AddRange(_newsSubscribers.Values.Select(p => p.Message.Clone()));
					messages.AddRange(_pfSubscribers.Values.Select(p => p.Message.Clone()));

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
							break;
					}

					break;
				case NewsMessage newsMsg:
					originTransId = newsMsg.OriginalTransactionId;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.ToString());
			}

			lock (_sync)
			{
				if (!_subscribersById.TryGetValue(originTransId, out var info))
					return;

				//if (info.Message.TransactionId == originTransId && info.Subscribers.Count > 1)
				message.SubscriptionIds = info.Subscribers.Cache;
			}
		}

		private SecurityId GetSecurityId(SecurityId securityId) => IsSupportSubscriptionBySecurity ? securityId : default;

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			if (_passThroughtIds.Contains(message.TransactionId))
			{
				base.OnSendInMessage(message);
				return;
			}

			var sendIn = false;
			var isOnlyHistory = false;
			MarketDataMessage sendOutMsg = null;
			SubscriptionInfo<MarketDataMessage> info;

			lock (_sync)
			{
				info = message.DataType == MarketDataTypes.News
					? ProcessSubscription(_newsSubscribers, message.NewsId ?? string.Empty, message, ref sendIn, ref isOnlyHistory, ref sendOutMsg)
					: ProcessSubscription(_subscribers, message.CreateKey(GetSecurityId(message.SecurityId)), message, ref sendIn, ref isOnlyHistory, ref sendOutMsg);
			}

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

				base.OnSendInMessage(message);
			}

			if (sendOutMsg != null)
				RaiseNewOutMessage(sendOutMsg);
		}

		private bool ProcessOutMarketDataMessage(MarketDataMessage message)
		{
			var originId = message.OriginalTransactionId;

			if (_onlyHistorySubscriptions.Remove(originId) || _passThroughtIds.Remove(originId))
				return false;

			IEnumerable<MarketDataMessage> replies;

			lock (_sync)
			{
				var info = _subscribersById.TryGetValue(originId);

				if (info == null)
					return false;

				replies = info.Message.DataType == MarketDataTypes.News
					? ProcessSubscriptionResult(_newsSubscribers, info.Message.NewsId ?? string.Empty, info, message)
					: ProcessSubscriptionResult(_subscribers, info.Message.CreateKey(GetSecurityId(info.Message.SecurityId)), info, message);
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

			var isSubscribe = info.Message.IsSubscribe;
			var removeInfo = !isSubscribe || !message.IsOk();

			info.IsSubscribed = isSubscribe && message.IsOk();

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

			if (removeInfo)
			{
				subscriptions.Remove(key);
				_subscribersById.RemoveWhere(p => p.Value == info);
			}

			return replies;
		}

		private SubscriptionInfo<MarketDataMessage> ProcessSubscription<T>(Dictionary<T, SubscriptionInfo<MarketDataMessage>> subscriptions, T key, MarketDataMessage message, ref bool sendIn, ref bool isOnlyHistory, ref MarketDataMessage sendOutMsg)
		{
			MarketDataMessage clone = null;
			var info = subscriptions.TryGetValue(key) ?? new SubscriptionInfo<MarketDataMessage>(clone = (MarketDataMessage)message.Clone());
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
					sendOutMsg = NonExist(message);
			}

			if (sendOutMsg != null)
				return info;

			//if (isSubscribe)
			info.Requests.Add(clone ?? (MarketDataMessage)message.Clone());

			_subscribersById.Add(transId, info);

			if (!sendIn && info.IsSubscribed)
			{
				sendOutMsg = new MarketDataMessage
				{
					DataType = message.DataType,
					IsSubscribe = isSubscribe,
					SecurityId = message.SecurityId,
					Arg = message.Arg,
					OriginalTransactionId = transId,
				};
			}

			if (subscribers.Count > 0)
				subscriptions[key] = info;
			else
				subscriptions.Remove(key);

			return info;
		}

		private MarketDataMessage NonExist(MarketDataMessage message)
		{
			if (!NonExistSubscriptionAsError)
				this.AddInfoLog(LocalizedStrings.SubscriptionNonExist);

			return new MarketDataMessage
			{
				//DataType = message.DataType,
				//IsSubscribe = false,
				//SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				Error = NonExistSubscriptionAsError ? new InvalidOperationException(LocalizedStrings.SubscriptionNonExist) : null,
			};
		}

		private void ProcessInPortfolioMessage(PortfolioMessage message)
		{
			var sendIn = false;
			var pfName = message.PortfolioName;
			
			SubscriptionInfo<PortfolioMessage> info;

			lock (_sync)
			{
				PortfolioMessage clone = null;
				info = _pfSubscribers.TryGetValue(pfName) ?? new SubscriptionInfo<PortfolioMessage>(clone = (PortfolioMessage)message.Clone());

				var subscribers = info.Subscribers;

				if (message.IsSubscribe)
				{
					subscribers.Add(message.TransactionId);
					sendIn = subscribers.Count == 1;
				}
				else
				{
					if (subscribers.Count > 0)
					{
						subscribers.Remove(message.OriginalTransactionId);
						sendIn = subscribers.Count == 0;
					}
					//else
					//	sendOutMsg = NonExist(message);
				}

				info.Requests.Add(clone ?? (PortfolioMessage)message.Clone());

				if (subscribers.Count > 0)
					_pfSubscribers[pfName] = info;
				else
					_pfSubscribers.Remove(pfName);
			}

			if (sendIn)
			{
				if (!message.IsSubscribe && message.OriginalTransactionId == 0)
					message.OriginalTransactionId = info.Message.TransactionId;

				base.OnSendInMessage(message);
			}
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