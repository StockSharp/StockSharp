namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Обертка над подключение <see cref="IConnector"/> для получения маркет-данных в реальном времени.
	/// </summary>
	/// <typeparam name="TConnector">Тип подключения.</typeparam>
	public class MarketDataConnector<TConnector> : Disposable, ISecurityDownloader
		where TConnector : Connector
	{
		private sealed class MarketDataEntityFactory : Algo.EntityFactory
		{
			private readonly ISecurityProvider _securityProvider;

			public MarketDataEntityFactory(ISecurityProvider securityProvider)
			{
				if (securityProvider == null)
					throw new ArgumentNullException("securityProvider");

				_securityProvider = securityProvider;
			}

			public override Security CreateSecurity(string id)
			{
				return _securityProvider.LookupById(id) ?? base.CreateSecurity(id);
			}
		}

		private readonly Func<TConnector> _createConnector;
		//private readonly bool _isSupportLookupSecurities;
		private readonly MarketDataBuffer<Trade> _tradesBuffer = new MarketDataBuffer<Trade>();
		private readonly MarketDataBuffer<MarketDepth> _depthsBuffer = new MarketDataBuffer<MarketDepth>();
		private readonly MarketDataBuffer<OrderLogItem> _orderLogBuffer = new MarketDataBuffer<OrderLogItem>();
		private readonly MarketDataBuffer<Level1ChangeMessage> _level1Buffer = new MarketDataBuffer<Level1ChangeMessage>();
		private readonly MarketDataBuffer<Candle> _candleBuffer = new MarketDataBuffer<Candle>();
		private readonly MarketDataBuffer<ExecutionMessage> _executionBuffer = new MarketDataBuffer<ExecutionMessage>();
		private readonly SynchronizedSet<News> _newsBuffer = new SynchronizedSet<News>(); 

		private readonly ISecurityProvider _securityProvider;
		private readonly ConnectorHydraTask<TConnector> _task;

		private bool _exportStarted;
		private Security _criteria;
		private Action<Security> _newSecurity;
		private bool _isRefreshed;
		private readonly SyncObject _refreshSync = new SyncObject();
		private bool _wasConnected;

		/// <summary>
		/// Создать <see cref="MarketDataConnector{TTrader}"/>.
		/// </summary>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="task">Задача.</param>
		/// <param name="createConnector">Обработчик, создающий подключение к торговой системе.</param>
		public MarketDataConnector(ISecurityProvider securityProvider, ConnectorHydraTask<TConnector> task, Func<TConnector> createConnector)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

			if (task == null)
				throw new ArgumentNullException("task");

			if (createConnector == null)
				throw new ArgumentNullException("createConnector");

			_securityProvider = securityProvider;
			_createConnector = createConnector;
			//_isSupportLookupSecurities = isSupportLookupSecurities;
			_task = task;
		}

		/// <summary>
		/// Ошибка в подключении.
		/// </summary>
		public event Action<Exception> TraderError;

		private TConnector _connector;

		/// <summary>
		/// Подключение к торговой системе.
		/// </summary>
		public TConnector Connector
		{
			get
			{
				if (_connector == null)
					throw new InvalidOperationException(LocalizedStrings.Str1360);

				return _connector;
			}
			private set
			{
				if (_connector != null)
				{
					UnInitializeConnector();
				}

				_connector = value;

				if (_connector != null)
				{
					InitializeConnector();
				}
			}
		}

		/// <summary>
		/// Инициализировать подключение.
		/// </summary>
		protected virtual void InitializeConnector()
		{
			_connector.DoIf<IConnector, Connector>(bt => bt.EntityFactory = new MarketDataEntityFactory(_securityProvider));

			_connector.Error += OnError;
			_connector.Connected += OnConnected;
			_connector.ConnectionError += OnConnectionError;
			_connector.NewTrades += OnNewTrades;
			_connector.MarketDepthsChanged += OnMarketDepthsChanged;
			_connector.NewOrderLogItems += OnNewOrderLogItems;
			_connector.ValuesChanged += OnValuesChanged;
			_connector.NewSecurities += OnNewSecurities;
			_connector.NewNews += OnNewNews;
			_connector.NewsChanged += OnNewsChanged;

			_connector.NewOrders += OnOrdersChanged;
			_connector.OrdersChanged += OnOrdersChanged;
			_connector.OrdersRegisterFailed += OnOrdersFailed;
			_connector.OrdersCancelFailed += OnOrdersFailed;
			_connector.NewStopOrders += OnOrdersChanged;
			_connector.StopOrdersChanged += OnOrdersChanged;
			_connector.StopOrdersRegisterFailed += OnOrdersFailed;
			_connector.StopOrdersCancelFailed += OnOrdersFailed;
			_connector.NewMyTrades += OnNewMyTrades; 

			var source = _connector as IExternalCandleSource;
			if (source != null)
				source.NewCandles += OnNewCandles;
		}

		/// <summary>
		/// Деинициализировать подключение.
		/// </summary>
		protected virtual void UnInitializeConnector()
		{
			var source = _connector as IExternalCandleSource;
			if (source != null)
				source.NewCandles -= OnNewCandles;

			_connector.Error -= OnError;
			_connector.Connected -= OnConnected;
			_connector.ConnectionError -= OnConnectionError;
			_connector.NewTrades -= OnNewTrades;
			_connector.MarketDepthsChanged -= OnMarketDepthsChanged;
			_connector.NewOrderLogItems -= OnNewOrderLogItems;
			_connector.ValuesChanged -= OnValuesChanged;
			_connector.NewSecurities -= OnNewSecurities;
			_connector.NewNews -= OnNewNews;
			_connector.NewsChanged -= OnNewsChanged;

			_connector.NewOrders -= OnOrdersChanged;
			_connector.OrdersChanged -= OnOrdersChanged;
			_connector.OrdersRegisterFailed -= OnOrdersFailed;
			_connector.OrdersCancelFailed -= OnOrdersFailed;
			_connector.NewStopOrders -= OnOrdersChanged;
			_connector.StopOrdersChanged -= OnOrdersChanged;
			_connector.StopOrdersRegisterFailed -= OnOrdersFailed;
			_connector.StopOrdersCancelFailed -= OnOrdersFailed;
			_connector.NewMyTrades -= OnNewMyTrades; 

			_connector.Dispose();

			_task.UnInitTrader(_connector);
		}

		/// <summary>
		/// Информация о последней ошибке.
		/// </summary>
		public Exception LastError { get; set; }

		/// <summary>
		/// Событие запуска экспорта маркет-данных. Вызвается только после первого запуска экспорта.
		/// </summary>
		public event Action ExportStarted;

		/// <summary>
		/// Получить накопленные стаканы.
		/// </summary>
		/// <returns>Накопленные стаканы.</returns>
		public IDictionary<Security, IEnumerable<MarketDepth>> GetMarketDepths()
		{
			ThrowIfError();
			return _depthsBuffer.Get();
		}

		/// <summary>
		/// Получить накопленные тиковые сделки.
		/// </summary>
		/// <returns>Накопленные сделки.</returns>
		public IDictionary<Security, IEnumerable<Trade>> GetTrades()
		{
			ThrowIfError();
			return _tradesBuffer.Get();
		}

		/// <summary>
		/// Получить накопленный лог заявок.
		/// </summary>
		/// <returns>Накопленный лог заявок.</returns>
		public IDictionary<Security, IEnumerable<OrderLogItem>> GetOrderLog()
		{
			ThrowIfError();
			return _orderLogBuffer.Get();
		}

		/// <summary>
		/// Получить накопленные изменения по инструменту.
		/// </summary>
		/// <returns>Накопленные изменения по инструменту.</returns>
		public IDictionary<Security, IEnumerable<Level1ChangeMessage>> GetLevel1Messages()
		{
			ThrowIfError();
			return _level1Buffer.Get();
		}

		/// <summary>
		/// Получить накопленные свечи.
		/// </summary>
		/// <returns>Накопленные свечи.</returns>
		public IDictionary<Security, IEnumerable<Candle>> GetCandles()
		{
			ThrowIfError();
			return _candleBuffer.Get();
		}

		/// <summary>
		/// Получить накопленные новости.
		/// </summary>
		/// <returns>Накопленные новости.</returns>
		public IEnumerable<News> GetNews()
		{
			ThrowIfError();
			return _newsBuffer.SyncGet(c => c.CopyAndClear());
		}

		/// <summary>
		/// Получить накопленные заявки и сделки по инструменту.
		/// </summary>
		/// <returns>Накопленные заявки и сделки по инструменту.</returns>
		public IDictionary<Security, IEnumerable<ExecutionMessage>> GetExecutionMessages()
		{
			ThrowIfError();
			return _executionBuffer.Get();
		}

		private void ThrowIfError()
		{
			if (LastError != null)
			{
				var copy = LastError;
				LastError = null;
				throw copy;
			}
		}

		private void OnConnected()
		{
			if (_criteria == null)
			{
				if (_exportStarted)
					return;

				_exportStarted = true;
				ExportStarted.SafeInvoke();

				var connectorSettings = _task.Settings as ConnectorHydraTaskSettings;

				if (connectorSettings != null && connectorSettings.IsDownloadNews)
					Connector.RegisterNews();
			}
			else
			{
				ProcessLookupSecurities(_criteria, _newSecurity, _wasConnected);
				_criteria = null;
				_isRefreshed = false;
			}
		}

		private void OnConnectionError(Exception error)
		{
			Stop();

			_task.AddErrorLog(error);
		}

		private void OnNewTrades(IEnumerable<Trade> trades)
		{
			trades.ForEach(AddTrade);
		}

		private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			depths.ForEach(AddMarketDepth);
		}

		private void OnNewOrderLogItems(IEnumerable<OrderLogItem> items)
		{
			items.ForEach(AddOrderLog);
		}

		private void OnNewSecurities(IEnumerable<Security> securities)
		{
			NewSecurities.SafeInvoke(securities);
		}

		private void OnNewsChanged(News news)
		{
			_newsBuffer.Add(news);
		}

		private void OnNewNews(News news)
		{
			_newsBuffer.Add(news);
		}

		private void OnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = serverTime,
				LocalTime = localTime,
			};

			msg.Changes.AddRange(changes);

			AddLevel1Change(security, msg);
		}

		private void OnNewCandles(CandleSeries series, IEnumerable<Candle> candles)
		{
			_candleBuffer.Add(series.Security, candles);
		}

		private void OnOrdersChanged(IEnumerable<Order> orders)
		{
			foreach (var order in orders)
			{
				_executionBuffer.Add(order.Security, order.ToMessage());
			}
		}

		private void OnOrdersFailed(IEnumerable<OrderFail> fails)
		{
			foreach (var fail in fails)
			{
				_executionBuffer.Add(fail.Order.Security, fail.ToMessage());
			}
		}

		private void OnNewMyTrades(IEnumerable<MyTrade> trades)
		{
			foreach (var trade in trades)
			{
				_executionBuffer.Add(trade.Order.Security, trade.ToMessage());
			}
		}

		/// <summary>
		/// Добавить новую сделку.
		/// </summary>
		/// <param name="trade">Новая сделка.</param>
		protected virtual void AddTrade(Trade trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			_tradesBuffer.Add(trade.Security, trade);
		}

		/// <summary>
		/// Добавить новый стакан.
		/// </summary>
		/// <param name="depth">Новый стакан.</param>
		protected virtual void AddMarketDepth(MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			//Trace.WriteLine("MDA " +dc.Security+":"+dc._DebugId+":"+ dc.LastChangeTime.ToString("HHmmss.fff"));
			if (depth.Bids.Length > 0 || depth.Asks.Length > 0)
			{
				_depthsBuffer.Add(depth.Security, depth.Clone());
			}
		}

		/// <summary>
		/// Добавить новую лог-заявку.
		/// </summary>
		/// <param name="item">Новая лог-заявка.</param>
		protected virtual void AddOrderLog(OrderLogItem item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			_orderLogBuffer.Add(item.Order.Security, item);
		}

		/// <summary>
		/// Добавить первый уровень маркет-данных.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="message">Первый уровень маркет-данных.</param>
		protected virtual void AddLevel1Change(Security security, Level1ChangeMessage message)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

			message = (Level1ChangeMessage)message.Clone();

			foreach (var change in message.Changes.ToArray())
			{
				if (!_task.Settings.SupportedLevel1Fields.Contains(change.Key))
					message.Changes.Remove(change.Key);
			}

			if (message.Changes.Count > 0)
				_level1Buffer.Add(security, message);
		}

		private void OnError(Exception error)
		{
			LastError = error;
			TraderError.SafeInvoke(error);
			_task.AddErrorLog(error);
		}

		/// <summary>
		/// Запустить накопление маркет-данных.
		/// </summary>
		public void Start()
		{
			Connector = _createConnector();

			if (!_task.IsExecLogEnabled() && Connector.TransactionAdapter != null)
			{
				if (Connector.TransactionAdapter != Connector.MarketDataAdapter)
					Connector.Adapter.InnerAdapters.Remove(Connector.TransactionAdapter);
				else
				{
					var adapter = Connector.TransactionAdapter;

					adapter.RemoveSupportedMessage(MessageTypes.OrderCancel);
					adapter.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);
					adapter.RemoveSupportedMessage(MessageTypes.OrderPairReplace);
					adapter.RemoveSupportedMessage(MessageTypes.OrderRegister);
					adapter.RemoveSupportedMessage(MessageTypes.OrderReplace);
					adapter.RemoveSupportedMessage(MessageTypes.OrderStatus);
					adapter.RemoveSupportedMessage(MessageTypes.Portfolio);
					adapter.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
					adapter.RemoveSupportedMessage(MessageTypes.Position);
				}
			}

			Connector.DoIf<IConnector, Connector>(t =>
			{
				var connectorSettings = _task.Settings as ConnectorHydraTaskSettings;

				var settings = t.ReConnectionSettings;

				if (connectorSettings == null)
				{
					settings.AttemptCount = -1;
					settings.ReAttemptCount = -1;
				}
				else
				{
					settings.Load(connectorSettings.ReConnectionSettings.Save());

					t.LogLevel = _task.LogLevel;
				}

				_task.InitTrader(t);
			});

			Connector.Connect();
		}

		/// <summary>
		/// Остановить накопление маркет-данных.
		/// </summary>
		public void Stop()
		{
			Connector = null;
			LastError = null;

			_criteria = null;
			//_isRefreshed = false;
			_wasConnected = false;
			_exportStarted = false;

			lock (_refreshSync)
			{
				_isRefreshed = true;
				_refreshSync.Pulse();
			}
		}

		/// <summary>
		/// Закачать новые инструменты.
		/// </summary>
		/// <param name="storage">Хранилище информации об инструментах.</param>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <param name="newSecurity">Обработчик, через который будет передан новый инструмент.</param>
		/// <param name="isCancelled">Обработчик, возвращающий признак отмены поиска.</param>
		public void Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			lock (_refreshSync)
				_isRefreshed = false;

			if (_connector == null)
			{
				_criteria = criteria;
				_newSecurity = newSecurity;
				_wasConnected = false;
				Start();

				lock (_refreshSync)
				{
					if (!_isRefreshed)
						_refreshSync.Wait(TimeSpan.FromMinutes(1));
				}
			}
			else
				ProcessLookupSecurities(criteria, newSecurity, true);
		}

		private void ProcessLookupSecurities(Security criteria, Action<Security> newSecurity, bool wasConnected)
		{
			Action<IEnumerable<Security>> lookupSecuritiesResultHandler = securities =>
			{
				securities.ForEach(newSecurity);

				lock (_refreshSync)
				{
					_isRefreshed = true;
					_refreshSync.Pulse();
				}

				if (!wasConnected)
					Stop();
			};

			Connector.LookupSecuritiesResult += lookupSecuritiesResultHandler;

			try
			{
				Connector.LookupSecurities(criteria);
			}
			catch
			{
				Connector.LookupSecuritiesResult -= lookupSecuritiesResultHandler;

				if (!wasConnected)
					Connector = null;

				throw;
			}
		}

		/// <summary>
		/// Событие получения инструментов.
		/// </summary>
		public event Action<IEnumerable<Security>> NewSecurities;

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Stop();
			base.DisposeManaged();
		}
	}
}
