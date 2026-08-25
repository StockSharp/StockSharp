namespace StockSharp.MatchingEngine;

/// <summary>
/// Settings for <see cref="MatchingEngineAdapter"/>.
/// </summary>
public class MatchingEngineSettings
{
	/// <summary>
	/// Check money balance before order registration.
	/// </summary>
	public bool CheckMoney { get; set; }

	/// <summary>
	/// Check trading state (session hours).
	/// </summary>
	public bool CheckTradingState { get; set; }

	/// <summary>
	/// The number, starting at which identifiers for orders will be generated.
	/// </summary>
	public long InitialOrderId { get; set; }

	/// <summary>
	/// The number, starting at which identifiers for trades will be generated.
	/// </summary>
	public long InitialTradeId { get; set; }

	/// <summary>
	/// Extend the book past its worst level when an order asks for more volume than the market
	/// holds, so that the order can be filled in full.
	/// </summary>
	/// <remarks>
	/// The levels this adds were never quoted by anyone: it is emulation, for replaying history
	/// where an order has to go through whatever the record shows. A venue matching real orders
	/// leaves it off, and an order larger than the market is filled by as much market as there is.
	/// </remarks>
	public bool IncreaseDepthVolume { get; set; }



}
