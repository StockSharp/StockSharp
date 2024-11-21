namespace StockSharp.Algo.Indicators;

/// <summary>
/// Part <see cref="Fractals"/>.
/// </summary>
[IndicatorHidden]
[IndicatorOut(typeof(ShiftedIndicatorValue))]
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
			return new ShiftedIndicatorValue(this, input.Time);

		Buffer.PushBack(IsUp ? candle.HighPrice : candle.LowPrice);

		if (++_counter < Length)
			return new ShiftedIndicatorValue(this, input.Time);

		var midValue = Buffer[_numCenter];

		for (var i = 0; i < _numCenter; i++)
		{
			if (IsUp && Buffer[i] >= Buffer[i + 1] || !IsUp && Buffer[i] <= Buffer[i + 1])
				return new ShiftedIndicatorValue(this, input.Time);
		}

		for (var i = _numCenter; i < Buffer.Count - 1; i++)
		{
			if (IsUp && Buffer[i] <= Buffer[i + 1] || !IsUp && Buffer[i] >= Buffer[i + 1])
				return new ShiftedIndicatorValue(this, input.Time);
		}

		_counter = default;
		return new ShiftedIndicatorValue(this, midValue, _numCenter, input.Time);
	}
}