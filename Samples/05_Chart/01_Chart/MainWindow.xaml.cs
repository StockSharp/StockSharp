namespace StockSharp.Samples.Chart;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Configuration;
using Ecng.Xaml;
using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Serialization;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Candles.Patterns;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Configuration;

public partial class MainWindow : ICandleBuilderSubscription
{
	private IChartArea _areaComb;
	private IChartCandleElement _candleElement;
	private readonly SynchronizedList<CandleMessage> _updatedCandles = new();
	private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, CandleMessage> _allCandles = new();
	private Security _security;
	private RandomWalkTradeGenerator _tradeGenerator;
	private readonly CachedSynchronizedDictionary<IChartIndicatorElement, IIndicator> _indicators = new();
	private ICandleBuilder _candleBuilder;
	private MarketDataMessage _mdMsg;
	private readonly ICandleBuilderValueTransform _candleTransform = new TickCandleBuilderValueTransform();
	private readonly CandleBuilderProvider _builderProvider = new(new InMemoryExchangeInfoProvider());
	private bool _historyLoaded;
	private bool _isRealTime;
	private DateTimeOffset _lastTime;
	private readonly Timer _dataTimer;
	private bool _isInTimerHandler;
	private readonly SyncObject _timerLock = new();
	private readonly SynchronizedList<Action> _dataThreadActions = new();
	private readonly CollectionSecurityProvider _securityProvider = new();
	private readonly TestMarketSubscriptionProvider _testProvider = new();

	private static readonly TimeSpan _realtimeInterval = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan _drawInterval = TimeSpan.FromMilliseconds(100);

	private CancellationTokenSource _drawCts = new();

	private DateTime _lastRealtimeUpdateTime;
	private DateTime _lastDrawTime;

	private IChartAnnotationElement _annotation;
	private ChartDrawData.AnnotationData _annotationData;
	private int _annotationId;

	private DateTimeOffset _lastCandleDrawTime;
	private bool _drawWithColor;
	private DrawingColor _candleDrawColor;

	MarketDataMessage ICandleBuilderSubscription.Message => _mdMsg;
	VolumeProfileBuilder ICandleBuilderSubscription.VolumeProfile { get; set; }
	public CandleMessage CurrentCandle { get; set; }

	public MainWindow()
	{
		MessageBoxBuilder.DefaultHandler = new DevExpMessageBoxHandler();

		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());

		try
		{
			ConfigManager.RegisterService<ICandlePatternProvider>(new InMemoryCandlePatternProvider());
			LoggingHelper.DoWithLog(ServicesRegistry.CandlePatternProvider.Init);
		}
		catch
		{
		}

		InitializeComponent();

		Title = Title.Put(LocalizedStrings.Chart);

		Loaded += OnLoaded;

		_dataTimer = ThreadingHelper
			.Timer(OnDataTimer)
			.Interval(TimeSpan.FromMilliseconds(1));

		SeriesEditor.DataType = TimeSpan.FromMinutes(1).TimeFrame();

		ConfigManager.RegisterService<ISubscriptionProvider>(_testProvider);
		ConfigManager.RegisterService<ISecurityProvider>(_securityProvider);

