#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Latency.Algo
File: ILatencyManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Latency
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The interface of the order registration delay calculation manager.
	/// </summary>
	public interface ILatencyManager : IPersistable
	{
		/// <summary>
		/// To zero calculations.
		/// </summary>
		void Reset();

		/// <summary>
		/// The aggregate value of registration delay by all orders.
		/// </summary>
		TimeSpan LatencyRegistration { get; }

		/// <summary>
		/// The aggregate value of cancelling delay by all orders.
		/// </summary>
		TimeSpan LatencyCancellation { get; }

		/// <summary>
		/// To process the message for transaction delay calculation. Messages of <see cref="OrderRegisterMessage"/>, <see cref="OrderReplaceMessage"/>, <see cref="OrderCancelMessage"/> and <see cref="ExecutionMessage"/> types are accepted.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The transaction delay. If it is impossible to calculate delay, <see langword="null" /> will be returned.</returns>
		TimeSpan? ProcessMessage(Message message);
	}
}