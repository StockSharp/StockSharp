#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderStatus.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// System order states.
	/// </summary>
	[DataContract]
	[Serializable]
	[Obsolete]
	public enum OrderStatus : long
	{
		/// <summary>
		/// The transaction is sent to the server.
		/// </summary>
		[EnumMember]SentToServer = 0,

		/// <summary>
		/// The transaction is received by the server.
		/// </summary>
		[EnumMember]ReceiveByServer = 1,

		/// <summary>
		/// Sending transaction error.
		/// </summary>
		[EnumMember]GateError = 2,

		/// <summary>
		/// The order is accepted by the exchange.
		/// </summary>
		[EnumMember]Accepted = 3,

		/// <summary>
		/// The order is not accepted by the exchange.
		/// </summary>
		[EnumMember]NotDone = 4,

		/// <summary>
		/// The transaction did not pass server check.
		/// </summary>
		[EnumMember]NotValidated = 5,

		/// <summary>
		/// The transaction did not pass server limits.
		/// </summary>
		[EnumMember]NotValidatedLimit = 6,

		/// <summary>
		/// The transaction was approved by manager.
		/// </summary>
		[EnumMember]AcceptedByManager = 7,

		/// <summary>
		/// The transaction did not approved by manager.
		/// </summary>
		[EnumMember]NotAcceptedByManager = 8,

		/// <summary>
		/// The transaction was cancelled by manager.
		/// </summary>
		[EnumMember]CanceledByManager = 9,

		/// <summary>
		/// The transaction is not supported by server.
		/// </summary>
		[EnumMember]NotSupported = 10,

		/// <summary>
		/// Digital signature fail.
		/// </summary>
		[EnumMember]NotSigned = 11,

		/// <summary>
		/// Cancel pending.
		/// </summary>
		[EnumMember]SentToCanceled = 12,

		/// <summary>
		/// Cancelled.
		/// </summary>
		[EnumMember]Cancelled = 13,

		/// <summary>
		/// Matched.
		/// </summary>
		[EnumMember]Matched = 14,

		/// <summary>
		/// Reject by server.
		/// </summary>
		[EnumMember]RejectedBySystem = 15,
	}
}