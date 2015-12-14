#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: LogLevels.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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