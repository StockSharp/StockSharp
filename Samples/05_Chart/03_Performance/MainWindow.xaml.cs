namespace StockSharp.Samples.Chart.Performance;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

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
using StockSharp.Xaml;
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
	private static readonly SecurityId _securityId = Paths.HistoryDefaultSecurity.ToSecurityId();
	private const int _timeframe = 1; //minutes
	private const decimal _priceStep = 0.01m;
	private const int _candlesPacketSize = 10;
	private const bool _addIndicator = true;
	//private const int _tradeEveryNCandles = 100;

	private IChartArea _area;
	private IChartCandleElement _candleElement;
	private IChartIndicatorElement _indicatorElement;
	private readonly CachedSynchronizedList<TimeFrameCandleMessage> _candles = [];
	private readonly TimeSpan _tfSpan = TimeSpan.FromTicks(_timeframe);
	private readonly DispatcherTimer _chartUpdateTimer = new();
	private decimal _lastPrice;
	private DateTime _lastTime;
	private bool _dataIsLoaded;
	private TimeFrameCandleMessage _lastCandle;

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
		ThemeExtensions.ApplyDefaultTheme();
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

		Chart.Reset([_candleElement]);

		var storage = new StorageRegistry();

		var maxDays = 50;

		BusyIndicator.IsSplashScreenShown = true;

		var path = _historyPath;

		var token = CancellationToken.None;

		//_curCandleNum = 0;

		Task.Factory.StartNew(async () =>
		{
			try
			{
				var date = DateTime.MinValue;

				await foreach (var tick in storage.GetTickMessageStorage(_securityId, new LocalMarketDataDrive(path)).LoadAsync(null, null).WithEnforcedCancellation(token))
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

					var candles = _candles.GetRange(i, Math.Min(_candlesPacketSize, _candles.Count - i));

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
		}, TaskCreationOptions.LongRunning);
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
		var lastCandle = _candles[^1];

		if (_candles.Count != numCandles && _lastCandle != null)
		{
			_lastCandle.State = CandleStates.Finished;
			DrawCandle(_lastCandle);
		}

		if (_candles.Count != numCandles || _lastCandle == null)
		{
			_lastCandle = candle = lastCandle;
		}
		else
		{
			candle = _lastCandle;

			candle.OpenPrice = lastCandle.OpenPrice;
			candle.HighPrice = lastCandle.HighPrice;
			candle.LowPrice = lastCandle.LowPrice;
			candle.ClosePrice = lastCandle.ClosePrice;
			candle.TotalVolume = lastCandle.TotalVolume;
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

		if (candle == null || time >= candle.CloseTime)
		{
			var bounds = _tfSpan.GetCandleBounds(time, _security.Board);
			candle = new TimeFrameCandleMessage
			{
				OpenTime = bounds.Min,
				CloseTime = bounds.Max,
				OpenPrice = price + 2 * _priceStep,
				ClosePrice = price,
				HighPrice = price + 4 * _priceStep,
				LowPrice = price - 2 * _priceStep,
				SecurityId = _securityId,
				TypedArg = _tfSpan,
			};

			_candles.Add(candle);
		}

		if (time < candle.OpenTime)
			throw new InvalidOperationException("invalid time");

		if (price > candle.HighPrice)
			candle.HighPrice = price;

		if (price < candle.LowPrice)
			candle.LowPrice = price;

		candle.ClosePrice = price;
		candle.TotalVolume += volume;

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

class MyMovingAverage(int period) : IIndicator
{
	private readonly int _period = period;
	private readonly Queue<decimal> _values = [];
	private decimal _sum;

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

	IIndicatorValue IIndicator.CreateValue(DateTime time, object[] values)
		=> values.Length == 0 ? new DecimalIndicatorValue(this, time) : new DecimalIndicatorValue(this, values[0].To<decimal>(), time);

	int IIndicator.NumValuesToInitialize => _period;

	Guid IIndicator.Id { get; } = default;
	public string Name { get; set; }
	bool IIndicator.IsFormed => true;
	IIndicatorContainer IIndicator.Container { get; } = null;

	IndicatorMeasures IIndicator.Measure => IndicatorMeasures.Price;

	Level1Fields? IIndicator.Source { get; set; }

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
