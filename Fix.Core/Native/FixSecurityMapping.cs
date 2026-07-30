namespace StockSharp.Fix.Native;

/// <summary>
/// Data from SecurityMapping FIX message.
/// </summary>
/// <param name="IdSource">Primary identifier source.</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecExchange">Security exchange code.</param>
/// <param name="AltId">Alternative identifier value.</param>
/// <param name="AltIdSource">Alternative identifier source.</param>
/// <param name="Action">Mapping action (A=Add, D=Delete).</param>
/// <param name="MdReqId">Market data request identifier.</param>
public record struct FixSecurityMapping(
	string IdSource,
	string Symbol,
	string SecExchange,
	string AltId,
	string AltIdSource,
	char? Action,
	FixId MdReqId);
