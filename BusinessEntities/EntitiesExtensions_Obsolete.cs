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
