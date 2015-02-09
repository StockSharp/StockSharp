namespace StockSharp.Algo.Risk
{
	using StockSharp.Localization;

	/// <summary>
	/// Типы действий.
	/// </summary>
	public enum RiskActions
	{
		/// <summary>
		/// Закрыть позиции.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str856Key)]
		ClosePositions,

		/// <summary>
		/// Остановить торговлю.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str857Key)]
		StopTrading,

		/// <summary>
		/// Отменить заявки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str858Key)]
		CancelOrders,
	}
}