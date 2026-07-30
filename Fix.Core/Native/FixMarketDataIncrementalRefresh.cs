namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for MarketDataIncrementalRefresh FIX message.
/// </summary>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="BuildFrom">Data build source.</param>
/// <param name="Entries">Market data entries.</param>
public record struct FixMarketDataIncrementalRefresh(
	string Symbol,
	string SecurityExchange,
	string MdReqId,
	DataType BuildFrom,
	ICollection<MDEntry> Entries);
