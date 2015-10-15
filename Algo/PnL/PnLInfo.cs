namespace StockSharp.Algo.PnL
{
	using System;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Information on trade, its closed volume and its profitability.
	/// </summary>
	public class PnLInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PnLInfo"/>.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <param name="closedVolume">The volume of position, which was closed by own trade.</param>
		/// <param name="pnL">The profit, realized by this trade.</param>
		public PnLInfo(ExecutionMessage trade, decimal closedVolume, decimal pnL)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			if (closedVolume < 0)
				throw new ArgumentOutOfRangeException("closedVolume", closedVolume, LocalizedStrings.Str946);

			Trade = trade;
			ClosedVolume = closedVolume;
			PnL = pnL;
		}

		/// <summary>
		/// Own trade.
		/// </summary>
		public ExecutionMessage Trade { get; private set; }

		/// <summary>
		/// The volume of position, which was closed by own trade.
		/// </summary>
		/// <remarks>
		/// For example, in strategy position was 2. The trade for -5 contracts. Closed position 2.
		/// </remarks>
		public decimal ClosedVolume { get; private set; }

		/// <summary>
		/// The profit, realized by this trade.
		/// </summary>
		public decimal PnL { get; private set; }
	}
}