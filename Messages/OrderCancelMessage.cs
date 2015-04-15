namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Сообщение, содержащее данные для снятия заявки.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderCancelMessage : OrderMessage
	{
		/// <summary>
		/// Идентификатор отменяемой заявки.
		/// </summary>
		[DataMember]
		public long? OrderId { get; set; }

		/// <summary>
		/// Идентификатор отменяемой заявки (ввиде строки, если электронная площадка не использует числовое представление идентификатора заявки).
		/// </summary>
		[DataMember]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Номер транзакции отмены.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Номер транзакции отменяемой заявки.
		/// </summary>
		[DataMember]
		public long OrderTransactionId { get; set; }

		/// <summary>
		/// Отменяемый объем. Если значение не указано, то отменяется весь активный объем заявки.
		/// </summary>
		[DataMember]
		public decimal Volume { get; set; }

		/// <summary>
		/// Создать <see cref="OrderCancelMessage"/>.
		/// </summary>
		public OrderCancelMessage()
			: base(MessageTypes.OrderCancel)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="OrderCancelMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new OrderCancelMessage
			{
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				TransactionId = TransactionId,
				OrderTransactionId = OrderTransactionId,
				Volume = Volume,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",OriginTransId={0},TransId={1},OrderId={2}".Put(OrderTransactionId, TransactionId, OrderId);
		}
	}
}