#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleHistoryTesting.SampleHistoryTestingPublic
File: SmaStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleHistoryTesting
{
	using System.Linq;
	using System.Collections.Generic;
	using System.Drawing;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Charting;

	class SmaStrategy : Strategy
	{
		private IChart _chart;
		private IChartCandleElement _candlesElem;
		private IChartTradeElement _tradesElem;
		private IChartIndicatorElement _shortElem;
		private IChartIndicatorElement _longElem;

		private readonly List<MyTrade> _myTrades = new();
		private readonly Subscription _series;
		private bool? _isShortLessThenLong;

		public SmaStrategy(CandleSeries series)
		{
			_series = new Subscription(series);
		}

		public SimpleMovingAverage LongSma { get; } = new();
		public SimpleMovingAverage ShortSma { get; } = new();

		protected override void OnStarted()
		{
			_chart = this.GetChart();

			if (_chart is not null)
			{
				// creates chart's components in his own thread
				_chart.ThreadDispatcher.Invoke(() =>
				{
					var area = _chart.CreateArea();
					_chart.AddArea(area);

					_candlesElem = _chart.CreateCandleElement();
					_candlesElem.ShowAxisMarker = false;
					_chart.AddElement(area, _candlesElem);

					_tradesElem = _chart.CreateTradeElement();
					_tradesElem.FullTitle = LocalizedStrings.Str985;
					_chart.AddElement(area, _tradesElem);

					_shortElem = _chart.CreateIndicatorElement();
					_shortElem.Color = Color.Coral;
					_shortElem.ShowAxisMarker = false;
					_shortElem.FullTitle = ShortSma.ToString();

					_chart.AddElement(area, _shortElem);

					_longElem = _chart.CreateIndicatorElement();
					_longElem.ShowAxisMarker = false;
					_longElem.FullTitle = LongSma.ToString();
					_chart.AddElement(area, _longElem);
				});
			}

			this
				.WhenCandlesFinished(_series.CandleSeries)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrade()
				.Do(trade => _myTrades.Add(trade))
				.Apply(this);

			_isShortLessThenLong = null;

			Subscribe(_series);

			base.OnStarted();
		}

		private void ProcessCandle(Candle candle)
		{
			// strategy are stopping
			if (ProcessState == ProcessStates.Stopping)
			{
				CancelActiveOrders();
				return;
			}

			this.AddInfoLog(LocalizedStrings.Str3634Params.Put(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.Security));

			// process new candle
			var longValue = LongSma.Process(candle);
			var shortValue = ShortSma.Process(candle);

			if (LongSma.IsFormed && ShortSma.IsFormed)
			{
				// calc new values for short and long
				var isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

				if (_isShortLessThenLong == null)
				{
					_isShortLessThenLong = isShortLessThenLong;
				}
				else if (_isShortLessThenLong != isShortLessThenLong) // crossing happened
				{
					// if short less than long, the sale, otherwise buy
					var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

					// calc size for open position or revert
					var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

					// calc order price as a close price + offset
					var price = candle.ClosePrice + ((direction == Sides.Buy ? Security.PriceStep : -Security.PriceStep) ?? 1);

					RegisterOrder(this.CreateOrder(direction, price, volume));

					// or revert position via market quoting
					//var strategy = new MarketQuotingStrategy(direction, volume);
					//ChildStrategies.Add(strategy);

					// store current values for short and long
					_isShortLessThenLong = isShortLessThenLong;
				}
			}

			var trade = _myTrades.FirstOrDefault();
			_myTrades.Clear();

			if (_chart == null)
				return;

			var data = _chart.CreateData();

			data
				.Group(candle.OpenTime)
					.Add(_candlesElem, candle)
					.Add(_shortElem, shortValue)
					.Add(_longElem, longValue)
					.Add(_tradesElem, trade);

			_chart.Draw(data);
		}
	}
}