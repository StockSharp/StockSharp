namespace StockSharp.Messages
{
	using System;
	using System.Security;

	/// <summary>
	/// Сообщение изменения пароля.
	/// </summary>
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
		/// Номер запроса.
		/// </summary>
		public long TransactionId { get; set; }

		/// <summary>
		/// Номер первоначального сообщения <see cref="ChangePasswordMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Новый пароль.
		/// </summary>
		public SecureString NewPassword { get; set; }

		/// <summary>
		/// Информация об ошибке смены пароля.
		/// </summary>
		public Exception Error { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="ChangePasswordMessage"/>.
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