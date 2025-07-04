namespace StockSharp.Algo.Indicators;

/// <summary>
/// Hull Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/hma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HMAKey,
	Description = LocalizedStrings.HullMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/hma.html")]
public class HullMovingAverage : LengthIndicator<decimal>
{
	private readonly WeightedMovingAverage _wmaSlow = new();
	private readonly WeightedMovingAverage _wmaFast = new();
	private readonly WeightedMovingAverage _wmaResult = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="HullMovingAverage"/>.
	/// </summary>
	public HullMovingAverage()
	{
		Length = 10;
		SqrtPeriod = 0;
	}

	private int _sqrtPeriod;

	/// <summary>
	/// Period of resulting average. If equal to 0, period of resulting average is equal to the square root of HMA period. By default equal to 0.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SqrtKey,
		Description = LocalizedStrings.PeriodResAvgDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SqrtPeriod
	{
		get => _sqrtPeriod;
		set
		{
			_sqrtPeriod = value;
			_wmaResult.Length = value == 0 ? (int)Math.Sqrt(Length) : value;

			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _wmaResult.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + _wmaResult.NumValuesToInitialize - 1;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_wmaSlow.Length = Length;
		_wmaFast.Length = Length / 2;
		_wmaResult.Length = SqrtPeriod == 0 ? (int)Math.Sqrt(Length) : SqrtPeriod;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var slowVal = _wmaSlow.Process(input);
		var fastVal = _wmaFast.Process(input);

		if (_wmaFast.IsFormed && _wmaSlow.IsFormed)
		{
			var diff = 2 * fastVal.ToDecimal() - slowVal.ToDecimal();
			return _wmaResult.Process(diff, input.Time, input.IsFinal).ToDecimal();
		}

		return null;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		SqrtPeriod = storage.GetValue<int>(nameof(SqrtPeriod));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(SqrtPeriod), SqrtPeriod);
	}
}