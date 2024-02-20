namespace StockSharp.Algo.Strategies.Protective
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The loss protection strategy.
	/// </summary>
	[Obsolete("Use ProtectiveController class.")]
	public class StopLossStrategy : ProtectiveStrategy
	{
		/// <summary>
		/// To create a strategy <see cref="StopLossStrategy"/>.
		/// </summary>
		/// <param name="trade">Protected position.</param>
		/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from the protected trade <paramref name="trade" /> is specified.</param>
		public StopLossStrategy(MyTrade trade, Unit protectiveLevel)
			: this(trade.Order.Side, trade.Trade.Price, trade.Trade.Volume, protectiveLevel)
		{
		}

		/// <summary>
		/// To create a strategy <see cref="StopLossStrategy"/>.
		/// </summary>
		/// <param name="protectiveSide">Protected position side.</param>
		/// <param name="protectivePrice">Protected position price.</param>
		/// <param name="protectiveVolume">The protected position volume.</param>
		/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from <paramref name="protectivePrice" /> is specified.</param>
		public StopLossStrategy(Sides protectiveSide, decimal protectivePrice, decimal protectiveVolume, Unit protectiveLevel)
			: base(protectiveSide, protectivePrice, protectiveVolume, protectiveLevel, protectiveSide == Sides.Sell)
		{
		}
	}
}