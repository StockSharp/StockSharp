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
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
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
	using StockSharp.Algo.Candles.Compression;
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
		private CandleMessage _currCandle;
		private readonly SynchronizedList<CandleMessage> _updatedCandles = new SynchronizedList<CandleMessage>();
		private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, Candle> _allCandles = new CachedSynchronizedOrderedDictionary<DateTimeOffset, Candle>();
		private Security _security;
		private readonly IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		private RandomWalkTradeGenerator _tradeGenerator;
		private readonly CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator> _indicators = new CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator>();
		private readonly object _lock = new object();
		private ICandleBuilder _candleBuilder;
		private MarketDataMessage _mdMsg;
		private readonly ICandleBuilderValueTransform _candleTransform = new TickCandleBuilderValueTransform();
		private bool _historyLoaded;
		private bool _isRealTime;
		private DateTimeOffset _lastTime;
		private readonly Timer _realTimeTimer;
		private readonly Timer _drawingTimer;
		private bool _isDrawing;
		private readonly SyncObject _drawingLock = new SyncObject();
		private bool _isRealTiming;
		private readonly SyncObject _realTimingLock = new SyncObject();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put(LocalizedStrings.Str3200);

			Loaded += OnLoaded;

			_realTimeTimer = ThreadingHelper
				.Timer(OnRealTimeCandles)
				.Interval(TimeSpan.FromMilliseconds(1));

			_drawingTimer = ThreadingHelper
				.Timer(OnDrawingCandles)
				.Interval(TimeSpan.FromMilliseconds(100));

			Theme.SelectedIndex = 1;

			SeriesEditor.Settings = new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = TimeSpan.FromMinutes(1)
			};
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

		protected override void OnClosing(CancelEventArgs e)
		{
			_drawingTimer.Dispose();
			_realTimeTimer.Dispose();

			base.OnClosing(e);
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

			foreach (var candle in _allCandles.CachedValues)
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

			var id = (SecurityId)Securities.SelectedItem;

			_security = new Security
			{
				Id = id.ToStringId(),
				PriceStep = id.SecurityCode.StartsWith("RI", StringComparison.InvariantCultureIgnoreCase) ? 10 : 
							id.SecurityCode.Contains("ES") ? 0.25m : 
							0.01m,
				Board = ExchangeBoard.Associated
			};

			_tradeGenerator = new RandomWalkTradeGenerator(id);
			_tradeGenerator.Init();
			_tradeGenerator.Process(_security.ToMessage());

			var series = new CandleSeries(
				SeriesEditor.Settings.CandleType,
				_security,
				SeriesEditor.Settings.Arg) { IsCalcVolumeProfile = true };

			_candleElement = new ChartCandleElement { FullTitle = "Candles" };
			Chart.AddElement(_areaComb, _candleElement, series);

			LoadData(series);
		}

		private void Draw_Click(object sender, RoutedEventArgs e)
		{
			RefreshCharts();
		}

		private void LoadData(CandleSeries series)
		{
			lock (_lock)
			{
				_currCandle = null;
				_historyLoaded = false;
				_allCandles.Clear();
			}

			_candleTransform.Process(new ResetMessage());
			_candleBuilder = series.CandleType.ToCandleMessageType().ToCandleMarketDataType().CreateCandleBuilder();

			Chart.Reset(new IChartElement[] { _candleElement });

			var storage = new StorageRegistry();

			BusyIndicator.IsBusy = true;

			var path = HistoryPath.Folder;
			var isBuild = BuildFromTicks.IsChecked == true;
			var format = Format.SelectedFormat;

			var maxDays = (isBuild || series.CandleType != typeof(TimeFrameCandle))
				? 5
				: 30 * (int)((TimeSpan)series.Arg).TotalMinutes;

			_mdMsg = series.ToMarketDataMessage(true);

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				if (isBuild)
				{
					foreach (var tick in storage.GetTickMessageStorage(series.Security, new LocalMarketDataDrive(path), format).Load())
					{
						_tradeGenerator.Process(tick);

						if (_candleTransform.Process(tick))
						{
							var candles = _candleBuilder.Process(_mdMsg, _currCandle, _candleTransform);

							lock (_lock)
							{
								foreach (var candle in candles)
								{
									_currCandle = candle;
									_updatedCandles.Add((CandleMessage)candle.Clone());
								}
							}
						}

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
					foreach (var candleMsg in storage.GetCandleMessageStorage(series.CandleType.ToCandleMessageType(), series.Security, series.Arg, new LocalMarketDataDrive(path), format).Load())
					{
						lock (_updatedCandles.SyncRoot)
						{
							if (candleMsg.State != CandleStates.Finished)
								candleMsg.State = CandleStates.Finished;

							_currCandle = candleMsg;
							_updatedCandles.Add(candleMsg);
						}

						_lastTime = candleMsg.OpenTime;

						if (candleMsg is TimeFrameCandleMessage)
							_lastTime += (TimeSpan)series.Arg;

						_tradeGenerator.Process(new ExecutionMessage
						{
							ExecutionType = ExecutionTypes.Tick,
							SecurityId = series.Security.ToSecurityId(),
							ServerTime = _lastTime,
							TradePrice = candleMsg.ClosePrice,
						});

						if (date != candleMsg.OpenTime.Date)
						{
							date = candleMsg.OpenTime.Date;

							var str = date.To<string>();
							this.GuiAsync(() => BusyIndicator.BusyContent = str);

							maxDays--;

							if (maxDays == 0)
								break;
						}
					}
				}

				lock (_lock)
					_historyLoaded = true;
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

		private void OnRealTimeCandles()
		{
			lock (_realTimingLock)
			{
				if (_isRealTiming)
					return;

				_isRealTiming = true;
			}

			try
			{
				lock (_lock)
				{
					if (!_historyLoaded || !_isRealTime)
						return;

					var nextTick = (ExecutionMessage)_tradeGenerator.Process(new TimeMessage { ServerTime = _lastTime });

					if (nextTick != null)
					{
						if (_candleTransform.Process(nextTick))
						{
							var candles = _candleBuilder.Process(_mdMsg, _currCandle, _candleTransform);

							lock (_lock)
							{
								foreach (var candle in candles)
								{
									_currCandle = candle;
									_updatedCandles.Add((CandleMessage)candle.Clone());
								}
							}
						}
					}
				
					_lastTime += TimeSpan.FromSeconds(RandomGen.GetInt(1, 10));
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
			finally
			{
				lock (_realTimingLock)
					_isRealTiming = false;
			}
		}

		private void OnDrawingCandles()
		{
			lock (_drawingLock)
			{
				if (_isDrawing)
					return;

				_isDrawing = true;
			}

			try
			{
				var candlesToUpdate = new List<Candle>();

				lock (_updatedCandles.SyncRoot)
				{
					if (_updatedCandles.Count == 0)
						return;

					var lastTime = DateTimeOffset.MinValue;

					foreach (var message in _updatedCandles.Reverse())
					{
						if (lastTime == message.OpenTime)
							continue;

						lastTime = message.OpenTime;

						candlesToUpdate.Add(message.ToCandle(_security));
					}

					_updatedCandles.Clear();
				}

				candlesToUpdate.Reverse();

				foreach (var candle in candlesToUpdate)
					_allCandles[candle.OpenTime] = candle;

				ChartDrawData chartData = null;

				foreach (var candle in candlesToUpdate)
				{
					if (chartData == null)
						chartData = new ChartDrawData();

					var chartGroup = chartData.Group(candle.OpenTime);

					lock (_lock)
					{
						chartGroup.Add(_candleElement, candle);
					}

					foreach (var pair in _indicators.CachedPairs)
					{
						chartGroup.Add(pair.Key, pair.Value.Process(candle));
					}
				}

				if (chartData != null)
					Chart.Draw(chartData);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
			finally
			{
				lock (_drawingLock)
					_isDrawing = false;
			}
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
			if (_candleElement == null)
				return;

			if (CustomColors.IsChecked == true)
			{
				_candleElement.Colorer = (dto, isUpCandle, isLastCandle) => dto.Hour % 2 != 0 ? null : (isUpCandle ? (Color?)Colors.Chartreuse : Colors.Aqua);
				_indicators.Keys.ForEach(el => el.Colorer = dto => dto.Hour % 2 != 0 ? null : (Color?)Colors.Magenta);
			}
			else
			{
				_candleElement.Colorer = null;
				_indicators.Keys.ForEach(el => el.Colorer = null);
			}

			// refresh prev painted elements
			Chart.Draw(new ChartDrawData());
		}

		private void IsRealtime_OnChecked(object sender, RoutedEventArgs e)
		{
			lock (_lock)
				_isRealTime = IsRealtime.IsChecked == true;
		}
	}
}