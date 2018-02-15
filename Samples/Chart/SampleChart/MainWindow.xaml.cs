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
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Threading;
	using System.Windows.Media;

	using DevExpress.Xpf.Core;

	using MoreLinq;

	using Ecng.Backup;
	using Ecng.Backup.Yandex;
	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		private ChartArea _areaComb;
		private ChartCandleElement _candleElement;
		private TimeFrameCandle _candle;
		private CandleMessageVolumeProfile _volumeProfile;
		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private readonly SynchronizedDictionary<DateTimeOffset, TimeFrameCandle> _updatedCandles = new SynchronizedDictionary<DateTimeOffset, TimeFrameCandle>();
		private readonly CachedSynchronizedList<TimeFrameCandle> _allCandles = new CachedSynchronizedList<TimeFrameCandle>();
		private decimal _lastPrice;
		//private const decimal _priceStep = 10m;
		private Security _security;
		private readonly IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		private RandomWalkTradeGenerator _tradeGenerator;
		private readonly CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator> _indicators = new CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator>();

		private TimeSpan _timeframe;

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put(LocalizedStrings.Str3200);

			Loaded += OnLoaded;

			_chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
			_chartUpdateTimer.Tick += ChartUpdateTimerOnTick;
			_chartUpdateTimer.Start();

			Theme.SelectedIndex = 1;
		}

		private void HistoryPath_OnFolderChanged(string path)
		{
			var secs = LocalMarketDataDrive.GetAvailableSecurities(path).ToArray();

			Securities.ItemsSource = secs;

			if (secs.Length > 0)
				Securities.SelectedIndex = 0;
		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			Chart.FillIndicators();
			Chart.SubscribeIndicatorElement += Chart_OnSubscribeIndicatorElement;
			Chart.UnSubscribeElement += Chart_OnUnSubscribeElement;

			ConfigManager.RegisterService<IBackupService>(new YandexDiskService());

			HistoryPath.Folder = @"..\..\..\..\Testing\HistoryData\".ToFullPath();

			if (Securities.SelectedItem == null)
				return;

			RefreshCharts();
		}

		private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var theme = (string)((ComboBoxItem)Theme.SelectedValue).Content;
			if (theme.IsEmpty())
				return;

			ApplicationThemeHelper.ApplicationThemeName = theme;
		}

		private void Chart_OnSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator)
		{
			var chartData = new ChartDrawData();

			foreach (var candle in _allCandles.Cache)
			{
				//if (candle.State != CandleStates.Finished)
				//	candle.State = CandleStates.Finished;

				chartData.Group(candle.OpenTime).Add(element, indicator.Process(candle));
			}

			Chart.Draw(chartData);

			_indicators.Add(element, indicator);

			CustomColors_Changed(null, null);
		}

		private void Chart_OnUnSubscribeElement(IChartElement element)
		{

			if (element is ChartIndicatorElement indElem)
				_indicators.Remove(indElem);
		}

		private void RefreshCharts()
		{
			Chart.ClearAreas();

			_areaComb = new ChartArea();

			var yAxis = _areaComb.YAxises.First();

			yAxis.AutoRange = true;
			Chart.IsAutoRange = true;
			Chart.IsAutoScroll = true;

			Chart.AddArea(_areaComb);

			_timeframe = TimeSpan.FromMinutes(((ComboBoxItem)Timeframe.SelectedItem).Tag.To<int>());

			var id = (SecurityId)Securities.SelectedItem;

			_security = new Security
			{
				Id = id.ToStringId(),
				PriceStep = id.SecurityCode.StartsWithIgnoreCase("RI") ? 10 : 0.01m,
				Board = _exchangeInfoProvider.GetExchangeBoard(id.BoardCode) ?? ExchangeBoard.Associated
			};

			_tradeGenerator = new RandomWalkTradeGenerator(id);
			_tradeGenerator.Init();
			_tradeGenerator.Process(_security.ToMessage());

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				_timeframe);

			_candleElement = new ChartCandleElement { FullTitle = "Candles" };
			Chart.AddElement(_areaComb, _candleElement, series);

			LoadData(_security);
		}

		private void Draw_Click(object sender, RoutedEventArgs e)
		{
			RefreshCharts();
		}

		private void LoadData(Security security)
		{
			_candle = null;
			_lastPrice = 0m;
			_allCandles.Clear();

			Chart.Reset(new IChartElement[] { _candleElement });

			var storage = new StorageRegistry();

			BusyIndicator.IsBusy = true;

			var path = HistoryPath.Folder;
			var isBuild = BuildFromTicks.IsChecked == true;
			var format = Format.SelectedFormat;

			var maxDays = isBuild ? 2 : 30 * (int)_timeframe.TotalMinutes;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				if (isBuild)
				{
					foreach (var tick in storage.GetTickMessageStorage(security, new LocalMarketDataDrive(path), format).Load())
					{
						AppendTick(security, tick);

						_lastTime = tick.ServerTime;

						if (date != tick.ServerTime.Date)
						{
							date = tick.ServerTime.Date;

							var str = date.To<string>();
							this.GuiAsync(() => BusyIndicator.BusyContent = str);

							maxDays--;

							if (maxDays == 0)
								break;
						}
					}
				}
				else
				{
					foreach (var candle in storage.GetCandleStorage(typeof(TimeFrameCandle), security, _timeframe, new LocalMarketDataDrive(path), format).Load())
					{
						lock (_updatedCandles.SyncRoot)
							_updatedCandles[candle.OpenTime] = _candle = (TimeFrameCandle)candle;

						_lastTime = candle.OpenTime + _timeframe;
						_lastPrice = _candle.ClosePrice;

						_tradeGenerator.Process(new ExecutionMessage
						{
							ExecutionType = ExecutionTypes.Tick,
							SecurityId = security.ToSecurityId(),
							ServerTime = _lastTime,
							TradePrice = _lastPrice,
						});

						if (date != candle.OpenTime.Date)
						{
							date = candle.OpenTime.Date;

							var str = date.To<string>();
							this.GuiAsync(() => BusyIndicator.BusyContent = str);

							maxDays--;

							if (maxDays == 0)
								break;
						}
					}
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				this.GuiAsync(() =>
				{
					BusyIndicator.IsBusy = false;
					Chart.IsAutoRange = false;
					//_areaComb.YAxises.First().AutoRange = false;
				});

			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private DateTimeOffset _lastTime;

		private void ChartUpdateTimerOnTick(object sender, EventArgs eventArgs)
		{
			if (IsRealtime.IsChecked == true && _lastPrice != 0m)
			{
				var nextTick = (ExecutionMessage)_tradeGenerator.Process(new TimeMessage { ServerTime = _lastTime });

				if (nextTick != null)
					AppendTick(_security, nextTick);
				
				_lastTime += TimeSpan.FromSeconds(10);
			}

			TimeFrameCandle[] candlesToUpdate;
			lock (_updatedCandles.SyncRoot)
			{
				candlesToUpdate = _updatedCandles.OrderBy(p => p.Key).Select(p => p.Value).ToArray();
				_updatedCandles.Clear();
			}

			var lastCandle = _allCandles.LastOrDefault();
			_allCandles.AddRange(candlesToUpdate.Where(c => lastCandle == null || c.OpenTime != lastCandle.OpenTime));

			ChartDrawData chartData = null;

			foreach (var candle in candlesToUpdate)
			{
				if (chartData == null)
					chartData = new ChartDrawData();

				var chartGroup = chartData.Group(candle.OpenTime);

				chartGroup.Add(_candleElement, candle);

				foreach (var pair in _indicators.CachedPairs)
				{
					chartGroup.Add(pair.Key, pair.Value.Process(candle));
				}
			}

			if (chartData != null)
				Chart.Draw(chartData);
		}

		private void AppendTick(Security security, ExecutionMessage tick)
		{
			var time = tick.ServerTime;
			var price = tick.TradePrice.Value;

			if (_candle == null || time >= _candle.CloseTime)
			{
				if (_candle != null)
				{
					_candle.State = CandleStates.Finished;

					lock (_updatedCandles.SyncRoot)
						_updatedCandles[_candle.OpenTime] = _candle;

					_lastPrice = _candle.ClosePrice;
					_tradeGenerator.Process(tick);
				}

				//var t = TimeframeSegmentDataSeries.GetTimeframePeriod(time.DateTime, _timeframe);
				var bounds = _timeframe.GetCandleBounds(time, security.Board);

				_candle = new TimeFrameCandle
				{
					TimeFrame = _timeframe,
					OpenTime = bounds.Min,
					CloseTime = bounds.Max,
					Security = security,
				};

				_volumeProfile = new CandleMessageVolumeProfile();
				_candle.PriceLevels = _volumeProfile.PriceLevels;

				_candle.OpenPrice = _candle.HighPrice = _candle.LowPrice = _candle.ClosePrice = price;
			}

			if (time < _candle.OpenTime)
				throw new InvalidOperationException("invalid time");

			if (price > _candle.HighPrice)
				_candle.HighPrice = price;

			if (price < _candle.LowPrice)
				_candle.LowPrice = price;

			_candle.ClosePrice = price;

			_candle.TotalVolume += tick.TradeVolume.Value;

			_volumeProfile.Update(tick.TradePrice.Value, tick.TradeVolume, tick.OriginSide);

			lock (_updatedCandles.SyncRoot)
				_updatedCandles[_candle.OpenTime] = _candle;
		}

		private void Error(string msg)
		{
			new MessageBoxBuilder()
				.Owner(this)
				.Error()
				.Text(msg)
				.Show();
		}

		private void Securities_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Draw.IsEnabled = Securities.SelectedItem != null;
		}

		private void CustomColors_Changed(object sender, RoutedEventArgs e)
		{
			if(_candleElement == null)
				return;

			var dd = new ChartDrawData();

			if (_checkCustomColors.IsChecked == true)
			{
				dd.SetCustomColorer(_candleElement, (dt, isUpCandle, isLastCandle) => dt.Hour % 2 != 0 ? null : (isUpCandle ? (Color?)Colors.Chartreuse : Colors.Aqua));
				_indicators.Keys.ForEach(el => dd.SetCustomColorer(el, (dt) => dt.Hour % 2 != 0 ? null : (Color?)Colors.Magenta));
			}
			else
			{
				dd.SetCustomColorer(_candleElement, null);
				_indicators.Keys.ForEach(el => dd.SetCustomColorer(el, null));
			}

			Chart.Draw(dd);
		}
	}
}