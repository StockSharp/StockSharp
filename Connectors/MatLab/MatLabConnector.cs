namespace StockSharp.MatLab
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Подключение, предоставляющий возможность использовать из MatLab скриптов подключения <see cref="IConnector"/>.
	/// </summary>
	public class MatLabConnector : Disposable
	{
		private readonly bool _ownTrader;

		/// <summary>
		/// Создать <see cref="MatLabConnector"/>.
		/// </summary>
		/// <param name="realConnector">Подключение, через которое будут отправляться заявки и получатся маркет-данные.</param>
		public MatLabConnector(IConnector realConnector)
			: this(realConnector, true)
		{
		}

		/// <summary>
		/// Создать <see cref="MatLabConnector"/>.
		/// </summary>
		/// <param name="realConnector">Подключение, через которое будут отправляться заявки и получатся маркет-данные.</param>
		/// <param name="ownTrader">Контролировать время жизни подключения <paramref name="realConnector"/>.</param>
		public MatLabConnector(IConnector realConnector, bool ownTrader)
		{
			if (realConnector == null)
				throw new ArgumentNullException("realConnector");

			RealConnector = realConnector;

			RealConnector.Connected += RealTraderOnConnected;
			RealConnector.ConnectionError += RealTraderOnConnectionError;
			RealConnector.Disconnected += RealTraderOnDisconnected;
			RealConnector.ProcessDataError += RealTraderOnProcessDataError;
			RealConnector.NewDataExported += RealTraderOnNewDataExported;
			RealConnector.MarketTimeChanged += RealTraderOnMarketTimeChanged;
			RealConnector.NewSecurities += RealTraderOnNewSecurities;
			RealConnector.SecuritiesChanged += RealTraderOnSecuritiesChanged;
			RealConnector.NewPortfolios += RealTraderOnNewPortfolios;
			RealConnector.PortfoliosChanged += RealTraderOnPortfoliosChanged;
			RealConnector.NewPositions += RealTraderOnNewPositions;
			RealConnector.PositionsChanged += RealTraderOnPositionsChanged;
			RealConnector.NewTrades += RealTraderOnNewTrades;
			RealConnector.NewMyTrades += RealTraderOnNewMyTrades;
			RealConnector.NewMarketDepths += RealTraderOnNewMarketDepths;
			RealConnector.MarketDepthsChanged += RealTraderOnMarketDepthsChanged;
			RealConnector.NewOrders += RealTraderOnNewOrders;
			RealConnector.OrdersChanged += RealTraderOnOrdersChanged;
			RealConnector.OrdersRegisterFailed += RealTraderOnOrdersRegisterFailed;
			RealConnector.OrdersCancelFailed += RealTraderOnOrdersCancelFailed;
			RealConnector.NewStopOrders += RealTraderOnNewStopOrders;
			RealConnector.StopOrdersChanged += RealTraderOnStopOrdersChanged;
			RealConnector.StopOrdersRegisterFailed += RealTraderOnStopOrdersRegisterFailed;
			RealConnector.StopOrdersCancelFailed += RealTraderOnStopOrdersCancelFailed;
			RealConnector.NewOrderLogItems += RealTraderOnNewOrderLogItems;

			_ownTrader = ownTrader;
		}

		/// <summary>
		/// Подключение, через которое будут отправляться заявки и получатся маркет-данные.
		/// </summary>
		public IConnector RealConnector { get; private set; }

		/// <summary>
		/// Событие успешного подключения.
		/// </summary>
		public event EventHandler Connected;

		/// <summary>
		/// Событие ошибки подключения (например, соединения было разорвано).
		/// </summary>
		public event EventHandler<ErrorEventArgs> ConnectionError;

		/// <summary>
		/// Событие успешного отключения.
		/// </summary>
		public event EventHandler Disconnected;

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		public event EventHandler<ErrorEventArgs> ProcessDataError;

		/// <summary>
		/// Событие, сигнализирующее о новых экспортируемых данных.
		/// </summary>
		public event EventHandler NewDataExported;

		/// <summary>
		/// Событие, сигнализирующее об изменении текущего времени на площадках <see cref="IConnector.ExchangeBoards"/>.
		/// Передается разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event EventHandler MarketTimeChanged;

		/// <summary>
		/// Событие появления новых инструментов.
		/// </summary>
		public event EventHandler<SecuritiesEventArgs> NewSecurities;

		/// <summary>
		/// Событие изменения параметров инструментов.
		/// </summary>
		public event EventHandler<SecuritiesEventArgs> SecuritiesChanged;

		/// <summary>
		/// Событие появления новых портфелей.
		/// </summary>
		public event EventHandler<PortfoliosEventArgs> NewPortfolios;

		/// <summary>
		/// Событие изменения параметров портфелей.
		/// </summary>
		public event EventHandler<PortfoliosEventArgs> PortfoliosChanged;

		/// <summary>
		/// Событие появления новых позиций.
		/// </summary>
		public event EventHandler<PositionsEventArgs> NewPositions;

		/// <summary>
		/// Событие изменения параметров позиций.
		/// </summary>
		public event EventHandler<PositionsEventArgs> PositionsChanged;

		/// <summary>
		/// Событие появления всех новых сделок.
		/// </summary>
		public event EventHandler<TradesEventArgs> NewTrades;

		/// <summary>
		/// Событие появления собственных новых сделок.
		/// </summary>
		public event EventHandler<MyTradesEventArgs> NewMyTrades;

		/// <summary>
		/// Событие появления новых заявок.
		/// </summary>
		public event EventHandler<OrdersEventArgs> NewOrders;

		/// <summary>
		/// Событие изменения состояния заявок (снята, удовлетворена).
		/// </summary>
		public event EventHandler<OrdersEventArgs> OrdersChanged;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией заявок.
		/// </summary>
		public event EventHandler<OrderFailsEventArgs> OrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием заявок.
		/// </summary>
		public event EventHandler<OrderFailsEventArgs> OrdersCancelFailed;

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		public event EventHandler<OrdersEventArgs> NewStopOrders;

		/// <summary>
		/// Событие изменения состояния стоп-заявок.
		/// </summary>
		public event EventHandler<OrdersEventArgs> StopOrdersChanged;

		/// <summary>
		/// Событие об ошибках, связанных с регистрацией стоп-заявок.
		/// </summary>
		public event EventHandler<OrderFailsEventArgs> StopOrdersRegisterFailed;

		/// <summary>
		/// Событие об ошибках, связанных со снятием стоп-заявок.
		/// </summary>
		public event EventHandler<OrderFailsEventArgs> StopOrdersCancelFailed;

		/// <summary>
		/// Событие появления новых стаканов с котировками.
		/// </summary>
		public event EventHandler<MarketDepthsEventArgs> NewMarketDepths;

		/// <summary>
		/// Событие изменения стаканов с котировками.
		/// </summary>
		public event EventHandler<MarketDepthsEventArgs> MarketDepthsChanged;

		/// <summary>
		/// Событие появления новых записей в логе заявок.
		/// </summary>
		public event EventHandler<OrderLogItemsEventArg> NewOrderLogItems;

		private void RealTraderOnNewMyTrades(IEnumerable<MyTrade> trades)
		{
			NewMyTrades.SafeInvoke(this, new MyTradesEventArgs(trades));
		}

		private void RealTraderOnProcessDataError(Exception exception)
		{
			ProcessDataError.SafeInvoke(this, new ErrorEventArgs(exception));
		}

		private void RealTraderOnStopOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersCancelFailed.SafeInvoke(this, new OrderFailsEventArgs(fails));
		}

		private void RealTraderOnStopOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			StopOrdersRegisterFailed.SafeInvoke(this, new OrderFailsEventArgs(fails));
		}

		private void RealTraderOnStopOrdersChanged(IEnumerable<Order> orders)
		{
			StopOrdersChanged.SafeInvoke(this, new OrdersEventArgs(orders));
		}

		private void RealTraderOnNewStopOrders(IEnumerable<Order> orders)
		{
			NewStopOrders.SafeInvoke(this, new OrdersEventArgs(orders));
		}

		private void RealTraderOnOrdersCancelFailed(IEnumerable<OrderFail> fails)
		{
			OrdersCancelFailed.SafeInvoke(this, new OrderFailsEventArgs(fails));
		}

		private void RealTraderOnOrdersRegisterFailed(IEnumerable<OrderFail> fails)
		{
			OrdersRegisterFailed.SafeInvoke(this, new OrderFailsEventArgs(fails));
		}

		private void RealTraderOnOrdersChanged(IEnumerable<Order> orders)
		{
			OrdersChanged.SafeInvoke(this, new OrdersEventArgs(orders));
		}

		private void RealTraderOnNewOrders(IEnumerable<Order> orders)
		{
			NewOrders.SafeInvoke(this, new OrdersEventArgs(orders));
		}

		private void RealTraderOnNewMarketDepths(IEnumerable<MarketDepth> depths)
		{
			NewMarketDepths.SafeInvoke(this, new MarketDepthsEventArgs(depths));
		}

		private void RealTraderOnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			MarketDepthsChanged.SafeInvoke(this, new MarketDepthsEventArgs(depths));
		}

		private void RealTraderOnNewTrades(IEnumerable<Trade> trades)
		{
			NewTrades.SafeInvoke(this, new TradesEventArgs(trades));
		}

		private void RealTraderOnPositionsChanged(IEnumerable<Position> positions)
		{
			PositionsChanged.SafeInvoke(this, new PositionsEventArgs(positions));
		}

		private void RealTraderOnNewPositions(IEnumerable<Position> positions)
		{
			NewPositions.SafeInvoke(this, new PositionsEventArgs(positions));
		}

		private void RealTraderOnPortfoliosChanged(IEnumerable<Portfolio> portfolios)
		{
			PortfoliosChanged.SafeInvoke(this, new PortfoliosEventArgs(portfolios));
		}

		private void RealTraderOnNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			NewPortfolios.SafeInvoke(this, new PortfoliosEventArgs(portfolios));
		}

		private void RealTraderOnSecuritiesChanged(IEnumerable<Security> securities)
		{
			SecuritiesChanged.SafeInvoke(this, new SecuritiesEventArgs(securities));
		}

		private void RealTraderOnNewSecurities(IEnumerable<Security> securities)
		{
			NewSecurities.SafeInvoke(this, new SecuritiesEventArgs(securities));
		}

		private void RealTraderOnNewOrderLogItems(IEnumerable<OrderLogItem> items)
		{
			NewOrderLogItems.SafeInvoke(this, new OrderLogItemsEventArg(items));
		}

		private void RealTraderOnMarketTimeChanged(TimeSpan diff)
		{
			MarketTimeChanged.Cast().SafeInvoke(this);
		}

		private void RealTraderOnNewDataExported()
		{
			NewDataExported.Cast().SafeInvoke(this);
		}

		private void RealTraderOnDisconnected()
		{
			Disconnected.Cast().SafeInvoke(this);
		}

		private void RealTraderOnConnectionError(Exception exception)
		{
			ConnectionError.SafeInvoke(this, new ErrorEventArgs(exception));
		}

		private void RealTraderOnConnected()
		{
			Connected.Cast().SafeInvoke(this);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			RealConnector.Connected -= RealTraderOnConnected;
			RealConnector.ConnectionError -= RealTraderOnConnectionError;
			RealConnector.Disconnected -= RealTraderOnDisconnected;
			RealConnector.ProcessDataError -= RealTraderOnProcessDataError;
			RealConnector.NewDataExported -= RealTraderOnNewDataExported;
			RealConnector.MarketTimeChanged -= RealTraderOnMarketTimeChanged;
			RealConnector.NewSecurities -= RealTraderOnNewSecurities;
			RealConnector.SecuritiesChanged -= RealTraderOnSecuritiesChanged;
			RealConnector.NewPortfolios -= RealTraderOnNewPortfolios;
			RealConnector.PortfoliosChanged -= RealTraderOnPortfoliosChanged;
			RealConnector.NewPositions -= RealTraderOnNewPositions;
			RealConnector.PositionsChanged -= RealTraderOnPositionsChanged;
			RealConnector.NewTrades -= RealTraderOnNewTrades;
			RealConnector.NewMyTrades -= RealTraderOnNewMyTrades;
			RealConnector.MarketDepthsChanged -= RealTraderOnMarketDepthsChanged;
			RealConnector.NewOrders -= RealTraderOnNewOrders;
			RealConnector.OrdersChanged -= RealTraderOnOrdersChanged;
			RealConnector.OrdersRegisterFailed -= RealTraderOnOrdersRegisterFailed;
			RealConnector.OrdersCancelFailed -= RealTraderOnOrdersCancelFailed;
			RealConnector.NewStopOrders -= RealTraderOnNewStopOrders;
			RealConnector.StopOrdersChanged -= RealTraderOnStopOrdersChanged;
			RealConnector.StopOrdersRegisterFailed -= RealTraderOnStopOrdersRegisterFailed;
			RealConnector.StopOrdersCancelFailed -= RealTraderOnStopOrdersCancelFailed;

			if (_ownTrader)
				RealConnector.Dispose();

			base.DisposeManaged();
		}
	}
}