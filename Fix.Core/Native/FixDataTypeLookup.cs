namespace StockSharp.Fix.Native;

/// <summary>
/// Data from DataTypeLookup FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="Exchange">Exchange code.</param>
/// <param name="MdEntryType">Market data entry type (0=Bid, 1=Offer, 2=Trade, etc.).</param>
/// <param name="MdEntryArg">Additional market data argument.</param>
/// <param name="Format">Data format identifier.</param>
/// <param name="IncludeDates">Whether to include available date ranges.</param>
public record struct FixDataTypeLookup(
	FixId MdReqId,
	string Symbol,
	string Exchange,
	char? MdEntryType,
	string MdEntryArg,
	int? Format,
	bool IncludeDates);
