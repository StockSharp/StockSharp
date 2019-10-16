namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

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

		/// <summary>
		/// To get the position by portfolio and instrument.
		/// </summary>
		/// <param name="portfolio">The portfolio on which the position should be found.</param>
		/// <param name="security">The instrument on which the position should be found.</param>
		/// <param name="clientCode">The client code.</param>
		/// <param name="depoName">The depository name where the stock is located physically. By default, an empty string is passed, which means the total position by all depositories.</param>
		/// <returns>Position.</returns>
		Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "");

		/// <summary>
		/// Subscribe on positions changes.
		/// </summary>
		/// <param name="security">Security for subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Max count.</param>
		/// <param name="adapter">Target adapter. Can be <see langword="null" />.</param>
		void SubscribePositions(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null);

		/// <summary>
		/// Unsubscribe from positions changes.
		/// </summary>
		void UnSubscribePositions();
	}
}