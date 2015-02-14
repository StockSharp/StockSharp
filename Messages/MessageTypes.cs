namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Типы сообщениий.
	/// </summary>
	public enum MessageTypes
	{
		/// <summary>
		/// Информация об инструменте.
		/// </summary>
		Security,

		/// <summary>
		/// Изменение level1 маркет-данных.
		/// </summary>
		Level1Change,

		/// <summary>
		/// Регистрация новой заявки.
		/// </summary>
		OrderRegister,

		/// <summary>
		/// Замена заявки на новую.
		/// </summary>
		OrderReplace,

		/// <summary>
		/// Парная замена заявок.
		/// </summary>
		OrderPairReplace,

		/// <summary>
		/// Отмена заявки.
		/// </summary>
		OrderCancel,

		/// <summary>
		/// Отмена группы заявок.
		/// </summary>
		OrderGroupCancel,

		/// <summary>
		/// Изменение времени.
		/// </summary>
		Time,

		/// <summary>
		/// Новость.
		/// </summary>
		News,

		/// <summary>
		/// Ошибка регистрации или снятия заявки.
		/// </summary>
		OrderError,

		/// <summary>
		/// Портфель.
		/// </summary>
		Portfolio,

		/// <summary>
		/// Позиция.
		/// </summary>
		Position,

		/// <summary>
		/// Свеча (тайм-фрейм).
		/// </summary>
		CandleTimeFrame,

		/// <summary>
		/// Изменение котировок.
		/// </summary>
		QuoteChange,

		/// <summary>
		/// Исполнение заявки.
		/// </summary>
		Execution,

		/// <summary>
		/// Изменение позиции.
		/// </summary>
		PositionChange,

		/// <summary>
		/// Изменение портфеля.
		/// </summary>
		PortfolioChange,

		/// <summary>
		/// Подписка/отписка на маркет-данные.
		/// </summary>
		MarketData,

		/// <summary>
		/// Ассоциация <see cref="SecurityId"/> с <see cref="SecurityId.Native"/>.
		/// </summary>
		[Obsolete]
		NativeSecurityId,

		/// <summary>
		/// Подключение.
		/// </summary>
		Connect,

		/// <summary>
		/// Отключение.
		/// </summary>
		Disconnect,

		/// <summary>
		/// Поиск инструментов.
		/// </summary>
		SecurityLookup,

		/// <summary>
		/// Поиск портфелей.
		/// </summary>
		PortfolioLookup,

		/// <summary>
		/// Окончание поиска инструментов.
		/// </summary>
		SecurityLookupResult,

		/// <summary>
		/// Ошибка.
		/// </summary>
		Error,

		/// <summary>
		/// Сессия.
		/// </summary>
		Session,

		/// <summary>
		/// Запросить состояние заявок.
		/// </summary>
		OrderStatus,

		/// <summary>
		/// Информация об электронной площадке.
		/// </summary>
		Board,

		/// <summary>
		/// Окончание поиска портфелей.
		/// </summary>
		PortfolioLookupResult,

		/// <summary>
		/// Изменение пароля.
		/// </summary>
		ChangePassword,

		/// <summary>
		/// Очистить очередь сообщений.
		/// </summary>
		ClearMessageQueue,

		/// <summary>
		/// Свеча (тиковая).
		/// </summary>
		CandleTick,

		/// <summary>
		/// Свеча (объем).
		/// </summary>
		CandleVolume,

		/// <summary>
		/// Свеча (рендж).
		/// </summary>
		CandleRange,

		/// <summary>
		/// Свеча (X&amp;0).
		/// </summary>
		CandlePnF,

		/// <summary>
		/// Свеча (ренко).
		/// </summary>
		CandleRenko,
	}
}