namespace StockSharp.Algo.Indicators;

/// <summary>
/// Keltner Channels indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/keltner_channels.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KCKey,
	Description = LocalizedStrings.KeltnerChannelsKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/keltner_channels.html")]
[IndicatorOut(typeof(IKeltnerChannelsValue))]
public class KeltnerChannels : BaseComplexIndicator<IKeltnerChannelsValue>
{
	private readonly AverageTrueRange _atr;

	/// <summary>
	/// Initializes a new instance of the <see cref="KeltnerChannels"/>.
	/// </summary>
	public KeltnerChannels()
		: this(new KeltnerChannelMiddle(), new AverageTrueRange(), new KeltnerChannelBand(), new KeltnerChannelBand())
	{
		Length = 20;
		Multiplier = 2;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KeltnerChannels"/>.
	/// </summary>
	/// <param name="middle">Middle line.</param>
	/// <param name="atr">Average True Range.</param>
	/// <param name="upper">Upper line.</param>
	/// <param name="lower">Lower line.</param>
	public KeltnerChannels(KeltnerChannelMiddle middle, AverageTrueRange atr, KeltnerChannelBand upper, KeltnerChannelBand lower)
		: base(middle, upper, lower)
	{
		Middle = middle ?? throw new ArgumentNullException(nameof(middle));
		_atr = atr ?? throw new ArgumentNullException(nameof(atr));
		Upper = upper ?? throw new ArgumentNullException(nameof(upper));
		Lower = lower ?? throw new ArgumentNullException(nameof(lower));
	}

	/// <summary>
	/// Middle line.
	/// </summary>
	[Browsable(false)]
	public KeltnerChannelMiddle Middle { get; }

	/// <summary>
	/// Upper line.
	/// </summary>
	[Browsable(false)]
	public KeltnerChannelBand Upper { get; }

	/// <summary>
	/// Lower line.
	/// </summary>
	[Browsable(false)]
	public KeltnerChannelBand Lower { get; }

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
		get => Middle.Length;
		set => Middle.Length = Upper.Length = Lower.Length = _atr.Length = value;
	}

	private decimal _multiplier;

	/// <summary>
	/// Multiplier for ATR.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_multiplier = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_atr.Reset();
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var middleValue = Middle.Process(input);
		var atrValue = _atr.Process(input);

		var result = new KeltnerChannelsValue(this, input.Time);

		if (IsFormed)
		{
			var middle = middleValue.ToDecimal(Source);
			var atr = atrValue.ToDecimal(Source);
			var offset = Multiplier * atr;

			result.Add(Middle, middleValue);
			result.Add(Upper, Upper.Process(input, middle + offset));
			result.Add(Lower, Lower.Process(input, middle - offset));
		}

		return result;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Middle.IsFormed && _atr.IsFormed;

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length},M={Multiplier}";

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Length), Length);
		storage.SetValue(nameof(Multiplier), Multiplier);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Length = storage.GetValue<int>(nameof(Length));
		Multiplier = storage.GetValue<decimal>(nameof(Multiplier));
	}

	/// <inheritdoc />
	protected override IKeltnerChannelsValue CreateValue(DateTime time)
		=> new KeltnerChannelsValue(this, time);
}

/// <summary>
/// Middle line of Keltner Channel (EMA).
/// </summary>
[IndicatorHidden]
public class KeltnerChannelMiddle : ExponentialMovingAverage
{
}

/// <summary>
/// Base class for upper and lower lines of Keltner Channel.
/// </summary>
[IndicatorHidden]
public class KeltnerChannelBand : DecimalLengthIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="KeltnerChannels"/> indicator value.
/// </summary>
public interface IKeltnerChannelsValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Middle"/> value.
	/// </summary>
	IIndicatorValue MiddleValue { get; }

	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Middle"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Middle { get; }

	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Upper"/> value.
	/// </summary>
	IIndicatorValue UpperValue { get; }

	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Upper"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Upper { get; }

	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Lower"/> value.
	/// </summary>
	IIndicatorValue LowerValue { get; }

	/// <summary>
	/// Gets the <see cref="KeltnerChannels.Lower"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? Lower { get; }
}

/// <summary>
/// KeltnerChannels indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeltnerChannelsValue"/> class.
/// </remarks>
/// <param name="indicator">The parent KeltnerChannels indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class KeltnerChannelsValue(KeltnerChannels indicator, DateTime time) : ComplexIndicatorValue<KeltnerChannels>(indicator, time), IKeltnerChannelsValue
{
	/// <inheritdoc />
	public IIndicatorValue MiddleValue => this[TypedIndicator.Middle];
	/// <inheritdoc />
	public decimal? Middle => MiddleValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue UpperValue => this[TypedIndicator.Upper];
	/// <inheritdoc />
	public decimal? Upper => UpperValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue LowerValue => this[TypedIndicator.Lower];
	/// <inheritdoc />
	public decimal? Lower => LowerValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Middle={Middle}, Upper={Upper}, Lower={Lower}";
}
