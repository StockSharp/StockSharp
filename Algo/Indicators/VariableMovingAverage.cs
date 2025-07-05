namespace StockSharp.Algo.Indicators;

/// <summary>
/// Variable Moving Average (VMA).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/vma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VMAKey,
	Description = LocalizedStrings.VariableMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/vma.html")]
public class VariableMovingAverage : LengthIndicator<decimal>
{
	private readonly StandardDeviation _stdDev;
	private decimal _prevFinalValue;
	private bool _isInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="VariableMovingAverage"/>.
	/// </summary>
	public VariableMovingAverage()
	{
		_stdDev = new();
		Length = 20;
		VolatilityIndex = 0.2m;

		Buffer.Operator = new DecimalOperator();
	}

	private decimal _volatilityIndex;

	/// <summary>
	/// Volatility index factor. Default value is 0.2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolatilityIndexKey,
		Description = LocalizedStrings.VolatilityIndexDescriptionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal VolatilityIndex
	{
		get => _volatilityIndex;
		set
		{
			_volatilityIndex = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_stdDev.Length = Length;
		_prevFinalValue = 0;
		_isInitialized = false;
		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _stdDev.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (!_isInitialized)
		{
			if (input.IsFinal)
			{
				Buffer.PushBack(newValue);
				_prevFinalValue = Buffer.Sum / Buffer.Count;
				_isInitialized = true;
			}
			else
			{
				return new DecimalIndicatorValue(this, (Buffer.SumNoFirst + newValue) / Length, input.Time);
			}

			return new DecimalIndicatorValue(this, _prevFinalValue, input.Time);
		}

		// Calculate standard deviation for volatility measure
		var stdDevValue = _stdDev.Process(input);

		if (!_stdDev.IsFormed)
			return new DecimalIndicatorValue(this, _prevFinalValue, input.Time);

		// Calculate Variable Index (VI)
		var avgPrice = Buffer.Sum / Buffer.Count;
		var volatility = stdDevValue.ToDecimal();

		// Avoid division by zero
		var vi = avgPrice != 0 ? Math.Abs(volatility / avgPrice) : 0;

		// Calculate smoothing constant based on volatility
		var smoothingConstant = 2m / (Length * (1 + VolatilityIndex * vi) + 1);

		// Calculate VMA using EMA-like formula with variable smoothing
		var curValue = (newValue - _prevFinalValue) * smoothingConstant + _prevFinalValue;

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
			_prevFinalValue = curValue;
		}

		return new DecimalIndicatorValue(this, curValue, input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		VolatilityIndex = storage.GetValue<decimal>(nameof(VolatilityIndex));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(VolatilityIndex), VolatilityIndex);
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, VOL={VolatilityIndex}";
}