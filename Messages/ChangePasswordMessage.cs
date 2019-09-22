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
	public class ChangePasswordMessage : BaseResultMessage<ChangePasswordMessage>, ITransactionIdMessage
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

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		[field: NonSerialized]
		private SecureString _newPassword;

		/// <summary>
		/// New password.
		/// </summary>
		[DataMember]
		public SecureString NewPassword
		{
			get => _newPassword;
			set => _newPassword = value;
		}

		/// <inheritdoc />
		protected override void CopyTo(ChangePasswordMessage destination)
		{
			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.NewPassword = NewPassword;
		}
	}
}