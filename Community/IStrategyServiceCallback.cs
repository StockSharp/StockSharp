#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IStrategyServiceCallback.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the feedback service <see cref="IStrategyService"/>.
	/// </summary>
	[ServiceContract]
	public interface IStrategyServiceCallback
	{
		/// <summary>
		/// A new strategy is created.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		[OperationContract(IsOneWay = true)]
		void Created(StrategyData strategy);

		/// <summary>
		/// The strategy is removed.
		/// </summary>
		/// <param name="strategyId">The strategy identifier.</param>
		[OperationContract(IsOneWay = true)]
		void Deleted(long strategyId);

		/// <summary>
		/// The strategy update.
		/// </summary>
		/// <param name="strategy">The strategy data.</param>
		[OperationContract(IsOneWay = true)]
		void Updated(StrategyData strategy);

		/// <summary>
		/// The user subscribed to the strategy. To be send to the user who created the strategy via <see cref="IStrategyService.CreateStrategy"/>.
		/// </summary>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <param name="userId">User id.</param>
		[OperationContract(IsOneWay = true)]
		void Subscribed(long strategyId, long userId);

		/// <summary>
		/// The user unsubscribed from the strategy. To be send to the user who created the strategy via <see cref="IStrategyService.CreateStrategy"/>.
		/// </summary>
		/// <param name="strategyId">The strategy identifier.</param>
		/// <param name="userId">User id.</param>
		[OperationContract(IsOneWay = true)]
		void UnSubscribed(long strategyId, long userId);
	}
}