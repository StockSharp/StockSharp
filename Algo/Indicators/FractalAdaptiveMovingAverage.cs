namespace StockSharp.Algo.Indicators;

/// <summary>
/// Fractal Adaptive Moving Average (FRAMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FRAMAKey,
	Description = LocalizedStrings.FractalAdaptiveMovingAverageKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/fractal_adaptive_moving_average.html")]
public class FractalAdaptiveMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevFrama;

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalAdaptiveMovingAverage"/>.
	/// </summary>
	public FractalAdaptiveMovingAverage()
	{
		Length = 20;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
			Buffer.PushBack(price);

		if (IsFormed)
		{
			var period = Length / 3;

			decimal calculateDimension(IEnumerable<decimal> prices)
				=> (prices.Max() - prices.Min()) / period;

			var n1 = calculateDimension(Buffer.Take(period));
			var n2 = calculateDimension(Buffer.Skip(period).Take(period));
			var n3 = calculateDimension(Buffer.Skip(period * 2));

			var d = (Math.Log((double)(n1 + n2)) - Math.Log((double)n3)) / Math.Log(2);
			d = Math.Max(Math.Min(d, 2), 1);

			var alpha = Math.Exp(-4.6 * (d - 1));
			var newFrama = (decimal)alpha * price + (1 - (decimal)alpha) * _prevFrama;

			if (input.IsFinal)
				_prevFrama = newFrama;

			return newFrama;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevFrama = default;
		base.Reset();
	}
}