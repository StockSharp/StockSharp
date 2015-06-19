namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public class StrategyService : IStrategyService
	{
		private sealed class StrategyConnector : Connector
		{
			private readonly StrategyContainer _strategy;
			private readonly TimeSpan _useCandlesTimeFrame;
			private readonly bool _onlyInitialize;
			private readonly StudioConnector _realConnector;
			private readonly SessionStrategy _sessionStrategy;
			private readonly HistoryMessageAdapter _historyMessageAdapter;
			private readonly ISecurityProvider _securityProvider;

			private bool _isHistory = true;
			private bool _isInitialization;
			private DateTime _connectTime;

			public StrategyConnector(StrategyContainer strategy, DateTimeOffset startDate, DateTimeOffset stopDate, TimeSpan useCandlesTimeFrame, bool onlyInitialize)
			{
				if (strategy == null)
					throw new ArgumentNullException("strategy");

				UpdateSecurityLastQuotes = false;
				UpdateSecurityByLevel1 = false;

				var entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();

				_strategy = strategy;
				_useCandlesTimeFrame = useCandlesTimeFrame;
				_onlyInitialize = onlyInitialize;
				_sessionStrategy = entityRegistry.ReadSessionStrategyById(strategy.Strategy.Id);

				if (_sessionStrategy == null)
					throw new InvalidOperationException("sessionStrategy = null");

				Id = strategy.Id;
				Name = strategy.Name + " Connector";

				_realConnector = (StudioConnector)ConfigManager.GetService<IStudioConnector>();
				_realConnector.NewMessage += RealConnectorNewMessage;

				EntityFactory = new StudioConnectorEntityFactory();

				_securityProvider = new StudioSecurityProvider();

				var storageRegistry = new StudioStorageRegistry { MarketDataSettings = strategy.MarketDataSettings };

				Adapter.InnerAdapters.Add(_historyMessageAdapter = new HistoryMessageAdapter(TransactionIdGenerator, _securityProvider)
				{
					StartDate = startDate,
					StopDate = stopDate,
					StorageRegistry = storageRegistry
				});
				//_historyMessageAdapter.UpdateCurrentTime(startDate);
				var transactionAdapter = new PassThroughMessageAdapter(TransactionIdGenerator);
				transactionAdapter.AddTransactionalSupport();
				Adapter.InnerAdapters.Add(transactionAdapter);

				_historyMessageAdapter.MarketTimeChangedInterval = useCandlesTimeFrame;

				// при инициализации по свечкам, время меняется быстрее и таймаут должен быть больше 30с.
				ReConnectionSettings.TimeOutInterval = TimeSpan.MaxValue;

				_historyMessageAdapter.BasketStorage.InnerStorages.AddRange(GetExecutionStorages());

				this.LookupById(strategy.Security.Id);

				new ChartAutoRangeCommand(true).Process(_strategy);
			}

			private IEnumerable<IMarketDataStorage<ExecutionMessage>> GetExecutionStorages()
			{
				var executionRegistry = _sessionStrategy.GetExecutionStorage();

				var securities = InteropHelper
					.GetDirectories(executionRegistry.DefaultDrive.Path)
					.Select(letterDir => new { letterDir, name = Path.GetFileName(letterDir) })
					.Where(t => t.name != null && t.name.Length == 1)
					.SelectMany(t => InteropHelper.GetDirectories(t.letterDir))
					.Select(p => Path.GetFileName(p).FolderNameToSecurityId())
					.Select(this.LookupById)
					.Where(s => s != null)
					.ToArray();

				return securities.Select(s => executionRegistry.GetExecutionStorage(s, ExecutionTypes.Order));
			}

			private void RealConnectorNewMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						return;
				}

				if (!_isHistory)
					SendOutMessage(message);
			}

			private void ProcessTime(DateTimeOffset time, string boardCode)
			{
				if (boardCode.IsEmpty())
					return;

				var exchangeBoard = ExchangeBoard.GetBoard(boardCode);

				if (exchangeBoard == null)
					return;

				if (time <= exchangeBoard.Exchange.ToExchangeTime(_connectTime))
					return;

				_isInitialization = false;
				_strategy.SetIsInitialization(false);
			}

			protected override void OnProcessMessage(Message message)
			{
				//_historyMessageAdapter.UpdateCurrentTime(message.LocalTime);

				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						if (message.Adapter == MarketDataAdapter)
							break;

						_isHistory = true;
						_isInitialization = true;
						_connectTime = message.LocalTime;
						_strategy.SetIsInitialization(true);

						_historyMessageAdapter
							.SecurityProvider
							.LookupAll()
							.ForEach(s => SendOutMessage(s.ToMessage()));

						break;
					}

					case (MessageTypes)(-1):
					{
						new ChartAutoRangeCommand(false).Process(_strategy);

						_strategy.PositionManager.Positions = _sessionStrategy.Positions.Select(p => p.Position).ToList();

						if (_onlyInitialize)
						{
							if (message.Adapter == MarketDataAdapter)
								new StopStrategyCommand(_strategy).Process(this);

							return;
						}

						_historyMessageAdapter.StopDate = DateTimeOffset.MaxValue;
						_historyMessageAdapter.MarketTimeChangedInterval = TimeSpan.FromMilliseconds(10);

						var messages = new List<Message>();

						messages.AddRange(_realConnector.Trades.Select(t => t.ToMessage()));
						messages.AddRange(_realConnector.Orders.Select(o => o.ToMessage()));
						messages.AddRange(_realConnector.OrderRegisterFails.Select(o => o.ToMessage()));
						messages.AddRange(_realConnector.OrderCancelFails.Select(o => o.ToMessage()));
						messages.AddRange(_realConnector.MyTrades.Select(t => t.ToMessage()));

						messages.ForEach(SendOutMessage);

						_isHistory = false;

						return;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType == ExecutionTypes.Tick && !_isHistory && _isInitialization)
							ProcessTime(execMsg.ServerTime, execMsg.SecurityId.BoardCode);

						break;
					}

					default:
					{
						var candleMsg = message as CandleMessage;

						if (candleMsg == null)
							break;

						if (!_isHistory)
							break;

						var stocksharpId = CreateSecurityId(candleMsg.SecurityId.SecurityCode, candleMsg.SecurityId.BoardCode);
						var security = Securities.FirstOrDefault(s => s.Id.CompareIgnoreCase(stocksharpId));

						if (security == null)
							throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(candleMsg.SecurityId));

						var volumeStep = security.VolumeStep ?? 1m;
						var decimals = volumeStep.GetCachedDecimals();

						var trades = candleMsg.ToTrades(volumeStep, decimals);

						foreach (var executionMessage in trades)
							base.OnProcessMessage(executionMessage);

						return;
					}
				}

				base.OnProcessMessage(message);
			}

			public override void SubscribeMarketData(Security security, MarketDataTypes type)
			{
				if (_isHistory && type == MarketDataTypes.Trades)
				{
					SendInMessage(new MarketDataMessage
					{
						//SecurityId = GetSecurityId(security),
						DataType = MarketDataTypes.CandleTimeFrame,
						IsSubscribe = true,
						From = _historyMessageAdapter.StartDate,
						To = _historyMessageAdapter.StopDate,
						Arg = _useCandlesTimeFrame
					}.FillSecurityInfo(this, security));
				}

				_realConnector.SubscribeMarketData(security, type);
			}

			public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
			{
				_realConnector.UnSubscribeMarketData(security, type);
			}

			public override void LookupSecurities(SecurityLookupMessage criteria)
			{
				_realConnector.LookupSecurities(criteria);
			}

			public override void LookupPortfolios(Portfolio criteria)
			{
				_realConnector.LookupPortfolios(criteria);
			}

			public override IEnumerable<Security> Lookup(Security criteria)
			{
				return _securityProvider.Lookup(criteria);
			}

			#region Orders

			/// <summary>
			/// Зарегистрировать заявку на бирже.
			/// </summary>
			/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
			protected override void OnRegisterOrder(Order order)
			{
				var regMsg = order.CreateRegisterMessage(_realConnector.GetSecurityId(order.Security));

				var depoName = order.Portfolio.GetValue<string>(PositionChangeTypes.DepoName);
				if (depoName != null)
					regMsg.AddValue(PositionChangeTypes.DepoName, depoName);

				_realConnector.SendInMessage(regMsg);
			}

			/// <summary>
			/// Перерегистрировать заявку на бирже.
			/// </summary>
			/// <param name="oldOrder">Заявка, которую нужно снять.</param>
			/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
			protected override void OnReRegisterOrder(Order oldOrder, Order newOrder)
			{
				if (IsSupportAtomicReRegister && oldOrder.Security.Board.IsSupportAtomicReRegister)
					_realConnector.SendInMessage(oldOrder.CreateReplaceMessage(newOrder, _realConnector.GetSecurityId(newOrder.Security)));
				else
					base.OnReRegisterOrder(oldOrder, newOrder);
			}

			/// <summary>
			/// Перерегистрировать пару заявок на бирже.
			/// </summary>
			/// <param name="oldOrder1">Первая заявка, которую нужно снять.</param>
			/// <param name="newOrder1">Первая новая заявка, которую нужно зарегистрировать.</param>
			/// <param name="oldOrder2">Вторая заявка, которую нужно снять.</param>
			/// <param name="newOrder2">Вторая новая заявка, которую нужно зарегистрировать.</param>
			protected override void OnReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
			{
				if (IsSupportAtomicReRegister && oldOrder1.Security.Board.IsSupportAtomicReRegister)
					_realConnector.SendInMessage(oldOrder1.CreateReplaceMessage(newOrder1, _realConnector.GetSecurityId(newOrder1.Security), oldOrder2, newOrder2, _realConnector.GetSecurityId(newOrder2.Security)));
				else
					base.OnReRegisterOrderPair(oldOrder1, newOrder1, oldOrder2, newOrder2);
			}

			/// <summary>
			/// Отменить заявку на бирже.
			/// </summary>
			/// <param name="order">Заявка, которую нужно отменять.</param>
			/// <param name="transactionId">Идентификатор транзакции отмены.</param>
			protected override void OnCancelOrder(Order order, long transactionId)
			{
				_realConnector.SendInMessage(order.CreateCancelMessage(_realConnector.GetSecurityId(order.Security), transactionId));
			}

			#endregion

			protected override void DisposeManaged()
			{
				_realConnector.NewMessage -= RealConnectorNewMessage;

				base.DisposeManaged();
			}
		}

		private sealed class StorageCandleManager : CandleManager
		{
			private sealed class StorageSeriesInfo
			{
				private readonly IMarketDataStorage<Candle> _storage;
				private readonly SynchronizedList<Candle> _candles = new SynchronizedList<Candle>();
				private DateTimeOffset? _firstTime;

				public StorageSeriesInfo(CandleSeries series)
				{
					_storage = ConfigManager.GetService<IStorageRegistry>().GetCandleStorage(series, new LocalMarketDataDrive(UserConfig.Instance.CandleSeriesDumpPath));
				}

				public void AddCandle(Candle candle)
				{
					if (_firstTime == null)
						_firstTime = candle.OpenTime;

					lock (_candles.SyncRoot)
					{
						if ((candle.OpenTime.Date - _firstTime.Value.Date).TotalDays >= 3)
						{
							_firstTime = candle.OpenTime;
							FlushCandles(_candles.CopyAndClear());
						}

						_candles.Add(candle);
					}
				}

				public void FlushCandles()
				{

				}

				private void FlushCandles(IEnumerable<Candle> candles)
				{
					//_storage.Save(candles);
				}
			}

			private readonly SynchronizedDictionary<CandleSeries, StorageSeriesInfo> _candles = new SynchronizedDictionary<CandleSeries, StorageSeriesInfo>();
			//private const int _maxCandleCount = 50;

			public StorageCandleManager(IConnector connector)
				: base(connector)
			{
				//Sources.OfType<StorageCandleSource>().Single().Drive = new LocalMarketDataDrive(UserConfig.Instance.CandleSeriesDumpPath);

				Sources.OfType<StorageCandleSource>().Single().StorageRegistry = ConfigManager.GetService<IStorageRegistry>();
				Sources.Where(s => !(s is StorageCandleSource)).ForEach(s => s.Processing += OnProcessing);

				Stopped += OnStopped;
			}

			private void OnProcessing(CandleSeries series, Candle candle)
			{
				if (candle.State != CandleStates.Finished)
					return;

				//if (candle.TotalVolume == 0)
				//	throw new Exception();

				_candles.SafeAdd(series, key => new StorageSeriesInfo(key)).AddCandle(candle);
			}

			private void OnStopped(CandleSeries series)
			{
				var info = _candles.TryGetValue(series);

				if (info != null)
					info.FlushCandles();
			}

			protected override void DisposeManaged()
			{
				Sources.Where(s => !(s is StorageCandleSource)).ForEach(s => s.Processing -= OnProcessing);

				Stopped -= OnStopped;

				_candles.SyncDo(d =>
				{
					foreach (var pair in d)
					{
						pair.Value.FlushCandles();
					}
				});

				base.DisposeManaged();
			}
		}

		public IStudioConnector Connector
		{
			get { return ConfigManager.GetService<IStudioConnector>(); }
		}

		public StrategyService()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<StartStrategyCommand>(this, false, cmd =>
			{
				var error = cmd.Strategy.CheckCanStart(false);

				if (error != null)
				{
					GuiDispatcher.GlobalDispatcher.AddAction(() => new MessageBoxBuilder()
						.Error()
						.Text(error)
						.Owner(Application.Current.GetActiveOrMainWindow())
						.Show());
					
					return;
				}

				Start(cmd.Strategy, cmd.StartDate, cmd.StopDate, cmd.CandlesTimeFrame, cmd.OnlyInitialize);
			}, cmd => CanStart(cmd.Strategy));
			
			cmdSvc.Register<StopStrategyCommand>(this, false, cmd => Stop(cmd.Strategy), cmd => CanStop(cmd.Strategy));
			cmdSvc.Register<ResetStrategyCommand>(this, false, cmd => cmd.Strategy.Reset());
		}

		public void InitStrategy(Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			strategy.Connector = ConfigManager.GetService<IConnector>();
			//strategy.SetCandleManager(new StrategyCandleManager(_candleManager));
		}

		private CandleManager CreateCandleManager(IConnector connector, TimeSpan candlesKeepTime)
		{
			return new CandleManager(connector) { Container = { CandlesKeepTime = candlesKeepTime } };

			//var candleManager = new StorageCandleManager(connector) { Container = { CandlesKeepTime = TimeSpan.FromDays(1) } };

			//foreach (var type in AppConfig.Instance.CandleSources)
			//{
			//	var source = type.CreateInstance<object>();

			//	source.DoIf<object, ICandleManagerSource>(s => candleManager.Sources.Add(s));
			//	source.DoIf<object, ICandleBuilderSource>(s => candleManager.Sources.OfType<ICandleBuilder>().ForEach(b => b.Sources.Add(s)));
			//}

			//candleManager.StorageRegistry = ConfigManager.GetService<IStorageRegistry>();

			//return candleManager;
		}

		private bool CanStart(StrategyContainer strategy)
		{
			return strategy != null && strategy.ProcessState == ProcessStates.Stopped;
		}

		private void Start(StrategyContainer strategy, DateTime? startDate, DateTime? stopDate, TimeSpan? candlesTimeFrame, bool onlyInitialize)
		{
			if (Connector == null)
				throw new InvalidOperationException("Connector=null");

			strategy.CheckCanStart();

			var from = startDate ?? DateTime.Today.AddDays(-strategy.HistoryDaysCount);
			var to = stopDate ?? DateTime.Now;

			var strategyConnector = new StrategyConnector(strategy, from, to, candlesTimeFrame ?? TimeSpan.FromMinutes(5), onlyInitialize);

			ConfigManager.GetService<LogManager>().Sources.Add(strategyConnector);

			strategy.Connector = strategyConnector;
			strategy.SetCandleManager(CreateCandleManager(strategyConnector, TimeSpan.FromDays((strategy.HistoryDaysCount + 1) * 2)));
			strategy.SetIsEmulation(false);

			strategy.Start();
			strategyConnector.Connect();
		}

		private bool CanStop(StrategyContainer strategy)
		{
			return strategy != null && strategy.ProcessState == ProcessStates.Started;
		}

		private void Stop(StrategyContainer strategy)
		{
			strategy
				.WhenStopped()
				.Do(() =>
				{
					var candleManager = strategy.GetCandleManager();

					if (candleManager != null)
						candleManager.Dispose();

					strategy.SafeGetConnector().Dispose();
					strategy.Connector = ConfigManager.GetService<IConnector>();
				})
				.Once()
				.Apply();

			strategy.Stop();
		}
	}
}