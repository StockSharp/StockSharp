namespace StockSharp.Messages
{
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее данные о позиции.
	/// </summary>
	public sealed class PositionMessage : Message
	{
		/// <summary>
		/// Портфель, в котором создана позиция.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str270Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Текстовое описание позиции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description { get; set; }

		/// <summary>
		/// Инструмент, по которому создана позиция.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str271Key)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Вид лимита для Т+ рынка.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str272Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Номер первоначального сообщения <see cref="PortfolioMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Создать <see cref="PositionMessage"/>.
		/// </summary>
		public PositionMessage()
			: base(MessageTypes.Position)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() +  ",Sec={0},P={1}".Put(SecurityId, PortfolioName);
		}

		/// <summary>
		/// Создать копию объекта <see cref="PositionMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new PositionMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				OriginalTransactionId = OriginalTransactionId,
				DepoName = DepoName,
				LimitType = LimitType,
				LocalTime = LocalTime,
				Description = Description
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}
	}
}