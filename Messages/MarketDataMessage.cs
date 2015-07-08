namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Типы маркет-данных.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum MarketDataTypes
	{
		/// <summary>
		/// Первый уровень маркет-данных.
		/// </summary>
		[EnumMember]
		Level1,

		/// <summary>
		/// Глубина рынка (стаканы).
		/// </summary>
		[EnumMember]
		MarketDepth,

		/// <summary>
		/// Тиковые сделки.
		/// </summary>
		[EnumMember]
		Trades,

		/// <summary>
		/// Лог заявок.
		/// </summary>
		[EnumMember]
		OrderLog,

		/// <summary>
		/// Новости.
		/// </summary>
		[EnumMember]
		News,

		/// <summary>
		/// Свечи (тайм-фрейм).
		/// </summary>
		[EnumMember]
		CandleTimeFrame,

		/// <summary>
		/// Свеча (тиковая).
		/// </summary>
		CandleTick,

		/// <summary>
		/// Свеча (объем).
		/// </summary>
		[EnumMember]
		CandleVolume,

		/// <summary>
		/// Свеча (рендж).
		/// </summary>
		[EnumMember]
		CandleRange,

		/// <summary>
		/// Свеча (X&amp;0).
		/// </summary>
		[EnumMember]
		CandlePnF,

		/// <summary>
		/// Свеча (ренко).
		/// </summary>
		[EnumMember]
		CandleRenko,
	}

	/// <summary>
	/// Сообщение о подписке или отписки на маркет-данные (при отправке используется как команда, при получении является событием подтверждения).
	/// </summary>
	[DataContract]
	[Serializable]
	public class MarketDataMessage : SecurityMessage
	{
		/// <summary>
		/// Дата начала, с которой необходимо получать данные.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset From { get; set; }

		/// <summary>
		/// Дата окончания, до которой необходимо получать данные.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset To { get; set; }

		/// <summary>
		/// Тип маркет-данных.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		public MarketDataTypes DataType { get; set; }

		/// <summary>
		/// Дополнительный аргумент для запроса маркет-данных.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str347Key)]
		[DescriptionLoc(LocalizedStrings.Str348Key)]
		[MainCategory]
		public object Arg { get; set; }

		/// <summary>
		/// Является ли сообщение подпиской на маркет-данные.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Идентификатор запроса.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Поддерживает ли торговая система запрашиваемый тип данных. Заполняется в случае ответа.
		/// </summary>
		[DataMember]
		public bool IsNotSupported { get; set; }

		/// <summary>
		/// Информация об ошибке. Сигнализирует об ошибке подписки или отписки. Заполняется в случае ответа.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Количество маркет-данных.
		/// </summary>
		[DataMember]
		public long Count { get; set; }

		/// <summary>
		/// Максимальная глубина стакана. Используется в случае <see cref="DataType"/> равное <see cref="MarketDataTypes.MarketDepth"/>.
		/// </summary>
		[DataMember]
		public int MaxDepth { get; set; }

		/// <summary>
		/// Идентификатор новости. Используется, если идет запрос получения текста новости.
		/// </summary>
		[DataMember]
		public string NewsId { get; set; }

		/// <summary>
		/// Максимальная глубина стакана по-умолчанию.
		/// </summary>
		public const int DefaultMaxDepth = 50;

		/// <summary>
		/// Создать <see cref="MarketDataMessage"/>.
		/// </summary>
		public MarketDataMessage()
			: base(MessageTypes.MarketData)
		{
		}

		/// <summary>
		/// Инициализировать <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <param name="type">Тип сообщения.</param>
		protected MarketDataMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new MarketDataMessage
			{
				Arg = Arg,
				DataType = DataType,
				Error = Error,
				From = From,
				To = To,
				IsSubscribe = IsSubscribe,
				TransactionId = TransactionId,
				Count = Count,
				MaxDepth = MaxDepth,
				NewsId = NewsId,
				LocalTime = LocalTime,
				IsNotSupported = IsNotSupported
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
			return base.ToString() + ",Sec={0},Types={1},IsSubscribe={2},TransId={3},OrigId={4}".Put(SecurityId, DataType, IsSubscribe, TransactionId, OriginalTransactionId);
		}
	}
}