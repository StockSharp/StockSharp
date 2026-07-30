namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for MarketDataSnapshotFullRefresh FIX message.
/// </summary>
/// <param name="SecurityId">Security identifier.</param>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="BuildFrom">Data build source.</param>
/// <param name="Entries">Market data entries.</param>
public record struct FixMarketDataSnapshotFullRefresh(
	SecurityId SecurityId,
	string MdReqId,
	DataType BuildFrom,
	ICollection<MDEntry> Entries);
