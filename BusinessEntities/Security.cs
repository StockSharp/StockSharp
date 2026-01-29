namespace StockSharp.BusinessEntities;

/// <summary>
/// Security (shares, futures, options etc.).
/// </summary>
[Serializable]
[DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SecurityKey,
	Description = LocalizedStrings.SecurityDescKey)]
public partial class Security : Cloneable<Security>, INotifyPropertyChanged
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

	private DateTime? _expiryDate;

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
	public DateTime? ExpiryDate
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

	private DateTime? _settlementDate;

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
	public DateTime? SettlementDate
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

	private DateTime? _issueDate;

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
	public DateTime? IssueDate
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

	private DateTime? _buyBackDate;

	/// <summary>
	/// BuyBack date.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public DateTime? BuyBackDate
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
		destination.CfiCode = CfiCode;
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

#pragma warning disable CS0618 // Type or member is obsolete
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
		destination.Turnover = Turnover;

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
#pragma warning restore CS0618 // Type or member is obsolete
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
