namespace StockSharp.Messages;

/// <summary>
/// Negotiated Trades Mode information.
/// </summary>
[Serializable]
[DataContract]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class NtmOrderInfo : Cloneable<NtmOrderInfo>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NtmOrderInfo"/>.
	/// </summary>
	public NtmOrderInfo()
	{
	}

	/// <summary>
	/// Partner-organization.
	/// </summary>
	[DataMember]
	public string Partner { get; set; }

	/// <summary>
	/// Execution date OTC.
	/// </summary>
	[DataMember]
	//[Nullable]
	public DateTimeOffset? SettleDate { get; set; }

	/// <summary>
	/// REPO NTM reference.
	/// </summary>
	[DataMember]
	public string MatchRef { get; set; }

	/// <summary>
	/// Settlement code.
	/// </summary>
	[DataMember]
	public string SettleCode { get; set; }

	/// <summary>
	/// Owner of transaction (OTC trade).
	/// </summary>
	[DataMember]
	public string ForAccount { get; set; }

	/// <summary>
	/// Currency code in ISO 4217 standard (OTC trade). Non-system trade parameter
	/// </summary>
	[DataMember]
	public CurrencyTypes CurrencyType { get; set; }

	/// <summary>
	/// Create a copy of <see cref="NtmOrderInfo"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override NtmOrderInfo Clone()
	{
		return new NtmOrderInfo
		{
			CurrencyType = CurrencyType,
			ForAccount = ForAccount,
			MatchRef = MatchRef,
			Partner = Partner,
			SettleCode = SettleCode,
			SettleDate = SettleDate
		};
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		MatchRef = storage.GetValue<string>(nameof(MatchRef));
		Partner = storage.GetValue<string>(nameof(Partner));
		SettleCode = storage.GetValue<string>(nameof(SettleCode));
		SettleDate = storage.GetValue<DateTimeOffset?>(nameof(SettleDate));
		ForAccount = storage.GetValue<string>(nameof(ForAccount));
		CurrencyType = storage.GetValue<CurrencyTypes>(nameof(CurrencyType));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(MatchRef), MatchRef);
		storage.SetValue(nameof(Partner), Partner);
		storage.SetValue(nameof(SettleCode), SettleCode);
		storage.SetValue(nameof(SettleDate), SettleDate);
		storage.SetValue(nameof(ForAccount), ForAccount);
		storage.SetValue(nameof(CurrencyType), CurrencyType);
	}
}