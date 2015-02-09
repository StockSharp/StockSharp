namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния портфеля.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum PortfolioStates
	{
		/// <summary>
		/// Активен.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str248Key)]
		Active,
		
		/// <summary>
		/// Заблокирован.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str249Key)]
		Blocked,
	}

	/// <summary>
	/// Сообщение, содержащее данные о портфеле.
	/// </summary>
	public class PortfolioMessage : Message
	{
		/// <summary>
		/// Кодовое название портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Валюта портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str250Key)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Код электронной площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Состояние портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str252Key)]
		[MainCategory]
		public PortfolioStates? State { get; set; }

		/// <summary>
		/// Номер первоначального сообщения <see cref="PortfolioMessage.TransactionId"/>,
		/// для которого данное сообщение является ответом.
		/// </summary>
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Номер транзакции подписки или отписки на изменения портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Является ли сообщение подпиской на изменения портфеля.
		/// </summary>
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Создать <see cref="PortfolioMessage"/>.
		/// </summary>
		public PortfolioMessage()
			: base(MessageTypes.Portfolio)
		{
		}

		/// <summary>
		/// Инициализировать <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected PortfolioMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Name={0}".Put(PortfolioName);
		}

		/// <summary>
		/// Создать копию объекта <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioMessage
			{
				PortfolioName = PortfolioName,
				Currency = Currency,
				BoardCode = BoardCode,
				OriginalTransactionId = OriginalTransactionId,
				IsSubscribe = IsSubscribe,
				State = State,
				TransactionId = TransactionId,
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}
	}
}