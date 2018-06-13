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
		private sealed class SubscriptionInfo
		{
			public MarketDataMessage Message { get; }

			public IList<MarketDataMessage> Subscriptions { get; }

			public int Subscribers { get; set; }

			public bool IsSubscribed { get; set; }

			public SubscriptionInfo(MarketDataMessage message)
			{
				Message = message ?? throw new ArgumentNullException(nameof(message));
				Subscriptions = new List<MarketDataMessage>();
			}
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<Helper.SubscriptionKey, SubscriptionInfo> _subscribers = new Dictionary<Helper.SubscriptionKey, SubscriptionInfo>();
		private readonly Dictionary<string, SubscriptionInfo> _newsSubscribers = new Dictionary<string, SubscriptionInfo>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, RefPair<PortfolioMessage, int>> _pfSubscribers = new Dictionary<string, RefPair<PortfolioMessage, int>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<long, SubscriptionInfo> _subscribersById = new Dictionary<long, SubscriptionInfo>();
		private readonly HashSet<long> _onlyHistorySubscriptions = new HashSet<long>();

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
		public bool IsRestoreOnReconnect { get; set; }

		/// <summary>
		/// Support multiple subscriptions with duplicate parameters.
		/// </summary>
		public bool SupportMultipleSubscriptions { get; set; }

		private void ClearSubscribers()
		{
			_subscribers.Clear();
			_newsSubscribers.Clear();
			_pfSubscribers.Clear();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
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
						if (!IsRestoreOnReconnect)
							ClearSubscribers();
					}

					base.SendInMessage(message);
					break;
				}

				case MessageTypes.Disconnect:
				{
					if (!IsRestoreOnReconnect)
					{
						var messages = new List<Message>();

						lock (_sync)
						{
							//if (_newsSubscribers.Count > 0)
							messages.AddRange(_newsSubscribers.Values.Select(p => p.Message));

							//if (_subscribers.Count > 0)
							messages.AddRange(_subscribers.Values.Select(p => p.Message));

							//if (_pfSubscribers.Count > 0)
							messages.AddRange(_pfSubscribers.Values.Select(p => p.First));
						
							ClearSubscribers();
						}

						foreach (var m in messages)
						{
							var msg = m.Clone();

							if (msg is MarketDataMessage mdMsg)
							{
								mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
								mdMsg.IsSubscribe = false;
							}
							else if (msg is PortfolioMessage pfMsg)
							{
								pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
								pfMsg.IsSubscribe = false;
							}

							base.SendInMessage(msg);
						}
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

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			List<Message> messages = null;
			List<Message> clones = null;

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var connectMsg = (ConnectMessage)message;

					if (connectMsg.Error == null && IsRestoreOnReconnect)
					{
						messages = new List<Message>();

						lock (_sync)
						{
							messages.AddRange(_subscribers.Values.Select(p => p.Message));
							messages.AddRange(_newsSubscribers.Values.Select(p => p.Message));
							messages.AddRange(_pfSubscribers.Values.Select(p => p.First));

							ClearSubscribers();
						}

						if (messages.Count == 0)
							messages = null;
					}

					break;
				}

				case ExtendedMessageTypes.RestoringSubscription:
				{
					if (IsRestoreOnReconnect)
					{
						messages = new List<Message>();

						lock (_sync)
						{
							messages.AddRange(_subscribers.Values.Select(p => p.Message));
							messages.AddRange(_newsSubscribers.Values.Select(p => p.Message));
							messages.AddRange(_pfSubscribers.Values.Select(p => p.First));

							ClearSubscribers();
						}

						if (messages.Count == 0)
							messages = null;
					}

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
				foreach (var m in messages)
				{
					var msg = m.Clone();

					msg.IsBack = true;
					msg.Adapter = this;

					if (msg is MarketDataMessage mdMsg)
					{
						mdMsg.TransactionId = TransactionIdGenerator.GetNextId();
					}
					else if (msg is PortfolioMessage pfMsg)
					{
						pfMsg.TransactionId = TransactionIdGenerator.GetNextId();
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
			var sendIn = false;
			var isOnlyHistory = false;
			MarketDataMessage sendOutMsg = null;
			SubscriptionInfo info;

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
			if (_onlyHistorySubscriptions.Remove(message.OriginalTransactionId))
				return false;

			IEnumerable<MarketDataMessage> replies;

			lock (_sync)
			{
				var info = _subscribersById.TryGetValue(message.OriginalTransactionId);

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

		private IEnumerable<MarketDataMessage> ProcessSubscriptionResult<T>(Dictionary<T, SubscriptionInfo> subscriptions, T key, SubscriptionInfo info, MarketDataMessage message)
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

		private SubscriptionInfo ProcessSubscription<T>(Dictionary<T, SubscriptionInfo> subscriptions, T key, MarketDataMessage message, ref bool sendIn, ref bool isOnlyHistory, ref MarketDataMessage sendOutMsg)
		{
			MarketDataMessage clone = null;
			var info = subscriptions.TryGetValue(key) ?? new SubscriptionInfo(clone = (MarketDataMessage)message.Clone());
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
			
			RefPair<PortfolioMessage, int> pair;

			lock (_sync)
			{
				pair = _pfSubscribers.TryGetValue(pfName) ?? RefTuple.Create((PortfolioMessage)message.Clone(), 0);

				var subscribersCount = pair.Second;

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

				if (subscribersCount > 0)
				{
					pair.Second = subscribersCount;
					_pfSubscribers[pfName] = pair;
				}
				else
					_pfSubscribers.Remove(pfName);
			}

			if (sendIn)
			{
				if (!message.IsSubscribe && message.OriginalTransactionId == 0)
					message.OriginalTransactionId = pair.First.TransactionId;

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
				IsRestoreOnReconnect = IsRestoreOnReconnect,
				SupportMultipleSubscriptions = SupportMultipleSubscriptions,
			};
		}
	}
}