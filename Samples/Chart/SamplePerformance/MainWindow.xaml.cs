#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SamplePerformance.SamplePerformancePublic
File: MainWindow.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace SamplePerformance
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	public partial class MainWindow
	{
		private static readonly string _historyPath = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
		private const string _securityId = "RIZ2@FORTS";
		private const int _timeframe = 1; //minutes
		private const int _priceStep = 10;
		private const int _candlesPacketSize = 10; // количество свечей в одном вызове Draw()
		private const bool _addIndicator = true;
		private const int _tradeEveryNCandles = 100;

		private ChartArea _area;
		private ChartCandleElement _candleElement;
		private ChartIndicatorElement _indicatorElement;
		private readonly CachedSynchronizedList<LightCandle> _candles = new CachedSynchronizedList<LightCandle>();
		private readonly TimeSpan _tfSpan = TimeSpan.FromTicks(_timeframe);
		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private decimal _lastPrice;
		private DateTimeOffset _lastTime;
		private bool _dataIsLoaded;
		private TimeFrameCandle _lastCandle;

		private readonly IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		private MyMovingAverage _indicator;
		private readonly MyMovingAverage _fpsAverage;

		private volatile int _curCandleNum;

		private Security _security = new Security
		{
			Id = _securityId,
			PriceStep = _priceStep,
			Board = ExchangeBoard.Forts
		};

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put(LocalizedStrings.Str3200);

			_fpsAverage = new MyMovingAverage(10);

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
			Theme.SelectedItem = "Chrome";

			Chart.FillIndicators();
			InitCharts();
			LoadData();
		}

		private void InitCharts()
		{
			Chart.ClearAreas();

			_area = new ChartArea();

			var yAxis = _area.YAxises.First();

			yAxis.AutoRange = true;
			Chart.IsAutoRange = true;
			Chart.IsAutoScroll = true;

			Chart.AddArea(_area);

			var series = new CandleSeries(
				typeof(TimeFrameCandle),
				_security,
				TimeSpan.FromMinutes(_timeframe));

			_indicatorElement = null;

			_candleElement = new ChartCandleElement
			{
				FullTitle = "Candles",
				YAxisId = yAxis.Id
			};
			Chart.AddElement(_area, _candleElement, series);

			if (_addIndicator)
			{
				_indicator = new MyMovingAverage(200)
				{
					Name = "MyMA"
				};

				_indicatorElement = new ChartIndicatorElement
				{
					DrawStyle = ChartIndicatorDrawStyles.Line,
					AntiAliasing = true,
					StrokeThickness = 1,
					Color = Colors.Blue,
					YAxisId = yAxis.Id,
				};

				Chart.AddElement(_area, _indicatorElement, series, _indicator);
			}
		}

		private void LoadData()
		{
			_lastPrice = 0m;

			_candles.Clear();
			var id = _securityId.ToSecurityId();

			_security = new Security
			{
				Id = _securityId,
				PriceStep = _priceStep,
				Board = _exchangeInfoProvider.GetExchangeBoard(id.BoardCode) ?? ExchangeBoard.Associated
			};

			Chart.Reset(new IChartElement[] { _candleElement });

			var storage = new StorageRegistry();

			var maxDays = 50;

			BusyIndicator.IsBusy = true;

			var path = _historyPath;

			_curCandleNum = 0;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security, new LocalMarketDataDrive(path)).Load())
				{
					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						var str = $"Loading ticks for {date:dd MMM yyyy}...";
						this.GuiAsync(() => BusyIndicator.BusyContent = str);

						if (--maxDays == 0)
							break;
					}

					AppendTick(tick);
				}
			})
			.ContinueWith(t =>
			{
				this.GuiAsync(() => BusyIndicator.IsBusy = false);

				for (var i = 0; i < _candles.Count; i += _candlesPacketSize)
				{
					var data = new ChartDrawData();

					var candles = _candles.GetRange(i, Math.Min(_candlesPacketSize, _candles.Count - i)).Select(c => c.ToCandle(_tfSpan, _security));

					foreach (var candle in candles)
					{
						candle.State = CandleStates.Finished;

						var group = data.Group(candle.OpenTime);

						group.Add(_candleElement, candle);

						if (_indicatorElement != null)
							group.Add(_indicatorElement, _indicator.Process((double)candle.ClosePrice));
					}

					Chart.Draw(data);
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				_dataIsLoaded = true;

				this.GuiAsync(() => BusyIndicator.IsBusy = false);

				Chart.IsAutoRange = false;
				//_area.YAxises.FirstOrDefault().Do(a => a.AutoRange = false);
			}, TaskScheduler.FromCurrentSynchronizationContext());
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
				TradePrice = Round(_lastPrice + (decimal)((RandomGen.GetDouble() - 0.5) * 5 * _priceStep), _priceStep),
				TradeVolume = RandomGen.GetInt(50) + 1
			});

			TimeFrameCandle candle;
			var lastLightCandle = _candles[_candles.Count - 1];

			if (_candles.Count != numCandles && _lastCandle != null)
			{
				_lastCandle.State = CandleStates.Finished;
				DrawCandle(_lastCandle);
			}

			if (_candles.Count != numCandles || _lastCandle == null)
			{
				_lastCandle = candle = lastLightCandle.ToCandle(_tfSpan, _security);
			}
			else
			{
				candle = _lastCandle;
				lastLightCandle.UpdateCandle(candle);
			}

			DrawCandle(candle);
		}

		private void DrawCandle(TimeFrameCandle candle)
		{
			var data = new ChartDrawData();
			var group = data.Group(candle.OpenTime);

			group.Add(_candleElement, candle);

			if (_indicatorElement != null)
				group.Add(_indicatorElement, _indicator.Process((double)candle.ClosePrice));

			Chart.Draw(data);
		}

		private void AppendTick(ExecutionMessage tick)
		{
			var time = tick.ServerTime;
			var candle = _candles.LastOrDefault();
			var price = (int)tick.TradePrice.Value;
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

		private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var theme = (string)Theme.SelectedValue;
			Chart.ChartTheme = theme;
		}
	}

	class LightCandle
	{
		public DateTimeOffset TimeFrom { get; set; }
		public DateTimeOffset TimeTo { get; set; }
		public int Open { get; set; }
		public int High { get; set; }
		public int Low { get; set; }
		public int Close { get; set; }
		public int Volume { get; set; }

		public TimeFrameCandle ToCandle(TimeSpan ts, Security security)
		{
			return new TimeFrameCandle
			{
				Security = security,
				TimeFrame = ts,
				OpenTime = TimeFrom,
				CloseTime = TimeTo,
				OpenPrice = Open,
				HighPrice = High,
				LowPrice = Low,
				ClosePrice = Close,
				TotalVolume = Volume,
			};
		}

		public void UpdateCandle(TimeFrameCandle candle)
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
		private readonly Queue<double> _values = new Queue<double>();
		private double _sum;

		public MyMovingAverage(int period)
		{
			_period = period;
		}

		public double Current { get; private set; }

		public DecimalIndicatorValue Process(double newValue)
		{
			while (_values.Count >= _period)
				_sum -= _values.Dequeue();

			_values.Enqueue(newValue);
			_sum += newValue;

			Current = _sum / _values.Count;

			return new DecimalIndicatorValue(this, (decimal)Current)
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

		Guid IIndicator.Id { get; } = default(Guid);
		public string Name { get; set; }
		bool IIndicator.IsFormed => true;
		IIndicatorContainer IIndicator.Container { get; } = null;
		Type IIndicator.InputType { get; } = typeof(DecimalIndicatorValue);
		Type IIndicator.ResultType { get; } = typeof(DecimalIndicatorValue);

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
}