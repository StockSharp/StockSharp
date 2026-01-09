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
[IndicatorOut(typeof(IDirectionalIndexValue))]
public class DirectionalIndex : BaseComplexIndicator<IDirectionalIndexValue>
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

		var plus = plusValue.ToDecimal(Source);
		var minus = minusValue.ToDecimal(Source);

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
	protected override IDirectionalIndexValue CreateValue(DateTime time)
		=> new DirectionalIndexValue(this, time);
}

/// <summary>
/// <see cref="DirectionalIndex"/> indicator value.
/// </summary>
public interface IDirectionalIndexValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Plus"/> value.
	/// </summary>
	IIndicatorValue PlusValue { get; }

	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Plus"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Plus { get; }

	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Minus"/> value.
	/// </summary>
	IIndicatorValue MinusValue { get; }

	/// <summary>
	/// Gets the <see cref="DirectionalIndex.Minus"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Minus { get; }
}

/// <summary>
/// DirectionalIndex indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DirectionalIndexValue"/> class.
/// </remarks>
/// <param name="indicator">The parent DirectionalIndex indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class DirectionalIndexValue(DirectionalIndex indicator, DateTime time) : ComplexIndicatorValue<DirectionalIndex>(indicator, time), IDirectionalIndexValue
{
	/// <inheritdoc />
	public IIndicatorValue PlusValue => this[TypedIndicator.Plus];
	/// <inheritdoc />
	public decimal? Plus => PlusValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue MinusValue => this[TypedIndicator.Minus];
	/// <inheritdoc />
	public decimal? Minus => MinusValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Plus={Plus}, Minus={Minus}";
}
