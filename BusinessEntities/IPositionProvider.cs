namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The position provider interface.
	/// </summary>
	public interface IPositionProvider
	{
		/// <summary>
		/// Get all positions.
		/// </summary>
		IEnumerable<Position> Positions { get; }

		/// <summary>
		/// New position received.
		/// </summary>
		event Action<Position> NewPosition;

		/// <summary>
		/// Position changed.
		/// </summary>
		event Action<Position> PositionChanged;
	}
}