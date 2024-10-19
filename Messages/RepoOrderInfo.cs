namespace StockSharp.Messages;

/// <summary>
/// REPO info.
/// </summary>
[Serializable]
[DataContract]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class RepoOrderInfo : Cloneable<RepoOrderInfo>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RepoOrderInfo"/>.
	/// </summary>
	public RepoOrderInfo()
	{
	}

	/// <summary>
	/// Partner-organization.
	/// </summary>
	[DataMember]
	public string Partner { get; set; }

	/// <summary>
	/// REPO expiration.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? Term { get; set; }

	/// <summary>
	/// Repo rate, in percentage.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? Rate { get; set; }

	/// <summary>
	/// Blocking code.
	/// </summary>
	[DataMember]
	//[Nullable]
	public bool? BlockSecurities { get; set; }

	/// <summary>
	/// The rate of fixed compensation payable in the event that the second part of the repo, the percentage.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? RefundRate { get; set; }

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
	/// REPO second price part.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? SecondPrice { get; set; }

	/// <summary>
	/// Execution date OTC.
	/// </summary>
	[DataMember]
	//[Nullable]
	public DateTimeOffset? SettleDate { get; set; }

	/// <summary>
	/// REPO-M the begin value of the discount.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? StartDiscount { get; set; }

	/// <summary>
	/// REPO-M the lower limit value of the discount.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? LowerDiscount { get; set; }

	/// <summary>
	/// REPO-M the upper limit value of the discount.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? UpperDiscount { get; set; }

	/// <summary>
	/// REPO-M volume.
	/// </summary>
	[DataMember]
	//[Nullable]
	public decimal? Value { get; set; }

	/// <summary>
	/// REPO-M.
	/// </summary>
	public bool IsModified { get; set; }

	/// <summary>
	/// Create a copy of <see cref="RepoOrderInfo"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override RepoOrderInfo Clone()
	{
		return new RepoOrderInfo
		{
			MatchRef = MatchRef,
			Partner = Partner,
			SettleCode = SettleCode,
			SettleDate = SettleDate,
			Value = Value,
			BlockSecurities = BlockSecurities,
			LowerDiscount = LowerDiscount,
			Rate = Rate,
			RefundRate = RefundRate,
			SecondPrice = SecondPrice,
			StartDiscount = StartDiscount,
			Term = Term,
			UpperDiscount = UpperDiscount,
			IsModified = IsModified
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
		Value = storage.GetValue<decimal?>(nameof(Value));
		BlockSecurities = storage.GetValue<bool?>(nameof(BlockSecurities));
		LowerDiscount = storage.GetValue<decimal?>(nameof(LowerDiscount));
		Rate = storage.GetValue<decimal?>(nameof(Rate));
		RefundRate = storage.GetValue<decimal?>(nameof(RefundRate));
		SecondPrice = storage.GetValue<decimal?>(nameof(SecondPrice));
		StartDiscount = storage.GetValue<decimal?>(nameof(StartDiscount));
		Term = storage.GetValue<decimal?>(nameof(Term));
		UpperDiscount = storage.GetValue<decimal?>(nameof(UpperDiscount));
		IsModified = storage.GetValue<bool>(nameof(IsModified));
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
		storage.SetValue(nameof(Value), Value);
		storage.SetValue(nameof(BlockSecurities), BlockSecurities);
		storage.SetValue(nameof(LowerDiscount), LowerDiscount);
		storage.SetValue(nameof(Rate), Rate);
		storage.SetValue(nameof(RefundRate), RefundRate);
		storage.SetValue(nameof(SecondPrice), SecondPrice);
		storage.SetValue(nameof(StartDiscount), StartDiscount);
		storage.SetValue(nameof(Term), Term);
		storage.SetValue(nameof(UpperDiscount), UpperDiscount);
		storage.SetValue(nameof(IsModified), IsModified);
	}
}