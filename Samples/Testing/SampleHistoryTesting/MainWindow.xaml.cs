namespace SampleHistoryTesting
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Collections.Generic;

	using Ecng.Xaml;
	using Ecng.Common;
	using Ecng.Collections;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		// emulation settings
		private sealed class EmulationInfo
		{
			public bool UseTicks { get; set; }
			public bool UseMarketDepth { get; set; }
			public TimeSpan? UseCandleTimeFrame { get; set; }
			public Color CurveColor { get; set; }
			public string StrategyName { get; set; }
			public bool UseOrderLog { get; set; }
		}

		private readonly List<HistoryEmulationConnector> _connectors = new List<HistoryEmulationConnector>();
		private readonly BufferedChart _bufferedChart;
		
		private DateTime _startEmulationTime;
		private ChartCandleElement _candlesElem;
		private ChartTradeElement _tradesElem;
		private ChartIndicatorElement _shortElem;
		private SimpleMovingAverage _shortMa;
		private ChartIndicatorElement _longElem;
		private SimpleMovingAverage _longMa;
		private ChartArea _area;

		public MainWindow()
		{
			InitializeComponent();

			_bufferedChart = new BufferedChart(Chart);

			HistoryPath.Text = @"..\..\..\HistoryData\".ToFullPath();

			From.Value = new DateTime(2012, 10, 1);
			To.Value = new DateTime(2012, 10, 25);
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!HistoryPath.Text.IsEmpty())
				dlg.SelectedPath = HistoryPath.Text;

			if (dlg.ShowDialog(this) == true)
			{
				HistoryPath.Text = dlg.SelectedPath;
			}
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			InitChart();

			if (HistoryPath.Text.IsEmpty() || !Directory.Exists(HistoryPath.Text))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			if (_connectors.Any(t => t.State != EmulationStates.Stopped))
			{
				MessageBox.Show(this, LocalizedStrings.Str3015);
				return;
			}

			var secIdParts = SecId.Text.Split('@');

			if (secIdParts.Length != 2)
			{
				MessageBox.Show(this, LocalizedStrings.Str3016);
				return;
			}

			var timeFrame = TimeSpan.FromMinutes(5);

			// create backtesting modes
			var settings = new[]
			{
				Tuple.Create(
					TicksCheckBox, 
					TicksTestingProcess, 
					TicksParameterGrid,
					// ticks
					new EmulationInfo {UseTicks = true, CurveColor = Colors.DarkGreen, StrategyName = LocalizedStrings.Str3017}),

				Tuple.Create(
					TicksAndDepthsCheckBox, 
					TicksAndDepthsTestingProcess, 
					TicksAndDepthsParameterGrid,
					// ticks + order book
					new EmulationInfo {UseTicks = true, UseMarketDepth = true, CurveColor = Colors.Red, StrategyName = LocalizedStrings.Str3018}),

				Tuple.Create(
					CandlesCheckBox, 
					CandlesTestingProcess, 
					CandlesParameterGrid,
					// candles
					new EmulationInfo {UseCandleTimeFrame = timeFrame, CurveColor = Colors.DarkBlue, StrategyName = LocalizedStrings.Str3019}),
				
				Tuple.Create(
					CandlesAndDepthsCheckBox, 
					CandlesAndDepthsTestingProcess, 
					CandlesAndDepthsParameterGrid,
					// candles + orderbook
					new EmulationInfo {UseMarketDepth = true, UseCandleTimeFrame = timeFrame, CurveColor = Colors.Cyan, StrategyName = LocalizedStrings.Str3020}),
			
				Tuple.Create(
					OrderLogCheckBox, 
					OrderLogTestingProcess, 
					OrderLogParameterGrid,
					// order log
					new EmulationInfo {UseOrderLog = true, CurveColor = Colors.CornflowerBlue, StrategyName = LocalizedStrings.Str3021})
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Text)
			};

			var startTime = (DateTime)From.Value;
			var stopTime = (DateTime)To.Value;

			// ОЛ необходимо загружать с 18.45 пред дня, чтобы стаканы строились правильно
			if (OrderLogCheckBox.IsChecked == true)
				startTime = startTime.Subtract(TimeSpan.FromDays(1)).AddHours(18).AddMinutes(45).AddTicks(1);

			// ProgressBar refresh step
			var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();

			// set ProgressBar bounds
			TicksTestingProcess.Maximum = TicksAndDepthsTestingProcess.Maximum = CandlesTestingProcess.Maximum = 100;
			TicksTestingProcess.Value = TicksAndDepthsTestingProcess.Value = CandlesTestingProcess.Value = 0;

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);
			//logManager.Listeners.Add(new DebugLogListener());	// for track logs in output window in Vusial Studio (poor performance).

			var generateDepths = GenDepthsCheckBox.IsChecked == true;
			var maxDepth = MaxDepth.Text.To<int>();
			var maxVolume = MaxVolume.Text.To<int>();

			var secCode = secIdParts[0];
			var board = ExchangeBoard.GetOrCreateBoard(secIdParts[1]);

			foreach (var set in settings)
			{
				if (set.Item1.IsChecked == false)
					continue;

				var progressBar = set.Item2;
				var statistic = set.Item3;
				var emulationInfo = set.Item4;

				// create test security
				var security = new Security
				{
					Id = SecId.Text, // sec id has the same name as folder with historical data
					Code = secCode,
					Board = board,
				};

				var level1Info = new Level1ChangeMessage
				{
					SecurityId = security.ToSecurityId(),
					ServerTime = startTime,
				}
				.TryAdd(Level1Fields.PriceStep, 10m)
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
				var connector = new HistoryEmulationConnector(
					new[] { security },
					new[] { portfolio })
				{
					StorageRegistry = storageRegistry,

					MarketEmulator =
					{
						Settings =
						{
							// set time frame is backtesting on candles
							UseCandlesTimeFrame =  emulationInfo.UseCandleTimeFrame,

							// match order if historical price touched our limit order price. 
							// It is terned off, and price should go through limit order price level
							// (more "severe" test mode)
							MatchOnTouch = false,
						}
					},

					//UseExternalCandleSource = true,
					CreateDepthFromOrdersLog = emulationInfo.UseOrderLog,
					CreateTradesFromOrdersLog = emulationInfo.UseOrderLog,
				};

				connector.StartDate = startTime;
				connector.StopDate = stopTime;

				connector.MarketTimeChangedInterval = timeFrame;

				((ILogSource)connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

				logManager.Sources.Add(connector);

				connector.NewSecurities += securities =>
				{
					if (securities.All(s => s != security))
						return;

					// fill level1 values
					connector.SendOutMessage(level1Info);

					if (emulationInfo.UseMarketDepth)
					{
						connector.RegisterMarketDepth(security);

						if (
								// if order book will be generated
								generateDepths ||
								// of backtesting will be on candles
								emulationInfo.UseCandleTimeFrame != TimeSpan.Zero
							)
						{
							// if no have order book historical data, but strategy is required,
							// use generator based on last prices
							connector.RegisterMarketDepth(new TrendMarketDepthGenerator(connector.GetSecurityId(security))
							{
								Interval = TimeSpan.FromSeconds(1), // order book freq refresh is 1 sec
								MaxAsksDepth = maxDepth,
								MaxBidsDepth = maxDepth,
								UseTradeVolume = true,
								MaxVolume = maxVolume,
								MinSpreadStepCount = 2,	// min spread generation is 2 pips
								MaxSpreadStepCount = 5,	// max spread generation size (prevent extremely size)
								MaxPriceStepCount = 3	// pips size,
							});
						}
					}

					if (emulationInfo.UseOrderLog)
					{
						connector.RegisterOrderLog(security);
					}

					if (emulationInfo.UseTicks)
					{
						connector.RegisterTrades(security);
					}

					// start historical data loading when connection established successfully and all data subscribed
					connector.Start();
				};

				var candleManager = new CandleManager(connector);
				var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);

				_shortMa = new SimpleMovingAverage { Length = 10 };
				_shortElem = new ChartIndicatorElement
				{
					Color = Colors.Coral,
					ShowAxisMarker = false,
					FullTitle = _shortMa.ToString()
				};
				_bufferedChart.AddElement(_area, _shortElem);

				_longMa = new SimpleMovingAverage { Length = 80 };
				_longElem = new ChartIndicatorElement
				{
					ShowAxisMarker = false,
					FullTitle = _longMa.ToString()
				};
				_bufferedChart.AddElement(_area, _longElem);

				// create strategy based on 80 5-min и 10 5-min
				var strategy = new SmaStrategy(_bufferedChart, _candlesElem, _tradesElem, _shortMa, _shortElem, _longMa, _longElem, series)
				{
					Volume = 1,
					Portfolio = portfolio,
					Security = security,
					Connector = connector,
					LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info,

					// by default interval is 1 min,
					// it is excessively for time range with several months
					UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>()
				};

				logManager.Sources.Add(strategy);

				// fill parameters panel
				statistic.Parameters.Clear();
				statistic.Parameters.AddRange(strategy.StatisticManager.Parameters);

				var pnlCurve = Curve.CreateCurve("P&L " + emulationInfo.StrategyName, emulationInfo.CurveColor, EquityCurveChartStyles.Area);
				var unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal + emulationInfo.StrategyName, Colors.Black);
				var commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159 + " " + emulationInfo.StrategyName, Colors.Red, EquityCurveChartStyles.DashedLine);
				var posItems = PositionCurve.CreateCurve(emulationInfo.StrategyName, emulationInfo.CurveColor);
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

				strategy.PositionChanged += () => posItems.Add(new EquityData { Time = strategy.CurrentTime, Value = strategy.Position });

				var nextTime = startTime + progressStep;

				// handle historical time for update ProgressBar
				connector.MarketTimeChanged += d =>
				{
					if (connector.CurrentTime < nextTime && connector.CurrentTime < stopTime)
						return;

					var steps = (connector.CurrentTime - startTime).Ticks / progressStep.Ticks + 1;
					nextTime = startTime + (steps * progressStep.Ticks).To<TimeSpan>();
					this.GuiAsync(() => progressBar.Value = steps);
				};

				connector.StateChanged += () =>
				{
					if (connector.State == EmulationStates.Stopped)
					{
						candleManager.Stop(series);
						strategy.Stop();

						logManager.Dispose();
						_connectors.Clear();

						SetIsEnabled(false);

						this.GuiAsync(() =>
						{
							if (connector.IsFinished)
							{
								progressBar.Value = progressBar.Maximum;
								MessageBox.Show(LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
							}
							else
								MessageBox.Show(LocalizedStrings.cancelled);
						});
					}
					else if (connector.State == EmulationStates.Started)
					{
						SetIsEnabled(true);

						// start strategy when emulation started
						strategy.Start();
						candleManager.Start(series);
					}
				};

				if (ShowDepth.IsChecked == true)
				{
					MarketDepth.UpdateFormat(security);

					connector.NewMessage += (message, dir) =>
					{
						var quoteMsg = message as QuoteChangeMessage;

						if (quoteMsg != null)
							MarketDepth.UpdateDepth(quoteMsg);
					};
				}

				_connectors.Add(connector);
			}

			_startEmulationTime = DateTime.Now;

			// start emulation
			foreach (var connector in _connectors)
			{
				// raise NewSecurities and NewPortfolio for full fill strategy properties
				connector.Connect();

				// 1 cent commission for trade
				connector.SendInMessage(new CommissionRuleMessage
				{
					Rule = new CommissionPerTradeRule { Value = 0.01m }
				});
			}

			TabControl.Items.Cast<TabItem>().First(i => i.Visibility == Visibility.Visible).IsSelected = true;
		}

		private void CheckBoxClick(object sender, RoutedEventArgs e)
		{
			var isEnabled = TicksCheckBox.IsChecked == true ||
			                TicksAndDepthsCheckBox.IsChecked == true ||
			                CandlesCheckBox.IsChecked == true ||
			                CandlesAndDepthsCheckBox.IsChecked == true ||
			                OrderLogCheckBox.IsChecked == true;

			StartBtn.IsEnabled = isEnabled;
			TabControl.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
		}

		private void StopBtnClick(object sender, RoutedEventArgs e)
		{
			foreach (var connector in _connectors)
			{
				connector.Disconnect();
			}
		}

		private void InitChart()
		{
			_bufferedChart.ClearAreas();
			Curve.Clear();
			PositionCurve.Clear();

			_area = new ChartArea();
			_bufferedChart.AddArea(_area);

			_candlesElem = new ChartCandleElement { ShowAxisMarker = false };
			_bufferedChart.AddElement(_area, _candlesElem);

			_tradesElem = new ChartTradeElement { FullTitle = "Сделки" };
			_bufferedChart.AddElement(_area, _tradesElem);
		}

		private void SetIsEnabled(bool started)
		{
			this.GuiAsync(() =>
			{
				StopBtn.IsEnabled = started;
				StartBtn.IsEnabled = !started;
				TicksCheckBox.IsEnabled = TicksAndDepthsCheckBox.IsEnabled = CandlesCheckBox.IsEnabled
					= CandlesAndDepthsCheckBox.IsEnabled = OrderLogCheckBox.IsEnabled = !started;

				_bufferedChart.IsAutoRange = started;
			});
		}
	}
}