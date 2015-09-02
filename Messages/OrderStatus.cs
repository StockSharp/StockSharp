namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// System order states.
	/// </summary>
	[DataContract]
	[Serializable]
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