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
	using Ecng.Xaml.Charting.Visuals.Annotations;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Logging;
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
		private RandomWalkTradeGenerator _tradeGenerator;
		private readonly CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator> _indicators = new CachedSynchronizedDictionary<ChartIndicatorElement, IIndicator>();
		private ICandleBuilder _candleBuilder;
		private MarketDataMessage _mdMsg;
		private readonly ICandleBuilderValueTransform _candleTransform = new TickCandleBuilderValueTransform();
		private readonly CandlesHolder _holder = new CandlesHolder();
		private readonly CandleBuilderProvider _builderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		private bool _historyLoaded;
		private bool _isRealTime;
		private DateTimeOffset _lastTime;
		private readonly Timer _dataTimer;
		private bool _isInTimerHandler;
		private readonly SyncObject _timerLock = new SyncObject();
		private readonly SynchronizedList<Action> _dataThreadActions = new SynchronizedList<Action>();

		private static readonly TimeSpan _realtimeInterval = TimeSpan.FromMilliseconds(1);
		private static readonly TimeSpan _drawInterval = TimeSpan.FromMilliseconds(100);

		private DateTime _lastRealtimeUpdateTime;
		private DateTime _lastDrawTime;

		private readonly IdGenerator _transactionIdGenerator = new IncrementalIdGenerator();
		private long _transactionId;

		private ChartAnnotation _annotation;
		private ChartDrawData.AnnotationData _annotationData;
		private int _annotationId;

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put(LocalizedStrings.Str3200);

			Loaded += OnLoaded;

			_dataTimer = ThreadingHelper
				.Timer(OnDataTimer)
				.Interval(TimeSpan.FromMilliseconds(1));

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
			Chart.AnnotationCreated += ChartOnAnnotationCreated;
			Chart.AnnotationModified += ChartOnAnnotationModified;
			Chart.AnnotationDeleted += ChartOnAnnotationDeleted;
			Chart.AnnotationSelected += ChartOnAnnotationSelected;

			ConfigManager.RegisterService<IBackupService>(new YandexDiskService());

			HistoryPath.Folder = @"..\..\..\..\Testing\HistoryData\".ToFullPath();

			if (Securities.SelectedItem == null)
				return;

			RefreshCharts();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_dataTimer.Dispose();
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
			_dataThreadActions.Add(() =>
			{
				var chartData = new ChartDrawData();

				foreach (var candle in _allCandles.CachedValues)
					chartData.Group(candle.OpenTime).Add(element, indicator.Process(candle));

				Chart.Reset(new[] { element });
				Chart.Draw(chartData);

				_indicators[element] = indicator;
			});
		}

		private void Chart_OnUnSubscribeElement(IChartElement element)
		{
			_dataThreadActions.Add(() =>
			{
				if (element is ChartIndicatorElement indElem)
					_indicators.Remove(indElem);
			});
		}

		private void RefreshCharts()
		{
			if (Dispatcher.CheckAccess())
			{
				_dataThreadActions.Add(RefreshCharts);
				return;
			}

			CandleSeries series = null;

			this.GuiSync(() =>
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

				series = new CandleSeries(
											 SeriesEditor.Settings.CandleType,
											 _security,
											 SeriesEditor.Settings.Arg) { IsCalcVolumeProfile = true };

				_candleElement = new ChartCandleElement { FullTitle = "Candles" };
				Chart.AddElement(_areaComb, _candleElement, series);

				_currCandle = null;
				_historyLoaded = false;
				_allCandles.Clear();
				_updatedCandles.Clear();
				_dataThreadActions.Clear();
			});

			Chart.Reset(new IChartElement[] { _candleElement });

			this.GuiAsync(() => LoadData(series));
		}

		private void Draw_Click(object sender, RoutedEventArgs e)
		{
			RefreshCharts();
		}

		private void LoadData(CandleSeries series)
		{
			var msgType = series.CandleType.ToCandleMessageType();

			_transactionId = _transactionIdGenerator.GetNextId();
			_holder.Clear();
			_holder.CreateCandleSeries(_transactionId, series);

			_candleTransform.Process(new ResetMessage());
			_candleBuilder = _builderProvider.Get(msgType.ToCandleMarketDataType());

			var storage = new StorageRegistry();

			BusyIndicator.IsBusy = true;

			var path = HistoryPath.Folder;
			var isBuild = BuildFromTicks.IsChecked == true;
			var format = Format.SelectedFormat;

			var maxDays = (isBuild || series.CandleType != typeof(TimeFrameCandle))
				? 2
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

							foreach (var candle in candles)
							{
								_currCandle = candle;
								_updatedCandles.Add((CandleMessage)candle.Clone());
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
					foreach (var candleMsg in storage.GetCandleMessageStorage(msgType, series.Security, series.Arg, new LocalMarketDataDrive(path), format).Load())
					{
						if (candleMsg.State != CandleStates.Finished)
							candleMsg.State = CandleStates.Finished;

						_currCandle = candleMsg;
						_updatedCandles.Add(candleMsg);

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

				_historyLoaded = true;
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				BusyIndicator.IsBusy = false;
				Chart.IsAutoRange = false;
				ModifyAnnotationBtn.IsEnabled = true;
				NewAnnotationBtn.IsEnabled = true;

			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private static void DoIfTime(Action action, DateTime now, ref DateTime lastExecutTime, TimeSpan period)
		{
			if (now - lastExecutTime < period)
				return;

			lastExecutTime = now;
			action();
		}

		private void OnDataTimer()
		{
			lock (_timerLock)
			{
				if (_isInTimerHandler)
					return;

				_isInTimerHandler = true;
			}

			try
			{
				if (_dataThreadActions.Count > 0)
				{
					Action[] actions = null;
					_dataThreadActions.SyncDo(l => actions = l.CopyAndClear());
					actions.ForEach(a => a());
				}

				var now = DateTime.UtcNow;
				DoIfTime(UpdateRealtimeCandles, now, ref _lastRealtimeUpdateTime, _realtimeInterval);
				DoIfTime(DrawChartElements,     now, ref _lastDrawTime,           _drawInterval);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
			finally
			{
				_isInTimerHandler = false;
			}
		}

		private void UpdateRealtimeCandles()
		{
			if (!_historyLoaded || !_isRealTime)
				return;

			var nextTick = (ExecutionMessage)_tradeGenerator.Process(new TimeMessage { ServerTime = _lastTime });

			if (nextTick != null)
			{
				if (_candleTransform.Process(nextTick))
				{
					var candles = _candleBuilder.Process(_mdMsg, _currCandle, _candleTransform);

					foreach (var candle in candles)
					{
						_currCandle = candle;
						_updatedCandles.Add((CandleMessage)candle.Clone());
					}
				}
			}

			_lastTime += TimeSpan.FromMilliseconds(RandomGen.GetInt(100, 2000));
		}

		private void DrawChartElements()
		{
			var messages = _updatedCandles.SyncGet(uc => uc.CopyAndClear());

			if (messages.Length == 0)
				return;

			var lastTime = DateTimeOffset.MinValue;
			var candlesToUpdate = new List<Candle>();

			foreach (var message in messages.Reverse())
			{
				if (lastTime == message.OpenTime)
					continue;

				lastTime = message.OpenTime;

				message.OriginalTransactionId = _transactionId;

				if (_holder.UpdateCandle(message, out var candle) != null)
				{
					if (candlesToUpdate.Count == 0 || candlesToUpdate.Last() != candle)
						candlesToUpdate.Add(candle);
				}
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
				chartGroup.Add(_candleElement, candle);

				foreach (var pair in _indicators.CachedPairs)
				{
					chartGroup.Add(pair.Key, pair.Value.Process(candle));
				}
			}

			if (chartData != null)
				Chart.Draw(chartData);
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
				_indicators.Keys.ForEach(el => el.Colorer = c => ((DateTimeOffset)c).Hour % 2 != 0 ? null : (Color?)Colors.Magenta);
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
			_isRealTime = IsRealtime.IsChecked == true;
		}

		private void GetMiddle(out DateTimeOffset time, out decimal price)
		{
			var dtMin = DateTimeOffset.MaxValue;
			var dtMax = DateTimeOffset.MinValue;
			var priceMin = decimal.MaxValue;
			var priceMax = decimal.MinValue;

			foreach (var candle in _allCandles.CachedValues)
			{
				if(candle.OpenTime < dtMin) dtMin = candle.OpenTime;
				if(candle.OpenTime > dtMax) dtMax = candle.OpenTime;

				if(candle.LowPrice < priceMin)  priceMin = candle.LowPrice;
				if(candle.HighPrice > priceMax) priceMax = candle.HighPrice;
			}

			time = dtMin + TimeSpan.FromTicks((dtMax - dtMin).Ticks / 2);
			price = priceMin + (priceMax - priceMin) / 2;
		}

		private void ModifyAnnotation(bool isNew)
		{
			Brush RandomBrush()
			{
				var b = new SolidColorBrush(Color.FromRgb((byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255)));
				b.Freeze();
				return b;
			}

			if (_annotation == null)
				return;

			IComparable x1, x2, y1, y2;

			var mode = RandomGen.GetDouble() > 0.5 ? AnnotationCoordinateMode.Absolute : AnnotationCoordinateMode.Relative;

			if (_annotationData == null)
			{
				if (mode == AnnotationCoordinateMode.Absolute)
				{
					GetMiddle(out var x0, out var y0);
					x1 = x0 - TimeSpan.FromMinutes(RandomGen.GetInt(10, 60));
					x2 = x0 + TimeSpan.FromMinutes(RandomGen.GetInt(10, 60));
					y1 = y0 - RandomGen.GetInt(5, 10) * _security.PriceStep ?? 0.01m;
					y2 = y0 + RandomGen.GetInt(5, 10) * _security.PriceStep ?? 0.01m;
				}
				else
				{
					x1 = 0.5 - RandomGen.GetDouble() / 10;
					x2 = 0.5 + RandomGen.GetDouble() / 10;
					y1 = 0.5 - RandomGen.GetDouble() / 10;
					y2 = 0.5 - RandomGen.GetDouble() / 10;
				}
			}
			else
			{
				mode = _annotationData.CoordinateMode.Value;

				if (mode == AnnotationCoordinateMode.Absolute)
				{
					x1 = (DateTimeOffset)_annotationData.X1 - TimeSpan.FromMinutes(1);
					x2 = (DateTimeOffset)_annotationData.X2 + TimeSpan.FromMinutes(1);
					y1 = (decimal)_annotationData.Y1 + _security.PriceStep ?? 0.01m;
					y2 = (decimal)_annotationData.Y2 - _security.PriceStep ?? 0.01m;
				}
				else
				{
					x1 = ((double)_annotationData.X1) - 0.05;
					x2 = ((double)_annotationData.X2) + 0.05;
					y1 = ((double)_annotationData.Y1) - 0.05;
					y2 = ((double)_annotationData.Y2) + 0.05;
				}
			}

			_dataThreadActions.Add(() =>
			{
				var data = new ChartDrawData.AnnotationData
				{
					X1 = x1,
					X2 = x2,
					Y1 = y1,
					Y2 = y2,
					IsVisible = true,
					Fill = RandomBrush(),
					Stroke = RandomBrush(),
					Foreground = RandomBrush(),
					Thickness = new Thickness(RandomGen.GetInt(1, 5)),
				};

				if (isNew)
				{
					data.Text = "random annotation #" + (++_annotationId);
					data.HorizontalAlignment = HorizontalAlignment.Stretch;
					data.VerticalAlignment = VerticalAlignment.Stretch;
					data.LabelPlacement = LabelPlacement.Axis;
					data.ShowLabel = true;
					data.CoordinateMode = mode;
				}

				var dd = new ChartDrawData();
				dd.Add(_annotation, data);

				Chart.Draw(dd);
			});
		}

		private void NewAnnotation_Click(object sender, RoutedEventArgs e)
		{
			if (_currCandle == null)
				return;

			var values = Enumerator.GetValues<ChartAnnotationTypes>().ToArray();

			_annotation = new ChartAnnotation { Type = values[RandomGen.GetInt(1, values.Length - 1)] };
			_annotationData = null;

			Chart.AddElement(_areaComb, _annotation);
			ModifyAnnotation(true);
		}

		private void ModifyAnnotation_Click(object sender, RoutedEventArgs e)
		{
			if (_annotation == null)
			{
				Error("no last annotation");
				return;
			}

			ModifyAnnotation(false);
		}

		private void ChartOnAnnotationCreated(ChartAnnotation ann) => _annotation = ann;

		private void ChartOnAnnotationSelected(ChartAnnotation ann, ChartDrawData.AnnotationData data)
		{
			_annotation = ann;
			_annotationData = data;
		}

		private void ChartOnAnnotationModified(ChartAnnotation ann, ChartDrawData.AnnotationData data)
		{
			_annotation = ann;
			_annotationData = data;
		}

		private void ChartOnAnnotationDeleted(ChartAnnotation ann)
		{
			if (_annotation == ann)
			{
				_annotation = null;
				_annotationData = null;
			}
		}
	}
}