namespace StockSharp.Algo.Indicators;

/// <summary>
/// The shifted value of the indicator.
/// </summary>
public class ShiftedIndicatorValue : SingleIndicatorValue<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ShiftedIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public ShiftedIndicatorValue(IIndicator indicator, DateTime time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ShiftedIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Indicator value.</param>
	/// <param name="shift">The shift of the indicator value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public ShiftedIndicatorValue(IIndicator indicator, decimal value, int shift, DateTime time)
		: base(indicator, value, time)
	{
		Shift = shift;
	}

	private int _shift;

	/// <summary>
	/// The shift of the indicator value.
	/// </summary>
	public int Shift
	{
		get => _shift;
		private set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_shift = value;
		}
	}

	/// <inheritdoc />
	public override IEnumerable<object> ToValues()
	{
		if (IsEmpty)
			yield break;

		foreach (var v in base.ToValues())
			yield return v;

		yield return Shift;
	}

	/// <inheritdoc />
	public override void FromValues(object[] values)
	{
		if (values.Length == 0)
			return;

		base.FromValues(values);
		
		Shift = values[1].To<int>();
	}
}