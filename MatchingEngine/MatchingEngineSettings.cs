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
	/// To add the additional volume into order book at registering orders with greater volume.
	/// </summary>
	public bool IncreaseDepthVolume { get; set; } = true;

	/// <summary>
	/// The size of spread in price increments for generation of order book from tick trades.
	/// </summary>
	public int SpreadSize { get; set; } = 2;
}
