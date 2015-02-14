namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Строчка лога заявок.
	/// </summary>
	[Serializable]
	[DescriptionLoc(LocalizedStrings.Str535Key)]
	public class OrderLogItem : MyTrade
	{
		/// <summary>
		/// Создать <see cref="OrderLogItem"/>.
		/// </summary>
		public OrderLogItem()
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			var result = LocalizedStrings.Str536Params.Put(Trade == null ? (Order.State == OrderStates.Done ? LocalizedStrings.Str537 : LocalizedStrings.Str538) : LocalizedStrings.Str539, Order);

			if (Trade != null)
				result += LocalizedStrings.Str540Params.Put(Trade);

			return result;
		}
	}
}