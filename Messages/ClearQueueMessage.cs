#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ClearQueueMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Clear message queue message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ClearQueueMessage : Message
	{
		/// <summary>
		/// Type of messages that should be deleted. If the value is <see langword="null" />, all messages will be deleted.
		/// </summary>
		public MessageTypes? ClearMessageType { get; set; }

		/// <summary>
		/// Security id. If the value is <see langword="null" />, all messages for the security will be deleted.
		/// </summary>
		[DataMember]
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// An additional argument for the market data filter.
		/// </summary>
		[DataMember]
		public object Arg { get; set; }

		/// <summary>
		/// Initialize <see cref="ClearQueueMessage"/>.
		/// </summary>
		public ClearQueueMessage()
			: base(MessageTypes.ClearQueue)
		{
		}
	}
}
