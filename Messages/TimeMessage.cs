namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Сообщение, содержащее данные о текущем времени.
	/// </summary>
	public sealed class TimeMessage : Message
	{
		/// <summary>
		/// Создать <see cref="TimeMessage"/>.
		/// </summary>
		public TimeMessage()
			: base(MessageTypes.Time)
		{
		}

		/// <summary>
		/// Идентификатор запроса.
		/// </summary>
		public string TransactionId { get; set; }

		/// <summary>
		/// Номер первоначального сообщения <see cref="TimeMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public string OriginalTransactionId { get; set; }

		///// <summary>
		///// Временной сдвиг от текущего времени. Используется в случае, если сервер брокера самостоятельно
		///// указывает сдвиг во времени.
		///// </summary>
		//public TimeSpan? Shift { get; set; }

		/// <summary>
		/// Серверное время.
		/// </summary>
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",ID={0},Response={1}".Put(TransactionId, OriginalTransactionId);
		}

		/// <summary>
		/// Создать копию объекта <see cref="TimeMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
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