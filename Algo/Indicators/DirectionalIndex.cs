﻿namespace StockSharp.Algo.Indicators;

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
public class DirectionalIndex : BaseComplexIndicator
{
	private class DxValue : ComplexIndicatorValue
	{
		private decimal _value;

		public DxValue(IComplexIndicator indicator, DateTimeOffset time)
			: base(indicator, time)
		{
		}

		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			IsEmpty = false;
			_value = value.To<decimal>();
			return new DecimalIndicatorValue(indicator, _value, Time) { IsFinal = IsFinal };
		}

		public override T GetValue<T>(Level1Fields? field)
		{
			return _value.To<T>();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DirectionalIndex"/>.
	/// </summary>
	public DirectionalIndex()
	{
		AddInner(Plus = new());
		AddInner(Minus = new());
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

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
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DiPlusKey,
		Description = LocalizedStrings.DiPlusLineKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DiPlus Plus { get; }

	/// <summary>
	/// DI-.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DiMinusKey,
		Description = LocalizedStrings.DiMinusLineKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DiMinus Minus { get; }

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = new DxValue(this, input.Time) { IsFinal = input.IsFinal };

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
}