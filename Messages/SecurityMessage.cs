namespace StockSharp.Messages;

/// <summary>
/// A message containing info about the security.
/// </summary>
[DataContract]
[Serializable]
public class SecurityMessage : BaseSubscriptionIdMessage<SecurityMessage>, ISecurityIdMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IdentifierKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[TypeConverter(typeof(StringToSecurityIdTypeConverter))]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Security name.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.SecurityNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Name { get; set; }

	/// <summary>
	/// Short security name.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortNameKey,
		Description = LocalizedStrings.ShortNameDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string ShortName { get; set; }

	/// <summary>
	/// Minimum volume step.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeStepKey,
		Description = LocalizedStrings.MinVolStepKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? VolumeStep { get; set; }

	/// <summary>
	/// Minimum volume allowed in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MinVolumeKey,
		Description = LocalizedStrings.MinVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? MinVolume { get; set; }

	/// <summary>
	/// Maximum volume allowed in order.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MaxVolumeKey,
		Description = LocalizedStrings.MaxVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? MaxVolume { get; set; }

	/// <summary>
	/// Lot multiplier.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LotKey,
		Description = LocalizedStrings.LotVolumeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? Multiplier { get; set; }

	/// <summary>
	/// Number of digits in price after coma.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DecimalsKey,
		Description = LocalizedStrings.DecimalsDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public int? Decimals { get; set; }

	private decimal? _priceStep;

	/// <summary>
	/// Minimum price step.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceStepKey,
		Description = LocalizedStrings.MinPriceStepKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? PriceStep
	{
		get => _priceStep;
		set => _priceStep = value > 0 ? value : null;
	}

	/// <summary>
	/// Security type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.SecurityTypeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public SecurityTypes? SecurityType { get; set; }

	/// <summary>
	/// Type in ISO 10962 standard.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CfiCodeKey,
		Description = LocalizedStrings.CfiCodeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string CfiCode { get; set; }

	/// <summary>
	/// Security expiration date (for derivatives - expiration, for bonds — redemption).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpiryDateKey,
		Description = LocalizedStrings.ExpiryDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public DateTimeOffset? ExpiryDate { get; set; }

	/// <summary>
	/// Settlement date for security (for derivatives and bonds).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SettlementDateKey,
		Description = LocalizedStrings.SettlementDateForSecurityKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public DateTimeOffset? SettlementDate { get; set; }

	/// <summary>
	/// Underlying asset on which the current security is built.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnderlyingAssetKey,
		Description = LocalizedStrings.UnderlyingAssetDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId UnderlyingSecurityId { get; set; }

	/// <summary>
	/// Minimum volume allowed in order for underlying security.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UnderlyingMinVolumeKey,
		Description = LocalizedStrings.UnderlyingMinVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public decimal? UnderlyingSecurityMinVolume { get; set; }

	/// <summary>
	/// Option strike price.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StrikeKey,
		Description = LocalizedStrings.OptionStrikePriceKey,
		GroupName = LocalizedStrings.OptionsKey)]
	//[Nullable]
	public decimal? Strike { get; set; }

	/// <summary>
	/// Option type.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OptionsContractKey,
		Description = LocalizedStrings.OptionContractTypeKey,
		GroupName = LocalizedStrings.OptionsKey)]
	//[Nullable]
	public OptionTypes? OptionType { get; set; }

	/// <summary>
	/// Type of binary option.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BinaryOptionKey,
		Description = LocalizedStrings.TypeBinaryOptionKey,
		GroupName = LocalizedStrings.OptionsKey)]
	public string BinaryOptionType { get; set; }

	/// <summary>
	/// Trading security currency.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CurrencyKey,
		Description = LocalizedStrings.CurrencyDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	//[Nullable]
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// Security class.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClassKey,
		Description = LocalizedStrings.SecurityClassKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Class { get; set; }

	/// <summary>
	/// Number of issued contracts.
	/// </summary>
	[DataMember]
	public decimal? IssueSize { get; set; }

	/// <summary>
	/// Date of issue.
	/// </summary>
	[DataMember]
	public DateTimeOffset? IssueDate { get; set; }

	/// <summary>
	/// Underlying security type.
	/// </summary>
	[DataMember]
	public SecurityTypes? UnderlyingSecurityType { get; set; }

	/// <summary>
	/// Can have short positions.
	/// </summary>
	[DataMember]
	public bool? Shortable { get; set; }

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
	public string BasketCode { get; set; }

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
	public string BasketExpression { get; set; }

	/// <summary>
	/// Face value.
	/// </summary>
	[DataMember]
	public decimal? FaceValue { get; set; }

	/// <summary>
	/// Identifier on primary exchange.
	/// </summary>
	[DataMember]
	public SecurityId PrimaryId { get; set; }

	/// <summary>
	/// <see cref="SettlementTypes"/>.
	/// </summary>
	[DataMember]
	public SettlementTypes? SettlementType { get; set; }

	/// <summary>
	/// <see cref="OptionStyles"/>.
	/// </summary>
	[DataMember]
	public OptionStyles? OptionStyle { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.Securities;

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityMessage"/>.
	/// </summary>
	public SecurityMessage()
		: base(MessageTypes.Security)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected SecurityMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <param name="copyOriginalTransactionId">Copy <see cref="IOriginalTransactionIdMessage.OriginalTransactionId"/>.</param>
	public void CopyTo(SecurityMessage destination, bool copyOriginalTransactionId)
	{
		var originTransId = destination.OriginalTransactionId;

		CopyTo(destination);

		if (!copyOriginalTransactionId)
			destination.OriginalTransactionId = originTransId;
	}

	/// <inheritdoc />
	public override void CopyTo(SecurityMessage destination)
	{
		CopyEx(destination, true);
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <param name="copyBase">Copy <see cref="BaseSubscriptionIdMessage{TMessage}"/>.</param>
	public void CopyEx(SecurityMessage destination, bool copyBase)
	{
		if (copyBase)
			base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.Name = Name;
		destination.ShortName = ShortName;
		destination.Currency = Currency;
		destination.ExpiryDate = ExpiryDate;
		destination.OptionType = OptionType;
		destination.PriceStep = PriceStep;
		destination.Decimals = Decimals;
		destination.SecurityType = SecurityType;
		destination.CfiCode = CfiCode;
		destination.SettlementDate = SettlementDate;
		destination.Strike = Strike;
		destination.UnderlyingSecurityId = UnderlyingSecurityId;
		destination.VolumeStep = VolumeStep;
		destination.MinVolume = MinVolume;
		destination.MaxVolume = MaxVolume;
		destination.Multiplier = Multiplier;
		destination.Class = Class;
		destination.BinaryOptionType = BinaryOptionType;
		destination.IssueSize = IssueSize;
		destination.IssueDate = IssueDate;
		destination.UnderlyingSecurityType = UnderlyingSecurityType;
		destination.UnderlyingSecurityMinVolume = UnderlyingSecurityMinVolume;
		destination.Shortable = Shortable;
		destination.BasketCode = BasketCode;
		destination.BasketExpression = BasketExpression;
		destination.FaceValue = FaceValue;
		destination.SettlementType = SettlementType;
		destination.OptionStyle = OptionStyle;
		destination.PrimaryId = PrimaryId;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Sec={SecurityId}";

		if (SecurityType != null)
			str += $",SecType={SecurityType}";

		if (!Name.IsEmpty())
			str += $",Name={Name}";

		if (!ShortName.IsEmpty())
			str += $",Short={ShortName}";

		if (ExpiryDate != null)
			str += $",Exp={ExpiryDate}";

		if (PriceStep != null)
			str += $",Price={PriceStep}";

		if (VolumeStep != null)
			str += $",Vol={VolumeStep}";

		if (MinVolume != null)
			str += $",MinVol={MinVolume}";

		if (MaxVolume != null)
			str += $",MaxVol={MaxVolume}";

		if (Decimals != null)
			str += $",Dec={Decimals}";

		if (Multiplier != null)
			str += $",Mult={Multiplier}";

		if (SettlementDate != null)
			str += $",Sett={SettlementDate}";

		if (Currency != null)
			str += $",Cur={Currency}";

		if (OptionType != null)
			str += $",Opt={OptionType}";

		if (Strike != null)
			str += $",Strike={Strike}";

		if (CfiCode != null)
			str += $",CFI={CfiCode}";

		if (UnderlyingSecurityId != default)
			str += $",Under={UnderlyingSecurityId}";

		if (Class != null)
			str += $",Class={Class}";

		if (BinaryOptionType != null)
			str += $",Bin={BinaryOptionType}";

		if (Shortable != null)
			str += $",Strike={Shortable}";

		if (BasketCode != null)
			str += $",Basket={BasketCode}/{BasketExpression}";

		if (FaceValue != null)
			str += $",FaceValue={FaceValue}";

		if (SettlementType != null)
			str += $",SettlementType={SettlementType}";

		if (OptionStyle != null)
			str += $",OptionStyle={OptionStyle}";

		if (PrimaryId != default)
			str += $",Primary={PrimaryId}";

		return str;
	}
}
