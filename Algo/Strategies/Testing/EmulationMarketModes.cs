namespace StockSharp.Algo.Strategies.Testing
{
	using StockSharp.Localization;

	/// <summary>
	/// Тип данных для эмуляции.
	/// </summary>
	public enum EmulationMarketDataModes
	{
		/// <summary>
		/// Хранилище.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1405Key)]
		Storage,

		/// <summary>
		/// Сгенерированные.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1406Key)]
		Generate,

		/// <summary>
		/// Никакие.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1407Key)]
		No
	}
}