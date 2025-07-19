namespace StockSharp.Algo.Indicators;

/// <summary>
/// Welles Wilder Directional Movement Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dmi.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DMIKey,
	Description = LocalizedStrings.WellesWilderDirectionalMovementIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/dmi.html")]
[IndicatorOut(typeof(DirectionalIndexValue))]
public class DirectionalIndex : BaseComplexIndicator<DirectionalIndexValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DirectionalIndex"/>.
	/// </summary>
	public DirectionalIndex()
	{
		AddInner(Plus = new());
		AddInner(Minus = new());
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
		get => Plus.Length;
		set
		{
			Plus.Length = Minus.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// DI+.
	/// </summary>
	[Browsable(false)]
	public DiPlus Plus { get; }

	/// <summary>
	/// DI-.
	/// </summary>
	[Browsable(false)]
	public DiMinus Minus { get; }

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize => Plus.NumValuesToInitialize;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
var value = new DirectionalIndexValue(this, input.Time) { IsFinal = input.IsFinal };

		var plusValue = Plus.Process(input);
		var minusValue = Minus.Process(input);

		value.Add(Plus, plusValue);
		value.Add(Minus, minusValue);

		if (plusValue.IsEmpty || minusValue.IsEmpty)
			return value;

		var plus = plusValue.ToDecimal();
		var minus = minusValue.ToDecimal();

		var diSum = plus + minus;
		var diDiff = Math.Abs(plus - minus);

		value.Add(this, value.SetValue(this, diSum != 0m ? (100 * diDiff / diSum) : 0m));

		return value;
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
	protected override DirectionalIndexValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="DirectionalIndex"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DirectionalIndexValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="DirectionalIndex"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class DirectionalIndexValue(DirectionalIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<DirectionalIndex>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Plus"/> value.
	/// </summary>
	public IIndicatorValue PlusValue => this[TypedIndicator.Plus];

	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Plus"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Plus => PlusValue.ToNullableDecimal();
	
	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Minus"/> value.
	/// </summary>
	public IIndicatorValue MinusValue => this[TypedIndicator.Minus];

	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Minus"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Minus => MinusValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Plus={Plus}, Minus={Minus}";
}
