namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Контейнер для исторической сессии, внутри которой происходит обработка сообщений.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str1125Key)]
	public class HistorySessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Дата в истории, с которой необходимо начать эмуляцию.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		private DateTimeOffset _currentTime;

		/// <summary>
		/// Текущее время.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get { return _currentTime; }
		}

		/// <summary>
		/// Установить значение для <see cref="CurrentTime"/>.
		/// </summary>
		/// <param name="currentTime">Новое текущее время.</param>
		public void UpdateCurrentTime(DateTimeOffset currentTime)
		{
			if (currentTime < StartDate || currentTime > StopDate)
				throw new ArgumentOutOfRangeException("currentTime", LocalizedStrings.Str1126Params.Put(currentTime, StartDate, StopDate));

			_currentTime = currentTime;
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new EmulationMessageAdapter(this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new HistoryMessageAdapter(this);
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; private set; }

		/// <summary>
		/// Создать <see cref="HistorySessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public HistorySessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			StartDate = DateTimeOffset.MinValue;
			StopDate = DateTimeOffset.MaxValue;
		}

		/// <summary>
		/// Создать <see cref="HistorySessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		public HistorySessionHolder(IdGenerator transactionIdGenerator, ISecurityProvider securityProvider)
			: this(transactionIdGenerator)
		{
			SecurityProvider = securityProvider;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1127Params.Put(StartDate, StopDate);
		}
	}
}