namespace StockSharp.Messages
{
	using System.Runtime.Serialization;
	
	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее общую информацию о заявке.
	/// </summary>
	public abstract class OrderMessage : SecurityMessage
	{
		/// <summary>
		/// Название портфеля, по которому необходимо выставить/снять заявку.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str229Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Тип заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes OrderType { get; set; }

		/// <summary>
		/// Пользовательский идентификатор заявки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Инициализировать <see cref="OrderMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected OrderMessage(MessageTypes type)
			: base(type)
		{
		}
	}
}