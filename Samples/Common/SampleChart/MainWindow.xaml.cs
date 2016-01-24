#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleChart.SampleChartPublic
File: MainWindow.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleChart
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;

	public partial class MainWindow
	{
		private ChartArea _areaComb;
		private ChartCandleElement _candleElement1;
		private TimeFrameCandle _candle;
		private VolumeProfile _volumeProfile;
		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private readonly SynchronizedDictionary<DateTimeOffset, TimeFrameCandle> _updatedCandles = new SynchronizedDictionary<DateTimeOffset, TimeFrameCandle>();
		private readonly CachedSynchronizedList<TimeFrameCandle> _allCandles = new CachedSynchronizedList<TimeFrameCandle>();
		private decimal _lastPrice;
		private Security _security = new Security
		{
			Id = "RIZ2@FORTS",
			PriceStep = 5,
			Board = ExchangeBoard.Forts
		};

		private const string _chartMainYAxis = "MainYAxis";

		private int _timeframe;

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put(LocalizedStrings.Str3200);

			Loaded += OnLoaded;

			_chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
			_chartUpdateTimer.Tick += ChartUpdateTimerOnTick;
			_chartUpdateTimer.Start();
		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			Theme.SelectedItem = "Chrome";
			InitCharts();

			Chart.SubscribeIndicatorElement += Chart_OnSubscribeIndicatorElement;

			HistoryPath.Folder = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
			LoadData();
		}

		private void Chart_OnSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator)
		{
			var values = _allCandles.Cache
				.Select(candle =>
				{
					if (candle.State != CandleStates.Finished)
						candle.State = CandleStates.Finished;

					return new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
					{
						{ element, indicator.Process(candle) }
					});
				});

			Chart.Draw(values);
		}

		private void InitCharts()
		{
			Chart.ClearAreas();

			_areaComb = new ChartArea();

			_areaComb.YAxises.Add(new ChartAxis
			{
				Id = _chartMainYAxis,
				AutoRange = false,
				AxisType = ChartAxisType.Numeric,
				AxisAlignment = ChartAxisAlignment.Right,
			});

			Chart.AddArea(_areaComb);

			_timeframe = int.Parse((string)((ComboBoxItem)Timeframe.SelectedItem).Tag);
			var step = (decimal)PriceStep.Value.Value;

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				TimeSpan.FromMinutes(_timeframe));

			_candleElement1 = new ChartCandleElement(_timeframe, step) { FullTitle = "Candles", YAxisId = _chartMainYAxis };
			Chart.AddElement(_areaComb, _candleElement1, series);

			var ns = typeof(IIndicator).Namespace;

			var rendererTypes = typeof(Chart).Assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && typeof(BaseChartIndicatorPainter).IsAssignableFrom(t))
				.ToDictionary(t => t.Name);

			var indicators = typeof(IIndicator).Assembly
				.GetTypes()
				.Where(t => t.Namespace == ns && !t.IsAbstract && typeof(IIndicator).IsAssignableFrom(t))
				.Select(t =>
				{
					var name = t.Name;
					var p = rendererTypes.TryGetValue(name + "Painter");
					if (p == null)
					{
						if (t.Name.EndsWith("Indicator"))
							name = name.Substring(0, name.Length - "Indicator".Length);

						p = rendererTypes.TryGetValue(name + "Painter");
					}

					return new IndicatorType(t, p);
				})
				.ToArray();

			Chart.IndicatorTypes.AddRange(indicators);
		}

		private void Draw_Click(object sender, RoutedEventArgs e)
		{
			LoadData();
		}

		private void LoadData()
		{
			_candle = null;
			_lastPrice = 0m;
			_allCandles.Clear();

			var id = new SecurityIdGenerator().Split(SecurityId.Text);

			_security = new Security
			{
				Id = SecurityId.Text,
				PriceStep = 5,
				Board = ExchangeBoard.GetBoard(id.BoardCode)
			};

			Chart.Reset(new IChartElement[] { _candleElement1 });

			var storage = new StorageRegistry();

			var maxDays = 2;

			BusyIndicator.IsBusy = true;

			var path = HistoryPath.Folder;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security, new LocalMarketDataDrive(path)).Load())
				{
					AppendTick(_security, tick);
					_lastTime = tick.ServerTime;

					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						this.GuiAsync(() =>
						{
							BusyIndicator.BusyContent = date.ToString();
						});

						maxDays--;

						if (maxDays == 0)
							break;
					}
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				BusyIndicator.IsBusy = false;
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private DateTimeOffset _lastTime;

		private void ChartUpdateTimerOnTick(object sender, EventArgs eventArgs)
		{
			if (IsRealtime.IsChecked == true && _lastPrice != 0m)
			{
				var step = PriceStep.Value ?? 10;
				var price = Round(_lastPrice + (decimal)((RandomGen.GetDouble() - 0.5) * 5 * step), (decimal)step);
				AppendTick(_security, new ExecutionMessage
				{
					ServerTime = _lastTime,
					TradePrice = price,
					TradeVolume = RandomGen.GetInt(50) + 1
				});
				_lastTime += TimeSpan.FromSeconds(10);
			}

			TimeFrameCandle[] candlesToUpdate;
			lock (_updatedCandles.SyncRoot)
			{
				candlesToUpdate = _updatedCandles.OrderBy(p => p.Key).Select(p => p.Value).ToArray();
				_updatedCandles.Clear();
			}

			_allCandles.AddRange(candlesToUpdate);

			candlesToUpdate.ForEach(c =>
			{
				Chart.Draw(c.OpenTime, new Dictionary<IChartElement, object>
				{
					{ _candleElement1, c },
				});
			});
		}

		private void AppendTick(Security security, ExecutionMessage tick)
		{
			var time = tick.ServerTime;
			var price = tick.TradePrice.Value;

			if (_candle == null || time >= _candle.CloseTime)
			{
				if (_candle != null)
				{
					var candle = (TimeFrameCandle)_candle.Clone();
					_updatedCandles[candle.OpenTime] = candle;
					_lastPrice = candle.ClosePrice;
				}

				//var t = TimeframeSegmentDataSeries.GetTimeframePeriod(time.DateTime, _timeframe);
				var tf = TimeSpan.FromMinutes(_timeframe);
				var bounds = tf.GetCandleBounds(time, _security.Board);
				_candle = new TimeFrameCandle
				{
					TimeFrame = tf,
					OpenTime = bounds.Min,
					CloseTime = bounds.Max,
				};
				_volumeProfile = new VolumeProfile();
				_candle.PriceLevels = _volumeProfile.PriceLevels;

				_candle.OpenPrice = _candle.HighPrice = _candle.LowPrice = _candle.ClosePrice = price;
				_volumeProfile.Update(new TickCandleBuilderSourceValue(security, tick));
			}

			if (time < _candle.OpenTime)
				throw new InvalidOperationException("invalid time");

			if (price > _candle.HighPrice)
				_candle.HighPrice = price;

			if (price < _candle.LowPrice)
				_candle.LowPrice = price;

			_candle.ClosePrice = price;

			_candle.TotalVolume += tick.TradeVolume.Value;

			_volumeProfile.Update(new TickCandleBuilderSourceValue(security, tick));
		}

		public static decimal Round(decimal value, decimal nearest)
		{
			return Math.Round(value / nearest) * nearest;
		}

		private void Error(string msg)
		{
			new MessageBoxBuilder()
				.Owner(this)
				.Error()
				.Text(msg)
				.Show();
		}

		private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var theme = (string)Theme.SelectedValue;
			Chart.ChartTheme = theme;
		}
	}
}