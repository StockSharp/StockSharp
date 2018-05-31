#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: AuthorizationModes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// Types of authorization.
	/// </summary>
	public enum AuthorizationModes
	{
		/// <summary>
		/// Anonymous.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AnonymousKey)]
		Anonymous,

		/// <summary>
		/// Windows authorization.
		/// </summary>
		[Display(Name = "Windows")]
		Windows,

		/// <summary>
		/// Custom.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CustomKey)]
		Custom,

		/// <summary>
		/// StockSharp.
		/// </summary>
		[Display(Name = "StockSharp")]
		Community
	}
}