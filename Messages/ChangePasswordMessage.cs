#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: ChangePasswordMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Security;

	/// <summary>
	/// Change password message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ChangePasswordMessage : Message
	{
		/// <summary>
		/// Initialize <see cref="ChangePasswordMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected ChangePasswordMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePasswordMessage"/>.
		/// </summary>
		public ChangePasswordMessage()
			: this(MessageTypes.ChangePassword)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// New password.
		/// </summary>
		[DataMember]
		public SecureString NewPassword { get; set; }

		/// <summary>
		/// Change password error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="ChangePasswordMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ChangePasswordMessage
			{
				TransactionId = TransactionId,
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				NewPassword = NewPassword,
				Error = Error,
			};
		}
	}
}