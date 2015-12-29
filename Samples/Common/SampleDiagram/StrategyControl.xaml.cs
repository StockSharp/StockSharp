namespace SampleDiagram
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using SampleDiagram.Layout;

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

	/// <summary>
	/// Логика взаимодействия для LiveStrategyControl.xaml
	/// </summary>
	public partial class StrategyControl
	{
		#region DependencyProperty

		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(DiagramStrategy),
			typeof(StrategyControl), new PropertyMetadata(null, OnStrategyPropertyChanged));

		private static void OnStrategyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			((StrategyControl)s).OnStrategyChanged((DiagramStrategy)e.OldValue, (DiagramStrategy)e.NewValue);
		}

		public DiagramStrategy Strategy
		{
			get { return (DiagramStrategy)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		public static readonly DependencyProperty TitleConverterProperty = DependencyProperty.Register("TitleConverter", typeof(IValueConverter),
			typeof(StrategyControl), new PropertyMetadata(null));

		public IValueConverter TitleConverter
		{
			get { return (IValueConverter)GetValue(TitleConverterProperty); }
			set { SetValue(TitleConverterProperty, value); }
		}

		#endregion

		private readonly BufferedChart _bufferedChart;
		private readonly LayoutManager _layoutManager;
		private readonly ICollection<EquityData> _pnlCurve;
		private readonly ICollection<EquityData> _unrealizedPnLCurve;
		private readonly ICollection<EquityData> _commissionCurve;
		private readonly ICollection<EquityData> _posItems;

		public override string Key => Strategy.Id.ToString();

		public ICommand StartCommand { get; protected set; }

		public ICommand StopCommand { get; protected set; }

		public ICommand AddBreakpointCommand => DiagramDebuggerControl.AddBreakpointCommand;

		public ICommand RemoveBreakpointCommand => DiagramDebuggerControl.RemoveBreakpointCommand;

		public ICommand StepNextCommand => DiagramDebuggerControl.StepNextCommand;

		public ICommand StepIntoCommand => DiagramDebuggerControl.StepIntoCommand;

		public ICommand StepOutCommand => DiagramDebuggerControl.StepOutCommand;

		public ICommand ContinueCommand => DiagramDebuggerControl.ContinueCommand;

		public StrategyControl()
		{
			InitializeComponent();

			_bufferedChart = new BufferedChart(Chart);
			_layoutManager = new LayoutManager(DockingManager);

			_pnlCurve = Curve.CreateCurve(LocalizedStrings.PnL, Colors.DarkGreen, EquityCurveChartStyles.Area);
			_unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal, Colors.Black);
			_commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159, Colors.Red, EquityCurveChartStyles.DashedLine);

			_posItems = PositionCurve.CreateCurve(LocalizedStrings.Str862, Colors.DarkGreen);
		}

		protected void Reset()
		{
			OrderGrid.Orders.Clear();
			MyTradeGrid.Trades.Clear();

			_pnlCurve.Clear();
			_unrealizedPnLCurve.Clear();
			_commissionCurve.Clear();
			_posItems.Clear();
		}

		private void OnStrategyChanged(DiagramStrategy oldStrategy, DiagramStrategy newStrategy)
		{
			if (oldStrategy != null)
			{
				ConfigManager
					.GetService<LogManager>()
					.Sources
					.Remove(oldStrategy);

				oldStrategy.OrderRegistering += OnStrategyOrderRegistering;
				oldStrategy.OrderReRegistering += OnStrategyOrderReRegistering;
				oldStrategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;

				oldStrategy.StopOrderRegistering += OnStrategyOrderRegistering;
				oldStrategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
				oldStrategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

				oldStrategy.NewMyTrades += OnStrategyNewMyTrade;
			}

			DiagramDebuggerControl.Strategy = newStrategy;

			if (newStrategy == null)
				return;

			ConfigManager
				.GetService<LogManager>()
				.Sources
				.Add(newStrategy);

			newStrategy.OrderRegistering += OnStrategyOrderRegistering;
			newStrategy.OrderReRegistering += OnStrategyOrderReRegistering;
			newStrategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;

			newStrategy.StopOrderRegistering += OnStrategyOrderRegistering;
			newStrategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
			newStrategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

			newStrategy.NewMyTrades += OnStrategyNewMyTrade;

			newStrategy.PnLChanged += () =>
			{
				var pnl = new EquityData
				{
					Time = newStrategy.CurrentTime,
					Value = newStrategy.PnL - newStrategy.Commission ?? 0
				};

				var unrealizedPnL = new EquityData
				{
					Time = newStrategy.CurrentTime,
					Value = newStrategy.PnLManager.UnrealizedPnL
				};

				var commission = new EquityData
				{
					Time = newStrategy.CurrentTime,
					Value = newStrategy.Commission ?? 0
				};

				_pnlCurve.Add(pnl);
				_unrealizedPnLCurve.Add(unrealizedPnL);
				_commissionCurve.Add(commission);
			};

			newStrategy.PositionChanged += () => _posItems.Add(new EquityData
			{
				Time = newStrategy.CurrentTime,
				Value = newStrategy.Position
			});

			newStrategy.SetChart(_bufferedChart);
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

		private void OnDiagramDebuggerControlChanged()
		{
			RaiseChanged();
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			storage.TryLoadSettings<SettingsStorage>("DebuggerControl", s => DiagramDebuggerControl.Load(s));
			storage.TryLoadSettings<string>("Layout", s => _layoutManager.LoadLayout(s));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("DebuggerControl", DiagramDebuggerControl.Save());
			storage.SetValue("Layout", _layoutManager.SaveLayout());
		}

		#endregion
	}

	public class LiveStrategyControl : StrategyControl
	{
		public LiveStrategyControl()
		{
			InitializeCommands();

			this.SetBindings(TitleProperty, this, "Strategy.Composition.Name", BindingMode.OneWay, new TitleConverter(LocalizedStrings.Str3176));
		}

		private void InitializeCommands()
		{
			StartCommand = new DelegateCommand(
				obj =>
				{
					var connector = ConfigManager.GetService<IConnector>();

					Strategy.Connector = connector;

					Strategy.SetCandleManager(new CandleManager(connector));
					Strategy.Start();
				},
				obj => Strategy != null && Strategy.ProcessState == ProcessStates.Stopped);

			StopCommand = new DelegateCommand(
				obj => Strategy.Start(),
				obj => Strategy != null && Strategy.ProcessState == ProcessStates.Started);
		}

		public override bool CanClose()
		{
			if (Strategy == null || Strategy.ProcessState == ProcessStates.Stopped)
				return true;

			new MessageBoxBuilder()
				.Owner(this)
				.Caption(Title)
				.Text(LocalizedStrings.Str3617Params.Put(Title))
				.Icon(MessageBoxImage.Warning)
				.Button(MessageBoxButton.OK)
				.Show();

			return false;
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			var compositionId = storage.GetValue<Guid>("CompositionId");
			var registry = ConfigManager.GetService<StrategiesRegistry>();
			var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Strategy = new DiagramStrategy
			{
				Id = storage.GetValue<Guid>("StrategyId"),
				Composition = registry.Clone(composition)
			};

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			if (Strategy != null)
			{
				storage.SetValue("CompositionId", Strategy.Composition.TypeId);
				storage.SetValue("StrategyId", Strategy.Id);
			}

			base.Save(storage);
		}

		#endregion
	}

	public class EmulationStrategyControl : StrategyControl
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

		private HistoryEmulationConnector _connector;

		public EmulationStrategyControl()
		{
			InitializeCommands();

			this.SetBindings(TitleProperty, this, "Strategy.Composition.Name", BindingMode.OneWay, new TitleConverter(LocalizedStrings.Str1174));
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

		public override bool CanClose()
		{
			if (_connector == null || _connector.State == EmulationStates.Stopped)
				return true;

			new MessageBoxBuilder()
				.Owner(this)
				.Caption(Title)
				.Text(LocalizedStrings.Str3617Params.Put(Title))
				.Icon(MessageBoxImage.Warning)
				.Button(MessageBoxButton.OK)
				.Show();

			return false;
		}

		private void StartEmulation()
		{
			if (_connector != null && _connector.State != EmulationStates.Stopped)
				throw new InvalidOperationException(LocalizedStrings.Str3015);

			if (Strategy == null)
				throw new InvalidOperationException("Strategy not selected.");

			var strategy = (EmulationDiagramStrategy)Strategy;

			if (strategy.DataPath.IsEmpty() || !Directory.Exists(strategy.DataPath))
				throw new InvalidOperationException(LocalizedStrings.Str3014);

			strategy.Reset();
			Reset();

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

			strategy.SetCandleManager(candleManager);

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
								TicksAndDepthsProgress.Value = TicksAndDepthsProgress.Maximum;
						});
						break;
					case EmulationStates.Started:
						break;
				}
			};

			TicksAndDepthsProgress.Value = 0;

			DiagramDebuggerControl.Debugger.IsEnabled = true;

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

			DiagramDebuggerControl.Debugger.IsEnabled = false;

			if (DiagramDebuggerControl.Debugger.IsWaiting)
				DiagramDebuggerControl.Debugger.Continue();
		}

		#region IPersistable

		public override void Load(SettingsStorage storage)
		{
			var compositionId = storage.GetValue<Guid>("CompositionId");
			var registry = ConfigManager.GetService<StrategiesRegistry>();
			var composition = (CompositionDiagramElement)registry.Strategies.FirstOrDefault(c => c.TypeId == compositionId);

			Strategy = new EmulationDiagramStrategy
			{
				Id = storage.GetValue<Guid>("StrategyId"),
				DataPath = storage.GetValue<string>("DataPath"),
				StartDate = storage.GetValue<DateTime>("StartDate"),
				StopDate = storage.GetValue<DateTime>("StopDate"),
				SecurityId = storage.GetValue<string>("SecurityId"),
				MarketDataSource = storage.GetValue<MarketDataSource>("MarketDataSource"),
				CandlesTimeFrame = storage.GetValue<TimeSpan>("CandlesTimeFrame"),
				Composition = registry.Clone(composition)
			};

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			if (Strategy != null)
			{
				var strategy = (EmulationDiagramStrategy)Strategy;

				storage.SetValue("CompositionId", strategy.Composition.TypeId);
				storage.SetValue("StrategyId", strategy.Id);
				storage.SetValue("DataPath", strategy.DataPath);
				storage.SetValue("StartDate", strategy.StartDate);
				storage.SetValue("StopDate", strategy.StopDate);
				storage.SetValue("SecurityId", strategy.SecurityId);
				storage.SetValue("MarketDataSource", strategy.MarketDataSource);
				storage.SetValue("CandlesTimeFrame", strategy.CandlesTimeFrame);
			}

			base.Save(storage);
		}

		#endregion
	}
}
