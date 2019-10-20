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
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Message security lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SecurityLookupMessage : SecurityMessage, ITransactionIdMessage
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
		public SecurityTypes[] SecurityTypes { get; set; }

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
			var clone = new SecurityLookupMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(SecurityLookupMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			destination.TransactionId = TransactionId;
			destination.SecurityTypes = SecurityTypes?.ToArray();

			base.CopyTo(destination);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",TransId={TransactionId},SecId={SecurityId},Name={Name},SecType={this.GetSecurityTypes().Select(t => t.To<string>()).Join("|")},ExpDate={ExpiryDate}";
		}
	}
}