namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The class to create connections to trading systems.
	/// </summary>
	partial class Connector
	{
		/// <summary>
		/// Own trades received.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Tick tades received.
		/// </summary>
		public event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Stop-orders changed.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Order registration errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Order cancellation errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Stop-order registration errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Stop-order cancellation errors event.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Securities received.
		/// </summary>
		public event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Securities changed.
		/// </summary>
		public event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Portfolios received.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Positions received.
		/// </summary>
		public event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Positions changed.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Order books received.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		/// <summary>
		/// Order books changed.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		/// <summary>
		/// Order log received.
		/// </summary>
		public event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		/// <summary>
		/// Server time changed <see cref="IConnector.ExchangeBoards"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<TimeSpan> MarketTimeChanged;

		/// <summary>
		/// News received.
		/// </summary>
		public event Action<News> NewNews;

		/// <summary>
		/// News updated (news body received <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		public event Action<News> NewsChanged;

		/// <summary>
		/// Message processed <see cref="Message"/>.
		/// </summary>
		public event Action<Message> NewMessage;

		/// <summary>
		/// Connected.
		/// </summary>
		public event Action Connected;

		/// <summary>
		/// Disconnected.
		/// </summary>
		public event Action Disconnected;

		/// <summary>
		/// Connection error (for example, the connection was aborted by server).
		/// </summary>
		public event Action<Exception> ConnectionError;

		/// <summary>
		/// Dats process error.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// Lookup result <see cref="IConnector.LookupSecurities(StockSharp.BusinessEntities.Security)"/> received.
		/// </summary>
		public event Action<IEnumerable<Security>> LookupSecuritiesResult;

		/// <summary>
		/// Lookup result <see cref="IConnector.LookupPortfolios"/> received.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> LookupPortfoliosResult;

		/// <summary>
		/// Successful subscription market-data.
		/// </summary>
		public event Action<Security, MarketDataTypes> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		public event Action<Security, MarketDataTypes, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Session changed.
		/// </summary>
		public event Action<ExchangeBoard, SessionStates> SessionStateChanged;

		/// <summary>
		/// Security changed.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		/// <summary>
		/// Connection restored.
		/// </summary>
		public event Action Restored;

		/// <summary>
		/// Connection timed-out.
		/// </summary>
		public event Action TimeOut;

		/// <summary>
		/// To call the event <see cref="Connector.NewMyTrades"/>.
		/// </summary>
		/// <param name="trades">My trades to be passed to the event.</param>
		private void RaiseNewMyTrades(IEnumerable<MyTrade> trades)
		{
			NewMyTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewTrades"/>.
		/// </summary>
		/// <param name="trades">Trades that should be passed to the event.</param>
		private void RaiseNewTrades(IEnumerable<Trade> trades)
		{
			NewTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewOrders"/>.
		/// </summary>
		/// <param name="orders">Orders that should be passed to the event.</param>
		private void RaiseNewOrders(IEnumerable<Order> orders)
		{
			NewOrders.SafeInvoke(orders);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewStopOrders"/>.
		/// </summary>
		/// <param name="stopOrders">Stop orders that should be passed to the event.</param>
		private void RaiseNewStopOrders(IEnumerable<Order> stopOrders)
		{
			NewStopOrders.SafeInvoke(stopOrders);
		}

		/// <summary>
		/// To call the event <see cref="Connector.OrdersChanged"/>.
		/// </summary>
		/// <param name="orders">Orders that should be passed to the event.</param>
		private void RaiseOrdersChanged(IEnumerable<Order> orders)
		{
			OrdersChanged.SafeInvoke(orders);
		}

		/// <summary>
		/// To call the event <see cref="Connector.StopOrdersChanged"/>.
		/// </summary>
		/// <param name="stopOrders">Stop orders that should be passed to the event.</param>
		private void RaiseStopOrdersChanged(IEnumerable<Order> stopOrders)
		{
			StopOrdersChanged.SafeInvoke(stopOrders);
		}

		/// <summary>
		/// To call the event <see cref="Connector.OrdersRegisterFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			OrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// To call the event <see cref="Connector.OrdersCancelFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			OrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// To call the event <see cref="Connector.StopOrdersRegisterFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseStopOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// To call the event <see cref="Connector.StopOrdersCancelFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseStopOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewSecurities"/>.
		/// </summary>
		/// <param name="securities">Instruments that should be passed to the event.</param>
		private void RaiseNewSecurities(Security[] securities)
		{
			NewSecurities.SafeInvoke(securities);

			foreach (var security in securities)
			{
				_added.SafeInvoke(security);
			}
		}

		/// <summary>
		/// To call the event <see cref="Connector.SecuritiesChanged"/>.
		/// </summary>
		/// <param name="securities">Instruments that should be passed to the event.</param>
		private void RaiseSecuritiesChanged(IEnumerable<Security> securities)
		{
			SecuritiesChanged.SafeInvoke(securities);
		}

		/// <summary>
		/// To call the event <see cref="Connector.SecuritiesChanged"/>.
		/// </summary>
		/// <param name="security">Instrument that should be passed to the event.</param>
		private void RaiseSecurityChanged(Security security)
		{
			RaiseSecuritiesChanged(new[] { security });
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewPortfolios"/>.
		/// </summary>
		/// <param name="portfolios">Portfolios that should be passed to the event.</param>
		private void RaiseNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			NewPortfolios.SafeInvoke(portfolios);
		}

		/// <summary>
		/// To call the event <see cref="Connector.PortfoliosChanged"/>.
		/// </summary>
		/// <param name="portfolios">Portfolios that should be passed to the event.</param>
		private void RaisePortfoliosChanged(IEnumerable<Portfolio> portfolios)
		{
			PortfoliosChanged.SafeInvoke(portfolios);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewPositions"/>.
		/// </summary>
		/// <param name="positions">Positions that should be passed to the event.</param>
		private void RaiseNewPositions(IEnumerable<Position> positions)
		{
			NewPositions.SafeInvoke(positions);
		}

		/// <summary>
		/// To call the event <see cref="Connector.PositionsChanged"/>.
		/// </summary>
		/// <param name="positions">Positions that should be passed to the event.</param>
		private void RaisePositionsChanged(IEnumerable<Position> positions)
		{
			PositionsChanged.SafeInvoke(positions);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewMarketDepths"/>.
		/// </summary>
		/// <param name="marketDepths">Order books that should be passed to the event.</param>
		private void RaiseNewMarketDepths(IEnumerable<MarketDepth> marketDepths)
		{
			NewMarketDepths.SafeInvoke(marketDepths);
		}

		/// <summary>
		/// To call the event <see cref="Connector.MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="marketDepths">Order books that should be passed to the event.</param>
		private void RaiseMarketDepthsChanged(IEnumerable<MarketDepth> marketDepths)
		{
			MarketDepthsChanged.SafeInvoke(marketDepths);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewNews"/>.
		/// </summary>
		/// <param name="news">News.</param>
		private void RaiseNewNews(News news)
		{
			NewNews.SafeInvoke(news);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewsChanged"/>.
		/// </summary>
		/// <param name="news">News.</param>
		private void RaiseNewsChanged(News news)
		{
			NewsChanged.SafeInvoke(news);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewOrderLogItems"/>.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		private void RaiseNewOrderLogItems(IEnumerable<OrderLogItem> items)
		{
			NewOrderLogItems.SafeInvoke(items);
		}

		/// <summary>
		/// To call the event <see cref="Connector.Connected"/>.
		/// </summary>
		private void RaiseConnected()
		{
			ConnectionState = ConnectionStates.Connected;
			Connected.SafeInvoke();
		}

		/// <summary>
		/// To call the event <see cref="Connector.Disconnected"/>.
		/// </summary>
		private void RaiseDisconnected()
		{
			// адаптеры маркет-данных сами должны оповещать коннектор
			//if (!IsMarketDataIndependent)
			//	RaiseExportStopped();

			ConnectionState = ConnectionStates.Disconnected;
			Disconnected.SafeInvoke();
		}

		/// <summary>
		/// To call the event <see cref="Connector.ConnectionError"/>.
		/// </summary>
		/// <param name="exception">Error connection.</param>
		private void RaiseConnectionError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			// адаптеры маркет-данных сами должны оповещать коннектор
			//if (!IsMarketDataIndependent)
			//	RaiseExportError(exception);

			ConnectionState = ConnectionStates.Failed;
			ConnectionError.SafeInvoke(exception);

			this.AddErrorLog(exception);
		}

		/// <summary>
		/// To call the event <see cref="Connector.Error"/>.
		/// </summary>
		/// <param name="exception">Data processing error.</param>
		private void RaiseError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			ErrorCount++;

			this.AddErrorLog(exception);
			Error.SafeInvoke(exception);
		}

		/// <summary>
		/// To call the event <see cref="Connector.MarketTimeChanged"/>.
		/// </summary>
		/// <param name="diff">The difference in the time since the last call of the event. The first time the event passes the <see cref="TimeSpan.Zero"/> value.</param>
		private void RaiseMarketTimeChanged(TimeSpan diff)
		{
			MarketTimeChanged.SafeInvoke(diff);
		}

		/// <summary>
		/// To call the event <see cref="Connector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="securities">Found instruments.</param>
		private void RaiseLookupSecuritiesResult(IEnumerable<Security> securities)
		{
			LookupSecuritiesResult.SafeInvoke(securities);
		}

		/// <summary>
		/// To call the event <see cref="Connector.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="portfolios">Found portfolios.</param>
		private void RaiseLookupPortfoliosResult(IEnumerable<Portfolio> portfolios)
		{
			LookupPortfoliosResult.SafeInvoke(portfolios);
		}

		private void RaiseMarketDataSubscriptionSucceeded(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.Str690Params.Put(security.Id, message.DataType);

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddInfoLog(msg + ".");

			MarketDataSubscriptionSucceeded.SafeInvoke(security, message.DataType);
		}

		private void RaiseMarketDataSubscriptionFailed(Security security, MarketDataTypes dataType, Exception error)
		{
			this.AddErrorLog(LocalizedStrings.Str634Params, security.Id, dataType, error);
			MarketDataSubscriptionFailed.SafeInvoke(security, dataType, error);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewMessage"/>.
		/// </summary>
		/// <param name="message">A new message.</param>
		private void RaiseNewMessage(Message message)
		{
			NewMessage.SafeInvoke(message);
		}

		private void RaiseValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			ValuesChanged.SafeInvoke(security, changes, serverTime, localTime);
		}

		private void RaiseRestored()
		{
			Restored.SafeInvoke();
		}

		private void RaiseTimeOut()
		{
			TimeOut.SafeInvoke();
		}
	}
}