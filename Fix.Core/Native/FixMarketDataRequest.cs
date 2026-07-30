namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data from MarketDataRequest FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="MdResponseId">Market data response identifier (for unsubscribe).</param>
/// <param name="SubscriptionRequestType">Subscription request type (0=Snapshot, 1=Subscribe, 2=Unsubscribe).</param>
/// <param name="MdEntryTypes">Market data entry types (0=Bid, 1=Offer, 2=Trade, etc.).</param>
/// <param name="MdEntryArgs">Market data entry arguments (e.g., timeframe for candles).</param>
/// <param name="SecurityIds">Security identifiers.</param>
/// <param name="SecurityTypes">Security types for each security.</param>
/// <param name="MarketDepth">Market depth (0 = full depth).</param>
/// <param name="FromDate">Start date for historical data.</param>
/// <param name="ToDate">End date for historical data.</param>
/// <param name="AllowBuildFromSmallerTimeFrame">Allow building candles from smaller timeframes.</param>
/// <param name="IsCalcVolumeProfile">Calculate volume profile.</param>
/// <param name="IsFinishedOnly">Return only finished candles.</param>
/// <param name="IsRegularTradingHours">Regular trading hours only.</param>
/// <param name="BuildMode">Build mode for market data.</param>
/// <param name="BuildFrom">Build from data type.</param>
/// <param name="BuildField">Build field for candles.</param>
/// <param name="Skip">Number of records to skip (pagination).</param>
/// <param name="Count">Number of records to return (pagination).</param>
/// <param name="Fields">Level1 fields to request.</param>
public record struct FixMarketDataRequest(
	FixId MdReqId,
	FixId MdResponseId,
	char? SubscriptionRequestType,
	char[] MdEntryTypes,
	string[] MdEntryArgs,
	SecurityId[] SecurityIds,
	string[] SecurityTypes,
	int? MarketDepth,
	DateTime? FromDate,
	DateTime? ToDate,
	bool? AllowBuildFromSmallerTimeFrame,
	bool? IsCalcVolumeProfile,
	bool? IsFinishedOnly,
	bool? IsRegularTradingHours,
	int? BuildMode,
	char? BuildFrom,
	char? BuildField,
	long? Skip,
	long? Count,
	Level1Fields[] Fields);
