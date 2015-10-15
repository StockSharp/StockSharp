namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for the position calculation manager.
	/// </summary>
	public interface IPositionManager
	{
		/// <summary>
		/// The position aggregate value.
		/// </summary>
		decimal Position { get; set; }

		/// <summary>
		/// Positions, grouped by instruments and portfolios.
		/// </summary>
		IEnumerable<Position> Positions { get; set; }

		/// <summary>
		/// The event of new position occurrence in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		event Action<Position> NewPosition;

		/// <summary>
		/// The event of position change in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		event Action<Position> PositionChanged;

		/// <summary>
		/// To null position.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate position by the order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>The position by the order.</returns>
		decimal ProcessOrder(Order order);

		/// <summary>
		/// To calculate the position by the trade.
		/// </summary>
		/// <param name="trade">Trade.</param>
		/// <returns>The position by the trade.</returns>
		decimal ProcessMyTrade(MyTrade trade);
	}
}