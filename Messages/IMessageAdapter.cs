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
	/// Types of <see cref="OrderCancelMessage.Volume"/> required to cancel orders.
	/// </summary>
	public enum OrderCancelVolumeRequireTypes
	{
		/// <summary>
		/// Non filled balance.
		/// </summary>
		Balance,

		/// <summary>
		/// Initial volume.
		/// </summary>
		Volume
	}

	/// <summary>
	/// Base message adapter interface which convert messages <see cref="Message"/> to native commands and back.
	/// </summary>
	public interface IMessageAdapter : IMessageChannel, IPersistable, ILogReceiver, IMessageAdapterExtension
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
		/// Supported by adapter market data types.
		/// </summary>
		MarketDataTypes[] SupportedMarketDataTypes { get; set; }

		/// <summary>
		/// Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <summary>
		/// Possible time-frames.
		/// </summary>
		IEnumerable<TimeSpan> TimeFrames { get; }

		/// <summary>
		/// Check possible time-frame by request.
		/// </summary>
		bool CheckTimeFrameByRequest { get; set; }

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
		/// <see cref="OrderStatusMessage"/> required to get orders and own trades.
		/// </summary>
		bool OrderStatusRequired { get; }

		/// <summary>
		/// The storage name, associated with the adapter.
		/// </summary>
		string StorageName { get; }

		/// <summary>
		/// Native identifier can be stored.
		/// </summary>
		bool IsNativeIdentifiersPersistable { get; }

		/// <summary>
		/// Identify security in messages by native identifier <see cref="SecurityId.Native"/>.
		/// </summary>
		bool IsNativeIdentifiers { get; }

		/// <summary>
		/// Translates <see cref="CandleMessage"/> as only fully filled.
		/// </summary>
		bool IsFullCandlesOnly { get; }

		/// <summary>
		/// Support any subscriptions (ticks, order books etc.).
		/// </summary>
		bool IsSupportSubscriptions { get; }

		/// <summary>
		/// Support filtering subscriptions (subscribe/unsubscribe for specified security).
		/// </summary>
		bool IsSupportSubscriptionBySecurity { get; }

		/// <summary>
		/// Support portfolio subscriptions.
		/// </summary>
		bool IsSupportSubscriptionByPortfolio { get; }

		/// <summary>
		/// Support candles subscription and live updates.
		/// </summary>
		bool IsSupportCandlesUpdates { get; }

		/// <summary>
		/// Message adapter categories.
		/// </summary>
		MessageAdapterCategories Categories { get; }

		/// <summary>
		/// <see cref="OrderCancelMessage.Volume"/> required to cancel orders.
		/// </summary>
		OrderCancelVolumeRequireTypes? OrderCancelVolumeRequired { get; }

		/// <summary>
		/// Board code for combined security.
		/// </summary>
		string AssociatedBoardCode { get; }

		/// <summary>
		/// Names of extended security fields in <see cref="SecurityMessage"/>.
		/// </summary>
		Tuple<string, Type>[] SecurityExtendedFields { get; }

		/// <summary>
		/// Support lookup all securities.
		/// </summary>
		bool IsSupportSecuritiesLookupAll { get; }

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

		/// <summary>
		/// Get possible time-frames for the specified instrument.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Possible time-frames.</returns>
		IEnumerable<TimeSpan> GetTimeFrames(SecurityId securityId);
	}
}