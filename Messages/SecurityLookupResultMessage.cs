#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: SecurityLookupResultMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security lookup result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SecurityLookupResultMessage : Message
	{
		/// <summary>
		/// ID of the original message <see cref="SecurityLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		public SecurityLookupResultMessage()
			: base(MessageTypes.SecurityLookupResult)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new SecurityLookupResultMessage
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