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
		static readonly string HistoryPath = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
		const string SecurityId = "RIZ2@FORTS";
		const int Timeframe = 1; //minutes
		const int PriceStep = 10;
		const int CandlesPacketSize = 10; // количество свечей в одном вызове Draw()
		const bool AddIndicator = true;
		const int TradeEveryNCandles = 100;

		private ChartArea _area;
		private ChartCandleElement _candleElement;
		private ChartIndicatorElement _indicatorElement;
		readonly CachedSynchronizedList<LightCandle> _candles = new CachedSynchronizedList<LightCandle>();
		readonly TimeSpan TFSpan = TimeSpan.FromTicks(Timeframe);
		private readonly DispatcherTimer _chartUpdateTimer = new DispatcherTimer();
		private decimal _lastPrice;
		private DateTimeOffset _lastTime;
		private bool _dataIsLoaded;
		private TimeFrameCandle _lastCandle;

		MyMovingAverage _indicator;
		readonly MyMovingAverage _fpsAverage;

		volatile int _curCandleNum;

		private Security _security = new Security
		{
			Id = SecurityId,
			PriceStep = PriceStep,
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
				TimeSpan.FromMinutes(Timeframe));

			_indicatorElement = null;

			_candleElement = new ChartCandleElement {FullTitle = "Candles", YAxisId = yAxis.Id};
			Chart.AddElement(_area, _candleElement, series);

			if (AddIndicator)
			{
				_indicator = new MyMovingAverage(200) {Name = "MyMA"};

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
			var id = new SecurityIdGenerator().Split(SecurityId);

			_security = new Security
			{
				Id = SecurityId,
				PriceStep = PriceStep,
				Board = ExchangeBoard.GetBoard(id.BoardCode)
			};

			Chart.Reset(new IChartElement[] { _candleElement });

			var storage = new StorageRegistry();

			var maxDays = 50;

			BusyIndicator.IsBusy = true;

			var path = HistoryPath;

			_curCandleNum = 0;

			Task.Factory.StartNew(() =>
			{
				var date = DateTime.MinValue;

				foreach (var tick in storage.GetTickMessageStorage(_security, new LocalMarketDataDrive(path)).Load())
				{
					if (date != tick.ServerTime.Date)
					{
						date = tick.ServerTime.Date;

						this.GuiAsync(() => BusyIndicator.BusyContent = $"Loading ticks for {date:dd MMM yyyy}...");

						if (--maxDays == 0)
							break;
					}

					AppendTick(tick);
				}
			})
			.ContinueWith(t => 
			{
				this.GuiAsync(() => BusyIndicator.IsBusy = false);

				for (var i = 0; i < _candles.Count; i += CandlesPacketSize)
				{
					var data = new ChartDrawData();

					var candles = _candles.GetRange(i, Math.Min(CandlesPacketSize, _candles.Count - i)).Select(c => c.ToCandle(TFSpan));

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
			if(!_dataIsLoaded || IsRealtime.IsChecked != true || _lastPrice == 0m)
				return;

			_lastTime += TimeSpan.FromSeconds(10);
			var numCandles = _candles.Count;

			AppendTick(new ExecutionMessage
			{
				ServerTime = _lastTime,
				TradePrice = Round(_lastPrice + (decimal)((RandomGen.GetDouble() - 0.5) * 5 * PriceStep), PriceStep),
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
				_lastCandle = candle = lastLightCandle.ToCandle(TFSpan);
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
				var bounds = TFSpan.GetCandleBounds(time, _security.Board);
				candle = new LightCandle
				{
					TimeFrom = bounds.Min,
					TimeTo = bounds.Max,
					Open = price + 2*PriceStep,
					Close = price,
					High = price + 4*PriceStep,
					Low = price - 2*PriceStep,
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
		public DateTimeOffset TimeFrom {get; set;}
		public DateTimeOffset TimeTo {get; set;}
		public int Open {get; set;}
		public int High {get; set;}
		public int Low {get; set;}
		public int Close {get; set;}
		public int Volume {get; set;}

		public TimeFrameCandle ToCandle(TimeSpan ts)
		{
			return new TimeFrameCandle
			{
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
		readonly int _period;
		readonly Queue<double> _values = new Queue<double>();
		double _sum;

		public MyMovingAverage(int period)
		{
			_period = period;
		}

		public double Current { get; private set; }

		public DecimalIndicatorValue Process(double newValue)
		{
			while(_values.Count >= _period)
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

		public void Load(SettingsStorage storage) {}
		public void Save(SettingsStorage storage) {}
		public IIndicator Clone() =>  null;
		object ICloneable.Clone() => Clone();

		public IIndicatorValue Process(IIndicatorValue input) { throw new NotImplementedException(); }

		public void Reset() {}
		public Guid Id { get; }
		public string Name { get; set; }
		public bool IsFormed => true;
		public IIndicatorContainer Container { get; }

		public event Action<IIndicatorValue, IIndicatorValue> Changed;
		public event Action Reseted;
	}
}