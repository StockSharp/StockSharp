#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Base message adapter interface which convert messages <see cref="Message"/> to native commands and back.
	/// </summary>
	public interface IMessageAdapter : IMessageChannel, IPersistable, ILogReceiver
	{
		/// <summary>
		/// Transaction id generator.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// Supported by adapter message types.
		/// </summary>
		MessageTypes[] SupportedMessages { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <summary>
		/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
		/// </summary>
		ReConnectionSettings ReConnectionSettings { get; }

		/// <summary>
		/// Lifetime ping interval.
		/// </summary>
		TimeSpan HeartbeatInterval { get; set; }

		/// <summary>
		/// <see cref="PortfolioLookupMessage"/> required to get portfolios and positions.
		/// </summary>
		bool PortfolioLookupRequired { get; }

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		bool SecurityLookupRequired { get; }

		/// <summary>
		/// <see cref="OrderStatusMessage"/> required to get orders and ow trades.
		/// </summary>
		bool OrderStatusRequired { get; }

		/// <summary>
		/// <see cref="OrderCancelMessage.Volume"/> required to cancel orders.
		/// </summary>
		bool OrderCancelVolumeRequired { get; }

		/// <summary>
		/// Board code for combined security.
		/// </summary>
		string AssociatedBoardCode { get; }

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		OrderCondition CreateOrderCondition();

		/// <summary>
		/// Check the connection is alive. Uses only for connected states.
		/// </summary>
		/// <returns><see langword="true" />, is the connection still alive, <see langword="false" />, if the connection was rejected.</returns>
		bool IsConnectionAlive();

		/// <summary>
		/// Create market depth builder.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Order log to market depth builder.</returns>
		IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId);
	}
}