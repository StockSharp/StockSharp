namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий сервис магазина стратегий.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/strategyservice.svc", CallbackContract = typeof(IStrategyServiceCallback))]
	public interface IStrategyService
	{
		/// <summary>
		/// Добавить стратегию в магазин.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategy">Данные о стратегии.</param>
		/// <returns>Идентификатор стратегии.</returns>
		long CreateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// Обновить стратегию в магазине.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategy">Данные о стратегии.</param>
		void UpdateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// Удалить стратегию из магазина.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		void DeleteStrategy(Guid sessionId, long strategyId);

		/// <summary>
		/// Получить все идентификаторы стратегий.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификаторы стратегий.</returns>
		IEnumerable<long> GetStrategies(Guid sessionId);

		/// <summary>
		/// Получить идентификаторы стратегий, подписанные через <see cref="Subscribe"/>.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификаторы стратегий.</returns>
		IEnumerable<long> GetSubscribedStrategies(Guid sessionId);

		/// <summary>
		/// Получить название и описание стратегий.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategyIds">Идентификаторы стратегий.</param>
		/// <returns>Информация о стратегиях.</returns>
		IEnumerable<StrategyData> GetLiteInfo(Guid sessionId, long[] strategyIds);

		/// <summary>
		/// Получить полное описание стратегии, включая исходный и исполняемый коды.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		/// <returns>Информация о стратегии.</returns>
		StrategyData GetFullInfo(Guid sessionId, long strategyId);

		/// <summary>
		/// Подписаться на стратегию.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		void Subscribe(Guid sessionId, long strategyId);

		/// <summary>
		/// Отписаться от стратегии.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		void UnSubscribe(Guid sessionId, long strategyId);
	}
}