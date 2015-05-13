namespace StockSharp.Algo.Testing
{
	using StockSharp.Localization;

	/// <summary>
	/// Состояния <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	public enum EmulationStates
	{
		/// <summary>
		/// Остановлен.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1128Key)]
		Stopped,

		/// <summary>
		/// Останавливается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Запускается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1129Key)]
		Starting,

		/// <summary>
		/// Работает.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1130Key)]
		Started,

		/// <summary>
		/// В процессе приостановки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1131Key)]
		Suspending, 

		/// <summary>
		/// Приостановлен.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1132Key)]
		Suspended,
	}
}