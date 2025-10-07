namespace StockSharp.BusinessEntities;

using StockSharp.Algo.Candles;

static partial class EntitiesExtensions
{
	static EntitiesExtensions()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		RegisterCandle(() => new TimeFrameCandle(), () => new TimeFrameCandleMessage());
		RegisterCandle(() => new TickCandle(), () => new TickCandleMessage());
		RegisterCandle(() => new VolumeCandle(), () => new VolumeCandleMessage());
		RegisterCandle(() => new RangeCandle(), () => new RangeCandleMessage());
		RegisterCandle(() => new PnFCandle(), () => new PnFCandleMessage());
		RegisterCandle(() => new RenkoCandle(), () => new RenkoCandleMessage());
		RegisterCandle(() => new HeikinAshiCandle(), () => new HeikinAshiCandleMessage());
#pragma warning restore CS0618 // Type or member is obsolete
	}

	private static readonly CachedSynchronizedPairSet<Type, Type> _candleTypes = [];

	/// <summary>
	/// Cast candle type <see cref="Candle"/> to the message <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="candleType">The type of the candle <see cref="Candle"/>.</param>
	/// <returns>The type of the message <see cref="CandleMessage"/>.</returns>
	public static Type ToCandleMessageType(this Type candleType)
	{
		if (candleType is null)
			throw new ArgumentNullException(nameof(candleType));

		if (!_candleTypes.TryGetValue(candleType, out var messageType))
			throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

		return messageType;
	}

	/// <summary>
	/// Cast message type <see cref="CandleMessage"/> to the candle type <see cref="Candle"/>.
	/// </summary>
	/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
	/// <returns>The type of the candle <see cref="Candle"/>.</returns>
	public static Type ToCandleType(this Type messageType)
	{
		if (messageType is null)
			throw new ArgumentNullException(nameof(messageType));

		if (!_candleTypes.TryGetKey(messageType, out var candleType))
			throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

		return candleType;
	}

	[Obsolete]
	private static readonly SynchronizedDictionary<Type, Func<Candle>> _candleCreators = [];

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <typeparam name="TCandle">Candle type.</typeparam>
	/// <typeparam name="TMessage">The type of candle message.</typeparam>
	/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
	/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
	[Obsolete("Conversion reduce performance.")]
	public static void RegisterCandle<TCandle, TMessage>(Func<TCandle> candleCreator, Func<TMessage> candleMessageCreator)
		where TCandle : Candle
		where TMessage : CandleMessage
	{
		RegisterCandle(typeof(TCandle), typeof(TMessage), candleCreator, candleMessageCreator);
	}

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <param name="candleType">Candle type.</param>
	/// <param name="messageType">The type of candle message.</param>
	/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
	/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
	[Obsolete("Conversion reduce performance.")]
	public static void RegisterCandle(Type candleType, Type messageType, Func<Candle> candleCreator, Func<CandleMessage> candleMessageCreator)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		if (candleCreator == null)
			throw new ArgumentNullException(nameof(candleCreator));

		if (candleMessageCreator == null)
			throw new ArgumentNullException(nameof(candleMessageCreator));

		_candleTypes.Add(candleType, messageType);
		_candleCreators.Add(candleType, candleCreator);
	}

	/// <summary>
	/// Cast <see cref="CandleSeries"/> to <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="series">Candles series.</param>
	/// <param name="isSubscribe">The message is subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Candles count.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
	[Obsolete("Use Subscription class.")]
	public static MarketDataMessage ToMarketDataMessage(this CandleSeries series, bool isSubscribe, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, bool throwIfInvalidType = true)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = isSubscribe,
			From = from ?? series.From,
			To = to ?? series.To,
			Count = count ?? series.Count,
			BuildMode = series.BuildCandlesMode,
			BuildFrom = series.BuildCandlesFrom2,
			BuildField = series.BuildCandlesField,
			IsCalcVolumeProfile = series.IsCalcVolumeProfile,
			AllowBuildFromSmallerTimeFrame = series.AllowBuildFromSmallerTimeFrame,
			IsRegularTradingHours = series.IsRegularTradingHours,
			IsFinishedOnly = series.IsFinishedOnly,
		};

		if (series.CandleType == null)
		{
			if (throwIfInvalidType)
				throw new ArgumentException(LocalizedStrings.WrongCandleType);
		}
		else
		{
			var msgType = series
				.CandleType
				.ToCandleMessageType();

			mdMsg.DataType2 = DataType.Create(msgType, series.Arg);
		}

		mdMsg.ValidateBounds();
		series.Security?.ToMessage(copyExtendedId: true).CopyTo(mdMsg, false);

		return mdMsg;
	}

	/// <summary>
	/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="security">Security.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this MarketDataMessage message, Security security, bool throwIfInvalidType)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return message.ToCandleSeries(new CandleSeries { Security = security }, throwIfInvalidType);
	}

	/// <summary>
	/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="series">Candles series.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this MarketDataMessage message, CandleSeries series, bool throwIfInvalidType)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (series == null)
			throw new ArgumentNullException(nameof(series));

		if (message.DataType2.IsCandles)
		{
			series.CandleType = message.DataType2.MessageType.ToCandleType();
			series.Arg = message.GetArg();
		}
		else
		{
			if (throwIfInvalidType)
				throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(message.DataType2), nameof(message));
		}

		series.From = message.From;
		series.To = message.To;
		series.Count = message.Count;
		series.BuildCandlesMode = message.BuildMode;
		series.BuildCandlesFrom2 = message.BuildFrom;
		series.BuildCandlesField = message.BuildField;
		series.IsCalcVolumeProfile = message.IsCalcVolumeProfile;
		series.AllowBuildFromSmallerTimeFrame = message.AllowBuildFromSmallerTimeFrame;
		series.IsRegularTradingHours = message.IsRegularTradingHours;
		series.IsFinishedOnly = message.IsFinishedOnly;

		return series;
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="security">The instrument to be used for candles formation.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this DataType dataType, Security security)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return new()
		{
			CandleType = dataType.MessageType.ToCandleType(),
			Arg = dataType.Arg,
			Security = security,
		};
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
	/// </summary>
	/// <param name="series">Candles series.</param>
	/// <returns>Data type info.</returns>
	[Obsolete("Use Subscription class.")]
	public static DataType ToDataType(this CandleSeries series)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		return DataType.Create(series.CandleType.ToCandleMessageType(), series.Arg);
	}

	/// <summary>
	/// Determines whether the specified type is derived from <see cref="Candle"/>.
	/// </summary>
	/// <param name="candleType">The candle type.</param>
	/// <returns><see langword="true"/> if the specified type is derived from <see cref="Candle"/>, otherwise, <see langword="false"/>.</returns>
	[Obsolete("Use ICandleMessage.")]
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
	[Obsolete("Use Subscription class.")]
	public static CandleSeries TimeFrame(this Security security, TimeSpan arg)
		=> arg.TimeFrame().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="RangeCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="RangeCandle.PriceRange"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Range(this Security security, Unit arg)
		=> arg.Range().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="VolumeCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="VolumeCandle.Volume"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Volume(this Security security, decimal arg)
		=> arg.Volume().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="TickCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="TickCandle.MaxTradeCount"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Tick(this Security security, int arg)
		=> arg.Tick().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="PnFCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="PnFCandle.PnFArg"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries PnF(this Security security, PnFArg arg)
		=> arg.PnF().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="RenkoCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="RenkoCandle.BoxSize"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Renko(this Security security, Unit arg)
		=> arg.Renko().ToCandleSeries(security);

	/// <summary>
	/// Determines the specified candle series if time frame based.
	/// </summary>
	/// <param name="series"><see cref="CandleSeries"/></param>
	/// <returns>Check result.</returns>
	[Obsolete("Use Subscription class.")]
	public static bool IsTimeFrame(this CandleSeries series)
		=> series.CheckOnNull(nameof(series)).CandleType == typeof(TimeFrameCandle);

	/// <summary>
	/// To set the <see cref="Unit.GetTypeValue"/> property for the value.
	/// </summary>
	/// <param name="unit">Unit.</param>
	/// <param name="security">Security.</param>
	/// <returns>Unit.</returns>
	[Obsolete("Unit.GetTypeValue obsolete.")]
	public static Unit SetSecurity(this Unit unit, Security security)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		return unit;
	}
}
