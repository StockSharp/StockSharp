#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Risk.Algo
File: RiskActions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Risk
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// Types of actions.
	/// </summary>
	public enum RiskActions
	{
		/// <summary>
		/// Close positions.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str856Key)]
		ClosePositions,

		/// <summary>
		/// Stop trading.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str857Key)]
		StopTrading,

		/// <summary>
		/// Cancel orders.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str858Key)]
		CancelOrders,
	}
}