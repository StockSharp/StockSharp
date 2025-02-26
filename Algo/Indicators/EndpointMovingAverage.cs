namespace StockSharp.Algo.Indicators;

/// <summary>
/// Endpoint Moving Average (EPMA) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EPMAKey,
	Description = LocalizedStrings.EndpointMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/endpoint_moving_average.html")]
public class EndpointMovingAverage : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EndpointMovingAverage"/>.
	/// </summary>
	public EndpointMovingAverage()
	{
		Length = 10;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
		}

		if (IsFormed)
		{
			var firstPoint = Buffer.Front();
			var lastPoint = input.IsFinal ? Buffer.Back() : price;
			var slope = (lastPoint - firstPoint) / (Length - 1);
			var epma = firstPoint + slope * (Length - 1);
			return epma;
		}

		return null;
	}
}