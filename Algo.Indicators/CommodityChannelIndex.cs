namespace StockSharp.Algo.Indicators;

/// <summary>
/// Commodity Channel Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/cci.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CCIKey,
	Description = LocalizedStrings.CommodityChannelIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/cci.html")]
public class CommodityChannelIndex : DecimalLengthIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CommodityChannelIndex"/>.
	/// </summary>
	public CommodityChannelIndex()
	{
		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var aveP = candle.GetTypicalPrice();

		if (input.IsFinal)
			Buffer.PushBack(aveP);

		if (!IsFormed)
			return null;

		var firstIndex = input.IsFinal ? 0 : 1;
		var isFlat = true;

		for (var i = firstIndex; i < Buffer.Count; i++)
			isFlat &= Buffer[i] == aveP;

		if (isFlat)
			return null;

		var average = CalculateAverage(firstIndex, aveP, input.IsFinal);
		var deviationWhole = 0m;
		var deviationRemainder = 0m;

		for (var i = firstIndex; i < Buffer.Count; i++)
			AddDistance(Buffer[i], average, Length, ref deviationWhole, ref deviationRemainder);

		if (!input.IsFinal)
			AddDistance(aveP, average, Length, ref deviationWhole, ref deviationRemainder);

		var meanDeviation = deviationWhole + deviationRemainder / Length;

		if (meanDeviation != 0)
			return DivideDifference(aveP, average, meanDeviation) / 0.015m;

		return null;
	}

	private decimal CalculateAverage(int firstIndex, decimal previewPrice, bool isFinal)
	{
		var whole = 0m;
		var remainder = 0m;

		for (var i = firstIndex; i < Buffer.Count; i++)
			AddDivided(Buffer[i], Length, ref whole, ref remainder);

		if (!isFinal)
			AddDivided(previewPrice, Length, ref whole, ref remainder);

		return whole + remainder / Length;
	}

	private static void AddDistance(decimal value, decimal average, int divisor, ref decimal whole, ref decimal remainder)
	{
		if ((value < 0 && average > 0) || (value > 0 && average < 0))
		{
			AddDivided(Math.Abs(value), divisor, ref whole, ref remainder);
			AddDivided(Math.Abs(average), divisor, ref whole, ref remainder);
		}
		else
			AddDivided((value - average).Abs(), divisor, ref whole, ref remainder);
	}

	private static decimal DivideDifference(decimal value, decimal average, decimal divisor)
	{
		if ((value < 0 && average > 0) || (value > 0 && average < 0))
		{
			var result = Math.Abs(value) / divisor + Math.Abs(average) / divisor;
			return value > average ? result : -result;
		}

		return (value - average) / divisor;
	}

	private static void AddDivided(decimal value, int divisor, ref decimal whole, ref decimal remainder)
	{
		var quotient = decimal.Truncate(value / divisor);
		whole += quotient;
		remainder += value - quotient * divisor;
	}
}
