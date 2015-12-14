#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.InteractiveBrokers
File: ServerLogLevels.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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