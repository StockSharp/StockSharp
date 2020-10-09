namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Security;
	using System.Xml.Serialization;

	/// <summary>
	/// Change password message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ChangePasswordMessage :
		BaseResultMessage<ChangePasswordMessage>, ITransactionIdMessage, IErrorMessage
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

		/// <inheritdoc />
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

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
			destination.Error = Error;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString();

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}