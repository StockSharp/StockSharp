using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies.HistorySMA
{
	public class SmaStrategyMartingaleStrategy : Strategy
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

		public SmaStrategyMartingaleStrategy()
		{
			_longSmaLength = Param(nameof(LongSmaLength), 80)
							.SetGreaterThanZero()
							.SetDisplay("Long SMA Length", "Length of the long SMA indicator", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(40, 120, 10);

			_shortSmaLength = Param(nameof(ShortSmaLength), 30)
							.SetGreaterThanZero()
							.SetDisplay("Short SMA Length", "Length of the short SMA indicator", "Indicators")
							.SetCanOptimize(true)
							.SetOptimize(10, 50, 5);

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
			var isShortLessThenLongCurrent = shortValue < longValue;
			var isShortLessThenLongPrevious = _prevShortValue < _prevLongValue;

			// Store current values as previous for next candle
			_prevLongValue = longValue;
			_prevShortValue = shortValue;

			// Check for crossover (signal)
			if (isShortLessThenLongPrevious == isShortLessThenLongCurrent)
				return;

			// Cancel any active orders before placing new ones
			CancelActiveOrders();

			// Determine direction of the trade
			var direction = isShortLessThenLongCurrent ? Sides.Sell : Sides.Buy;

			// Calculate position size (increase position with each trade - martingale approach)
			var volume = Volume + Math.Abs(Position);

			// Create and register the order with appropriate price
			var price = Security.ShrinkPrice(shortValue);
			RegisterOrder(CreateOrder(direction, price, volume));
		}
	}
}