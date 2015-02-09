namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Сообщение окончания поиска инструментов.
	/// </summary>
	public class SecurityLookupResultMessage : Message
	{
		/// <summary>
		/// Номер первоначального сообщения <see cref="SecurityLookupMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Информация об ошибке поиска инструментов.
		/// </summary>
		public Exception Error { get; set; }

		/// <summary>
		/// Создать <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		public SecurityLookupResultMessage()
			: base(MessageTypes.SecurityLookupResult)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new SecurityLookupResultMessage
			{
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				Error = Error
			};
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Orig={0}".Put(OriginalTransactionId);
		}
	}
}