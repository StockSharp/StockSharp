namespace StockSharp.BusinessEntities;

using StockSharp.Algo.Candles;

static partial class EntitiesExtensions
{
	/// <summary>
	/// Cast candle type <see cref="Candle"/> to the message <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="candleType">The type of the candle <see cref="Candle"/>.</param>
	/// <returns>The type of the message <see cref="CandleMessage"/>.</returns>
	[Obsolete("Use candle message type directly.")]
	public static Type ToCandleMessageType(this Type candleType)
	{
		if (candleType is null)
			throw new ArgumentNullException(nameof(candleType));

		if (candleType == typeof(TimeFrameCandle))
			return typeof(TimeFrameCandleMessage);

		throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);
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
}