		ThemeExtensions.ApplyDefaultTheme();
	}

	private void Theme_OnClick(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.Invert();
	}

	private void HistoryPath_OnFolderChanged(string path)
	{
		using var drive = new LocalMarketDataDrive(path);
		var secs = drive.AvailableSecurities.ToArray();

		Securities.ItemsSource = secs;

		if (secs.Length > 0)
			Securities.SelectedIndex = 0;
	}

	private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
	{
		Chart.FillIndicators();
		Chart.SubscribeCandleElement += Chart_OnSubscribeCandleElement;
		Chart.SubscribeIndicatorElement += Chart_OnSubscribeIndicatorElement;
		Chart.UnSubscribeElement += Chart_OnUnSubscribeElement;
		Chart.AnnotationCreated += ChartOnAnnotationCreated;
		Chart.AnnotationModified += ChartOnAnnotationModified;
		Chart.AnnotationDeleted += ChartOnAnnotationDeleted;
		Chart.AnnotationSelected += ChartOnAnnotationSelected;

		Chart.RegisterOrder += (area, order) =>
		{
			MessageBox.Show($"RegisterOrder: sec={order.Security.Id}, {order.Side} {order.Volume}@{order.Price}");
		};

		HistoryPath.Folder = Paths.HistoryDataPath;

		Chart.SecurityProvider = _securityProvider;

		if (Securities.SelectedItem == null)
			return;

		RefreshCharts();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_dataTimer.Dispose();
		base.OnClosing(e);
	}

	private void Chart_OnSubscribeCandleElement(IChartCandleElement el, Subscription subscription)
	{
		CurrentCandle = null;
		_historyLoaded = false;
		_allCandles.Clear();
		_updatedCandles.Clear();
		_dataThreadActions.Clear();

		Chart.Reset(new[] {el});

		LoadData((SecurityId)Securities.SelectedItem, subscription.DataType);
	}

	private void Chart_OnSubscribeIndicatorElement(IChartIndicatorElement element, Subscription subscription, IIndicator indicator)
	{
		_dataThreadActions.Add(() =>
		{
			var oldReset = Chart.DisableIndicatorReset;
			try
			{
				Chart.DisableIndicatorReset = true;
				indicator.Reset();
			}
			finally
			{
				Chart.DisableIndicatorReset = oldReset;
			}

			var chartData = Chart.CreateData();

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
			_drawCts.Cancel();
			_drawCts = new();

			if (element is IChartIndicatorElement indElem)
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

		_drawCts.Cancel();
		_drawCts = new();

		this.GuiSync(() =>
		{
			Chart.ClearAreas();

			_areaComb = Chart.AddArea();

			var yAxis = _areaComb.YAxises.First();

			yAxis.AutoRange = true;
			Chart.IsAutoRange = true;
			Chart.IsAutoScroll = true;

			var id = (SecurityId)Securities.SelectedItem;

			_security = new Security
			{
				Id = id.ToStringId(),
			};

			_securityProvider.Clear();
			_securityProvider.Add(_security);

			_tradeGenerator = new RandomWalkTradeGenerator(id);
			_tradeGenerator.Init();
			_tradeGenerator.Process(_security.ToMessage());

			_candleElement = Chart.CreateCandleElement();
			_candleElement.PriceStep = 20;
			Chart.AddElement(_areaComb, _candleElement, new Subscription(SeriesEditor.DataType, _security));
		});
	}

	private void Draw_Click(object sender, RoutedEventArgs e)
	{
		RefreshCharts();
	}

	private void LoadData(SecurityId secId, DataType dt)
	{
		var msgType = dt.MessageType;

		_candleTransform.Process(new ResetMessage());
		_candleBuilder = _builderProvider.Get(msgType);

		var storage = new StorageRegistry();

		//BusyIndicator.IsBusy = true;

		var path = HistoryPath.Folder;
		var isBuild = BuildFromTicks.IsChecked == true;
		var format = Format.SelectedFormat;

		var maxDays = (isBuild || !dt.IsTFCandles)
			? 15
			: 30 * (int)dt.GetTimeFrame().TotalMinutes;

		_mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = dt,
			SecurityId = secId,
			IsCalcVolumeProfile = true,
		};

		var token = _drawCts.Token;

		Task.Factory.StartNew(() =>
		{
			var date = DateTime.MinValue;

			if (isBuild)
			{
				foreach (var tick in storage.GetTickMessageStorage(secId, new LocalMarketDataDrive(path), format).Load())
				{
					if(token.IsCancellationRequested)
						break;

					_tradeGenerator.Process(tick);

					if (_candleTransform.Process(tick))
					{
						var candles = _candleBuilder.Process(this, _candleTransform);

						foreach (var candle in candles)
						{
							_updatedCandles.Add(candle.TypedClone());
						}
					}

					_lastTime = tick.ServerTime;

					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						//var str = date.To<string>();
						//this.GuiAsync(() => BusyIndicator.BusyContent = str);

						maxDays--;

						if (maxDays == 0)
							break;
					}
				}
			}
			else
			{
				foreach (var candleMsg in storage.GetCandleMessageStorage(msgType, secId, dt.Arg, new LocalMarketDataDrive(path), format).Load())
				{
					if(token.IsCancellationRequested)
						break;

					if (candleMsg.State != CandleStates.Finished)
						candleMsg.State = CandleStates.Finished;

					CurrentCandle = candleMsg;
					_updatedCandles.Add(candleMsg);

					_lastTime = candleMsg.OpenTime;

					if (candleMsg is TimeFrameCandleMessage)
						_lastTime += dt.GetTimeFrame();

					_tradeGenerator.Process(new ExecutionMessage
					{
						DataTypeEx = DataType.Ticks,
						SecurityId = secId,
						ServerTime = _lastTime,
						TradePrice = candleMsg.ClosePrice,
					});

					if (date != candleMsg.OpenTime.Date)
					{
						date = candleMsg.OpenTime.Date;

						//var str = date.To<string>();
						//this.GuiAsync(() => BusyIndicator.BusyContent = str);

						maxDays--;

						if (maxDays == 0)
							break;
					}
				}
			}

			_historyLoaded = true;
		}, token)
		.ContinueWith(t =>
		{
			if (t.Exception != null)
				Error(t.Exception.Message);

			//BusyIndicator.IsBusy = false;
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
				var actions = _dataThreadActions.SyncGet(l => l.CopyAndClear());
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
			if(nextTick.TradePrice != null)
				_testProvider.UpdateData(_security, nextTick.TradePrice.Value);

			if (_candleTransform.Process(nextTick))
			{
				var candles = _candleBuilder.Process(this, _candleTransform);

				foreach (var candle in candles)
				{
					_updatedCandles.Add(candle.TypedClone());
				}
			}
		}

		_lastTime += TimeSpan.FromMilliseconds(RandomGen.GetInt(100, 10000));
	}

	private static DrawingColor GetRandomColor() => DrawingColor.FromArgb(255, (byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255), (byte)RandomGen.GetInt(0, 255));

	private void DrawChartElements()
	{
		var messages = _updatedCandles.SyncGet(uc => uc.CopyAndClear());

		if (messages.Length == 0)
			return;

		var lastTime = DateTimeOffset.MinValue;
		var candlesToUpdate = new List<CandleMessage>();

		foreach (var candle in messages.Reverse())
		{
			if (lastTime == candle.OpenTime)
				continue;

			lastTime = candle.OpenTime;

			if (candlesToUpdate.Count == 0 || candlesToUpdate.Last() != candle)
				candlesToUpdate.Add(candle);
		}

		candlesToUpdate.Reverse();

		foreach (var candle in candlesToUpdate)
			_allCandles[candle.OpenTime] = candle;

		IChartDrawData chartData = null;

		foreach (var candle in candlesToUpdate)
		{
			if (chartData == null)
				chartData = Chart.CreateData();

			if (_lastCandleDrawTime != candle.OpenTime)
			{
				_lastCandleDrawTime = candle.OpenTime;
				_candleDrawColor = GetRandomColor();
			}

			var chartGroup = chartData.Group(candle.OpenTime);
			chartGroup.Add(_candleElement, candle);
			chartGroup.Add(_candleElement, _drawWithColor ? _candleDrawColor : null);

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
			_candleElement.Colorer = (dto, isUpCandle, isLastCandle) => dto.Hour % 2 != 0 ? null : (isUpCandle ? DrawingColor.Chartreuse : DrawingColor.Aqua);
			_indicators.Keys.ForEach(el => el.Colorer = c => ((DateTimeOffset)c).Hour % 2 != 0 ? null : DrawingColor.Magenta);
		}
		else
		{
			_candleElement.Colorer = null;
			_indicators.Keys.ForEach(el => el.Colorer = null);
		}

		// refresh prev painted elements
		Chart.Draw(Chart.CreateData());
	}

	private void CustomColors2_Changed(object sender, RoutedEventArgs e)
	{
		var colored = CustomColors2.IsChecked == true;
		_drawWithColor = colored;
		_dataThreadActions.Add(() =>
		{
			if(_allCandles.IsEmpty())
				return;

			var dd = Chart.CreateData();
			foreach (var c in _allCandles)
				dd.Group(c.Value.OpenTime).Add(_candleElement, colored ? GetRandomColor() : null);

			Chart.Draw(dd);
		});
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
		static Brush RandomBrush()
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

			var dd = Chart.CreateData();
			dd.Add(_annotation, data);

			Chart.Draw(dd);
		});
	}

	private void NewAnnotation_Click(object sender, RoutedEventArgs e)
	{
		if (CurrentCandle == null)
			return;

		var values = Enumerator.GetValues<ChartAnnotationTypes>().ToArray();

		_annotation = Chart.CreateAnnotation();
		_annotation.Type = values[RandomGen.GetInt(1, values.Length - 1)];
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

	private void ChartOnAnnotationCreated(IChartAnnotationElement ann) => _annotation = ann;

	private void ChartOnAnnotationSelected(IChartAnnotationElement ann, ChartDrawData.AnnotationData data)
	{
		_annotation = ann;
		_annotationData = data;
	}

	private void ChartOnAnnotationModified(IChartAnnotationElement ann, ChartDrawData.AnnotationData data)
	{
		_annotation = ann;
		_annotationData = data;
	}

	private void ChartOnAnnotationDeleted(IChartAnnotationElement ann)
	{
		if (_annotation == ann)
		{
			_annotation = null;
			_annotationData = null;
		}
	}

	private class TestMarketSubscriptionProvider : ISubscriptionProvider
	{
		private readonly HashSet<Subscription> _l1Subscriptions = new();

		public void UpdateData(Security sec, decimal price)
		{
			var ps = sec.PriceStep ?? 1;

			var msg = new Level1ChangeMessage
			{
				SecurityId = sec.ToSecurityId(),
				ServerTime = DateTimeOffset.UtcNow,
			};

			if (RandomGen.GetBool())
				msg.Changes.TryAdd(Level1Fields.BestBidPrice, price - RandomGen.GetInt(1, 10) * ps);

			if (RandomGen.GetBool())
				msg.Changes.TryAdd(Level1Fields.BestAskPrice, price + RandomGen.GetInt(1, 10) * ps);

			foreach (var l1Subscriptions in _l1Subscriptions)
			{
				_level1Received?.Invoke(l1Subscriptions, msg);
			}
		}

		private event Action<Subscription, Level1ChangeMessage> _level1Received;

		event Action<Subscription, Level1ChangeMessage> ISubscriptionProvider.Level1Received
		{
			add => _level1Received += value;
			remove => _level1Received -= value;
		}

		IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => _l1Subscriptions;

		Subscription ISubscriptionProvider.SecurityLookup => default;
		Subscription ISubscriptionProvider.PortfolioLookup => default;
		Subscription ISubscriptionProvider.BoardLookup => default;
		Subscription ISubscriptionProvider.OrderLookup => default;
		Subscription ISubscriptionProvider.DataTypeLookup => default;

		event Action<Subscription, object> ISubscriptionProvider.SubscriptionReceived { add { } remove { } }

		event Action<Subscription, IOrderBookMessage> ISubscriptionProvider.OrderBookReceived { add { } remove { } }
		event Action<Subscription, ITickTradeMessage> ISubscriptionProvider.TickTradeReceived { add { } remove { } }
		event Action<Subscription, IOrderLogMessage> ISubscriptionProvider.OrderLogReceived { add { } remove { } }
		event Action<Subscription, Security> ISubscriptionProvider.SecurityReceived { add { } remove { } }
		event Action<Subscription, ExchangeBoard> ISubscriptionProvider.BoardReceived { add { } remove { } }
		event Action<Subscription, News> ISubscriptionProvider.NewsReceived { add { } remove { } }
		event Action<Subscription, ICandleMessage> ISubscriptionProvider.CandleReceived { add { } remove { } }
		event Action<Subscription, MyTrade> ISubscriptionProvider.OwnTradeReceived { add { } remove { } }
		event Action<Subscription, Order> ISubscriptionProvider.OrderReceived { add { } remove { } }
		event Action<Subscription, OrderFail> ISubscriptionProvider.OrderRegisterFailReceived { add { } remove { } }
		event Action<Subscription, OrderFail> ISubscriptionProvider.OrderCancelFailReceived { add { } remove { } }
		event Action<Subscription, OrderFail> ISubscriptionProvider.OrderEditFailReceived { add { } remove { } }
		event Action<Subscription, Portfolio> ISubscriptionProvider.PortfolioReceived { add { } remove { } }
		event Action<Subscription, Position> ISubscriptionProvider.PositionReceived { add { } remove { } }
		event Action<Subscription, DataType> ISubscriptionProvider.DataTypeReceived { add { } remove { } }
		
		event Action<Subscription> ISubscriptionProvider.SubscriptionOnline { add { } remove { } }
		event Action<Subscription> ISubscriptionProvider.SubscriptionStarted { add { } remove { } }
		event Action<Subscription, Exception> ISubscriptionProvider.SubscriptionStopped { add { } remove { } }
		event Action<Subscription, Exception, bool> ISubscriptionProvider.SubscriptionFailed { add { } remove { } }

		void ISubscriptionProvider.Subscribe(Subscription subscription)
		{
			if (subscription.DataType == DataType.Level1)
				_l1Subscriptions.Add(subscription);
		}

		void ISubscriptionProvider.UnSubscribe(Subscription subscription)
		{
			_l1Subscriptions.Remove(subscription);
		}
	}
}
