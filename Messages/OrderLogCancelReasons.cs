#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	/// <summary>
	/// Reasons for orders cancelling in the orders log.
	/// </summary>
	public enum OrderLogCancelReasons
	{
		/// <summary>
		/// The order re-registration.
		/// </summary>
		ReRegistered,

		/// <summary>
		/// Cancel order.
		/// </summary>
		Canceled,

		/// <summary>
		/// Group canceling of orders.
		/// </summary>
		GroupCanceled,

		/// <summary>
		/// The sign of deletion of order residual due to cross-trade.
		/// </summary>
		CrossTrade,
	}
}