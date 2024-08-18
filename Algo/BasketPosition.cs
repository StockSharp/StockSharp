namespace StockSharp.Algo;

/// <summary>
/// The basket with positions which belong to <see cref="BasketPortfolio"/>.
/// </summary>
public abstract class BasketPosition : Position
{
	/// <summary>
	/// Positions from which this basket is created.
	/// </summary>
	public abstract IEnumerable<Position> InnerPositions { get; }
}