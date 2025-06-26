namespace StockSharp.Algo.Indicators;

/// <summary>
/// Jurik Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/jma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.JMAKey,
	Description = LocalizedStrings.JurikMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/jma.html")]
public class JurikMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevMa1;
	private decimal _prevMa2;
	private decimal _beta;
	private decimal _phaseRatio;

	private int _phase;

	/// <summary>
	/// Phase.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PhaseKey,
		Description = LocalizedStrings.MaPhaseKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Phase
	{
		get => _phase;
		set
		{
			if (value < -100 || value > 100)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_phase = value;
			Reset();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JurikMovingAverage"/>.
	/// </summary>
	public JurikMovingAverage()
	{
		Length = 20;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (!IsFormed && input.IsFinal)
		{
			_prevMa1 = price;
			_prevMa2 = price;
			Buffer.PushBack(price);

			return price;
		}

		var ma1 = _prevMa1 + _beta * (price - _prevMa1);
		var ma2 = _prevMa2 + _beta * (ma1 - _prevMa2);
		var jma = ma2 + _phaseRatio * (ma2 - _prevMa2);

		if (input.IsFinal)
		{
			_prevMa1 = ma1;
			_prevMa2 = ma2;

			Buffer.PushBack(jma);
		}

		return jma;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_prevMa1 = 0m;
		_prevMa2 = 0m;

		var len = Length;

		_beta = 0.45m * (len - 1) / (0.45m * (len - 1) + 2m);
		_phaseRatio = (_phase + 100m) / 200m;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Phase = storage.GetValue<int>(nameof(Phase));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Phase), Phase);
	}
}
