namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Messages;

	/// <summary>
	/// Типы данных, на основе которых должны строиться свечи.
	/// </summary>
	public static class CandleDataTypes
	{
		/// <summary>
		/// Сделки.
		/// </summary>
		public const Level1Fields Trades = Level1Fields.LastTradePrice;

		/// <summary>
		/// Середина спреда.
		/// </summary>
		public const Level1Fields Midpoint = (Level1Fields)(-1);

		/// <summary>
		/// Лучший бид.
		/// </summary>
		public const Level1Fields Bid = Level1Fields.BestBidPrice;

		/// <summary>
		/// Лучший оффер.
		/// </summary>
		public const Level1Fields Ask = Level1Fields.BestAskPrice;

		/// <summary>
		/// Лучшая пара котировок.
		/// </summary>
		public const Level1Fields BidAsk = (Level1Fields)(-2);

		/// <summary>
		/// Волатильность (подразумеваемая).
		/// </summary>
		public const Level1Fields ImpliedVolatility = Level1Fields.ImpliedVolatility;

		/// <summary>
		/// Волатильность (историческая).
		/// </summary>
		public const Level1Fields HistoricalVolatility = Level1Fields.HistoricalVolatility;

		/// <summary>
		/// Лучший доходный оффер.
		/// </summary>
		public const Level1Fields YieldAsk = (Level1Fields)(-3);

		/// <summary>
		/// Лучший доходных бид.
		/// </summary>
		public const Level1Fields YieldBid = (Level1Fields)(-4);

		/// <summary>
		/// Лучшая доходная пара котировок.
		/// </summary>
		public const Level1Fields YieldBidAsk = (Level1Fields)(-5);

		/// <summary>
		/// Доходная последняя сделка.
		/// </summary>
		public const Level1Fields YieldLast = (Level1Fields)(-6);
	}
}