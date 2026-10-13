namespace StockSharp.MatchingEngine;

/// <summary>
/// Position information for a security.
/// </summary>
public class PositionInfo(SecurityId securityId)
{
	/// <summary>
	/// Security ID.
	/// </summary>
	public SecurityId SecurityId { get; } = securityId;

	/// <summary>
	/// Begin value (initial position).
	/// </summary>
	public decimal BeginValue { get; set; }

	/// <summary>
	/// Position change since begin.
	/// </summary>
	public decimal Diff { get; set; }

	/// <summary>
	/// Current position value.
	/// </summary>
	public decimal CurrentValue => BeginValue + Diff;

	/// <summary>
	/// Average entry price.
	/// </summary>
	public decimal AveragePrice { get; set; }

	/// <summary>
	/// Total volume of active buy orders.
	/// </summary>
	public decimal TotalBidsVolume { get; set; }

	/// <summary>
	/// Total volume of active sell orders.
	/// </summary>
	public decimal TotalAsksVolume { get; set; }

	/// <summary>
	/// Total value of active buy orders (volume * price).
	/// </summary>
	public decimal TotalBidsValue { get; set; }

	/// <summary>
	/// Total value of active sell orders (volume * price).
	/// </summary>
	public decimal TotalAsksValue { get; set; }

	/// <summary>
	/// Margin leverage for this position. Null means default (1x).
	/// </summary>
	public decimal? Leverage { get; set; }
}
