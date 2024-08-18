namespace StockSharp.Algo.PnL;

/// <summary>
/// Information on trade, its closed volume and its profitability.
/// </summary>
public class PnLInfo
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PnLInfo"/>.
	/// </summary>
	/// <param name="serverTime"><see cref="ServerTime"/>.</param>
	/// <param name="closedVolume">The volume of position, which was closed by own trade.</param>
	/// <param name="pnL">The profit, realized by this trade.</param>
	public PnLInfo(DateTimeOffset serverTime, decimal closedVolume, decimal pnL)
	{
		if (closedVolume < 0)
			throw new ArgumentOutOfRangeException(nameof(closedVolume), closedVolume, LocalizedStrings.InvalidValue);

		ServerTime = serverTime;
		ClosedVolume = closedVolume;
		PnL = pnL;
	}

	/// <summary>
	/// Time.
	/// </summary>
	public DateTimeOffset ServerTime { get; }

	/// <summary>
	/// The volume of position, which was closed by own trade.
	/// </summary>
	/// <remarks>
	/// For example, in strategy position was 2. The trade for -5 contracts. Closed position 2.
	/// </remarks>
	public decimal ClosedVolume { get; }

	/// <summary>
	/// The profit, realized by this trade.
	/// </summary>
	public decimal PnL { get; }
}