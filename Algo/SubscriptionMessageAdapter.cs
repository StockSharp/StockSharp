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
		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<Tuple<MarketDataTypes, SecurityId, DateTimeOffset?, DateTimeOffset?, long?, int?>, RefPair<MarketDataMessage, int>> _subscribers = new Dictionary<Tuple<MarketDataTypes, SecurityId, DateTimeOffset?, DateTimeOffset?, long?, int?>, RefPair<MarketDataMessage, int>>();
		private readonly Dictionary<Tuple<MarketDataTypes, SecurityId, object>, RefPair<MarketDataMessage, int>> _candleSubscribers = new Dictionary<Tuple<MarketDataTypes, SecurityId, object>, RefPair<MarketDataMessage, int>>();
		private readonly Dictionary<string, RefPair<MarketDataMessage, int>> _newsSubscribers = new Dictionary<string, RefPair<MarketDataMessage, int>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, RefPair<PortfolioMessage, int>> _pfSubscribers = new Dictionary<string, RefPair<PortfolioMessage, int>>(StringComparer.InvariantCultureIgnoreCase);
		//private readonly Dictionary<Tuple<MarketDataTypes, SecurityId>, List<MarketDataMessage>> _pendingMessages = new Dictionary<Tuple<MarketDataTypes, SecurityId>, List<MarketDataMessage>>();

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

		private void ClearSubscribers()
		{
			_subscribers.Clear();
			_newsSubscribers.Clear();
			_pfSubscribers.Clear();
			_candleSubscribers.Clear();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_sync)
					{
						ClearSubscribers();
						//_pendingMessages.Clear();
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
							if (_newsSubscribers.Count > 0)
								messages.AddRange(_newsSubscribers.Values.Select(p => p.First));

							if (_subscribers.Count > 0)
								messages.AddRange(_subscribers.Values.Select(p => p.First));

							if (_candleSubscribers.Count > 0)
								messages.AddRange(_candleSubscribers.Values.Select(p => p.First));

							if (_pfSubscribers.Count > 0)
								messages.AddRange(_pfSubscribers.Values.Select(p => p.First));
						
							ClearSubscribers();
						}

						foreach (var m in messages)
						{
							var msg = m.Clone();
							var mdMsg = msg as MarketDataMessage;

							if (mdMsg != null)
							{
								mdMsg.TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId();
								mdMsg.IsSubscribe = false;
							}
							else
							{
								var pfMsg = (PortfolioMessage)msg;

								pfMsg.TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId();
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
				{
					var pfMsg = (PortfolioMessage)message;
					var sendIn = false;

					lock (_sync)
					{
						var pair = _pfSubscribers.TryGetValue(pfMsg.PortfolioName) ?? RefTuple.Create((PortfolioMessage)pfMsg.Clone(), 0);
						var subscribersCount = pair.Second;

						if (pfMsg.IsSubscribe)
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
							_pfSubscribers[pfMsg.PortfolioName] = pair;
						}
						else
							_pfSubscribers.Remove(pfMsg.PortfolioName);
					}
					
					if (sendIn)
						base.SendInMessage(message);

					break;
				}

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
			List<Message> messages = null;

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
							messages.AddRange(_subscribers.Values.Select(p => p.First));
							messages.AddRange(_newsSubscribers.Values.Select(p => p.First));
							messages.AddRange(_candleSubscribers.Values.Select(p => p.First));
							messages.AddRange(_pfSubscribers.Values.Select(p => p.First));

							ClearSubscribers();
						}

						if (messages.Count == 0)
							messages = null;
					}

					break;
				}
				// TODO
				//case MessageTypes.MarketData:
				//	ProcessOutMarketDataMessage((MarketDataMessage)message);
				//	break;
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (messages != null)
			{
				foreach (var m in messages)
				{
					var msg = m.Clone();

					msg.IsBack = true;
					msg.Adapter = this;

					var mdMsg = msg as MarketDataMessage;

					if (mdMsg != null)
					{
						mdMsg.TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId();
					}
					else
					{
						var pfMsg = (PortfolioMessage)msg;
						pfMsg.TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId();
					}

					base.OnInnerAdapterNewOutMessage(msg);
				}
			}
		}

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			var sendIn = false;
			MarketDataMessage sendOutMsg = null;

			lock (_sync)
			{
				var isSubscribe = message.IsSubscribe;

				switch (message.DataType)
				{
					case MarketDataTypes.News:
					{
						var subscriber = message.NewsId ?? string.Empty;

						var pair = _newsSubscribers.TryGetValue(subscriber) ?? RefTuple.Create((MarketDataMessage)message.Clone(), 0);
						var subscribersCount = pair.Second;

						if (isSubscribe)
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
							else
								sendOutMsg = NonExist(message);
						}

						if (sendOutMsg == null)
						{
							if (!sendIn)
							{
								sendOutMsg = new MarketDataMessage
								{
									DataType = message.DataType,
									IsSubscribe = isSubscribe,
									OriginalTransactionId = message.TransactionId,
								};
							}

							if (subscribersCount > 0)
							{
								pair.Second = subscribersCount;
								_newsSubscribers[subscriber] = pair;
							}
							else
								_newsSubscribers.Remove(subscriber);
						}

						break;
					}
					case MarketDataTypes.CandleTimeFrame:
					case MarketDataTypes.CandleRange:
					case MarketDataTypes.CandlePnF:
					case MarketDataTypes.CandleRenko:
					case MarketDataTypes.CandleTick:
					case MarketDataTypes.CandleVolume:
					{
						var key = Tuple.Create(message.DataType, message.SecurityId, message.Arg);

						var pair = _candleSubscribers.TryGetValue(key) ?? RefTuple.Create((MarketDataMessage)message.Clone(), 0);
						var subscribersCount = pair.Second;

						if (isSubscribe)
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
							else
								sendOutMsg = NonExist(message);
						}

						if (sendOutMsg == null)
						{
							if (!sendIn)
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
								pair.Second = subscribersCount;
								_candleSubscribers[key] = pair;
							}
							else
								_candleSubscribers.Remove(key);
						}

						break;
					}
					default:
					{
						var key = Tuple.Create(message.DataType, message.SecurityId, message.From, message.To, message.Count, message.MaxDepth);

						var pair = _subscribers.TryGetValue(key) ?? RefTuple.Create((MarketDataMessage)message.Clone(), 0);
						var subscribersCount = pair.Second;

						if (isSubscribe)
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
							else
								sendOutMsg = NonExist(message);
						}

						if (sendOutMsg == null)
						{
							if (!sendIn)
							{
								sendOutMsg = new MarketDataMessage
								{
									DataType = message.DataType,
									IsSubscribe = isSubscribe,
									SecurityId = message.SecurityId,
									OriginalTransactionId = message.TransactionId,
								};
							}

							if (subscribersCount > 0)
							{
								pair.Second = subscribersCount;
								_subscribers[key] = pair;
							}
							else
								_subscribers.Remove(key);
						}

						break;
					}
				}
			}

			if (sendIn)
				base.SendInMessage(message);

			if (sendOutMsg != null)
				RaiseNewOutMessage(sendOutMsg);
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

		/// <summary>
		/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionMessageAdapter(InnerAdapter);
		}
	}
}