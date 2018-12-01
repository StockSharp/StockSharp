#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: SecurityMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing info about the security.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class SecurityMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		[ReadOnly(true)]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str362Key)]
		[MainCategory]
		public string Name { get; set; }

		/// <summary>
		/// Short security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str363Key)]
		[DescriptionLoc(LocalizedStrings.Str364Key)]
		[MainCategory]
		public string ShortName { get; set; }

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeStepKey)]
		[DescriptionLoc(LocalizedStrings.Str366Key)]
		[MainCategory]
		[Nullable]
		public decimal? VolumeStep { get; set; }

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str330Key)]
		[DescriptionLoc(LocalizedStrings.LotVolumeKey)]
		[MainCategory]
		[Nullable]
		public decimal? Multiplier { get; set; }

		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DecimalsKey)]
		[DescriptionLoc(LocalizedStrings.Str548Key)]
		[MainCategory]
		[Nullable]
		public int? Decimals { get; set; }
		
		/// <summary>
		/// Minimum price step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceStepKey)]
		[DescriptionLoc(LocalizedStrings.MinPriceStepKey)]
		[MainCategory]
		[Nullable]
		public decimal? PriceStep { get; set; }

		/// <summary>
		/// Security type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		[Nullable]
		public SecurityTypes? SecurityType { get; set; }

		/// <summary>
		/// Type in ISO 10962 standard.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CfiCodeKey)]
		[DescriptionLoc(LocalizedStrings.CfiCodeDescKey)]
		[MainCategory]
		public string CfiCode { get; set; }

		/// <summary>
		/// Security expiration date (for derivatives - expiration, for bonds — redemption).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str371Key)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? ExpiryDate { get; set; }

		/// <summary>
		/// Settlement date for security (for derivatives and bonds).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SettlementDateKey)]
		[DescriptionLoc(LocalizedStrings.Str373Key)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? SettlementDate { get; set; }

		/// <summary>
		/// Underlying asset code, on which the current security is based.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.UnderlyingAssetKey)]
		[DescriptionLoc(LocalizedStrings.UnderlyingAssetCodeKey)]
		public string UnderlyingSecurityCode { get; set; }

		/// <summary>
		/// Option strike price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[Nullable]
		public decimal? Strike { get; set; }

		/// <summary>
		/// Option type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OptionsContractKey)]
		[DescriptionLoc(LocalizedStrings.OptionContractTypeKey)]
		[Nullable]
		public OptionTypes? OptionType { get; set; }

		/// <summary>
		/// Type of binary option.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BinaryOptionKey)]
		[DescriptionLoc(LocalizedStrings.TypeBinaryOptionKey)]
		public string BinaryOptionType { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// ID of the original message <see cref="SecurityLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Security class.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClassKey)]
		[DescriptionLoc(LocalizedStrings.SecurityClassKey)]
		[MainCategory]
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
		[MainCategory]
		public SecurityTypes? UnderlyingSecurityType { get; set; }

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
		/// Create a copy of <see cref="SecurityMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <param name="copyOriginalTransactionId">Copy <see cref="OriginalTransactionId"/>.</param>
		public void CopyTo(SecurityMessage destination, bool copyOriginalTransactionId = true)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

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
			destination.UnderlyingSecurityCode = UnderlyingSecurityCode;
			destination.VolumeStep = VolumeStep;
			destination.Multiplier = Multiplier;
			destination.Class = Class;
			destination.BinaryOptionType = BinaryOptionType;
			destination.LocalTime = LocalTime;
			destination.IssueSize = IssueSize;
			destination.IssueDate = IssueDate;
			destination.UnderlyingSecurityType = UnderlyingSecurityType;
			destination.BasketCode = BasketCode;
			destination.BasketExpression = BasketExpression;

			if (copyOriginalTransactionId)
				destination.OriginalTransactionId = OriginalTransactionId;
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

			if (BasketCode != null)
				str += $",Basket={BasketCode}/{BasketExpression}";

			return str;
		}
	}
}