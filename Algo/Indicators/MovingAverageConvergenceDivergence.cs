namespace StockSharp.Algo.Indicators;

/// <summary>
/// Convergence/divergence of moving averages.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/macd.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MACDKey,
	Description = LocalizedStrings.MACDDescKey)]
[Doc("topics/api/indicators/list_of_indicators/macd.html")]
public class MovingAverageConvergenceDivergence : BaseIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergence"/>.
	/// </summary>
	public MovingAverageConvergenceDivergence()
		: this(new ExponentialMovingAverage { Length = 26 }, new ExponentialMovingAverage { Length = 12 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergence"/>.
	/// </summary>
	/// <param name="longMa">Long moving average.</param>
	/// <param name="shortMa">Short moving average.</param>
	public MovingAverageConvergenceDivergence(ExponentialMovingAverage longMa, ExponentialMovingAverage shortMa)
	{
		ShortMa = shortMa ?? throw new ArgumentNullException(nameof(shortMa));
		LongMa = longMa ?? throw new ArgumentNullException(nameof(longMa));

		AddResetTracking(ShortMa);
		AddResetTracking(LongMa);
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => LongMa.NumValuesToInitialize.Max(ShortMa.NumValuesToInitialize);

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// Long moving average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongMaKey,
		Description = LocalizedStrings.LongMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage LongMa { get; }

	/// <summary>
	/// Short moving average.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortMaKey,
		Description = LocalizedStrings.ShortMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public ExponentialMovingAverage ShortMa { get; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => LongMa.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var shortValue = ShortMa.Process(input);
		var longValue = LongMa.Process(input);
		return new DecimalIndicatorValue(this, shortValue.ToDecimal() - longValue.ToDecimal(), input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		LongMa.LoadIfNotNull(storage, nameof(LongMa));
		ShortMa.LoadIfNotNull(storage, nameof(ShortMa));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(LongMa), LongMa.Save());
		storage.SetValue(nameof(ShortMa), ShortMa.Save());
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, L={LongMa}, S={ShortMa}";
}
