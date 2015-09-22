namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the strategy store service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/strategyservice.svc", CallbackContract = typeof(IStrategyServiceCallback))]
	public interface IStrategyService
	{
		/// <summary>
		/// To add the strategy to the store .
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy identifier.</returns>
		long CreateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		void UpdateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		void DeleteStrategy(Guid sessionId, long strategyId);

		/// <summary>
		/// To get all strategies identifiers.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Strategies identifiers.</returns>
		IEnumerable<long> GetStrategies(Guid sessionId);

		/// <summary>
		/// To get strategies identifiers signed by <see cref="Subscribe"/>.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Strategies identifiers.</returns>
		IEnumerable<long> GetSubscribedStrategies(Guid sessionId);

		/// <summary>
		/// To get the name and description of strategies.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyIds">Strategies identifiers.</param>
		/// <returns>Information about strategies.</returns>
		IEnumerable<StrategyData> GetLiteInfo(Guid sessionId, long[] strategyIds);

		/// <summary>
		/// To get the complete description of the strategy, including the source and executable codes.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <returns>Information about the strategy.</returns>
		StrategyData GetFullInfo(Guid sessionId, long strategyId);

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		void Subscribe(Guid sessionId, long strategyId);

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		void UnSubscribe(Guid sessionId, long strategyId);
	}
}