namespace StockSharp.Algo
{
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Types of authorization.
	/// </summary>
	public enum AuthorizationModes
	{
		/// <summary>
		/// Anonymous.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str886Key)]
		Anonymous,

		/// <summary>
		/// Windows authorization.
		/// </summary>
		[EnumDisplayName("Windows")]
		Windows,

		/// <summary>
		/// User.
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