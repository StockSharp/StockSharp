namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Order log item.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DescriptionLoc(LocalizedStrings.Str535Key)]
	public class OrderLogItem : MyTrade
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogItem"/>.
		/// </summary>
		public OrderLogItem()
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			var result = LocalizedStrings.Str536Params.Put(Trade == null ? (Order.State == OrderStates.Done ? LocalizedStrings.Str537 : LocalizedStrings.Str538) : LocalizedStrings.Str539, Order);

			if (Trade != null)
				result += LocalizedStrings.Str540Params.Put(Trade);

			return result;
		}
	}
}