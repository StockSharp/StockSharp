namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Chaikin volatility.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/chv.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ChaikinVolatilityKey,
	Description = LocalizedStrings.ChaikinVolatilityIndicatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/chv.html")]
public class ChaikinVolatility : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChaikinVolatility"/>.
	/// </summary>
	public ChaikinVolatility()
	{
		Ema = new();
		Roc = new();

		AddResetTracking(Ema);
		AddResetTracking(Roc);
	}

	/// <summary>
	/// Moving Average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MAKey,
		Description = LocalizedStrings.MovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage Ema { get; }

	/// <summary>
	/// Rate of change.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ROCKey,
		Description = LocalizedStrings.RateOfChangeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public RateOfChange Roc { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Roc.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => Ema.NumValuesToInitialize + Roc.NumValuesToInitialize - 1;

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var emaValue = Ema.Process(input, candle.GetLength());

		if (Ema.IsFormed)
		{
			var val = Roc.Process(emaValue);

			return val.IsEmpty
				? new DecimalIndicatorValue(this, input.Time)
				: new DecimalIndicatorValue(this, val.ToDecimal(Source), input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		await Ema.LoadIfNotNullAsync(storage, nameof(Ema), cancellationToken);
		await Roc.LoadIfNotNullAsync(storage, nameof(Roc), cancellationToken);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue(nameof(Ema), await Ema.SaveAsync(cancellationToken));
		storage.SetValue(nameof(Roc), await Roc.SaveAsync(cancellationToken));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, Ema={Ema}, Roc={Roc}";
}
