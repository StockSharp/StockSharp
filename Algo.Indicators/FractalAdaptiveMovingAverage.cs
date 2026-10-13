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
public class FractalAdaptiveMovingAverage : DecimalLengthIndicator
{
	private decimal _prevFrama;

	/// <inheritdoc />
	public override int Length
	{
		get => base.Length;
		set
		{
			if (value < 4)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			base.Length = value;
		}
	}

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
		var price = input.ToDecimal(Source);
		var wasFormed = IsFormed;

		if (input.IsFinal)
		{
			Buffer.PushBack(price);

			if (!wasFormed && IsFormed)
				_prevFrama = price;
		}

		if (IsFormed)
		{
			var period = Length / 3;
			var isPreview = !input.IsFinal;
			var n1 = CalculateDimension(0, period, period, price, isPreview);
			var n2 = CalculateDimension(period, period, period, price, isPreview);
			var n3 = CalculateDimension(period * 2, Length - period * 2, period, price, isPreview);

			var d = (Math.Log((double)(n1 + n2)) - Math.Log((double)n3)) / Math.Log(2);
			d = double.IsNaN(d) ? 1 : d.Min(2).Max(1);

			var alpha = Math.Exp(-4.6 * (d - 1));
			var newFrama = (decimal)alpha * price + (1 - (decimal)alpha) * _prevFrama;

			if (input.IsFinal)
				_prevFrama = newFrama;

			return newFrama;
		}

		return null;
	}

	private decimal CalculateDimension(int start, int count, int period, decimal previewPrice, bool isPreview)
	{
		var first = GetWindowPrice(start, previewPrice, isPreview);
		var min = first;
		var max = first;
		var end = start + count;

		for (var i = start + 1; i < end; i++)
		{
			var price = GetWindowPrice(i, previewPrice, isPreview);

			if (price < min)
				min = price;
			else if (price > max)
				max = price;
		}

		return (max - min) / period;
	}

	private decimal GetWindowPrice(int index, decimal previewPrice, bool isPreview)
		=> isPreview
			? index == Length - 1 ? previewPrice : Buffer[index + 1]
			: Buffer[index];

	/// <inheritdoc />
	public override void Reset()
	{
		_prevFrama = default;
		base.Reset();
	}
}
