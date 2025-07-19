namespace StockSharp.Algo.Indicators;

/// <summary>
/// Price Channels indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PriceChannelsKey,
	Description = LocalizedStrings.PriceChannelsDescriptionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/price_channels.html")]
[IndicatorOut(typeof(PriceChannelsValue))]
public class PriceChannels : BaseComplexIndicator<PriceChannelsValue>
{
	private readonly Highest _upperChannel;
	private readonly Lowest _lowerChannel;

	/// <summary>
	/// Initializes a new instance of the <see cref="PriceChannels"/>.
	/// </summary>
	public PriceChannels()
	{
		_upperChannel = new() { Name = nameof(UpperChannel) };
		_lowerChannel = new() { Name = nameof(LowerChannel) };

		AddInner(_upperChannel);
		AddInner(_lowerChannel);

		Length = 20;
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
		get => _upperChannel.Length;
		set
		{
			_upperChannel.Length = value;
			_lowerChannel.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Upper channel (highest high).
	/// </summary>
	[Browsable(false)]
	public Highest UpperChannel => _upperChannel;

	/// <summary>
	/// Lower channel (lowest low).
	/// </summary>
	[Browsable(false)]
	public Lowest LowerChannel => _lowerChannel;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var upperValue = _upperChannel.Process(input, candle.HighPrice);
		var lowerValue = _lowerChannel.Process(input, candle.LowPrice);

		var result = new PriceChannelsValue(this, input.Time);
		result.Add(_upperChannel, upperValue);
		result.Add(_lowerChannel, lowerValue);

		return result;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override PriceChannelsValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Price Channels indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PriceChannelsValue"/>.
/// </remarks>
/// <param name="indicator">The indicator.</param>
/// <param name="time">The time.</param>
public class PriceChannelsValue(PriceChannels indicator, DateTimeOffset time) : ComplexIndicatorValue<PriceChannels>(indicator, time)
{
	/// <summary>
	/// Gets the upper channel value.
	/// </summary>
	public IIndicatorValue UpperChannelValue => this[TypedIndicator.UpperChannel];

	/// <summary>
	/// Gets the upper channel value.
	/// </summary>
	[Browsable(false)]
	public decimal? UpperChannel => UpperChannelValue.ToNullableDecimal();

	/// <summary>
	/// Gets the lower channel value.
	/// </summary>
	public IIndicatorValue LowerChannelValue => this[TypedIndicator.LowerChannel];

	/// <summary>
	/// Gets the lower channel value.
	/// </summary>
	[Browsable(false)]
	public decimal? LowerChannel => LowerChannelValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"UpperChannel={UpperChannel}, LowerChannel={LowerChannel}";
}