namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Candles.VolumePriceStatistics;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Вспомогательный класс для работы со свечами.
	/// </summary>
	public static class CandleHelper
	{
		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="TimeFrameCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries TimeFrame(this Security security, TimeSpan arg)
		{
			return new CandleSeries(typeof(TimeFrameCandle), security, arg);
		}

		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="RangeCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="RangeCandle.PriceRange"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries Range(this Security security, Unit arg)
		{
			return new CandleSeries(typeof(RangeCandle), security, arg);
		}

		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="VolumeCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="VolumeCandle.Volume"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries Volume(this Security security, decimal arg)
		{
			return new CandleSeries(typeof(VolumeCandle), security, arg);
		}

		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="TickCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="TickCandle.MaxTradeCount"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries Tick(this Security security, decimal arg)
		{
			return new CandleSeries(typeof(TickCandle), security, arg);
		}

		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="PnFCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="PnFCandle.PnFArg"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries PnF(this Security security, PnFArg arg)
		{
			return new CandleSeries(typeof(PnFCandle), security, arg);
		}

		/// <summary>
		/// Создать <see cref="CandleSeries"/> для свечек <see cref="RenkoCandle"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Значение <see cref="RenkoCandle.BoxSize"/>.</param>
		/// <returns>Серия свечек.</returns>
		public static CandleSeries Renko(this Security security, Unit arg)
		{
			return new CandleSeries(typeof(RenkoCandle), security, arg);
		}

		/// <summary>
		/// Запустить получение свечек.
		/// </summary>
		/// <param name="manager">Менеджер свечек.</param>
		/// <param name="series">Серия свечек.</param>
		public static void Start(this ICandleManager manager, CandleSeries series)
		{
			manager.ThrowIfNull().Start(series, series.From, series.To);
		}

		/// <summary>
		/// Прекратить получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public static void Stop(this CandleSeries series)
		{
			var manager = series.ThrowIfNull().CandleManager;

			// серию ранее не запускали, значит и останавливать не нужно
			if (manager == null)
				return;

			manager.Stop(series);
		}

		private static ICandleManagerContainer GetContainer(this CandleSeries series)
		{
			return series.ThrowIfNull().CandleManager.Container;
		}

		/// <summary>
		/// Получить количество свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Количество свечек.</returns>
		public static int GetCandleCount(this CandleSeries series)
		{
			return series.GetContainer().GetCandleCount(series);
		}

		/// <summary>
		/// Получить все свечи на период <paramref name="time"/>.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <param name="time">Период свечи.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this CandleSeries series, DateTime time) 
			where TCandle : Candle
		{
			return series.GetContainer().GetCandles(series, time).OfType<TCandle>();
		}

		/// <summary>
		/// Получить все свечи.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this CandleSeries series)
			where TCandle : Candle
		{
			return series.GetContainer().GetCandles(series).OfType<TCandle>();
		}

		/// <summary>
		/// Получить свечи по диапазону дат.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <param name="timeRange">Диапазон дат, в которые должны входить свечи. Учитывается значение <see cref="Candle.OpenTime"/>.</param>
		/// <returns>Найденные свечи.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this CandleSeries series, Range<DateTimeOffset> timeRange)
			where TCandle : Candle
		{
			return series.GetContainer().GetCandles(series, timeRange).OfType<TCandle>();
		}

		/// <summary>
		/// Получить свечи по общему количеству.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleCount">Количество свечек, которое необходимо вернуть.</param>
		/// <returns>Найденные свечи.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this CandleSeries series, int candleCount)
		{
			return series.GetContainer().GetCandles(series, candleCount).OfType<TCandle>();
		}

		/// <summary>
		/// Получить свечу по индексу.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleIndex">Порядковый номер свечи с конца.</param>
		/// <returns>Найденная свеча. Если свечи не существует, то будет возвращено null.</returns>
		public static TCandle GetCandle<TCandle>(this CandleSeries series, int candleIndex)
			where TCandle : Candle
		{
			return (TCandle)series.GetContainer().GetCandle(series, candleIndex);
		}

		/// <summary>
		/// Получить временную свечу за определенную дату.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="time">Дата свечи.</param>
		/// <returns>Найденная свеча (null, если свеча по заданным критериям не существует).</returns>
		public static TimeFrameCandle GetTimeFrameCandle(this CandleSeries series, DateTime time)
		{
			return series.GetCandles<TimeFrameCandle>().FirstOrDefault(c => c.OpenTime == time);
		}

		/// <summary>
		/// Получить текущую свечу.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Найденная свеча. Если свеча не существует, то будет возвращено null.</returns>
		public static TCandle GetCurrentCandle<TCandle>(this CandleSeries series)
			where TCandle : Candle
		{
			return series.GetCandle<TCandle>(0);
		}

		/// <summary>
		/// Получить серию свечек по заданным параметрам.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="manager">Менеджер свечек.</param>
		/// <param name="security">Инструмент, по которому нужно фильтровать сделки для формирования свечек.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <returns>Серия свечек. Null, если такая серия не зарегистрирована.</returns>
		public static CandleSeries GetSeries<TCandle>(this ICandleManager manager, Security security, object arg)
			where TCandle : Candle
		{
			return manager.ThrowIfNull().Series.FirstOrDefault(s => s.CandleType == typeof(TCandle) && s.Security == security && s.Arg.Equals(arg));
		}

		private static CandleSeries ThrowIfNull(this CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			return series;
		}

		private static ICandleManager ThrowIfNull(this ICandleManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException("manager");

			return manager;
		}

		private sealed class CandleEnumerable<TValue> : SimpleEnumerable<Candle>, IEnumerableEx<Candle>
		{
			private sealed class CandleEnumerator : SimpleEnumerator<Candle>
			{
				private sealed class EnumeratorCandleBuilderSource : ConvertableCandleBuilderSource<TValue>
				{
					private readonly Security _security;

					public EnumeratorCandleBuilderSource(Security security)
					{
						if (security == null)
							throw new ArgumentNullException("security");

						_security = security;
					}

					public override int SpeedPriority
					{
						get { return 0; }
					}

					public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
					{
						if (series == null)
							throw new ArgumentNullException("series");

						if (series.Security != _security)
							yield break;

						yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
					}

					public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
					{
					}

					public override void Stop(CandleSeries series)
					{
						RaiseStopped(series);
					}

					public void PushNewValue(CandleSeries series, TValue value)
					{
						NewSourceValues(series, new[] { value });
					}
				}

				private readonly CandleSeries _series;
				private bool _isNewCandle;
				private readonly IEnumerator<TValue> _valuesEnumerator;
				private readonly EnumeratorCandleBuilderSource _builderSource;
				private Candle _lastCandle;
				private readonly CandleManager _candleManager;

				public CandleEnumerator(CandleSeries series, IEnumerable<TValue> values)
				{
					if (series == null)
						throw new ArgumentNullException("series");

					if (values == null)
						throw new ArgumentNullException("values");

					_series = series;
					_series.ProcessCandle += SeriesOnProcessCandle;

					_valuesEnumerator = values.GetEnumerator();

					_candleManager = new CandleManager();

					_builderSource = new EnumeratorCandleBuilderSource(series.Security);
					_candleManager.Sources.OfType<ICandleBuilder>().ForEach(b => b.Sources.Add(_builderSource));

					_candleManager.Start(series);
				}

				private void SeriesOnProcessCandle(Candle candle)
				{
					_lastCandle = candle;

					if (candle.State != CandleStates.Finished)
						return;

					Current = candle;
					_isNewCandle = true;
				}

				public override bool MoveNext()
				{
					while (!_isNewCandle)
					{
						if (!_valuesEnumerator.MoveNext())
							break;

						_builderSource.PushNewValue(_series, _valuesEnumerator.Current);
					}

					if (_isNewCandle)
					{
						_isNewCandle = false;
						return true;
					}

					if (_lastCandle != null)
					{
						Current = _lastCandle;
						_lastCandle = null;
						return true;
					}
					else
					{
						Current = null;
						return false;
					}
				}

				protected override void DisposeManaged()
				{
					Reset();
					_series.ProcessCandle -= SeriesOnProcessCandle;
					_series.Stop();
					_candleManager.Dispose();

					base.DisposeManaged();
				}
			}

			private readonly IEnumerableEx<TValue> _values;

			public CandleEnumerable(CandleSeries series, IEnumerableEx<TValue> values)
				: base(() => new CandleEnumerator(series, values))
			{
				_values = values;
			}

			int IEnumerableEx.Count
			{
				get { return _values.Count; }
			}
		}

		/// <summary>
		/// Построить свечи из коллекции тиковых сделок.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="trades">Тиковые сделки.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerableEx<Trade> trades, object arg)
			where TCandle : Candle
		{
			var firstTrade = trades.FirstOrDefault();

			if (firstTrade == null)
				return Enumerable.Empty<TCandle>();

			return trades.ToCandles(new CandleSeries(typeof(TCandle), firstTrade.Security, arg)).Cast<TCandle>();
		}

		/// <summary>
		/// Построить свечи из коллекции тиковых сделок.
		/// </summary>
		/// <param name="trades">Тиковые сделки.</param>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerableEx<Candle> ToCandles(this IEnumerableEx<Trade> trades, CandleSeries series)
		{
			return new CandleEnumerable<Trade>(series, trades);
		}

		/// <summary>
		/// Построить свечи из коллекции тиковых сделок.
		/// </summary>
		/// <param name="trades">Тиковые сделки.</param>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerableEx<CandleMessage> ToCandles(this IEnumerableEx<ExecutionMessage> trades, CandleSeries series)
		{
			return trades
				.ToEntities<ExecutionMessage, Trade>(series.Security)
				.ToCandles(series)
				.ToMessages<Candle, CandleMessage>();
		}

		/// <summary>
		/// Построить свечи из коллекции стаканов.
		/// </summary>
		/// <param name="depths">Стаканы.</param>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerableEx<Candle> ToCandles(this IEnumerableEx<MarketDepth> depths, CandleSeries series)
		{
			return new CandleEnumerable<MarketDepth>(series, depths);
		}

		/// <summary>
		/// Построить свечи из коллекции стаканов.
		/// </summary>
		/// <param name="depths">Стаканы.</param>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public static IEnumerableEx<CandleMessage> ToCandles(this IEnumerableEx<QuoteChangeMessage> depths, CandleSeries series)
		{
			return depths
				.ToEntities<QuoteChangeMessage, MarketDepth>(series.Security)
				.ToCandles(series)
				.ToMessages<Candle, CandleMessage>();
		}

		/// <summary>
		/// Построить тики из свечек.
		/// </summary>
		/// <param name="candles">Свечи.</param>
		/// <returns>Сделки.</returns>
		public static IEnumerableEx<Trade> ToTrades(this IEnumerableEx<Candle> candles)
		{
			var candle = candles.FirstOrDefault();

			if (candle == null)
				return Enumerable.Empty<Trade>().ToEx();

			return candles
				.ToMessages<Candle, CandleMessage>()
				.ToTrades(candle.Security.VolumeStep)
				.ToEntities<ExecutionMessage, Trade>(candle.Security);
		}

		/// <summary>
		/// Построить тиковые сделки из свечек.
		/// </summary>
		/// <param name="candles">Свечи.</param>
		/// <param name="volumeStep">Шаг объема.</param>
		/// <returns>Тиковые сделки.</returns>
		public static IEnumerableEx<ExecutionMessage> ToTrades(this IEnumerableEx<CandleMessage> candles, decimal volumeStep)
		{
			return new TradeEnumerable(candles, volumeStep);
		}

		/// <summary>
		/// Построить тиковые сделки из свечи.
		/// </summary>
		/// <param name="candleMsg">Свеча.</param>
		/// <param name="volumeStep">Шаг объема.</param>
		/// <param name="decimals">Количество знаком после запятой у объема.</param>
		/// <returns>Тиковые сделки.</returns>
		public static IEnumerable<ExecutionMessage> ToTrades(this CandleMessage candleMsg, decimal volumeStep, int decimals)
		{
			if (candleMsg == null)
				throw new ArgumentNullException("candleMsg");

			var vol = MathHelper.Round(candleMsg.TotalVolume / 4, volumeStep, decimals, MidpointRounding.AwayFromZero);
			var isUptrend = candleMsg.ClosePrice >= candleMsg.OpenPrice;

			ExecutionMessage o = null;
			ExecutionMessage h = null;
			ExecutionMessage l = null;
			ExecutionMessage c = null;

			if (candleMsg.OpenPrice == candleMsg.ClosePrice && 
				candleMsg.LowPrice == candleMsg.HighPrice && 
				candleMsg.OpenPrice == candleMsg.LowPrice ||
				candleMsg.TotalVolume == 1)
			{
				// все цены в свече равны или объем равен 1 - считаем ее за один тик
				o = CreateTick(candleMsg, Sides.Buy, candleMsg.OpenPrice, candleMsg.TotalVolume, candleMsg.OpenInterest);
			}
			else if (candleMsg.TotalVolume == 2)
			{
				h = CreateTick(candleMsg, Sides.Buy, candleMsg.HighPrice, 1);
				l = CreateTick(candleMsg, Sides.Sell, candleMsg.LowPrice, 1, candleMsg.OpenInterest);
			}
			else if (candleMsg.TotalVolume == 3)
			{
				o = CreateTick(candleMsg, isUptrend ? Sides.Buy : Sides.Sell, candleMsg.OpenPrice, 1);
				h = CreateTick(candleMsg, Sides.Buy, candleMsg.HighPrice, 1);
				l = CreateTick(candleMsg, Sides.Sell, candleMsg.LowPrice, 1, candleMsg.OpenInterest);
			}
			else
			{
				o = CreateTick(candleMsg, isUptrend ? Sides.Buy : Sides.Sell, candleMsg.OpenPrice, vol);
				h = CreateTick(candleMsg, Sides.Buy, candleMsg.HighPrice, vol);
				l = CreateTick(candleMsg, Sides.Sell, candleMsg.LowPrice, vol);
				c = CreateTick(candleMsg, isUptrend ? Sides.Buy : Sides.Sell, candleMsg.ClosePrice, candleMsg.TotalVolume - 3 * vol, candleMsg.OpenInterest);
			}

			var ticks = candleMsg.ClosePrice > candleMsg.OpenPrice
					? new[] { o, l, h, c }
					: new[] { o, h, l, c };

			return ticks.Where(t => t != null);
		}

		private static ExecutionMessage CreateTick(CandleMessage candleMsg, Sides side, decimal price, decimal volume, decimal? openInterest = null)
		{
			return new ExecutionMessage
			{
				LocalTime = candleMsg.LocalTime,
				SecurityId = candleMsg.SecurityId,
				ServerTime = candleMsg.OpenTime,
				//TradeId = _tradeIdGenerator.Next,
				TradePrice = price,
				Volume = volume,
				Side = side,
				ExecutionType = ExecutionTypes.Tick,
				OpenInterest = openInterest
			};
		}
		
		private sealed class TradeEnumerable : SimpleEnumerable<ExecutionMessage>, IEnumerableEx<ExecutionMessage>
		{
			private sealed class TradeEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly decimal _volumeStep;
				private readonly IEnumerator<CandleMessage> _valuesEnumerator;
				private IEnumerator<ExecutionMessage> _currCandleEnumerator;
				private readonly int _decimals;

				public TradeEnumerator(IEnumerable<CandleMessage> candles, decimal volumeStep)
				{
					_volumeStep = volumeStep;
					_decimals = volumeStep.GetCachedDecimals();
					_valuesEnumerator = candles.GetEnumerator();
					_valuesEnumerator.MoveNext();
				}

				private IEnumerator<ExecutionMessage> CreateEnumerator(CandleMessage candleMsg)
				{
					return candleMsg.ToTrades(_volumeStep, _decimals).GetEnumerator();
				}

				public bool MoveNext()
				{
					if (_currCandleEnumerator == null)
					{
						if (_valuesEnumerator.MoveNext())
						{
							_currCandleEnumerator = CreateEnumerator(_valuesEnumerator.Current);
						}
						else
						{
							Current = null;
							return false;
						}
					}

					if (_currCandleEnumerator.MoveNext())
					{
						Current = _currCandleEnumerator.Current;
						return true;
					}

					if (_valuesEnumerator.MoveNext())
					{
						_currCandleEnumerator = CreateEnumerator(_valuesEnumerator.Current);

						_currCandleEnumerator.MoveNext();
						Current = _currCandleEnumerator.Current;

						return true;
					}
					
					Current = null;
					return false;
				}

				public void Reset()
				{
					_valuesEnumerator.Reset();
					Current = null;
				}

				public void Dispose()
				{
					Reset();
					_valuesEnumerator.Dispose();
				}

				public ExecutionMessage Current { get; private set; }

				object IEnumerator.Current
				{
					get { return Current; }
				}
			}

			public TradeEnumerable(IEnumerableEx<CandleMessage> candles, decimal volumeStep)
				: base(() => new TradeEnumerator(candles, volumeStep))
			{
				if (candles == null)
					throw new ArgumentNullException("candles");

				_values = candles;
			}

			private readonly IEnumerableEx<CandleMessage> _values;

			public int Count
			{
				get { return _values.Count * 4; }
			}
		}

		/// <summary>
		/// Зарегистрирована ли группировка свечек по определённому признаку.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечек.</typeparam>
		/// <param name="manager">Менеджер свечек.</param>
		/// <param name="security">Инструмент, для которого зарегистрирована группировка.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <returns><see langword="true"/>, если зарегистрирована. Иначе, <see langword="false"/>.</returns>
		public static bool IsCandlesRegistered<TCandle>(this ICandleManager manager, Security security, object arg)
			where TCandle : Candle
		{
			return manager.GetSeries<TCandle>(security, arg) != null;
		}

		/// <summary>
		/// Получить время формирования свечи.
		/// </summary>
		/// <param name="timeFrame">Тайм-фрейм, по которому необходимо получить время формирования свечи.</param>
		/// <param name="currentTime">Текущее время, входящее в диапазон временных рамок.</param>
		/// <returns>Время формирования свечи.</returns>
		public static DateTime GetCandleTime(this TimeSpan timeFrame, DateTime currentTime)
		{
			return timeFrame.GetCandleBounds(currentTime).Min;
		}

		/// <summary>
		/// Получить временные рамки свечи.
		/// </summary>
		/// <param name="timeFrame">Тайм-фрейм, по которому необходимо получить временные рамки.</param>
		/// <param name="currentTime">Текущее время, входящее в диапазон временных рамок.</param>
		/// <returns>Временные рамки свечи.</returns>
		public static Range<DateTime> GetCandleBounds(this TimeSpan timeFrame, DateTime currentTime)
		{
			return timeFrame.GetCandleBounds(currentTime, ExchangeBoard.Associated);
		}

		/// <summary>
		/// Получить временные рамки свечи относительно времени работы биржи.
		/// </summary>
		/// <param name="timeFrame">Тайм-фрейм, по которому необходимо получить временные рамки.</param>
		/// <param name="currentTime">Текущее время, входящее в диапазон временных рамок.</param>
		/// <param name="board">Информация о площадке, из которой будет взято время работы <see cref="ExchangeBoard.WorkingTime"/>.</param>
		/// <returns>Временные рамки свечи.</returns>
		public static Range<DateTime> GetCandleBounds(this TimeSpan timeFrame, DateTime currentTime, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			return timeFrame.GetCandleBounds(currentTime, board.WorkingTime);
		}

		private static readonly long _weekTf = TimeSpan.FromDays(7).Ticks;

		/// <summary>
		/// Получить временные рамки свечи относительно режиме работы биржи.
		/// </summary>
		/// <param name="timeFrame">Тайм-фрейм, по которому необходимо получить временные рамки.</param>
		/// <param name="currentTime">Текущее время, входящее в диапазон временных рамок.</param>
		/// <param name="time">Информация о режиме работы биржи.</param>
		/// <returns>Временные рамки свечи.</returns>
		public static Range<DateTime> GetCandleBounds(this TimeSpan timeFrame, DateTime currentTime, WorkingTime time)
		{
			if (time == null)
				throw new ArgumentNullException("time");

			if (timeFrame.Ticks == _weekTf)
			{
				var monday = currentTime.StartOfWeek(DayOfWeek.Monday);

				var endDay = currentTime.Date;

				while (endDay.DayOfWeek != DayOfWeek.Sunday)
				{
					var nextDay = endDay.AddDays(1);

					if (nextDay.Month != endDay.Month)
						break;

					endDay = nextDay;
				}

				return new Range<DateTime>(monday, endDay.EndOfDay());
			}
			else if (timeFrame.Ticks == TimeHelper.TicksPerMonth)
			{
				var month = new DateTime(currentTime.Year, currentTime.Month, 1);
				return new Range<DateTime>(month, (month + TimeSpan.FromDays(month.DaysInMonth())).EndOfDay());
			}

			var period = time.GetPeriod(currentTime);

			// http://stocksharp.com/forum/yaf_postsm13887_RealtimeEmulationTrader---niepravil-nyie-sviechi.aspx#post13887
			// отсчет свечек идет от начала сессии и игнорируются клиринги
			var startTime = period != null && period.Times.Length > 0 ? period.Times[0].Min : TimeSpan.Zero;

			var length = (currentTime.TimeOfDay - startTime).To<long>();
			var beginTime = currentTime.Date + (startTime + length.Floor(timeFrame.Ticks).To<TimeSpan>());

			//последняя свеча должна заканчиваться в конец торговой сессии
			var tempEndTime = beginTime.TimeOfDay + timeFrame;
			TimeSpan stopTime;

			if (period != null && period.Times.Length > 0)
			{
				var last = period.Times.LastOrDefault(t => tempEndTime > t.Min);
				stopTime = last == null ? TimeSpan.MaxValue : last.Max;
			}
			else
				stopTime = TimeSpan.MaxValue;

			var endTime = beginTime + timeFrame.Min(stopTime - beginTime.TimeOfDay);

			// если currentTime попало на клиринг
			if (endTime < beginTime)
				endTime = beginTime.Date + tempEndTime;

			var days = timeFrame.Days > 1 ? timeFrame.Days - 1 : 0;

			var min = beginTime.Truncate(TimeSpan.TicksPerMillisecond);
			var max = endTime.Truncate(TimeSpan.TicksPerMillisecond).AddDays(days);

			return new Range<DateTime>(min, max);
		}

		/// <summary>
		/// Получить длину свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо получить длины.</param>
		/// <returns>Длина свечи.</returns>
		public static decimal GetLength(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return candle.HighPrice - candle.LowPrice;
		}

		/// <summary>
		/// Получить тело свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо получить тело.</param>
		/// <returns>Тело свечи.</returns>
		public static decimal GetBody(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return (candle.OpenPrice - candle.ClosePrice).Abs();
		}

		/// <summary>
		/// Получить длину верхней тени свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо получить длины верхней тени.</param>
		/// <returns>Длина верхней тени свечи. Если 0, то тень отсутствует.</returns>
		public static decimal GetTopShadow(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return candle.HighPrice - candle.OpenPrice.Max(candle.ClosePrice);
		}

		/// <summary>
		/// Получить длину нижней тени свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо получить длины нижней тени.</param>
		/// <returns>Длина нижней тени свечи. Если 0, то тень отсутствует.</returns>
		public static decimal GetBottomShadow(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return candle.OpenPrice.Min(candle.ClosePrice) - candle.LowPrice;
		}

		//
		// http://en.wikipedia.org/wiki/Candlestick_chart
		//

		/// <summary>
		/// Белая ли или черная свеча.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо определить цвет.</param>
		/// <returns><see langword="true"/>, если свеча белая, <see langword="false"/>, если черная, и null, если свеча плоская.</returns>
		public static bool? IsWhiteOrBlack(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			if (candle.OpenPrice == candle.ClosePrice)
				return null;

			return candle.OpenPrice < candle.ClosePrice;
		}

		/// <summary>
		/// Бестеневая ли свеча тени.
		/// </summary>
		/// <param name="candle">Свеча, для которой необходимо определить наличие теней.</param>
		/// <returns><see langword="true"/>, если свеча не имеет теней, <see langword="false"/>, если имеет.</returns>
		public static bool IsMarubozu(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return candle.GetLength() == candle.GetBody();
		}

		/// <summary>
		/// Нейтральная ли свеча сделкам.
		/// </summary>
		/// <remarks>
		/// Нейтральность определяется как ситуация, когда в период свечи ни покупатели ни продавцы не создали тренд.
		/// </remarks>
		/// <param name="candle">Свеча, для которой необходимо рассчитать, нейтральна ли она.</param>
		/// <returns><see langword="true"/>, если свеча нейтральна, <see langword="false"/>, если не нейтральная.</returns>
		public static bool IsSpinningTop(this Candle candle)
		{
			return !candle.IsMarubozu() && (candle.GetBottomShadow() == candle.GetTopShadow());
		}

		/// <summary>
		/// Является ли свеча молотом.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо проверить на паттерн.</param>
		/// <returns><see langword="true"/>, если является, <see langword="false"/>, если нет.</returns>
		public static bool IsHammer(this Candle candle)
		{
			return !candle.IsMarubozu() && (candle.GetBottomShadow() == 0 || candle.GetTopShadow() == 0);
		}

		/// <summary>
		/// Является ли свеча стрекозой или надгробьем.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо проверить на паттерн.</param>
		/// <returns><see langword="true"/>, если стрекоза, <see langword="false"/>, если надгробье, <see langword="null"/> - ни то, ни другое.</returns>
		public static bool? IsDragonflyOrGravestone(this Candle candle)
		{
			if (candle.IsWhiteOrBlack() == null)
			{
				if (candle.GetTopShadow() == 0)
					return true;
				else if (candle.GetBottomShadow() == 0)
					return false;
			}

			return null;
		}

		/// <summary>
		/// Бычья ли или медвежья свеча.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо проверить на тренд.</param>
		/// <returns><see langword="true"/>, если бычья, <see langword="false"/>, если медвежья, <see langword="null"/> - ни то, ни другое.</returns>
		public static bool? IsBullishOrBearish(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			var isWhiteOrBlack = candle.IsWhiteOrBlack();

			switch (isWhiteOrBlack)
			{
				case true:
					if (candle.GetBottomShadow() >= candle.GetBody())
						return true;
					break;
				case false:
					if (candle.GetTopShadow() >= candle.GetBody())
						return true;
					break;
			}

			return null;
		}

		/// <summary>
		/// Получить количество временных интервалов в пределах заданного отрезка времени.
		/// </summary>
		/// <param name="security">Инструмент, по которому вычисляется время работы биржи через свойство <see cref="Security.Board"/>.</param>
		/// <param name="range">Заданный отрезок времени, для которого нужно получить количество временных интервалов.</param>
		/// <param name="timeFrame">Размер временного интервала.</param>
		/// <returns>Полученное количество временных интервалов.</returns>
		public static long GetTimeFrameCount(this Security security, Range<DateTimeOffset> range, TimeSpan timeFrame)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return security.Board.GetTimeFrameCount(range, timeFrame);
		}

		/// <summary>
		/// Получить количество временных интервалов в пределах заданного отрезка времени.
		/// </summary>
		/// <param name="board">Информация о площадке, по которому вычисляется время работы через свойство <see cref="ExchangeBoard.WorkingTime"/>.</param>
		/// <param name="range">Заданный отрезок времени, для которого нужно получить количество временных интервалов.</param>
		/// <param name="timeFrame">Размер временного интервала.</param>
		/// <returns>Полученное количество временных интервалов.</returns>
		public static long GetTimeFrameCount(this ExchangeBoard board, Range<DateTimeOffset> range, TimeSpan timeFrame)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			if (range == null)
				throw new ArgumentNullException("range");

			var workingTime = board.WorkingTime;

			var to = board.Exchange.ToExchangeTime(range.Max);
			var from = board.Exchange.ToExchangeTime(range.Min);

			var days = (int)(to.Date - from.Date).TotalDays;

			var period = workingTime.GetPeriod(from);

			if (period == null || period.Times.IsEmpty())
			{
				return (to - from).Ticks / timeFrame.Ticks;
			}

			if (days == 0)
			{
				return workingTime.GetTimeFrameCount(from, new Range<TimeSpan>(from.TimeOfDay, to.TimeOfDay), timeFrame);
			}

			var totalCount = workingTime.GetTimeFrameCount(from, new Range<TimeSpan>(from.TimeOfDay, TimeHelper.LessOneDay), timeFrame);
			totalCount += workingTime.GetTimeFrameCount(to, new Range<TimeSpan>(TimeSpan.Zero, to.TimeOfDay), timeFrame);

			if (days <= 1)
				return totalCount;

			var fullDayLength = period.Times.Sum(r => r.Length.Ticks);
			totalCount += TimeSpan.FromTicks((days - 1) * fullDayLength).Ticks / timeFrame.Ticks;

			return totalCount;
		}

		private static long GetTimeFrameCount(this WorkingTime workingTime, DateTime date, Range<TimeSpan> fromToRange, TimeSpan timeFrame)
		{
			if (workingTime == null)
				throw new ArgumentNullException("workingTime");

			if (fromToRange == null)
				throw new ArgumentNullException("fromToRange");

			var period = workingTime.GetPeriod(date);

			if (period == null)
				return 0;

			return period.Times
						.Select(fromToRange.Intersect)
						.Where(intersection => intersection != null)
						.Sum(intersection => intersection.Length.Ticks / timeFrame.Ticks);
		}

		internal static CandleSeries CheckSeries(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			var series = candle.Series;

			if (series == null)
				throw new ArgumentException("candle");

			return series;
		}

		internal static bool CheckTime(this CandleSeries series, DateTimeOffset time)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			return series.Security.Board.IsTradeTime(time) && time >= series.From && time < series.To;
		}

		/// <summary>
		/// Рассчитать <see cref="ValueArea"/> для группы свечек.
		/// </summary>
		/// <param name="candles">Свечи.</param>
		/// <returns><see cref="ValueArea"/>.</returns>
		public static ValueArea GetValueArea(this IEnumerable<Candle> candles)
		{
			var va = new ValueArea(candles.SelectMany(c => c.VolumeProfileInfo.PriceLevels));
			va.Calculate();
			return va;
		}

		/// <summary>
		/// Запустить таймер получения из переданного <paramref name="connector"/> свечек реального времени.
		/// </summary>
		/// <typeparam name="TConnector">Тип подключения, реализующего <see cref="IExternalCandleSource"/>.</typeparam>
		/// <param name="connector">Подключение, реализующее <see cref="IExternalCandleSource"/>.</param>
		/// <param name="registeredSeries">Все зарегистрированные серии свечек.</param>
		/// <param name="offset">Временной отступ для нового запроса получение новой свечи. Необходим для того, чтобы сервер успел сформировать данные в своем хранилище свечек.</param>
		/// <param name="requestNewCandles">Обработчик, получающий новые свечи.</param>
		/// <param name="interval">Периодичность обновления данных.</param>
		/// <returns>Созданный таймер.</returns>
		public static Timer StartRealTime<TConnector>(this TConnector connector, CachedSynchronizedSet<CandleSeries> registeredSeries, TimeSpan offset, Action<CandleSeries, Range<DateTimeOffset>> requestNewCandles, TimeSpan interval)
			where TConnector : class, IConnector, IExternalCandleSource
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (registeredSeries == null)
				throw new ArgumentNullException("registeredSeries");

			if (requestNewCandles == null)
				throw new ArgumentNullException("requestNewCandles");

			return ThreadingHelper.Timer(() =>
			{
				if (connector.ConnectionState != ConnectionStates.Connected || connector.ExportState != ConnectionStates.Connected)
					return;

				lock (registeredSeries.SyncRoot)
				{
					foreach (var series in registeredSeries.Cache)
					{
						var tf = (TimeSpan)series.Arg;
						var time = connector.CurrentTime.LocalDateTime;
            			var bounds = tf.GetCandleBounds(time, series.Security.Board);

						var beginTime = time - bounds.Min < offset ? bounds.Min - tf : bounds.Min;
						var finishTime = bounds.Max;

						requestNewCandles(series, new Range<DateTimeOffset>(beginTime, finishTime));
					}
				}
			})
			.Interval(interval);
		}
	}
}