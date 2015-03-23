namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, запрашивающее текущие зарегистрированные заявки и сделки.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderStatusMessage : Message
	{
		/// <summary>
		/// Номер транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Идентификатор запрашиваемой заявки.
		/// </summary>
		[DataMember]
		public long? OrderId { get; set; }

		/// <summary>
		/// Идентификатор запрашиваемой заявки (ввиде строки, если электронная площадка не использует числовое представление идентификатора заявки).
		/// </summary>
		[DataMember]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Номер транзакции запрашиваемой заявки.
		/// </summary>
		[DataMember]
		public long? OrderTransactionId { get; set; }

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
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				OrderTransactionId = OrderTransactionId
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