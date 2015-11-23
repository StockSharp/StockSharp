namespace SampleChart
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

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
		private ChartArea _areaComb, _areaBVR1, _areaBVR2;
		private ChartBoxVolumeElement _bvElement;
		private ChartClusterProfileElement _cpElement;
		private ChartCandleElement _candleElement1, _candleElement2;
		private TimeFrameCandle _candle;
		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private readonly HashSet<TimeFrameCandle> _updatedCandles = new HashSet<TimeFrameCandle>();
		private readonly List<TimeFrameCandle> _allCandles = new List<TimeFrameCandle>();
		private decimal _lastPrice;
		private readonly Security _security = new Security
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
			_comboTheme.SelectedItem = "Chrome";
			InitCharts();

			_chartCombined.SubscribeIndicatorElement += (elem, ser, arg) => Chart_OnSubscribeIndicatorElement(elem, ser, arg, _chartCombined);
			_chartBindVisibleRange.SubscribeIndicatorElement += (elem, ser, arg) => Chart_OnSubscribeIndicatorElement(elem, ser, arg, _chartBindVisibleRange);

			LoadData(@"..\..\..\..\Testing\HistoryData\".ToFullPath());
		}

		private void Chart_OnSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator, ChartPanel chart)
		{
			var values = _allCandles
				.Select(candle =>
				{
					if (candle.State != CandleStates.Finished)
						candle.State = CandleStates.Finished;

					return new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
					{
						{ element, indicator.Process(candle) }
					});
				})
				.ToArray();

			chart.Draw(values);
		}

		private void InitCharts()
		{
			_candle = null;
			_lastPrice = 0m;
			_allCandles.Clear();
			_chartCombined.ClearAreas();

			_chartBindVisibleRange.ClearAreas();

			_areaComb = new ChartArea();
			_areaBVR1 = new ChartArea();
			_areaBVR2 = new ChartArea();

			_areaComb.YAxises.Add(new ChartAxis
			{
				Id = _chartMainYAxis,
				AutoRange = false,
				AxisType = ChartAxisType.Numeric,
				AxisAlignment = ChartAxisAlignment.Right,
			});

			_areaBVR2.YAxises.Add(new ChartAxis
			{
				Id = _chartMainYAxis,
				AutoRange = false,
				AxisType = ChartAxisType.Numeric,
				AxisAlignment = ChartAxisAlignment.Right,
			});

			_chartCombined.AddArea(_areaComb);
			_chartBindVisibleRange.AddArea(_areaBVR1);
			_chartBindVisibleRange.AddArea(_areaBVR2);

			_timeframe = int.Parse((string)((ComboBoxItem)_comboMainTimeframe.SelectedItem).Tag);
			var step = (decimal)_updownPriceStep.Value.Value;

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				TimeSpan.FromMinutes(_timeframe));

			_candleElement1 = new ChartCandleElement { FullTitle = "Candles", YAxisId = _chartMainYAxis };
			_chartCombined.AddElement(_areaComb, _candleElement1, series);

			_bvElement = new ChartBoxVolumeElement(_timeframe, step) { FullTitle = "BoxVolume", YAxisId = _chartMainYAxis };
			_chartCombined.AddElement(_areaComb, _bvElement);

			_cpElement = new ChartClusterProfileElement(_timeframe, step) { FullTitle = "Cluster profile" };
			_chartBindVisibleRange.AddElement(_areaBVR1, _cpElement);

			_candleElement2 = new ChartCandleElement { FullTitle = "Candles", YAxisId = _chartMainYAxis };
			_chartBindVisibleRange.AddElement(_areaBVR2, _candleElement2, series);

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

			_chartCombined.IndicatorTypes.AddRange(indicators);
			_chartBindVisibleRange.IndicatorTypes.AddRange(indicators);
		}

		private void LoadData_OnClick(object sender, RoutedEventArgs e)
		{
			var dialog = new VistaFolderBrowserDialog
			{
				SelectedPath = Directory.GetCurrentDirectory()
			};

			if (dialog.ShowDialog() != true)
				return;

			LoadData(dialog.SelectedPath);
		}

		private void LoadData(string path)
		{
			var storage = new StorageRegistry();

			var maxDays = 2;

			BusyIndicator.IsBusy = true;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security, new LocalMarketDataDrive(path)).Load())
				{
					AppendTick(tick.ServerTime, tick.TradePrice.Value, tick.Volume.Value);
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
			if (_checkRealtime.IsChecked == true && _lastPrice != 0m)
			{
				var step = _updownPriceStep.Value ?? 10;
				var price = Round(_lastPrice + (decimal)((RandomGen.GetDouble() - 0.5) * 5 * step), (decimal)step);
				AppendTick(_lastTime, price, RandomGen.GetInt(50) + 1);
				_lastTime += TimeSpan.FromSeconds(10);
			}

			var candlesToUpdate = _updatedCandles.OrderBy(c => c.OpenTime).ToArray();
			_updatedCandles.Clear();

			candlesToUpdate.ForEach(c =>
			{
				_chartCombined.Draw(c.OpenTime, new Dictionary<IChartElement, object> { { _candleElement1, c } });
				_chartCombined.Draw(c.OpenTime, new Dictionary<IChartElement, object> { { _bvElement, c } });

				_chartBindVisibleRange.Draw(c.OpenTime, new Dictionary<IChartElement, object> { { _candleElement2, c } });
				_chartBindVisibleRange.Draw(c.OpenTime, new Dictionary<IChartElement, object> { { _cpElement, c } });
			});
		}

		private void AppendTick(DateTimeOffset time, decimal price, decimal vol)
		{
			_updatedCandles.Add(GetCandle(time, price, vol));
			_lastPrice = price;
		}

		private TimeFrameCandle GetCandle(DateTimeOffset time, decimal price, decimal vol)
		{
			if (_candle == null || time >= _candle.CloseTime)
			{
				//var t = TimeframeSegmentDataSeries.GetTimeframePeriod(time.DateTime, _timeframe);
				var tf = TimeSpan.FromMinutes(_timeframe);
				var bounds = tf.GetCandleBounds(time, _security.Board);
				_candle = new TimeFrameCandle
				{
					TimeFrame = tf,
					OpenTime = bounds.Min,
					CloseTime = bounds.Max,
				};

				_candle.OpenPrice = _candle.HighPrice = _candle.LowPrice = _candle.ClosePrice = price;
				_candle.VolumeProfileInfo.Update(new InputTick(time, price, vol));

				_allCandles.Add(_candle);

				return _candle;
			}

			if (time < _candle.OpenTime)
				throw new InvalidOperationException("invalid time");

			if (price > _candle.HighPrice)
				_candle.HighPrice = price;

			if (price < _candle.LowPrice)
				_candle.LowPrice = price;

			_candle.ClosePrice = price;

			_candle.TotalVolume += vol;

			_candle.VolumeProfileInfo.Update(new InputTick(time, price, vol));

			return _candle;
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

		private class InputTick : ICandleBuilderSourceValue
		{
			public InputTick(DateTimeOffset time, decimal price, decimal volume)
			{
				Time = time;
				Price = price;
				Volume = volume;
			}

			public Security Security => null;
			public DateTimeOffset Time { get; }
			public decimal Price { get; }
			public decimal Volume { get; }
			public Sides? OrderDirection => Sides.Buy;
		}

		private void _comboTheme_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var theme = (string)_comboTheme.SelectedValue;
			_chartCombined.ChartTheme = theme;
			_chartBindVisibleRange.ChartTheme = theme;
		}
	}
}