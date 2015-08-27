namespace SampleRandomEmulation
{
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;

	class SmaStrategy : Strategy
	{
		private readonly CandleSeries _series;
		private bool _isShortLessThenLong;

		public SmaStrategy(CandleSeries series, SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
		{
			_series = series;

			LongSma = longSma;
			ShortSma = shortSma;
		}

		public SimpleMovingAverage LongSma { get; private set; }
		public SimpleMovingAverage ShortSma { get; private set; }

		protected override void OnStarted()
		{
			_series
				.WhenCandlesFinished()
				.Do(ProcessCandle)
				.Apply(this);

			// store current values for short and long
			_isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

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

			// update indicators
			LongSma.Process(candle);
			ShortSma.Process(candle);

			// calc new values for short and long
			var isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			// crossing happened
			if (_isShortLessThenLong != isShortLessThenLong)
			{
				// if short less than long, the sale, otherwise buy
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				// calc size for open position or revert
				var volume = Position == 0 ? Volume : Position.Abs() * 2;

				// register order (limit order)
				RegisterOrder(this.CreateOrder(direction, (decimal)(Security.GetCurrentPrice(this, direction) ?? 0), volume));

				// or revert position via market quoting
				//var strategy = new MarketQuotingStrategy(direction, volume);
				//ChildStrategies.Add(strategy);

				// store current values for short and long
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}