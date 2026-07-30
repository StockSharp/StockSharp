namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data from SecurityListRequest FIX message.
/// </summary>
/// <param name="SecurityReqId">Security list request identifier.</param>
/// <param name="Currency">Currency filter.</param>
/// <param name="SecurityDesc">Security description filter.</param>
/// <param name="Strike">Strike price filter (for options).</param>
/// <param name="SecurityType">Security type filter.</param>
/// <param name="SecurityTypes">Multiple security types filter.</param>
/// <param name="CfiCode">CFI code filter.</param>
/// <param name="Symbol">Security symbol filter.</param>
/// <param name="SecurityExchange">Security exchange filter.</param>
/// <param name="Text">Free text filter.</param>
/// <param name="RedemptionDate">Redemption date filter (for bonds).</param>
/// <param name="PutOrCall">Put or call indicator (for options: 0=Put, 1=Call).</param>
/// <param name="Skip">Number of records to skip (pagination).</param>
/// <param name="Count">Number of records to return (pagination).</param>
/// <param name="OnlySecurityId">Return only security identifiers without full info.</param>
/// <param name="SecurityIds">Specific security identifiers to request.</param>
/// <param name="DisableArchive">Whether to exclude archived securities.</param>
public record struct FixSecurityListRequest(
	FixId SecurityReqId,
	string Currency,
	string SecurityDesc,
	decimal? Strike,
	string SecurityType,
	string[] SecurityTypes,
	string CfiCode,
	string Symbol,
	string SecurityExchange,
	string Text,
	DateTime? RedemptionDate,
	int? PutOrCall,
	long? Skip,
	long? Count,
	bool OnlySecurityId,
	SecurityId[] SecurityIds,
	bool DisableArchive);
