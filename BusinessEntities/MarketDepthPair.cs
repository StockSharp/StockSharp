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
		/// <param name="security">Security.</param>
		/// <param name="bid">Bid.</param>
		/// <param name="ask">Ask.</param>
		public MarketDepthPair(Security security, Quote bid, Quote ask)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (bid != null && bid.OrderDirection != Sides.Buy)
				throw new ArgumentException(LocalizedStrings.Str492);

			if (ask != null && ask.OrderDirection != Sides.Sell)
				throw new ArgumentException(LocalizedStrings.Str493);

			Security = security;
			Bid = bid;
			Ask = ask;

			_isFull = bid != null && ask != null;
		}

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; private set; }

		/// <summary>
		/// Bid.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BidKey)]
		[DescriptionLoc(LocalizedStrings.Str494Key)]
		public Quote Bid { get; }

		/// <summary>
		/// Ask.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AskKey)]
		[DescriptionLoc(LocalizedStrings.Str495Key)]
		public Quote Ask { get; }

		/// <summary>
		/// Spread by price. Is <see langword="null" />, if one of the quotes is empty.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str496Key)]
		[DescriptionLoc(LocalizedStrings.Str497Key)]
		public decimal? SpreadPrice => _isFull ? Ask.Security.ShrinkPrice(Ask.Price - Bid.Price) : (decimal?)null;

		/// <summary>
		/// Spread by volume. If negative, it best ask has a greater volume than the best bid. Is <see langword="null" />, if one of the quotes is empty.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str498Key)]
		[DescriptionLoc(LocalizedStrings.Str499Key)]
		public decimal? SpreadVolume => _isFull ? (Ask.Volume - Bid.Volume).Abs() : (decimal?)null;

		/// <summary>
		/// The middle of spread. Is <see langword="null" />, if one of the quotes is empty.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str500Key)]
		[DescriptionLoc(LocalizedStrings.Str501Key)]
		public decimal? MiddlePrice => _isFull ? (Bid.Price + SpreadPrice / 2) : null;

		/// <summary>
		/// Quotes pair has <see cref="MarketDepthPair.Bid"/> and <see cref="MarketDepthPair.Ask"/>.
		/// </summary>
		public bool IsFull => _isFull;

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{{{0}}} {{{1}}}".Put(Bid, Ask);
		}
	}
}