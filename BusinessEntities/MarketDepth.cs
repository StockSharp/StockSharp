namespace StockSharp.BusinessEntities;

using System.Collections;

/// <summary>
/// Order book.
/// </summary>
/// <remarks>
/// Create order book.
/// </remarks>
/// <param name="security">Security.</param>
[System.Runtime.Serialization.DataContract]
[Serializable]
[Obsolete("Use IOrderBookMessage.")]
public class MarketDepth(Security security) : Cloneable<MarketDepth>, IEnumerable<QuoteChange>, IOrderBookMessage
{
	QuoteChangeStates? IOrderBookMessage.State { get => null; set => throw new NotSupportedException(); }

	private SecurityId? _securityId;

	SecurityId ISecurityIdMessage.SecurityId
	{
		get => _securityId ??= Security?.Id.ToSecurityId() ?? default;
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Security.
	/// </summary>
	public Security Security { get; } = security ?? throw new ArgumentNullException(nameof(security));

	/// <summary>
	/// Whether to use aggregated quotes <see cref="QuoteChange.InnerQuotes"/> at the join of the volumes with the same price.
	/// </summary>
	/// <remarks>
	/// The default is disabled for performance.
	/// </remarks>
	public bool UseAggregatedQuotes { get; set; }

	/// <inheritdoc/>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ServerTimeKey,
		Description = LocalizedStrings.ChangeServerTimeKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 2)]
	public DateTimeOffset ServerTime { get; set; }

	/// <summary>
	/// Last change time.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use ServerTime property.")]
	public DateTimeOffset LastChangeTime
	{
		get => ServerTime;
		set => ServerTime = value;
	}

	/// <inheritdoc/>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LocalTimeKey,
		Description = LocalizedStrings.LocalTimeDescKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 3)]
	public DateTimeOffset LocalTime { get; set; }

	/// <inheritdoc/>
	public long SeqNum { get; set; }

	/// <inheritdoc/>
	public Messages.DataType BuildFrom { get; set; }

	private QuoteChange[] _bids = [];

	/// <inheritdoc/>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BidsKey,
		Description = LocalizedStrings.QuotesBuyKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 0)]
	public QuoteChange[] Bids
	{
		get => _bids;
		set => _bids = value ?? throw new ArgumentNullException(nameof(value));
	}

	private QuoteChange[] _asks = [];

	/// <inheritdoc/>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AsksKey,
		Description = LocalizedStrings.QuotesSellKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 1)]
	public QuoteChange[] Asks
	{
		get => _asks;
		set => _asks = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Trading security currency.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrencyKey)]
	public CurrencyTypes? Currency { get; set; }

	/// <summary>
	/// The best bid. If the order book does not contain bids, will be returned <see langword="null" />.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestBidKey)]
	public QuoteChange? BestBid { get; private set; }

	/// <summary>
	/// The best ask. If the order book does not contain asks, will be returned <see langword="null" />.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestAskKey)]
	public QuoteChange? BestAsk { get; private set; }

	/// <summary>
	/// The best pair. If the order book is empty, will be returned <see langword="null" />.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestPairKey)]
	public MarketDepthPair BestPair => GetPair(0);

	/// <summary>
	/// To get the total price size by bids.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalBidsPriceKey)]
	public decimal TotalBidsPrice => _bids.Length > 0 ? Security.ShrinkPrice(_bids.Sum(b => b.Price)) : 0;

	/// <summary>
	/// To get the total price size by offers.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalAsksPriceKey)]
	public decimal TotalAsksPrice => _asks.Length > 0 ? Security.ShrinkPrice(_asks.Sum(a => a.Price)) : 0;

	/// <summary>
	/// Get bids total volume.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalBidsVolumeKey)]
	public decimal TotalBidsVolume => _bids.Sum(b => b.Volume);

	/// <summary>
	/// Get asks total volume.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalAsksVolumeKey)]
	public decimal TotalAsksVolume => _asks.Sum(a => a.Volume);

	/// <summary>
	/// Get total volume.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalVolumeKey)]
	public decimal TotalVolume => TotalBidsVolume + TotalAsksVolume;

	/// <summary>
	/// To get the total price size.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalPriceKey)]
	public decimal TotalPrice => TotalBidsPrice + TotalAsksPrice;

	/// <summary>
	/// Total quotes count (bids + asks).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalQuotesCountKey)]
	public int Count => _bids.Length + _asks.Length;

	/// <summary>
	/// Depth of book.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DepthOfBookKey)]
	public int Depth { get; private set; }

	/// <summary>
	/// To reduce the order book to the required depth.
	/// </summary>
	/// <param name="newDepth">New order book depth.</param>
	public void Decrease(int newDepth)
	{
		var currentDepth = Depth;

		if (newDepth < 0)
			throw new ArgumentOutOfRangeException(nameof(newDepth), newDepth, LocalizedStrings.InvalidValue);
		else if (newDepth > currentDepth)
			throw new ArgumentOutOfRangeException(nameof(newDepth), newDepth, LocalizedStrings.NewDepthCannotMoreCurrent.Put(currentDepth));

		Bids = Decrease(_bids, newDepth);
		Asks = Decrease(_asks, newDepth);

		UpdateDepthAndTime();
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
		return orderDirection == Sides.Buy ? Bids : Asks;
	}

	/// <summary>
	/// To get the best quote by the direction <see cref="Sides"/>.
	/// </summary>
	/// <param name="orderDirection">Order side.</param>
	/// <returns>The best quote. If the order book is empty, then the <see langword="null" /> will be returned.</returns>
	public QuoteChange? GetBestQuote(Sides orderDirection)
	{
		return orderDirection == Sides.Buy ? BestBid : BestAsk;
	}

	/// <summary>
	/// To get a pair of quotes (bid + offer) by the depth index.
	/// </summary>
	/// <param name="depthIndex">Depth index. Zero index means the best pair of quotes.</param>
	/// <returns>The pair of quotes. If the index is larger than book order depth <see cref="MarketDepth.Depth"/>, then the <see langword="null" /> is returned.</returns>
	public MarketDepthPair GetPair(int depthIndex)
	{
		var (bid, ask) = Messages.Extensions.GetPair(this, depthIndex);

		if (bid is null && ask is null)
			return null;

		return new MarketDepthPair(bid, ask);
	}

	/// <summary>
	/// To get a pair of quotes for a given book depth.
	/// </summary>
	/// <param name="depth">Book depth. The counting is from the best quotes.</param>
	/// <returns>Spread.</returns>
	public IEnumerable<MarketDepthPair> GetTopPairs(int depth)
		=> Messages.Extensions.GetTopPairs(this, depth).Select(t => new MarketDepthPair(t.bid, t.ask));

	/// <summary>
	/// To get quotes for a given book depth.
	/// </summary>
	/// <param name="depth">Book depth. Quotes are in order of price increasing from bids to offers.</param>
	/// <returns>Spread.</returns>
	public IEnumerable<QuoteChange> GetTopQuotes(int depth)
		=> Messages.Extensions.GetTopQuotes(this, depth);

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

		_bids = [.. bids];
		_asks = [.. asks];

		UpdateDepthAndTime(lastChangeTime);

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
					//	if (existedQuote is not AggregatedQuote aggQuote)
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
					Bids = quotes;
				else
					Asks = quotes;
			}

			UpdateDepthAndTime();
		//}

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
		return Bids.Reverse().Concat(Asks).Cast<QuoteChange>().GetEnumerator();
	}

	/// <summary>
	/// To get the enumerator object.
	/// </summary>
	/// <returns>The enumerator object.</returns>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
	/// Remove the volume for the price.
	/// </summary>
	/// <param name="price">Remove the quote for the price.</param>
	/// <param name="volume">The volume to be deleted. If it is not specified, then all the quote is removed.</param>
	/// <param name="lastChangeTime">Order book change time.</param>
	public void Remove(decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default)
	{
		var dir = GetDirection(price) ?? throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.QuotePriceNotSpecified);

		Remove(dir, price, volume, lastChangeTime);
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
			throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

		if (volume < 0)
			throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

		var quotes = GetQuotesInternal(direction);
		var index = GetQuoteIndex(quotes, price);

		if (index == -1)
			throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.QuotePriceNotSpecified);

		var quote = quotes[index];

		decimal leftVolume;

		if (volume > 0)
		{
			if (quote.Volume < volume)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.VolumeLessThanRequired.Put(quote));

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
				Bids = quotes;
			else
				Asks = quotes;

			UpdateDepthAndTime(lastChangeTime);
		}
		else
		{
			quote.Volume = leftVolume;
			UpdateTime(lastChangeTime);
		}
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
		return direction == Sides.Buy ? _bids : _asks;
	}

	private QuoteChange[] GetQuotes(decimal price)
	{
		var dir = GetDirection(price);

		if (dir == null)
			return [];
		else
			return dir == Sides.Buy ? _bids : _asks;
	}

	private Sides? GetDirection(decimal price)
	{
		if (BestBid != null && BestBid.Value.Price >= price)
			return Sides.Buy;
		else if (BestAsk != null && BestAsk.Value.Price <= price)
			return Sides.Sell;
		else
			return null;
	}

	private void UpdateDepthAndTime(DateTimeOffset lastChangeTime = default)
	{
		Depth = _bids.Length > _asks.Length ? _bids.Length : _asks.Length;

		BestBid = _bids.Length > 0 ? _bids[0] : null;
		BestAsk = _asks.Length > 0 ? _asks[0] : null;

		UpdateTime(lastChangeTime);
	}

	private void UpdateTime(DateTimeOffset lastChangeTime)
	{
		if (lastChangeTime != default)
		{
			ServerTime = lastChangeTime;
		}
	}

	/// <summary>
	/// Create a copy of <see cref="MarketDepth"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override MarketDepth Clone()
	{
		return new(Security)
		{
			//MaxDepth = MaxDepth,
			//UseAggregatedQuotes = UseAggregatedQuotes,
			//AutoVerify = AutoVerify,
			Currency = Currency,
			LocalTime = LocalTime,
			ServerTime = ServerTime,
			_bids = [.. _bids],
			_asks = [.. _asks],
			SeqNum = SeqNum,
			BuildFrom = BuildFrom,
		};
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return this.Select(q => q.ToString()).JoinNL();
	}
}