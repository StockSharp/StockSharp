using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies.HistoryTrend
{
	public class OneCandleTrendStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public OneCandleTrendStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Check if candle is finished
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Trend following strategy: buy on bullish candle, sell on bearish candle
			if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
			{
				// Bullish candle - buy
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
			{
				// Bearish candle - sell
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}