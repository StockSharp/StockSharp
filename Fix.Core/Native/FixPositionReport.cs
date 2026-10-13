namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for PositionReport FIX message.
/// </summary>
/// <param name="PosReqId">Position request identifier.</param>
/// <param name="Account">Account identifier.</param>
/// <param name="Symbol">Security symbol.</param>
/// <param name="SecurityExchange">Security exchange code.</param>
/// <param name="SecurityType">Security type.</param>
/// <param name="LimitType">T+ limit type.</param>
/// <param name="ClientCode">Client code.</param>
/// <param name="Currency">Currency.</param>
/// <param name="StrategyId">Strategy identifier.</param>
/// <param name="Side">Position side.</param>
/// <param name="BuildFrom">Data build source.</param>
/// <param name="DepoName">Depository name.</param>
/// <param name="Description">Position description.</param>
/// <param name="Changes">Position changes collection.</param>
/// <param name="TransactTime">Server time at which the position was reported (FIX tag 60).</param>
public record struct FixPositionReport(
	FixId PosReqId,
	string Account,
	string Symbol,
	string SecurityExchange,
	SecurityTypes? SecurityType,
	TPlusLimits? LimitType,
	string ClientCode,
	CurrencyTypes? Currency,
	string StrategyId,
	Sides? Side,
	DataType BuildFrom,
	string DepoName,
	string Description,
	ICollection<KeyValuePair<PositionChangeTypes, object>> Changes,
	DateTime? TransactTime = null)
{
	/// <summary>
	/// Owning portfolio's numeric PK (broker extension, distinct from the display
	/// <see cref="Account"/>). Stamped from the message side-channel and appended last on the
	/// wire so older readers ignore it; lets a WS client learn its portfolio id from the feed
	/// itself instead of resolving the shared display name. 0 when unknown.
	/// </summary>
	public long PortfolioId { get; set; }
}
