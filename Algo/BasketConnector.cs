namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using SubscriptionAction = System.Action<BusinessEntities.IConnector, BusinessEntities.Security>;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий список подключений к торговым системам, с которыми оперирует агрегатор.
	/// </summary>
	public interface IInnerConnectorList : INotifyList<IConnector>, ISynchronizedCollection<IConnector>
	{
		/// <summary>
		/// Внутренние подключения, отсортированные по скорости работы.
		/// </summary>
		IEnumerable<IConnector> SortedConnectors { get; }

		/// <summary>
		/// Индексатор, через который задаются приоритеты скорости на внутренние подключения (чем меньше значение, те быстрее подключение).
		/// </summary>
		/// <param name="connector">Внутреннее подключение.</param>
		/// <returns>Приоритет подключения. Если задается значение -1, то подключение считается выключенным.</returns>
		int this[IConnector connector] { get; set; }

		/// <summary>
		/// Найти внутренниее подключение, которому принадлежит портфель <paramref name="portfolioName"/>.
		/// </summary>
		/// <param name="portfolioName">Название портфеля.</param>
		/// <returns>Найденное подключение. Если подключение для переданного портфеля не найден, то будет возвращено <see langword="null"/>.</returns>
		IConnector GetConnector(string portfolioName);
	}

	/// <summary>
	/// Базовый интерфейс подключения-агрегатора, позволяющего оперировать одновременно несколькими подключениям, подключенными к разным торговым системам.
	/// </summary>
	public interface IBasketConnector : IConnector
	{
		/// <summary>
		/// Подключения к торговым системам, с которыми оперирует агрегатор.
		/// </summary>
		IInnerConnectorList InnerConnectors { get; }
	}

	/// <summary>
	/// Подключение-агрегатор, позволяющий оперировать одновременно несколькими подключениям, подключенных к разным торговым системам.
	/// </summary>
	[Obsolete("BasketConnector устарел, необходимо использовать связку Connector и BasketMessageAdapter.")]
	public class BasketConnector : BaseLogReceiver, IBasketConnector
	{
		private sealed class InnerTraderList : CachedSynchronizedList<IConnector>, IInnerConnectorList
		{
			private readonly Dictionary<Portfolio, IConnector> _portfolioConnectors = new Dictionary<Portfolio, IConnector>();
 
			private readonly BasketConnector _parent;
			
			public InnerTraderList(BasketConnector parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			public IConnector GetConnector(string portfolioName)
			{
				lock (SyncRoot)
					return SortedConnectors.FirstOrDefault(t => t.Portfolios.Any(p => p.Name.CompareIgnoreCase(portfolioName)));
			}

			public IConnector SafeGetConnector(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException("portfolio");

				lock (SyncRoot)
				{
					return _portfolioConnectors.SafeAdd(portfolio, pf =>
					{
						var connector = GetConnector(pf.Name);

						if (connector == null)
							throw new InvalidOperationException(LocalizedStrings.Str1108Params.Put(pf.Name));

						return connector;
					});
				}
			}

			public IEnumerable<IConnector> SortedConnectors
			{
				get { return Cache.Where(t => this[t] != -1).OrderBy(t => this[t]); }
			}

			public IEnumerable<Trade> Trades
			{
				get
				{
					return Cache.SelectMany(t => t.Trades);
				}
			}

			public IEnumerable<ExchangeBoard> ExchangeBoards
			{
				get
				{
					return Cache.SelectMany(t => t.ExchangeBoards);
				}
			}

			public IEnumerable<Security> Securities
			{
				get
				{
					return Cache.SelectMany(t => t.Securities);
				}
			}

			public IEnumerable<Portfolio> Portfolios
			{
				get
				{
					return Cache.SelectMany(t => t.Portfolios);
				}
			}

			protected override bool OnAdding(IConnector item)
			{
				Subscribe(item);
				return base.OnAdding(item);
			}

			protected override void OnAdded(IConnector item)
			{
				base.OnAdded(item);
				ProcessConnectorValues(item);
			}

			protected override bool OnInserting(int index, IConnector item)
			{
				Subscribe(item);
				return base.OnInserting(index, item);
			}

			protected override void OnInserted(int index, IConnector item)
			{
				base.OnInserted(index, item);
				ProcessConnectorValues(item);
			}

			protected override bool OnRemoving(IConnector item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				lock (SyncRoot)
					ForEach(UnSubscribe);

				return base.OnClearing();
			}

			private void Subscribe(IConnector connector, int priority = 0)
			{
				if (connector == null)
					throw new ArgumentNullException("connector");

				connector.DoIf<IConnector, BaseLogSource>(bt => bt.Parent = _parent);

				connector.NewPortfolios += portfolios => _parent.OnInnerConnectorNewPortfolios(connector, portfolios);
				connector.PortfoliosChanged += portfolios => _parent.OnInnerConnectorPortfoliosChanged(connector, portfolios);
				connector.NewPositions += positions => _parent.OnInnerConnectorNewPositions(connector, positions);
				connector.PositionsChanged += positions => _parent.OnInnerConnectorPositionsChanged(connector, positions);
				connector.NewSecurities += securities => _parent.OnInnerConnectorNewSecurities(connector, securities);
				connector.SecuritiesChanged += securities => _parent.OnInnerConnectorSecuritiesChanged(connector, securities);
				connector.NewTrades += trades => _parent.OnInnerConnectorNewTrades(connector, trades);
				connector.NewMyTrades += trades => _parent.OnInnerConnectorNewMyTrades(connector, trades);
				connector.NewOrders += orders => _parent.OnInnerConnectorNewOrders(connector, orders);
				connector.OrdersChanged += orders => _parent.OnInnerConnectorOrdersChanged(connector, orders);
				connector.OrdersRegisterFailed += fails => _parent.OnInnerConnectorOrdersRegisterFailed(connector, fails);
				connector.OrdersCancelFailed += fails => _parent.OnInnerConnectorOrdersCancelFailed(connector, fails);
				connector.NewStopOrders += orders => _parent.OnInnerConnectorNewStopOrders(connector, orders);
				connector.StopOrdersChanged += orders => _parent.OnInnerConnectorStopOrdersChanged(connector, orders);
				connector.StopOrdersRegisterFailed += fails => _parent.OnInnerConnectorStopOrdersRegisterFailed(connector, fails);
				connector.StopOrdersCancelFailed += fails => _parent.OnInnerConnectorStopOrdersCancelFailed(connector, fails);
				connector.NewMarketDepths += depths => _parent.OnInnerConnectorNewMarketDepths(connector, depths);
				connector.MarketDepthsChanged += depths => _parent.OnInnerConnectorMarketDepthsChanged(connector, depths);
				connector.NewOrderLogItems += items => _parent.OnInnerConnectorNewOrderLogItems(connector, items);
				connector.Connected += () => _parent.OnInnerConnectorConnected(connector);
				connector.Disconnected += () => _parent.OnInnerConnectorDisconnected(connector);
				connector.ConnectionError += error => _parent.OnInnerConnectorConnectionError(connector, error);
				connector.ExportStarted += () => _parent.OnInnerConnectorExportStarted(connector);
				connector.ExportStopped += () => _parent.OnInnerConnectorExportStopped(connector);
				connector.ExportError += error => _parent.OnInnerConnectorExportError(connector, error);
				connector.ProcessDataError += error => _parent.OnInnerConnectorProcessDataError(connector, error);
				connector.NewDataExported += () => _parent.OnInnerConnectorNewDataExported(connector);
				connector.NewNews += news => _parent.OnInnerConnectorNewNews(connector, news);
				connector.NewsChanged += news => _parent.OnInnerConnectorNewsChanged(connector, news);
				connector.NewMessage += (message, direction) => _parent.OnInnerConnectorNewMessage(connector, message, direction);
				connector.LookupSecuritiesResult += result => _parent.OnInnerConnectorLookupSecuritiesResult(connector, result);
				connector.LookupPortfoliosResult += result => _parent.OnInnerConnectorLookupPortfoliosResult(connector, result);
				connector.MarketDataSubscriptionSucceeded += (s, t) => _parent.OnInnerConnectorSubscriptionSucceeded(connector, s, t);
				connector.MarketDataSubscriptionFailed += (s, t, err) => _parent.OnInnerConnectorSubscriptionFailed(connector, s, t, err);
				connector.SessionStateChanged += (b, s) => _parent.OnInnerConnectorSessionStateChanged(connector, b, s);

				if (Count == 0)
					connector.MarketTimeChanged += diff => _parent.OnInnerConnectorMarketTimeChanged(connector, diff);

				_enables.Add(connector, priority);
			}

			public void UnSubscribe(IConnector connector)
			{
				if (connector == null)
					throw new ArgumentNullException("connector");

				//connector.NewPortfolios -= _parent.OnInnerConnectorNewPortfolios;
				//connector.PortfoliosChanged -= _parent.OnInnerConnectorPortfoliosChanged;
				//connector.NewPositions -= _parent.OnInnerConnectorNewPositions;
				//connector.PositionsChanged -= _parent.OnInnerConnectorPositionsChanged;
				//connector.NewSecurities -= _parent.OnInnerConnectorNewSecurities;
				//connector.SecuritiesChanged -= _parent.OnInnerConnectorSecuritiesChanged;
				//connector.NewTrades -= _parent.OnInnerConnectorNewTrades;
				//connector.NewMyTrades -= _parent.OnInnerConnectorNewMyTrades;
				//connector.NewOrders -= _parent.OnInnerConnectorNewOrders;
				//connector.OrdersChanged -= _parent.OnInnerConnectorOrdersChanged;
				//connector.OrdersRegisterFailed -= _parent.OnInnerConnectorOrdersRegisterFailed;
				//connector.OrdersCancelFailed -= _parent.OnInnerConnectorOrdersCancelFailed;
				//connector.NewStopOrders -= _parent.OnInnerConnectorNewStopOrders;
				//connector.StopOrdersChanged -= _parent.OnInnerConnectorStopOrdersChanged;
				//connector.StopOrdersRegisterFailed -= _parent.OnInnerConnectorStopOrdersRegisterFailed;
				//connector.StopOrdersCancelFailed -= _parent.OnInnerConnectorStopOrdersCancelFailed;
				//connector.NewMarketDepths -= _parent.OnInnerConnectorNewMarketDepths;
				//connector.MarketDepthsChanged -= _parent.OnInnerConnectorMarketDepthsChanged;
				//connector.NewOrderLogItems -= _parent.OnInnerConnectorNewOrderLogItems;
				//connector.Connected -= _parent.OnInnerConnectorConnected;
				//connector.Disconnected -= _parent.OnInnerConnectorDisconnected;
				//connector.ConnectionError -= _parent.OnInnerConnectorConnectionError;
				//connector.ProcessDataError -= _parent.OnInnerConnectorProcessDataError;
				//connector.NewDataExported -= _parent.OnInnerConnectorNewDataExported;
				//connector.MarketTimeChanged -= _parent.OnInnerConnectorMarketTimeChanged;

				_enables.Remove(connector);

				foreach (var portfolio in connector.Portfolios)
				{
					_portfolioConnectors.Remove(portfolio);
				}

				var baseTrader = connector as BaseLogSource;

				if (baseTrader != null)
					baseTrader.Parent = null;
			}

			private void ProcessConnectorValues(IConnector connector)
			{
				if (connector == null)
					throw new ArgumentNullException("connector");

				_parent.OnInnerConnectorNewPortfolios(connector, connector.Portfolios);
				_parent.OnInnerConnectorNewSecurities(connector, connector.Securities);
			}

			private readonly Dictionary<IConnector, int> _enables = new Dictionary<IConnector, int>(); 
			
			public int this[IConnector connector]
			{
				get
				{
					lock (SyncRoot)
						return _enables.TryGetValue2(connector) ?? -1;
				}
				set
				{
					if (value < -1)
						throw new ArgumentOutOfRangeException();

					lock (SyncRoot)
					{
						if (!Contains(connector))
							Add(connector);

						_enables[connector] = value;
						_portfolioConnectors.Clear();
					}
				}
			}
		}

		private readonly SynchronizedDictionary<Tuple<Security, MarketDataTypes>, RefTriple<IEnumerator<IConnector>, SubscriptionAction, SubscriptionAction>> _subscriptionQueue = new SynchronizedDictionary<Tuple<Security, MarketDataTypes>, RefTriple<IEnumerator<IConnector>, SubscriptionAction, SubscriptionAction>>();

		/// <summary>
		/// Создать <see cref="BasketConnector"/>.
		/// </summary>
		public BasketConnector()
		{
			_innerConnectors = new InnerTraderList(this);
			SupportTradesUnique = true;
		}

		/// <summary>
		/// Поддерживать ли уникальность данных в пределах всех вложенных подключений <see cref="InnerConnectors"/>.
		/// Например, если один и тот же инструмент транслируется из нескольких вложенных подключений <see cref="InnerConnectors"/>,
		/// то событие <see cref="NewSecurities"/> будет вызвано только один раз. По-умолчанию режим выключен.
		/// </summary>
		public bool SupportUnique { get; set; }

		/// <summary>
		/// Поддерживать ли уникальность сделок в пределах всех вложенных подключений <see cref="InnerConnectors"/>. По-умолчанию режим включен.
		/// </summary>
		public bool SupportTradesUnique { get; set; }

		private readonly InnerTraderList _innerConnectors;

		/// <summary>
		/// Подключения к торговым системам, с которыми оперирует агрегатор.
		/// </summary>
		public IInnerConnectorList InnerConnectors
		{
			get { return _innerConnectors; }
		}

		private readonly CachedSynchronizedSet<ExchangeBoard> _exchanges = new CachedSynchronizedSet<ExchangeBoard>();

		/// <summary>
		/// Список всех биржевых площадок, для которых загружены инструменты <see cref="IConnector.Securities"/>.
		/// </summary>
		public IEnumerable<ExchangeBoard> ExchangeBoards
		{
			get
			{
				return SupportUnique ? _exchanges.Cache : _innerConnectors.ExchangeBoards;
			}
		}

		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>();

		/// <summary>
		/// Список всех загруженных инструментов.
		/// Вызывать необходимо после того, как пришло событие <see cref="NewSecurities"/>.
		/// Иначе будет возвращено постое множество.
		/// </summary>
		public virtual IEnumerable<Security> Securities
		{
			get
			{
				return SupportUnique ? _securities.CachedValues : _innerConnectors.Securities;
			}
		}

		/// <summary>
		/// Получить все заявки.
		/// </summary>
		public virtual IEnumerable<Order> Orders
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.Orders); }
		}

		/// <summary>
		/// Получить все стоп-заявки.
		/// </summary>
		public virtual IEnumerable<Order> StopOrders
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.StopOrders); }
		}

		/// <summary>
		/// Получить все ошибки при регистрации заявок.
		/// </summary>
		public virtual IEnumerable<OrderFail> OrderRegisterFails
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.OrderRegisterFails); }
		}

		/// <summary>
		/// Получить все ошибки при снятии заявок.
		/// </summary>
		public virtual IEnumerable<OrderFail> OrderCancelFails
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.OrderCancelFails); }
		}

		private readonly SynchronizedDictionary<Security, SynchronizedDictionary<long, Trade>> _trades = new SynchronizedDictionary<Security, SynchronizedDictionary<long, Trade>>();
		
		/// <summary>
		/// Получить все сделки.
		/// </summary>
		public virtual IEnumerable<Trade> Trades
		{
			get
			{
				return SupportTradesUnique
							? _trades.SyncGet(d => d.Values.SelectMany(d2 => d2.Values).ToArray())
							: _innerConnectors.Trades;
			}
		}

		/// <summary>
		/// Получить все собственные сделки.
		/// </summary>
		public virtual IEnumerable<MyTrade> MyTrades
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.MyTrades); }
		}

		private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new CachedSynchronizedDictionary<string, Portfolio>();

		/// <summary>
		/// Получить все портфели.
		/// </summary>
		public virtual IEnumerable<Portfolio> Portfolios
		{
			get
			{
				return SupportUnique ? _portfolios.CachedValues : _innerConnectors.Portfolios;
			}
		}

		/// <summary>
		/// Получить все позиции.
		/// </summary>
		public virtual IEnumerable<Position> Positions
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.Positions); }
		}

		/// <summary>
		/// Все новости.
		/// </summary>
		public virtual IEnumerable<News> News
		{
			get { return _innerConnectors.Cache.SelectMany(t => t.News); }
		}

		/// <summary>
		/// Состояние соединения.
		/// </summary>
		public virtual ConnectionStates ConnectionState { get; private set; }

		/// <summary>
		/// Состояние экспорта.
		/// </summary>
		public virtual ConnectionStates ExportState { get; private set; }

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции.
		/// </summary>
		public virtual bool IsSupportAtomicReRegister
		{
			get
			{
				var traders = GetSortedTraders();
				return !traders.IsEmpty() && traders.All(t => t.IsSupportAtomicReRegister);
			}
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterSecurity"/>.
		/// </summary>
		public virtual IEnumerable<Security> RegisteredSecurities
		{
			get { return GetSortedTraders().SelectMany(t => t.RegisteredSecurities); }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterMarketDepth"/>.
		/// </summary>
		public virtual IEnumerable<Security> RegisteredMarketDepths
		{
			get { return GetSortedTraders().SelectMany(t => t.RegisteredMarketDepths); }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterTrades"/>.
		/// </summary>
		public virtual IEnumerable<Security> RegisteredTrades
		{
			get { return GetSortedTraders().SelectMany(t => t.RegisteredTrades); }
		}

		/// <summary>
		/// Список всех инструментов, зарегистрированных через <see cref="IConnector.RegisterOrderLog"/>.
		/// </summary>
		public virtual IEnumerable<Security> RegisteredOrderLogs
		{
			get { return GetSortedTraders().SelectMany(t => t.RegisteredOrderLogs); }
		}

		/// <summary>
		/// Список всех портфелей, зарегистрированных через <see cref="IConnector.RegisterPortfolio"/>.
		/// </summary>
		public virtual IEnumerable<Portfolio> RegisteredPortfolios
		{
			get { return GetSortedTraders().SelectMany(t => t.RegisteredPortfolios); }
		}

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
		/// Событие изменения состояния заявок (снята, удовлетворена).
		/// </summary>
		public event Action<IEnumerable<Order>> OrdersChanged;

		/// <summary>
		/// Событие ошибок при регистрации заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersRegisterFailed;

		/// <summary>
		/// Событие ошибок при снятии заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> OrdersCancelFailed;

		/// <summary>
		/// Событие ошибок при регистрации стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersRegisterFailed;

		/// <summary>
		/// Событие ошибок при снятии стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<OrderFail>> StopOrdersCancelFailed;

		/// <summary>
		/// Событие появления новых стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> NewStopOrders;

		/// <summary>
		/// Событие изменения состояния стоп-заявок.
		/// </summary>
		public event Action<IEnumerable<Order>> StopOrdersChanged;

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
		/// Событие, сигнализирующее о новых экспортируемых данных.
		/// </summary>
		public event Action NewDataExported;

		/// <summary>
		/// Событие, сигнализирующее об ошибке при получении или обработке новых данных с сервера.
		/// </summary>
		public event Action<Exception> ProcessDataError;

		/// <summary>
		/// Событие, сигнализирующее об изменении текущего времени на биржевых площадках <see cref="IConnector.ExchangeBoards"/>.
		/// Передается разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public event Action<TimeSpan> MarketTimeChanged;

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
		/// Получить состояние сессии для заданной площадки.
		/// </summary>
		/// <param name="board">Биржевая площадка электронных торгов.</param>
		/// <returns>Состояние сессии. Если информация о состоянии сессии отсутствует, то будет возвращено <see langword="null"/>.</returns>
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return GetSortedTraders().Select(t => t.GetSessionState(board)).FirstOrDefault(s => s != null);
		}

		///// <summary>
		///// Получить биржевое время.
		///// </summary>
		///// <param name="exchange">Биржа.</param>
		///// <returns>Биржевое время.</returns>
		//public DateTime GetMarketTime(Exchange exchange)
		//{
		//	if (exchange == null)
		//		throw new ArgumentNullException("exchange");

		//	var trader = GetSortedTraders().FirstOrDefault(t => t.ExchangeBoards.Any(b => b.Exchange == exchange));
		//	return trader == null ? exchange.ToExchangeTime(TimeHelper.Now) : trader.GetMarketTime(exchange);
		//}

		/// <summary>
		/// Получить подключения <see cref="IInnerConnectorList.SortedConnectors"/>, отсортированные в зависимости от заданного приоритета. По-умолчанию сортировка отсутствует.
		/// </summary>
		/// <returns>Отсортированные подключения.</returns>
		protected virtual IEnumerable<IConnector> GetSortedTraders()
		{
			return _innerConnectors.SortedConnectors;
		}

		private IEnumerable<IConnector> GetConnectedConnectors()
		{
			return _innerConnectors.SortedConnectors.Where(t => t.ConnectionState == ConnectionStates.Connected);
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		public virtual void Connect()
		{
			ConnectionState = ConnectionStates.Connecting;
			GetSortedTraders().ForEach(t => t.Connect());
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		public virtual void Disconnect()
		{
			ConnectionState = ConnectionStates.Disconnecting;
			GetSortedTraders().ForEach(t => t.Disconnect());
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		public virtual void StartExport()
		{
			ExportState = ConnectionStates.Connecting;
			GetSortedTraders().ForEach(t => t.StartExport());
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу, запущенный через <see cref="IConnector.StartExport"/>.
		/// </summary>
		public virtual void StopExport()
		{
			ExportState = ConnectionStates.Disconnecting;
			GetSortedTraders().ForEach(t => t.StopExport());
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		public virtual void LookupSecurities(Security criteria)
		{
			GetConnectedConnectors().ForEach(t => t.LookupSecurities(criteria));
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		public virtual void LookupSecurities(SecurityLookupMessage criteria)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Получить инструмент по значению его поля.
		/// </summary>
		/// <param name="fieldName">Название поля инструмента.</param>
		/// <param name="fieldValue">Значение поля инструмента.</param>
		/// <returns>Полученный инструмент. Если инструмент по данным критериям отсутствует, то будет возвращено null.</returns>
		public Security LoadSecurityBy(string fieldName, object fieldValue)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные портфели будут переданы через событие <see cref="LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		public void LookupPortfolios(Portfolio criteria)
		{
			GetConnectedConnectors().ForEach(t => t.LookupPortfolios(criteria));
		}

		/// <summary>
		/// Получить позицию по портфелю и инструменту.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому нужно найти позицию.</param>
		/// <param name="security">Инструмент, по которому нужно найти позицию.</param>
		/// <param name="depoName">Название депозитария, где находится физически ценная бумага.
		/// По-умолчанию передается пустая строка, что означает суммарную позицию по всем депозитариям.</param>
		/// <returns>Позиция.</returns>
		public virtual Position GetPosition(Portfolio portfolio, Security security, string depoName = "")
		{
			return _innerConnectors.SafeGetConnector(portfolio).GetPosition(portfolio, security, depoName);
		}

		/// <summary>
		/// Получить стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Стакан котировок.</returns>
		public virtual MarketDepth GetMarketDepth(Security security)
		{
			return GetTrader(security).GetMarketDepth(security);
		}

		/// <summary>
		/// Получить отфильтрованный стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Отфильтрованный стакан котировок.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return GetTrader(security).GetFilteredMarketDepth(security);
		}

		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		public virtual void RegisterOrder(Order order)
		{
			GetTrader(order).RegisterOrder(order);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		public virtual void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			GetTrader(oldOrder).ReRegisterOrder(oldOrder, newOrder);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять и на основе нее зарегистрировать новую.</param>
		/// <param name="price">Цена новой заявки.</param>
		/// <param name="volume">Объем новой заявки.</param>
		/// <returns>Новая заявка.</returns>
		public virtual Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume)
		{
			return GetTrader(oldOrder).ReRegisterOrder(oldOrder, price, volume);
		}

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменить.</param>
		public virtual void CancelOrder(Order order)
		{
			GetTrader(order).CancelOrder(order);
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно null, то инструмент не попадает в фильтр снятия заявок.</param>
		public virtual void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (portfolio != null)
				_innerConnectors.SafeGetConnector(portfolio).CancelOrders(isStopOrder, portfolio, direction, board, security);
			else
			{
				foreach (var trader in GetConnectedConnectors())
					trader.CancelOrders(isStopOrder, null, direction, board, security);
			}
		}

		/// <summary>
		/// Подписаться на получение рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			ProcessSubscribeAction(security, type, (t, s) => t.SubscribeMarketData(s, type));
		}

		/// <summary>
		/// Отписаться от получения рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			ProcessUnSubscribeAction(security, type, (t, s) => t.UnSubscribeMarketData(s, type));
		}

		/// <summary>
		/// Начать получать котировки (стакан) по инструменту.
		/// Значение котировок можно получить через метод <see cref="GetMarketDepth(StockSharp.BusinessEntities.Security)"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		public virtual void RegisterMarketDepth(Security security)
		{
			ProcessSubscribeAction(security, MarketDataTypes.MarketDepth, (t, s) => t.RegisterMarketDepth(s));
		}

		/// <summary>
		/// Остановить получение котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		public virtual void UnRegisterMarketDepth(Security security)
		{
			ProcessUnSubscribeAction(security, MarketDataTypes.MarketDepth, (t, s) => t.UnRegisterMarketDepth(s));
		}

		/// <summary>
		/// Начать получать отфильтрованные котировки (стакан) по инструменту.
		/// Значение котировок можно получить через метод <see cref="IConnector.GetFilteredMarketDepth"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать котировки.</param>
		public void RegisterFilteredMarketDepth(Security security)
		{
			GetTrader(security).RegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// Остановить получение отфильтрованных котировок по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение котировок.</param>
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			GetTrader(security).UnRegisterFilteredMarketDepth(security);
		}

		/// <summary>
		/// Начать получать сделки (тиковые данные) по инструменту. Новые сделки будут приходить через
		/// событие <see cref="NewTrades"/>.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать сделки.</param>
		public virtual void RegisterTrades(Security security)
		{
			ProcessSubscribeAction(security, MarketDataTypes.Trades, (t, s) => t.RegisterTrades(s));
		}

		/// <summary>
		/// Остановить получение сделок (тиковые данные) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение сделок.</param>
		public virtual void UnRegisterTrades(Security security)
		{
			ProcessUnSubscribeAction(security, MarketDataTypes.Trades, (t, s) => t.UnRegisterTrades(s));
		}

		/// <summary>
		/// Начать получать новую информацию (например, <see cref="P:StockSharp.BusinessEntities.Security.LastTrade"/> или <see cref="P:StockSharp.BusinessEntities.Security.BestBid"/>) по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		public virtual void RegisterSecurity(Security security)
		{
			ProcessSubscribeAction(security, MarketDataTypes.Level1, (t, s) => t.RegisterSecurity(s));
		}

		/// <summary>
		/// Остановить получение новой информации.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение новой информации.</param>
		public virtual void UnRegisterSecurity(Security security)
		{
			ProcessUnSubscribeAction(security, MarketDataTypes.Level1, (t, s) => t.UnRegisterSecurity(s));
		}

		/// <summary>
		/// Начать получать новую информацию по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо начать получать новую информацию.</param>
		public virtual void RegisterPortfolio(Portfolio portfolio)
		{
			_innerConnectors.SafeGetConnector(portfolio).RegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Остановить получение новой информации по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому необходимо остановить получение новой информации.</param>
		public virtual void UnRegisterPortfolio(Portfolio portfolio)
		{
			_innerConnectors.SafeGetConnector(portfolio).UnRegisterPortfolio(portfolio);
		}

		/// <summary>
		/// Начать получать новости.
		/// </summary>
		public virtual void RegisterNews()
		{
			GetConnectedConnectors().ForEach(t => t.RegisterNews());
		}

		/// <summary>
		/// Остановить получение новостей.
		/// </summary>
		public virtual void UnRegisterNews()
		{
			GetConnectedConnectors().ForEach(t => t.UnRegisterNews());
		}

		/// <summary>
		/// Запросить текст новости <see cref="BusinessEntities.News.Story"/>. После получения текста будет вызвано событие <see cref="IConnector.NewsChanged"/>.
		/// </summary>
		/// <param name="news">Новость.</param>
		public void RequestNewsStory(News news)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Начать получать лог заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать лог заявок.</param>
		public virtual void RegisterOrderLog(Security security)
		{
			ProcessSubscribeAction(security, MarketDataTypes.OrderLog, (t, s) => t.RegisterOrderLog(s));
		}

		/// <summary>
		/// Остановить получение лога заявок для инструмента.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо остановить получение лога заявок.</param>
		public virtual void UnRegisterOrderLog(Security security)
		{
			ProcessUnSubscribeAction(security, MarketDataTypes.OrderLog, (t, s) => t.UnRegisterOrderLog(s));
		}

		/// <summary>
		/// Событие изменения инструмента.
		/// </summary>
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTime> ValuesChanged;

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return _innerConnectors
				.Cache
				.Select(connector => connector.GetSecurityValue(security, field))
				.FirstOrDefault(value => value != null);
		}

		/// <summary>
		/// Получить набор доступных полей <see cref="Level1Fields"/>, для которых есть маркет-данные для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Набор доступных полей.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			foreach (var connector in _innerConnectors.Cache)
			{
				var fields = connector.GetLevel1Fields(security);

				if (!fields.IsEmpty())
					return fields;
			}

			return Enumerable.Empty<Level1Fields>();
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			return _innerConnectors
				.Cache
				.Select(connector => connector.Lookup(criteria))
				.FirstOrDefault(value => !value.IsEmpty());
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private IConnector GetTrader(Security security, bool throwIfNotFound = true)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var trader = GetSortedTraders().FirstOrDefault(t => t.Securities.Contains(security));

			if (!throwIfNotFound)
				return trader;

			if (trader == null)
				throw new ArgumentException(LocalizedStrings.Str1109Params.Put(security.Id), "security");

			//ThrowIfTraderUnregistered(trader);

			return trader;
		}

		private void ProcessSubscribeAction(Security security, MarketDataTypes type, Action<IConnector, Security> action)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (action == null)
				throw new ArgumentNullException("action");

			var key = Tuple.Create(security, type);

			if (_subscriptionQueue.ContainsKey(key))
				return;

			var enumerator = GetConnectedConnectors().ToArray().Cast<IConnector>().GetEnumerator();

			_subscriptionQueue.Add(key, new RefTriple<IEnumerator<IConnector>, SubscriptionAction, SubscriptionAction>(enumerator, action, null));

			ProcessSubscriptionAction(enumerator, security, type, action);
		}

		private void ProcessUnSubscribeAction(Security security, MarketDataTypes type, SubscriptionAction action)
		{
			if (security == null) 
				throw new ArgumentNullException("security");

			if (action == null)
				throw new ArgumentNullException("action");

			lock (_subscriptionQueue.SyncRoot)
			{
				var tuple = _subscriptionQueue.TryGetValue(Tuple.Create(security, type));
				if (tuple != null)
				{
					tuple.Third = action;
					return;
				}
			}

			var trader = GetTrader(security, false);

			if (trader != null)
				action(trader, security);
			else
				this.AddErrorLog(LocalizedStrings.Str1110Params, type, security);
		}

		private void ProcessSubscriptionAction(IEnumerator<IConnector> enumerator, Security security, MarketDataTypes type, SubscriptionAction action)
		{
			if (enumerator.MoveNext())
				action(enumerator.Current, security);
			else
				RaiseMarketDataSubscriptionFailed(security, type, new ArgumentException(LocalizedStrings.Str1109Params.Put(security.Id), "security"));
		}

		private void OnInnerConnectorSubscriptionFailed(IConnector connector, Security security, MarketDataTypes type, Exception error)
		{
			this.AddDebugLog(LocalizedStrings.Str1111Params, connector, security, type, error);

			bool cancel;
			RefTriple<IEnumerator<IConnector>, SubscriptionAction, SubscriptionAction> tuple;

			lock (_subscriptionQueue.SyncRoot)
			{
				tuple = _subscriptionQueue.TryGetValue(Tuple.Create(security, type));
				cancel = tuple != null && tuple.Third != null;
			}

			if (cancel)
				RaiseMarketDataSubscriptionFailed(security, type, new InvalidOperationException(LocalizedStrings.SubscriptionProcessCancelled));
			else if (tuple != null)
				ProcessSubscriptionAction(tuple.First, security, type, tuple.Second);
			else
				RaiseMarketDataSubscriptionFailed(security, type, new InvalidOperationException(LocalizedStrings.Str633Params.Put(security.Id, type)));
		}

		private void OnInnerConnectorSubscriptionSucceeded(IConnector connector, Security security, MarketDataTypes type)
		{
			SubscriptionAction unsubscribe = null;

			lock (_subscriptionQueue.SyncRoot)
			{
				var key = Tuple.Create(security, type);
				var tuple = _subscriptionQueue.TryGetValue(key);

				if (tuple != null)
				{
					unsubscribe = tuple.Third;
					_subscriptionQueue.Remove(key);
				}
			}

			this.AddInfoLog(LocalizedStrings.Str1112Params, security.Id, connector);
			RaiseMarketDataSubscriptionSucceeded(security, type);

			unsubscribe.SafeInvoke(connector, security);
		}

		/// <summary>
		/// Вызвать событие <see cref="MarketDataSubscriptionSucceeded"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип данных.</param>
		protected void RaiseMarketDataSubscriptionSucceeded(Security security, MarketDataTypes dataType)
		{
			MarketDataSubscriptionSucceeded.SafeInvoke(security, dataType);
		}

		/// <summary>
		/// Вызвать событие <see cref="MarketDataSubscriptionFailed"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="type">Тип данных.</param>
		/// <param name="error">Ошибка.</param>
		protected void RaiseMarketDataSubscriptionFailed(Security security, MarketDataTypes type, Exception error)
		{
			_subscriptionQueue.Remove(Tuple.Create(security, type));

			this.AddErrorLog(LocalizedStrings.Str634Params, security.Id, type, error);

			MarketDataSubscriptionFailed.SafeInvoke(security, type, error);
		}

		private IConnector GetTrader(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return _innerConnectors.SafeGetConnector(order.Portfolio);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			_innerConnectors.ForEach(t => t.Dispose());
			_innerConnectors.Clear();

			SupportUnique = storage.GetValue<bool>("supportUnique");
			SupportTradesUnique = storage.GetValue("supportTradesUnique", true);

			//var entityRegistry = ConfigManager.TryGetService<IEntityRegistry>();

			foreach (var traderStorage in storage.GetValue<IEnumerable<SettingsStorage>>("innerTraders"))
			{
				var trader = traderStorage.LoadEntire<Connector>();

				if (traderStorage.GetValue("isEmulationMode", false))
				{
					//var emulationPortfolios = traderStorage.GetValue("emulationPortfolios", string.Empty).Split(',');
					trader = new RealTimeEmulationTrader(trader/*, emulationPortfolios.Select(name =>
					{
						Portfolio portfolio = null;

						if (entityRegistry != null)
							portfolio = entityRegistry.Portfolios.ReadById(name);

						return portfolio ?? new Portfolio { Name = name };
					}).ToArray()*/);
				}

				_innerConnectors.Add(trader);
				_innerConnectors[trader] = traderStorage.GetValue("priority", -1);
			}

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("supportUnique", SupportUnique);
			storage.SetValue("supportTradesUnique", SupportTradesUnique);
			storage.SetValue("innerTraders", SaveInnerConnectors(_innerConnectors.Cache).ToArray());

			base.Save(storage);
		}

		/// <summary>
		/// Сохранить настройки вложенных подключений.
		/// </summary>
		/// <param name="innerConnectors">Вложенные подключения.</param>
		/// <returns>Настройки.</returns>
		protected virtual IEnumerable<SettingsStorage> SaveInnerConnectors(IEnumerable<IConnector> innerConnectors)
		{
			if (innerConnectors == null)
				throw new ArgumentNullException("innerConnectors");

			return innerConnectors.Select(connector =>
			{
				var realTimeEmulationTrader = connector as RealTimeEmulationTrader;
				var connectorStorage = (realTimeEmulationTrader != null ? realTimeEmulationTrader.UnderlyingConnector : connector).SaveEntire(false);

				connectorStorage.SetValue("isEmulationMode", realTimeEmulationTrader != null);

				if (realTimeEmulationTrader != null)
				{
					connectorStorage.SetValue("emulationPortfolios", realTimeEmulationTrader.Portfolios.Select(p => p.Name).Join(","));
				}

				connectorStorage.SetValue("priority", _innerConnectors[connector]);

				return connectorStorage;
			});
		}

		private void ProcessUnique<TEntity, TId>(IEnumerable<TEntity> newEntities, Func<TEntity, TId> getKey, SynchronizedDictionary<TId, TEntity> cache, Action<IEnumerable<TEntity>> newEntitiesEvent)
		{
			if (SupportUnique)
			{
				var uniqueEntities = new List<TEntity>();

				var entities = newEntities;
				cache.SyncDo(d =>
				{
					foreach (var newEntity in entities)
					{
						var id = getKey(newEntity);

						if (!d.ContainsKey(id))
						{
							d.Add(id, newEntity);
							uniqueEntities.Add(newEntity);
						}
					}
				});

				newEntities = uniqueEntities;
			}

			if (!newEntities.IsEmpty())
				newEntitiesEvent.SafeInvoke(newEntities);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewPortfolios"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="portfolios">Новые портфели.</param>
		protected virtual void OnInnerConnectorNewPortfolios(IConnector innerConnector, IEnumerable<Portfolio> portfolios)
		{
			ProcessUnique(portfolios, p => p.Name, _portfolios, NewPortfolios);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.PortfoliosChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="portfolios">Измененные портфели.</param>
		protected virtual void OnInnerConnectorPortfoliosChanged(IConnector innerConnector, IEnumerable<Portfolio> portfolios)
		{
			PortfoliosChanged.SafeInvoke(portfolios);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewPositions"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="positions">Новые позиции.</param>
		protected virtual void OnInnerConnectorNewPositions(IConnector innerConnector, IEnumerable<Position> positions)
		{
			NewPositions.SafeInvoke(positions);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.PositionsChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="positions">Измененные позиции.</param>
		protected virtual void OnInnerConnectorPositionsChanged(IConnector innerConnector, IEnumerable<Position> positions)
		{
			PositionsChanged.SafeInvoke(positions);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewSecurities"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="securities">Новые инструменты.</param>
		protected virtual void OnInnerConnectorNewSecurities(IConnector innerConnector, IEnumerable<Security> securities)
		{
			ProcessUnique(securities, s => s.Id, _securities, NewSecurities);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.SecuritiesChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="securities">Измененные инструменты.</param>
		protected virtual void OnInnerConnectorSecuritiesChanged(IConnector innerConnector, IEnumerable<Security> securities)
		{
			SecuritiesChanged.SafeInvoke(securities);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewTrades"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="trades">Новые сделки.</param>
		protected virtual void OnInnerConnectorNewTrades(IConnector innerConnector, IEnumerable<Trade> trades)
		{
			if (SupportTradesUnique)
			{
				var uniqueTrades = trades.ToList();

				_trades.SyncDo(d =>
				{
					foreach (var trade in trades)
					{
						var dict = d.SafeAdd(trade.Security);

						if (dict.ContainsKey(trade.Id))
							uniqueTrades.Remove(trade);
						else
							dict.Add(trade.Id, trade);
					}
				});

				if (!uniqueTrades.IsEmpty())
					NewTrades.SafeInvoke(uniqueTrades);
			}
			else
				NewTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewMyTrades"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="trades">Новые сделки.</param>
		protected virtual void OnInnerConnectorNewMyTrades(IConnector innerConnector, IEnumerable<MyTrade> trades)
		{
			NewMyTrades.SafeInvoke(trades);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewOrders"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="orders">Новые заявки.</param>
		protected virtual void OnInnerConnectorNewOrders(IConnector innerConnector, IEnumerable<Order> orders)
		{
			NewOrders.SafeInvoke(orders);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.OrdersChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="orders">Измененные заявки.</param>
		protected virtual void OnInnerConnectorOrdersChanged(IConnector innerConnector, IEnumerable<Order> orders)
		{
			OrdersChanged.SafeInvoke(orders);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.OrdersRegisterFailed"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="fails">Информация об ошибках.</param>
		protected virtual void OnInnerConnectorOrdersRegisterFailed(IConnector innerConnector, IEnumerable<OrderFail> fails)
		{
			OrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.OrdersCancelFailed"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="fails">Информация об ошибках.</param>
		protected virtual void OnInnerConnectorOrdersCancelFailed(IConnector innerConnector, IEnumerable<OrderFail> fails)
		{
			OrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewStopOrders"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="orders">Новые заявки.</param>
		protected virtual void OnInnerConnectorNewStopOrders(IConnector innerConnector, IEnumerable<Order> orders)
		{
			NewStopOrders.SafeInvoke(orders);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.StopOrdersChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="orders">Измененные заявки.</param>
		protected virtual void OnInnerConnectorStopOrdersChanged(IConnector innerConnector, IEnumerable<Order> orders)
		{
			StopOrdersChanged.SafeInvoke(orders);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.StopOrdersRegisterFailed"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="fails">Информация об ошибках.</param>
		protected virtual void OnInnerConnectorStopOrdersRegisterFailed(IConnector innerConnector, IEnumerable<OrderFail> fails)
		{
			StopOrdersRegisterFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.StopOrdersCancelFailed"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="fails">Информация об ошибках.</param>
		protected virtual void OnInnerConnectorStopOrdersCancelFailed(IConnector innerConnector, IEnumerable<OrderFail> fails)
		{
			StopOrdersCancelFailed.SafeInvoke(fails);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewMarketDepths"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="depths">Новые стаканы.</param>
		protected virtual void OnInnerConnectorNewMarketDepths(IConnector innerConnector, IEnumerable<MarketDepth> depths)
		{
			NewMarketDepths.SafeInvoke(depths);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.MarketDepthsChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="depths">Измененные стаканы.</param>
		protected virtual void OnInnerConnectorMarketDepthsChanged(IConnector innerConnector, IEnumerable<MarketDepth> depths)
		{
			MarketDepthsChanged.SafeInvoke(depths);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewOrderLogItems"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="items">Новые строки лога заявок.</param>
		protected virtual void OnInnerConnectorNewOrderLogItems(IConnector innerConnector, IEnumerable<OrderLogItem> items)
		{
			NewOrderLogItems.SafeInvoke(items);
		}

		private bool _canRaiseConnected = true;
		private bool _canRaiseDisconnected;

		/// <summary>
		/// Обработчик события <see cref="IConnector.Connected"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		protected virtual void OnInnerConnectorConnected(IConnector innerConnector)
		{
			var canProcess = _innerConnectors.SyncGet(c =>
			{
				if (_canRaiseConnected)
				{
					var traders = GetSortedTraders();

					if (!traders.IsEmpty() && traders.All(t => t.ConnectionState == ConnectionStates.Connected))
					{
						_canRaiseConnected = false;
						_canRaiseDisconnected = true;

						return true;
					}
				}

				return false;
			});

			if (canProcess)
			{
				ConnectionState = ConnectionStates.Connected;
				Connected.SafeInvoke();
			}
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.Disconnected"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		protected virtual void OnInnerConnectorDisconnected(IConnector innerConnector)
		{
			var canProcess = _innerConnectors.SyncGet(c =>
			{
				if (_canRaiseDisconnected)
				{
					var traders = GetSortedTraders();

					if (!traders.IsEmpty() && traders.All(t => t.ConnectionState == ConnectionStates.Disconnected))
					{
						_canRaiseConnected = true;
						_canRaiseDisconnected = false;

						return true;
					}
				}

				return false;
			});

			if (canProcess)
			{
				ConnectionState = ConnectionStates.Disconnected;
				Disconnected.SafeInvoke();
			}
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.ConnectionError"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="error">Информация об ошибке.</param>
		protected virtual void OnInnerConnectorConnectionError(IConnector innerConnector, Exception error)
		{
			lock (_innerConnectors.SyncRoot)
			{
				_canRaiseConnected = true;
				_canRaiseDisconnected = false;
			}

			ConnectionState = ConnectionStates.Failed;
			ConnectionError.SafeInvoke(error);
		}

		private bool _canRaiseExportStarted = true;
		private bool _canRaiseExportStopped;

		/// <summary>
		/// Обработчик события <see cref="IConnector.ExportStarted"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		protected virtual void OnInnerConnectorExportStarted(IConnector innerConnector)
		{
			var canProcess = _innerConnectors.SyncGet(c =>
			{
				if (_canRaiseExportStarted)
				{
					var traders = GetSortedTraders();

					if (!traders.IsEmpty() && traders.All(t => t.ExportState == ConnectionStates.Connected))
					{
						_canRaiseExportStarted = false;
						_canRaiseExportStopped = true;

						return true;
					}
				}

				return false;
			});

			if (canProcess)
			{
				ExportState = ConnectionStates.Connected;
				ExportStarted.SafeInvoke();
			}
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.ExportStopped"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		protected virtual void OnInnerConnectorExportStopped(IConnector innerConnector)
		{
			var canProcess = _innerConnectors.SyncGet(c =>
			{
				if (_canRaiseExportStopped)
				{
					var traders = GetSortedTraders();

					if (!traders.IsEmpty() && traders.All(t => t.ExportState == ConnectionStates.Disconnected))
					{
						_canRaiseExportStarted = true;
						_canRaiseExportStopped = false;

						return true;
					}
				}

				return false;
			});

			if (canProcess)
			{
				ExportState = ConnectionStates.Disconnected;
				ExportStopped.SafeInvoke();
			}
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.ExportError"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="error">Информация об ошибке.</param>
		protected virtual void OnInnerConnectorExportError(IConnector innerConnector, Exception error)
		{
			lock (_innerConnectors.SyncRoot)
			{
				_canRaiseExportStarted = true;
				_canRaiseExportStopped = false;
			}

			ExportState = ConnectionStates.Failed;
			ExportError.SafeInvoke(error);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.ProcessDataError"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="error">Информация об ошибке.</param>
		protected virtual void OnInnerConnectorProcessDataError(IConnector innerConnector, Exception error)
		{
			ProcessDataError.SafeInvoke(error);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewDataExported"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		protected virtual void OnInnerConnectorNewDataExported(IConnector innerConnector)
		{
			NewDataExported.SafeInvoke();
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewNews"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="news">Новость.</param>
		protected virtual void OnInnerConnectorNewNews(IConnector innerConnector, News news)
		{
			NewNews.SafeInvoke(news);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewsChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="news">Новость.</param>
		protected virtual void OnInnerConnectorNewsChanged(IConnector innerConnector, News news)
		{
			NewsChanged.SafeInvoke(news);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.NewMessage"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="message">Сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected virtual void OnInnerConnectorNewMessage(IConnector innerConnector, Message message, MessageDirections direction)
		{
			NewMessage.SafeInvoke(message, direction);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.MarketTimeChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="diff">Разница во времени, прошедшее с последнего вызова события. Первый раз событие передает значение <see cref="TimeSpan.Zero"/>.</param>
		protected virtual void OnInnerConnectorMarketTimeChanged(IConnector innerConnector, TimeSpan diff)
		{
			MarketTimeChanged.SafeInvoke(diff);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.LookupSecuritiesResult"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="securities">Найденные инструменты.</param>
		protected virtual void OnInnerConnectorLookupSecuritiesResult(IConnector innerConnector, IEnumerable<Security> securities)
		{
			LookupSecuritiesResult.SafeInvoke(securities);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.LookupPortfoliosResult"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="portfolios">Найденные портфели.</param>
		protected virtual void OnInnerConnectorLookupPortfoliosResult(IConnector innerConnector, IEnumerable<Portfolio> portfolios)
		{
			LookupPortfoliosResult.SafeInvoke(portfolios);
		}

		/// <summary>
		/// Обработчик события <see cref="IConnector.SessionStateChanged"/> вложенного подключения.
		/// </summary>
		/// <param name="innerConnector">Вложенное подключение.</param>
		/// <param name="board">Биржевая площадка.</param>
		/// <param name="state">Состояние сессии.</param>
		protected virtual void OnInnerConnectorSessionStateChanged(IConnector innerConnector, ExchangeBoard board, SessionStates state)
		{
			SessionStateChanged.SafeInvoke(board, state);
		}

		/// <summary>
		/// Освободить ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			foreach (var trader in _innerConnectors.Cache)
			{
				// AMU: отписаться от событий trader перед Dispose
				// При работе с несколькими Quik'ами возникает проблема с IsConnected в обработчике события BasketTrader.Disconnected - 
				// может быть ситуация когда Dispose происходит между проверкой IsDisposed и последующей проверкой ApiWrapper.IsDllConnected() в QuikTrader.IsConnected
				// Disconnected у BasketTrader вызовется из DisposeManaged() BaseTrader'а
				lock (_innerConnectors.SyncRoot)
					_innerConnectors.UnSubscribe(trader);
				
				trader.Dispose();
			}

			base.DisposeManaged();
		}
	}
}
