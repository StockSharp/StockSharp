namespace StockSharp.Samples.Chart.Performance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Drawing;

using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Xaml;
using Ecng.Serialization;
using Ecng.Drawing;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using StockSharp.Configuration;
using StockSharp.Charting;

public partial class MainWindow
{
	private class LoadingContext : NotifiableObject
	{
		private string _title;

		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				NotifyChanged(nameof(Title));
			}
		}
	}

	private static readonly string _historyPath = Paths.HistoryDataPath;
	private static readonly SecurityId _securityId = "SBER@TQBR".ToSecurityId();
	private const int _timeframe = 1; //minutes
	private const decimal _priceStep = 0.01m;
	private const int _candlesPacketSize = 10;
	private const bool _addIndicator = true;
	//private const int _tradeEveryNCandles = 100;

	private IChartArea _area;
	private IChartCandleElement _candleElement;
	private IChartIndicatorElement _indicatorElement;
	private readonly CachedSynchronizedList<LightCandle> _candles = new();
	private readonly TimeSpan _tfSpan = TimeSpan.FromTicks(_timeframe);
	private readonly DispatcherTimer _chartUpdateTimer = new();
	private decimal _lastPrice;
	private DateTimeOffset _lastTime;
	private bool _dataIsLoaded;
	private TimeFrameCandleMessage _lastCandle;

	private readonly IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

	private MyMovingAverage _indicator;
	//private readonly MyMovingAverage _fpsAverage;

	//private volatile int _curCandleNum;

	private readonly Security _security = new()
	{
		Id = _securityId.ToStringId(),
		PriceStep = _priceStep,
		Board = ExchangeBoard.Forts
	};

	private readonly LoadingContext _loadingContext;

	public MainWindow()
	{
		InitializeComponent();

		Title = Title.Put(LocalizedStrings.Chart);

		_loadingContext = new LoadingContext();
		BusyIndicator.SplashScreenDataContext = _loadingContext;

		//_fpsAverage = new MyMovingAverage(10);

		Loaded += OnLoaded;

		PreviewMouseDoubleClick += (sender, args) => { Chart.IsAutoRange = true; };
		PreviewMouseWheel += (sender, args) => { Chart.IsAutoRange = false; };
		PreviewMouseRightButtonDown += (sender, args) => { Chart.IsAutoRange = false; };

		_chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
		_chartUpdateTimer.Tick += ChartUpdateTimerOnTick;
		_chartUpdateTimer.Start();
	}

	private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
	{
		Chart.FillIndicators();
		InitCharts();
		LoadData();
	}

	private void InitCharts()
	{
		Chart.ClearAreas();

		_area = Chart.AddArea();

		var yAxis = _area.YAxises.First();

		yAxis.AutoRange = true;
		Chart.IsAutoRange = true;
		Chart.IsAutoScroll = true;

		var series = new Subscription(TimeSpan.FromMinutes(_timeframe).TimeFrame(), _security);

		_indicatorElement = null;

		_candleElement = Chart.CreateCandleElement();
		_candleElement.FullTitle = "Candles";
		_candleElement.YAxisId = yAxis.Id;
		Chart.AddElement(_area, _candleElement, series);

		if (_addIndicator)
		{
			_indicator = new MyMovingAverage(200)
			{
				Name = "MyMA"
			};

			_indicatorElement = Chart.CreateIndicatorElement();
			_indicatorElement.DrawStyle = DrawStyles.Line;
			_indicatorElement.AntiAliasing = true;
			_indicatorElement.StrokeThickness = 1;
			_indicatorElement.Color = System.Drawing.Color.Blue;
			_indicatorElement.YAxisId = yAxis.Id;

			Chart.AddElement(_area, _indicatorElement, series, _indicator);
		}
	}

	private void LoadData()
	{
		_lastPrice = 0m;

		_candles.Clear();

		Chart.Reset(new IChartElement[] { _candleElement });

		var storage = new StorageRegistry();

		var maxDays = 50;

		BusyIndicator.IsSplashScreenShown = true;

		var path = _historyPath;

		//_curCandleNum = 0;

		ThreadingHelper.Thread(() =>
		{
			try
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_securityId, new LocalMarketDataDrive(path)).Load())
				{
					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						var str = $"Loading ticks for {date:dd MMM yyyy}...";
						this.GuiAsync(() => _loadingContext.Title = str);

						if (--maxDays == 0)
							break;
					}

					AppendTick(tick);
				}

				this.GuiAsync(() => BusyIndicator.IsSplashScreenShown = false);

				for (var i = 0; i < _candles.Count; i += _candlesPacketSize)
				{
					var data = new ChartDrawData();

					var candles = _candles.GetRange(i, Math.Min(_candlesPacketSize, _candles.Count - i)).Select(c => c.ToCandle(_tfSpan, _securityId));

					foreach (var candle in candles)
					{
						candle.State = CandleStates.Finished;

						var group = data.Group(candle.OpenTime);

						group.Add(_candleElement, candle);

						if (_indicatorElement != null)
							group.Add(_indicatorElement, _indicator.Process(candle));
					}

					Chart.Draw(data);
				}
			}
			catch (Exception e)
			{
				this.GuiAsync(() => Error(e.Message));
			}
			finally
			{
				this.GuiAsync(() =>
				{
					_dataIsLoaded = true;

					BusyIndicator.IsSplashScreenShown = false;

					Chart.IsAutoRange = false;
					//_area.YAxises.FirstOrDefault().Do(a => a.AutoRange = false);
				});
			}
		}).Launch();
	}

	public static decimal Round(decimal value, decimal nearest)
	{
		return Math.Round(value / nearest) * nearest;
	}

	private void ChartUpdateTimerOnTick(object sender, EventArgs eventArgs)
	{
		if (!_dataIsLoaded || IsRealtime.IsChecked != true || _lastPrice == 0m)
			return;

		_lastTime += TimeSpan.FromSeconds(10);
		var numCandles = _candles.Count;

		AppendTick(new ExecutionMessage
		{
			ServerTime = _lastTime,
			TradePrice = Round(_lastPrice + ((RandomGen.GetDouble().To<decimal>() - 0.5m) * 5 * _priceStep), _priceStep),
			TradeVolume = RandomGen.GetInt(50) + 1
		});

		TimeFrameCandleMessage candle;
		var lastLightCandle = _candles[_candles.Count - 1];

		if (_candles.Count != numCandles && _lastCandle != null)
		{
			_lastCandle.State = CandleStates.Finished;
			DrawCandle(_lastCandle);
		}

		if (_candles.Count != numCandles || _lastCandle == null)
		{
			_lastCandle = candle = lastLightCandle.ToCandle(_tfSpan, _securityId);
		}
		else
		{
			candle = _lastCandle;
			lastLightCandle.UpdateCandle(candle);
		}

		DrawCandle(candle);
	}

	private void DrawCandle(TimeFrameCandleMessage candle)
	{
		var data = new ChartDrawData();
		var group = data.Group(candle.OpenTime);

		group.Add(_candleElement, candle);

		if (_indicatorElement != null)
			group.Add(_indicatorElement, _indicator.Process(candle));

		Chart.Draw(data);
	}

	private void AppendTick(ExecutionMessage tick)
	{
		var time = tick.ServerTime;
		var candle = _candles.LastOrDefault();
		var price = tick.TradePrice.Value;
		var volume = (int)tick.TradeVolume.Value;

		if (candle == null || time >= candle.TimeTo)
		{
			var bounds = _tfSpan.GetCandleBounds(time, _security.Board);
			candle = new LightCandle
			{
				TimeFrom = bounds.Min,
				TimeTo = bounds.Max,
				Open = price + 2 * _priceStep,
				Close = price,
				High = price + 4 * _priceStep,
				Low = price - 2 * _priceStep,
			};

			_candles.Add(candle);
		}

		if (time < candle.TimeFrom)
			throw new InvalidOperationException("invalid time");

		if (price > candle.High)
			candle.High = price;

		if (price < candle.Low)
			candle.Low = price;

		candle.Close = price;
		candle.Volume += volume;

		_lastPrice = price;
		_lastTime = time;
	}

	private void Error(string msg)
	{
		new MessageBoxBuilder()
			.Owner(this)
			.Error()
			.Text(msg)
			.Show();
	}
}

