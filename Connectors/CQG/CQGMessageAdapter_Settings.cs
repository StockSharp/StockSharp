#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.CQG.CQG
File: CQGMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.CQG
{
	using System.ComponentModel;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// CQG message adapter.
	/// </summary>
	[Icon("CQG_logo.png")]
	[Doc("http://stocksharp.com/doc/html/aac980b1-ac5b-415b-811c-a8d128942391.htm")]
	[DisplayName("CQG")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "CQG")]
	partial class CQGMessageAdapter
	{
		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return string.Empty;
		}
	}
}