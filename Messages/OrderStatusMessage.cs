namespace StockSharp.Messages
{
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, запрашивающее текущие зарегистрированные заявки и сделки.
	/// </summary>
	public class OrderStatusMessage : Message
	{
		/// <summary>
		/// Номер транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Создать <see cref="OrderStatusMessage"/>.
		/// </summary>
		public OrderStatusMessage()
			: base(MessageTypes.OrderStatus)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="OrderStatusMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new OrderStatusMessage
			{
				LocalTime = LocalTime,
				TransactionId = TransactionId,
			};
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",TransId={0}".Put(TransactionId);
		}
	}
}