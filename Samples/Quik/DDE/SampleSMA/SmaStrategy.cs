#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSMA.SampleSMAPublic
File: SmaStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSMA
{
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Messages;

	class SmaStrategy : Strategy
	{
		private readonly ICandleManager _candleManager;
		private readonly CandleSeries _series;
		private bool _isShortLessThenLong;

		public SmaStrategy(ICandleManager candleManager, CandleSeries series, SimpleMovingAverage longSma, SimpleMovingAverage shortSma)
		{
			_candleManager = candleManager;
			_series = series;

			LongSma = longSma;
			ShortSma = shortSma;
		}

		public SimpleMovingAverage LongSma { get; }
		public SimpleMovingAverage ShortSma { get; }

		protected override void OnStarted()
		{
			_candleManager
				.WhenCandlesFinished(_series)
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
				//RegisterOrder(this.CreateOrder(direction, (decimal)Security.GetCurrentPrice(direction), volume));

				// переворачиваем позицию через котирование
				var strategy = new MarketQuotingStrategy(direction, volume);
				ChildStrategies.Add(strategy);

				// запоминаем текущее положение относительно друг друга
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}