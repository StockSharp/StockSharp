namespace StockSharp.Algo
{
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Корзина позиций, которые принадлежат <see cref="BasketPortfolio"/>.
	/// </summary>
	public abstract class BasketPosition : Position
	{
		/// <summary>
		/// Позиции, из которых создана данная корзина.
		/// </summary>
		public abstract IEnumerable<Position> InnerPositions { get; }
	}
}