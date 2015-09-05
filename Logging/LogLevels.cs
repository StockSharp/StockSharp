namespace StockSharp.Logging
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Levels of log messages <see cref="LogMessage"/>.
	/// </summary>
	[DataContract]
	public enum LogLevels
	{
		/// <summary>
		/// To use the logging level of the container.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.InheritedKey)]
		[EnumMember]
		Inherit,

		/// <summary>
		/// Debug message, information, warnings and errors.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str12Key)]
		Debug,
		
		/// <summary>
		/// Information, warnings and errors.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.InfoKey)]
		Info,

		/// <summary>
		/// Warnings and errors.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.WarningsKey)]
		Warning,
		
		/// <summary>
		/// Errors only.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ErrorsKey)]
		Error,

		/// <summary>
		/// Logs off.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.OffKey)]
		Off,
	}
}