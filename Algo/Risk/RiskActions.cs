namespace StockSharp.Algo.Risk
{
	using StockSharp.Localization;

	/// <summary>
	/// Types of actions.
	/// </summary>
	public enum RiskActions
	{
		/// <summary>
		/// Close positions.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str856Key)]
		ClosePositions,

		/// <summary>
		/// Stop trading.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str857Key)]
		StopTrading,

		/// <summary>
		/// Cancel orders.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str858Key)]
		CancelOrders,
	}
}