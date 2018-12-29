namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Withdraw types.
	/// </summary>
	public enum WithdrawTypes
	{
		/// <summary>
		/// Cryptocurrency.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CryptocurrencyKey)]
		Crypto,

		/// <summary>
		/// Bank wire.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BankWireKey)]
		BankWire,

		/// <summary>
		/// Bank card.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BankCardKey)]
		BankCard,
	}

	/// <summary>
	/// Bank details.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class BankDetails : IPersistable
	{
		/// <summary>
		/// Bank account.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AccountKey,
			Description = LocalizedStrings.BankAccountKey,
			Order = 0)]
		public string Account { get; set; }

		/// <summary>
		/// Bank account name.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AccountNameKey,
			Description = LocalizedStrings.BankAccountNameKey,
			Order = 1)]
		public string AccountName { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.NameKey,
			Description = LocalizedStrings.NameKey + LocalizedStrings.Dot,
			Order = 2)]
		public string Name { get; set; }

		/// <summary>
		/// Address.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AddressKey,
			Description = LocalizedStrings.AddressKey + LocalizedStrings.Dot,
			Order = 3)]
		public string Address { get; set; }

		/// <summary>
		/// Country.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CountryKey,
			Description = LocalizedStrings.CountryKey + LocalizedStrings.Dot,
			Order = 4)]
		public string Country { get; set; }

		/// <summary>
		/// City.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CityKey,
			Description = LocalizedStrings.CityKey + LocalizedStrings.Dot,
			Order = 5)]
		public string City { get; set; }

		/// <summary>
		/// Bank SWIFT.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SwiftKey,
			Description = LocalizedStrings.BankSwiftKey,
			Order = 6)]
		public string Swift { get; set; }

		/// <summary>
		/// Bank BIC.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BicKey,
			Description = LocalizedStrings.BankBicKey,
			Order = 7)]
		public string Bic { get; set; }
		
		/// <summary>
		/// IBAN.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IbanKey,
			Description = LocalizedStrings.IbanKey + LocalizedStrings.Dot,
			Order = 8)]
		public string Iban { get; set; }

		/// <summary>
		/// Postal code.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PostalCodeKey,
			Description = LocalizedStrings.PostalCodeKey + LocalizedStrings.Dot,
			Order = 8)]
		public string PostalCode { get; set; }

		/// <summary>
		/// Currency.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CurrencyKey,
			Description = LocalizedStrings.CurrencyKey + LocalizedStrings.Dot,
			Order = 9)]
		public CurrencyTypes Currency { get; set; } = CurrencyTypes.BTC;

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Account = storage.GetValue<string>(nameof(Account));
			AccountName = storage.GetValue<string>(nameof(AccountName));
			Name = storage.GetValue<string>(nameof(Name));
			Address = storage.GetValue<string>(nameof(Address));
			Country = storage.GetValue<string>(nameof(Country));
			City = storage.GetValue<string>(nameof(City));
			Bic = storage.GetValue<string>(nameof(Bic));
			Swift = storage.GetValue<string>(nameof(Swift));
			Iban = storage.GetValue<string>(nameof(Iban));
			PostalCode = storage.GetValue<string>(nameof(PostalCode));
			Currency = storage.GetValue<CurrencyTypes>(nameof(Currency));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Account), Account);
			storage.SetValue(nameof(AccountName), AccountName);
			storage.SetValue(nameof(Name), Name);
			storage.SetValue(nameof(Address), Address);
			storage.SetValue(nameof(Country), Country);
			storage.SetValue(nameof(City), City);
			storage.SetValue(nameof(Bic), Bic);
			storage.SetValue(nameof(Swift), Swift);
			storage.SetValue(nameof(Iban), Iban);
			storage.SetValue(nameof(PostalCode), PostalCode);
			storage.SetValue(nameof(Currency), Currency);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Acc={Account}&AccName={AccountName}&Curr={Currency}&Name={Name}&Add={Address}&Cntry={Country}&City={City}&Bic={Bic}&Swift={Swift}&Iban={Iban}&Postal={PostalCode}";
		}
	}

	/// <summary>
	/// Withdraw info.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class WithdrawInfo : IPersistable
	{
		/// <summary>
		/// Withdraw type.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TypeKey,
			Description = LocalizedStrings.WithdrawTypeKey,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 0)]
		public WithdrawTypes Type { get; set; }

		/// <summary>
		/// Crypto address.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CryptoAddressKey,
			Description = LocalizedStrings.CryptoAddressKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 2)]
		public string CryptoAddress { get; set; }

		/// <summary>
		/// Express withdraw.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ExpressKey,
			Description = LocalizedStrings.ExpressWithdrawKey,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 3)]
		public bool Express { get; set; }

		/// <summary>
		/// Charge fee.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ChargeFeeKey,
			Description = LocalizedStrings.ChargeFeeKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 3)]
		public decimal? ChargeFee { get; set; }

		/// <summary>
		/// Payment id.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PaymentIdKey,
			Description = LocalizedStrings.PaymentIdKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 4)]
		public string PaymentId { get; set; }

		/// <summary>
		/// Bank details.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BankKey,
			Description = LocalizedStrings.BankDetailsKey,
			GroupName = LocalizedStrings.BankKey,
			Order = 50)]
		public BankDetails BankDetails { get; set; }

		/// <summary>
		/// Intermediary bank details.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IntermediaryBankKey,
			Description = LocalizedStrings.IntermediaryBankDetailsKey,
			GroupName = LocalizedStrings.BankKey,
			Order = 51)]
		public BankDetails IntermediaryBankDetails { get; set; }

		/// <summary>
		/// Company details.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CompanyKey,
			Description = LocalizedStrings.CompanyDetailsKey,
			GroupName = LocalizedStrings.BankKey,
			Order = 7)]
		public BankDetails CompanyDetails { get; set; }

		/// <summary>
		/// Bank card number.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BankCardKey,
			Description = LocalizedStrings.BankCardNumberKey,
			GroupName = LocalizedStrings.BankKey,
			Order = 8)]
		public string CardNumber { get; set; }

		/// <summary>
		/// Comment of bank transaction.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str135Key,
			Description = LocalizedStrings.BankCommentKey,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 9)]
		public string Comment { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Type = storage.GetValue<WithdrawTypes>(nameof(Type));
			Express = storage.GetValue<bool>(nameof(Express));
			ChargeFee = storage.GetValue<decimal?>(nameof(ChargeFee));
			BankDetails = storage.GetValue<SettingsStorage>(nameof(BankDetails))?.Load<BankDetails>();
			IntermediaryBankDetails = storage.GetValue<SettingsStorage>(nameof(IntermediaryBankDetails))?.Load<BankDetails>();
			CompanyDetails = storage.GetValue<SettingsStorage>(nameof(CompanyDetails))?.Load<BankDetails>();
			CardNumber = storage.GetValue<string>(nameof(CardNumber));
			PaymentId = storage.GetValue<string>(nameof(PaymentId));
			CryptoAddress = storage.GetValue<string>(nameof(CryptoAddress));
			Comment = storage.GetValue<string>(nameof(Comment));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Type), Type);
			storage.SetValue(nameof(Express), Express);
			storage.SetValue(nameof(ChargeFee), ChargeFee);
			storage.SetValue(nameof(BankDetails), BankDetails?.Save());
			storage.SetValue(nameof(IntermediaryBankDetails), IntermediaryBankDetails?.Save());
			storage.SetValue(nameof(CompanyDetails), CompanyDetails?.Save());
			storage.SetValue(nameof(CardNumber), CardNumber);
			storage.SetValue(nameof(PaymentId), PaymentId);
			storage.SetValue(nameof(CryptoAddress), CryptoAddress);
			storage.SetValue(nameof(Comment), Comment);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Type={Type}&Addr={CryptoAddress}&Id={PaymentId}&Comment={Comment}&Company={CompanyDetails}&Bank={BankDetails}&Fee={ChargeFee}";
		}
	}
}