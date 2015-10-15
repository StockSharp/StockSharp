namespace StockSharp.Algo.Testing
{
	using StockSharp.Localization;

	/// <summary>
	/// States <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	public enum EmulationStates
	{
		/// <summary>
		/// Stopped.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1128Key)]
		Stopped,

		/// <summary>
		/// Stopping.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Starting.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1129Key)]
		Starting,

		/// <summary>
		/// Working.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1130Key)]
		Started,

		/// <summary>
		/// In the process of suspension.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1131Key)]
		Suspending, 

		/// <summary>
		/// Suspended.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1132Key)]
		Suspended,
	}
}