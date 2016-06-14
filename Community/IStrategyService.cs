#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IStrategyService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the strategy store service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/strategyservice.svc")]
	public interface IStrategyService
	{
		/// <summary>
		/// To add the strategy to the store .
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The strategy identifier.</returns>
		[OperationContract]
		long CreateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// To update the strategy in the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategy">The strategy data.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UpdateStrategy(Guid sessionId, StrategyData strategy);

		/// <summary>
		/// To remove the strategy from the store.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte DeleteStrategy(Guid sessionId, long strategyId);

		/// <summary>
		/// To get all strategies identifiers.
		/// </summary>
		/// <param name="lastCheckTime">Last time of calling the method.</param>
		/// <returns>Strategies identifiers and revisions.</returns>
		[OperationContract]
		IEnumerable<Tuple<long, int>> GetStrategies(DateTime lastCheckTime);

		/// <summary>
		/// To get the name and description of strategies.
		/// </summary>
		/// <param name="strategyIds">Strategies identifiers.</param>
		/// <returns>Information about strategies.</returns>
		[OperationContract]
		IEnumerable<StrategyData> GetDescription(long[] strategyIds);

		/// <summary>
		/// To get the source or executable codes.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <returns>The source or executable codes.</returns>
		[OperationContract]
		StrategyData GetContent(Guid sessionId, long strategyId);

		/// <summary>
		/// To get active subscriptions signed by <see cref="Subscribe"/>.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="lastCheckTime">Last time of calling the method.</param>
		/// <returns>Active subscriptions.</returns>
		[OperationContract]
		IEnumerable<StrategySubscription> GetSubscriptions(Guid sessionId, DateTime lastCheckTime);

		/// <summary>
		/// To subscribe for the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <param name="isAutoRenew">Is auto renewable subscription.</param>
		/// <returns>The strategy subscription.</returns>
		[OperationContract]
		StrategySubscription Subscribe(Guid sessionId, long strategyId, bool isAutoRenew);

		/// <summary>
		/// To unsubscribe from the strategy.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="subscriptionId">The subscription identifier.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UnSubscribe(Guid sessionId, long subscriptionId);
	}
}