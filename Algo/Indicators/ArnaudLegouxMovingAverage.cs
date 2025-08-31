namespace StockSharp.Algo.Indicators;

/// <summary>
/// Arnaud Legoux Moving Average (ALMA) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ALMAKey,
	Description = LocalizedStrings.ArnaudLegouxMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/arnaud_legoux_moving_average.html")]
public class ArnaudLegouxMovingAverage : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ArnaudLegouxMovingAverage"/>.
	/// </summary>
	public ArnaudLegouxMovingAverage()
	{
		Length = 9;
		Offset = 0.85m;
		Sigma = 6;
	}

	private decimal _offset;

	/// <summary>
	/// Offset.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OffsetKey,
		Description = LocalizedStrings.OffsetKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[Range(0.000001, 0.999999)]
	public decimal Offset
	{
		get => _offset;
		set
		{
			if (value <= 0 || value >= 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_offset = value;
			Reset();
		}
	}

	private int _sigma;

	/// <summary>
	/// Sigma.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SigmaKey,
		Description = LocalizedStrings.SigmaKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Sigma
	{
		get => _sigma;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_sigma = value;
			Reset();
		}
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
			var m = Offset * (Length - 1);
			var s = Length / (decimal)Sigma;

			var weightSum = 0m;
			var sum = 0m;

			for (var i = 0; i < Length; i++)
			{
				var weight = (decimal)Math.Exp(-Math.Pow((double)((i - m) / s), 2) / 2);
				weightSum += weight;

				decimal value;

				if (input.IsFinal)
				{
					value = Buffer[Length - 1 - i];
				}
				else
				{
					if (i == 0)
						value = price;
					else
						value = Buffer[Length - i];
				}

				sum += weight * value;
			}

			return sum / weightSum;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Offset), Offset);
		storage.SetValue(nameof(Sigma), Sigma);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Offset = storage.GetValue<decimal>(nameof(Offset));
		Sigma = storage.GetValue<int>(nameof(Sigma));
	}
}
