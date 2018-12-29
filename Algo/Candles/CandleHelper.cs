#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Extension class for candles.
	/// </summary>
	public static class CandleHelper
	{
		/// <summary>
		/// Possible data types that can be used as candles source.
		/// </summary>
		public static IEnumerable<MarketDataTypes> CandleDataSources { get; } = new[] { MarketDataTypes.Level1, MarketDataTypes.Trades, MarketDataTypes.MarketDepth, MarketDataTypes.OrderLog };

		/// <summary>
		/// Determines whether the specified type is derived from <see cref="Candle"/>.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <returns><see langword="true"/> if the specified type is derived from <see cref="Candle"/>, otherwise, <see langword="false"/>.</returns>
		public static bool IsCandle(this Type candleType)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			return candleType.IsSubclassOf(typeof(Candle));
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="TimeFrameCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries TimeFrame(this Security security, TimeSpan arg)
		{
			return new CandleSeries(typeof(TimeFrameCandle), security, arg);
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="RangeCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="RangeCandle.PriceRange"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries Range(this Security security, Unit arg)
		{
			return new CandleSeries(typeof(RangeCandle), security, arg);
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="VolumeCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="VolumeCandle.Volume"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries Volume(this Security security, decimal arg)
		{
			return new CandleSeries(typeof(VolumeCandle), security, arg);
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="TickCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="TickCandle.MaxTradeCount"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries Tick(this Security security, decimal arg)
		{
			return new CandleSeries(typeof(TickCandle), security, arg);
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="PnFCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="PnFCandle.PnFArg"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries PnF(this Security security, PnFArg arg)
		{
			return new CandleSeries(typeof(PnFCandle), security, arg);
		}

		/// <summary>
		/// To create <see cref="CandleSeries"/> for <see cref="RenkoCandle"/> candles.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The value of <see cref="RenkoCandle.BoxSize"/>.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries Renko(this Security security, Unit arg)
		{
			return new CandleSeries(typeof(RenkoCandle), security, arg);
		}

		/// <summary>
		/// To start candles getting.
		/// </summary>
		/// <param name="manager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		public static void Start(this ICandleManager manager, CandleSeries series)
		{
			manager.ThrowIfNull().Start(series, series.From, series.To);
		}

		///// <summary>
		///// To stop candles getting.
		///// </summary>
		///// <param name="series">Candles series.</param>
		//public static void Stop(this CandleSeries series)
		//{
		//	var manager = series.ThrowIfNull().CandleManager;

		//	// серию ранее не запускали, значит и останавливать не нужно
		//	if (manager == null)
		//		return;

		//	manager.Stop(series);
		//}

		//private static ICandleManagerContainer GetContainer(this CandleSeries series)
		//{
		//	return series.ThrowIfNull().CandleManager.Container;
		//}

		/// <summary>
		/// To get the number of candles.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <returns>Number of candles.</returns>
		public static int GetCandleCount(this ICandleManager candleManager, CandleSeries series)
		{
			return candleManager.ThrowIfNull().Container.GetCandleCount(series);
		}

		/// <summary>
		/// To get all candles for the <paramref name="time" /> period.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="time">The candle period.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this ICandleManager candleManager, CandleSeries series, DateTimeOffset time) 
			where TCandle : Candle
		{
			return candleManager.ThrowIfNull().Container.GetCandles(series, time).OfType<TCandle>();
		}

		/// <summary>
		/// To get all candles.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this ICandleManager candleManager, CandleSeries series)
			where TCandle : Candle
		{
			return candleManager.ThrowIfNull().Container.GetCandles(series).OfType<TCandle>();
		}

		/// <summary>
		/// To get candles by date range.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="timeRange">The date range which should include candles. The <see cref="Candle.OpenTime"/> value is taken into consideration.</param>
		/// <returns>Found candles.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this ICandleManager candleManager, CandleSeries series, Range<DateTimeOffset> timeRange)
			where TCandle : Candle
		{
			return candleManager.ThrowIfNull().Container.GetCandles(series, timeRange).OfType<TCandle>();
		}

		/// <summary>
		/// To get candles by the total number.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="candleCount">The number of candles that should be returned.</param>
		/// <returns>Found candles.</returns>
		public static IEnumerable<TCandle> GetCandles<TCandle>(this ICandleManager candleManager, CandleSeries series, int candleCount)
		{
			return candleManager.ThrowIfNull().Container.GetCandles(series, candleCount).OfType<TCandle>();
		}

		/// <summary>
		/// To get a candle by the index.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="candleIndex">The candle's position number from the end.</param>
		/// <returns>The found candle. If the candle does not exist, then <see langword="null" /> will be returned.</returns>
		public static TCandle GetCandle<TCandle>(this ICandleManager candleManager, CandleSeries series, int candleIndex)
			where TCandle : Candle
		{
			return (TCandle)candleManager.ThrowIfNull().Container.GetCandle(series, candleIndex);
		}

		/// <summary>
		/// To get a temporary candle on the specific date.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="time">The candle date.</param>
		/// <returns>The found candle (<see langword="null" />, if the candle by the specified criteria does not exist).</returns>
		public static TimeFrameCandle GetTimeFrameCandle(this ICandleManager candleManager, CandleSeries series, DateTimeOffset time)
		{
			return candleManager.GetCandles<TimeFrameCandle>(series).FirstOrDefault(c => c.OpenTime == time);
		}

		/// <summary>
		/// To get the current candle.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series.</param>
		/// <returns>The found candle. If the candle does not exist, the <see langword="null" /> will be returned.</returns>
		public static TCandle GetCurrentCandle<TCandle>(this ICandleManager candleManager, CandleSeries series)
			where TCandle : Candle
		{
			return candleManager.GetCandle<TCandle>(series, 0);
		}

		/// <summary>
		/// To get a candles series by the specified parameters.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="security">The instrument by which trades should be filtered for the candles creation.</param>
		/// <param name="arg">Candle arg.</param>
		/// <returns>The candles series. <see langword="null" /> if this series is not registered.</returns>
		public static CandleSeries GetSeries<TCandle>(this ICandleManager candleManager, Security security, object arg)
			where TCandle : Candle
		{
			return candleManager.ThrowIfNull().Series.FirstOrDefault(s => s.CandleType == typeof(TCandle) && s.Security == security && s.Arg.Equals(arg));
		}

		private static ICandleManager ThrowIfNull(this ICandleManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			return manager;
		}

		private sealed class CandleMessageEnumerable : SimpleEnumerable<CandleMessage>
		{
			private sealed class CandleMessageEnumerator : SimpleEnumerator<CandleMessage>
			{
				private IEnumerator<Message> _messagesEnumerator;
				private readonly List<CandleMessage> _finishedCandles = new List<CandleMessage>();
				private readonly ICandleBuilder _candleBuilder;
				private readonly MarketDataMessage _mdMsg;
				private readonly bool _onlyFormed;
				private readonly IEnumerable<Message> _messages;

				private ICandleBuilderValueTransform _transform;

				private CandleMessage _lastActiveCandle;
				private CandleMessage _lastCandle;

				public CandleMessageEnumerator(MarketDataMessage mdMsg, bool onlyFormed, IEnumerable<Message> messages, ICandleBuilderValueTransform transform, ICandleBuilder candleBuilder)
				{
					_mdMsg = mdMsg ?? throw new ArgumentNullException(nameof(mdMsg));
					_onlyFormed = onlyFormed;
					_messages = messages ?? throw new ArgumentNullException(nameof(messages));
					_transform = transform;

					_messagesEnumerator = _messages.GetEnumerator();
					_candleBuilder = candleBuilder ?? throw new ArgumentNullException(nameof(candleBuilder));
				}

				public override void Reset()
				{
					base.Reset();

					_finishedCandles.Clear();
					_messagesEnumerator = _messages.GetEnumerator();
					_lastActiveCandle = null;
					_lastCandle = null;
					//_candleBuilder.Reset();
				}

				protected override void DisposeManaged()
				{
					_finishedCandles.Clear();
					_lastActiveCandle = null;
					_lastCandle = null;

					_messagesEnumerator.Dispose();
					_candleBuilder.Dispose();

					base.DisposeManaged();
				}

				public override bool MoveNext()
				{
					while (_finishedCandles.Count == 0)
					{
						if (!_messagesEnumerator.MoveNext())
							break;

						var sourceMsg = _messagesEnumerator.Current;

						if (_transform == null)
						{
							switch (sourceMsg.Type)
							{
								case MessageTypes.QuoteChange:
									_transform = new QuoteCandleBuilderValueTransform();
									break;

								case MessageTypes.Level1Change:
									_transform = new Level1CandleBuilderValueTransform();
									break;

								case MessageTypes.Execution:
								{
									var execMsg = (ExecutionMessage)sourceMsg;

									switch (execMsg.ExecutionType)
									{
										case ExecutionTypes.Tick:
											_transform = new TickCandleBuilderValueTransform();
											break;
										case ExecutionTypes.OrderLog:
											_transform = new OrderLogCandleBuilderValueTransform();
											break;
										default:
											throw new ArgumentOutOfRangeException(nameof(execMsg.ExecutionType), execMsg.ExecutionType, LocalizedStrings.Str1219);
									}

									break;
								}
								default:
									throw new ArgumentOutOfRangeException(nameof(sourceMsg.Type), sourceMsg.Type, LocalizedStrings.Str1219);
							}
						}

						if (!_transform.Process(sourceMsg))
							continue;

						_lastActiveCandle = null;

						foreach (var candleMessage in _candleBuilder.Process(_mdMsg, _lastCandle, _transform))
						{
							_lastCandle = candleMessage;

							if (candleMessage.State == CandleStates.Finished)
								_finishedCandles.Add(candleMessage);

							if (!_onlyFormed)
							{
								if (candleMessage.State != CandleStates.Finished)
									_lastActiveCandle = candleMessage;
							}
						}
					}

					if (_finishedCandles.Count > 0)
					{
						Current = _finishedCandles[0];
						_finishedCandles.RemoveAt(0);

						return true;
					}

					if (_lastActiveCandle != null)
					{
						Current = _lastActiveCandle;
						_lastActiveCandle = null;

						return true;
					}

					Current = null;
					return false;
				}
			}

			public CandleMessageEnumerable(MarketDataMessage mdMsg, bool onlyFormed, IEnumerable<ExecutionMessage> executions, CandleBuilderProvider candleBuilderProvider)
				: base(() => new CandleMessageEnumerator(mdMsg, onlyFormed, executions, null, CreateBuilder(mdMsg, candleBuilderProvider)))
			{
				if (mdMsg == null)
					throw new ArgumentNullException(nameof(mdMsg));

				if (executions == null)
					throw new ArgumentNullException(nameof(executions));
			}

			public CandleMessageEnumerable(MarketDataMessage mdMsg, bool onlyFormed, IEnumerable<QuoteChangeMessage> depths, Level1Fields type, CandleBuilderProvider candleBuilderProvider)
				: base(() => new CandleMessageEnumerator(mdMsg, onlyFormed, depths, new QuoteCandleBuilderValueTransform { Type = type }, CreateBuilder(mdMsg, candleBuilderProvider)))
			{
				if (mdMsg == null)
					throw new ArgumentNullException(nameof(mdMsg));

				if (depths == null)
					throw new ArgumentNullException(nameof(depths));
			}

			private static ICandleBuilder CreateBuilder(MarketDataMessage mdMsg, CandleBuilderProvider candleBuilderProvider)
			{
				if (mdMsg == null)
					throw new ArgumentNullException(nameof(mdMsg));

				if (candleBuilderProvider == null)
					candleBuilderProvider = ConfigManager.TryGetService<CandleBuilderProvider>() ?? new CandleBuilderProvider(ServicesRegistry.EnsureGetExchangeInfoProvider());

				return candleBuilderProvider.Get(mdMsg.DataType);
			}
		}

		/// <summary>
		/// To create candles from the tick trades collection.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="trades">Tick trades.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerable<Trade> trades, object arg, bool onlyFormed = true)
			where TCandle : Candle
		{
			var firstTrade = trades.FirstOrDefault();

			if (firstTrade == null)
				return Enumerable.Empty<TCandle>();

			return trades.ToCandles(new CandleSeries(typeof(TCandle), firstTrade.Security, arg), onlyFormed).Cast<TCandle>();
		}

		/// <summary>
		/// To create candles from the tick trades collection.
		/// </summary>
		/// <param name="trades">Tick trades.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<Candle> ToCandles(this IEnumerable<Trade> trades, CandleSeries series, bool onlyFormed = true)
		{
			return trades
				.ToMessages<Trade, ExecutionMessage>()
				.ToCandles(series, onlyFormed)
				.ToCandles<Candle>(series.Security, series.CandleType);
		}

		/// <summary>
		/// To create candles from the tick trades collection.
		/// </summary>
		/// <param name="trades">Tick trades.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<ExecutionMessage> trades, CandleSeries series, bool onlyFormed = true, CandleBuilderProvider candleBuilderProvider = null)
		{
			return trades.ToCandles(series.ToMarketDataMessage(true), onlyFormed, candleBuilderProvider);
		}

		/// <summary>
		/// To create candles from the tick trades collection.
		/// </summary>
		/// <param name="executions">Tick data.</param>
		/// <param name="mdMsg">Market data subscription.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<ExecutionMessage> executions, MarketDataMessage mdMsg, bool onlyFormed = true, CandleBuilderProvider candleBuilderProvider = null)
		{
			return new CandleMessageEnumerable(mdMsg, onlyFormed, executions, candleBuilderProvider);
		}

		/// <summary>
		/// To create candles from the order books collection.
		/// </summary>
		/// <param name="depths">Market depths.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="type">Type of candle depth based data.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<Candle> ToCandles(this IEnumerable<MarketDepth> depths, CandleSeries series, Level1Fields type = Level1Fields.SpreadMiddle, bool onlyFormed = true, CandleBuilderProvider candleBuilderProvider = null)
		{
			return depths
				.ToMessages<MarketDepth, QuoteChangeMessage>()
				.ToCandles(series, type, onlyFormed, candleBuilderProvider)
				.ToCandles<Candle>(series.Security, series.CandleType);
		}

		/// <summary>
		/// To create candles from the order books collection.
		/// </summary>
		/// <param name="depths">Market depths.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="type">Type of candle depth based data.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<QuoteChangeMessage> depths, CandleSeries series, Level1Fields type = Level1Fields.SpreadMiddle, bool onlyFormed = true, CandleBuilderProvider candleBuilderProvider = null)
		{
			return depths.ToCandles(series.ToMarketDataMessage(true), type, onlyFormed, candleBuilderProvider);
		}

		/// <summary>
		/// To create candles from the order books collection.
		/// </summary>
		/// <param name="depths">Market depths.</param>
		/// <param name="mdMsg">Market data subscription.</param>
		/// <param name="type">Type of candle depth based data.</param>
		/// <param name="onlyFormed">Process only formed candles.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <returns>Candles.</returns>
		public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<QuoteChangeMessage> depths, MarketDataMessage mdMsg, Level1Fields type = Level1Fields.SpreadMiddle, bool onlyFormed = true, CandleBuilderProvider candleBuilderProvider = null)
		{
			return new CandleMessageEnumerable(mdMsg, onlyFormed, depths, type, candleBuilderProvider);
		}

		/// <summary>
		/// To create ticks from candles.
		/// </summary>
		/// <param name="candles">Candles.</param>
		/// <returns>Trades.</returns>
		public static IEnumerable<Trade> ToTrades(this IEnumerable<Candle> candles)
		{
			var candle = candles.FirstOrDefault();

			if (candle == null)
				return Enumerable.Empty<Trade>();

			return candles
				.ToMessages<Candle, CandleMessage>()
				.ToTrades(candle.Security.VolumeStep ?? 1m)
				.ToEntities<ExecutionMessage, Trade>(candle.Security);
		}

		/// <summary>
		/// To create tick trades from candles.
		/// </summary>
		/// <param name="candles">Candles.</param>
		/// <param name="volumeStep">Volume step.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerable<ExecutionMessage> ToTrades(this IEnumerable<CandleMessage> candles, decimal volumeStep)
		{
			return new TradeEnumerable(candles, volumeStep);
		}

		/// <summary>
		/// To create tick trades from candle.
		/// </summary>
		/// <param name="candleMsg">Candle.</param>
		/// <param name="volumeStep">Volume step.</param>
		/// <param name="decimals">The number of decimal places for the volume.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerable<ExecutionMessage> ToTrades(this CandleMessage candleMsg, decimal volumeStep, int decimals)
		{
			if (candleMsg == null)
				throw new ArgumentNullException(nameof(candleMsg));

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
				TradeVolume = volume,
				Side = side,
				ExecutionType = ExecutionTypes.Tick,
				OpenInterest = openInterest
			};
		}
		
		private sealed class TradeEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
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
					Current = null;
					_valuesEnumerator.Dispose();
				}

				public ExecutionMessage Current { get; private set; }

				object IEnumerator.Current => Current;
			}

			public TradeEnumerable(IEnumerable<CandleMessage> candles, decimal volumeStep)
				: base(() => new TradeEnumerator(candles, volumeStep))
			{
				if (candles == null)
					throw new ArgumentNullException(nameof(candles));

				//_values = candles;
			}

			//private readonly IEnumerableEx<CandleMessage> _values;

			//public int Count => _values.Count * 4;
		}

		/// <summary>
		/// Whether the grouping of candles by the specified attribute is registered.
		/// </summary>
		/// <typeparam name="TCandle">Candles type.</typeparam>
		/// <param name="manager">The candles manager.</param>
		/// <param name="security">The instrument for which the grouping is registered.</param>
		/// <param name="arg">Candle arg.</param>
		/// <returns><see langword="true" /> if registered. Otherwise, <see langword="false" />.</returns>
		public static bool IsCandlesRegistered<TCandle>(this ICandleManager manager, Security security, object arg)
			where TCandle : Candle
		{
			return manager.GetSeries<TCandle>(security, arg) != null;
		}

		/// <summary>
		/// To get the candle time range.
		/// </summary>
		/// <param name="timeFrame">The time frame for which you need to get time range.</param>
		/// <param name="currentTime">The current time within the range of time frames.</param>
		/// <returns>The candle time frames.</returns>
		public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime)
		{
			return timeFrame.GetCandleBounds(currentTime, ExchangeBoard.Associated);
		}

		/// <summary>
		/// To get candle time frames relatively to the exchange working hours.
		/// </summary>
		/// <param name="timeFrame">The time frame for which you need to get time range.</param>
		/// <param name="currentTime">The current time within the range of time frames.</param>
		/// <param name="board">The information about the board from which <see cref="ExchangeBoard.WorkingTime"/> working hours will be taken.</param>
		/// <returns>The candle time frames.</returns>
		public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return timeFrame.GetCandleBounds(currentTime, board, board.WorkingTime);
		}

		private static readonly long _weekTf = TimeSpan.FromDays(7).Ticks;

		/// <summary>
		/// To get candle time frames relatively to the exchange working pattern.
		/// </summary>
		/// <param name="timeFrame">The time frame for which you need to get time range.</param>
		/// <param name="currentTime">The current time within the range of time frames.</param>
		/// <param name="board">Board info.</param>
		/// <param name="time">The information about the exchange working pattern.</param>
		/// <returns>The candle time frames.</returns>
		public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime, ExchangeBoard board, WorkingTime time)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			if (time == null)
				throw new ArgumentNullException(nameof(time));

			var exchangeTime = currentTime.ToLocalTime(board.TimeZone);
			Range<DateTime> bounds;

			if (timeFrame.Ticks == _weekTf)
			{
				var monday = exchangeTime.StartOfWeek(DayOfWeek.Monday);

				var endDay = exchangeTime.Date;

				while (endDay.DayOfWeek != DayOfWeek.Sunday)
				{
					var nextDay = endDay.AddDays(1);

					if (nextDay.Month != endDay.Month)
						break;

					endDay = nextDay;
				}

				bounds = new Range<DateTime>(monday, endDay.EndOfDay());
			}
			else if (timeFrame.Ticks == TimeHelper.TicksPerMonth)
			{
				var month = new DateTime(exchangeTime.Year, exchangeTime.Month, 1);
				bounds = new Range<DateTime>(month, (month + TimeSpan.FromDays(month.DaysInMonth())).EndOfDay());
			}
			else
			{
				var period = time.GetPeriod(exchangeTime);

				// http://stocksharp.com/forum/yaf_postsm13887_RealtimeEmulationTrader---niepravil-nyie-sviechi.aspx#post13887
				// отсчет свечек идет от начала сессии и игнорируются клиринги
				var startTime = period != null && period.Times.Count > 0 ? period.Times[0].Min : TimeSpan.Zero;

				var length = (exchangeTime.TimeOfDay - startTime).To<long>();
				var beginTime = exchangeTime.Date + (startTime + length.Floor(timeFrame.Ticks).To<TimeSpan>());

				//последняя свеча должна заканчиваться в конец торговой сессии
				var tempEndTime = beginTime.TimeOfDay + timeFrame;
				TimeSpan stopTime;

				if (period != null && period.Times.Count > 0)
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

				bounds = new Range<DateTime>(min, max);
			}

			var offset = currentTime.Offset;
			var diff = currentTime.DateTime - exchangeTime;

			return new Range<DateTimeOffset>(
				(bounds.Min + diff).ApplyTimeZone(offset),
				(bounds.Max + diff).ApplyTimeZone(offset));
		}

		/// <summary>
		/// To get the candle length.
		/// </summary>
		/// <param name="candle">The candle for which you need to get a length.</param>
		/// <returns>The candle length.</returns>
		public static decimal GetLength(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			return candle.HighPrice - candle.LowPrice;
		}

		/// <summary>
		/// To get the candle body.
		/// </summary>
		/// <param name="candle">The candle for which you need to get the body.</param>
		/// <returns>The candle body.</returns>
		public static decimal GetBody(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			return (candle.OpenPrice - candle.ClosePrice).Abs();
		}

		/// <summary>
		/// To get the candle upper shadow length.
		/// </summary>
		/// <param name="candle">The candle for which you need to get the upper shadow length.</param>
		/// <returns>The candle upper shadow length. If 0, there is no shadow.</returns>
		public static decimal GetTopShadow(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			return candle.HighPrice - candle.OpenPrice.Max(candle.ClosePrice);
		}

		/// <summary>
		/// To get the candle lower shadow length.
		/// </summary>
		/// <param name="candle">The candle for which you need to get the lower shadow length.</param>
		/// <returns>The candle lower shadow length. If 0, there is no shadow.</returns>
		public static decimal GetBottomShadow(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			return candle.OpenPrice.Min(candle.ClosePrice) - candle.LowPrice;
		}

		//
		// http://en.wikipedia.org/wiki/Candlestick_chart
		//

		/// <summary>
		/// Whether the candle is white or black.
		/// </summary>
		/// <param name="candle">The candle for which you need to get a color.</param>
		/// <returns><see langword="true" /> if the candle is white, <see langword="false" /> if the candle is black and <see langword="null" /> if the candle is plane.</returns>
		public static bool? IsWhiteOrBlack(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (candle.OpenPrice == candle.ClosePrice)
				return null;

			return candle.OpenPrice < candle.ClosePrice;
		}

		/// <summary>
		/// Whether the candle is shadowless.
		/// </summary>
		/// <param name="candle">The candle for which you need to identify the shadows presence.</param>
		/// <returns><see langword="true" /> if the candle has no shadows, <see langword="false" /> if it has shadows.</returns>
		public static bool IsMarubozu(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			return candle.GetLength() == candle.GetBody();
		}

		/// <summary>
		/// Whether the candle is neutral to trades.
		/// </summary>
		/// <param name="candle">The candle for which you need to calculate whether it is neutral.</param>
		/// <returns><see langword="true" /> if the candle is neutral, <see langword="false" /> if it is not neutral.</returns>
		/// <remarks>
		/// The neutrality is defined as a situation when during the candle neither buyers nor sellers have not created a trend.
		/// </remarks>
		public static bool IsSpinningTop(this Candle candle)
		{
			return !candle.IsMarubozu() && (candle.GetBottomShadow() == candle.GetTopShadow());
		}

		/// <summary>
		/// Whether the candle is hammer.
		/// </summary>
		/// <param name="candle">The candle which should match the pattern.</param>
		/// <returns><see langword="true" /> if it is matched, <see langword="false" /> if not.</returns>
		public static bool IsHammer(this Candle candle)
		{
			return !candle.IsMarubozu() && (candle.GetBottomShadow() == 0 || candle.GetTopShadow() == 0);
		}

		/// <summary>
		/// Whether the candle is dragonfly or tombstone.
		/// </summary>
		/// <param name="candle">The candle which should match the pattern.</param>
		/// <returns><see langword="true" /> if the dragonfly, <see langword="false" /> if the tombstone, <see langword="null" /> - neither one nor the other.</returns>
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
		/// Whether the candle is bullish or bearish.
		/// </summary>
		/// <param name="candle">The candle which should be checked for the trend.</param>
		/// <returns><see langword="true" /> if bullish, <see langword="false" />, if bearish, <see langword="null" /> - neither one nor the other.</returns>
		public static bool? IsBullishOrBearish(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

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
		/// To get the number of time frames within the specified time range.
		/// </summary>
		/// <param name="security">The instrument by which exchange working hours are calculated through the <see cref="Security.Board"/> property.</param>
		/// <param name="range">The specified time range for which you need to get the number of time frames.</param>
		/// <param name="timeFrame">The time frame size.</param>
		/// <returns>The received number of time frames.</returns>
		public static long GetTimeFrameCount(this Security security, Range<DateTimeOffset> range, TimeSpan timeFrame)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.Board.GetTimeFrameCount(range, timeFrame);
		}

		/// <summary>
		/// To get the number of time frames within the specified time range.
		/// </summary>
		/// <param name="board">The information about the board by which working hours are calculated through the <see cref="ExchangeBoard.WorkingTime"/> property.</param>
		/// <param name="range">The specified time range for which you need to get the number of time frames.</param>
		/// <param name="timeFrame">The time frame size.</param>
		/// <returns>The received number of time frames.</returns>
		public static long GetTimeFrameCount(this ExchangeBoard board, Range<DateTimeOffset> range, TimeSpan timeFrame)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			if (range == null)
				throw new ArgumentNullException(nameof(range));

			var workingTime = board.WorkingTime;

			var to = range.Max.ToLocalTime(board.TimeZone);
			var from = range.Min.ToLocalTime(board.TimeZone);

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
				throw new ArgumentNullException(nameof(workingTime));

			if (fromToRange == null)
				throw new ArgumentNullException(nameof(fromToRange));

			var period = workingTime.GetPeriod(date);

			if (period == null)
				return 0;

			return period.Times
						.Select(fromToRange.Intersect)
						.Where(intersection => intersection != null)
						.Sum(intersection => intersection.Length.Ticks / timeFrame.Ticks);
		}

		//internal static CandleSeries CheckSeries(this Candle candle)
		//{
		//	if (candle == null)
		//		throw new ArgumentNullException(nameof(candle));

		//	var series = candle.Series;

		//	if (series == null)
		//		throw new ArgumentException(nameof(candle));

		//	return series;
		//}

		internal static bool CheckTime(this CandleSeries series, DateTimeOffset time)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return time >= series.From && time < series.To && (!series.IsRegularTradingHours || series.Security.Board.IsTradeTime(time));
		}

		/// <summary>
		/// To calculate the area for the candles group.
		/// </summary>
		/// <param name="candles">Candles.</param>
		/// <returns>The area.</returns>
		public static CandleMessageVolumeProfile GetValueArea(this IEnumerable<Candle> candles)
		{
			var area = new CandleMessageVolumeProfile();

			foreach (var candle in candles)
			{
				if (candle.PriceLevels == null)
					continue;

				foreach (var priceLevel in candle.PriceLevels)
				{
					area.Update(priceLevel);
				}
			}

			area.Calculate();
			return area;
		}

		///// <summary>
		///// To start timer of getting from sent <paramref name="connector" /> of real time candles.
		///// </summary>
		///// <typeparam name="TConnector">The type of the connection implementing <see cref="IExternalCandleSource"/>.</typeparam>
		///// <param name="connector">The connection implementing <see cref="IExternalCandleSource"/>.</param>
		///// <param name="registeredSeries">All registered candles series.</param>
		///// <param name="offset">The time shift for the new request to obtain a new candle. It is needed for the server will have time to create data in its candles storage.</param>
		///// <param name="requestNewCandles">The handler getting new candles.</param>
		///// <param name="interval">The interval between data updates.</param>
		///// <returns>Created timer.</returns>
		//public static Timer StartRealTime<TConnector>(this TConnector connector, CachedSynchronizedSet<CandleSeries> registeredSeries, TimeSpan offset, Action<CandleSeries, Range<DateTimeOffset>> requestNewCandles, TimeSpan interval)
		//	where TConnector : class, IConnector//, IExternalCandleSource
		//{
		//	if (connector == null)
		//		throw new ArgumentNullException(nameof(connector));

		//	if (registeredSeries == null)
		//		throw new ArgumentNullException(nameof(registeredSeries));

		//	if (requestNewCandles == null)
		//		throw new ArgumentNullException(nameof(requestNewCandles));

		//	return ThreadingHelper.Timer(() =>
		//	{
		//		try
		//		{
		//			if (connector.ConnectionState != ConnectionStates.Connected)
		//				return;

		//			lock (registeredSeries.SyncRoot)
		//			{
		//				foreach (var series in registeredSeries.Cache)
		//				{
		//					var tf = (TimeSpan)series.Arg;
		//					var time = connector.CurrentTime;
		//					var bounds = tf.GetCandleBounds(time, series.Security.Board);

		//					var beginTime = (time - bounds.Min) < offset ? (bounds.Min - tf) : bounds.Min;
		//					var finishTime = bounds.Max;

		//					requestNewCandles(series, new Range<DateTimeOffset>(beginTime, finishTime));
		//				}
		//			}
		//		}
		//		catch (Exception ex)
		//		{
		//			ex.LogError();
		//		}
		//	})
		//	.Interval(interval);
		//}

		/// <summary>
		/// Compress candles to bigger time-frame candles.
		/// </summary>
		/// <param name="source">Smaller time-frame candles.</param>
		/// <param name="compressor">Compressor of candles from smaller time-frames to bigger.</param>
		/// <param name="includeLastCandle">Output last active candle as finished.</param>
		/// <returns>Bigger time-frame candles.</returns>
		public static IEnumerable<CandleMessage> Compress(this IEnumerable<CandleMessage> source, BiggerTimeFrameCandleCompressor compressor, bool includeLastCandle)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (compressor == null)
				throw new ArgumentNullException(nameof(compressor));

			CandleMessage lastActiveCandle = null;
			
			foreach (var message in source)
			{
				foreach (var candleMessage in compressor.Process(message))
				{
					if (candleMessage.State == CandleStates.Finished)
					{
						lastActiveCandle = null;
						yield return candleMessage;
					}
					else
						lastActiveCandle = candleMessage;
				}
			}

			if (!includeLastCandle || lastActiveCandle == null)
				yield break;

			lastActiveCandle.State = CandleStates.Finished;
			yield return lastActiveCandle;
		}

		/// <summary>
		/// Filter time-frames to find multiple smaller time-frames.
		/// </summary>
		/// <param name="timeFrames">All time-frames.</param>
		/// <param name="original">Original time-frame.</param>
		/// <returns>Multiple smaller time-frames.</returns>
		public static IEnumerable<TimeSpan> FilterSmallerTimeFrames(this IEnumerable<TimeSpan> timeFrames, TimeSpan original)
		{
			return timeFrames.Where(t => t < original && (original.Ticks % t.Ticks) == 0);
		}
	}
}