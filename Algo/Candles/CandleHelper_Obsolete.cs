namespace StockSharp.Algo.Candles;

using StockSharp.Algo.Candles.Compression;

static partial class CandleHelper
{
	/// <summary>
	/// To start candles getting.
	/// </summary>
	/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
	/// <param name="subscriptionProvider">The subscription provider.</param>
	/// <param name="series">Candles series.</param>
	[Obsolete]
	public static void Start<TCandle>(this ISubscriptionProvider subscriptionProvider, CandleSeries series)
		where TCandle : ICandleMessage
	{
		subscriptionProvider.CheckOnNull(nameof(subscriptionProvider)).Subscribe(new(series)
		{
			MarketData =
			{
				From = series.From,
				To = series.To,
			}
		});
	}

	/// <summary>
	/// To get a candles series by the specified parameters.
	/// </summary>
	/// <typeparam name="TCandle">Candles type.</typeparam>
	/// <param name="subscriptionProvider">The subscription provider.</param>
	/// <param name="security">The instrument by which trades should be filtered for the candles creation.</param>
	/// <param name="arg">Candle arg.</param>
	/// <returns>The candles series. <see langword="null" /> if this series is not registered.</returns>
	[Obsolete]
	public static CandleSeries GetSeries<TCandle>(this ISubscriptionProvider subscriptionProvider, Security security, object arg)
		where TCandle : ICandleMessage
	{
		return subscriptionProvider.CheckOnNull(nameof(subscriptionProvider)).Subscriptions.Select(s => s.CandleSeries).WhereNotNull().FirstOrDefault(s => s.CandleType == typeof(TCandle) && s.Security == security && s.Arg.Equals(arg));
	}

	/// <summary>
	/// To create candles from the tick trades collection.
	/// </summary>
	/// <typeparam name="TCandle">Candles type.</typeparam>
	/// <param name="trades">Tick trades.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="onlyFormed">Send only formed candles.</param>
	/// <returns>Candles.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerable<Trade> trades, object arg, bool onlyFormed = true)
		where TCandle : Candle
	{
		var firstTrade = trades.FirstOrDefault();

		if (firstTrade == null)
			return [];

		return trades.ToCandles(new CandleSeries(typeof(TCandle), firstTrade.Security, arg) { IsFinishedOnly = onlyFormed }).Cast<TCandle>();
	}

	/// <summary>
	/// To create candles from the tick trades collection.
	/// </summary>
	/// <param name="trades">Tick trades.</param>
	/// <param name="series">Candles series.</param>
	/// <returns>Candles.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static IEnumerable<Candle> ToCandles(this IEnumerable<Trade> trades, CandleSeries series)
	{
		return trades
			.ToMessages<Trade, ExecutionMessage>()
			.ToCandles(series)
			.ToCandles<Candle>(series.Security);
	}

	/// <summary>
	/// To create candles from the tick trades collection.
	/// </summary>
	/// <param name="trades">Tick trades.</param>
	/// <param name="series">Candles series.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	[Obsolete]
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<ExecutionMessage> trades, CandleSeries series, CandleBuilderProvider candleBuilderProvider = null)
	{
		return trades.ToCandles(series.ToMarketDataMessage(true), candleBuilderProvider);
	}

	/// <summary>
	/// To create candles from the order books collection.
	/// </summary>
	/// <param name="depths">Market depths.</param>
	/// <param name="series">Candles series.</param>
	/// <param name="type">Type of candle depth based data.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	[Obsolete("Use IOrderBookMessage.")]
	public static IEnumerable<Candle> ToCandles(this IEnumerable<MarketDepth> depths, CandleSeries series, Level1Fields type = Level1Fields.SpreadMiddle, CandleBuilderProvider candleBuilderProvider = null)
	{
		return depths
			.ToMessages<MarketDepth, QuoteChangeMessage>()
			.ToCandles(series, type, candleBuilderProvider)
			.ToCandles<Candle>(series.Security);
	}

	/// <summary>
	/// To create candles from the order books collection.
	/// </summary>
	/// <param name="depths">Market depths.</param>
	/// <param name="series">Candles series.</param>
	/// <param name="type">Type of candle depth based data.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	[Obsolete]
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<QuoteChangeMessage> depths, CandleSeries series, Level1Fields type = Level1Fields.SpreadMiddle, CandleBuilderProvider candleBuilderProvider = null)
	{
		return depths.ToCandles(series.ToMarketDataMessage(true), type, candleBuilderProvider);
	}

	/// <summary>
	/// Whether the grouping of candles by the specified attribute is registered.
	/// </summary>
	/// <typeparam name="TCandle">Candles type.</typeparam>
	/// <param name="subscriptionProvider">The subscription provider.</param>
	/// <param name="security">The instrument for which the grouping is registered.</param>
	/// <param name="arg">Candle arg.</param>
	/// <returns><see langword="true" /> if registered. Otherwise, <see langword="false" />.</returns>
	[Obsolete]
	public static bool IsCandlesRegistered<TCandle>(this ISubscriptionProvider subscriptionProvider, Security security, object arg)
		where TCandle : ICandleMessage
	{
		return subscriptionProvider.GetSeries<TCandle>(security, arg) is not null;
	}
}