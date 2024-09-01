namespace StockSharp.Algo.Indicators;

/// <summary>
/// Dynamic Zones RSI indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DZRSIKey,
	Description = LocalizedStrings.DynamicZonesRSIKey)]
[Doc("topics/indicators/dynamic_zones_rsi.html")]
public class DynamicZonesRSI : LengthIndicator<decimal>
{
	private readonly RelativeStrengthIndex _rsi;

	/// <summary>
	/// Initializes a new instance of the <see cref="DynamicZonesRSI"/>.
	/// </summary>
	public DynamicZonesRSI()
	{
		_rsi = new() { Length = 14 };
		Length = _rsi.Length;
		Buffer.MaxComparer = Comparer<decimal>.Default;
		Buffer.MinComparer = Comparer<decimal>.Default;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	private decimal _oversoldLevel = 20;

	/// <summary>
	/// Oversold level.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OversoldKey,
		Description = LocalizedStrings.OversoldLevelKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal OversoldLevel
	{
		get => _oversoldLevel;
		set
		{
			if (_oversoldLevel == value)
				return;

			_oversoldLevel = value;
			Reset();
		}
	}

	private decimal _overboughtLevel = 80;

	/// <summary>
	/// Overbought level.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OverboughtKey,
		Description = LocalizedStrings.OverboughtLevelKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal OverboughtLevel
	{
		get => _overboughtLevel;
		set
		{
			if (_overboughtLevel == value)
				return;

			_overboughtLevel = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int Length
	{
		get => base.Length;
		set
		{
			base.Length = value;
			_rsi.Length = value;
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var rsiValue = _rsi.Process(input);

		if (_rsi.IsFormed)
		{
			var rsi = rsiValue.ToDecimal();

			decimal min, max;
			if (input.IsFinal)
			{
				Buffer.AddEx(rsi);

				min = Buffer.Min.Value;
				max = Buffer.Max.Value;
			}
			else
			{
				min = Buffer.Count > 0 ? Math.Min(Buffer.Min.Value, rsi) : rsi;
				max = Buffer.Count > 0 ? Math.Max(Buffer.Max.Value, rsi) : rsi;
			}

			if (IsFormed)
			{
				var dynamicOversold = min + (max - min) * OversoldLevel / 100m;
				var dynamicOverbought = min + (max - min) * OverboughtLevel / 100m;

				decimal dynamicRsi;
				if (rsi <= dynamicOversold)
					dynamicRsi = 0;
				else if (rsi >= dynamicOverbought)
					dynamicRsi = 100;
				else
					dynamicRsi = (rsi - dynamicOversold) / (dynamicOverbought - dynamicOversold) * 100;

				return new DecimalIndicatorValue(this, dynamicRsi, input.Time);
			}
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_rsi.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(OversoldLevel), OversoldLevel);
		storage.SetValue(nameof(OverboughtLevel), OverboughtLevel);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		OversoldLevel = storage.GetValue<decimal>(nameof(OversoldLevel));
		OverboughtLevel = storage.GetValue<decimal>(nameof(OverboughtLevel));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length} OS={OversoldLevel} OB={OverboughtLevel}";
}