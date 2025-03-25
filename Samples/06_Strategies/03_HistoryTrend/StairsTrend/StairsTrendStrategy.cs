using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies.HistoryTrend
{
	public class StairsTrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _lengthParam;
		private readonly StrategyParam<DataType> _candleType;
		
		private int _bullLength;
		private int _bearLength;
		
		public int Length
		{
			get => _lengthParam.Value;
			set => _lengthParam.Value = value;
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public StairsTrendStrategy()
		{
			_lengthParam = Param(nameof(Length), 3)
						   .SetGreaterThanZero()
						   .SetDisplay("Length", "Number of consecutive candles to trigger signal", "Strategy")
						   .SetCanOptimize(true)
						   .SetOptimize(2, 10, 1);

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
			
			// Reset counters
			_bullLength = 0;
			_bearLength = 0;

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

			// Update counters based on candle direction
			if (candle.OpenPrice < candle.ClosePrice)
			{
				// Bullish candle
				_bullLength++;
				_bearLength = 0;
			}
			else if (candle.OpenPrice > candle.ClosePrice)
			{
				// Bearish candle
				_bullLength = 0;
				_bearLength++;
			}

			// Trend following strategy: 
			// Buy after Length consecutive bullish candles
			if (_bullLength >= Length && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Sell after Length consecutive bearish candles
			else if (_bearLength >= Length && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}