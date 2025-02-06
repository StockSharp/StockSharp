namespace StockSharp.BusinessEntities;

using System.Runtime.CompilerServices;

/// <summary>
/// Security (shares, futures, options etc.).
/// </summary>
[Serializable]
[DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SecurityKey,
	Description = LocalizedStrings.SecurityDescKey)]
public class Security : Cloneable<Security>, INotifyPropertyChanged
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Security"/>.
	/// </summary>
	public Security()
	{
	}

	private string _id;

	/// <summary>
	/// Security ID.
	/// </summary>
	[DataMember]
	[ReadOnly(true)]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdentifierKey,
		Description = LocalizedStrings.SecurityIdKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	[BasicSetting]
	public string Id
	{
		get => _id;
		set
		{
			if (_id == value)
				return;

			_id = value;
			Notify();
		}
	}

	private string _code;

	/// <summary>
	/// Security code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CodeKey,
		Description = LocalizedStrings.SecCodeKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	[Required(AllowEmptyStrings = false)]
	[BasicSetting]
	public virtual string Code
	{
		get => _code;
		set
		{
			if (_code == value)
				return;

			_code = value;
			Notify();
		}
	}

	private ExchangeBoard _board;

	/// <summary>
	/// Exchange board where the security is traded.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.ExchangeBoardDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	[Required]
	[BasicSetting]
	public virtual ExchangeBoard Board
	{
		get => _board;
		set
		{
			if (_board == value)
				return;

			_board = value;
			Notify();
		}
	}

	private SecurityTypes? _type;

	/// <summary>
	/// Security type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.SecurityTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	[BasicSetting]
	public virtual SecurityTypes? Type
	{
		get => _type;
		set
		{
			if (_type == value)
				return;

			_type = value;
			Notify();
		}
	}

	private string _name;

	/// <summary>
	/// Security name.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.SecurityNameKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public string Name
	{
		get => _name;
		set
		{
			if (_name == value)
				return;

			_name = value;
			Notify();
		}
	}

	private string _shortName;

	/// <summary>
	/// Short security name.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortNameKey,
		Description = LocalizedStrings.ShortNameDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	public string ShortName
	{
		get => _shortName;
		set
		{
			if (_shortName == value)
				return;

			_shortName = value;
			Notify();
		}
	}

	private CurrencyTypes? _currency;

	/// <summary>
	/// Trading security currency.
	/// </summary>
	[DataMember]
	//[Nullable]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	[EditorExtension(AutoComplete = true, Sorted = true)]
	public CurrencyTypes? Currency
	{
		get => _currency;
		set
		{
			_currency = value;
			Notify();
		}
	}

	private SecurityExternalId _externalId = new();

	/// <summary>
	/// Security ID in other systems.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExternalIdKey,
		Description = LocalizedStrings.ExternalIdDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public SecurityExternalId ExternalId
	{
		get => _externalId;
		set
		{
			_externalId = value ?? throw new ArgumentNullException(nameof(value));
			Notify();
		}
	}

	private string _class;

	/// <summary>
	/// Security class.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClassKey,
		Description = LocalizedStrings.SecurityClassKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 8)]
	public string Class
	{
		get => _class;
		set
		{
			if (_class == value)
				return;

			_class = value;
			Notify();
		}
	}

	private decimal? _priceStep;

	/// <summary>
	/// Minimum price step.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceStepKey,
		Description = LocalizedStrings.MinPriceStepKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	//[Nullable]
	[DecimalNullOrMoreZero]
	[BasicSetting]
	public decimal? PriceStep
	{
		get => _priceStep;
		set
		{
			if (_priceStep == value)
				return;

			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_priceStep = value;
			Notify();
		}
	}

	private decimal? _volumeStep;

	/// <summary>
	/// Minimum volume step.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeStepKey,
		Description = LocalizedStrings.MinVolStepKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	[DecimalNullOrMoreZero]
	[BasicSetting]
	public decimal? VolumeStep
	{
		get => _volumeStep;
		set
		{
			if (_volumeStep == value)
				return;

			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_volumeStep = value;
			Notify();
		}
	}

	private decimal? _minVolume;

	/// <summary>
	/// Minimum volume allowed in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MinVolumeKey,
		Description = LocalizedStrings.MinVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	//[GreaterThanZero]
	[BasicSetting]
	public decimal? MinVolume
	{
		get => _minVolume;
		set
		{
			if (_minVolume == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_minVolume = value;
			Notify();
		}
	}

	private decimal? _maxVolume;

	/// <summary>
	/// Maximum volume allowed in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MaxVolumeKey,
		Description = LocalizedStrings.MaxVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 11)]
	//[GreaterThanZero]
	public decimal? MaxVolume
	{
		get => _maxVolume;
		set
		{
			if (_maxVolume == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_maxVolume = value;
			Notify();
		}
	}

	private decimal? _multiplier;

	/// <summary>
	/// Lot multiplier.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LotKey,
		Description = LocalizedStrings.LotVolumeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 12)]
	[BasicSetting]
	public decimal? Multiplier
	{
		get => _multiplier;
		set
		{
			if (_multiplier == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_multiplier = value;
			Notify();
		}
	}

	private int? _decimals;

	/// <summary>
	/// Number of digits in price after coma.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DecimalsKey,
		Description = LocalizedStrings.DecimalsDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 13)]
	[DecimalNullOrNotNegative]
	[BasicSetting]
	public int? Decimals
	{
		get => _decimals;
		set
		{
			if (_decimals == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_decimals = value;
			Notify();
		}
	}

	private DateTimeOffset? _expiryDate;

	/// <summary>
	/// Security expiration date (for derivatives - expiration, for bonds — redemption).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpiryDateKey,
		Description = LocalizedStrings.ExpiryDateDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 14)]
	[BasicSetting]
	public DateTimeOffset? ExpiryDate
	{
		get => _expiryDate;
		set
		{
			if (_expiryDate == value)
				return;

			_expiryDate = value;
			Notify();
		}
	}

	private DateTimeOffset? _settlementDate;

	/// <summary>
	/// Settlement date for security (for derivatives and bonds).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettlementDateKey,
		Description = LocalizedStrings.SettlementDateForSecurityKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 15)]
	[BasicSetting]
	public DateTimeOffset? SettlementDate
	{
		get => _settlementDate;
		set
		{
			if (_settlementDate == value)
				return;

			_settlementDate = value;
			Notify();
		}
	}

	private string _cfiCode;

	/// <summary>
	/// Type in ISO 10962 standard.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CfiCodeKey,
		Description = LocalizedStrings.CfiCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 16)]
	public string CfiCode
	{
		get => _cfiCode;
		set
		{
			if (_cfiCode == value)
				return;

			_cfiCode = value;
			Notify();
		}
	}

	private decimal? _faceValue;

	/// <summary>
	/// Face value.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FaceValueKey,
		Description = LocalizedStrings.FaceValueDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 17)]
	public decimal? FaceValue
	{
		get => _faceValue;
		set
		{
			if (_faceValue == value)
				return;

			_faceValue = value;
			Notify();
		}
	}

	private SettlementTypes? _settlementType;

	/// <summary>
	/// <see cref="SettlementTypes"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettlementKey,
		Description = LocalizedStrings.SettlementTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 18)]
	public SettlementTypes? SettlementType
	{
		get => _settlementType;
		set
		{
			if (_settlementType == value)
				return;

			_settlementType = value;
			Notify();
		}
	}

	private OptionStyles? _optionStyle;

	/// <summary>
	/// <see cref="OptionStyles"/>.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OptionStyleKey,
		Description = LocalizedStrings.OptionStyleDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 19)]
	public OptionStyles? OptionStyle
	{
		get => _optionStyle;
		set
		{
			if (_optionStyle == value)
				return;

			_optionStyle = value;
			Notify();
		}
	}

	private string _primaryId;

	/// <summary>
	/// Identifier on primary exchange.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PrimaryIdKey,
		Description = LocalizedStrings.PrimaryIdDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 20)]
	public string PrimaryId
	{
		get => _primaryId;
		set
		{
			if (_primaryId == value)
				return;

			_primaryId = value;
			Notify();
		}
	}

	private decimal? _stepPrice;

	//[DataMember]
	/// <summary>
	/// Step price.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StepPriceKey,
		Description = LocalizedStrings.StepPriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 200)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? StepPrice
	{
		get => _stepPrice;
		set
		{
			if (_stepPrice == value)
				return;

			_stepPrice = value;
			Notify();
		}
	}

	private ITickTradeMessage _lastTick;

	/// <summary>
	/// Information about the last trade. If during the session on the instrument there were no trades, the value equals to <see langword="null" />.
	/// </summary>
	[XmlIgnore]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LastTradeKey,
		Description = LocalizedStrings.LastTradeDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 201)]
	[Browsable(false)]
	public ITickTradeMessage LastTick
	{
		get => _lastTick;
		set
		{
			if (_lastTick == value)
				return;

			_lastTick = value;
			Notify();

#pragma warning disable CS0618 // Type or member is obsolete
			Notify(nameof(LastTrade));
#pragma warning restore CS0618 // Type or member is obsolete

			if (value == null)
				return;

			if (value.ServerTime != default)
				LastChangeTime = value.ServerTime;
		}
	}

	/// <summary>
	/// Information about the last trade. If during the session on the instrument there were no trades, the value equals to <see langword="null" />.
	/// </summary>
	[XmlIgnore]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LastTradeKey,
		Description = LocalizedStrings.LastTradeDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 201)]
	[Browsable(false)]
	[Obsolete("Use LastTick property.")]
	public Trade LastTrade => LastTick is ExecutionMessage execMsg ? execMsg.ToTrade(this) : (Trade)LastTick;

	private decimal? _openPrice;

	//[DataMember]
	/// <summary>
	/// First trade price for the session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FirstTradePriceKey,
		Description = LocalizedStrings.FirstTradePriceForSessionKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 202)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? OpenPrice
	{
		get => _openPrice;
		set
		{
			if (_openPrice == value)
				return;

			_openPrice = value;
			Notify();
		}
	}

	private decimal? _closePrice;

	//[DataMember]
	/// <summary>
	/// Last trade price for the previous session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LastTradePriceKey,
		Description = LocalizedStrings.LastTradePriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 203)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? ClosePrice
	{
		get => _closePrice;
		set
		{
			if (_closePrice == value)
				return;

			_closePrice = value;
			Notify();
		}
	}

	private decimal? _lowPrice;

	//[DataMember]
	/// <summary>
	/// Lowest price for the session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowPriceKey,
		Description = LocalizedStrings.LowPriceForSessionKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 204)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? LowPrice
	{
		get => _lowPrice;
		set
		{
			if (_lowPrice == value)
				return;

			_lowPrice = value;
			Notify();
		}
	}

	private decimal? _highPrice;

	//[DataMember]
	/// <summary>
	/// Highest price for the session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestPriceKey,
		Description = LocalizedStrings.HighestPriceForSessionKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 205)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? HighPrice
	{
		get => _highPrice;
		set
		{
			if (_highPrice == value)
				return;

			_highPrice = value;
			Notify();
		}
	}

	private QuoteChange? _bestBid;

	//[DataMember]
	/// <summary>
	/// Best bid in market depth.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BestBidKey,
		Description = LocalizedStrings.BestBidDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 206)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public QuoteChange? BestBid
	{
		get => _bestBid;
		set
		{
			//TODO: решить другим методом, OnEquals не тормозит, медленно работает GUI
			//PYH: Тормозит OnEquals

			//if (_bestBid == value)
			//	return;

			_bestBid = value;
			Notify();
		}
	}

	private QuoteChange? _bestAsk;

	//[DataMember]
	/// <summary>
	/// Best ask in market depth.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BestAskKey,
		Description = LocalizedStrings.BestAskDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 207)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public QuoteChange? BestAsk
	{
		get => _bestAsk;
		set
		{
			// if (_bestAsk == value)
			//	return;

			_bestAsk = value;
			Notify();
		}
	}

	//[DisplayName("Лучшая пара")]
	//[Description("Лучшая пара котировок.")]
	//[ExpandableObject]
	//[StatisticsCategory]
	/// <summary>
	/// Best pair quotes.
	/// </summary>
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BestPairKey,
		Description = LocalizedStrings.BestPairKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 208)]
	public MarketDepthPair BestPair => new(BestBid, BestAsk);

	private SecurityStates? _state;

	//[DataMember]
	//[Enum]
	/// <summary>
	/// Current state of security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StateKey,
		Description = LocalizedStrings.SecurityStateKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 209)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public SecurityStates? State
	{
		get => _state;
		set
		{
			if (_state == value)
				return;

			_state = value;
			Notify();
		}
	}

	private decimal? _minPrice;

	//[DataMember]
	/// <summary>
	/// Lower price limit.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceMinKey,
		Description = LocalizedStrings.PriceMinLimitKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 210)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? MinPrice
	{
		get => _minPrice;
		set
		{
			if (_minPrice == value)
				return;

			_minPrice = value;
			Notify();
		}
	}

	private decimal? _maxPrice;

	//[DataMember]
	/// <summary>
	/// Upper price limit.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceMaxKey,
		Description = LocalizedStrings.PriceMaxLimitKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 211)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? MaxPrice
	{
		get => _maxPrice;
		set
		{
			if (_maxPrice == value)
				return;

			_maxPrice = value;
			Notify();
		}
	}

	private decimal? _marginBuy;

	//[DataMember]
	/// <summary>
	/// Initial margin to buy.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarginBuyKey,
		Description = LocalizedStrings.MarginBuyDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 212)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? MarginBuy
	{
		get => _marginBuy;
		set
		{
			if (_marginBuy == value)
				return;

			_marginBuy = value;
			Notify();
		}
	}

	private decimal? _marginSell;

	//[DataMember]
	/// <summary>
	/// Initial margin to sell.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarginSellKey,
		Description = LocalizedStrings.MarginSellDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 213)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? MarginSell
	{
		get => _marginSell;
		set
		{
			if (_marginSell == value)
				return;

			_marginSell = value;
			Notify();
		}
	}

	private string _underlyingSecurityId;

	/// <summary>
	/// Underlying asset on which the current security is built.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnderlyingAssetKey,
		Description = LocalizedStrings.UnderlyingAssetDescKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 100)]
	public string UnderlyingSecurityId
	{
		get => _underlyingSecurityId;
		set
		{
			if (_underlyingSecurityId == value)
				return;

			_underlyingSecurityId = value;
			Notify();
		}
	}

	private OptionTypes? _optionType;

	/// <summary>
	/// Option type.
	/// </summary>
	[DataMember]
	//[Nullable]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OptionTypeKey,
		Description = LocalizedStrings.OptionContractTypeKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 101)]
	public OptionTypes? OptionType
	{
		get => _optionType;
		set
		{
			if (_optionType == value)
				return;

			_optionType = value;
			Notify();
		}
	}

	private decimal? _strike;

	/// <summary>
	/// Option strike price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StrikeKey,
		Description = LocalizedStrings.OptionStrikePriceKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 102)]
	public decimal? Strike
	{
		get => _strike;
		set
		{
			if (_strike == value)
				return;

			//if (value < 0)
			//	throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_strike = value;
			Notify();
		}
	}

	private string _binaryOptionType;

	/// <summary>
	/// Type of binary option.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BinaryKey,
		Description = LocalizedStrings.TypeBinaryOptionKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 103)]
	public string BinaryOptionType
	{
		get => _binaryOptionType;
		set
		{
			if (_binaryOptionType == value)
				return;

			_binaryOptionType = value;
			Notify();
		}
	}

	private decimal? _impliedVolatility;

	//[DataMember]
	/// <summary>
	/// Volatility (implied).
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IVKey,
		Description = LocalizedStrings.ImpliedVolatilityKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 104)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? ImpliedVolatility
	{
		get => _impliedVolatility;
		set
		{
			if (_impliedVolatility == value)
				return;

			_impliedVolatility = value;
			Notify();
		}
	}

	private decimal? _historicalVolatility;

	//[DataMember]
	/// <summary>
	/// Volatility (historical).
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HVKey,
		Description = LocalizedStrings.HistoricalVolatilityKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 105)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? HistoricalVolatility
	{
		get => _historicalVolatility;
		set
		{
			if (_historicalVolatility == value)
				return;

			_historicalVolatility = value;
			Notify();
		}
	}

	private decimal? _theorPrice;

	//[DataMember]
	/// <summary>
	/// Theoretical price.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TheorPriceKey,
		Description = LocalizedStrings.TheoreticalPriceKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 106)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? TheorPrice
	{
		get => _theorPrice;
		set
		{
			if (_theorPrice == value)
				return;

			_theorPrice = value;
			Notify();
		}
	}

	private decimal? _delta;

	//[DataMember]
	/// <summary>
	/// Option delta.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DeltaKey,
		Description = LocalizedStrings.OptionDeltaKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 107)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Delta
	{
		get => _delta;
		set
		{
			if (_delta == value)
				return;

			_delta = value;
			Notify();
		}
	}

	private decimal? _gamma;

	//[DataMember]
	/// <summary>
	/// Option gamma.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.GammaKey,
		Description = LocalizedStrings.OptionGammaKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 108)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Gamma
	{
		get => _gamma;
		set
		{
			if (_gamma == value)
				return;

			_gamma = value;
			Notify();
		}
	}

	private decimal? _vega;

	//[DataMember]
	/// <summary>
	/// Option vega.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VegaKey,
		Description = LocalizedStrings.OptionVegaKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 109)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Vega
	{
		get => _vega;
		set
		{
			if (_vega == value)
				return;

			_vega = value;
			Notify();
		}
	}

	private decimal? _theta;

	//[DataMember]
	/// <summary>
	/// Option theta.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ThetaKey,
		Description = LocalizedStrings.OptionThetaKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 110)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Theta
	{
		get => _theta;
		set
		{
			if (_theta == value)
				return;

			_theta = value;
			Notify();
		}
	}

	private decimal? _rho;

	//[DataMember]
	/// <summary>
	/// Option rho.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RhoKey,
		Description = LocalizedStrings.OptionRhoKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 111)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Rho
	{
		get => _rho;
		set
		{
			if (_rho == value)
				return;

			_rho = value;
			Notify();
		}
	}

	private decimal? _openInterest;

	//[DataMember]
	/// <summary>
	/// Number of open positions (open interest).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenInterestKey,
		Description = LocalizedStrings.OpenInterestDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 220)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? OpenInterest
	{
		get => _openInterest;
		set
		{
			if (_openInterest == value)
				return;

			_openInterest = value;
			Notify();
		}
	}

	private DateTimeOffset _localTime;

	/// <summary>
	/// Local time of the last instrument change.
	/// </summary>
	[Browsable(false)]
	[XmlIgnore]
	public DateTimeOffset LocalTime
	{
		get => _localTime;
		set
		{
			_localTime = value;
			Notify();
		}
	}

	private DateTimeOffset _lastChangeTime;

	//[StatisticsCategory]
	/// <summary>
	/// Time of the last instrument change.
	/// </summary>
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	[XmlIgnore]
	public DateTimeOffset LastChangeTime
	{
		get => _lastChangeTime;
		set
		{
			_lastChangeTime = value;
			Notify();
		}
	}

	private decimal? _bidsVolume;

	//[DataMember]
	/// <summary>
	/// Total volume in all buy orders.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BidsVolumeKey,
		Description = LocalizedStrings.BidsVolumeDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 221)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? BidsVolume
	{
		get => _bidsVolume;
		set
		{
			_bidsVolume = value;
			Notify();
		}
	}

	private int? _bidsCount;

	//[DataMember]
	/// <summary>
	/// Number of buy orders.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BidsKey,
		Description = LocalizedStrings.BidsCountDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 222)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public int? BidsCount
	{
		get => _bidsCount;
		set
		{
			_bidsCount = value;
			Notify();
		}
	}

	private decimal? _asksVolume;

	//[DataMember]
	/// <summary>
	/// Total volume in all sell orders.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AsksVolumeKey,
		Description = LocalizedStrings.AsksVolumeDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 223)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? AsksVolume
	{
		get => _asksVolume;
		set
		{
			_asksVolume = value;
			Notify();
		}
	}

	private int? _asksCount;

	//[DataMember]
	/// <summary>
	/// Number of sell orders.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AsksKey,
		Description = LocalizedStrings.AsksCountDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 224)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public int? AsksCount
	{
		get => _asksCount;
		set
		{
			_asksCount = value;
			Notify();
		}
	}

	private int? _tradesCount;

	//[DataMember]
	/// <summary>
	/// Number of trades.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradesOfKey,
		Description = LocalizedStrings.LimitOrderTifKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 225)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public int? TradesCount
	{
		get => _tradesCount;
		set
		{
			_tradesCount = value;
			Notify();
		}
	}

	private decimal? _highBidPrice;

	//[DataMember]
	/// <summary>
	/// Maximum bid during the session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BidMaxKey,
		Description = LocalizedStrings.BidMaxDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 226)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? HighBidPrice
	{
		get => _highBidPrice;
		set
		{
			_highBidPrice = value;
			Notify();
		}
	}

	private decimal? _lowAskPrice;

	//[DataMember]
	/// <summary>
	/// Minimum ask during the session.
	/// </summary>
	[XmlIgnore]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AskMinKey,
		Description = LocalizedStrings.AskMinDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 227)]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? LowAskPrice
	{
		get => _lowAskPrice;
		set
		{
			_lowAskPrice = value;
			Notify();
		}
	}

	private decimal? _yield;

	//[DataMember]
	/// <summary>
	/// Yield.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.YieldKey,
		Description = LocalizedStrings.YieldKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 228)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Yield
	{
		get => _yield;
		set
		{
			_yield = value;
			Notify();
		}
	}

	private decimal? _vwap;

	//[DataMember]
	/// <summary>
	/// Average price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AveragePriceKey,
		Description = LocalizedStrings.AveragePriceKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 229)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? VWAP
	{
		get => _vwap;
		set
		{
			_vwap = value;
			Notify();
		}
	}

	private decimal? _settlementPrice;

	//[DataMember]
	/// <summary>
	/// Settlement price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettlementPriceKey,
		Description = LocalizedStrings.SettlementPriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 230)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? SettlementPrice
	{
		get => _settlementPrice;
		set
		{
			_settlementPrice = value;
			Notify();
		}
	}

	private decimal? _averagePrice;

	//[DataMember]
	/// <summary>
	/// Average price per session.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AveragePriceKey,
		Description = LocalizedStrings.AveragePriceDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 231)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? AveragePrice
	{
		get => _averagePrice;
		set
		{
			_averagePrice = value;
			Notify();
		}
	}

	private decimal? _volume;

	//[DataMember]
	/// <summary>
	/// Volume per session.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.VolumeDescKey,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 232)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Volume
	{
		get => _volume;
		set
		{
			_volume = value;
			Notify();
		}
	}

	private decimal? _turnover;

	/// <summary>
	/// Turnover.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TurnoverKey,
		Description = LocalizedStrings.TurnoverKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.StatisticsKey,
		Order = 232)]
	[XmlIgnore]
	[Browsable(false)]
	//[Obsolete("Use the IConnector.GetSecurityValue.")]
	public decimal? Turnover
	{
		get => _turnover;
		set
		{
			_turnover = value;
			Notify();
		}
	}

	private decimal? _issueSize;

	/// <summary>
	/// Number of issued contracts.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IssueSizeKey,
		Description = LocalizedStrings.IssueSizeKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 21)]
	[DataMember]
	public decimal? IssueSize
	{
		get => _issueSize;
		set
		{
			_issueSize = value;
			Notify();
		}
	}

	private DateTimeOffset? _issueDate;

	/// <summary>
	/// Date of issue.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IssueDateKey,
		Description = LocalizedStrings.IssueDateKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 22)]
	[DataMember]
	public DateTimeOffset? IssueDate
	{
		get => _issueDate;
		set
		{
			_issueDate = value;
			Notify();
		}
	}

	private bool? _shortable;

	/// <summary>
	/// Can have short positions.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortableKey,
		Description = LocalizedStrings.ShortableDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 22)]
	[DataMember]
	public bool? Shortable
	{
		get => _shortable;
		set
		{
			_shortable = value;
			Notify();
		}
	}

	private SecurityTypes? _underlyingSecurityType;

	/// <summary>
	/// Underlying security type.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AssetTypeKey,
		Description = LocalizedStrings.UnderlyingSecurityTypeKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 103)]
	[DataMember]
	public SecurityTypes? UnderlyingSecurityType
	{
		get => _underlyingSecurityType;
		set
		{
			_underlyingSecurityType = value;
			Notify();
		}
	}

	private decimal? _underlyingSecurityMinVolume;

	/// <summary>
	/// Minimum volume allowed in order for underlying security.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnderlyingMinVolumeKey,
		Description = LocalizedStrings.UnderlyingMinVolumeDescKey,
		GroupName = LocalizedStrings.DerivativesKey,
		Order = 104)]
	[DataMember]
	public decimal? UnderlyingSecurityMinVolume
	{
		get => _underlyingSecurityMinVolume;
		set
		{
			_underlyingSecurityMinVolume = value;
			Notify();
		}
	}

	private decimal? _buyBackPrice;

	/// <summary>
	/// BuyBack price.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? BuyBackPrice
	{
		get => _buyBackPrice;
		set
		{
			_buyBackPrice = value;
			Notify();
		}
	}

	private DateTimeOffset? _buyBackDate;

	/// <summary>
	/// BuyBack date.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public DateTimeOffset? BuyBackDate
	{
		get => _buyBackDate;
		set
		{
			_buyBackDate = value;
			Notify();
		}
	}

	/// <summary>
	/// Basket security type. Can be <see langword="null"/> in case of regular security.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CodeKey,
		Description = LocalizedStrings.BasketCodeKey,
		GroupName = LocalizedStrings.BasketKey,
		Order = 200)]
	public virtual string BasketCode { get; set; }

	/// <summary>
	/// Basket security expression. Can be <see langword="null"/> in case of regular security.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpressionKey,
		Description = LocalizedStrings.ExpressionDescKey,
		GroupName = LocalizedStrings.BasketKey,
		Order = 201)]
	public virtual string BasketExpression { get; set; }

	private decimal? _commissionTaker;

	/// <summary>
	/// Commission (taker).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? CommissionTaker
	{
		get => _commissionTaker;
		set
		{
			_commissionTaker = value;
			Notify();
		}
	}

	private decimal? _commissionMaker;

	/// <summary>
	/// Commission (maker).
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public decimal? CommissionMaker
	{
		get => _commissionMaker;
		set
		{
			_commissionMaker = value;
			Notify();
		}
	}

	[field: NonSerialized]
	private PropertyChangedEventHandler _propertyChanged;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	/// <inheritdoc />
	public override string ToString() => Id;

	/// <summary>
	/// Create a copy of <see cref="Security"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Security Clone()
	{
		var clone = new Security();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// To copy fields of the current instrument to <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The instrument in which you should to copy fields.</param>
	public void CopyTo(Security destination)
	{
		if (destination == null)
			throw new ArgumentNullException(nameof(destination));

		destination.Id = Id;
		destination.Name = Name;
		destination.Type = Type;
		destination.Code = Code;
		destination.Class = Class;
		destination.ShortName = ShortName;
		destination.VolumeStep = VolumeStep;
		destination.MinVolume = MinVolume;
		destination.MaxVolume = MaxVolume;
		destination.Multiplier = Multiplier;
		destination.PriceStep = PriceStep;
		destination.Decimals = Decimals;
		destination.SettlementDate = SettlementDate;
		destination.Board = Board;
		destination.ExpiryDate = ExpiryDate;
		destination.OptionType = OptionType;
		destination.Strike = Strike;
		destination.BinaryOptionType = BinaryOptionType;
		destination.UnderlyingSecurityId = UnderlyingSecurityId;
		destination.ExternalId = ExternalId.Clone();
		destination.Currency = Currency;
		destination.StepPrice = StepPrice;
		destination.LowPrice = LowPrice;
		destination.HighPrice = HighPrice;
		destination.ClosePrice = ClosePrice;
		destination.OpenPrice = OpenPrice;
		destination.MinPrice = MinPrice;
		destination.MaxPrice = MaxPrice;
		destination.State = State;
		destination.TheorPrice = TheorPrice;
		destination.ImpliedVolatility = ImpliedVolatility;
		destination.HistoricalVolatility = HistoricalVolatility;
		destination.MarginBuy = MarginBuy;
		destination.MarginSell = MarginSell;
		destination.OpenInterest = OpenInterest;
		destination.BidsCount = BidsCount;
		destination.BidsVolume = BidsVolume;
		destination.AsksCount = AsksCount;
		destination.AsksVolume = AsksVolume;
		destination.CfiCode = CfiCode;
		destination.Turnover = Turnover;
		destination.IssueSize = IssueSize;
		destination.IssueDate = IssueDate;
		destination.UnderlyingSecurityType = UnderlyingSecurityType;
		destination.UnderlyingSecurityMinVolume = UnderlyingSecurityMinVolume;
		destination.BuyBackDate = BuyBackDate;
		destination.BuyBackPrice = BuyBackPrice;
		destination.Shortable = Shortable;
		destination.BasketCode = BasketCode;
		destination.BasketExpression = BasketExpression;
		destination.CommissionTaker = CommissionTaker;
		destination.CommissionMaker = CommissionMaker;
		destination.FaceValue = FaceValue;
		destination.SettlementType = SettlementType;
		destination.OptionStyle = OptionStyle;
		destination.PrimaryId = PrimaryId;

		//if (LastTrade != null)
		//{
		//	destination.LastTrade = LastTrade.Clone();
		//	destination.LastTrade.Security = destination;
		//}

		//if (BestBid != null)
		//{
		//	destination.BestBid = BestBid.Clone();
		//	destination.BestBid.Security = destination;
		//}

		//if (BestAsk != null)
		//{
		//	destination.BestAsk = BestAsk.Clone();
		//	destination.BestAsk.Security = destination;
		//}
	}

	/// <summary>
	/// To call the event <see cref="INotifyPropertyChanged.PropertyChanged"/>.
	/// </summary>
	/// <param name="propName">Property name.</param>
	protected void Notify([CallerMemberName]string propName = null)
	{
		_propertyChanged?.Invoke(this, propName);
	}
}
