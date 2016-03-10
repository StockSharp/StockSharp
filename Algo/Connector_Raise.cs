#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector_Raise.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		/// <summary>
		/// Own trade received.
		/// </summary>
		public event Action<MyTrade> NewMyTrade;

		/// <summary>
		/// Own trades received.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Tick trade received.
		/// </summary>
		public event Action<Trade> NewTrade;

		/// <summary>
		/// Tick trades received.
		/// </summary>
		public event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Order received.
		/// </summary>
		public event Action<Order> NewOrder;

		/// <summary>
		/// Orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Order changed (cancelled, matched).
		/// </summary>
		public event Action<Order> OrderChanged;

		/// <summary>
		/// Stop-orders received.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Orders changed (cancelled, matched).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Order registration error event.
		/// </summary>
		public event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// Order cancellation error event.
		/// </summary>
		public event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// Stop-orders changed.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Security received.
		/// </summary>
		public event Action<Security> NewSecurity;

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
		/// Security changed.
		/// </summary>
		public event Action<Security> SecurityChanged;

		/// <summary>
		/// Securities changed.
		/// </summary>
		public event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// New portfolio received.
		/// </summary>
		public event Action<Portfolio> NewPortfolio;

		/// <summary>
		/// Portfolios received.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Portfolio changed.
		/// </summary>
		public event Action<Portfolio> PortfolioChanged;

		/// <summary>
		/// Portfolios changed.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Position received.
		/// </summary>
		public event Action<Position> NewPosition;

		/// <summary>
		/// Positions received.
		/// </summary>
		public event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Position changed.
		/// </summary>
		public event Action<Position> PositionChanged;

		/// <summary>
		/// Positions changed.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Order book received.
		/// </summary>
		public event Action<MarketDepth> NewMarketDepth;

		/// <summary>
		/// Order book changed.
		/// </summary>
		public event Action<MarketDepth> MarketDepthChanged;

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
		public event Action<OrderLogItem> NewOrderLogItem;

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
		/// Connected.
		/// </summary>
		public event Action<IMessageAdapter> ConnectedEx;

		/// <summary>
		/// Disconnected.
		/// </summary>
		public event Action<IMessageAdapter> DisconnectedEx;

		/// <summary>
		/// Connection error (for example, the connection was aborted by server).
		/// </summary>
		public event Action<IMessageAdapter, Exception> ConnectionErrorEx;

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
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <summary>
		/// Connection restored.
		/// </summary>
		public event Action Restored;

		/// <summary>
		/// Connection timed-out.
		/// </summary>
		public event Action TimeOut;

		private void RaiseNewMyTrade(MyTrade trade)
		{
			NewMyTrade?.Invoke(trade);
			NewMyTrades?.Invoke(new[] { trade });
		}

		private void RaiseNewTrade(Trade trade)
		{
			NewTrade?.Invoke(trade);
			NewTrades?.Invoke(new[] { trade });
		}

		private void RaiseNewOrder(Order order)
		{
			NewOrder?.Invoke(order);
			NewOrders?.Invoke(new[] { order });
		}

		private void RaiseOrderChanged(Order order)
		{
			OrderChanged?.Invoke(order);
			OrdersChanged?.Invoke(new[] { order });
		}

		/// <summary>
		/// To call the event <see cref="NewStopOrders"/>.
		/// </summary>
		/// <param name="stopOrders">Stop orders that should be passed to the event.</param>
		private void RaiseNewStopOrders(IEnumerable<Order> stopOrders)
		{
			NewStopOrders?.Invoke(stopOrders);
		}

		/// <summary>
		/// To call the event <see cref="StopOrdersChanged"/>.
		/// </summary>
		/// <param name="stopOrders">Stop orders that should be passed to the event.</param>
		private void RaiseStopOrdersChanged(IEnumerable<Order> stopOrders)
		{
			StopOrdersChanged?.Invoke(stopOrders);
		}

		private void RaiseOrderRegisterFailed(OrderFail fail)
		{
			OrderRegisterFailed?.Invoke(fail);
			OrdersRegisterFailed?.Invoke(new[] { fail });
		}

		private void RaiseOrderCancelFailed(OrderFail fail)
		{
			OrderCancelFailed?.Invoke(fail);
			OrdersCancelFailed?.Invoke(new[] { fail });
		}

		/// <summary>
		/// To call the event <see cref="StopOrdersRegisterFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseStopOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersRegisterFailed?.Invoke(fails);
		}

		/// <summary>
		/// To call the event <see cref="StopOrdersCancelFailed"/>.
		/// </summary>
		/// <param name="fails">Error information that should be passed to the event.</param>
		private void RaiseStopOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersCancelFailed?.Invoke(fails);
		}

		private void RaiseNewSecurity(Security security)
		{
			var arr = new[] { security };

            _added?.Invoke(arr);

			NewSecurity?.Invoke(security);
			NewSecurities?.Invoke(arr);
		}

		private void RaiseSecuritiesChanged(Security[] securities)
		{
			SecuritiesChanged?.Invoke(securities);

			var evt = SecurityChanged;

			if (evt == null)
				return;

			foreach (var security in securities)
				evt(security);
		}

		private void RaiseSecurityChanged(Security security)
		{
			SecurityChanged?.Invoke(security);
			SecuritiesChanged?.Invoke(new[] { security });
		}

		private void RaiseNewPortfolio(Portfolio portfolio)
		{
			NewPortfolio?.Invoke(portfolio);
			NewPortfolios?.Invoke(new[] { portfolio });
		}

		private void RaisePortfolioChanged(Portfolio portfolio)
		{
			PortfolioChanged?.Invoke(portfolio);
			PortfoliosChanged?.Invoke(new[] { portfolio });
		}

		private void RaiseNewPosition(Position position)
		{
			NewPosition?.Invoke(position);
			NewPositions?.Invoke(new[] { position });
		}

		private void RaisePositionChanged(Position position)
		{
			PositionChanged?.Invoke(position);
			PositionsChanged?.Invoke(new[] { position });
		}

		private void RaiseNewMarketDepth(MarketDepth marketDepth)
		{
			NewMarketDepth?.Invoke(marketDepth);
			NewMarketDepths?.Invoke(new[] { marketDepth });
		}

		private void RaiseMarketDepthChanged(MarketDepth marketDepth)
		{
			MarketDepthChanged?.Invoke(marketDepth);
			MarketDepthsChanged?.Invoke(new[] { marketDepth });
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewNews"/>.
		/// </summary>
		/// <param name="news">News.</param>
		private void RaiseNewNews(News news)
		{
			NewNews?.Invoke(news);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewsChanged"/>.
		/// </summary>
		/// <param name="news">News.</param>
		private void RaiseNewsChanged(News news)
		{
			NewsChanged?.Invoke(news);
		}

		private void RaiseNewOrderLogItem(OrderLogItem item)
		{
			NewOrderLogItem?.Invoke(item);
			NewOrderLogItems?.Invoke(new[] { item });
		}

		/// <summary>
		/// To call the event <see cref="Connected"/>.
		/// </summary>
		private void RaiseConnected()
		{
			ConnectionState = ConnectionStates.Connected;
			Connected?.Invoke();
		}

		/// <summary>
		/// To call the event <see cref="ConnectedEx"/>.
		/// </summary>
		/// <param name="adapter">Adapter, initiated event.</param>
		private void RaiseConnectedEx(IMessageAdapter adapter)
		{
			ConnectedEx?.Invoke(adapter);
		}

		/// <summary>
		/// To call the event <see cref="Disconnected"/>.
		/// </summary>
		private void RaiseDisconnected()
		{
			ConnectionState = ConnectionStates.Disconnected;
			Disconnected?.Invoke();
		}

		/// <summary>
		/// To call the event <see cref="DisconnectedEx"/>.
		/// </summary>
		/// <param name="adapter">Adapter, initiated event.</param>
		private void RaiseDisconnectedEx(IMessageAdapter adapter)
		{
			DisconnectedEx?.Invoke(adapter);
		}

		/// <summary>
		/// To call the event <see cref="ConnectionError"/>.
		/// </summary>
		/// <param name="exception">Error connection.</param>
		private void RaiseConnectionError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			ConnectionState = ConnectionStates.Failed;
			ConnectionError?.Invoke(exception);

			this.AddErrorLog(exception);
		}

		/// <summary>
		/// To call the event <see cref="ConnectionErrorEx"/>.
		/// </summary>
		/// <param name="adapter">Adapter, initiated event.</param>
		/// <param name="exception">Error connection.</param>
		private void RaiseConnectionErrorEx(IMessageAdapter adapter, Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			ConnectionErrorEx?.Invoke(adapter, exception);
		}

		/// <summary>
		/// To call the event <see cref="Connector.Error"/>.
		/// </summary>
		/// <param name="exception">Data processing error.</param>
		private void RaiseError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			ErrorCount++;

			this.AddErrorLog(exception);
			Error?.Invoke(exception);
		}

		/// <summary>
		/// To call the event <see cref="Connector.MarketTimeChanged"/>.
		/// </summary>
		/// <param name="diff">The difference in the time since the last call of the event. The first time the event passes the <see cref="TimeSpan.Zero"/> value.</param>
		private void RaiseMarketTimeChanged(TimeSpan diff)
		{
			MarketTimeChanged?.Invoke(diff);
		}

		/// <summary>
		/// To call the event <see cref="Connector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="securities">Found instruments.</param>
		private void RaiseLookupSecuritiesResult(IEnumerable<Security> securities)
		{
			LookupSecuritiesResult?.Invoke(securities);
		}

		/// <summary>
		/// To call the event <see cref="Connector.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="portfolios">Found portfolios.</param>
		private void RaiseLookupPortfoliosResult(IEnumerable<Portfolio> portfolios)
		{
			LookupPortfoliosResult?.Invoke(portfolios);
		}

		private void RaiseMarketDataSubscriptionSucceeded(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.Str690Params.Put(security.Id, message.DataType);

			if (message.From != null && message.To != null)
				msg += LocalizedStrings.Str691Params.Put(message.From.Value, message.To.Value);

			this.AddInfoLog(msg + ".");

			MarketDataSubscriptionSucceeded?.Invoke(security, message.DataType);
		}

		private void RaiseMarketDataSubscriptionFailed(Security security, MarketDataTypes dataType, Exception error)
		{
			this.AddErrorLog(LocalizedStrings.Str634Params, security.Id, dataType, error);
			MarketDataSubscriptionFailed?.Invoke(security, dataType, error);
		}

		/// <summary>
		/// To call the event <see cref="Connector.NewMessage"/>.
		/// </summary>
		/// <param name="message">A new message.</param>
		private void RaiseNewMessage(Message message)
		{
			NewMessage?.Invoke(message);
		}

		private void RaiseValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			ValuesChanged?.Invoke(security, changes, serverTime, localTime);
		}

		private void RaiseRestored()
		{
			Restored?.Invoke();
		}

		private void RaiseTimeOut()
		{
			TimeOut?.Invoke();
		}
	}
}