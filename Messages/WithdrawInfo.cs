namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;

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
	[DataContract]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class BankDetails : Cloneable<BankDetails>
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
		/// Create a copy of <see cref="BankDetails"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override BankDetails Clone()
		{
			return new BankDetails
			{
				Account = Account,
				AccountName = AccountName,
				Name = Name,
				Address = Address,
				Country = Country,
				City = City,
				Bic = Bic,
				Swift = Swift,
				Iban = Iban,
				PostalCode = PostalCode,
			};
		}
	}

	/// <summary>
	/// Withdraw info.
	/// </summary>
	[Serializable]
	[DataContract]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class WithdrawInfo : Cloneable<WithdrawInfo>
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
		/// Currency.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CurrencyKey,
			Description = LocalizedStrings.CurrencyKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 1)]
		public CurrencyTypes Currency { get; set; } = CurrencyTypes.BTC;

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
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 5)]
		public BankDetails BankDetails { get; set; }

		/// <summary>
		/// Intermediary bank details.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IntermediaryBankKey,
			Description = LocalizedStrings.IntermediaryBankDetailsKey,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 6)]
		public BankDetails IntermediaryBankDetails { get; set; }

		/// <summary>
		/// Company details.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CompanyKey,
			Description = LocalizedStrings.CompanyDetailsKey,
			GroupName = LocalizedStrings.WithdrawKey,
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
			GroupName = LocalizedStrings.WithdrawKey,
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
			Order = 9)]
		public string Comment { get; set; }

		/// <summary>
		/// Create a copy of <see cref="WithdrawInfo"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override WithdrawInfo Clone()
		{
			return new WithdrawInfo
			{
				Type = Type,
				Currency = Currency,
				Express = Express,
				ChargeFee = ChargeFee,
				BankDetails = BankDetails?.Clone(),
				IntermediaryBankDetails = IntermediaryBankDetails?.Clone(),
				CompanyDetails = CompanyDetails?.Clone(),
				CardNumber = CardNumber,
				PaymentId = PaymentId,
				CryptoAddress = CryptoAddress,
				Comment = Comment,
			};
		}
	}
}