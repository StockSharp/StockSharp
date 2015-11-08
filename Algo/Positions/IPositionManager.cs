namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

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
		IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> Positions { get; set; }

		/// <summary>
		/// The event of new position occurrence in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> NewPosition;

		/// <summary>
		/// The event of position change in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> PositionChanged;

		/// <summary>
		/// To null position.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate position.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The position by order or trade.</returns>
		decimal? ProcessMessage(Message message);
	}
}