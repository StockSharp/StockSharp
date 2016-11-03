#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector_Subscription.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		private const MarketDataTypes _filteredMarketDepth = (MarketDataTypes)(-1);

		private sealed class SubscriptionManager
		{
			private readonly Connector _connector;

			private sealed class ContinuousInfo : Tuple<ContinuousSecurity, MarketDataMessage>
			{
				public ContinuousInfo(ContinuousSecurity security, MarketDataMessage message)
					: base(security, message)
				{
				}

				public TimeSpan Elapsed { get; set; }
			}

			public void ClearCache()
			{
				_unsubscribeActions.Clear();
				_subscribers.Clear();
				_continuousSecurities.Clear();
				_lookupMessages.Clear();
				_registeredFilteredMarketDepths.Clear();
			}

			private readonly SynchronizedDictionary<Security, SynchronizedSet<MarketDataTypes>> _unsubscribeActions = new SynchronizedDictionary<Security, SynchronizedSet<MarketDataTypes>>();
			private readonly SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<Security, int>> _subscribers = new SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<Security, int>>();
			private readonly SynchronizedLinkedList<ContinuousInfo> _continuousSecurities = new SynchronizedLinkedList<ContinuousInfo>();

			private readonly SynchronizedDictionary<long, Tuple<SecurityLookupMessage, MarketDataMessage>> _lookupMessages = new SynchronizedDictionary<long, Tuple<SecurityLookupMessage, MarketDataMessage>>();

			private readonly CachedSynchronizedDictionary<Security, int> _registeredFilteredMarketDepths = new CachedSynchronizedDictionary<Security, int>();

			public SubscriptionManager(Connector connector)
			{
				if (connector == null)
					throw new ArgumentNullException(nameof(connector));

				_connector = connector;

				_connector.MarketDataSubscriptionSucceeded += OnConnectorMarketDataSubscriptionSucceeded;
				_connector.MarketDataSubscriptionFailed += OnConnectorMarketDataSubscriptionFailed;

				_connector.NewMessage += OnConnectorNewMessage;
			}

			private void OnConnectorNewMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.SecurityLookupResult:
					{
						var id = ((SecurityLookupResultMessage)message).OriginalTransactionId;

						var tuple = _lookupMessages.TryGetValue(id);

						if (tuple != null)
						{
							var security = _connector.FilterSecurities(tuple.Item1).FirstOrDefault();
							
							ProcessSecurityMarketData(security, tuple.Item2);

							_lookupMessages.Remove(id);
						}

						break;
					}

					//TODO убрать подписку на MarketDataSubscriptionSucceeded
					//case MessageTypes.MarketData:
					//{
					//	if (direction == MessageDirections.ToMarketData)
					//		break;

					//	break;
					//}
				}
			}

			private void ProcessSecurityMarketData(Security security, MarketDataMessage message/*, Security subscriber*/)
			{
				//var message = new MarketDataMessage
				//{
				//	DataType = dataType,
				//	IsSubscribe = true,
				//	//SecurityId = _connector.GetSecurityId(subscriber),
				//	TransactionId = _connector.TransactionIdGenerator.GetNextId()
				//};

				//switch (dataType)
				//{
				//	case MarketDataTypes.Trades:
				//		message.Arg = ExecutionTypes.Tick;
				//		break;
				//	case MarketDataTypes.OrderLog:
				//		message.Arg = ExecutionTypes.OrderLog;
				//		break;
				//}

				//message.FillSecurityInfo(_connector, subscriber);

				if (security == null)
				{
					var reply = new MarketDataMessage
					{
						OriginalTransactionId = message.TransactionId,
						Error = new ArgumentException(LocalizedStrings.Str692Params.Put(message.SecurityId, _connector.Name))
					};
					_connector.SendOutMessage(reply);
				}
				else
				{
					_connector.SendInMessage(message);
				}
			}

			private void OnConnectorMarketDataSubscriptionSucceeded(Security security, MarketDataMessage message)
			{
				var types = _unsubscribeActions.TryGetValue(security);

				if (types == null)
					return;

				if (!types.Remove(message.DataType))
					return;

				var subscribers = GetSubscribers(message.DataType);

				if (TryUnSubscribe(subscribers, security))
					SendUnSubscribeMessage(/*security, */message);
			}

			private void OnConnectorMarketDataSubscriptionFailed(Security security, MarketDataMessage message)
			{
				var types = _unsubscribeActions.TryGetValue(security);

				if (types == null)
					return;

				types.Remove(message.DataType);
			}

			private CachedSynchronizedDictionary<Security, int> GetSubscribers(MarketDataTypes type)
			{
				return _subscribers.SafeAdd(type);
			}

			public IEnumerable<Security> RegisteredSecurities => GetSubscribers(MarketDataTypes.Level1).CachedKeys;

			public IEnumerable<Security> RegisteredMarketDepths => GetSubscribers(MarketDataTypes.MarketDepth).CachedKeys;

			public IEnumerable<Security> RegisteredTrades => GetSubscribers(MarketDataTypes.Trades).CachedKeys;

			public IEnumerable<Security> RegisteredOrderLogs => GetSubscribers(MarketDataTypes.OrderLog).CachedKeys;

			private readonly CachedSynchronizedDictionary<Portfolio, int> _registeredPortfolios = new CachedSynchronizedDictionary<Portfolio, int>();

			public IEnumerable<Portfolio> RegisteredPortfolios => _registeredPortfolios.CachedKeys;

			public void Subscribe(Security security, MarketDataMessage message)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				if (security is IndexSecurity)
					((IndexSecurity)security).InnerSecurities.ForEach(s => _connector.SubscribeMarketData(s, message));
				else if (security is ContinuousSecurity)
					SubscribeContinuous((ContinuousSecurity)security, message);
				else
					TrySubscribe(security, message);
			}

			public void UnSubscribe(Security security, MarketDataMessage message)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				if (security is IndexSecurity)
					((IndexSecurity)security).InnerSecurities.ForEach(s => _connector.UnSubscribeMarketData(s, message));
				else if (security is ContinuousSecurity)
					UnSubscribeContinuous((ContinuousSecurity)security, message);
				else
					TryUnSubscribe(security, message);
			}

			public void RegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio)
					((BasketPortfolio)portfolio).InnerPortfolios.ForEach(_connector.RegisterPortfolio);
				else if (TrySubscribe(_registeredPortfolios, portfolio))
					_connector.OnRegisterPortfolio(portfolio);
			}

			public void UnRegisterPortfolio(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (portfolio is BasketPortfolio)
					((BasketPortfolio)portfolio).InnerPortfolios.ForEach(_connector.UnRegisterPortfolio);
				else if (TryUnSubscribe(_registeredPortfolios, portfolio))
					_connector.OnUnRegisterPortfolio(portfolio);
			}

			public void RegisterFilteredMarketDepth(Security security)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				if (TrySubscribe(_registeredFilteredMarketDepths, security))
					_connector.OnRegisterFilteredMarketDepth(security);

				Subscribe(security, new MarketDataMessage
				{
					DataType = MarketDataTypes.MarketDepth,
					TransactionId = _connector.TransactionIdGenerator.GetNextId(),
					IsSubscribe = true,
				}.FillSecurityInfo(_connector, security));
			}

			public void UnRegisterFilteredMarketDepth(Security security)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				if (TryUnSubscribe(_registeredFilteredMarketDepths, security))
					_connector.OnUnRegisterFilteredMarketDepth(security);

				UnSubscribe(security, new MarketDataMessage
				{
					DataType = MarketDataTypes.MarketDepth,
					TransactionId = _connector.TransactionIdGenerator.GetNextId(),
					IsSubscribe = false,
				}.FillSecurityInfo(_connector, security));
			}

			public bool IsFilteredMarketDepthRegistered(Security security)
			{
				return _registeredFilteredMarketDepths.ContainsKey(security);
			}

			private bool _isNewsSubscribed;

			public void RegisterNews()
			{
				if (_isNewsSubscribed)
					return;

				_isNewsSubscribed = true;
				_connector.OnRegisterNews();
			}

			public void UnRegisterNews()
			{
				if (!_isNewsSubscribed)
					return;

				_isNewsSubscribed = false;
				_connector.OnUnRegisterNews();
			}

			private void SubscribeContinuous(ContinuousSecurity security, MarketDataMessage message)
			{
				lock (_continuousSecurities.SyncRoot)
				{
					var info = new ContinuousInfo(security, message);

					if (_continuousSecurities.Contains(info))
						return;

					_continuousSecurities.AddFirst(info);

					if (_continuousSecurities.Count == 1)
						_connector.MarketTimeChanged += ConnectorOnMarketTimeChanged;
				}
			}

			private void UnSubscribeContinuous(ContinuousSecurity security, MarketDataMessage message)
			{
				lock (_continuousSecurities.SyncRoot)
				{
					var node = _continuousSecurities.Find(new ContinuousInfo(security, message));

					if (node == null)
						return;

					var diff = node.Value.Elapsed;
					var curr = node;

					while (curr != null)
					{
						curr.Value.Elapsed += diff;
						curr = curr.Next;
					}

					_continuousSecurities.Remove(node);

					if (_continuousSecurities.Count == 0)
						_connector.MarketTimeChanged -= ConnectorOnMarketTimeChanged;
				}
			}

			private DateTime NextExpInUtc(LinkedListNode<ContinuousInfo> node)
			{
				if (node == null)
					throw new ArgumentNullException(nameof(node));

				var contSec = node.Value.Item1;
				var currSec = contSec.GetSecurity(_connector.CurrentTime);
				return contSec.ExpirationJumps[currSec].UtcDateTime;
			}

			private void ConnectorOnMarketTimeChanged(TimeSpan diff)
			{
				var first = _continuousSecurities.First;

				if (first == null)
					return;

				if (first.Value.Elapsed > diff)
					first.Value.Elapsed -= diff;
				else
				{
					var underlyingSecurities = new List<Tuple<ContinuousSecurity, MarketDataMessage, Security, Security>>();

					lock (_continuousSecurities.SyncRoot)
					{
						var curr = first;

						while (curr != null && curr.Value.Elapsed <= diff && diff > TimeSpan.Zero)
						{
							diff -= curr.Value.Elapsed;
							_continuousSecurities.Remove(curr);

							var currSec = curr.Value.Item1.GetSecurity(_connector.CurrentTime);

							if (currSec != null)
							{
								var jumpInUtc = NextExpInUtc(curr);

								var c = _continuousSecurities.First;

								while (c != null)
								{
									if (jumpInUtc < NextExpInUtc(c))
										break;

									c = c.Next;
								}

								if (c == null)
									_continuousSecurities.AddLast(curr);
								else
								{
									c.Value.Elapsed = NextExpInUtc(c) - jumpInUtc;
									_continuousSecurities.AddBefore(c, curr);
								}

								if (curr.Previous != null)
								{
									curr.Value.Elapsed = jumpInUtc - NextExpInUtc(curr.Previous);
								}
								else
								{
									curr.Value.Elapsed = jumpInUtc - _connector.CurrentTime.Convert(TimeZoneInfo.Utc);
								}

								underlyingSecurities.Add(Tuple.Create(curr.Value.Item1, curr.Value.Item2, curr.Value.Item1.ExpirationJumps.GetPrevSecurity(currSec), currSec));
							}
							else
							{
								underlyingSecurities.Add(Tuple.Create(curr.Value.Item1, curr.Value.Item2, curr.Value.Item1.ExpirationJumps.LastSecurity, (Security)null));
								UnSubscribeContinuous(curr.Value.Item1, curr.Value.Item2);
							}

							curr = _continuousSecurities.First;
						}
					}

					foreach (var tuple in underlyingSecurities)
					{
						if (tuple.Item3 != null)
							UnSubscribe(tuple.Item3, tuple.Item2);

						if (tuple.Item4 != null)
							Subscribe(tuple.Item4, tuple.Item2);
					}
				}
			}

			private static bool TrySubscribe<T>(CachedSynchronizedDictionary<T, int> subscribers, T subscriber)
			{
				return subscribers.ChangeSubscribers(subscriber, true) == 1;
			}

			private void TrySubscribe(Security subscriber, MarketDataMessage message)
			{
				//Если уже выполняется поиск данного инструмента, то нет необходимости в повторном вызове OnRegisterXXX.
				//Если на инструмент была подписка ранее, то просто вызываем событие SubscriptionSucceed.
				var subscribersCount = GetSubscribers(message.DataType).ChangeSubscribers(subscriber, true);

				//var securityId = _connector.GetSecurityId(subscriber);

				if (subscribersCount == 1)
				{
					var lookupMessage = new SecurityLookupMessage
					{
						SecurityId = message.SecurityId,
						SecurityType = message.SecurityType,
						TransactionId = _connector.TransactionIdGenerator.GetNextId()
					};

					_lookupMessages.Add(lookupMessage.TransactionId, Tuple.Create(lookupMessage, (MarketDataMessage)message.Clone()));
					_connector.LookupSecurities(lookupMessage);
				}
				else// if (subscribed == true)
				{
					var reply = new MarketDataMessage
					{
						OriginalTransactionId = message.TransactionId,
						IsSubscribe = true,
					};
					_connector.SendOutMessage(reply);
				}
			}

			private static bool TryUnSubscribe<T>(CachedSynchronizedDictionary<T, int> subscribers, T subscriber)
			{
				return subscribers.ChangeSubscribers(subscriber, false) == 0;
			}

			private void TryUnSubscribe(Security subscriber, MarketDataMessage message)
			{
				if (TryUnSubscribe(GetSubscribers(message.DataType), subscriber))
					SendUnSubscribeMessage(/*subscriber, */message);
			}

			private void SendUnSubscribeMessage(/*Security subscriber, */MarketDataMessage message)
			{
				//var msg = new MarketDataMessage
				//{
				//	DataType = type,
				//	IsSubscribe = false,
				//	TransactionId = _connector.TransactionIdGenerator.GetNextId(),
				//};

				//switch (type)
				//{
				//	case MarketDataTypes.Trades:
				//		msg.Arg = ExecutionTypes.Tick;
				//		break;
				//	case MarketDataTypes.OrderLog:
				//		msg.Arg = ExecutionTypes.OrderLog;
				//		break;
				//}

				//msg.FillSecurityInfo(_connector, subscriber);

				_connector.SendInMessage(message);
			}

			//public void ReStart()
			//{
			//	try
			//	{
			//		RegisteredSecurities.ForEach(s => _connector.OnUnRegisterSecurity(s));
			//		RegisteredMarketDepths.ForEach(s => _connector.OnUnRegisterMarketDepth(s));
			//		RegisteredOrderLogs.ForEach(s => _connector.OnUnRegisterOrderLog(s));
			//		RegisteredTrades.ForEach(s => _connector.OnUnRegisterTrades(s));
			//		RegisteredPortfolios.ForEach(s => _connector.OnUnRegisterPortfolio(s));

			//		if (_isNewsSubscribed)
			//			_connector.OnUnRegisterNews();
			//	}
			//	catch (Exception ex)
			//	{
			//		_connector.RaiseError(ex);
			//	}

			//	RegisteredSecurities.ForEach(s => _connector.OnRegisterSecurity(s));
			//	RegisteredMarketDepths.ForEach(s => _connector.OnRegisterMarketDepth(s));
			//	RegisteredOrderLogs.ForEach(s => _connector.OnRegisterOrderLog(s));
			//	RegisteredTrades.ForEach(s => _connector.OnRegisterTrades(s));
			//	RegisteredPortfolios.ForEach(s => _connector.OnRegisterPortfolio(s));

			//	if (_isNewsSubscribed)
			//		_connector.OnRegisterNews();
			//}

			public void Stop()
			{
				RegisteredSecurities.ForEach(_connector.UnRegisterSecurity);
				RegisteredMarketDepths.ForEach(_connector.UnRegisterMarketDepth);
				RegisteredOrderLogs.ForEach(_connector.UnRegisterOrderLog);
				RegisteredTrades.ForEach(_connector.UnRegisterTrades);
				RegisteredPortfolios.ForEach(_connector.UnRegisterPortfolio);

				UnRegisterNews();
			}
		}

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterSecurity"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredSecurities => _subscriptionManager.RegisteredSecurities;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterMarketDepth"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredMarketDepths => _subscriptionManager.RegisteredMarketDepths;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterTrades"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredTrades => _subscriptionManager.RegisteredTrades;

		/// <summary>
		/// List of all securities, subscribed via <see cref="RegisterOrderLog"/>.
		/// </summary>
		public IEnumerable<Security> RegisteredOrderLogs => _subscriptionManager.RegisteredOrderLogs;

		/// <summary>
		/// List of all portfolios, subscribed via <see cref="RegisterPortfolio"/>.
		/// </summary>
		public IEnumerable<Portfolio> RegisteredPortfolios => _subscriptionManager.RegisteredPortfolios;

		/// <summary>
		/// To sign up to get market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain subscribe info.</param>
		public virtual void SubscribeMarketData(Security security, MarketDataMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.TransactionId == 0)
				message.TransactionId = TransactionIdGenerator.GetNextId();

			message.FillSecurityInfo(this, security);

			_subscriptionManager.Subscribe(security, message);
		}

		/// <summary>
		/// To unsubscribe from getting market data by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		/// <param name="message">The message that contain unsubscribe info.</param>
		public virtual void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.TransactionId == 0)
				message.TransactionId = TransactionIdGenerator.GetNextId();

			message.FillSecurityInfo(this, security);

			_subscriptionManager.UnSubscribe(security, message);
		}

		private void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			SubscribeMarketData(security, new MarketDataMessage
			{
				DataType = type,
				IsSubscribe = true,
			});
		}

		private void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			SubscribeMarketData(security, new MarketDataMessage
			{
				DataType = type,
				IsSubscribe = false,
			});
		}

		/// <summary>
		/// To start getting new information (for example, <see cref="Security.LastTrade"/> or <see cref="Security.BestBid"/>) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be started.</param>
		public void RegisterSecurity(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <summary>
		/// To stop getting new information.
		/// </summary>
		/// <param name="security">The instrument by which new information getting should be stopped.</param>
		public void UnRegisterSecurity(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Level1);
		}

		/// <summary>
		/// To start getting quotes (order book) by the instrument. Quotes values are available through the event <see cref="Connector.MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		public void RegisterMarketDepth(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <summary>
		/// To stop getting quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterMarketDepth(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.MarketDepth);
		}

		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="IConnector.GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		public void RegisterFilteredMarketDepth(Security security)
		{
			_subscriptionManager.RegisterFilteredMarketDepth(security);
		}

		private void OnRegisterFilteredMarketDepth(Security security)
		{
			// при подписке на отфильтрованный стакан необходимо заполнить его
			// первоначальное состояние в пототке обработки всех остальных сообщений
			SendOutMessage(new MarketDataMessage
			{
				IsSubscribe = true,
				DataType = _filteredMarketDepth
			}.FillSecurityInfo(this, security));	
		}

		/// <summary>
		/// To stop getting filtered quotes by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be stopped.</param>
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			_subscriptionManager.UnRegisterFilteredMarketDepth(security);
		}

		private void OnUnRegisterFilteredMarketDepth(Security security)
		{
			SendOutMessage(new MarketDataMessage
			{
				IsSubscribe = false,
				DataType = _filteredMarketDepth
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// To start getting trades (tick data) by the instrument. New trades will come through the event <see cref="IConnector.NewTrades"/>.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be started.</param>
		public void RegisterTrades(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <summary>
		/// To stop getting trades (tick data) by the instrument.
		/// </summary>
		/// <param name="security">The instrument by which trades getting should be stopped.</param>
		public void UnRegisterTrades(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.Trades);
		}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		public void RegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.RegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		protected virtual void OnRegisterPortfolio(Portfolio portfolio)
		{
			SendInMessage(new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = true
			});
		}

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		public void UnRegisterPortfolio(Portfolio portfolio)
		{
			_subscriptionManager.UnRegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		protected virtual void OnUnRegisterPortfolio(Portfolio portfolio)
		{
			SendInMessage(new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = false
			});
		}

		/// <summary>
		/// Subscribe on order log for the security.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		public void RegisterOrderLog(Security security)
		{
			SubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <summary>
		/// Unsubscribe from order log for the security.
		/// </summary>
		/// <param name="security">Security for unsubscription.</param>
		public void UnRegisterOrderLog(Security security)
		{
			UnSubscribeMarketData(security, MarketDataTypes.OrderLog);
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		public void RegisterNews()
		{
			_subscriptionManager.RegisterNews();
		}

		/// <summary>
		/// Subscribe on news.
		/// </summary>
		protected virtual void OnRegisterNews()
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true
			});
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		public void UnRegisterNews()
		{
			_subscriptionManager.UnRegisterNews();
		}

		/// <summary>
		/// Request news <see cref="BusinessEntities.News.Story"/> body. After receiving the event <see cref="Connector.NewsChanged"/> will be triggered.
		/// </summary>
		/// <param name="news">News.</param>
		public virtual void RequestNewsStory(News news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true,
				NewsId = news.Id.To<string>(),
			});
		}

		/// <summary>
		/// Unsubscribe from news.
		/// </summary>
		protected virtual void OnUnRegisterNews()
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = false
			});
		}
	}
}