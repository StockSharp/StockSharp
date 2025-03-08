namespace StockSharp.Messages;

/// <summary>
/// Candle states.
/// </summary>
[DataContract]
[Serializable]
public enum CandleStates
{
	/// <summary>
	/// Empty state (candle doesn't exist).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoneKey)]
	None,

	/// <summary>
	/// Candle active.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ActiveKey)]
	Active,

	/// <summary>
	/// Candle finished.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FinishedKey)]
	Finished,
}

/// <summary>
/// The message contains information about the candle.
/// </summary>
/// <remarks>
/// Initialize <see cref="CandleMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class CandleMessage(MessageTypes type) : Message(type), ISubscriptionIdMessage, ICandleMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityIdKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public SecurityId SecurityId { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleOpenTimeKey,
		Description = LocalizedStrings.CandleOpenTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public DateTimeOffset OpenTime { get; set; }

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
		Name = LocalizedStrings.CandleCloseTimeKey,
		Description = LocalizedStrings.CandleCloseTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset CloseTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenPriceKey,
		Description = LocalizedStrings.CandleOpenPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public decimal OpenPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestPriceKey,
		Description = LocalizedStrings.HighPriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public decimal HighPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowestPriceKey,
		Description = LocalizedStrings.LowPriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public decimal LowPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClosingPriceKey,
		Description = LocalizedStrings.ClosePriceOfCandleKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public decimal ClosePrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	//[Nullable]
	public decimal? OpenVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	//[Nullable]
	public decimal? CloseVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	//[Nullable]
	public decimal? HighVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	//[Nullable]
	public decimal? LowVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	public decimal? RelativeVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TotalPriceKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal TotalPrice { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.TotalCandleVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[BasicSetting]
	public decimal TotalVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BuyVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? BuyVolume { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SellVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal? SellVolume { get; set; }

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

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StateKey,
		Description = LocalizedStrings.CandleStateKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public CandleStates State { get; set; }

	/// <inheritdoc />
	[DataMember]
	[XmlIgnore]
	public IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

	/// <inheritdoc />
	public abstract object Arg { get; }

	/// <summary>
	/// <see cref="DataType.Arg"/> type.
	/// </summary>
	public abstract Type ArgType { get; }

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
					_dataType = DataType.Create(GetType(), arg);
			}

			return _dataType;
		}
		set => _dataType = value;
	}

	/// <inheritdoc />
	[DataMember]
	public long OriginalTransactionId { get; set; }

	/// <inheritdoc />
	[XmlIgnore]
	public long SubscriptionId { get; set; }

	/// <inheritdoc />
	[XmlIgnore]
	public long[] SubscriptionIds { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <summary>
	/// Copy parameters.
	/// </summary>
	/// <param name="copy">Copy.</param>
	/// <returns>Copy.</returns>
	protected CandleMessage CopyTo(CandleMessage copy)
	{
		base.CopyTo(copy);

		copy.OriginalTransactionId = OriginalTransactionId;
		copy.SubscriptionId = SubscriptionId;
		copy.SubscriptionIds = SubscriptionIds;//?.ToArray();
		copy.DataType = DataType;

		copy.OpenPrice = OpenPrice;
		copy.OpenTime = OpenTime;
		copy.OpenVolume = OpenVolume;
		copy.ClosePrice = ClosePrice;
		copy.CloseTime = CloseTime;
		copy.CloseVolume = CloseVolume;
		copy.HighPrice = HighPrice;
		copy.HighVolume = HighVolume;
		copy.HighTime = HighTime;
		copy.LowPrice = LowPrice;
		copy.LowVolume = LowVolume;
		copy.LowTime = LowTime;
		copy.OpenInterest = OpenInterest;
		copy.SecurityId = SecurityId;
		copy.TotalVolume = TotalVolume;
		copy.BuyVolume = BuyVolume;
		copy.SellVolume = SellVolume;
		copy.RelativeVolume = RelativeVolume;
		copy.DownTicks = DownTicks;
		copy.UpTicks = UpTicks;
		copy.TotalTicks = TotalTicks;
		copy.PriceLevels = PriceLevels?/*.Select(l => l.Clone())*/.ToArray();
		copy.State = State;
		copy.BuildFrom = BuildFrom;
		copy.SeqNum = SeqNum;

		return copy;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = $"{Type},Sec={SecurityId},A={DataType?.Arg},T={OpenTime:yyyy/MM/dd HH:mm:ss.fff},O={OpenPrice},H={HighPrice},L={LowPrice},C={ClosePrice},V={TotalVolume},S={State},TransId={OriginalTransactionId}";

		if (SeqNum != default)
			str += $",SQ={SeqNum}";

		return str;
	}

	DateTimeOffset IServerTimeMessage.ServerTime
	{
		get => OpenTime;
		set => OpenTime = value;
	}
}

/// <summary>
/// Typed <see cref="CandleMessage"/>.
/// </summary>
/// <typeparam name="TArg"><see cref="TypedArg"/> type.</typeparam>
/// <remarks>
/// Initialize <see cref="CandleMessage"/>.
/// </remarks>
/// <param name="type">Message type.</param>
/// <param name="arg"><see cref="TypedArg"/>.</param>
[DataContract]
[Serializable]
public abstract class TypedCandleMessage<TArg>(MessageTypes type, TArg arg) : CandleMessage(type)
{

	/// <inheritdoc />
	public override object Arg => TypedArg;

	/// <inheritdoc />
	public override Type ArgType => typeof(TArg);

