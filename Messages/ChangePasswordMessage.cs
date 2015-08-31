namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Security;

	/// <summary>
	/// Сообщение изменения пароля.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ChangePasswordMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ChangePasswordMessage"/>.
		/// </summary>
		public ChangePasswordMessage()
			: base(MessageTypes.ChangePassword)
		{
		}

		/// <summary>
		/// Идентификатор запроса.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Идентификатор первоначального сообщения <see cref="ChangePasswordMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Новый пароль.
		/// </summary>
		[DataMember]
		public SecureString NewPassword { get; set; }

		/// <summary>
		/// Информация об ошибке смены пароля.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Создать копию <see cref="ChangePasswordMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new ChangePasswordMessage
			{
				LocalTime = LocalTime,
				NewPassword = NewPassword,
				Error = Error,
			};
		}
	}
}