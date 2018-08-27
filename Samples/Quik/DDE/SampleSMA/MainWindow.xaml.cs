#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSMA.SampleSMAPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSMA
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Media;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies.Reporting;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private readonly TimeSpan _timeFrame = TimeSpan.FromMinutes(5);
		private QuikTrader _trader;
		private SmaStrategy _strategy;
		private bool _isTodaySmaDrawn;
		private CandleManager _candleManager;
		private Security _lkoh;
		private readonly ChartArea _area;
		private ChartCandleElement _candlesElem;
		private ChartIndicatorElement _longMaElem;
		private ChartIndicatorElement _shortMaElem;

		public MainWindow()
		{
			InitializeComponent();

			_area = new ChartArea();
			Chart.Areas.Add(_area);

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			Path.Text = QuikTerminal.GetDefaultPath();
		}

		private void OrdersOrderSelected()
		{
			CancelOrders.IsEnabled = !Orders.SelectedOrders.IsEmpty();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_trader?.Dispose();
			base.OnClosing(e);
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!Path.Text.IsEmpty())
				dlg.SelectedPath = Path.Text;

			if (dlg.ShowDialog(this) == true)
			{
				Path.Text = dlg.SelectedPath;
			}
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (_trader == null || _trader.ConnectionState == ConnectionStates.Disconnected)
			{
				if (_trader == null)
				{
					if (Path.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2983);
						return;
					}

					// создаем подключение
					_trader = new QuikTrader(Path.Text) { IsDde = true };

					Portfolios.Portfolios = new PortfolioDataSource(_trader);

					_trader.Connected += () =>
					{
						_candleManager = new CandleManager(_trader);

						_trader.NewSecurity += security =>
						{
							if (!security.Code.CompareIgnoreCase("LKOH"))
								return;

							// находим нужную бумагу
							var lkoh = security;

							_lkoh = lkoh;

							this.GuiAsync(() =>
							{
								Start.IsEnabled = true;
							});
						};

						_trader.NewMyTrade += trade =>
						{
							if (_strategy != null)
							{
								// найти те сделки, которые совершила стратегия скользящей средней
								if (_strategy.Orders.Contains(trade.Order))
									Trades.Trades.Add(trade);
							}
						};

						_candleManager.Processing += (series, candle) =>
						{
							// если скользящие за сегодняшний день отрисованы, то рисуем в реальном времени текущие скользящие
							if (_isTodaySmaDrawn && candle.State == CandleStates.Finished)
								ProcessCandle(candle);
						};
						//_trader.Error += ex => this.GuiAsync(() => MessageBox.Show(this, ex.ToString()));
						_trader.ConnectionError += ex =>
						{
							if (ex != null)
								this.GuiAsync(() => MessageBox.Show(this, ex.ToString()));
						};

						this.GuiAsync(() =>
						{
							ConnectBtn.IsEnabled = false;
							Report.IsEnabled = true;
						});
					};
				}

				_trader.Connect();
			}
			else
				_trader.Disconnect();
		}

		private void OnLog(LogMessage message)
		{
			// если стратегия вывела не просто сообщение, то вывести на экран.
			if (message.Level != LogLevels.Info && message.Level != LogLevels.Debug)
				this.GuiAsync(() => MessageBox.Show(this, message.Message));
		}

		private void OnStrategyPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			this.GuiAsync(() =>
			{
				Status.Content = _strategy.ProcessState;
				PnL.Content = _strategy.PnL;
				Slippage.Content = _strategy.Slippage;
				Position.Content = _strategy.Position;
				Latency.Content = _strategy.Latency;
			});
		}

		private void CancelOrdersClick(object sender, RoutedEventArgs e)
		{
			Orders.SelectedOrders.ForEach(_trader.CancelOrder);
		}

		private void StartClick(object sender, RoutedEventArgs e)
		{
			if (_strategy == null)
			{
				if (Portfolios.SelectedPortfolio == null)
				{
					MessageBox.Show(this, LocalizedStrings.Str3009);
					return;
				}

				// регистрируем наш тайм-фрейм
				var series = new CandleSeries(typeof(TimeFrameCandle), _lkoh, _timeFrame);

				// создаем торговую стратегию, скользящие средние на 80 5-минуток и 10 5-минуток
				_strategy = new SmaStrategy(_candleManager, series, new SimpleMovingAverage { Length = 80 }, new SimpleMovingAverage { Length = 10 })
				{
					Volume = 1,
					Security = _lkoh,
					Portfolio = Portfolios.SelectedPortfolio,
					Connector = _trader,
				};
				_strategy.Log += OnLog;
				_strategy.PropertyChanged += OnStrategyPropertyChanged;

				_candlesElem = new ChartCandleElement();
				_area.Elements.Add(_candlesElem);

				_longMaElem = new ChartIndicatorElement
				{
					FullTitle = LocalizedStrings.Long,
					Color = Colors.OrangeRed
				};
				_area.Elements.Add(_longMaElem);

				_shortMaElem = new ChartIndicatorElement
				{
					FullTitle = LocalizedStrings.Short,
					Color = Colors.RoyalBlue
				};
				_area.Elements.Add(_shortMaElem);

				IEnumerable<Candle> candles = CultureInfo.InvariantCulture.DoInCulture(() => File.ReadAllLines("LKOH_history.txt").Select(line =>
				{
					var parts = line.Split(',');
					var time = (parts[0] + parts[1]).ToDateTime("yyyyMMddHHmmss").ApplyTimeZone(TimeHelper.Moscow);
					return (Candle)new TimeFrameCandle
					{
						OpenPrice = parts[2].To<decimal>(),
						HighPrice = parts[3].To<decimal>(),
						LowPrice = parts[4].To<decimal>(),
						ClosePrice = parts[5].To<decimal>(),
						TimeFrame = _timeFrame,
						OpenTime = time,
						CloseTime = time + _timeFrame,
						TotalVolume = parts[6].To<decimal>(),
						Security = _lkoh,
						State = CandleStates.Finished,
					};
				}).ToArray());

				var lastCandleTime = default(DateTimeOffset);

				// начинаем вычислять скользящие средние
				foreach (var candle in candles)
				{
					ProcessCandle(candle);
					lastCandleTime = candle.OpenTime;
				}

				_candleManager.Start(series);

				// вычисляем временные отрезки текущей свечи
				var bounds = _timeFrame.GetCandleBounds(_trader.CurrentTime);

				candles = _candleManager.Container.GetCandles(series, new Range<DateTimeOffset>(lastCandleTime + _timeFrame, bounds.Min));

				foreach (var candle in candles)
				{
					ProcessCandle(candle);
				}

				_isTodaySmaDrawn = true;

				Report.IsEnabled = true;
			}

			if (_strategy.ProcessState == ProcessStates.Stopped)
			{
				// запускаем процесс получения стакана, необходимый для работы алгоритма котирования
				_trader.RegisterMarketDepth(_strategy.Security);
				_strategy.Start();
				Start.Content = LocalizedStrings.Str242;
			}
			else
			{
				_trader.UnRegisterMarketDepth(_strategy.Security);
				_strategy.Stop();
				Start.Content = LocalizedStrings.Str2421;
			}
		}

		private void ProcessCandle(Candle candle)
		{
			var longValue = candle.State == CandleStates.Finished ? _strategy.LongSma.Process(candle) : null;
			var shortValue = candle.State == CandleStates.Finished ? _strategy.ShortSma.Process(candle) : null;

			var chartData = new ChartDrawData();

			chartData
				.Group(candle.OpenTime)
					.Add(_candlesElem, candle)
					.Add(_longMaElem, longValue)
					.Add(_shortMaElem, shortValue);

			Chart.Draw(chartData);
		}

		private void ReportClick(object sender, RoutedEventArgs e)
		{
			// сгерерировать отчет по прошедшему тестированию
			new ExcelStrategyReport(_strategy, "sma.xlsx").Generate();

			// открыть отчет
			Process.Start("sma.xlsx");
		}
	}
}