	/// <summary>
	/// Candle arg.
	/// </summary>
	public TArg TypedArg { get; set; } = arg;

	/// <summary>
	/// Copy to candle.
	/// </summary>
	protected TypedCandleMessage<TArg> CopyTo(TypedCandleMessage<TArg> copy)
	{
		base.CopyTo(copy);

		if(typeof(TArg).IsValueType)
			copy.TypedArg = TypedArg;
		else if(TypedArg is ICloneable cloneable)
			copy.TypedArg = (TArg)cloneable.Clone();
		else
			throw new InvalidOperationException($"unable to copy {nameof(TypedArg)}");

		return copy;
	}
}

/// <summary>
/// The message contains information about the time-frame candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TFKey,
	Description = LocalizedStrings.TimeFrameCandleKey
)]
public class TimeFrameCandleMessage : TypedCandleMessage<TimeSpan>, ITimeFrameCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	public TimeFrameCandleMessage()
		: this(MessageTypes.CandleTimeFrame)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected TimeFrameCandleMessage(MessageTypes type)
		: base(type, default)
	{
	}

	/// <summary>
	/// Time frame.
	/// </summary>
	public TimeSpan TimeFrame => TypedArg;

	/// <summary>
	/// Create a copy of <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new TimeFrameCandleMessage());
}

/// <summary>
/// The message contains information about the tick candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TickKey,
	Description = LocalizedStrings.TickCandleKey
)]
public class TickCandleMessage : TypedCandleMessage<int>, ITickCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TickCandleMessage"/>.
	/// </summary>
	public TickCandleMessage()
		: base(MessageTypes.CandleTick, default)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="TickCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new TickCandleMessage());
}

/// <summary>
/// The message contains information about the volume candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VolumeKey,
	Description = LocalizedStrings.VolumeCandleKey
)]
public class VolumeCandleMessage : TypedCandleMessage<decimal>, IVolumeCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeCandleMessage"/>.
	/// </summary>
	public VolumeCandleMessage()
		: base(MessageTypes.CandleVolume, default)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="VolumeCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new VolumeCandleMessage());
}

/// <summary>
/// The message contains information about the range candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RangeKey,
	Description = LocalizedStrings.RangeCandleKey
)]
public class RangeCandleMessage : TypedCandleMessage<Unit>, IRangeCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RangeCandleMessage"/>.
	/// </summary>
	public RangeCandleMessage()
		: base(MessageTypes.CandleRange, new())
	{
	}

	/// <summary>
	/// Create a copy of <see cref="RangeCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new RangeCandleMessage());
}

/// <summary>
/// Point in figure (X0) candle arg.
/// </summary>
[DataContract]
[Serializable]
public class PnFArg : Equatable<PnFArg>
{
	private Unit _boxSize = new(1);

	/// <summary>
	/// Range of price above which increase the candle body.
	/// </summary>
	[DataMember]
	public Unit BoxSize
	{
		get => _boxSize;
		set => _boxSize = value ?? throw new ArgumentNullException(nameof(value));
	}

	private int _reversalAmount = 1;

	/// <summary>
	/// The number of boxes required to cause a reversal.
	/// </summary>
	[DataMember]
	public int ReversalAmount
	{
		get => _reversalAmount;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_reversalAmount = value;
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Box = {BoxSize} RA = {ReversalAmount}";
	}

	/// <summary>
	/// Create a copy of <see cref="PnFArg"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override PnFArg Clone()
	{
		return new PnFArg
		{
			BoxSize = BoxSize.Clone(),
			ReversalAmount = ReversalAmount,
		};
	}

	/// <summary>
	/// Compare <see cref="PnFArg"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(PnFArg other)
	{
		return other.BoxSize == BoxSize && other.ReversalAmount == ReversalAmount;
	}

	/// <summary>
	/// Get the hash code of the object <see cref="PnFArg"/>.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return BoxSize.GetHashCode() ^ ReversalAmount.GetHashCode();
	}
}

/// <summary>
/// The message contains information about the X0 candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PnFKey,
	Description = LocalizedStrings.PnFCandleKey
)]
public class PnFCandleMessage : TypedCandleMessage<PnFArg>, IPnFCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PnFCandleMessage"/>.
	/// </summary>
	public PnFCandleMessage()
		: base(MessageTypes.CandlePnF, new())
	{
	}

	/// <summary>
	/// Create a copy of <see cref="PnFCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new PnFCandleMessage());
}

/// <summary>
/// The message contains information about the renko candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RenkoKey,
	Description = LocalizedStrings.RenkoCandleKey
)]
public class RenkoCandleMessage : TypedCandleMessage<Unit>, IRenkoCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RenkoCandleMessage"/>.
	/// </summary>
	public RenkoCandleMessage()
		: base(MessageTypes.CandleRenko, new())
	{
	}

	/// <summary>
	/// Create a copy of <see cref="RenkoCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new RenkoCandleMessage());
}

/// <summary>
/// The message contains information about the Heikin-Ashi candle.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HeikinAshiKey,
	Description = LocalizedStrings.HeikinAshiKey
)]
public class HeikinAshiCandleMessage : TimeFrameCandleMessage, IHeikinAshiCandleMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HeikinAshiCandleMessage"/>.
	/// </summary>
	public HeikinAshiCandleMessage()
		: base(MessageTypes.CandleHeikinAshi)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="HeikinAshiCandleMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => CopyTo(new HeikinAshiCandleMessage());
}
