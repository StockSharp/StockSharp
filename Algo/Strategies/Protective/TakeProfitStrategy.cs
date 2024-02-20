namespace StockSharp.Algo.Strategies.Protective
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Profit protection strategy.
	/// </summary>
	[Obsolete("Use ProtectiveController class.")]
	public class TakeProfitStrategy : ProtectiveStrategy
	{
		/// <summary>
		/// To create a strategy <see cref="TakeProfitStrategy"/>.
		/// </summary>
		/// <param name="trade">Protected position.</param>
		/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from the protected trade <paramref name="trade" /> is specified.</param>
		public TakeProfitStrategy(MyTrade trade, Unit protectiveLevel)
			: this(trade.Order.Side, trade.Trade.Price, trade.Trade.Volume, protectiveLevel)
		{
		}

		/// <summary>
		/// To create a strategy <see cref="TakeProfitStrategy"/>.
		/// </summary>
		/// <param name="protectiveSide">Protected position side.</param>
		/// <param name="protectivePrice">Protected position price.</param>
		/// <param name="protectiveVolume">The protected position volume.</param>
		/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from <paramref name="protectivePrice" /> is specified.</param>
		public TakeProfitStrategy(Sides protectiveSide, decimal protectivePrice, decimal protectiveVolume, Unit protectiveLevel)
			: base(protectiveSide, protectivePrice, protectiveVolume, protectiveLevel, protectiveSide == Sides.Buy)
		{
		}
	}
}