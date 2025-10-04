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
[IndicatorOut(typeof(IPriceChannelsValue))]
public class PriceChannels : BaseComplexIndicator<IPriceChannelsValue>
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
	protected override IPriceChannelsValue CreateValue(DateTimeOffset time)
		=> new PriceChannelsValue(this, time);
}

/// <summary>
/// Price Channels indicator value.
/// </summary>
public interface IPriceChannelsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the upper channel value.
	/// </summary>
	IIndicatorValue UpperChannelValue { get; }

	/// <summary>
	/// Gets the upper channel value.
	/// </summary>
	[Browsable(false)]
	decimal? UpperChannel { get; }

	/// <summary>
	/// Gets the lower channel value.
	/// </summary>
	IIndicatorValue LowerChannelValue { get; }

	/// <summary>
	/// Gets the lower channel value.
	/// </summary>
	[Browsable(false)]
	decimal? LowerChannel { get; }
}

class PriceChannelsValue(PriceChannels indicator, DateTimeOffset time) : ComplexIndicatorValue<PriceChannels>(indicator, time), IPriceChannelsValue
{
	public IIndicatorValue UpperChannelValue => this[TypedIndicator.UpperChannel];
	public decimal? UpperChannel => UpperChannelValue.ToNullableDecimal(TypedIndicator.Source);

	public IIndicatorValue LowerChannelValue => this[TypedIndicator.LowerChannel];
	public decimal? LowerChannel => LowerChannelValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"UpperChannel={UpperChannel}, LowerChannel={LowerChannel}";
}