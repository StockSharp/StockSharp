namespace StockSharp.Messages
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Сообщение окончания поиска портфелей.
	/// </summary>
	public class PortfolioLookupResultMessage : Message
	{
		/// <summary>
		/// Номер первоначального сообщения <see cref="PortfolioMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Информация об ошибке поиска портфелей.
		/// </summary>
		public Exception Error { get; set; }

		/// <summary>
		/// Создать <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		public PortfolioLookupResultMessage()
			: base(MessageTypes.PortfolioLookupResult)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="Message"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new PortfolioLookupResultMessage
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