#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: MarketDepth.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Order book.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class MarketDepth : Cloneable<MarketDepth>, IEnumerable<QuoteChange>
	{
		/// <summary>
		/// Create order book.
		/// </summary>
		/// <param name="security">Security.</param>
		public MarketDepth(Security security)
		{
			Security = security ?? throw new ArgumentNullException(nameof(security));
		}

		private int _maxDepth = 100;

		/// <summary>
		/// The maximum depth of order book.
		/// </summary>
		/// <remarks>
		/// The default value is 100. If the exceeded the maximum depth the event <see cref="MarketDepth.QuoteOutOfDepth"/> will triggered.
		/// </remarks>
		[DisplayNameLoc(LocalizedStrings.Str1660Key)]
		[Browsable(false)]
		[Obsolete]
		public int MaxDepth
		{
			get => _maxDepth;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str480);

				_maxDepth = value;
			}
		}

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; }

		/// <summary>
		/// Whether to use aggregated quotes <see cref="QuoteChange.InnerQuotes"/> at the join of the volumes with the same price.
		/// </summary>
		/// <remarks>
		/// The default is disabled for performance.
		/// </remarks>
		public bool UseAggregatedQuotes { get; set; }

		/// <summary>
		/// Last change time.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ServerTimeKey,
			Description = LocalizedStrings.Str168Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 2)]
		public DateTimeOffset LastChangeTime { get; set; }

		/// <summary>
		/// The order book local time stamp.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str203Key,
			Description = LocalizedStrings.Str204Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 3)]
		public DateTimeOffset LocalTime { get; set; }

		/// <summary>
		/// Sequence number.
		/// </summary>
		/// <remarks>Zero means no information.</remarks>
		public long SeqNum { get; set; }

		/// <summary>
		/// Determines the message is generated from the specified <see cref="Messages.DataType"/>.
		/// </summary>
		public Messages.DataType BuildFrom { get; set; }

		/// <summary>
		/// Get the array of bids sorted by descending price. The first (best) bid will be the maximum price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str281Key,
			Description = LocalizedStrings.Str282Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 0)]
		[Obsolete("Use Bids2 property.")]
		public Quote[] Bids => Bids2.Select(c => c.ToQuote(Sides.Buy, Security)).ToArray();

		/// <summary>
		/// Get the array of asks sorted by ascending price. The first (best) ask will be the minimum price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str283Key,
			Description = LocalizedStrings.Str284Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 1)]
		[Obsolete("Use Asks2 property.")]
		public Quote[] Asks => Asks2.Select(c => c.ToQuote(Sides.Sell, Security)).ToArray();

		private QuoteChange[] _bids2 = ArrayHelper.Empty<QuoteChange>();

		/// <summary>
		/// Get the array of bids sorted by descending price. The first (best) bid will be the maximum price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str281Key,
			Description = LocalizedStrings.Str282Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 0)]
		public QuoteChange[] Bids2
		{
			get => _bids2;
			private set => _bids2 = value ?? throw new ArgumentNullException(nameof(value));
		}

		private QuoteChange[] _asks2 = ArrayHelper.Empty<QuoteChange>();

		/// <summary>
		/// Get the array of asks sorted by ascending price. The first (best) ask will be the minimum price.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str283Key,
			Description = LocalizedStrings.Str284Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 1)]
		public QuoteChange[] Asks2 
		{ 
			get => _asks2;
			private set => _asks2 = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// The best bid. If the order book does not contain bids, will be returned <see langword="null" />.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str291Key)]
		[Obsolete("Use BestBid2 property.")]
		public Quote BestBid => BestBid2?.ToQuote(Sides.Buy, Security);

		/// <summary>
		/// The best ask. If the order book does not contain asks, will be returned <see langword="null" />.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str292Key)]
		[Obsolete("Use BestAsk2 property.")]
		public Quote BestAsk => BestAsk2?.ToQuote(Sides.Sell, Security);

		/// <summary>
		/// The best bid. If the order book does not contain bids, will be returned <see langword="null" />.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str291Key)]
		public QuoteChange? BestBid2 { get; private set; }

		/// <summary>
		/// The best ask. If the order book does not contain asks, will be returned <see langword="null" />.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str292Key)]
		public QuoteChange? BestAsk2 { get; private set; }

		/// <summary>
		/// The best pair. If the order book is empty, will be returned <see langword="null" />.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.BestPairKey)]
		public MarketDepthPair BestPair => GetPair(0);

		/// <summary>
		/// To get the total price size by bids.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalBidsPriceKey)]
		public decimal TotalBidsPrice => _bids2.Length > 0 ? Security.ShrinkPrice(_bids2.Sum(b => b.Price)) : 0;

		/// <summary>
		/// To get the total price size by offers.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalAsksPriceKey)]
		public decimal TotalAsksPrice => _asks2.Length > 0 ? Security.ShrinkPrice(_asks2.Sum(a => a.Price)) : 0;

		/// <summary>
		/// Get bids total volume.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalBidsVolumeKey)]
		public decimal TotalBidsVolume => _bids2.Sum(b => b.Volume);

		/// <summary>
		/// Get asks total volume.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalAsksVolumeKey)]
		public decimal TotalAsksVolume => _asks2.Sum(a => a.Volume);

		/// <summary>
		/// Get total volume.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalVolumeKey)]
		public decimal TotalVolume => TotalBidsVolume + TotalAsksVolume;

		/// <summary>
		/// To get the total price size.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalPriceKey)]
		public decimal TotalPrice => TotalBidsPrice + TotalAsksPrice;

		/// <summary>
		/// Total quotes count (bids + asks).
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TotalQuotesCountKey)]
		public int Count => _bids2.Length + _asks2.Length;

		private int _depth;

		/// <summary>
		/// Depth of book.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str1197Key)]
		public int Depth
		{
			get => _depth;
			private set
			{
				if (_depth == value)
					return;

				_depth = value;
#pragma warning disable 612
				DepthChanged?.Invoke();
#pragma warning restore 612
			}
		}

		/// <summary>
		/// Event on exceeding the maximum allowable depth of quotes.
		/// </summary>
		[Obsolete]
