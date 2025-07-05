namespace StockSharp.Algo.Indicators;

/// <summary>
/// Trix (Triple Exponential Average Oscillator).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/trix.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TrixKey,
	Description = LocalizedStrings.TrixOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/trix.html")]
public class Trix : LengthIndicator<IIndicatorValue>
{
	private readonly ExponentialMovingAverage _ema1 = new();
	private readonly ExponentialMovingAverage _ema2 = new();
	private readonly ExponentialMovingAverage _ema3 = new();
	private readonly RateOfChange _roc = new() { Length = 1 };

	/// <summary>
	/// Initializes a new instance of the <see cref="Trix"/>.
	/// </summary>
	public Trix()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// The length of period <see cref="RateOfChange"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ROCKey,
		Description = LocalizedStrings.RocLengthKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int RocLength
	{
		get => _roc.Length;
		set
		{
			_roc.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int Length
	{
		get => _ema1.Length;
		set
		{
			_ema3.Length = _ema2.Length = _ema1.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_ema1.Reset();
		_ema2.Reset();
		_ema3.Reset();

		_roc.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _roc.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _ema1.NumValuesToInitialize + _ema2.NumValuesToInitialize + _ema3.NumValuesToInitialize + _roc.NumValuesToInitialize - 3;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var ema1Value = _ema1.Process(input);

		if (!_ema1.IsFormed)
			return null;

		var ema2Value = _ema2.Process(ema1Value);

		if (!_ema2.IsFormed)
			return null;

		var ema3Value = _ema3.Process(ema2Value);

		return _ema3.IsFormed
			? 10m * _roc.Process(ema3Value).ToDecimal()
			: null;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		RocLength = storage.GetValue(nameof(RocLength), RocLength);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(RocLength), RocLength);
	}
}