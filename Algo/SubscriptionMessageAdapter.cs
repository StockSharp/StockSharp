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
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private sealed class SubscriptionInfo<TMessage>
			where TMessage : Message
		{
			public TMessage Message { get; }

			public IList<TMessage> Subscriptions { get; }

			public int Subscribers { get; set; }

			public bool IsSubscribed { get; set; }

			public SubscriptionInfo(TMessage message)
			{
				Message = message ?? throw new ArgumentNullException(nameof(message));
				Subscriptions = new List<TMessage>();
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
		/// <summary>
		/// Error case like connection lost etc.
		/// </summary>
		public bool IsRestoreOnErrorReconnect { get; set; }

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		/// <summary>
		/// Normal case connect/disconnect.
		/// </summary>
		public bool IsRestoreOnNormalReconnect { get; set; }

		/// <summary>
		/// Support multiple subscriptions with duplicate parameters.
		/// </summary>
		public bool SupportMultipleSubscriptions { get; set; }

		private void ClearSubscribers()
		{
			_subscribers.Clear();
			_newsSubscribers.Clear();
			_pfSubscribers.Clear();
			_subscribersById.Clear();
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
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
					base.SendInMessage(message);
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

					base.SendInMessage(message);
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
							pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
							pfMsg.IsSubscribe = false;

							if (IsRestoreOnNormalReconnect)
								_passThroughtIds.Add(pfMsg.TransactionId);
						}

						base.SendInMessage(msg);
					}

					base.SendInMessage(message);
					break;
				}

				case MessageTypes.MarketData:
					ProcessInMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.Portfolio:
					ProcessInPortfolioMessage((PortfolioMessage)message);
					break;

				default:
					base.SendInMessage(message);
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
			List<Message> clones = null;

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

				case ExtendedMessageTypes.RestoringSubscription:
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

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					clones = ReplicateMessages(message);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					
					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Tick:
						case ExecutionTypes.OrderLog:
						{
							clones = ReplicateMessages(message);
							break;
						}
					}

					break;
				}

				case MessageTypes.News:
				{
					clones = ReplicateMessages(message);
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (clones != null)
			{
				foreach (var clone in clones)
				{
					base.OnInnerAdapterNewOutMessage(clone);
				}
			}

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

		private List<Message> ReplicateMessages(Message message)
		{
			long originTransId;

			switch (message)
			{
				case CandleMessage candleMsg:
					originTransId = candleMsg.OriginalTransactionId;
					break;
				case ExecutionMessage execMsg:
					originTransId = execMsg.OriginalTransactionId;
					break;
				case NewsMessage newsMsg:
					originTransId = newsMsg.OriginalTransactionId;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message?.ToString());
			}

			lock (_sync)
			{
				if (!_subscribersById.TryGetValue(originTransId, out var info))
					return null;

				if (info.Message.TransactionId == originTransId && info.Subscriptions.Count > 1)
				{
					var clones = new List<Message>(info.Subscriptions.Count - 1);

					foreach (var subscription in info.Subscriptions)
					{
						if (subscription.TransactionId == info.Message.TransactionId)
							continue;

						var clone = message.Clone();

						switch (clone)
						{
							case CandleMessage candleMsg:
								candleMsg.OriginalTransactionId = subscription.TransactionId;
								break;
							case ExecutionMessage execMsg:
								execMsg.OriginalTransactionId = subscription.TransactionId;
								break;
							case NewsMessage newsMsg:
								newsMsg.OriginalTransactionId = subscription.TransactionId;
								break;
						}

						clones.Add(clone);
					}

					return clones;
				}
			}

			return null;
		}

		private SecurityId GetSecurityId(MarketDataMessage message) => IsSupportSubscriptionBySecurity ? message.SecurityId : default(SecurityId);

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			if (_passThroughtIds.Contains(message.TransactionId))
			{
				base.SendInMessage(message);
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
					: ProcessSubscription(_subscribers, message.CreateKey(GetSecurityId(message)), message, ref sendIn, ref isOnlyHistory, ref sendOutMsg);
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

				base.SendInMessage(message);
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
					: ProcessSubscriptionResult(_subscribers, info.Message.CreateKey(GetSecurityId(message)), info, message);
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
			var removeInfo = !isSubscribe || message.Error != null || message.IsNotSupported;

			info.IsSubscribed = isSubscribe && message.Error == null && !message.IsNotSupported;

			var replies = new List<MarketDataMessage>();

			// TODO только нужная подписка
			foreach (var subscription in info.Subscriptions)
			{
				var reply = (MarketDataMessage)subscription.Clone();
				reply.OriginalTransactionId = subscription.TransactionId;
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
			var subscribersCount = info.Subscribers;
			var isSubscribe = message.IsSubscribe;

			if (isSubscribe)
			{
				subscribersCount++;
				sendIn = subscribersCount == 1;

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
				if (subscribersCount > 0)
				{
					subscribersCount--;
					sendIn = subscribersCount == 0;
				}
				else
					sendOutMsg = NonExist(message);
			}

			if (sendOutMsg != null)
				return info;

			//if (isSubscribe)
			info.Subscriptions.Add(clone ?? (MarketDataMessage)message.Clone());

			_subscribersById.Add(message.TransactionId, info);

			if (!sendIn && info.IsSubscribed)
			{
				sendOutMsg = new MarketDataMessage
				{
					DataType = message.DataType,
					IsSubscribe = isSubscribe,
					SecurityId = message.SecurityId,
					Arg = message.Arg,
					OriginalTransactionId = message.TransactionId,
				};
			}

			if (subscribersCount > 0)
			{
				info.Subscribers = subscribersCount;
				subscriptions[key] = info;
			}
			else
				subscriptions.Remove(key);

			return info;
		}

		private static MarketDataMessage NonExist(MarketDataMessage message)
		{
			return new MarketDataMessage
			{
				DataType = message.DataType,
				IsSubscribe = false,
				SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				Error = new InvalidOperationException(LocalizedStrings.SubscriptionNonExist),
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

				var subscribersCount = info.Subscribers;

				if (message.IsSubscribe)
				{
					subscribersCount++;
					sendIn = subscribersCount == 1;
				}
				else
				{
					if (subscribersCount > 0)
					{
						subscribersCount--;
						sendIn = subscribersCount == 0;
					}
					//else
					//	sendOutMsg = NonExist(message);
				}

				info.Subscriptions.Add(clone ?? (PortfolioMessage)message.Clone());

				if (subscribersCount > 0)
				{
					info.Subscribers = subscribersCount;
					_pfSubscribers[pfName] = info;
				}
				else
					_pfSubscribers.Remove(pfName);
			}

			if (sendIn)
			{
				if (!message.IsSubscribe && message.OriginalTransactionId == 0)
					message.OriginalTransactionId = info.Message.TransactionId;

				base.SendInMessage(message);
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
			};
		}
	}
}