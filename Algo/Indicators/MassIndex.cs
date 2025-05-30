namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Mass Index.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MIKey,
	Description = LocalizedStrings.MassIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/mass_index.html")]
public class MassIndex : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage _singleEma;
	private readonly ExponentialMovingAverage _doubleEma;
	private readonly Sum _sum;

	/// <summary>
	/// Initializes a new instance of the <see cref="MassIndex"/>.
	/// </summary>
	public MassIndex()
	{
		_singleEma = new() { Length = 9 };
		_doubleEma = new() { Length = 9 };

		_sum = new();
		Length = 25;
	}

	/// <summary>
	/// <see cref="ExponentialMovingAverage"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EMAKey,
		Description = LocalizedStrings.ExponentialMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int EmaLength
	{
		get => _singleEma.Length;
		set
		{
			_singleEma.Length = _doubleEma.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _singleEma.NumValuesToInitialize + _sum.NumValuesToInitialize - 1;

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _sum.IsFormed;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var range = candle.GetLength();

		var singleEma = _singleEma.Process(input, range);
		var doubleEma = _doubleEma.Process(singleEma);

		if (_doubleEma.IsFormed)
		{
			var emaRatio = singleEma.ToDecimal() / doubleEma.ToDecimal();
			var sumValue = _sum.Process(input, emaRatio);

			if (_sum.IsFormed)
				return sumValue.ToDecimal();
		}

		return null;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(EmaLength), EmaLength);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		EmaLength = storage.GetValue<int>(nameof(EmaLength));
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_singleEma.Reset();
		_doubleEma.Reset();
		_sum.Reset();

		_sum.Length = Length;

		base.Reset();
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" EMA={EmaLength}";
}
