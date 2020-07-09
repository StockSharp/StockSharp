#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleHistoryTesting.SampleHistoryTestingPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Finam;
	using StockSharp.Yahoo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;
	using StockSharp.Configuration;

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
			public bool UseLevel1 { get; set; }
			public Func<IdGenerator, IMessageAdapter> CustomHistoryAdapter { get; set; }
		}

		private readonly List<ProgressBar> _progressBars = new List<ProgressBar>();
		private readonly List<CheckBox> _checkBoxes = new List<CheckBox>();
		private readonly CachedSynchronizedList<HistoryEmulationConnector> _connectors = new CachedSynchronizedList<HistoryEmulationConnector>();
		
		private DateTime _startEmulationTime;
		private ChartCandleElement _candlesElem;
		private ChartTradeElement _tradesElem;
		private ChartIndicatorElement _shortElem;
		private SimpleMovingAverage _shortMa;
		private ChartIndicatorElement _longElem;
		private SimpleMovingAverage _longMa;
		private ChartArea _area;

		private readonly InMemoryExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		public MainWindow()
		{
			InitializeComponent();

			HistoryPath.Folder = Paths.HistoryDataPath;

			SecId.Text = "SBER@TQBR";

			From.EditValue = new DateTime(2020, 4, 1);
			To.EditValue = new DateTime(2020, 4, 30);

			TimeFrame.SelectedIndex = 0;

			_progressBars.AddRange(new[]
			{
				TicksProgress,
				TicksAndDepthsProgress,
				DepthsProgress,
				CandlesProgress,
				CandlesAndDepthsProgress,
				OrderLogProgress,
				Level1Progress,
				FinamCandlesProgress,
				YahooCandlesProgress,
				RandomProgress,
			});

			_checkBoxes.AddRange(new[]
			{
				TicksCheckBox,
				TicksAndDepthsCheckBox,
				DepthsCheckBox,
				CandlesCheckBox,
				CandlesAndDepthsCheckBox,
				OrderLogCheckBox,
				Level1CheckBox,
				FinamCandlesCheckBox,
				YahooCandlesCheckBox,
				RandomCheckBox,
			});
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			if (_connectors.Count > 0)
			{
				foreach (var connector in _connectors.Cache)
					connector.Start();

				return;
			}

			if (HistoryPath.Folder.IsEmpty() || !Directory.Exists(HistoryPath.Folder))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			if (_connectors.Any(t => t.State != ChannelStates.Stopped))
			{
				MessageBox.Show(this, LocalizedStrings.Str3015);
				return;
			}

			var id = SecId.Text.ToSecurityId();

			//if (secIdParts.Length != 2)
			//{
			//	MessageBox.Show(this, LocalizedStrings.Str3016);
			//	return;
			//}

			var timeFrame = TimeSpan.FromMinutes(TimeFrame.SelectedIndex == 0 ? 1 : 5);

			var secCode = id.SecurityCode;
			var board = _exchangeInfoProvider.GetOrCreateBoard(id.BoardCode);

			// create test security
			var security = new Security
			{
				Id = SecId.Text, // sec id has the same name as folder with historical data
				Code = secCode,
				Board = board,
			};

			// create backtesting modes
			var settings = new[]
			{
				Tuple.Create(
					TicksCheckBox,
					TicksProgress,
					TicksParameterGrid,
					// ticks
					new EmulationInfo
					{
						UseTicks = true,
						CurveColor = Colors.DarkGreen,
						StrategyName = LocalizedStrings.Ticks
					},
					TicksChart,
					TicksEquity,
					TicksPosition),

				Tuple.Create(
					TicksAndDepthsCheckBox,
					TicksAndDepthsProgress,
					TicksAndDepthsParameterGrid,
					// ticks + order book
					new EmulationInfo
					{
						UseTicks = true,
						UseMarketDepth = true,
						CurveColor = Colors.Red,
						StrategyName = LocalizedStrings.XamlStr757
					},
					TicksAndDepthsChart,
					TicksAndDepthsEquity,
					TicksAndDepthsPosition),

				Tuple.Create(
					DepthsCheckBox,
					DepthsProgress,
					DepthsParameterGrid,
					// order book
					new EmulationInfo
					{
						UseMarketDepth = true,
						CurveColor = Colors.OrangeRed,
						StrategyName = LocalizedStrings.MarketDepths
					},
					DepthsChart,
					DepthsEquity,
					DepthsPosition),

				Tuple.Create(
					CandlesCheckBox,
					CandlesProgress,
					CandlesParameterGrid,
					// candles
					new EmulationInfo
					{
						UseCandleTimeFrame = timeFrame,
						CurveColor = Colors.DarkBlue,
						StrategyName = LocalizedStrings.Candles
					},
					CandlesChart,
					CandlesEquity,
					CandlesPosition),
				
				Tuple.Create(
					CandlesAndDepthsCheckBox,
					CandlesAndDepthsProgress,
					CandlesAndDepthsParameterGrid,
					// candles + orderbook
					new EmulationInfo
					{
						UseMarketDepth = true,
						UseCandleTimeFrame = timeFrame,
						CurveColor = Colors.Cyan,
						StrategyName = LocalizedStrings.XamlStr635
					},
					CandlesAndDepthsChart,
					CandlesAndDepthsEquity,
					CandlesAndDepthsPosition),
			
				Tuple.Create(
					OrderLogCheckBox,
					OrderLogProgress,
					OrderLogParameterGrid,
					// order log
					new EmulationInfo
					{
						UseOrderLog = true,
						CurveColor = Colors.CornflowerBlue,
						StrategyName = LocalizedStrings.OrderLog
					},
					OrderLogChart,
					OrderLogEquity,
					OrderLogPosition),

				Tuple.Create(
					Level1CheckBox,
					Level1Progress,
					Level1ParameterGrid,
					// order log
					new EmulationInfo
					{
						UseLevel1 = true,
						CurveColor = Colors.Aquamarine,
						StrategyName = LocalizedStrings.Level1
					},
					Level1Chart,
					Level1Equity,
					Level1Position),

				Tuple.Create(
					FinamCandlesCheckBox,
					FinamCandlesProgress,
					FinamCandlesParameterGrid,
					// candles
					new EmulationInfo
					{
						UseCandleTimeFrame = timeFrame,
						CustomHistoryAdapter = g => new FinamMessageAdapter(g),
						CurveColor = Colors.DarkBlue,
						StrategyName = LocalizedStrings.FinamCandles
					},
					FinamCandlesChart,
					FinamCandlesEquity,
					FinamCandlesPosition),

				Tuple.Create(
					YahooCandlesCheckBox,
					YahooCandlesProgress,
					YahooCandlesParameterGrid,
					// candles
					new EmulationInfo
					{
						UseCandleTimeFrame = timeFrame,
						CustomHistoryAdapter = g => new YahooMessageAdapter(g),
						CurveColor = Colors.DarkBlue,
						StrategyName = LocalizedStrings.YahooCandles
					},
					YahooCandlesChart,
					YahooCandlesEquity,
					YahooCandlesPosition),

				Tuple.Create(
					RandomCheckBox,
					RandomProgress,
					RandomParameterGrid,
					// candles
					new EmulationInfo
					{
						UseCandleTimeFrame = timeFrame,
						CustomHistoryAdapter = g => new OwnMessageAdapter(g),
						CurveColor = Colors.DarkBlue,
						StrategyName = LocalizedStrings.Custom
					},
					RandomChart,
					RandomEquity,
					RandomPosition),
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Folder)
			};

			var startTime = ((DateTime)From.EditValue).UtcKind();
			var stopTime = ((DateTime)To.EditValue).UtcKind();

			// (ru only) ОЛ необходимо загружать с 18.45 пред дня, чтобы стаканы строились правильно
			if (OrderLogCheckBox.IsChecked == true)
				startTime = startTime.Subtract(TimeSpan.FromDays(1)).AddHours(18).AddMinutes(45).AddTicks(1).ApplyMoscow().UtcDateTime;

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
			var secId = security.ToSecurityId();

			SetIsEnabled(false, false, false);

			foreach (var set in settings)
			{
				if (set.Item1.IsChecked == false)
					continue;

				var title = (string)set.Item1.Content;

				InitChart(set.Item5, set.Item6, set.Item7);

				var progressBar = set.Item2;
				var statistic = set.Item3;
				var emulationInfo = set.Item4;

				var level1Info = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = startTime,
				}
				.TryAdd(Level1Fields.PriceStep, 0.01m)
				.TryAdd(Level1Fields.StepPrice, 0.01m)
				.TryAdd(Level1Fields.MinPrice, 0.01m)
				.TryAdd(Level1Fields.MaxPrice, 1000000m)
				.TryAdd(Level1Fields.MarginBuy, 10000m)
				.TryAdd(Level1Fields.MarginSell, 10000m);

				// test portfolio
				var portfolio = Portfolio.CreateSimulator();

				var secProvider = (ISecurityProvider)new CollectionSecurityProvider(new[] { security });

				// create backtesting connector
				var connector = new HistoryEmulationConnector(
					secProvider, new[] { portfolio })
				{
					EmulationAdapter =
					{
						Settings =
						{
							// match order if historical price touched our limit order price. 
							// It is terned off, and price should go through limit order price level
							// (more "severe" test mode)
							MatchOnTouch = false,
						}
					},

					//UseExternalCandleSource = emulationInfo.UseCandleTimeFrame != null,

					//CreateDepthFromOrdersLog = emulationInfo.UseOrderLog,
					//CreateTradesFromOrdersLog = emulationInfo.UseOrderLog,

					HistoryMessageAdapter =
					{
						StorageRegistry = storageRegistry,

						OrderLogMarketDepthBuilders =
						{
							{
								secId,
								new OrderLogMarketDepthBuilder(secId)
							}
						}
					},

					// set market time freq as time frame
					MarketTimeChangedInterval = timeFrame,
				};

				((ILogSource)connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

				logManager.Sources.Add(connector);

				var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame)
				{
					BuildCandlesMode = emulationInfo.UseCandleTimeFrame == null ? MarketDataBuildModes.Build : MarketDataBuildModes.Load,
					BuildCandlesFrom2 = emulationInfo.UseOrderLog ? DataType.OrderLog : null,
				};

				_shortMa = new SimpleMovingAverage { Length = 10 };
				_shortElem = new ChartIndicatorElement
				{
					Color = Colors.Coral,
					ShowAxisMarker = false,
					FullTitle = _shortMa.ToString()
				};

				var chart = set.Item5;

				chart.AddElement(_area, _shortElem);

				_longMa = new SimpleMovingAverage { Length = 80 };
				_longElem = new ChartIndicatorElement
				{
					ShowAxisMarker = false,
					FullTitle = _longMa.ToString()
				};
				chart.AddElement(_area, _longElem);

				// create strategy based on 80 5-min и 10 5-min
				var strategy = new SmaStrategy(series, _longMa, _shortMa, chart, _candlesElem, _tradesElem, _longElem, _shortElem)
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

				if (emulationInfo.CustomHistoryAdapter != null)
				{
					connector.Adapter.InnerAdapters.Remove(connector.MarketDataAdapter);

					var emu = connector.EmulationAdapter.Emulator;
					connector.Adapter.InnerAdapters.Add(new EmulationMessageAdapter(emulationInfo.CustomHistoryAdapter(connector.TransactionIdGenerator), new InMemoryMessageChannel(new MessageByLocalTimeQueue(), "History out", err => err.LogError()), true, emu.SecurityProvider, emu.PortfolioProvider, emu.ExchangeInfoProvider));
				}

				// set history range
				connector.HistoryMessageAdapter.StartDate = startTime;
				connector.HistoryMessageAdapter.StopDate = stopTime;

				connector.SecurityReceived += (subscr, s) =>
				{
					if (s != security)
						return;

					// fill level1 values
					connector.EmulationAdapter.SendInMessage(level1Info);

					if (emulationInfo.UseMarketDepth)
					{
						connector.SubscribeMarketDepth(security);

						if	(
								// if order book will be generated
								generateDepths ||
								// or backtesting will be on candles
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
						connector.SubscribeOrderLog(security);
					}

					if (emulationInfo.UseTicks)
					{
						connector.SubscribeTrades(security);
					}

					if (emulationInfo.UseLevel1)
					{
						connector.SubscribeLevel1(security);
					}

					// start strategy before emulation started
					strategy.Start();

					// start historical data loading when connection established successfully and all data subscribed
					connector.Start();
				};

				// fill parameters panel
				statistic.Parameters.Clear();
				statistic.Parameters.AddRange(strategy.StatisticManager.Parameters);

				var equity = set.Item6;

				var pnlCurve = equity.CreateCurve(LocalizedStrings.PnL + " " + emulationInfo.StrategyName, Colors.Green, Colors.Red, ChartIndicatorDrawStyles.Area);
				var unrealizedPnLCurve = equity.CreateCurve(LocalizedStrings.PnLUnreal + " " + emulationInfo.StrategyName, Colors.Black, ChartIndicatorDrawStyles.Line);
				var commissionCurve = equity.CreateCurve(LocalizedStrings.Str159 + " " + emulationInfo.StrategyName, Colors.Red, ChartIndicatorDrawStyles.DashedLine);
				
				strategy.PnLChanged += () =>
				{
					var data = new ChartDrawData();

					data
						.Group(strategy.CurrentTime)
							.Add(pnlCurve, strategy.PnL - (strategy.Commission ?? 0))
							.Add(unrealizedPnLCurve, strategy.PnLManager.UnrealizedPnL ?? 0)
							.Add(commissionCurve, strategy.Commission ?? 0);

					equity.Draw(data);
				};

				var posItems = set.Item7.CreateCurve(emulationInfo.StrategyName, emulationInfo.CurveColor, ChartIndicatorDrawStyles.Line);

				strategy.PositionChanged += () =>
				{
					var data = new ChartDrawData();

					data
						.Group(strategy.CurrentTime)
							.Add(posItems, strategy.Position);

					set.Item7.Draw(data);
				};

				connector.ProgressChanged += steps => this.GuiAsync(() => progressBar.Value = steps);

				connector.StateChanged += () =>
				{
					if (connector.State == ChannelStates.Stopped)
					{
						strategy.Stop();

						SetIsChartEnabled(chart, false);

						if (_connectors.All(c => c.State == ChannelStates.Stopped))
						{
							logManager.Dispose();
							_connectors.Clear();

							SetIsEnabled(true, false, false);
						}

						this.GuiAsync(() =>
						{
							if (connector.IsFinished)
							{
								progressBar.Value = progressBar.Maximum;
								MessageBox.Show(this, LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime), title);
							}
							else
								MessageBox.Show(this, LocalizedStrings.cancelled, title);
						});
					}
					else if (connector.State == ChannelStates.Started)
					{
						if (_connectors.All(c => c.State == ChannelStates.Started))
							SetIsEnabled(false, true, true);

						SetIsChartEnabled(chart, true);
					}
					else if (connector.State == ChannelStates.Suspended)
					{
						if (_connectors.All(c => c.State == ChannelStates.Suspended))
							SetIsEnabled(true, false, true);
					}
				};

				if (ShowDepth.IsChecked == true)
				{
					MarketDepth.UpdateFormat(security);

					connector.NewMessage += message =>
					{
						if (message is QuoteChangeMessage quoteMsg)
							MarketDepth.UpdateDepth(quoteMsg);
					};
				}

				_connectors.Add(connector);

				progressBar.Value = 0;
			}

			_startEmulationTime = DateTime.Now;

			// start emulation
			foreach (var connector in _connectors.Cache)
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
			var isEnabled = _checkBoxes.Any(c => c.IsChecked == true);

			StartBtn.IsEnabled = isEnabled;
			TabControl.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
		}

		private void StopBtnClick(object sender, RoutedEventArgs e)
		{
			foreach (var connector in _connectors.Cache)
			{
				connector.Disconnect();
			}
		}

		private void PauseBtnClick(object sender, RoutedEventArgs e)
		{
			foreach (var connector in _connectors.Cache)
			{
				connector.Suspend();
			}
		}

		private void InitChart(IChart chart, EquityCurveChart equity, EquityCurveChart position)
		{
			chart.ClearAreas();
			equity.Clear();
			position.Clear();

			_area = new ChartArea();
			chart.AddArea(_area);

			_candlesElem = new ChartCandleElement { ShowAxisMarker = false };
			chart.AddElement(_area, _candlesElem);

			_tradesElem = new ChartTradeElement { FullTitle = LocalizedStrings.Str985 };
			chart.AddElement(_area, _tradesElem);
		}

		private void SetIsEnabled(bool canStart, bool canSuspend, bool canStop)
		{
			this.GuiAsync(() =>
			{
				StopBtn.IsEnabled = canStop;
				StartBtn.IsEnabled = canStart;
				PauseBtn.IsEnabled = canSuspend;

				foreach (var checkBox in _checkBoxes)
				{
					checkBox.IsEnabled = !canStop;
				}
			});
		}

		private void SetIsChartEnabled(IChart chart, bool started)
		{
			this.GuiAsync(() => chart.IsAutoRange = started);
		}
	}
}