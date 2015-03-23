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

			// запоминаем текущее положение относительно друг друга
			_isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			base.OnStarted();
		}

		private void ProcessCandle(Candle candle)
		{
			// если наша стратегия в процессе остановки
			if (ProcessState == ProcessStates.Stopping)
			{
				// отменяем активные заявки
				CancelActiveOrders();
				return;
			}

			// добавляем новую свечу
			LongSma.Process(candle);
			ShortSma.Process(candle);

			// вычисляем новое положение относительно друг друга
			var isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			// если произошло пересечение
			if (_isShortLessThenLong != isShortLessThenLong)
			{
				// если короткая меньше чем длинная, то продажа, иначе, покупка.
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				// вычисляем размер для открытия или переворота позы
				var volume = Position == 0 ? Volume : Position.Abs() * 2;

				// регистрируем заявку (обычным способом - лимитированной заявкой)
				RegisterOrder(this.CreateOrder(direction, (decimal)(Security.GetCurrentPrice(this, direction) ?? 0), volume));

				// переворачиваем позицию через котирование
				//var strategy = new MarketQuotingStrategy(direction, volume);
				//ChildStrategies.Add(strategy);

				// запоминаем текущее положение относительно друг друга
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}