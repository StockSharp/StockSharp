using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Ecng.Common;
using Ecng.Xaml;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Algo.Strategies.Optimization;

namespace Parallel_testing_terminal
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private Security _security;
		private Portfolio _portfolio;
		private BruteForceOptimizer _optimizer;
		private readonly LogManager _logManager;
		private readonly string _pathHistory = Paths.HistoryDataPath;

		public MainWindow()
		{
			InitializeComponent();
			DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
			DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

			CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));

			_logManager = new LogManager();
			_logManager.Listeners.Add(new FileLogListener("log.txt"));
			_logManager.Listeners.Add(new GuiLogListener(Monitor));
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			_security = new Security
			{
				Id = "SBER@TQBR",
				Code = "SBER",
				PriceStep = 0.01m,
				Board = ExchangeBoard.Micex
			};
			_portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
			var storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(_pathHistory),
			};

			var startTime = DatePickerBegin.SelectedDate.Value.ChangeKind(DateTimeKind.Utc);
			var stopTime = DatePickerEnd.SelectedDate.Value.ChangeKind(DateTimeKind.Utc);

			// create backtesting connector
			_optimizer = new(new[] { _security }, new[] { _portfolio }, storageRegistry)
			{
				EmulationSettings =
				{
                    // by defaut is CPU count * 2
                    //BatchSize = 2,
                }
			};
			_logManager.Sources.Add(_optimizer);

			var strategies = new List<(Strategy, IStrategyParam[])>
			{
				CreateStrategy("s1", Colors.Brown, 1),
				CreateStrategy("s2", Colors.Blue, 2),
				CreateStrategy("s3", Colors.Black, 3),
				CreateStrategy("s4", Colors.DarkGreen, 4),
				CreateStrategy("s5", Colors.DarkOrange, 5)
			};

			_optimizer.StateChanged += (oldState, newState) =>
			{
				if (newState == ChannelStates.Stopped)
				{
					this.GuiAsync(() => MessageBox.Show(this, "Stopped"));
				}
			};

			_optimizer.Start(startTime, stopTime, strategies, strategies.Count);
		}

		private (Strategy, IStrategyParam[]) CreateStrategy(string name, Color color, int length)
		{
			var candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_security);

			// ready-to-use candles much faster than compression on fly mode
			// turn off compression to boost optimizer (!!! make sure you have candles)

			//candleSeries.BuildCandlesMode = MarketDataBuildModes.Build;

			var strategy = new StairsStrategyCountertrendStrategy(candleSeries)
			{
				Length = length,
				Security = _security,
				Portfolio = _portfolio,
			};
			var curveItems = EquityCurveChart.CreateCurve(name, color, ChartIndicatorDrawStyles.Line);
			strategy.PnLChanged += () =>
			{
				var data = new ChartDrawData();
				data.Group(strategy.CurrentTime)
					.Add(curveItems, strategy.PnL);
				EquityCurveChart.Draw(data);
			};
			StrategiesStatisticsPanel.AddStrategy(strategy);

			var parameters = new[] { strategy.Parameters[nameof(strategy.Length)] };
			return (strategy, parameters);
		}
	}
}