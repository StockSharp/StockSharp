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
	using System.ComponentModel.DataAnnotations;
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
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InheritedKey)]
		[EnumMember]
		Inherit,

		/// <summary>
		/// Verbose message, debug message, information, warnings and errors.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VerboseKey)]
		Verbose,

		/// <summary>
		/// Debug message, information, warnings and errors.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str12Key)]
		Debug,
		
		/// <summary>
		/// Information, warnings and errors.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InfoKey)]
		Info,

		/// <summary>
		/// Warnings and errors.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.WarningsKey)]
		Warning,
		
		/// <summary>
		/// Errors only.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorsKey)]
		Error,

		/// <summary>
		/// Logs off.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OffKey)]
		Off,
	}
}