namespace StockSharp.Algo
{
	using Ecng.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Виды авторизаций.
	/// </summary>
	public enum AuthorizationModes
	{
		/// <summary>
		/// Анонимный.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str886Key)]
		Anonymous,

		/// <summary>
		/// Windows авторизация.
		/// </summary>
		[EnumDisplayName("Windows")]
		Windows,

		/// <summary>
		/// Пользовательский.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str887Key)]
		Custom,

		/// <summary>
		/// StockSharp.
		/// </summary>
		[EnumDisplayName("StockSharp")]
		Community
	}
}