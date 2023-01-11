namespace StockSharp.Algo.Strategies.Protective
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Protective strategy base interface.
	/// </summary>
	public interface IProtectiveStrategy
	{
		/// <summary>
		/// Protected volume.
		/// </summary>
		decimal ProtectiveVolume { get; set; }

		/// <summary>
		/// Protected position price.
		/// </summary>
		decimal ProtectivePrice { get; }

		/// <summary>
		/// Protected position side.
		/// </summary>
		Sides ProtectiveSide { get; }

		/// <summary>
		/// The protected volume change event.
		/// </summary>
		event Action ProtectiveVolumeChanged;
	}
}