namespace StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="FractalPart"/> indicator value.
/// </summary>
public class FractalPartIndicatorValue : ShiftedIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FractalPartIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public FractalPartIndicatorValue(FractalPart indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalPartIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Indicator value.</param>
	/// <param name="shift">The shift of the indicator value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public FractalPartIndicatorValue(FractalPart indicator, decimal value, int shift, DateTimeOffset time)
		: base(indicator, value, shift, time)
	{
	}

	/// <summary>
	/// Has pattern.
	/// </summary>
	public bool HasPattern => !IsEmpty;

	/// <summary>
	/// Cast object from <see cref="FractalPartIndicatorValue"/> to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">Object <see cref="FractalPartIndicatorValue"/>.</param>
	/// <returns><see cref="bool"/> value.</returns>
	public static explicit operator bool(FractalPartIndicatorValue value)
		=> value.CheckOnNull(nameof(value)).HasPattern;
}

/// <summary>
/// Part <see cref="Fractals"/>.
/// </summary>
[IndicatorHidden]
[IndicatorOut(typeof(FractalPartIndicatorValue))]
public class FractalPart : LengthIndicator<decimal>
{
	private int _numCenter;
	private int _counter;

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalPart"/>.
	/// </summary>
	/// <param name="isUp"><see cref="IsUp"/></param>
	public FractalPart(bool isUp)
	{
		IsUp = isUp;
	}

	/// <summary>
	/// Up value.
	/// </summary>
	public bool IsUp { get; }

	/// <inheritdoc />
	public override int Length
	{
		get => base.Length;
		set
		{
			if (value <= 2 || value % 2 == 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			base.Length = value;
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_numCenter = Length / 2;
		_counter = default;

		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (!input.IsFinal)
			return new FractalPartIndicatorValue(this, input.Time);

		Buffer.PushBack(IsUp ? candle.HighPrice : candle.LowPrice);

		var counter = _counter + 1;

		if (input.IsFinal)
			_counter = counter;

		if (counter < Length)
			return new FractalPartIndicatorValue(this, input.Time);

		var midValue = Buffer[_numCenter];

		for (var i = 0; i < _numCenter; i++)
		{
			if (IsUp && Buffer[i] >= Buffer[i + 1] || !IsUp && Buffer[i] <= Buffer[i + 1])
				return new FractalPartIndicatorValue(this, input.Time);
		}

		for (var i = _numCenter; i < Buffer.Count - 1; i++)
		{
			if (IsUp && Buffer[i] <= Buffer[i + 1] || !IsUp && Buffer[i] >= Buffer[i + 1])
				return new FractalPartIndicatorValue(this, input.Time);
		}

		if (input.IsFinal)
			_counter = default;

		return new FractalPartIndicatorValue(this, midValue, _numCenter, input.Time);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length} {(IsUp ? "Up" : "Down")}";
}