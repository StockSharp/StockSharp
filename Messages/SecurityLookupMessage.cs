#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: SecurityLookupMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Message security lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SecurityLookupMessage : SecurityMessage//, IEquatable<SecurityLookupMessage>
	{
		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Securities types.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		public IEnumerable<SecurityTypes> SecurityTypes { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityLookupMessage"/>.
		/// </summary>
		public SecurityLookupMessage()
			: base(MessageTypes.SecurityLookup)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityLookupMessage
			{
				TransactionId = TransactionId,
				SecurityTypes = SecurityTypes,
			};
			
			CopyTo(clone);

			return clone;
		}

		///// <summary>
		///// Determines whether the specified criterias are considered equal.
		///// </summary>
		///// <param name="other">Another search criteria with which to compare.</param>
		///// <returns><see langword="true" />, if criterias are equal, otherwise, <see langword="false" />.</returns>
		//public bool Equals(SecurityLookupMessage other)
		//{
		//	if (!SecurityId.IsDefault() && SecurityId.Equals(other.SecurityId))
		//		return true;

		//	if (Name == other.Name && 
		//		ShortName == other.ShortName && 
		//		Currency == other.Currency && 
		//		ExpiryDate == other.ExpiryDate && 
		//		OptionType == other.OptionType &&
		//		((SecurityTypes == null && other.SecurityTypes == null) ||
		//		(SecurityTypes != null && other.SecurityTypes != null && SecurityTypes.SequenceEqual(other.SecurityTypes))) && 
		//		SettlementDate == other.SettlementDate &&
		//		BinaryOptionType == other.BinaryOptionType &&
		//		Strike == other.Strike &&
		//		UnderlyingSecurityCode == other.UnderlyingSecurityCode && CFICode == other.CFICode)
		//		return true;

		//	return false;
		//}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TransId={TransactionId},SecId={SecurityId},Name={Name},SecType={SecurityType},ExpDate={ExpiryDate}";
		}
	}
}