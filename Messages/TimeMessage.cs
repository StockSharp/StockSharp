#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: TimeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The message contains information about the current time.
	/// </summary>
	[DataContract]
	[Serializable]
	public class TimeMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeMessage"/>.
		/// </summary>
		public TimeMessage()
			: base(MessageTypes.Time)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected TimeMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TimeMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public string OriginalTransactionId { get; set; }

		/// <summary>
		/// Server time.
		/// </summary>
		[DataMember]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",ID={TransactionId},Response={OriginalTransactionId}";
		}

		/// <summary>
		/// Create a copy of <see cref="TimeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new TimeMessage
			{
				TransactionId = TransactionId,
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				ServerTime = ServerTime,
			};
		}
	}
}