#pragma warning disable 67
		public event Action<Quote> QuoteOutOfDepth;
#pragma warning restore 67

		/// <summary>
		/// Depth <see cref="Depth"/> changed.
		/// </summary>
		[Obsolete]
		public event Action DepthChanged;

		/// <summary>
		/// Quotes changed.
		/// </summary>
		[Obsolete]
		public event Action QuotesChanged;

		/// <summary>
		/// To reduce the order book to the required depth.
		/// </summary>
		/// <param name="newDepth">New order book depth.</param>
		public void Decrease(int newDepth)
		{
			var currentDepth = Depth;

			if (newDepth < 0)
				throw new ArgumentOutOfRangeException(nameof(newDepth), newDepth, LocalizedStrings.Str481);
			else if (newDepth > currentDepth)
				throw new ArgumentOutOfRangeException(nameof(newDepth), newDepth, LocalizedStrings.Str482Params.Put(currentDepth));

			Bids2 = Decrease(_bids2, newDepth);
			Asks2 = Decrease(_asks2, newDepth);

			UpdateDepthAndTime();
			RaiseQuotesChanged();
		}

		private static QuoteChange[] Decrease(QuoteChange[] quotes, int newDepth)
		{
			if (quotes is null)
				throw new ArgumentNullException(nameof(quotes));

			if (newDepth <= quotes.Length)
				Array.Resize(ref quotes, newDepth);

			return quotes;
		}

		/// <summary>
		/// To get a quote by the direction <see cref="Sides"/> and the depth index.
		/// </summary>
		/// <param name="orderDirection">Orders side.</param>
		/// <param name="depthIndex">Depth index. Zero index means the best quote.</param>
		/// <returns>Quote. If a quote does not exist for specified depth, then the <see langword="null" /> will be returned.</returns>
		public QuoteChange? GetQuote(Sides orderDirection, int depthIndex)
		{
			return GetQuotesInternal(orderDirection).ElementAtOr(depthIndex);
		}

		/// <summary>
		/// To get a quote by the price.
		/// </summary>
		/// <param name="price">Quote price.</param>
		/// <returns>Found quote. If there is no quote in the order book for the passed price, then the <see langword="null" /> will be returned.</returns>
		public QuoteChange? GetQuote(decimal price)
		{
			var quotes = GetQuotes(price);
			var i = GetQuoteIndex(quotes, price);
			return i < 0 ? default : quotes[i];
		}

		/// <summary>
		/// To get quotes by the direction <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Orders side.</param>
		/// <returns>Quotes.</returns>
		public QuoteChange[] GetQuotes(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? Bids2 : Asks2;
		}

		/// <summary>
		/// To get the best quote by the direction <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Order side.</param>
		/// <returns>The best quote. If the order book is empty, then the <see langword="null" /> will be returned.</returns>
		public QuoteChange? GetBestQuote(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? BestBid2 : BestAsk2;
		}

		/// <summary>
		/// To get a pair of quotes (bid + offer) by the depth index.
		/// </summary>
		/// <param name="depthIndex">Depth index. Zero index means the best pair of quotes.</param>
		/// <returns>The pair of quotes. If the index is larger than book order depth <see cref="MarketDepth.Depth"/>, then the <see langword="null" /> is returned.</returns>
		public MarketDepthPair GetPair(int depthIndex)
		{
			if (depthIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(depthIndex), depthIndex, LocalizedStrings.Str483);

			var bid = GetQuote(Sides.Buy, depthIndex);
			var ask = GetQuote(Sides.Sell, depthIndex);

			if (bid == null && ask == null)
				return null;
				
			return new MarketDepthPair(Security, bid, ask);
		}

		/// <summary>
		/// To get a pair of quotes for a given book depth.
		/// </summary>
		/// <param name="depth">Book depth. The counting is from the best quotes.</param>
		/// <returns>Spread.</returns>
		public IEnumerable<MarketDepthPair> GetTopPairs(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.Str484);

			var retVal = new List<MarketDepthPair>();

			for (var i = 0; i < depth; i++)
			{
				var single = GetPair(i);

				if (single != null)
					retVal.Add(single);
				else
					break;
			}

			return retVal;
		}

		/// <summary>
		/// To get quotes for a given book depth.
		/// </summary>
		/// <param name="depth">Book depth. Quotes are in order of price increasing from bids to offers.</param>
		/// <returns>Spread.</returns>
		public IEnumerable<QuoteChange> GetTopQuotes(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.Str484);

			var retVal = new List<QuoteChange>();

			for (var i = depth - 1; i >= 0; i--)
			{
				var single = GetQuote(Sides.Buy, i);

				if (single != null)
					retVal.Add(single.Value);
			}

			for (var i = 0; i < depth; i++)
			{
				var single = GetQuote(Sides.Sell, i);

				if (single != null)
					retVal.Add(single.Value);
				else
					break;
			}

			return retVal;
		}

		/// <summary>
		/// Update the order book by new quotes.
		/// </summary>
		/// <param name="quotes">The new quotes.</param>
		/// <param name="lastChangeTime">Last change time.</param>
		/// <returns>Market depth.</returns>
		/// <remarks>
		/// The old quotes will be removed from the book.
		/// </remarks>
		[Obsolete]
		public MarketDepth Update(IEnumerable<Quote> quotes, DateTimeOffset lastChangeTime = default)
		{
			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));

			var bids = Enumerable.Empty<Quote>();
			var asks = Enumerable.Empty<Quote>();

			foreach (var group in quotes.GroupBy(q => q.OrderDirection))
			{
				if (group.Key == Sides.Buy)
					bids = group;
				else
					asks = group;
			}

			return Update(bids, asks, false, lastChangeTime);
		}

		/// <summary>
		/// Update the order book by new bids and asks.
		/// </summary>
		/// <param name="bids">The new bids.</param>
		/// <param name="asks">The new asks.</param>
		/// <param name="isSorted">Are quotes sorted. This parameter is used for optimization in order to prevent re-sorting.</param>
		/// <param name="lastChangeTime">Last change time.</param>
		/// <returns>Market depth.</returns>
		/// <remarks>
		/// The old quotes will be removed from the book.
		/// </remarks>
		[Obsolete]
		public MarketDepth Update(IEnumerable<Quote> bids, IEnumerable<Quote> asks, bool isSorted = false, DateTimeOffset lastChangeTime = default)
		{
			if (bids == null)
				throw new ArgumentNullException(nameof(bids));

			if (asks == null)
				throw new ArgumentNullException(nameof(asks));

			if (!isSorted)
			{
				bids = bids.OrderBy(q => 0 - q.Price);
				asks = asks.OrderBy(q => q.Price);
			}

			var bidsArr = bids.Select(EntitiesExtensions.ToQuoteChange).ToArray();
			var asksArr = asks.Select(EntitiesExtensions.ToQuoteChange).ToArray();

			//if (AutoVerify)
			//{
			//	if (!Verify(bidsArr, asksArr))
			//		throw new ArgumentException(LocalizedStrings.Str485);
			//}

			//Truncate(bidsArr, asksArr, lastChangeTime);
			
			return Update(bidsArr, asksArr, lastChangeTime);
		}

		/// <summary>
		/// To update the order book. The version without checks and blockings.
		/// </summary>
		/// <param name="bids">Sorted bids.</param>
		/// <param name="asks">Sorted asks.</param>
		/// <param name="lastChangeTime">Change time.</param>
		/// <returns>Market depth.</returns>
		public MarketDepth Update(QuoteChange[] bids, QuoteChange[] asks, DateTimeOffset lastChangeTime)
		{
			if (bids is null)
				throw new ArgumentNullException(nameof(bids));

			if (asks is null)
				throw new ArgumentNullException(nameof(asks));

			_bids2 = bids.ToArray();
			_asks2 = asks.ToArray();
			
			UpdateDepthAndTime(lastChangeTime, false);
			RaiseQuotesChanged();

			return this;
		}

		/// <summary>
		/// To refresh the quote. If a quote with the same price is already in the order book, it is updated as passed. Otherwise, it automatically rebuilds the order book.
		/// </summary>
		/// <param name="quote">The new quote.</param>
		/// <param name="side">Side.</param>
		public void UpdateQuote(QuoteChange quote, Sides side)
		{
			SetQuote(quote, side, false);
		}

		/// <summary>
		/// Add buy quote.
		/// </summary>
		/// <param name="price">Buy price.</param>
		/// <param name="volume">Buy volume.</param>
		public void AddBid(decimal price, decimal volume)
		{
			AddQuote(new QuoteChange
			{
				Price = price,
				Volume = volume,
			}, Sides.Buy);
		}

		/// <summary>
		/// Add sell quote.
		/// </summary>
		/// <param name="price">Sell price.</param>
		/// <param name="volume">Sell volume.</param>
		public void AddAsk(decimal price, decimal volume)
		{
			AddQuote(new QuoteChange
			{
				Price = price,
				Volume = volume,
			}, Sides.Sell);
		}

		/// <summary>
		/// To add the quote. If a quote with the same price is already in the order book, they are combined into the <see cref="QuoteChange.InnerQuotes"/>.
		/// </summary>
		/// <param name="quote">The new quote.</param>
		/// <param name="side">Side.</param>
		public void AddQuote(QuoteChange quote, Sides side)
		{
			SetQuote(quote, side, true);
		}

		private void SetQuote(QuoteChange quote, Sides side, bool isAggregate)
		{
			//CheckQuote(quote);

			//Quote outOfDepthQuote = null;

			//lock (_syncRoot)
			//{
				var quotes = GetQuotes(side);

				var index = GetQuoteIndex(quotes, quote.Price);

				if (index != -1)
				{
					if (isAggregate)
					{
						var existedQuote = quotes[index];

						//if (UseAggregatedQuotes)
						//{
						//	if (!(existedQuote is AggregatedQuote aggQuote))
						//	{
						//		aggQuote = new AggregatedQuote
						//		{
						//			Price = quote.Price,
						//			Security = quote.Security,
						//			OrderDirection = quote.OrderDirection
						//		};

						//		aggQuote.InnerQuotes.Add(existedQuote);

						//		quotes[index] = aggQuote;
						//	}

						//	aggQuote.InnerQuotes.Add(quote);
						//}
						//else
						existedQuote.Volume += quote.Volume;
					}
					else
					{
						quotes[index] = quote;
					}
				}
				else
				{
					for (index = 0; index < quotes.Length; index++)
					{
						var currentPrice = quotes[index].Price;

						if (side == Sides.Buy)
						{
							if (quote.Price > currentPrice)
								break;
						}
						else
						{
							if (quote.Price < currentPrice)
								break;
						}
					}

					Array.Resize(ref quotes, quotes.Length + 1);

					if (index < (quotes.Length - 1))
						Array.Copy(quotes, index, quotes, index + 1, quotes.Length - 1 - index);

					quotes[index] = quote;

					//if (quotes.Length > MaxDepth)
					//{
					//	outOfDepthQuote = quotes[quotes.Length - 1];
					//	quotes = RemoveAt(quotes, quotes.Length - 1);
					//}

					if (side == Sides.Buy)
						Bids2 = quotes;
					else
						Asks2 = quotes;
				}

				UpdateDepthAndTime();

				//if (quotes.Length > MaxDepth)
				//	throw new InvalidOperationException(LocalizedStrings.Str486Params.Put(MaxDepth, quotes.Length));
			//}

			RaiseQuotesChanged();

			//if (outOfDepthQuote != null)
			//	QuoteOutOfDepth?.Invoke(outOfDepthQuote);
		}

		#region IEnumerable<QuoteChange>

		/// <summary>
		/// To get the enumerator object.
		/// </summary>
		/// <returns>The enumerator object.</returns>
		public IEnumerator<QuoteChange> GetEnumerator()
		{
			return Bids2.Reverse().Concat(Asks2).Cast<QuoteChange>().GetEnumerator();
		}

		/// <summary>
		/// To get the enumerator object.
		/// </summary>
		/// <returns>The enumerator object.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// To get all pairs from the order book.
		/// </summary>
		/// <returns>Pairs from which the order book is composed.</returns>
		public IEnumerable<MarketDepthPair> ToPairs()
		{
			return GetTopPairs(Depth);
		}

		/// <summary>
		/// Remove the quote.
		/// </summary>
		/// <param name="quote">The quote to remove.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		[Obsolete]
		public void Remove(Quote quote, DateTimeOffset lastChangeTime = default)
		{
			if (quote == null)
				throw new ArgumentNullException(nameof(quote));

			Remove(quote.OrderDirection, quote.Price, quote.Volume, lastChangeTime);
		}

		/// <summary>
		/// Remove the volume for the price.
		/// </summary>
		/// <param name="price">Remove the quote for the price.</param>
		/// <param name="volume">The volume to be deleted. If it is not specified, then all the quote is removed.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		public void Remove(decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default)
		{
			var dir = GetDirection(price);

			if (dir == null)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.Str487);

			Remove((Sides)dir, price, volume, lastChangeTime);
		}

		/// <summary>
		/// Remove the volume for the price.
		/// </summary>
		/// <param name="direction">Order side.</param>
		/// <param name="price">Remove the quote for the price.</param>
		/// <param name="volume">The volume to be deleted. If it is not specified, then all the quote is removed.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		public void Remove(Sides direction, decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.Str488);

			if (volume < 0)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.Str489);

			var quotes = GetQuotesInternal(direction);
			var index = GetQuoteIndex(quotes, price);

			if (index == -1)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.Str487);

			var quote = quotes[index];

			decimal leftVolume;

			if (volume > 0)
			{
				if (quote.Volume < volume)
					throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.Str490Params.Put(quote));

				leftVolume = quote.Volume - volume;

				//if (UseAggregatedQuotes)
				//{
				//	if (quote is AggregatedQuote aggQuote)
				//	{
				//		while (volume > 0)
				//		{
				//			var innerQuote = aggQuote.InnerQuotes.First();

				//			if (innerQuote.Volume > volume)
				//			{
				//				innerQuote.Volume -= volume;
				//				break;
				//			}
				//			else
				//			{
				//				aggQuote.InnerQuotes.Remove(innerQuote);
				//				volume -= innerQuote.Volume;
				//			}
				//		}
				//	}
				//}
			}
			else
				leftVolume = 0;

			if (leftVolume == 0)
			{
				quotes = RemoveAt(quotes, index);

				if (direction == Sides.Buy)
					Bids2 = quotes;
				else
					Asks2 = quotes;

				UpdateDepthAndTime(lastChangeTime);
			}
			else
			{
				quote.Volume = leftVolume;
				UpdateTime(lastChangeTime);
			}

			RaiseQuotesChanged();
		}

		private static QuoteChange[] RemoveAt(QuoteChange[] quotes, int index)
		{
			var newQuotes = new QuoteChange[quotes.Length - 1];

			if (index > 0)
				Array.Copy(quotes, 0, newQuotes, 0, index);

			if (index < (quotes.Length - 1))
				Array.Copy(quotes, index + 1, newQuotes, index, quotes.Length - index - 1);

			return newQuotes;
		}

		private static int GetQuoteIndex(QuoteChange[] quotes, decimal price)
		{
			var stop = quotes.Length - 1;
			if (stop < 0)
				return -1;

			var first = quotes[0];

			var cmp = decimal.Compare(price, first.Price);
			if (cmp == 0)
				return 0;

			var last = quotes[stop];
			var desc = first.Price - last.Price > 0m;

			if (desc)
				cmp = -cmp;

			if (cmp < 0)
				return -1;

			cmp = decimal.Compare(price, last.Price);

			if (desc)
				cmp = -cmp;

			if (cmp > 0)
				return -1;

			if (cmp == 0)
				return stop;

			var start = 0;

			while (stop - start >= 0)
			{
				var mid = (start + stop) >> 1;

				cmp = decimal.Compare(price, quotes[mid].Price);

				if (desc)
					cmp = -cmp;
				if (cmp > 0)
					start = mid + 1;
				else if (cmp < 0)
					stop = mid - 1;
				else
					return mid;
			}

			return -1;
		}

		private QuoteChange[] GetQuotesInternal(Sides direction)
		{
			return direction == Sides.Buy ? _bids2 : _asks2;
		}

		private QuoteChange[] GetQuotes(decimal price)
		{
			var dir = GetDirection(price);

			if (dir == null)
				return ArrayHelper.Empty<QuoteChange>();
			else
				return dir == Sides.Buy ? _bids2 : _asks2;
		}

		private Sides? GetDirection(decimal price)
		{
			if (BestBid2 != null && BestBid2.Value.Price >= price)
				return Sides.Buy;
			else if (BestAsk2 != null && BestAsk2.Value.Price <= price)
				return Sides.Sell;
			else
				return null;
		}

		private void UpdateDepthAndTime(DateTimeOffset lastChangeTime = default, bool depthChangedEventNeeded = true)
		{
			if (depthChangedEventNeeded)
			{
				Depth = _bids2.Length > _asks2.Length ? _bids2.Length : _asks2.Length;
			}
			else
			{
				_depth = _bids2.Length > _asks2.Length ? _bids2.Length : _asks2.Length;
			}

			BestBid2 = _bids2.Length > 0 ? _bids2[0] : (QuoteChange?)null;
			BestAsk2 = _asks2.Length > 0 ? _asks2[0] : (QuoteChange?)null;

			UpdateTime(lastChangeTime);
		}

		private void UpdateTime(DateTimeOffset lastChangeTime)
		{
			if (lastChangeTime != default)
			{
				LastChangeTime = lastChangeTime;
			}
		}

		private void RaiseQuotesChanged()
		{
#pragma warning disable 612
			QuotesChanged?.Invoke();
#pragma warning restore 612
		}

		/// <summary>
		/// Create a copy of <see cref="MarketDepth"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override MarketDepth Clone()
		{
			return new MarketDepth(Security)
			{
				//MaxDepth = MaxDepth,
				//UseAggregatedQuotes = UseAggregatedQuotes,
				//AutoVerify = AutoVerify,
				Currency = Currency,
				LocalTime = LocalTime,
				LastChangeTime = LastChangeTime,
				_bids2 = _bids2.ToArray(),
				_asks2 = _asks2.ToArray(),
				SeqNum = SeqNum,
				BuildFrom = BuildFrom,
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return this.Select(q => q.ToString()).Join(Environment.NewLine);
		}
	}
}