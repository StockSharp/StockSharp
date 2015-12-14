#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IOrderLogMarketDepthBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	/// <summary>
	/// Base interface for order book builder.
	/// </summary>
	public interface IOrderLogMarketDepthBuilder
	{
		/// <summary>
		/// Market depth.
		/// </summary>
		QuoteChangeMessage Depth { get; }

		/// <summary>
		/// Process order log item.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Order book was changed.</returns>
		bool Update(ExecutionMessage item);
	}
}