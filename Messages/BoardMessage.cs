namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение, содержащее данные об электронной площадке.
	/// </summary>
	public class BoardMessage : Message
	{
		/// <summary>
		/// Код биржи, которой принадлежит прощадка. Может совпадать с <see cref="Code"/>, если площадка и биржа является одним целым.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StockExchangeKey)]
		[DescriptionLoc(LocalizedStrings.Str56Key)]
		[MainCategory]
		public string ExchangeCode { get; set; }

		/// <summary>
		/// Код площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string Code { get; set; }

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через <see cref="OrderReplaceMessage"/> в виде одной транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		[MainCategory]
		public bool IsSupportAtomicReRegister { get; set; }

		/// <summary>
		/// Поддерживается ли рыночный тип заявок <see cref="OrderTypes.Market"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		[MainCategory]
		public bool IsSupportMarketOrders { get; set; }

		/// <summary>
		/// Время экспирации инструментов.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str64Key)]
		[MainCategory]
		public TimeSpan ExpiryTime { get; set; }

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Время работы площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.WorkingTimeKey)]
		[DescriptionLoc(LocalizedStrings.WorkingHoursKey)]
		[MainCategory]
		public WorkingTime WorkingTime
		{
			get { return _workingTime; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (WorkingTime == value)
					return;

				_workingTime = value;
			}
		}

		[field: NonSerialized]
		private TimeZoneInfo _timeZoneInfo = TimeZoneInfo.Utc;

		/// <summary>
		/// Информация о временной зоне, где находится биржа.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TimeZoneKey)]
		[DescriptionLoc(LocalizedStrings.Str68Key)]
		[MainCategory]
		public TimeZoneInfo TimeZoneInfo
		{
			get { return _timeZoneInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (TimeZoneInfo == value)
					return;

				_timeZoneInfo = value;
			}
		}

		/// <summary>
		/// Создать <see cref="BoardMessage"/>.
		/// </summary>
		public BoardMessage()
			: base(MessageTypes.Board)
		{
		}

		/// <summary>
		/// Создать копию объекта.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new BoardMessage
			{
				Code = Code,
				ExchangeCode = ExchangeCode,
				ExpiryTime = ExpiryTime,
				IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				IsSupportMarketOrders = IsSupportMarketOrders,
				WorkingTime = WorkingTime.Clone(),
				TimeZoneInfo = TimeZoneInfo,
			};
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Code={0},Ex={1}".Put(Code, ExchangeCode);
		}
	}
}