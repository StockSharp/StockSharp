#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: OrderLogItem.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Order log item.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderLogOfKey,
		Description = LocalizedStrings.OrderLogDescKey)]
	[Obsolete("Use IOrderLogMessage.")]
	public class OrderLogItem : MyTrade, IOrderLogMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogItem"/>.
		/// </summary>
		public OrderLogItem()
		{
		}

		IOrderMessage IOrderLogMessage.Order => Order;
		ITickTradeMessage IOrderLogMessage.Trade => Trade;

		/// <inheritdoc />
		public override string ToString()
		{
			var result = LocalizedStrings.OLFromOrder.Put(Trade == null ? (Order.State == OrderStates.Done ? LocalizedStrings.Cancellation : LocalizedStrings.Registration) : LocalizedStrings.Matching, Order);

			if (Trade != null)
				result += " " + LocalizedStrings.OLFromTrade.Put(Trade);

			return result;
		}
	}
}