namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bollinger Bands.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/bollinger_bands.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BollingerKey,
	Description = LocalizedStrings.BollingerBandsKey)]
[Doc("topics/api/indicators/list_of_indicators/bollinger_bands.html")]
[IndicatorOut(typeof(IBollingerBandsValue))]
public class BollingerBands : BaseComplexIndicator<IBollingerBandsValue>
{
	private readonly StandardDeviation _dev = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBands"/>.
	/// </summary>
	public BollingerBands()
		: this(new SimpleMovingAverage())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBands"/>.
	/// </summary>
	/// <param name="ma">Moving Average.</param>
	public BollingerBands(LengthIndicator<decimal> ma)
	{
		AddInner(MovingAverage = ma);
		AddInner(UpBand = new(MovingAverage, _dev) { Name = nameof(UpBand) });
		AddInner(LowBand = new(MovingAverage, _dev) { Name = nameof(LowBand) });
		_dev.Length = ma.Length;
		Width = 2;
	}

	/// <summary>
	/// Middle line.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> MovingAverage { get; }

	/// <summary>
	/// Upper band +.
	/// </summary>
	[Browsable(false)]
	public BollingerBand UpBand { get; }

	/// <summary>
	/// Lower band -.
	/// </summary>
	[Browsable(false)]
	public BollingerBand LowBand { get; }

	/// <summary>
	/// Period length. By default equal to 1.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => MovingAverage.Length;
		set
		{
			MovingAverage.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Bollinger Bands channel width. Default value equal to 2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChannelWidthKey,
		Description = LocalizedStrings.ChannelWidthDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Width
	{
		get => UpBand.Width;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			UpBand.Width = value;
			LowBand.Width = -value;

			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_dev.Length = MovingAverage.Length;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => MovingAverage.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		_dev.Process(input);

		var maValue = MovingAverage.Process(input);

		var value = new BollingerBandsValue(this, input.Time);

		value.Add(MovingAverage, maValue);
		value.Add(UpBand, UpBand.Process(input));
		value.Add(LowBand, LowBand.Process(input));

		return value;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override IBollingerBandsValue CreateValue(DateTimeOffset time)
		=> new BollingerBandsValue(this, time);
}

/// <summary>
/// <see cref="BollingerBands"/> indicator value.
/// </summary>
public interface IBollingerBandsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="BollingerBands.MovingAverage"/> value.
	/// </summary>
	IIndicatorValue MovingAverageValue { get; }

	/// <summary>
	/// Gets the <see cref="BollingerBands.MovingAverage"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? MovingAverage { get; }

	/// <summary>
	/// Gets the <see cref="BollingerBands.UpBand"/> value.
	/// </summary>
	IIndicatorValue UpBandValue { get; }

	/// <summary>
	/// Gets the <see cref="BollingerBands.UpBand"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? UpBand { get; }

	/// <summary>
	/// Gets the <see cref="BollingerBands.LowBand"/> value.
	/// </summary>
	IIndicatorValue LowBandValue { get; }

	/// <summary>
	/// Gets the <see cref="BollingerBands.LowBand"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? LowBand { get; }
}

/// <summary>
/// BollingerBands indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BollingerBandsValue"/> class.
/// </remarks>
/// <param name="indicator">The parent BollingerBands indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class BollingerBandsValue(BollingerBands indicator, DateTimeOffset time) : ComplexIndicatorValue<BollingerBands>(indicator, time), IBollingerBandsValue
{
	/// <inheritdoc />
	public IIndicatorValue MovingAverageValue => this[TypedIndicator.MovingAverage];
	/// <inheritdoc />
	public decimal? MovingAverage => MovingAverageValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue UpBandValue => this[TypedIndicator.UpBand];
	/// <inheritdoc />
	public decimal? UpBand => UpBandValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue LowBandValue => this[TypedIndicator.LowBand];
	/// <inheritdoc />
	public decimal? LowBand => LowBandValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"MovingAverage={MovingAverage}, UpBand={UpBand}, LowBand={LowBand}";
}
