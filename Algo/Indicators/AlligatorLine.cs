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
		_medianPrice = new MedianPrice();
		_sma = new SmoothedMovingAverage();
		//_sma = new SimpleMovingAverage();
	}

	private int _shift;

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
			_shift = value;
			Reset();
		}
	}

	/// <summary>
	/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
	/// </summary>
	public override void Reset()
	{
		_sma.Length = Length;
		_medianPrice.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count > Shift;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
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
			? new DecimalIndicatorValue(this, Buffer[input.IsFinal ? 0 : Math.Min(1, Buffer.Count - 1)], input.Time)
			: new DecimalIndicatorValue(this, input.Time);
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
}