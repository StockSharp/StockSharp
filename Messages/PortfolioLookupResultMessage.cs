#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioLookupResultMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio lookup result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioLookupResultMessage : Message
	{
		/// <summary>
		/// ID of the original message <see cref="PortfolioMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Lookup error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		public PortfolioLookupResultMessage()
			: base(MessageTypes.PortfolioLookupResult)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new PortfolioLookupResultMessage
			{
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				Error = Error
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Orig={OriginalTransactionId}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}