class LightCandle
{
	public DateTimeOffset TimeFrom { get; set; }
	public DateTimeOffset TimeTo { get; set; }
	public decimal Open { get; set; }
	public decimal High { get; set; }
	public decimal Low { get; set; }
	public decimal Close { get; set; }
	public decimal Volume { get; set; }

	public TimeFrameCandleMessage ToCandle(TimeSpan ts, SecurityId securityId)
	{
		return new()
		{
			SecurityId = securityId,
			TypedArg = ts,
			OpenTime = TimeFrom,
			CloseTime = TimeTo,
			OpenPrice = Open,
			HighPrice = High,
			LowPrice = Low,
			ClosePrice = Close,
			TotalVolume = Volume,
		};
	}

	public void UpdateCandle(TimeFrameCandleMessage candle)
	{
		candle.OpenPrice = Open;
		candle.HighPrice = High;
		candle.LowPrice = Low;
		candle.ClosePrice = Close;
		candle.TotalVolume = Volume;
	}
}

class MyMovingAverage : IIndicator
{
	private readonly int _period;
	private readonly Queue<decimal> _values = new();
	private decimal _sum;

	public MyMovingAverage(int period)
	{
		_period = period;
	}

	public decimal Current { get; private set; }

	public DecimalIndicatorValue Process(ICandleMessage candle)
	{
		while (_values.Count >= _period)
			_sum -= _values.Dequeue();

		_values.Enqueue(candle.ClosePrice);
		_sum += candle.ClosePrice;

		Current = _sum / _values.Count;

		return new DecimalIndicatorValue(this, Current, candle.ServerTime)
		{
			IsEmpty = false,
			IsFinal = true,
		};
	}

	void IPersistable.Load(SettingsStorage storage)
	{
	}

	void IPersistable.Save(SettingsStorage storage)
	{
	}

	public IIndicator Clone() => null;
	object ICloneable.Clone() => Clone();

	IIndicatorValue IIndicator.Process(IIndicatorValue input)
	{
		throw new NotSupportedException();
	}

	void IIndicator.Reset()
	{
	}

	IIndicatorValue IIndicator.CreateValue(DateTimeOffset time, object[] values)
		=> values.Length == 0 ? new DecimalIndicatorValue(this, time) : new DecimalIndicatorValue(this, values[0].To<decimal>(), time);

	int IIndicator.NumValuesToInitialize => _period;

	Guid IIndicator.Id { get; } = default;
	public string Name { get; set; }
	bool IIndicator.IsFormed => true;
	IIndicatorContainer IIndicator.Container { get; } = null;

	IndicatorMeasures IIndicator.Measure => IndicatorMeasures.Price;

	DrawStyles IIndicator.Style => DrawStyles.Line;
	Color? IIndicator.Color => default;

	event Action<IIndicatorValue, IIndicatorValue> IIndicator.Changed
	{
		add { }
		remove { }
	}

	event Action IIndicator.Reseted
	{
		add { }
		remove { }
	}
}
