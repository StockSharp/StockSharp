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
	public interface IMessageAdapter : IMessageChannel, IPersistable, ILogReceiver
	{
		/// <summary>
		/// Transaction id generator.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// Possible supported by adapter message types.
		/// </summary>
		IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; set; }

		/// <summary>
		/// Supported by adapter message types.
		/// </summary>
		IEnumerable<MessageTypes> SupportedInMessages { get; set; }

		/// <summary>
		/// Supported by adapter message types.
		/// </summary>
		IEnumerable<MessageTypes> SupportedOutMessages { get; set; }

		/// <summary>
		/// Supported by adapter result message types.
		/// </summary>
		IEnumerable<MessageTypes> SupportedResultMessages { get; set; }

		/// <summary>
		/// Supported by adapter market data types.
		/// </summary>
		IEnumerable<DataType> SupportedMarketDataTypes { get; set; }

		/// <summary>
		/// Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <summary>
		/// Possible options for candles building.
		/// </summary>
		IEnumerable<Level1Fields> CandlesBuildFrom { get; }

		/// <summary>
		/// Check possible time-frame by request.
		/// </summary>
		bool CheckTimeFrameByRequest { get; }

		/// <summary>
		/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
		/// </summary>
		ReConnectionSettings ReConnectionSettings { get; }

		/// <summary>
		///  Server check interval for track the connection alive. The value is <see cref="TimeSpan.Zero"/> turned off tracking.
		/// </summary>
		TimeSpan HeartbeatInterval { get; set; }

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
		/// Names of extended security fields in <see cref="SecurityMessage"/>.
		/// </summary>
		IEnumerable<Tuple<string, Type>> SecurityExtendedFields { get; }

		/// <summary>
		/// Available options for <see cref="MarketDataMessage.MaxDepth"/>.
		/// </summary>
		IEnumerable<int> SupportedOrderBookDepths { get; }

		/// <summary>
		/// Adapter translates incremental order books.
		/// </summary>
		bool IsSupportOrderBookIncrements { get; }

		/// <summary>
		/// Adapter fills <see cref="ExecutionMessage.PnL"/>.
		/// </summary>
		bool IsSupportExecutionsPnL { get; }

		/// <summary>
		/// Adapter provides news related with specified security.
		/// </summary>
		bool IsSecurityNewsOnly { get; }

		/// <summary>
		/// Type of <see cref="OrderCondition"/>.
		/// </summary>
		/// <remarks>
		/// If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.
		/// </remarks>
		Type OrderConditionType { get; }

		/// <summary>
		/// Start sending <see cref="TimeMessage"/> before connection established.
		/// </summary>
		bool HeartbeatBeforConnect { get; }

		/// <summary>
		/// Icon.
		/// </summary>
		Uri Icon { get; }

		/// <summary>
		/// Send auto response for <see cref="OrderStatusMessage"/> and <see cref="PortfolioLookupMessage"/> unsubscribes.
		/// </summary>
		bool IsAutoReplyOnTransactonalUnsubscription { get; }

		/// <summary>
		/// Create market depth builder.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Order log to market depth builder.</returns>
		IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId);

		/// <summary>
		/// Get possible args for the specified candle type and instrument.
		/// </summary>
		/// <param name="candleType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Possible args.</returns>
		IEnumerable<object> GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to);

		/// <summary>
		/// Get maximum size step allowed for historical download.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="iterationInterval">Interval between iterations.</param>
		/// <returns>Step.</returns>
		TimeSpan GetHistoryStepSize(DataType dataType, out TimeSpan iterationInterval);

		/// <summary>
		/// Is for the specified <paramref name="dataType"/> all securities downloading enabled.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Check result.</returns>
		bool IsAllDownloadingSupported(DataType dataType);

		/// <summary>
		/// Support filtering subscriptions (subscribe/unsubscribe for specified security).
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Check result.</returns>
		bool IsSecurityRequired(DataType dataType);
	}
}