namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее данные по изменениям для позиции.
	/// </summary>
	[DataContract]
	[Serializable]
	public sealed class PortfolioChangeMessage : BaseChangeMessage<PositionChangeTypes>
	{
		/// <summary>
		/// Название портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Код электронной площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Создать <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		public PortfolioChangeMessage()
			: base(MessageTypes.PortfolioChange)
		{
		}

		/// <summary>
		/// Создать копию объекта.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var msg = new PortfolioChangeMessage
			{
				LocalTime = LocalTime,
				PortfolioName = PortfolioName,
				BoardCode = BoardCode,
				ServerTime = ServerTime
			};

			msg.Changes.AddRange(Changes);
			this.CopyExtensionInfo(msg);

			return msg;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",P={0},Changes={1}".Put(PortfolioName, Changes.Select(c => c.ToString()).Join(","));
		}
	}
}