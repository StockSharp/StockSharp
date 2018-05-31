#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: EmulationStates.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// States <see cref="HistoryEmulationConnector"/>.
	/// </summary>
	public enum EmulationStates
	{
		/// <summary>
		/// Stopped.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1128Key)]
		Stopped,

		/// <summary>
		/// Stopping.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Starting.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1129Key)]
		Starting,

		/// <summary>
		/// Working.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1130Key)]
		Started,

		/// <summary>
		/// In the process of suspension.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1131Key)]
		Suspending, 

		/// <summary>
		/// Suspended.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1132Key)]
		Suspended,
	}
}