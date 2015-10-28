namespace SampleDiagramPublic
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Diagram;

	/// <summary>
	/// Interaction logic for EmulationControl.xaml
	/// </summary>
	public partial class EmulationControl
	{
		#region DependencyProperty

		public static readonly DependencyProperty StrategiesRegistryProperty = DependencyProperty.Register("StrategiesRegistry", typeof(StrategiesRegistry), typeof(EmulationControl),
			new PropertyMetadata(null));

		public StrategiesRegistry StrategiesRegistry
		{
			get { return (StrategiesRegistry)GetValue(StrategiesRegistryProperty); }
			set { SetValue(StrategiesRegistryProperty, value); }
		}

		public static readonly DependencyProperty CompositionProperty = DependencyProperty.Register("Composition", typeof(CompositionDiagramElement), typeof(EmulationControl),
			new PropertyMetadata(null));

		public CompositionDiagramElement Composition
		{
			get { return (CompositionDiagramElement)GetValue(CompositionProperty); }
			set { SetValue(CompositionProperty, value); }
		}

		#endregion

		private readonly BufferedChart _bufferedChart;
		private readonly LogManager _logManager;

		private HistoryEmulationConnector _connector;

		public EmulationControl()
		{
			InitializeComponent();

			HistoryPathTextBox.Text = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
			SecusityTextBox.Text = "RIZ2@FORTS";
			FromDatePicker.Value = new DateTime(2012, 10, 1);
			ToDatePicke.Value = new DateTime(2012, 10, 25);

			_bufferedChart = new BufferedChart(Chart);

			_logManager = new LogManager();
			_logManager.Listeners.Add(new FileLogListener("sample.log"));
			_logManager.Listeners.Add(new GuiLogListener(Monitor));
			//logManager.Listeners.Add(new DebugLogListener());	// for track logs in output window in Vusial Studio (poor performance).
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!HistoryPathTextBox.Text.IsEmpty())
				dlg.SelectedPath = HistoryPathTextBox.Text;

			if (dlg.ShowDialog(Application.Current.MainWindow) == true)
			{
				HistoryPathTextBox.Text = dlg.SelectedPath;
			}
		}

		private void StartButtonOnClick(object sender, RoutedEventArgs e)
		{
			_logManager.Sources.Clear();
			_bufferedChart.ClearAreas();

			Curve.Clear();
			PositionCurve.Clear();

			if (HistoryPathTextBox.Text.IsEmpty() || !Directory.Exists(HistoryPathTextBox.Text))
			{
				MessageBox.Show("Wrong path.");
				return;
			}

			if (_connector != null && _connector.State != EmulationStates.Stopped)
			{
				MessageBox.Show("Already launched.");
				return;
			}

			var secGen = new SecurityIdGenerator();
			var secIdParts = secGen.Split(SecusityTextBox.Text);
			var secCode = secIdParts.Item1;
			var board = ExchangeBoard.GetOrCreateBoard(secIdParts.Item2);
			var timeFrame = TimeSpan.FromMinutes(5);

			// create test security
			var security = new Security
			{
				Id = SecusityTextBox.Text, // sec id has the same name as folder with historical data
				Code = secCode,
				Board = board,
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPathTextBox.Text)
			};

			var startTime = ((DateTime)FromDatePicker.Value).ChangeKind(DateTimeKind.Utc);
			var stopTime = ((DateTime)ToDatePicke.Value).ChangeKind(DateTimeKind.Utc);

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
			.TryAdd(Level1Fields.PriceStep, secIdParts.Item1 == "RIZ2" ? 10m : 1)
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

				UseExternalCandleSource = false,

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

			_logManager.Sources.Add(_connector);

			var candleManager = new CandleManager(_connector);

			// create strategy based on 80 5-min и 10 5-min
			var strategy = new DiagramStrategy
			{
				Volume = 1,
				Portfolio = portfolio,
				Security = security,
				Connector = _connector,
				//LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info,

				Composition = Composition,

				// by default interval is 1 min,
				// it is excessively for time range with several months
				UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>()
			};

			strategy.SetChart(_bufferedChart);
			strategy.SetCandleManager(candleManager);

			_logManager.Sources.Add(strategy);

			strategy.OrderRegistering += OnStrategyOrderRegistering;
			strategy.OrderReRegistering += OnStrategyOrderReRegistering;
			strategy.OrderRegisterFailed += OnStrategyOrderRegisterFailed;
			
			strategy.StopOrderRegistering += OnStrategyOrderRegistering;
			strategy.StopOrderReRegistering += OnStrategyOrderReRegistering;
			strategy.StopOrderRegisterFailed += OnStrategyOrderRegisterFailed;

			strategy.NewMyTrades += OnStrategyNewMyTrade;

			var pnlCurve = Curve.CreateCurve(LocalizedStrings.PnL + " " + strategy.Name, Colors.DarkGreen, EquityCurveChartStyles.Area);
			var unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal + strategy.Name, Colors.Black);
			var commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159 + " " + strategy.Name, Colors.Red, EquityCurveChartStyles.DashedLine);
			
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

			strategy.PositionChanged += () => posItems.Add(new EquityData { Time = strategy.CurrentTime, Value = strategy.Position });

			_connector.NewSecurities += securities =>
			{
				if (securities.All(s => s != security))
					return;

				// fill level1 values
				_connector.SendInMessage(level1Info);

				//_connector.RegisterMarketDepth(security);
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
						SetIsEnabled(false);

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
						SetIsEnabled(true);
						break;
				}
			};

			TicksAndDepthsProgress.Value = 0;

			// raise NewSecurities and NewPortfolio for full fill strategy properties
			_connector.Connect();

			// 1 cent commission for trade
			_connector.SendInMessage(new CommissionRuleMessage
			{
				Rule = new CommissionPerTradeRule { Value = 0.01m }
			});
		}

		private void StopButtonOnClick(object sender, RoutedEventArgs e)
		{
			_connector.Disconnect();
		}

		private void SetIsEnabled(bool started)
		{
			this.GuiAsync(() =>
			{
				StopButton.IsEnabled = started;
				StartButton.IsEnabled = !started;
			});
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

		private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var element = (CompositionDiagramElement)StrategiesComboBox.SelectedItem;
			
			Composition = element != null ? StrategiesRegistry.Clone(element) : null;
		}

		private void ResetStrategyClick(object sender, RoutedEventArgs e)
		{
			StrategiesComboBox.SelectedItem = null;
		}
	}
}
