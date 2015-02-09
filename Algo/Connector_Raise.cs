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
		/// Событие появления собственных новых сделок.
		/// </summary>
		public event Action<IEnumerable<MyTrade>> NewMyTrades;

		/// <summary>
		/// Событие появления всех новых сделок.
		/// </summary>
		public event Action<IEnumerable<Trade>> NewTrades;

		/// <summary>
		/// Событие появления новых заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> NewOrders;

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Событие изменения состояния заявок (снята, удовлетворена).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Событие изменения стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Событие появления новых инструментов.
		/// </summary>
		public event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Событие изменения параметров инструментов.
		/// </summary>
		public event Action<IEnumerable<Security>> SecuritiesChanged;

		/// <summary>
		/// Событие появления новых портфелей.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> NewPortfolios;

		/// <summary>
		/// Событие изменения параметров портфелей.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> PortfoliosChanged;

		/// <summary>
		/// Событие появления новых позиций.
		/// </summary>
		public event Action<IEnumerable<Position>> NewPositions;

		/// <summary>
		/// Событие изменения параметров позиций.
		/// </summary>
		public event Action<IEnumerable<Position>> PositionsChanged;

		/// <summary>
		/// Событие появления новых стаканов с котировками.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> NewMarketDepths;

		/// <summary>
		/// Событие изменения стаканов с котировками.
		/// </summary>
		public event Action<IEnumerable<MarketDepth>> MarketDepthsChanged;

		/// <summary>
		/// Событие появления новых записей в логе заявок.
		/// </summary>
		public event Action<IEnumerable<OrderLogItem>> NewOrderLogItems;

		/// <summary>
		/// Событие, сигнализирующее об изменении текущего времени на биржевых площадках <see cref="IConnector.ExchangeBoards"/>.
		/// Передается разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<TimeSpan> MarketTimeChanged;

		/// <summary>
		/// Событие появления новости.
		/// </summary>
		public event Action<News> NewNews;

		/// <summary>
		/// Событие изменения новости (например, при скачивании текста <see cref="StockSharp.BusinessEntities.News.Story"/>).
		/// </summary>
		public event Action<News> NewsChanged;

		/// <summary>
		/// Событие обработки нового сообщения <see cref="Message"/>.
		/// </summary>
		public event Action<Message, MessageDirections> NewMessage;

		/// <summary>
		/// Событие успешного подключения.
		/// </summary>
		public event Action Connected;

		/// <summary>
		/// Событие успешного отключения.
		/// </summary>
		public event Action Disconnected;

		/// <summary>
		/// Событие ошибки подключения (например, соединения было разорвано).
		/// </summary>
		public event Action<Exception> ConnectionError;

		/// <summary>
		/// Событие успешного запуска экспорта.
		/// </summary>
		public event Action ExportStarted;

		/// <summary>
		/// Событие успешной остановки экспорта.
		/// </summary>
		public event Action ExportStopped;

		/// <summary>
		/// Событие ошибки экспорта (например, соединения было разорвано).
		/// </summary>
		public event Action<Exception> ExportError;

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		public event Action<Exception> ProcessDataError;

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		public event Action NewDataExported;

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="IConnector.LookupSecurities(StockSharp.BusinessEntities.Security)"/>.
		/// </summary>
		public event Action<IEnumerable<Security>> LookupSecuritiesResult;

		/// <summary>
		/// Событие, передающее результат поиска, запущенного через метод <see cref="IConnector.LookupPortfolios"/>.
		/// </summary>
		public event Action<IEnumerable<Portfolio>> LookupPortfoliosResult;

		/// <summary>
		/// Событие успешной регистрации инструмента для получения маркет-данных.
		/// </summary>
		public event Action<Security, MarketDataTypes> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Событие ошибки регистрации инструмента для получения маркет-данных.
		/// </summary>
		public event Action<Security, MarketDataTypes, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Событие изменения состояния сессии для биржевой площадки.
		/// </summary>
		public event Action<ExchangeBoard, SessionStates> SessionStateChanged;

		/// <summary>
		/// Событие изменения инструмента.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		/// <summary>
		/// Вызвать событие <see cref="NewMyTrades"/>.
		/// </summary>
		/// <param name="trades">Мои сделки, которые нужно передать в событие.</param>
		private void RaiseNewMyTrades(IEnumerable<MyTrade> trades)
		{
			NewMyTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewTrades"/>.
		/// </summary>
		/// <param name="trades">Сделки, которые нужно передать в событие.</param>
		private void RaiseNewTrades(IEnumerable<Trade> trades)
		{
			NewTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewOrders"/>.
		/// </summary>
		/// <param name="orders">Заявки, которые нужно передать в событие.</param>
		private void RaiseNewOrders(IEnumerable<Order> orders)
		{
			NewOrders.SafeInvoke(orders);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewStopOrders"/>.
		/// </summary>
		/// <param name="stopOrders">Стоп-заявки, которые нужно передать в событие.</param>
		private void RaiseNewStopOrders(IEnumerable<Order> stopOrders)
		{
			NewStopOrders.SafeInvoke(stopOrders);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrdersChanged"/>.
		/// </summary>
		/// <param name="orders">Заявки, которые нужно передать в событие.</param>
		private void RaiseOrdersChanged(IEnumerable<Order> orders)
		{
			OrdersChanged.SafeInvoke(orders);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrdersChanged"/>.
		/// </summary>
		/// <param name="stopOrders">Стоп-заявки, которые нужно передать в событие.</param>
		private void RaiseStopOrdersChanged(IEnumerable<Order> stopOrders)
		{
			StopOrdersChanged.SafeInvoke(stopOrders);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrdersRegisterFailed"/>.
		/// </summary>
		/// <param name="fails">Информация об ошибках, которую нужно передать в событие.</param>
		private void RaiseOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			OrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Вызвать событие <see cref="OrdersCancelFailed"/>.
		/// </summary>
		/// <param name="fails">Информация об ошибках, которую нужно передать в событие.</param>
		private void RaiseOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			OrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrdersRegisterFailed"/>.
		/// </summary>
		/// <param name="fails">Информация об ошибках, которую нужно передать в событие.</param>
		private void RaiseStopOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Вызвать событие <see cref="StopOrdersCancelFailed"/>.
		/// </summary>
		/// <param name="fails">Информация об ошибках, которую нужно передать в событие.</param>
		private void RaiseStopOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewSecurities"/>.
		/// </summary>
		/// <param name="securities">Инструменты, которые нужно передать в событие.</param>
		private void RaiseNewSecurities(IEnumerable<Security> securities)
		{
			NewSecurities.SafeInvoke(securities);
		}

		/// <summary>
		/// Вызвать событие <see cref="SecuritiesChanged"/>.
		/// </summary>
		/// <param name="securities">Инструменты, которые нужно передать в событие.</param>
		private void RaiseSecuritiesChanged(IEnumerable<Security> securities)
		{
			SecuritiesChanged.SafeInvoke(securities);
		}

		/// <summary>
		/// Вызвать событие <see cref="SecuritiesChanged"/>.
		/// </summary>
		/// <param name="security">Инструмент, который нужно передать в событие.</param>
		private void RaiseSecurityChanged(Security security)
		{
			RaiseSecuritiesChanged(new[] { security });
		}

		/// <summary>
		/// Вызвать событие <see cref="NewPortfolios"/>.
		/// </summary>
		/// <param name="portfolios">Портфели, которые нужно передать в событие.</param>
		private void RaiseNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			NewPortfolios.SafeInvoke(portfolios);
		}

		/// <summary>
		/// Вызвать событие <see cref="PortfoliosChanged"/>.
		/// </summary>
		/// <param name="portfolios">Портфели, которые нужно передать в событие.</param>
		private void RaisePortfoliosChanged(IEnumerable<Portfolio> portfolios)
		{
			PortfoliosChanged.SafeInvoke(portfolios);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewPositions"/>.
		/// </summary>
		/// <param name="positions">Позиции, которые нужно передать в событие.</param>
		private void RaiseNewPositions(IEnumerable<Position> positions)
		{
			NewPositions.SafeInvoke(positions);
		}

		/// <summary>
		/// Вызвать событие <see cref="PositionsChanged"/>.
		/// </summary>
		/// <param name="positions">Позиции, которые нужно передать в событие.</param>
		private void RaisePositionsChanged(IEnumerable<Position> positions)
		{
			PositionsChanged.SafeInvoke(positions);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewMarketDepths"/>.
		/// </summary>
		/// <param name="marketDepths">Стаканы, которые нужно передать в событие.</param>
		private void RaiseNewMarketDepths(IEnumerable<MarketDepth> marketDepths)
		{
			NewMarketDepths.SafeInvoke(marketDepths);
		}

		/// <summary>
		/// Вызвать событие <see cref="MarketDepthsChanged"/>.
		/// </summary>
		/// <param name="marketDepths">Стаканы, которые нужно передать в событие.</param>
		private void RaiseMarketDepthsChanged(IEnumerable<MarketDepth> marketDepths)
		{
			MarketDepthsChanged.SafeInvoke(marketDepths);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewNews"/>.
		/// </summary>
		/// <param name="news">Новость.</param>
		private void RaiseNewNews(News news)
		{
			NewNews.SafeInvoke(news);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewsChanged"/>.
		/// </summary>
		/// <param name="news">Новость.</param>
		private void RaiseNewsChanged(News news)
		{
			NewsChanged.SafeInvoke(news);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewOrderLogItems"/>.
		/// </summary>
		/// <param name="items">Строчки лога заявок.</param>
		private void RaiseNewOrderLogItems(IEnumerable<OrderLogItem> items)
		{
			NewOrderLogItems.SafeInvoke(items);
		}

		/// <summary>
		/// Вызвать событие <see cref="Connected"/>.
		/// </summary>
		private void RaiseConnected()
		{
			ConnectionState = ConnectionStates.Connected;
			Connected.SafeInvoke();
		}

		/// <summary>
		/// Вызвать событие <see cref="Disconnected"/>.
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
		/// Вызвать событие <see cref="ConnectionError"/>.
		/// </summary>
		/// <param name="exception">Ошибка соединения.</param>
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
		/// Вызвать событие <see cref="ExportStarted"/>.
		/// </summary>
		protected void RaiseExportStarted()
		{
			_prevTime = default(DateTimeOffset);

			ExportState = ConnectionStates.Connected;
			ExportStarted.SafeInvoke();
		}

		/// <summary>
		/// Вызвать событие <see cref="ExportStopped"/>.
		/// </summary>
		protected void RaiseExportStopped()
		{
			ExportState = ConnectionStates.Disconnected;
			ExportStopped.SafeInvoke();
		}

		/// <summary>
		/// Вызвать событие <see cref="ExportError"/>.
		/// </summary>
		/// <param name="exception">Ошибка соединения.</param>
		private void RaiseExportError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			ExportState = ConnectionStates.Failed;
			ExportError.SafeInvoke(exception);

			this.AddErrorLog(exception);
		}

		/// <summary>
		/// Вызвать событие <see cref="ProcessDataError"/>.
		/// </summary>
		/// <param name="exception">Ошибка обработки данных.</param>
		protected void RaiseProcessDataError(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException("exception");

			DataErrorCount++;

			this.AddErrorLog(exception);
			ProcessDataError.SafeInvoke(exception);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewDataExported"/>.
		/// </summary>
		private void RaiseNewDataExported()
		{
			NewDataExported.SafeInvoke();
		}

		/// <summary>
		/// Вызвать событие <see cref="MarketTimeChanged"/>.
		/// </summary>
		/// <param name="diff">Разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.</param>
		private void RaiseMarketTimeChanged(TimeSpan diff)
		{
			MarketTimeChanged.SafeInvoke(diff);
		}

		/// <summary>
		/// Вызвать событие <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="securities">Найденные инструменты.</param>
		private void RaiseLookupSecuritiesResult(IEnumerable<Security> securities)
		{
			LookupSecuritiesResult.SafeInvoke(securities);
		}

		/// <summary>
		/// Вызвать событие <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="portfolios">Найденные портфели.</param>
		private void RaiseLookupPortfoliosResult(IEnumerable<Portfolio> portfolios)
		{
			LookupPortfoliosResult.SafeInvoke(portfolios);
		}

		private void RaiseMarketDataSubscriptionSucceeded(Security security, MarketDataMessage message)
		{
			var msg = LocalizedStrings.Str690Params.Put(security.Id, message.DataType);

			if (message.From != DateTimeOffset.MinValue && message.To != DateTimeOffset.MaxValue)
				msg += LocalizedStrings.Str691Params.Put(message.From, message.To);

			this.AddInfoLog(msg + ".");

			MarketDataSubscriptionSucceeded.SafeInvoke(security, message.DataType);
		}

		private void RaiseMarketDataSubscriptionFailed(Security security, MarketDataTypes dataType, Exception error)
		{
			this.AddErrorLog(LocalizedStrings.Str634Params, security.Id, dataType, error);
			MarketDataSubscriptionFailed.SafeInvoke(security, dataType, error);
		}

		/// <summary>
		/// Вызвать событие <see cref="NewMessage"/>.
		/// </summary>
		/// <param name="message">Новое сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		private void RaiseNewMessage(Message message, MessageDirections direction)
		{
			NewMessage.SafeInvoke(message, direction);
		}

		private void RaiseValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			ValuesChanged.SafeInvoke(security, changes, serverTime, localTime);
		}
	}
}