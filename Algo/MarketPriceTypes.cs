#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: MarketPriceTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The type of market prices.
	/// </summary>
	[DataContract]
	public enum MarketPriceTypes
	{
		/// <summary>
		/// The counter-price (for quick closure of position).
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str975Key)]
		[EnumMember]
		Opposite,

		/// <summary>
		/// The concurrent price (for quoting at the edge of spread).
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str976Key)]
		[EnumMember]
		Following,

		/// <summary>
		/// Spread middle.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SpreadKey)]
		[EnumMember]
		Middle,
	}
}