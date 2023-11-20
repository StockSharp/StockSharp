#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: OrderTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Order types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderTypes
	{
		/// <summary>
		/// Limit.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LimitOrderKey)]
		Limit,

		/// <summary>
		/// Market.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketKey)]
		Market,

		/// <summary>
		/// Conditional (stop-loss, take-profit).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StopOrderTypeKey)]
		Conditional,
	}
}