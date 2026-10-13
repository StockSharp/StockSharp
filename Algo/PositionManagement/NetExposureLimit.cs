namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// How far the uncovered net position in one instrument may run before <see cref="NetExposureHedger"/>
/// closes it.
/// </summary>
/// <remarks>
/// The two caps are read together: whichever of them is crossed closes the whole uncovered position,
/// so <see cref="MaxNotional"/> caps <see cref="MaxVolume"/> rather than replacing it.
/// </remarks>
public sealed class NetExposureLimit
{
	/// <summary>
	/// The absolute volume of the uncovered position that may be carried. Anything past it is closed.
	/// </summary>
	public decimal MaxVolume { get; set; }

	/// <summary>
	/// What the uncovered position may be worth before it is closed, or <see langword="null"/> when
	/// the instrument is capped by volume alone. Answers only while a price is known.
	/// </summary>
	public decimal? MaxNotional { get; set; }
}
