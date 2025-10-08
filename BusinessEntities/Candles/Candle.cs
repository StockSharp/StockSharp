namespace StockSharp.Algo.Candles;

using StockSharp.BusinessEntities;

/// <summary>
/// Base candle class (contains main parameters).
/// </summary>
[DataContract]
[Serializable]
[KnownType(typeof(TimeFrameCandle))]
[Obsolete("Use ICandleMessage.")]
public abstract class Candle : Cloneable<Candle>, ICandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Candle"/>.
	/// </summary>
	protected Candle()
	{
	}

	private SecurityId? _securityId;

	SecurityId ISecurityIdMessage.SecurityId
	{
		get => _securityId ??= Security?.Id.ToSecurityId() ?? default;
		set => throw new NotSupportedException();
	}

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => OpenTime;
		set => OpenTime = value;
	}

	DateTimeOffset ILocalTimeMessage.LocalTime => OpenTime;

	/// <summary>
	/// Security.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.SecurityKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Security Security { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleOpenTimeKey,
		Description = LocalizedStrings.CandleOpenTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset OpenTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleCloseTimeKey,
		Description = LocalizedStrings.CandleCloseTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset CloseTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleHighTimeKey,
		Description = LocalizedStrings.CandleHighTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset HighTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleLowTimeKey,
		Description = LocalizedStrings.CandleLowTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset LowTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenPriceKey,
		Description = LocalizedStrings.CandleOpenPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal OpenPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClosingPriceKey,
		Description = LocalizedStrings.ClosePriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal ClosePrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestPriceKey,
		Description = LocalizedStrings.HighPriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal HighPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowestPriceKey,
		Description = LocalizedStrings.LowPriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal LowPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TotalPriceKey,
		Description = LocalizedStrings.TotalPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal TotalPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenVolumeKey,
		Description = LocalizedStrings.OpenVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? OpenVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CloseVolumeKey,
		Description = LocalizedStrings.CloseVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? CloseVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighVolumeKey,
		Description = LocalizedStrings.HighVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? HighVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowVolumeKey,
		Description = LocalizedStrings.LowVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? LowVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.TotalCandleVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal TotalVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RelativeVolumeKey,
		Description = LocalizedStrings.RelativeVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? RelativeVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BuyVolumeKey,
		Description = LocalizedStrings.BuyVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? BuyVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SellVolumeKey,
		Description = LocalizedStrings.SellVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? SellVolume { get; set; }

	/// <inheritdoc />
	public abstract object Arg { get; set; }

	private DataType _dataType;

	/// <inheritdoc />
	public DataType DataType
	{
		get
		{
			if (_dataType is null)
			{
				var arg = Arg;

				if (!arg.IsNull(true))
					_dataType = DataType.Create(typeof(TimeFrameCandleMessage), arg);
			}

			return _dataType;
		}
		set => throw new NotSupportedException();
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TicksKey,
		Description = LocalizedStrings.TickCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int? TotalTicks { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TickUpKey,
		Description = LocalizedStrings.TickUpCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int? UpTicks { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TickDownKey,
		Description = LocalizedStrings.TickDownCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int? DownTicks { get; set; }

	private CandleStates _state;

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StateKey,
		Description = LocalizedStrings.CandleStateKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public CandleStates State
	{
		get => _state;
		set
		{
			ThrowIfFinished();
			_state = value;
		}
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceLevelsKey,
		Description = LocalizedStrings.PriceLevelsKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OIKey,
		Description = LocalizedStrings.OpenInterestDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? OpenInterest { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <inheritdoc />
	public abstract Type ArgType { get; }

	/// <inheritdoc />
	public override string ToString()
	{
		return "{0:HH:mm:ss} {1} (O:{2}, H:{3}, L:{4}, C:{5}, V:{6})"
			.Put(OpenTime, GetType().Name + "_" + Security + "_" + Arg, OpenPrice, HighPrice, LowPrice, ClosePrice, TotalVolume);
	}

	private void ThrowIfFinished()
	{
		if (State == CandleStates.Finished)
			throw new InvalidOperationException(LocalizedStrings.CannotChangeFormedCandle);
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <typeparam name="TCandle">The candle type.</typeparam>
	/// <param name="destination">The object, to which copied information.</param>
	/// <returns>The object, to which copied information.</returns>
	protected TCandle CopyTo<TCandle>(TCandle destination)
		where TCandle : Candle
	{
		destination.Arg = Arg;
		destination.ClosePrice = ClosePrice;
		destination.CloseTime = CloseTime;
		destination.CloseVolume = CloseVolume;
		destination.DownTicks = DownTicks;
		destination.HighPrice = HighPrice;
		destination.HighTime = HighTime;
		destination.HighVolume = HighVolume;
		destination.LowPrice = LowPrice;
		destination.LowTime = LowTime;
		destination.LowVolume = LowVolume;
		destination.OpenInterest = OpenInterest;
		destination.OpenPrice = OpenPrice;
		destination.OpenTime = OpenTime;
		destination.OpenVolume = OpenVolume;
		destination.RelativeVolume = RelativeVolume;
		destination.Security = Security;
		//destination.Series = Series;
		//destination.Source = Source;
		//destination.State = State;
		destination.TotalPrice = TotalPrice;
		destination.TotalTicks = TotalTicks;
		destination.TotalVolume = TotalVolume;
		destination.BuyVolume = BuyVolume;
		destination.SellVolume = SellVolume;
		//destination.VolumeProfileInfo = VolumeProfileInfo;
		destination.PriceLevels = PriceLevels?./*Select(l => l.Clone()).*/ToArray();
		destination.SeqNum = SeqNum;
		destination.BuildFrom = BuildFrom;

		return destination;
	}
}

/// <summary>
/// Base candle class (contains main parameters).
/// </summary>
/// <typeparam name="TArg"></typeparam>
[Obsolete("Use ICandleMessage.")]
public abstract class Candle<TArg> : Candle
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Candle"/>.
	/// </summary>
	protected Candle()
	{
	}

	private TArg _typedArg;

	/// <summary>
	/// Arg.
	/// </summary>
	public TArg TypedArg
	{
		get => _typedArg;
		set
		{
			Validate(value);
			_typedArg = value;
		}
	}

	/// <summary>
	/// Validate value.
	/// </summary>
	/// <param name="value">Value.</param>
	protected virtual void Validate(TArg value) { }

	/// <inheritdoc />
	public override object Arg
	{
		get => TypedArg;
		set => TypedArg = (TArg)value;
	}

	/// <inheritdoc />
	public override Type ArgType => typeof(TArg);
}

/// <summary>
/// Time-frame candle.
/// </summary>
[DataContract]
[Serializable]
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeFrameCandleKey)]
[Obsolete("Use TimeFrameCandleMessage.")]
public class TimeFrameCandle : Candle<TimeSpan>, ITimeFrameCandleMessage
{
	/// <summary>
	/// Time-frame.
	/// </summary>
	[DataMember]
	public TimeSpan TimeFrame
	{
		get => TypedArg;
		set => TypedArg = value;
	}

	/// <inheritdoc />
	protected override void Validate(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(value));
	}

	/// <summary>
	/// Create a copy of <see cref="TimeFrameCandle"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Candle Clone()
	{
		return CopyTo(new TimeFrameCandle());
	}
}