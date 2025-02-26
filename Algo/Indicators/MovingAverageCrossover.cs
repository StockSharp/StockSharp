namespace StockSharp.Algo.Indicators;

/// <summary>
/// Moving Average Crossover indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MACKey,
	Description = LocalizedStrings.MovingAverageCrossoverKey)]
[Doc("topics/api/indicators/list_of_indicators/moving_average_crossover.html")]
public class MovingAverageCrossover : BaseIndicator
{
	private readonly SimpleMovingAverage _fastMa;
	private readonly SimpleMovingAverage _slowMa;

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageCrossover"/>.
	/// </summary>
	public MovingAverageCrossover()
		: this(new() { Length = 25 }, new() { Length = 50 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageCrossover"/>.
	/// </summary>
	/// <param name="fastMa">Fast moving average.</param>
	/// <param name="slowMa">Slow moving average.</param>
	public MovingAverageCrossover(SimpleMovingAverage fastMa, SimpleMovingAverage slowMa)
	{
		_fastMa = fastMa ?? throw new ArgumentNullException(nameof(fastMa));
		_slowMa = slowMa ?? throw new ArgumentNullException(nameof(slowMa));
	}

	/// <summary>
	/// Shortest Moving Average period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortKey,
		Description = LocalizedStrings.ShortPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortPeriod
	{
		get => _fastMa.Length;
		set
		{
			_fastMa.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Longest Moving Average period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongKey,
		Description = LocalizedStrings.LongPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => _slowMa.Length;
		set
		{
			_slowMa.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _fastMa.IsFormed && _slowMa.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _fastMa.NumValuesToInitialize.Max(_slowMa.NumValuesToInitialize);

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_fastMa.Reset();
		_slowMa.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var fastValue = _fastMa.Process(input);
		var slowValue = _slowMa.Process(input);

		if (IsFormed)
		{
			var fast = fastValue.ToDecimal();
			var slow = slowValue.ToDecimal();
			var signal = fast.CompareTo(slow);
			return new DecimalIndicatorValue(this, signal, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(ShortPeriod), ShortPeriod);
		storage.SetValue(nameof(LongPeriod), LongPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		ShortPeriod = storage.GetValue<int>(nameof(ShortPeriod));
		LongPeriod = storage.GetValue<int>(nameof(LongPeriod));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" S={ShortPeriod} L={LongPeriod}";
}