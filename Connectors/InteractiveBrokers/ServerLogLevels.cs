namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Localization;

	/// <summary>
	/// Server-side logging levels.
	/// </summary>
	public enum ServerLogLevels
	{
		/// <summary>
		/// System messages.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2529Key)]
		System = 1,

		/// <summary>
		/// Errors.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ErrorsKey)]
		Error = 2,

		/// <summary>
		/// Warnings.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.WarningsKey)]
		Warning = 3,

		/// <summary>
		/// Information messages.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.InfoKey)]
		Information = 4,

		/// <summary>
		/// Detailed messages.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2530Key)]
		Detail = 5
	}
}