namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Localization;

	/// <summary>
	/// Уровни логирования серверной стороны.
	/// </summary>
	public enum ServerLogLevels
	{
		/// <summary>
		/// Системные сообщения.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2529Key)]
		System = 1,

		/// <summary>
		/// Ошибки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.ErrorsKey)]
		Error = 2,

		/// <summary>
		/// Предупреждения.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.WarningsKey)]
		Warning = 3,

		/// <summary>
		/// Информационные сообщения.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.InfoKey)]
		Information = 4,

		/// <summary>
		/// Детальные сообщения.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2530Key)]
		Detail = 5
	}
}