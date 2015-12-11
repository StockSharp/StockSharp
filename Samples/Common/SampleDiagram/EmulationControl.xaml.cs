namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Diagram;

	public partial class EmulationControl
	{
		private class TradeCandleBuilderSourceEx : TradeCandleBuilderSource
		{
			public TradeCandleBuilderSourceEx(IConnector connector)
				: base(connector)
			{
			}

			protected override void RegisterSecurity(Security security)
			{
			}

			protected override void UnRegisterSecurity(Security security)
			{
			}
		}

		#region DependencyProperty

		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(EmulationDiagramStrategy), 
			typeof(EmulationControl), new PropertyMetadata(null));

		public EmulationDiagramStrategy Strategy
		{
			get { return (EmulationDiagramStrategy)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		#endregion

		private readonly BufferedChart _bufferedChart;

		private HistoryEmulationConnector _connector;

		public override object Key => Strategy;

		public ICommand StartCommand { get; private set; }

		public ICommand StopCommand { get; private set; }

		public ICommand AddBreakpointCommand => DiagramDebuggerControl.AddBreakpointCommand;

		public ICommand RemoveBreakpointCommand => DiagramDebuggerControl.RemoveBreakpointCommand;

		public ICommand StepNextCommand => DiagramDebuggerControl.StepNextCommand;

		public ICommand StepToOutParamCommand => DiagramDebuggerControl.StepToOutParamCommand;

		public ICommand StepIntoCommand => DiagramDebuggerControl.StepIntoCommand;

		public ICommand StepOutCommand => DiagramDebuggerControl.StepOutCommand;

		public ICommand ContinueCommand => DiagramDebuggerControl.ContinueCommand;

		public EmulationControl()
		{
			InitializeCommands();
            InitializeComponent();

			_bufferedChart = new BufferedChart(Chart);
		}

		private void InitializeCommands()
		{
			StartCommand = new DelegateCommand(
				obj => StartEmulation(),
				obj => _connector == null || _connector.State == EmulationStates.Stopped);

			StopCommand = new DelegateCommand(
				obj => StopEmulation(),
				obj => _connector != null && _connector.State == EmulationStates.Started);
		}

		private void StartEmulation()
		{
			if (_connector != null && _connector.State != EmulationStates.Stopped)
			{
				MessageBox.Show("Already launched.");
				return;
			}

			if (Strategy == null)
			{
				MessageBox.Show("No strategy selected.");
				return;
			}

			var strategy = Strategy;

			if (strategy.DataPath.IsEmpty() || !Directory.Exists(strategy.DataPath))
			{
				MessageBox.Show("Wrong path.");
				return;
			}

			_bufferedChart.ClearAreas();

			Curve.Clear();
			PositionCurve.Clear();

			var secGen = new SecurityIdGenerator();
			var secIdParts = secGen.Split(strategy.SecurityId);
			var secCode = secIdParts.SecurityCode;
			var board = ExchangeBoard.GetOrCreateBoard(secIdParts.BoardCode);
			var timeFrame = strategy.CandlesTimeFrame;
			var useCandles = strategy.MarketDataSource == MarketDataSource.Candles;

			// create test security
			var security = new Security
			{
				Id = strategy.SecurityId, // sec id has the same name as folder with historical data
				Code = secCode,
				Board = board,
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(strategy.DataPath)
			};

			var startTime = strategy.StartDate.ChangeKind(DateTimeKind.Utc);
			var stopTime = strategy.StopDate.ChangeKind(DateTimeKind.Utc);

			// ProgressBar refresh step
			var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();

			// set ProgressBar bounds
			TicksAndDepthsProgress.Value = 0;
			TicksAndDepthsProgress.Maximum = 100;

			var level1Info = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = startTime,
			}
				.TryAdd(Level1Fields.PriceStep, secIdParts.SecurityCode == "RIZ2" ? 10m : 1)
				.TryAdd(Level1Fields.StepPrice, 6m)
				.TryAdd(Level1Fields.MinPrice, 10m)
				.TryAdd(Level1Fields.MaxPrice, 1000000m)
				.TryAdd(Level1Fields.MarginBuy, 10000m)
				.TryAdd(Level1Fields.MarginSell, 10000m);

			// test portfolio
			var portfolio = new Portfolio
			{
				Name = "test account",
				BeginValue = 1000000,
			};

			// create backtesting connector
			_connector = new HistoryEmulationConnector(
				new[] { security },
				new[] { portfolio })
			{
				EmulationAdapter =
				{
					Emulator =
					{
						Settings =
						{
							// match order if historical price touched our limit order price. 
							// It is terned off, and price should go through limit order price level
							// (more "severe" test mode)
							MatchOnTouch = false,
						}
					}
				},

				UseExternalCandleSource = useCandles,

				HistoryMessageAdapter =
				{
					StorageRegistry = storageRegistry,

					// set history range
					StartDate = startTime,
					StopDate = stopTime,
				},

				// set market time freq as time frame
				MarketTimeChangedInterval = timeFrame,
			};

			//((ILogSource)_connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

			ConfigManager.GetService<LogManager>().Sources.Add(_connector);

			var candleManager = !useCandles
									? new CandleManager(new TradeCandleBuilderSourceEx(_connector))
									: new CandleManager(_connector);

			strategy.Volume = 1;
			strategy.Portfolio = portfolio;
			strategy.Security = security;
			strategy.Connector = _connector;
			//LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info,

			// by default interval is 1 min,
			// it is excessively for time range with several months
			strategy.UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>();

			strategy.SetChart(_bufferedChart);
			strategy.SetCandleManager(candleManager);

			ConfigManager.GetService<LogManager>().Sources.Add(strategy);

			strategy.OrderRegistering += OnStrategyOrderRegistering;
			strategy.OrderReRegistering += OnStrategyOrderReRegistering;
			strategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;

			strategy.StopOrderRegistering += OnStrategyOrderRegistering;
			strategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
			strategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

			strategy.NewMyTrades += OnStrategyNewMyTrade;

			var pnlCurve = Curve.CreateCurve(LocalizedStrings.PnL + " " + strategy.Name, Colors.DarkGreen,
											 EquityCurveChartStyles.Area);
			var unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal + strategy.Name, Colors.Black);
			var commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159 + " " + strategy.Name, Colors.Red,
													EquityCurveChartStyles.DashedLine);

			strategy.PnLChanged += () =>
			{
				var pnl = new EquityData
				{
					Time = strategy.CurrentTime,
					Value = strategy.PnL - strategy.Commission ?? 0
				};

				var unrealizedPnL = new EquityData
				{
					Time = strategy.CurrentTime,
					Value = strategy.PnLManager.UnrealizedPnL
				};

				var commission = new EquityData
				{
					Time = strategy.CurrentTime,
					Value = strategy.Commission ?? 0
				};

				pnlCurve.Add(pnl);
				unrealizedPnLCurve.Add(unrealizedPnL);
				commissionCurve.Add(commission);
			};

			var posItems = PositionCurve.CreateCurve(strategy.Name, Colors.DarkGreen);

			strategy.PositionChanged += () => posItems.Add(new EquityData
			{
				Time = strategy.CurrentTime,
				Value = strategy.Position
			});

			_connector.NewSecurities += securities =>
			{
				if (securities.All(s => s != security))
					return;

				// fill level1 values
				_connector.SendInMessage(level1Info);

				//_connector.RegisterMarketDepth(security);
				if (!useCandles)
					_connector.RegisterTrades(security);

				// start strategy before emulation started
				strategy.Start();

				// start historical data loading when connection established successfully and all data subscribed
				_connector.Start();
			};

			var nextTime = startTime + progressStep;

			// handle historical time for update ProgressBar
			_connector.MarketTimeChanged += d =>
			{
				if (_connector.CurrentTime < nextTime && _connector.CurrentTime < stopTime)
					return;

				var steps = (_connector.CurrentTime - startTime).Ticks / progressStep.Ticks + 1;
				nextTime = startTime + (steps * progressStep.Ticks).To<TimeSpan>();
				this.GuiAsync(() => TicksAndDepthsProgress.Value = steps);
			};

			_connector.StateChanged += () =>
			{
				switch (_connector.State)
				{
					case EmulationStates.Stopped:
						strategy.Stop();

						this.GuiAsync(() =>
						{
							if (_connector.IsFinished)
							{
								TicksAndDepthsProgress.Value = TicksAndDepthsProgress.Maximum;
								MessageBox.Show("Done.");
							}
							else
								MessageBox.Show("Cancelled.");
						});
						break;
					case EmulationStates.Started:
						break;
				}
			};

			TicksAndDepthsProgress.Value = 0;

			// raise NewSecurities and NewPortfolio for full fill strategy properties
			_connector.Connect();

			// 1 cent commission for trade
			_connector.SendInMessage(new CommissionRuleMessage
			{
				Rule = new CommissionPerTradeRule
				{
					Value = 0.01m
				}
			});
		}

		private void StopEmulation()
		{
			_connector.Disconnect();
		}

		private void OnStrategyOrderRegisterFailed(OrderFail fail)
		{
			OrderGrid.AddRegistrationFail(fail);
		}

		private void OnStrategyOrderReRegistering(Order oldOrder, Order newOrder)
		{
			OrderGrid.Orders.Add(newOrder);
		}

		private void OnStrategyOrderRegistering(Order order)
		{
			OrderGrid.Orders.Add(order);
		}

		private void OnStrategyNewMyTrade(IEnumerable<MyTrade> trades)
		{
			MyTradeGrid.Trades.AddRange(trades);
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			var compositionId = storage.GetValue<Guid>("CompositionId");

			var registry = ConfigManager.GetService<StrategiesRegistry>();
			var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Strategy = new EmulationDiagramStrategy
			{
				DataPath = storage.GetValue<string>("DataPath"),
				StartDate = storage.GetValue<DateTime>("StartDate"),
				StopDate = storage.GetValue<DateTime>("StopDate"),
				SecurityId = storage.GetValue<string>("SecurityId"),
				MarketDataSource = storage.GetValue<MarketDataSource>("MarketDataSource"),
				CandlesTimeFrame = storage.GetValue<TimeSpan>("CandlesTimeFrame"),
				Composition = registry.Clone(composition)
			};
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Strategy == null)
				return;

			storage.SetValue("CompositionId", Strategy.Composition.TypeId);
			storage.SetValue("DataPath", Strategy.DataPath);
			storage.SetValue("StartDate", Strategy.StartDate);
			storage.SetValue("StopDate", Strategy.StopDate);
			storage.SetValue("SecurityId", Strategy.SecurityId);
			storage.SetValue("MarketDataSource", Strategy.MarketDataSource);
			storage.SetValue("CandlesTimeFrame", Strategy.CandlesTimeFrame);
		}

		#endregion
	}
}
