namespace StockSharp.Algo.Indicators;

/// <summary>
/// Welles Wilder Average Directional Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/adx.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AdxKey,
	Description = LocalizedStrings.AverageDirectionalIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/adx.html")]
[IndicatorOut(typeof(AverageDirectionalIndexValue))]
public class AverageDirectionalIndex : BaseComplexIndicator<AverageDirectionalIndexValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
	/// </summary>
	public AverageDirectionalIndex()
		: this(new DirectionalIndex { Length = 14 }, new WilderMovingAverage { Length = 14 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
	/// </summary>
	/// <param name="dx">Welles Wilder Directional Movement Index.</param>
	/// <param name="movingAverage">Moving Average.</param>
	public AverageDirectionalIndex(DirectionalIndex dx, LengthIndicator<decimal> movingAverage)
		: base(dx, movingAverage)
	{
		Dx = dx;
		MovingAverage = movingAverage;
		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Welles Wilder Directional Movement Index.
	/// </summary>
	[Browsable(false)]
	public DirectionalIndex Dx { get; }

	/// <summary>
	/// Moving Average.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> MovingAverage { get; }

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
		get => MovingAverage.Length;
		set
		{
			MovingAverage.Length = Dx.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override AverageDirectionalIndexValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="AverageDirectionalIndex"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AverageDirectionalIndexValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="AverageDirectionalIndex"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class AverageDirectionalIndexValue(AverageDirectionalIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<AverageDirectionalIndex>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="AverageDirectionalIndex.Dx"/> value.
	/// </summary>
	public DirectionalIndexValue Dx => (DirectionalIndexValue)this[TypedIndicator.Dx];
	
	/// <summary>
	/// Gets the <see cref="AverageDirectionalIndex.MovingAverage"/> value.
	/// </summary>
	public IIndicatorValue MovingAverageValue => this[TypedIndicator.MovingAverage];

	/// <summary>
	/// Gets the <see cref="AverageDirectionalIndex.MovingAverage"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? MovingAverage => MovingAverageValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Dx={Dx}, MovingAverage={MovingAverage}";
}