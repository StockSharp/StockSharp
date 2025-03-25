using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies.HistorySMA
{
	public class SmaStrategyClassicStrategy : Strategy
	{
		private readonly StrategyParam<int> _longSmaLength;
		private readonly StrategyParam<int> _shortSmaLength;
		private readonly StrategyParam<DataType> _candleType;

		// Variables to store previous indicator values
		private decimal _prevLongValue;
		private decimal _prevShortValue;
		private bool _isFirstValue = true;

		public int LongSmaLength
		{
			get => _longSmaLength.Value;
			set => _longSmaLength.Value = value;
		}

		public int ShortSmaLength
		{
			get => _shortSmaLength.Value;
			set => _shortSmaLength.Value = value;
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public SmaStrategyClassicStrategy()
		{
			_longSmaLength = Param(nameof(LongSmaLength), 20)
							.SetGreaterThanZero()
							.SetDisplay("Long SMA Length", "Length of the long SMA indicator", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(10, 50, 5);

			_shortSmaLength = Param(nameof(ShortSmaLength), 10)
							.SetGreaterThanZero()
							.SetDisplay("Short SMA Length", "Length of the short SMA indicator", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(5, 25, 5);

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

			// Create the indicators
			var longSma = new SimpleMovingAverage { Length = LongSmaLength };
			var shortSma = new SimpleMovingAverage { Length = ShortSmaLength };

			// Add indicators to the strategy's collection for automatic IsFormed tracking
			Indicators.Add(longSma);
			Indicators.Add(shortSma);

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(longSma, shortSma, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, longSma, System.Drawing.Color.Blue);
				DrawIndicator(area, shortSma, System.Drawing.Color.Red);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// For the first value, we just store it and don't generate signals
			if (_isFirstValue)
			{
				_prevLongValue = longValue;
				_prevShortValue = shortValue;
				_isFirstValue = false;
				return;
			}

			// Get current and previous indicator values comparison
			var isShortLessCurrent = shortValue < longValue;
			var isShortLessPrev = _prevShortValue < _prevLongValue;

			// Store current values as previous for next candle
			_prevLongValue = longValue;
			_prevShortValue = shortValue;

			// Check for crossover (signal)
			if (isShortLessCurrent == isShortLessPrev)
				return;

			// Calculate position size (increase position with each trade)
			var volume = Volume + Math.Abs(Position);

			// Execute trades based on signal
			if (isShortLessCurrent)
				SellMarket(volume);
			else
				BuyMarket(volume);
		}
	}
}