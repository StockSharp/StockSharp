namespace StockSharp.Algo.Indicators;

/// <summary>
/// The realization of one of indicator lines Alligator (Jaw, Teeth, and Lips).
/// </summary>
[IndicatorHidden]
public class AlligatorLine : LengthIndicator<decimal>
{
	private readonly MedianPrice _medianPrice;
	private readonly SmoothedMovingAverage _sma;

	/// <summary>
	/// Initializes a new instance of the <see cref="AlligatorLine"/>.
	/// </summary>
	public AlligatorLine()
	{
		_medianPrice = new();
		_sma = new();
	}

	private int _shift = 1;

	/// <summary>
	/// Shift to the future.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShiftKey,
		Description = LocalizedStrings.ShiftToFutureKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Shift
	{
		get => _shift;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_shift = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override int GetCapacity() => Shift + 1;

	/// <inheritdoc />
	public override void Reset()
	{
		_sma.Length = Length;
		_medianPrice.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count > Shift;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + Shift;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var smaResult = _sma.Process(_medianPrice.Process(input));

		if (_sma.IsFormed & input.IsFinal)
		{
			//если кол-во в буфере больше Shift, то первое значение отдали в прошлый раз, удалим его.
			if (Buffer.Count > Shift)
				Buffer.PopFront();

			Buffer.PushBack(smaResult.ToDecimal());
		}

		return Buffer.Count > Shift
			? Buffer[input.IsFinal ? 0 : Math.Min(1, Buffer.Count - 1)]
			: null;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Shift = storage.GetValue<int>(nameof(Shift));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Shift), Shift);
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()}, Shift={Shift}";
}