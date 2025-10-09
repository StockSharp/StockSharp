namespace StockSharp.Messages;

/// <summary>
/// Build modes.
/// </summary>
[DataContract]
[Serializable]
public enum MarketDataBuildModes
{
	/// <summary>
	/// Request built data and build the missing data from trades, depths etc.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadAndBuildKey)]
	LoadAndBuild,

	/// <summary>
	/// Request only built data.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadKey)]
	Load,

	/// <summary>
	/// Build from trades, depths etc.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BuildKey)]
	Build
}

/// <summary>
/// Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
/// </summary>
[DataContract]
[Serializable]
public class MarketDataMessage : SecurityMessage, ISubscriptionMessage, IGeneratedMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FromKey,
		Description = LocalizedStrings.StartDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? From { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UntilKey,
		Description = LocalizedStrings.ToDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? To { get; set; }

	/// <summary>
	/// Market data fields, which will be received with subscribed to <see cref="DataType.Level1"/> messages.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarketDataFieldsKey,
		Description = LocalizedStrings.MarketDataFieldsDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public IEnumerable<Level1Fields> Fields { get; set; }

	DataType IDataTypeMessage.DataType => DataType2;

	private DataType _dataType2 = DataType.Level1;

	/// <summary>
	/// Market data type.
	/// </summary>
	[DataMember]
	public DataType DataType2
	{
		get => _dataType2;
		set => _dataType2 = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	[DataMember]
	public bool IsSubscribe { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long TransactionId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Skip { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Count { get; set; }

	/// <summary>
	/// Max depth of requested order book. Uses in case <see cref="DataType2"/> = <see cref="DataType.MarketDepth"/>.
	/// </summary>
	[DataMember]
	public int? MaxDepth { get; set; }

	/// <summary>
	/// News id. Uses in case of request news text.
	/// </summary>
	[DataMember]
	public string NewsId { get; set; }

	/// <summary>
	/// To perform the calculation <see cref="CandleMessage.PriceLevels"/>. By default, it is disabled.
	/// </summary>
	[DataMember]
	public bool IsCalcVolumeProfile { get; set; }

	/// <summary>
	/// Build mode.
	/// </summary>
	[DataMember]
	public MarketDataBuildModes BuildMode { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <summary>
	/// Extra info for the <see cref="BuildFrom"/>.
	/// </summary>
	[DataMember]
	public Level1Fields? BuildField { get; set; }

	/// <summary>
	/// Allow build candles from smaller timeframe.
	/// </summary>
	/// <remarks>
	/// Available only for <see cref="TimeFrameCandleMessage"/>.
	/// </remarks>
	[DataMember]
	public bool AllowBuildFromSmallerTimeFrame { get; set; } = true;

	/// <summary>
	/// Use only the regular trading hours for which data will be requested.
	/// </summary>
	[DataMember]
	public bool? IsRegularTradingHours { get; set; }

	/// <summary>
	/// Request <see cref="CandleStates.Finished"/> only candles.
	/// </summary>
	[DataMember]
	public bool IsFinishedOnly { get; set; }

	/// <summary>
	/// Board code.
	/// </summary>
	[DataMember]
	public string BoardCode { get; set; }

	/// <summary>
	/// Interval for data refresh.
	/// </summary>
	[DataMember]
	public TimeSpan? RefreshSpeed { get; set; }

	/// <summary>
	/// Order log to market depth builder.
	/// </summary>
	public IOrderLogMarketDepthBuilder DepthBuilder { get; set; }

	/// <inheritdoc />
	[DataMember]
	public FillGapsDays? FillGaps { get; set; }

	/// <summary>
	/// Pass through incremental <see cref="QuoteChangeMessage"/>.
	/// </summary>
	[DataMember]
	public bool DoNotBuildOrderBookIncrement { get; set; }

	bool ISubscriptionMessage.FilterEnabled => false;
	bool ISubscriptionMessage.SpecificItemRequest => false;

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDataMessage"/>.
	/// </summary>
	public MarketDataMessage()
		: base(MessageTypes.MarketData)
	{
	}

	/// <summary>
	/// Initialize <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected MarketDataMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new MarketDataMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(MarketDataMessage destination)
	{
		base.CopyTo(destination);

		destination.DataType2 = DataType2.TypedClone();
		destination.From = From;
		destination.To = To;
		destination.IsSubscribe = IsSubscribe;
		destination.TransactionId = TransactionId;
		destination.Skip = Skip;
		destination.Count = Count;
		destination.MaxDepth = MaxDepth;
		destination.NewsId = NewsId;
		destination.BuildMode = BuildMode;
		destination.BuildFrom = BuildFrom?.TypedClone();
		destination.BuildField = BuildField;
		destination.IsCalcVolumeProfile = IsCalcVolumeProfile;
		destination.AllowBuildFromSmallerTimeFrame = AllowBuildFromSmallerTimeFrame;
		destination.IsRegularTradingHours = IsRegularTradingHours;
		destination.IsFinishedOnly = IsFinishedOnly;
		destination.BoardCode = BoardCode;
		destination.RefreshSpeed = RefreshSpeed;
		destination.DepthBuilder = DepthBuilder;
		destination.FillGaps = FillGaps;
		destination.DoNotBuildOrderBookIncrement = DoNotBuildOrderBookIncrement;
		destination.Fields = Fields?.ToArray();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",DataType={DataType2},IsSubscribe={IsSubscribe}";

		if (TransactionId != default)
			str += $",TransId={TransactionId}";

		if (OriginalTransactionId != default)
			str += $",OrigId={OriginalTransactionId}";

		if (MaxDepth != default)
			str += $",MaxDepth={MaxDepth}";

		if (Skip != default)
			str += $",Skip={Skip}";

		if (Count != default)
			str += $",Cnt={Count}";

		if (From != default)
			str += $",From={From}";

		if (To != default)
			str += $",To={To}";

		if (BuildMode == MarketDataBuildModes.Build)
			str += $",Build={BuildMode}/{BuildFrom}/{BuildField}";

		if (AllowBuildFromSmallerTimeFrame)
			str += $",SmallTF={AllowBuildFromSmallerTimeFrame}";

		if (IsRegularTradingHours is bool rth)
			str += $",RTH={rth}";

		if (IsFinishedOnly)
			str += $",FinOnly={IsFinishedOnly}";

		if (IsCalcVolumeProfile)
			str += $",Profile={IsCalcVolumeProfile}";

		if (!BoardCode.IsEmpty())
			str += $",BoardCode={BoardCode}";

		if (RefreshSpeed != null)
			str += $",Speed={RefreshSpeed}";

		if (FillGaps is not null)
			str += $",gaps={FillGaps}";

		if (DoNotBuildOrderBookIncrement)
			str += $",NotBuildInc={DoNotBuildOrderBookIncrement}";

		if (Fields is not null)
			str += $",Fields={Fields.Select(f => f.GetDisplayName()).JoinCommaSpace()}";

		return str;
	}
}