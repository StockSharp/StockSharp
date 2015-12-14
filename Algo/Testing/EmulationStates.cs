#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: EmulationStates.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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