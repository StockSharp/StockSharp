namespace StockSharp.Messages
{
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее фильтр для снятия заявок.
	/// </summary>
	public class OrderGroupCancelMessage : OrderMessage
	{
		///// <summary>
		///// Тип инструмента. Если значение null, то отмена идет по всем типам инструментов.
		///// </summary>
		//[DataMember]
		//[DisplayName("Тип")]
		//[Description("Тип инструмента.")]
		//[MainCategory]
		//public SecurityTypes? SecurityType { get; set; }

		/// <summary>
		/// Номер транзакции отмены.
		/// </summary>
		public long TransactionId { get; set; }

		/// <summary>
		/// <see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str226Key)]
		[DescriptionLoc(LocalizedStrings.Str227Key)]
		[MainCategory]
		public bool? IsStop { get; set; }

		/// <summary>
		/// Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str228Key)]
		[MainCategory]
		public Sides? Side { get; set; }

		/// <summary>
		/// Создать <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		public OrderGroupCancelMessage()
			: base(MessageTypes.OrderGroupCancel)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",IsStop={0},Side={1},SecType={2}".Put(IsStop, Side, SecurityType);
		}

		/// <summary>
		/// Создать копию объекта <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new OrderGroupCancelMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				IsStop = IsStop,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				//SecurityType = SecurityType,
				Side = Side,
				TransactionId = TransactionId,
			};

			CopyTo(clone);

			return clone;
		}
	}
}