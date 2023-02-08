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

		private readonly List<MyTrade> _myTrades = new();
		private bool? _isShortLessThenLong;

		public Subscription Subscription { get; set; }

		public IChartCandleElement ChartCandlesElem { get; set; }
		public IChartTradeElement ChartTradesElem { get; set; }
		public IChartIndicatorElement ChartLongElem { get; set; }
		public IChartIndicatorElement ChartShortElem { get; set; }

		public SimpleMovingAverage LongSma { get; } = new();
		public SimpleMovingAverage ShortSma { get; } = new();

		protected override void OnStarted()
		{
			// !!! DO NOT FORGET add it in case use AllowTrading property (see code below)
			Indicators.Add(LongSma);
			Indicators.Add(ShortSma);

			_chart = this.GetChart();

			this
				.WhenCandlesFinished(Subscription.CandleSeries)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrade()
				.Do(trade => _myTrades.Add(trade))
				.Apply(this);

			_isShortLessThenLong = null;

			Subscribe(Subscription);

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

			// all indicators added in OnStarted now is fully formed and we can use it
			if (AllowTrading)
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
					.Add(ChartCandlesElem, candle)
					.Add(ChartShortElem, shortValue)
					.Add(ChartLongElem, longValue)
					.Add(ChartTradesElem, trade);

			_chart.Draw(data);
		}
	}
}