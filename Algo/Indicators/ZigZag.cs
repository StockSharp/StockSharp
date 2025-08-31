namespace StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="ZigZag"/> indicator value.
/// </summary>
public class ZigZagIndicatorValue : ShiftedIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZagIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public ZigZagIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZagIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Indicator value.</param>
	/// <param name="shift">The shift of the indicator value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="isUp"><see cref="IsUp"/></param>
	public ZigZagIndicatorValue(IIndicator indicator, decimal value, int shift, DateTimeOffset time, bool isUp)
		: base(indicator, value, shift, time)
	{
		IsUp = isUp;
	}

	/// <summary>
	/// Is the trend up.
	/// </summary>
	public bool IsUp { get; }
}

/// <summary>
/// Zig Zag.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/zigzag.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ZigZagKey,
	Description = LocalizedStrings.ZigZagDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[IndicatorOut(typeof(ZigZagIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/zigzag.html")]
public class ZigZag : BaseIndicator
{
	private readonly CircularBufferEx<decimal> _buffer = new(2);
	private decimal? _lastExtremum;
	private int _shift;
	private bool? _isUpTrend;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZag"/>.
	/// </summary>
	public ZigZag()
	{
	}

	private decimal _deviation = 0.001m;

	/// <summary>
	/// Percentage change.
	/// </summary>
	/// <remarks>
	/// It is specified in the range from 0 to 1.
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PercentageChangeKey,
		Description = LocalizedStrings.PercentageChangeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[Range(0.000001, 0.999999)]
	public decimal Deviation
	{
		get => _deviation;
		set
		{
			if (value <= 0 || value >= 1)
				throw new ArgumentOutOfRangeException(nameof(value));

			if (_deviation == value)
				return;

			_deviation = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _buffer.Capacity;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _buffer.Capacity == _buffer.Count;

	/// <inheritdoc />
	public override void Reset()
	{
		_buffer.Clear();
		_lastExtremum = default;
		_shift = default;
		_isUpTrend = default;

		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
		=> CalcZigZag(input, input.ToDecimal());

	/// <inheritdoc />
	protected ZigZagIndicatorValue CalcZigZag(IIndicatorValue input, decimal price)
	{
		IList<decimal> buffer;

		if (input.IsFinal)
		{
			_buffer.PushBack(price);
			buffer = _buffer;
		}
		else
		{
			buffer = [.. _buffer.Skip(1), price];
		}

		if (!IsFormed)
			return new ZigZagIndicatorValue(this, input.Time);

		var lastExtremum = _lastExtremum ?? price;
		var isUpTrend = _isUpTrend ?? price >= buffer[^2];

		var threshold = lastExtremum * Deviation;
		var changeTrend = false;

		if (isUpTrend)
		{
			if (lastExtremum < price)
				lastExtremum = price;
			else
				changeTrend = price <= (lastExtremum - threshold);
		}
		else
		{
			if (lastExtremum > price)
				lastExtremum = price;
			else
				changeTrend = price >= (lastExtremum + threshold);
		}

		if (changeTrend)
		{
			try
			{
				return new ZigZagIndicatorValue(this, lastExtremum, _shift, input.Time, isUpTrend);
			}
			finally
			{
				if (input.IsFinal)
				{
					_isUpTrend = !isUpTrend;
					_lastExtremum = price;
					_shift = 1;
				}
			}
		}
		else
		{
			if (input.IsFinal)
			{
				_lastExtremum = lastExtremum;
				_isUpTrend = isUpTrend;
				_shift++;
			}
		}

		return new ZigZagIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Deviation = storage.GetValue<decimal>(nameof(Deviation));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Deviation), Deviation);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" D={Deviation}";
}