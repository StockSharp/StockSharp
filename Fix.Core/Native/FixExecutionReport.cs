namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for ExecutionReport FIX message.
/// </summary>
/// <param name="OrdStatusReqId">Order status request identifier.</param>
/// <param name="MassStatusReqId">Mass status request identifier.</param>
/// <param name="OrigClOrdId">Original client order identifier.</param>
/// <param name="ClOrdId">Client order identifier.</param>
/// <param name="ExecMsg">Execution message with order/trade data.</param>
public record struct FixExecutionReport(
	FixId OrdStatusReqId,
	FixId MassStatusReqId,
	FixId OrigClOrdId,
	FixId ClOrdId,
	ExecutionMessage ExecMsg)
{
	/// <summary>
	/// Owning user's numeric id (broker extension, not a standard FIX tag). Carried over
	/// the SBE wire so the OrderRouter can fan a stop/algo report out to the owning client
	/// by UserId — robust across restarts and immune to portfolio-name collisions.
	/// </summary>
	public long UserId { get; set; }

	/// <summary>
	/// Canonical portfolio identity — the broker's <c>BrokerPortfolio</c> PK (broker extension,
	/// not a standard FIX tag). Carried over the SBE wire so a stop/algo report's portfolio FK
	/// is bound by id rather than the shared display name.
	/// </summary>
	public long PortfolioId { get; set; }
}
