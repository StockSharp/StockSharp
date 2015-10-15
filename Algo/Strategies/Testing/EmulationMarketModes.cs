namespace StockSharp.Algo.Strategies.Testing
{
	using StockSharp.Localization;

	/// <summary>
	/// The data type for paper trading.
	/// </summary>
	public enum EmulationMarketDataModes
	{
		/// <summary>
		/// Storage.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1405Key)]
		Storage,

		/// <summary>
		/// Generated.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1406Key)]
		Generate,

		/// <summary>
		/// None.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1407Key)]
		No
	}
}