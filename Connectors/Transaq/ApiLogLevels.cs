namespace StockSharp.Transaq
{
	using StockSharp.Localization;

	/// <summary>
	/// Уровни логирования.
	/// </summary>
	public enum ApiLogLevels
	{
		/// <summary>
		/// Минимально.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3508Key)]
		Min = 1,

		/// <summary>
		/// Стандарт (оптимально).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3509Key)]
		Standard = 2,

		/// <summary>
		/// Максимально.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3510Key)]
		Max = 3
	}
}