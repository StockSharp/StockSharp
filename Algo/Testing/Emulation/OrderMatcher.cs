namespace StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Order matcher implementation.
/// </summary>
public class OrderMatcher : IOrderMatcher
{
	/// <inheritdoc />
	public MatchResult Match(EmulatorOrder order, IOrderBook book, MatchingSettings settings)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));
		if (book is null)
			throw new ArgumentNullException(nameof(book));
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		// Post-only check: reject if would cross
		if (order.PostOnly && WouldCross(order, book))
		{
			return new MatchResult
			{
				Order = order,
				IsRejected = true,
				RejectionReason = "Post-only order would cross the book",
				FinalState = OrderStates.Done,
				RemainingVolume = order.Balance,
				ShouldPlaceInBook = false,
			};
		}

		var trades = new List<MatchTrade>();
		var matchedOrders = new List<EmulatorOrder>();

		// Market orders
		if (order.OrderType == OrderTypes.Market)
		{
			return MatchMarketOrder(order, book, settings, trades, matchedOrders);
		}

		// Limit orders
		return MatchLimitOrder(order, book, settings, trades, matchedOrders);
	}

	private static MatchResult MatchMarketOrder(
		EmulatorOrder order,
		IOrderBook book,
		MatchingSettings settings,
		List<MatchTrade> trades,
		List<EmulatorOrder> matchedOrders)
	{
		var oppositeSide = order.Side.Invert();
		var remaining = order.Balance;

		// Consume from opposite side at any price
		foreach (var (price, volume, orders) in ((OrderBook)book).ConsumeVolume(oppositeSide, null, remaining))
		{
			var consumed = Math.Min(volume, remaining);
			trades.Add(new MatchTrade(price, consumed, order.Side, orders));
			matchedOrders.AddRange(orders.Where(o => o.IsUserOrder));
			remaining -= consumed;

			if (remaining <= 0)
				break;
		}

		var hasExecution = trades.Count > 0;

		return new MatchResult
		{
			Order = order,
			Trades = trades,
			MatchedOrders = matchedOrders,
			RemainingVolume = remaining,
			ShouldPlaceInBook = false, // Market orders never go to book
			FinalState = OrderStates.Done,
		};
	}

	private static MatchResult MatchLimitOrder(
		EmulatorOrder order,
		IOrderBook book,
		MatchingSettings settings,
		List<MatchTrade> trades,
		List<EmulatorOrder> matchedOrders)
	{
		var oppositeSide = order.Side.Invert();
		var remaining = order.Balance;
		var limitPrice = order.Price;

		// Get matchable volume
		foreach (var (price, volume, orders) in ((OrderBook)book).ConsumeVolume(oppositeSide, limitPrice, remaining))
		{
			var consumed = Math.Min(volume, remaining);
			// Use order price for candle-based matching (like V1's MatchOrderByCandle)
			var tradePrice = settings.UseOrderPriceForLimitTrades ? limitPrice : price;
			trades.Add(new MatchTrade(tradePrice, consumed, order.Side, orders));
			matchedOrders.AddRange(orders.Where(o => o.IsUserOrder));
			remaining -= consumed;

			if (remaining <= 0)
				break;
		}

		var hasExecution = trades.Count > 0;
		var isFullyMatched = remaining <= 0;

		// Determine final state based on TimeInForce
		var shouldPlaceInBook = false;
		var finalState = OrderStates.Active;

		switch (order.TimeInForce)
		{
			case TimeInForce.PutInQueue:
			case null:
				// Place remaining in book
				shouldPlaceInBook = remaining > 0;
				finalState = isFullyMatched ? OrderStates.Done : OrderStates.Active;
				break;

			case TimeInForce.MatchOrCancel: // FOK
				if (!isFullyMatched)
				{
					// Rollback - don't execute any trades
					// Actually we need to not consume at all, so let's rebuild
					return MatchFOK(order, book, settings);
				}
				finalState = OrderStates.Done;
				break;

			case TimeInForce.CancelBalance: // IOC
				// Execute what we can, cancel the rest
				finalState = OrderStates.Done;
				break;
		}

		return new MatchResult
		{
			Order = order,
			Trades = trades,
			MatchedOrders = matchedOrders,
			RemainingVolume = remaining,
			ShouldPlaceInBook = shouldPlaceInBook,
			FinalState = finalState,
		};
	}

	private static MatchResult MatchFOK(EmulatorOrder order, IOrderBook book, MatchingSettings settings)
	{
		var oppositeSide = order.Side.Invert();
		var limitPrice = order.Price;

		// Check if we can fill entire order without actually consuming
		var availableVolume = 0m;
		foreach (var level in book.GetLevels(oppositeSide))
		{
			// Check price
			if (order.Side == Sides.Buy && level.Price > limitPrice)
				break;
			if (order.Side == Sides.Sell && level.Price < limitPrice)
				break;

			availableVolume += level.Volume;
			if (availableVolume >= order.Balance)
				break;
		}

		if (availableVolume < order.Balance)
		{
			// Cannot fill entirely - reject (but order is "done" with full balance remaining)
			return new MatchResult
			{
				Order = order,
				Trades = [],
				MatchedOrders = [],
				RemainingVolume = order.Balance,
				ShouldPlaceInBook = false,
				FinalState = OrderStates.Done,
			};
		}

		// Can fill - do actual matching
		var trades = new List<MatchTrade>();
		var matchedOrders = new List<EmulatorOrder>();
		var remaining = order.Balance;

		foreach (var (price, volume, orders) in ((OrderBook)book).ConsumeVolume(oppositeSide, limitPrice, remaining))
		{
			var consumed = Math.Min(volume, remaining);
			trades.Add(new MatchTrade(price, consumed, order.Side, orders));
			matchedOrders.AddRange(orders.Where(o => o.IsUserOrder));
			remaining -= consumed;

			if (remaining <= 0)
				break;
		}

		return new MatchResult
		{
			Order = order,
			Trades = trades,
			MatchedOrders = matchedOrders,
			RemainingVolume = 0,
			ShouldPlaceInBook = false,
			FinalState = OrderStates.Done,
		};
	}

	/// <inheritdoc />
	public bool WouldCross(EmulatorOrder order, IOrderBook book)
	{
		if (order.OrderType == OrderTypes.Market)
			return true;

		var oppositeBest = order.Side == Sides.Buy ? book.BestAsk : book.BestBid;

		if (oppositeBest is null)
			return false;

		if (order.Side == Sides.Buy)
			return order.Price >= oppositeBest.Value.price;
		else
			return order.Price <= oppositeBest.Value.price;
	}

	/// <inheritdoc />
	public decimal? GetMarketPrice(Sides side, IOrderBook book)
	{
		// For buy orders, get best ask; for sell orders, get best bid
		var oppositeBest = side == Sides.Buy ? book.BestAsk : book.BestBid;
		return oppositeBest?.price;
	}
}
