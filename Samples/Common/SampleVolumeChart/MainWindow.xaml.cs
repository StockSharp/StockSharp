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

using System.Windows.Media;

namespace SampleChart
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Xaml.Charting.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Xaml.Charting.IndicatorPainters;

	public partial class MainWindow
	{
		static readonly string HistoryPath = @"..\..\..\..\Testing\HistoryData\".ToFullPath();
		const string SecurityId = "RIZ2@FORTS";
		const int Timeframe = 1; //minutes
		const decimal PriceStep = 10m;
		const int CandlesPacketSize = 10; // количество свечей в одном вызове Draw()
		const int CandlesMultiplier = 1; // множитель количества свечей
		const bool AddTrades = false;
		const bool AddIndicator = true;
		const bool AddVolumeChartData = true;
		const int TradeEveryNCandles = 100;

		private ChartArea _area;
		private ChartCandleElement _candleElement;
		private ChartIndicatorElement _indicatorElement;
		private ChartTradeElement _tradeElement;
		readonly CachedSynchronizedList<TimeFrameCandle> _candles = new CachedSynchronizedList<TimeFrameCandle>();
		readonly TimeSpan TFSpan = TimeSpan.FromMinutes(Timeframe);

		ExponentialMovingAverage _indicator;

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

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			Theme.SelectedItem = "Chrome";
			InitCharts();

			Chart.SubscribeIndicatorElement += Chart_OnSubscribeIndicatorElement;

			LoadData();
		}

		private void Chart_OnSubscribeIndicatorElement(ChartIndicatorElement element, CandleSeries series, IIndicator indicator)
		{
// реальное количество свечей больше чем в _candles из-за множителя, поэтому этот код закомментирован
//			var values = _candles.Cache
//				.Select(candle =>
//				{
//					if (candle.State != CandleStates.Finished)
//						candle.State = CandleStates.Finished;
//
//					return new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(candle.OpenTime, new Dictionary<IChartElement, object>
//					{
//						{ element, indicator.Process(candle) }
//					});
//				});
//
//			Chart.Draw(values);
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
			_tradeElement = null;

			_candleElement = new ChartCandleElement(Timeframe, PriceStep) {FullTitle = "Candles", YAxisId = yAxis.Id};
			Chart.AddElement(_area, _candleElement, series);

			if (AddIndicator)
			{
				_indicator = new ExponentialMovingAverage { Length = 50, Name = "EMA" };
				_indicatorElement = new ChartIndicatorElement
				{
					DrawStyle = ChartIndicatorDrawStyles.Line,
					Antialiasing = true,
					StrokeThickness = 1,
					Color = Colors.Red,
					YAxisId = yAxis.Id,
				};

				Chart.AddElement(_area, _indicatorElement, series, _indicator);
			}

			if (AddTrades)
			{
				_tradeElement = new ChartTradeElement { FullTitle = "Trades" };
				Chart.AddElement(_area, _tradeElement, _security);
			}

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

		private void LoadData()
		{
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

				_candles.ForEach(c => c.State = CandleStates.Finished);
			})
			.ContinueWith(t => 
			{
				this.GuiAsync(() => BusyIndicator.IsBusy = false);

				for (var year = 0; year < CandlesMultiplier; ++year)
				{
					var localYear = year;
					for (var i = 0; i < _candles.Count; i += CandlesPacketSize)
					{
						var candles = _candles.GetRange(i, Math.Min(CandlesPacketSize, _candles.Count - i)).Select(c => IncreaseCandleYear(c, localYear));

						Chart.Draw(candles.Select(c =>
						{
							var dict = (IDictionary<IChartElement, object>)new Dictionary<IChartElement, object>
							{
								{ _candleElement, c}
							};

							if(_indicatorElement != null)
								dict.Add(_indicatorElement, _indicator.Process(c));

							if(_tradeElement != null && _curCandleNum++ % TradeEveryNCandles == 0)
								dict.Add(_tradeElement, new MyTrade
								{
									Trade = new Trade
									{
										Security = _security,
										Volume = 1,
										Price = c.LowPrice,
										OrderDirection = Sides.Buy,
										Time = c.OpenTime
									},
									Order = new Order {Security = _security, Direction = Sides.Buy}
								});
							
							return RefTuple.Create(c.OpenTime, dict);
						}).ToArray());
					}
				}
			})
			.ContinueWith(t =>
			{
				if (t.Exception != null)
					Error(t.Exception.Message);

				this.GuiAsync(() => BusyIndicator.IsBusy = false);

				Chart.IsAutoScroll = Chart.IsAutoRange = false;
				_area.YAxises.FirstOrDefault().Do(a => a.AutoRange = false);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private DateTimeOffset IncreaseYear(DateTimeOffset dt, int num)
		{
			if(dt == default(DateTimeOffset) || num == 0)
				return dt;

			return new DateTime(dt.Year + num, dt.Month, dt.Day) + dt.TimeOfDay;
		}

		private TimeFrameCandle IncreaseCandleYear(TimeFrameCandle candle, int num)
		{
			if(num == 0)
				return candle;

			candle = (TimeFrameCandle)candle.Clone();
			candle.OpenTime = IncreaseYear(candle.OpenTime, num);
			candle.CloseTime = IncreaseYear(candle.CloseTime, num);
			candle.HighTime = IncreaseYear(candle.HighTime, num);
			candle.LowTime = IncreaseYear(candle.LowTime, num);
			candle.State = CandleStates.Finished;

			return candle;
		}

		private void AppendTick(ExecutionMessage tick)
		{
			var time = tick.ServerTime;
			var candle = _candles.LastOrDefault();
			var price = tick.TradePrice.Value;
			var volume = tick.TradeVolume.Value;

			if (candle == null || time >= candle.CloseTime)
			{
				var bounds = TFSpan.GetCandleBounds(time, _security.Board);
				candle = new TimeFrameCandle
				{
					TimeFrame = TFSpan,
					OpenTime = bounds.Min,
					CloseTime = bounds.Max,
					PriceLevels = AddVolumeChartData ? new SimpleVolumeProfile() : null,
				};

				candle.OpenPrice = candle.HighPrice = candle.LowPrice = candle.ClosePrice = price;

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

			if(AddVolumeChartData)
				((SimpleVolumeProfile)candle.PriceLevels).AddVolume(price, volume);
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

	class SimpleVolumeProfile : IEnumerable<CandlePriceLevel>
	{
		readonly Dictionary<decimal, CandlePriceLevel> _volumes = new Dictionary<decimal, CandlePriceLevel>();

		public IEnumerator<CandlePriceLevel> GetEnumerator() { return _volumes.Values.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public void AddVolume(decimal price, decimal vol)
		{
			if(vol <= 0) return;

			var l = _volumes.TryGetValue(price);
			if(l == null)
				_volumes.Add(price, l = new CandlePriceLevel {Price = price});

			l.BuyVolume += vol;
		}
	}
}