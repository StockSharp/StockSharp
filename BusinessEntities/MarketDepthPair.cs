#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: MarketDepthPair.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;
	using System.ComponentModel.DataAnnotations;

	/// <summary>
	/// Quotes pair.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class MarketDepthPair
	{
		private readonly bool _isFull;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDepthPair"/>.
		/// </summary>
		/// <param name="bid">Bid.</param>
		/// <param name="ask">Ask.</param>
		public MarketDepthPair(QuoteChange? bid, QuoteChange? ask)
		{
			Bid = bid;
			Ask = ask;

			_isFull = bid != null && ask != null;
		}

		/// <summary>
		/// Bid.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.BidKey,
			Description = LocalizedStrings.QuoteBuyKey,
			GroupName = LocalizedStrings.CommonKey)]
		public QuoteChange? Bid { get; }

		/// <summary>
		/// Ask.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AskKey,
			Description = LocalizedStrings.QuoteSellKey,
			GroupName = LocalizedStrings.CommonKey)]
		public QuoteChange? Ask { get; }

		/// <summary>
		/// Spread by price. Is <see langword="null" />, if one of the quotes is empty.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SpreadPriceKey,
			Description = LocalizedStrings.SpreadPriceDescKey,
			GroupName = LocalizedStrings.CommonKey)]
		public decimal? SpreadPrice => _isFull ? (Ask.Value.Price - Bid.Value.Price) : null;

		/// <summary>
		/// Spread by volume. If negative, it best ask has a greater volume than the best bid. Is <see langword="null" />, if one of the quotes is empty.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SpreadVolumeKey,
			Description = LocalizedStrings.SpreadVolumeDescKey,
			GroupName = LocalizedStrings.CommonKey)]
		public decimal? SpreadVolume => _isFull ? (Ask.Value.Volume - Bid.Value.Volume).Abs() : null;

		/// <summary>
		/// Get middle price.
		/// </summary>
		/// <param name="priceStep"><see cref="Security.PriceStep"/></param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public decimal? GetMiddlePrice(decimal? priceStep) => (Bid?.Price).GetSpreadMiddle(Ask?.Price, priceStep);

		/// <summary>
		/// Quotes pair has <see cref="Bid"/> and <see cref="Ask"/>.
		/// </summary>
		public bool IsFull => _isFull;

		/// <inheritdoc />
		public override string ToString()
		{
			return "{{{0}}} {{{1}}}".Put(Bid, Ask);
		}
	}
}