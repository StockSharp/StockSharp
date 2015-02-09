namespace StockSharp.Community
{
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий обратную связь сервиса <see cref="IStrategyService"/>.
	/// </summary>
	[ServiceContract]
	public interface IStrategyServiceCallback
	{
		/// <summary>
		/// Создана новая стратегия.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		[OperationContract(IsOneWay = true)]
		void Created(StrategyData strategy);

		/// <summary>
		/// Удалена стратегия.
		/// </summary>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		[OperationContract(IsOneWay = true)]
		void Deleted(long strategyId);

		/// <summary>
		/// Обновление стратегии.
		/// </summary>
		/// <param name="strategy">Данные о стратегии.</param>
		[OperationContract(IsOneWay = true)]
		void Updated(StrategyData strategy);

		/// <summary>
		/// Пользователь подписался на стратегию. Отправляется тому пользователю, кто создал стратегию через <see cref="IStrategyService.CreateStrategy"/>.
		/// </summary>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		/// <param name="userId">Идентификатор пользователя.</param>
		[OperationContract(IsOneWay = true)]
		void Subscribed(long strategyId, long userId);

		/// <summary>
		/// Пользователь отписался от стратегии. Отправляется тому пользователю, кто создал стратегию через <see cref="IStrategyService.CreateStrategy"/>.
		/// </summary>
		/// <param name="strategyId">Идентификатор стратегии.</param>
		/// <param name="userId">Идентификатор пользователя.</param>
		[OperationContract(IsOneWay = true)]
		void UnSubscribed(long strategyId, long userId);
	}
}