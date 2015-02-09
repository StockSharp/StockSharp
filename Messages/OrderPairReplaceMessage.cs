namespace StockSharp.Messages
{
	using Ecng.Common;

	/// <summary>
	/// Сообщение, содержащее информацию для перерегистрации пары заявок.
	/// </summary>
	public class OrderPairReplaceMessage : SecurityMessage
	{
		/// <summary>
		/// Сообщение, содержащее информацию для перерегистрации первой заявки.
		/// </summary>
		public OrderReplaceMessage Message1 { get; set; }

		/// <summary>
		/// Сообщение, содержащее информацию для перерегистрации второй заявки.
		/// </summary>
		public OrderReplaceMessage Message2 { get; set; }

		/// <summary>
		/// Создать <see cref="OrderPairReplaceMessage"/>.
		/// </summary>
		public OrderPairReplaceMessage()
			: base(MessageTypes.OrderPairReplace)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="OrderPairReplaceMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new OrderPairReplaceMessage
			{
				LocalTime = LocalTime,
				Message1 = Message1.CloneNullable(),
				Message2 = Message2.CloneNullable(),
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
			return base.ToString() + ",{0},{1}".Put(Message1, Message2);
		}
	}
}