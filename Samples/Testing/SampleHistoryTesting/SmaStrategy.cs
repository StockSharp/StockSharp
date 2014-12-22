namespace SampleHistoryTesting
{
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	class SmaStrategy : Strategy
	{
		private readonly IChart _chart;
		private readonly ChartCandleElement _candlesElem;
		private readonly ChartTradeElement _tradesElem;
		private readonly ChartIndicatorElement _shortElem;
		private readonly ChartIndicatorElement _longElem;
		private readonly List<MyTrade> _myTrades = new List<MyTrade>();
		private readonly CandleSeries _series;
		private bool _isShortLessThenLong;

		public SmaStrategy(IChart chart, ChartCandleElement candlesElem, ChartTradeElement tradesElem, 
			SimpleMovingAverage shortMa, ChartIndicatorElement shortElem,
			SimpleMovingAverage longMa, ChartIndicatorElement longElem,
			CandleSeries series)
		{
			_chart = chart;
			_candlesElem = candlesElem;
			_tradesElem = tradesElem;
			_shortElem = shortElem;
			_longElem = longElem;
			
			_series = series;

			ShortSma = shortMa;
			LongSma = longMa;
		}

		public SimpleMovingAverage LongSma { get; private set; }
		public SimpleMovingAverage ShortSma { get; private set; }

		protected override void OnStarted()
		{
			_series
				.WhenCandlesFinished()
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrades()
				.Do(trades => _myTrades.AddRange(trades))
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

			this.AddInfoLog(LocalizedStrings.Str2177Params.Put(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume));

			// добавляем новую свечу
			var longValue = LongSma.Process(candle);
			var shortValue = ShortSma.Process(candle);

			// вычисляем новое положение относительно друг друга
			var isShortLessThenLong = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();

			// если произошло пересечение
			if (_isShortLessThenLong != isShortLessThenLong)
			{
				// если короткая меньше чем длинная, то продажа, иначе, покупка.
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				// вычисляем размер для открытия или переворота позы
				var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

				if (!SafeGetConnector().RegisteredMarketDepths.Contains(Security))
				{
					var price = Security.GetMarketPrice(Connector, direction);

					// регистрируем псевдо-маркетную заявку - лимитная заявка с ценой гарантирующей немедленное исполнение.
					if (price != null)
						RegisterOrder(this.CreateOrder(direction, price.Value, volume));
				}
				else
				{
					// переворачиваем позицию через котирование
					var strategy = new MarketQuotingStrategy(direction, volume)
					{
						WaitAllTrades = true,
					};
					ChildStrategies.Add(strategy);
				}

				// запоминаем текущее положение относительно друг друга
				_isShortLessThenLong = isShortLessThenLong;
			}

			var trade = _myTrades.FirstOrDefault();
			_myTrades.Clear();

			var dict = new Dictionary<IChartElement, object>
			{
				{ _candlesElem, candle },
				{ _shortElem, shortValue },
				{ _longElem, longValue },
				{ _tradesElem, trade }
			};

			_chart.Draw(candle.OpenTime, dict);
		}
	}
}