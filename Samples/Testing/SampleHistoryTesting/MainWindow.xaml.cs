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
	using StockSharp.Algo.Candles.Compression;
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

		private readonly List<ProgressBar> _progressBars = new List<ProgressBar>();
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

			_progressBars.AddRange(new[]
			{
				TicksTestingProcess,
				TicksAndDepthsTestingProcess,
				DepthsTestingProcess,
				CandlesTestingProcess,
				CandlesAndDepthsTestingProcess,
				OrderLogTestingProcess
			});
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
					new EmulationInfo {UseTicks = true, CurveColor = Colors.DarkGreen, StrategyName = LocalizedStrings.Ticks}),

				Tuple.Create(
					TicksAndDepthsCheckBox,
					TicksAndDepthsTestingProcess,
					TicksAndDepthsParameterGrid,
					// ticks + order book
					new EmulationInfo {UseTicks = true, UseMarketDepth = true, CurveColor = Colors.Red, StrategyName = LocalizedStrings.XamlStr757}),

				Tuple.Create(
					DepthsCheckBox,
					DepthsTestingProcess,
					DepthsParameterGrid,
					// order book
					new EmulationInfo {UseMarketDepth = true, CurveColor = Colors.OrangeRed, StrategyName = LocalizedStrings.MarketDepths}),


				Tuple.Create(
					CandlesCheckBox,
					CandlesTestingProcess,
					CandlesParameterGrid,
					// candles
					new EmulationInfo {UseCandleTimeFrame = timeFrame, CurveColor = Colors.DarkBlue, StrategyName = LocalizedStrings.Candles}),
				
				Tuple.Create(
					CandlesAndDepthsCheckBox,
					CandlesAndDepthsTestingProcess,
					CandlesAndDepthsParameterGrid,
					// candles + orderbook
					new EmulationInfo {UseMarketDepth = true, UseCandleTimeFrame = timeFrame, CurveColor = Colors.Cyan, StrategyName = LocalizedStrings.XamlStr635}),
			
				Tuple.Create(
					OrderLogCheckBox,
					OrderLogTestingProcess,
					OrderLogParameterGrid,
					// order log
					new EmulationInfo {UseOrderLog = true, CurveColor = Colors.CornflowerBlue, StrategyName = LocalizedStrings.OrderLog})
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Text)
			};

			var startTime = ((DateTime)From.Value).ChangeKind(DateTimeKind.Utc);
			var stopTime = ((DateTime)To.Value).ChangeKind(DateTimeKind.Utc);

			// ОЛ необходимо загружать с 18.45 пред дня, чтобы стаканы строились правильно
			if (OrderLogCheckBox.IsChecked == true)
				startTime = startTime.Subtract(TimeSpan.FromDays(1)).AddHours(18).AddMinutes(45).AddTicks(1);

			// ProgressBar refresh step
			var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();

			// set ProgressBar bounds
			_progressBars.ForEach(p =>
			{
				p.Value = 0;
				p.Maximum = 100;
			});
			
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
					MarketEmulator =
					{
						Settings =
						{
							// match order if historical price touched our limit order price. 
							// It is terned off, and price should go through limit order price level
							// (more "severe" test mode)
							MatchOnTouch = false,
						}
					},

					UseExternalCandleSource = emulationInfo.UseCandleTimeFrame != null,

					CreateDepthFromOrdersLog = emulationInfo.UseOrderLog,
					CreateTradesFromOrdersLog = emulationInfo.UseOrderLog,

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

				((ILogSource)connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

				logManager.Sources.Add(connector);

				var candleManager = emulationInfo.UseCandleTimeFrame == null
					? new CandleManager(new TradeCandleBuilderSourceEx(connector))
					: new CandleManager(connector);

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

				connector.NewSecurities += securities =>
				{
					if (securities.All(s => s != security))
						return;

					// fill level1 values
					connector.SendInMessage(level1Info);

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

					// start strategy before emulation started
					strategy.Start();
					candleManager.Start(series);

					// start historical data loading when connection established successfully and all data subscribed
					connector.Start();
				};

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
								MessageBox.Show(this, LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
							}
							else
								MessageBox.Show(this, LocalizedStrings.cancelled);
						});
					}
					else if (connector.State == EmulationStates.Started)
					{
						SetIsEnabled(true);
					}
				};

				if (ShowDepth.IsChecked == true)
				{
					MarketDepth.UpdateFormat(security);

					connector.NewMessage += message =>
					{
						var quoteMsg = message as QuoteChangeMessage;

						if (quoteMsg != null)
							MarketDepth.UpdateDepth(quoteMsg);
					};
				}

				_connectors.Add(connector);

				progressBar.Value = 0;
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
							DepthsCheckBox.IsChecked == true ||
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