namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bollinger %b indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BBPKey,
	Description = LocalizedStrings.BollingerPercentBKey)]
[Doc("topics/api/indicators/list_of_indicators/bollinger_percent_b.html")]
public class BollingerPercentB : BaseIndicator
{
	private readonly BollingerBands _bollingerBands;

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerPercentB"/>.
	/// </summary>
	public BollingerPercentB()
	{
		_bollingerBands = new();
		Length = 20;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StdDevKey,
		Description = LocalizedStrings.StdDevMultiplierKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal StdDevMultiplier
	{
		get => _bollingerBands.Width;
		set
		{
			_bollingerBands.Width = value;
			Reset();
		}
	}

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _bollingerBands.Length;
		set
		{
			_bollingerBands.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _bollingerBands.NumValuesToInitialize;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var bbValue = (BollingerBandsValue)_bollingerBands.Process(input);

		if (_bollingerBands.IsFormed)
		{
			if (input.IsFinal)
				IsFormed = true;

			if (bbValue.UpBand is not decimal upperBand)
				return new DecimalIndicatorValue(this, input.Time);

			if (bbValue.LowBand is not decimal lowerBand)
				return new DecimalIndicatorValue(this, input.Time);

			var bandWidth = upperBand - lowerBand;

			if (bandWidth != 0)
			{
				var price = input.ToDecimal();

				var percentB = (price - lowerBand) / bandWidth * 100;
				return new DecimalIndicatorValue(this, percentB, input.Time);
			}
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_bollingerBands.Reset();
		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(nameof(BollingerBands), _bollingerBands.Save());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_bollingerBands.LoadIfNotNull(storage, nameof(BollingerBands));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" LEN={Length} STD={StdDevMultiplier}";
}
