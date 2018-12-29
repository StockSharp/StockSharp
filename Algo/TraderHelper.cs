#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: TraderHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.Positions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Price rounding rules.
	/// </summary>
	public enum ShrinkRules
	{
		/// <summary>
		/// Automatically to determine rounding to lesser or to bigger value.
		/// </summary>
		Auto,

		/// <summary>
		/// To round to lesser value.
		/// </summary>
		Less,

		/// <summary>
		/// To round to bigger value.
		/// </summary>
		More,
	}

	/// <summary>
	/// The auxiliary class for provision of various algorithmic functionalities.
	/// </summary>
	public static partial class TraderHelper
	{
		private static readonly bool[][] _stateChangePossibilities;

		static TraderHelper()
		{
			_stateChangePossibilities = new bool[5][];

			for (var i = 0; i < _stateChangePossibilities.Length; i++)
				_stateChangePossibilities[i] = new bool[_stateChangePossibilities.Length];

			_stateChangePossibilities[(int)OrderStates.None][(int)OrderStates.None] = true;
			_stateChangePossibilities[(int)OrderStates.None][(int)OrderStates.Pending] = true;
			_stateChangePossibilities[(int)OrderStates.None][(int)OrderStates.Active] = true;
			_stateChangePossibilities[(int)OrderStates.None][(int)OrderStates.Done] = true;
			_stateChangePossibilities[(int)OrderStates.None][(int)OrderStates.Failed] = true;

			_stateChangePossibilities[(int)OrderStates.Pending][(int)OrderStates.None] = false;
			_stateChangePossibilities[(int)OrderStates.Pending][(int)OrderStates.Pending] = true;
			_stateChangePossibilities[(int)OrderStates.Pending][(int)OrderStates.Active] = true;
			_stateChangePossibilities[(int)OrderStates.Pending][(int)OrderStates.Done] = true;
			_stateChangePossibilities[(int)OrderStates.Pending][(int)OrderStates.Failed] = true;

			_stateChangePossibilities[(int)OrderStates.Active][(int)OrderStates.None] = false;
			_stateChangePossibilities[(int)OrderStates.Active][(int)OrderStates.Pending] = false;
			_stateChangePossibilities[(int)OrderStates.Active][(int)OrderStates.Active] = true;
			_stateChangePossibilities[(int)OrderStates.Active][(int)OrderStates.Done] = true;
			_stateChangePossibilities[(int)OrderStates.Active][(int)OrderStates.Failed] = false;

			_stateChangePossibilities[(int)OrderStates.Done][(int)OrderStates.None] = false;
			_stateChangePossibilities[(int)OrderStates.Done][(int)OrderStates.Pending] = false;
			_stateChangePossibilities[(int)OrderStates.Done][(int)OrderStates.Active] = false;
			_stateChangePossibilities[(int)OrderStates.Done][(int)OrderStates.Done] = true;
			_stateChangePossibilities[(int)OrderStates.Done][(int)OrderStates.Failed] = false;

			_stateChangePossibilities[(int)OrderStates.Failed][(int)OrderStates.None] = false;
			_stateChangePossibilities[(int)OrderStates.Failed][(int)OrderStates.Pending] = false;
			_stateChangePossibilities[(int)OrderStates.Failed][(int)OrderStates.Active] = false;
			_stateChangePossibilities[(int)OrderStates.Failed][(int)OrderStates.Done] = false;
			_stateChangePossibilities[(int)OrderStates.Failed][(int)OrderStates.Failed] = true;
		}

		/// <summary>
		/// Check the possibility order's state change.
		/// </summary>
		/// <param name="prev">Previous order's state.</param>
		/// <param name="curr">Current order's state.</param>
		/// <returns>The current order's state.</returns>
		public static OrderStates CheckModification(this OrderStates prev, OrderStates curr)
		{
			if (!_stateChangePossibilities[(int)prev][(int)curr])
				throw new InvalidOperationException($"{prev} -> {curr}");

			return curr;
		}

		/// <summary>
		/// To filter the order book from own orders.
		/// </summary>
		/// <param name="quotes">The initial order book to be filtered.</param>
		/// <param name="ownOrders">Active orders for this instrument.</param>
		/// <param name="orders">Orders to be ignored.</param>
		/// <returns>The filtered order book.</returns>
		public static IEnumerable<Quote> GetFilteredQuotes(this IEnumerable<Quote> quotes, IEnumerable<Order> ownOrders, IEnumerable<Order> orders)
		{
			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));

			if (ownOrders == null)
				throw new ArgumentNullException(nameof(ownOrders));

			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			var dict = new Dictionary<Tuple<Sides, decimal>, HashSet<Order>>();

			foreach (var order in ownOrders)
			{
				if (!dict.SafeAdd(Tuple.Create(order.Direction, order.Price)).Add(order))
					throw new InvalidOperationException(LocalizedStrings.Str415Params.Put(order));
			}

			var retVal = new List<Quote>(quotes.Select(q => q.Clone()));

			foreach (var quote in retVal.ToArray())
			{
				var o = dict.TryGetValue(Tuple.Create(quote.OrderDirection, quote.Price));

				if (o != null)
				{
					foreach (var order in o)
					{
						if (!orders.Contains(order))
							quote.Volume -= order.Balance;
					}

					if (quote.Volume <= 0)
						retVal.Remove(quote);
				}
			}

			return retVal;
		}

		///// <summary>
		///// To get market price for the instrument by maximal and minimal possible prices.
		///// </summary>
		///// <param name="security">The instrument used for the market price calculation.</param>
		///// <param name="provider">The market data provider.</param>
		///// <param name="side">Order side.</param>
		///// <returns>The market price. If there is no information on maximal and minimal possible prices, then <see langword="null" /> will be returned.</returns>
		//public static decimal? GetMarketPrice(this Security security, IMarketDataProvider provider, Sides side)
		//{
		//	var board = security.CheckExchangeBoard();

		//	if (board.IsSupportMarketOrders)
		//		throw new ArgumentException(LocalizedStrings.Str1210Params.Put(board.Code), nameof(security));

		//	var minPrice = (decimal?)provider.GetSecurityValue(security, Level1Fields.MinPrice);
		//	var maxPrice = (decimal?)provider.GetSecurityValue(security, Level1Fields.MaxPrice);

		//	if (side == Sides.Buy && maxPrice != null)
		//		return maxPrice.Value;
		//	else if (side == Sides.Sell && minPrice != null)
		//		return minPrice.Value;
		//	else
		//		return null;
		//		//throw new ArgumentException("У инструмента {0} отсутствует информация о планках.".Put(security), "security");
		//}

		/// <summary>
		/// To calculate the current price by the instrument depending on the order direction.
		/// </summary>
		/// <param name="security">The instrument used for the current price calculation.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="direction">Order side.</param>
		/// <param name="priceType">The type of market price.</param>
		/// <param name="orders">Orders to be ignored.</param>
		/// <returns>The current price. If information in order book is insufficient, then <see langword="null" /> will be returned.</returns>
		public static Unit GetCurrentPrice(this Security security, IMarketDataProvider provider, Sides? direction = null, MarketPriceTypes priceType = MarketPriceTypes.Following, IEnumerable<Order> orders = null)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			var depth = provider.GetMarketDepth(security);

			decimal? currentPrice = null;

			if (direction != null)
			{
				var result = depth.GetCurrentPrice((Sides)direction, priceType, orders);

				if (result != null)
					return result;

				currentPrice = (decimal?)provider.GetSecurityValue(security,
					direction == Sides.Buy ? Level1Fields.BestAskPrice : Level1Fields.BestBidPrice);
			}

			if (currentPrice == null)
				currentPrice = (decimal?)provider.GetSecurityValue(security, Level1Fields.LastTradePrice);

			if (currentPrice == null)
				currentPrice = 0;

			return new Unit((decimal)currentPrice).SetSecurity(security);
		}

		/// <summary>
		/// To calculate the current price by the order book depending on the order direction.
		/// </summary>
		/// <param name="depth">The order book for the current price calculation.</param>
		/// <param name="side">The order direction. If it is a buy, <see cref="MarketDepth.BestAsk"/> value is used, otherwise <see cref="MarketDepth.BestBid"/>.</param>
		/// <param name="priceType">The type of current price.</param>
		/// <param name="orders">Orders to be ignored.</param>
		/// <returns>The current price. If information in order book is insufficient, then <see langword="null" /> will be returned.</returns>
		/// <remarks>
		/// For correct operation of the method the order book export shall be launched.
		/// </remarks>
		public static Unit GetCurrentPrice(this MarketDepth depth, Sides side, MarketPriceTypes priceType = MarketPriceTypes.Following, IEnumerable<Order> orders = null)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			if (orders != null)
			{
				var quotes = depth.GetFilteredQuotes(Enumerable.Empty<Order>(), orders);
				depth = new MarketDepth(depth.Security).Update(quotes, depth.LastChangeTime);
			}

			var pair = depth.BestPair;
			return pair?.GetCurrentPrice(side, priceType);
		}

		/// <summary>
		/// To calculate the current price based on the best pair of quotes, depending on the order direction.
		/// </summary>
		/// <param name="bestPair">The best pair of quotes, used for the current price calculation.</param>
		/// <param name="side">The order direction. If it is a buy, <see cref="MarketDepthPair.Ask"/> value is used, otherwise <see cref="MarketDepthPair.Bid"/>.</param>
		/// <param name="priceType">The type of current price.</param>
		/// <returns>The current price. If information in order book is insufficient, then <see langword="null" /> will be returned.</returns>
		/// <remarks>
		/// For correct operation of the method the order book export shall be launched.
		/// </remarks>
		public static Unit GetCurrentPrice(this MarketDepthPair bestPair, Sides side, MarketPriceTypes priceType = MarketPriceTypes.Following)
		{
			if (bestPair == null)
				throw new ArgumentNullException(nameof(bestPair));

			decimal? currentPrice;

			switch (priceType)
			{
				case MarketPriceTypes.Opposite:
				{
					var quote = (side == Sides.Buy ? bestPair.Ask : bestPair.Bid);
					currentPrice = quote?.Price;
					break;
				}
				case MarketPriceTypes.Following:
				{
					var quote = (side == Sides.Buy ? bestPair.Bid : bestPair.Ask);
					currentPrice = quote?.Price;
					break;
				}
				case MarketPriceTypes.Middle:
				{
					if (bestPair.IsFull)
						currentPrice = bestPair.Bid.Price + bestPair.SpreadPrice / 2;
					else
						currentPrice = null;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(priceType), priceType, LocalizedStrings.Str1219);
			}

			return currentPrice == null
				? null
				: new Unit(currentPrice.Value).SetSecurity(bestPair.Security);
		}

		/// <summary>
		/// To use shifting for price, depending on direction <paramref name="side" />.
		/// </summary>
		/// <param name="price">Price.</param>
		/// <param name="side">The order direction, used as shift direction (for buy the shift is added, for sell - subtracted).</param>
		/// <param name="offset">Price shift.</param>
		/// <param name="security">Security.</param>
		/// <returns>New price.</returns>
		public static decimal ApplyOffset(this Unit price, Sides side, Unit offset, Security security)
		{
			if (price == null)
				throw new ArgumentNullException(nameof(price));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (price.GetTypeValue == null)
				price.SetSecurity(security);

			if (offset.GetTypeValue == null)
				offset.SetSecurity(security);

			return security.ShrinkPrice((decimal)(side == Sides.Buy ? price + offset : price - offset));
		}

		/// <summary>
		/// To cut the price for the order, to make it multiple of the minimal step, also to limit number of decimal places.
		/// </summary>
		/// <param name="order">The order for which the price will be cut <see cref="Order.Price"/>.</param>
		/// <param name="rule">The price rounding rule.</param>
		public static void ShrinkPrice(this Order order, ShrinkRules rule = ShrinkRules.Auto)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			order.Price = order.Security.ShrinkPrice(order.Price, rule);
		}

		/// <summary>
		/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
		/// </summary>
		/// <param name="security">The instrument from which the <see cref="Security.PriceStep"/> and <see cref="Security.Decimals"/> values are taken.</param>
		/// <param name="price">The price to be made multiple.</param>
		/// <param name="rule">The price rounding rule.</param>
		/// <returns>The multiple price.</returns>
		public static decimal ShrinkPrice(this Security security, decimal price, ShrinkRules rule = ShrinkRules.Auto)
		{
			security.CheckPriceStep();

			return price.Round(security.PriceStep ?? 1m, security.Decimals ?? 0,
				rule == ShrinkRules.Auto
					? (MidpointRounding?)null
					: (rule == ShrinkRules.Less ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven)).RemoveTrailingZeros();
		}

		/// <summary>
		/// To get the position on own trade.
		/// </summary>
		/// <param name="trade">Own trade, used for position calculation. At buy the trade volume <see cref="Trade.Volume"/> is taken with positive sign, at sell - with negative.</param>
		/// <returns>Position.</returns>
		public static decimal? GetPosition(this MyTrade trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			return trade.ToMessage().GetPosition(false);
		}

		/// <summary>
		/// To get the position on own trade.
		/// </summary>
		/// <param name="message">Own trade, used for position calculation. At buy the trade volume <see cref="ExecutionMessage.TradeVolume"/> is taken with positive sign, at sell - with negative.</param>
		/// <param name="byOrder">To check implemented volume by order balance (<see cref="ExecutionMessage.Balance"/>) or by received trades. The default is checked by the order.</param>
		/// <returns>Position.</returns>
		public static decimal? GetPosition(this ExecutionMessage message, bool byOrder)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var sign = message.Side == Sides.Buy ? 1 : -1;

			decimal? position;

			if (byOrder)
				position = message.OrderVolume - message.Balance;
			else
				position = message.TradeVolume;

			return position * sign;
		}

		/// <summary>
		/// To calculate profit-loss based on the portfolio.
		/// </summary>
		/// <param name="portfolio">The portfolio, for which the profit-loss shall be calculated.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal? GetPnL(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return portfolio.CurrentValue - portfolio.BeginValue;
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this ExchangeBoard board, DateTimeOffset time)
		{
			return board.ToMessage().IsTradeTime(time, out var _);
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <param name="period">Current working time period.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this ExchangeBoard board, DateTimeOffset time, out WorkingTimePeriod period)
		{
			return board.ToMessage().IsTradeTime(time, out period);
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this BoardMessage board, DateTimeOffset time)
		{
			return board.IsTradeTime(time, out var _);
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <param name="period">Current working time period.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this BoardMessage board, DateTimeOffset time, out WorkingTimePeriod period)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			var exchangeTime = time.ToLocalTime(board.TimeZone);
			var workingTime = board.WorkingTime;

			return workingTime.IsTradeTime(exchangeTime, out period);
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="workingTime">Board working hours.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <param name="period">Current working time period.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this WorkingTime workingTime, DateTime time, out WorkingTimePeriod period)
		{
			var isWorkingDay = workingTime.IsTradeDate(time);

			if (!isWorkingDay)
			{
				period = null;
				return false;
			}

			period = workingTime.GetPeriod(time);

			var tod = time.TimeOfDay;
			return period == null || period.Times.IsEmpty() || period.Times.Any(r => r.Contains(tod));
		}

		/// <summary>
		/// To check, whether date is traded.
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="date">The passed date to be checked.</param>
		/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
		/// <returns><see langword="true" />, if the date is traded, otherwise, is not traded.</returns>
		public static bool IsTradeDate(this ExchangeBoard board, DateTimeOffset date, bool checkHolidays = false)
		{
			return board.ToMessage().IsTradeDate(date, checkHolidays);
		}

		/// <summary>
		/// To check, whether date is traded.
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="date">The passed date to be checked.</param>
		/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
		/// <returns><see langword="true" />, if the date is traded, otherwise, is not traded.</returns>
		public static bool IsTradeDate(this BoardMessage board, DateTimeOffset date, bool checkHolidays = false)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			var exchangeTime = date.ToLocalTime(board.TimeZone);
			var workingTime = board.WorkingTime;

			return workingTime.IsTradeDate(exchangeTime, checkHolidays);
		}

		/// <summary>
		/// To check, whether date is traded.
		/// </summary>
		/// <param name="workingTime">Board working hours.</param>
		/// <param name="date">The passed date to be checked.</param>
		/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
		/// <returns><see langword="true" />, if the date is traded, otherwise, is not traded.</returns>
		public static bool IsTradeDate(this WorkingTime workingTime, DateTime date, bool checkHolidays = false)
		{
			var period = workingTime.GetPeriod(date);

			if ((period == null || period.Times.Count == 0) && workingTime.SpecialWorkingDays.Length == 0 && workingTime.SpecialHolidays.Length == 0)
				return true;

			bool isWorkingDay;

			if (checkHolidays && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
				isWorkingDay = workingTime.SpecialWorkingDays.Contains(date.Date);
			else
				isWorkingDay = !workingTime.SpecialHolidays.Contains(date.Date);

			return isWorkingDay;
		}

		/// <summary>
		/// Get last trade date.
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="date">The date from which to start checking.</param>
		/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
		/// <returns>Last trade date.</returns>
		public static DateTimeOffset LastTradeDay(this BoardMessage board, DateTimeOffset date, bool checkHolidays = true)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			while (!board.IsTradeDate(date, checkHolidays))
				date = date.AddDays(-1);

			return date;
		}

		/// <summary>
		/// To create copy of the order for re-registration.
		/// </summary>
		/// <param name="oldOrder">The original order.</param>
		/// <param name="newPrice">Price of the new order.</param>
		/// <param name="newVolume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		public static Order ReRegisterClone(this Order oldOrder, decimal? newPrice = null, decimal? newVolume = null)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			return new Order
			{
				Portfolio = oldOrder.Portfolio,
				Direction = oldOrder.Direction,
				TimeInForce = oldOrder.TimeInForce,
				Security = oldOrder.Security,
				Type = oldOrder.Type,
				Price = newPrice ?? oldOrder.Price,
				Volume = newVolume ?? oldOrder.Volume,
				ExpiryDate = oldOrder.ExpiryDate,
				VisibleVolume = oldOrder.VisibleVolume,
				BrokerCode = oldOrder.BrokerCode,
				ClientCode = oldOrder.ClientCode,
				RepoInfo = oldOrder.RepoInfo?.Clone(),
				RpsInfo = oldOrder.RpsInfo?.Clone(),
			};
		}

		/// <summary>
		/// To create from regular order book a sparse on, with minimal price step of <see cref="Security.PriceStep"/>.
		/// </summary>
		/// <remarks>
		/// In sparsed book shown quotes with no active orders. The volume of these quotes is 0.
		/// </remarks>
		/// <param name="depth">The regular order book.</param>
		/// <returns>The sparse order book.</returns>
		public static MarketDepth Sparse(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return depth.Sparse(depth.Security.PriceStep ?? 1m);
		}

		/// <summary>
		/// To create from regular order book a sparse one.
		/// </summary>
		/// <remarks>
		/// In sparsed book shown quotes with no active orders. The volume of these quotes is 0.
		/// </remarks>
		/// <param name="depth">The regular order book.</param>
		/// <param name="priceStep">Minimum price step.</param>
		/// <returns>The sparse order book.</returns>
		public static MarketDepth Sparse(this MarketDepth depth, decimal priceStep)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			var bids = depth.Bids.Sparse(priceStep);
			var asks = depth.Asks.Sparse(priceStep);

			var pair = depth.BestPair;
			var spreadQuotes = pair?.Sparse(priceStep).ToArray() ?? ArrayHelper.Empty<Quote>();

			return new MarketDepth(depth.Security).Update(
				bids.Concat(spreadQuotes.Where(q => q.OrderDirection == Sides.Buy)),
				asks.Concat(spreadQuotes.Where(q => q.OrderDirection == Sides.Sell)),
				false, depth.LastChangeTime);
		}

		/// <summary>
		/// To create form pair of quotes a sparse collection of quotes, which will be included into the range between the pair.
		/// </summary>
		/// <remarks>
		/// In sparsed collection shown quotes with no active orders. The volume of these quotes is 0.
		/// </remarks>
		/// <param name="pair">The pair of regular quotes.</param>
		/// <param name="priceStep">Minimum price step.</param>
		/// <returns>The sparse collection of quotes.</returns>
		public static IEnumerable<Quote> Sparse(this MarketDepthPair pair, decimal priceStep)
		{
			if (pair == null)
				throw new ArgumentNullException(nameof(pair));

			if (priceStep <= 0)
				throw new ArgumentOutOfRangeException(nameof(priceStep), priceStep, LocalizedStrings.Str1213);

			if (pair.SpreadPrice == null)
				return Enumerable.Empty<Quote>();

			var security = pair.Bid.Security;

			var retVal = new List<Quote>();

			var bidPrice = pair.Bid.Price;
			var askPrice = pair.Ask.Price;

			while (true)
			{
				bidPrice += priceStep;
				askPrice -= priceStep;

				if (bidPrice > askPrice)
					break;

				retVal.Add(new Quote
				{
					Security = security,
					Price = bidPrice,
					OrderDirection = Sides.Buy,
				});

				if (bidPrice == askPrice)
					break;

				retVal.Add(new Quote
				{
					Security = security,
					Price = askPrice,
					OrderDirection = Sides.Sell,
				});
			}

			return retVal.OrderBy(q => q.Price);
		}

		/// <summary>
		/// To create the sparse collection of quotes from regular quotes.
		/// </summary>
		/// <remarks>
		/// In sparsed collection shown quotes with no active orders. The volume of these quotes is 0.
		/// </remarks>
		/// <param name="quotes">Regular quotes. The collection shall contain quotes of the same direction (only bids or only offers).</param>
		/// <param name="priceStep">Minimum price step.</param>
		/// <returns>The sparse collection of quotes.</returns>
		public static IEnumerable<Quote> Sparse(this IEnumerable<Quote> quotes, decimal priceStep)
		{
			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));

			if (priceStep <= 0)
				throw new ArgumentOutOfRangeException(nameof(priceStep), priceStep, LocalizedStrings.Str1213);

			var list = quotes.OrderBy(q => q.Price).ToList();

			if (list.Count < 2)
				return ArrayHelper.Empty<Quote>();

			var firstQuote = list[0];

			var retVal = new List<Quote>();

			for (var i = 0; i < (list.Count - 1); i++)
			{
				var from = list[i];

				if (from.OrderDirection != firstQuote.OrderDirection)
					throw new ArgumentException(LocalizedStrings.Str1214, nameof(quotes));

				var toPrice = list[i + 1].Price;

				for (var price = (from.Price + priceStep); price < toPrice; price += priceStep)
				{
					retVal.Add(new Quote
					{
						Security = firstQuote.Security,
						Price = price,
						OrderDirection = firstQuote.OrderDirection,
					});
				}
			}

			if (firstQuote.OrderDirection == Sides.Buy)
				return retVal.OrderByDescending(q => q.Price);
			else
				return retVal;
		}

		/// <summary>
		/// To merge the initial order book and its sparse representation.
		/// </summary>
		/// <param name="original">The initial order book.</param>
		/// <param name="rare">The sparse order book.</param>
		/// <returns>The merged order book.</returns>
		public static MarketDepth Join(this MarketDepth original, MarketDepth rare)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			if (rare == null)
				throw new ArgumentNullException(nameof(rare));

			return new MarketDepth(original.Security).Update(original.Concat(rare), original.LastChangeTime);
		}

		/// <summary>
		/// To group the order book by the price range.
		/// </summary>
		/// <param name="depth">The order book to be grouped.</param>
		/// <param name="priceRange">The price range, for which grouping shall be performed.</param>
		/// <returns>The grouped order book.</returns>
		public static MarketDepth Group(this MarketDepth depth, Unit priceRange)
		{
			return new MarketDepth(depth.Security).Update(depth.Bids.Group(priceRange), depth.Asks.Group(priceRange), true, depth.LastChangeTime);
		}

		/// <summary>
		/// To de-group the order book, grouped using the method <see cref="Group(StockSharp.BusinessEntities.MarketDepth,StockSharp.Messages.Unit)"/>.
		/// </summary>
		/// <param name="depth">The grouped order book.</param>
		/// <returns>The de-grouped order book.</returns>
		public static MarketDepth UnGroup(this MarketDepth depth)
		{
			return new MarketDepth(depth.Security).Update(
				depth.Bids.Cast<AggregatedQuote>().SelectMany(gq => gq.InnerQuotes),
				depth.Asks.Cast<AggregatedQuote>().SelectMany(gq => gq.InnerQuotes),
				false, depth.LastChangeTime);
		}

		/// <summary>
		/// To delete in order book levels, which shall disappear in case of trades occurrence <paramref name="trades" />.
		/// </summary>
		/// <param name="depth">The order book to be cleared.</param>
		/// <param name="trades">Trades.</param>
		public static void EmulateTrades(this MarketDepth depth, IEnumerable<ExecutionMessage> trades)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			var changedVolume = new Dictionary<decimal, decimal>();

			var maxTradePrice = decimal.MinValue;
			var minTradePrice = decimal.MaxValue;

			foreach (var trade in trades)
			{
				var price = trade.GetTradePrice();

				minTradePrice = minTradePrice.Min(price);
				maxTradePrice = maxTradePrice.Max(price);

				var quote = depth.GetQuote(price);

				if (null == quote)
					continue;

				if (!changedVolume.TryGetValue(price, out var vol))
					vol = quote.Volume;

				vol -= trade.SafeGetVolume();
				changedVolume[quote.Price] = vol;
			}

			var bids = new Quote[depth.Bids.Length];

			void B1()
			{
				var i = 0;
				var count = 0;

				for (; i < depth.Bids.Length; i++)
				{
					var quote = depth.Bids[i];
					var price = quote.Price;

					if (price > minTradePrice)
						continue;

					if (price == minTradePrice)
					{
						if (changedVolume.TryGetValue(price, out var vol))
						{
							if (vol <= 0)
								continue;

							quote = quote.Clone();
							quote.Volume = vol;
						}
					}

					bids[count++] = quote;
					i++;

					break;
				}

				Array.Copy(depth.Bids, i, bids, count, depth.Bids.Length - i);
				Array.Resize(ref bids, count + (depth.Bids.Length - i));
			}

			B1();

			var asks = new Quote[depth.Asks.Length];

			void A1()
			{
				var i = 0;
				var count = 0;

				for (; i < depth.Asks.Length; i++)
				{
					var quote = depth.Asks[i];
					var price = quote.Price;

					if (price < maxTradePrice)
						continue;

					if (price == maxTradePrice)
					{
						if (changedVolume.TryGetValue(price, out var vol))
						{
							if (vol <= 0)
								continue;

							quote = quote.Clone();
							quote.Volume = vol;
						}
					}

					asks[count++] = quote;
					i++;

					break;
				}

				Array.Copy(depth.Asks, i, asks, count, depth.Asks.Length - i);
				Array.Resize(ref asks, count + (depth.Asks.Length - i));
			}

			A1();

			depth.Update(bids, asks, depth.LastChangeTime);
		}

		/// <summary>
		/// To group quotes by the price range.
		/// </summary>
		/// <param name="quotes">Quotes to be grouped.</param>
		/// <param name="priceRange">The price range, for which grouping shall be performed.</param>
		/// <returns>Grouped quotes.</returns>
		public static IEnumerable<AggregatedQuote> Group(this IEnumerable<Quote> quotes, Unit priceRange)
		{
			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));

			if (priceRange == null)
				throw new ArgumentNullException(nameof(priceRange));

			//if (priceRange.Value < double.Epsilon)
			//	throw new ArgumentOutOfRangeException(nameof(priceRange), priceRange, "Размер группировки меньше допустимого.");

			//if (quotes.Count() < 2)
			//	return Enumerable.Empty<AggregatedQuote>();

			var firstQuote = quotes.FirstOrDefault();

			if (firstQuote == null)
				return Enumerable.Empty<AggregatedQuote>();

			var retVal = quotes.GroupBy(q => priceRange.AlignPrice(firstQuote.Price, q.Price)).Select(g =>
			{
				var aggQuote = new AggregatedQuote(false) { Price = g.Key };
				aggQuote.InnerQuotes.AddRange(g);
				return aggQuote;
			});
			
			retVal = firstQuote.OrderDirection == Sides.Sell ? retVal.OrderBy(q => q.Price) : retVal.OrderByDescending(q => q.Price);

			return retVal;
		}

		internal static decimal AlignPrice(this Unit priceRange, decimal firstPrice, decimal price)
		{
			if (priceRange == null)
				throw new ArgumentNullException(nameof(priceRange));

			decimal priceLevel;

			if (priceRange.Type == UnitTypes.Percent)
				priceLevel = (decimal)(firstPrice + MathHelper.Floor((((price - firstPrice) * 100) / firstPrice), priceRange.Value).Percents());
			else
				priceLevel = MathHelper.Floor(price, (decimal)priceRange);

			return priceLevel;
		}

		/// <summary>
		/// To calculate the change between order books.
		/// </summary>
		/// <param name="from">First order book.</param>
		/// <param name="to">Second order book.</param>
		/// <returns>The order book, storing only increments.</returns>
		public static QuoteChangeMessage GetDelta(this QuoteChangeMessage from, QuoteChangeMessage to)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));

			if (to == null)
				throw new ArgumentNullException(nameof(to));

			return new QuoteChangeMessage
			{
				LocalTime = to.LocalTime,
				SecurityId = to.SecurityId,
				Bids = GetDelta(from.Bids, to.Bids, Sides.Buy),
				Asks = GetDelta(from.Asks, to.Asks, Sides.Sell),
				ServerTime = to.ServerTime,
				IsSorted = true,
			};
		}

		/// <summary>
		/// To calculate the change between quotes.
		/// </summary>
		/// <param name="from">First quotes.</param>
		/// <param name="to">Second quotes.</param>
		/// <param name="side">The direction, showing the type of quotes.</param>
		/// <returns>Changes.</returns>
		public static IEnumerable<QuoteChange> GetDelta(this IEnumerable<QuoteChange> from, IEnumerable<QuoteChange> to, Sides side)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));

			if (to == null)
				throw new ArgumentNullException(nameof(to));

			var mapFrom = new Dictionary<decimal, QuoteChange>();
			var mapTo = new Dictionary<decimal, QuoteChange>();

			foreach (var change in from)
			{
				if (!mapFrom.TryAdd(change.Price, change))
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(change.Price), nameof(from));
			}

			foreach (var change in to)
			{
				if (!mapTo.TryAdd(change.Price, change))
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(change.Price), nameof(to));
			}

			foreach (var pair in mapFrom)
			{
				var price = pair.Key;
				var quoteFrom = pair.Value;

				var quoteTo = mapTo.TryGetValue(price);

				if (quoteTo != null)
				{
					if (quoteTo.Volume == quoteFrom.Volume)
						mapTo.Remove(price);		// то же самое
				}
				else
				{
					var empty = quoteFrom.Clone();
					empty.Volume = 0;				// была, а теперь нет
					mapTo[price] = empty;
				}
			}

			return mapTo
				.Values
				.OrderBy(q => q.Price * (side == Sides.Buy ? -1 : 1))
				.ToArray();
		}

		/// <summary>
		/// To add change to the first order book.
		/// </summary>
		/// <param name="from">First order book.</param>
		/// <param name="delta">Change.</param>
		/// <returns>The changed order book.</returns>
		public static QuoteChangeMessage AddDelta(this QuoteChangeMessage from, QuoteChangeMessage delta)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));

			if (delta == null)
				throw new ArgumentNullException(nameof(delta));

			if (!from.IsSorted)
				throw new ArgumentException(nameof(from));

			if (!delta.IsSorted)
				throw new ArgumentException(nameof(delta));

			return new QuoteChangeMessage
			{
				LocalTime = delta.LocalTime,
				SecurityId = from.SecurityId,
				Bids = AddDelta(from.Bids, delta.Bids, true),
				Asks = AddDelta(from.Asks, delta.Asks, false),
				ServerTime = delta.ServerTime,
				IsSorted = true,
			};
		}

		/// <summary>
		/// To add change to quote.
		/// </summary>
		/// <param name="fromQuotes">Quotes.</param>
		/// <param name="deltaQuotes">Changes.</param>
		/// <param name="isBids">The indication of quotes direction.</param>
		/// <returns>Changed quotes.</returns>
		public static IEnumerable<QuoteChange> AddDelta(this IEnumerable<QuoteChange> fromQuotes, IEnumerable<QuoteChange> deltaQuotes, bool isBids)
		{
			var result = new List<QuoteChange>();

			using (var fromEnu = fromQuotes.GetEnumerator())
			{
				var hasFrom = fromEnu.MoveNext();

				foreach (var quoteChange in deltaQuotes)
				{
					var canAdd = true;

					while (hasFrom)
					{
						var current = fromEnu.Current;

						if (isBids)
						{
							if (current.Price > quoteChange.Price)
								result.Add(current);
							else if (current.Price == quoteChange.Price)
							{
								if (quoteChange.Volume != 0)
									result.Add(quoteChange);

								hasFrom = fromEnu.MoveNext();
								canAdd = false;

								break;
							}
							else
								break;
						}
						else
						{
							if (current.Price < quoteChange.Price)
								result.Add(current);
							else if (current.Price == quoteChange.Price)
							{
								if (quoteChange.Volume != 0)
									result.Add(quoteChange);

								hasFrom = fromEnu.MoveNext();
								canAdd = false;

								break;
							}
							else
								break;
						}

						hasFrom = fromEnu.MoveNext();
					}

					if (canAdd && quoteChange.Volume != 0)
						result.Add(quoteChange);
				}

				while (hasFrom)
				{
					result.Add(fromEnu.Current);
					hasFrom = fromEnu.MoveNext();
				}
			}

			return result;
		}

		/// <summary>
		/// To check, whether the order was cancelled.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if the order is cancelled, otherwise, <see langword="false" />.</returns>
		public static bool IsCanceled(this Order order)
		{
			return order.ToMessage().IsCanceled();
		}

		/// <summary>
		/// To check, is the order matched completely.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if the order is matched completely, otherwise, <see langword="false" />.</returns>
		public static bool IsMatched(this Order order)
		{
			return order.ToMessage().IsMatched();
		}

		/// <summary>
		/// To check, is a part of volume is implemented in the order.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if part of volume is implemented, otherwise, <see langword="false" />.</returns>
		public static bool IsMatchedPartially(this Order order)
		{
			return order.ToMessage().IsMatchedPartially();
		}

		/// <summary>
		/// To check, if no contract in order is implemented.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if no contract is implemented, otherwise, <see langword="false" />.</returns>
		public static bool IsMatchedEmpty(this Order order)
		{
			return order.ToMessage().IsMatchedEmpty();
		}

		/// <summary>
		/// To check, whether the order was cancelled.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if the order is cancelled, otherwise, <see langword="false" />.</returns>
		public static bool IsCanceled(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (order.OrderState != OrderStates.Done)	// для ускорения в эмуляторе
				return false;

			return order.OrderState == OrderStates.Done && order.Balance > 0;
		}

		/// <summary>
		/// To check, is the order matched completely.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if the order is matched completely, otherwise, <see langword="false" />.</returns>
		public static bool IsMatched(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return order.OrderState == OrderStates.Done && order.Balance == 0;
		}

		/// <summary>
		/// To check, is a part of volume is implemented in the order.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if part of volume is implemented, otherwise, <see langword="false" />.</returns>
		public static bool IsMatchedPartially(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return order.Balance > 0 && order.Balance != order.OrderVolume;
		}

		/// <summary>
		/// To check, if no contract in order is implemented.
		/// </summary>
		/// <param name="order">The order to be checked.</param>
		/// <returns><see langword="true" />, if no contract is implemented, otherwise, <see langword="false" />.</returns>
		public static bool IsMatchedEmpty(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return order.Balance > 0 && order.Balance == order.OrderVolume;
		}

		/// <summary>
		/// To get order trades.
		/// </summary>
		/// <param name="order">Orders.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Trades.</returns>
		public static IEnumerable<MyTrade> GetTrades(this Order order, IConnector connector)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			return connector.MyTrades.Filter(order);
		}

		/// <summary>
		/// To calculate the implemented part of volume for order.
		/// </summary>
		/// <param name="order">The order, for which the implemented part of volume shall be calculated.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="byOrder">To check implemented volume by order balance (<see cref="Order.Balance"/>) or by received trades. The default is checked by the order.</param>
		/// <returns>The implemented part of volume.</returns>
		public static decimal GetMatchedVolume(this Order order, IConnector connector, bool byOrder)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (order.Type == OrderTypes.Conditional)
			{
				//throw new ArgumentException("Стоп-заявки не могут иметь реализованный объем.", "order");

				throw new ArgumentException(nameof(order));

				//order = order.DerivedOrder;

				//if (order == null)
				//	return 0;
			}

			return byOrder ? order.Volume - order.Balance : order.GetTrades(connector).Sum(o => o.Trade.Volume);
		}

		/// <summary>
		/// To get weighted mean price of order matching.
		/// </summary>
		/// <param name="order">The order, for which the weighted mean matching price shall be got.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>The weighted mean price. If no order exists no trades, 0 is returned.</returns>
		public static decimal GetAveragePrice(this Order order, IConnector connector)
		{
			return order.GetTrades(connector).GetAveragePrice();
		}

		/// <summary>
		/// To get the weighted mean price of matching by own trades.
		/// </summary>
		/// <param name="trades">Trades, for which the weighted mean price of matching shall be got.</param>
		/// <returns>The weighted mean price. If no trades, 0 is returned.</returns>
		public static decimal GetAveragePrice(this IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			var numerator = 0m;
			var denominator = 0m;
			var currentAvgPrice = 0m;

			foreach (var myTrade in trades)
			{
				var order = myTrade.Order;
				var trade = myTrade.Trade;

				var direction = (order.Direction == Sides.Buy) ? 1m : -1m;

				//Если открываемся или переворачиваемся
				if (direction != denominator.Sign() && trade.Volume > denominator.Abs())
				{
					var newVolume = trade.Volume - denominator.Abs();
					numerator = direction * trade.Price * newVolume;
					denominator = direction * newVolume;
				}
				else
				{
					//Если добавляемся в сторону уже открытой позиции
					if (direction == denominator.Sign())
						numerator += direction * trade.Price * trade.Volume;
					else
						numerator += direction * currentAvgPrice * trade.Volume;

					denominator += direction * trade.Volume;
				}

				currentAvgPrice = (denominator != 0) ? numerator / denominator : 0m;
			}

			return currentAvgPrice;
		}

		/// <summary>
		/// To get probable trades for order book for the given order.
		/// </summary>
		/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
		/// <param name="order">The order, for which probable trades shall be calculated.</param>
		/// <returns>Probable trades.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Order order)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			order = order.ReRegisterClone();
			depth = depth.Clone();

			order.LastChangeTime = depth.LastChangeTime = DateTimeOffset.Now;
			order.LocalTime = depth.LocalTime = DateTime.Now;

			var testPf = new Portfolio { Name = "test account", BeginValue = decimal.MaxValue / 2 };
			order.Portfolio = testPf;

			var trades = new List<MyTrade>();

			using (IMarketEmulator emulator = new MarketEmulator())
			{
				var errors = new List<Exception>();

				emulator.NewOutMessage += msg =>
				{
					if (!(msg is ExecutionMessage execMsg))
						return;

					if (execMsg.Error != null)
						errors.Add(execMsg.Error);

					if (execMsg.HasTradeInfo())
					{
						trades.Add(new MyTrade
						{
							Order = order,
							Trade = execMsg.ToTrade(new Trade { Security = order.Security })
						});
					}
				};

				var depthMsg = depth.ToMessage();
				var regMsg = order.CreateRegisterMessage();
				var pfMsg = testPf.ToChangeMessage();

				pfMsg.ServerTime = depthMsg.ServerTime = order.LastChangeTime;
				pfMsg.LocalTime = regMsg.LocalTime = depthMsg.LocalTime = order.LocalTime;

				emulator.SendInMessage(pfMsg);
				emulator.SendInMessage(depthMsg);
				emulator.SendInMessage(regMsg);

				if (errors.Count > 0)
					throw new AggregateException(errors);
			}

			return trades;
		}

		/// <summary>
		/// To get probable trades by the order book for the market price and given volume.
		/// </summary>
		/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
		/// <param name="orderDirection">Order side.</param>
		/// <param name="volume">The volume, supposed to be implemented.</param>
		/// <returns>Probable trades.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides orderDirection, decimal volume)
		{
			return depth.GetTheoreticalTrades(orderDirection, volume, 0);
		}

		/// <summary>
		/// To get probable trades by order book for given price and volume.
		/// </summary>
		/// <param name="depth">The order book, reflecting situation on market at the moment of function call.</param>
		/// <param name="orderDirection">Order side.</param>
		/// <param name="volume">The volume, supposed to be implemented.</param>
		/// <param name="price">The price, based on which the order is supposed to be forwarded. If it equals 0, option of market order will be considered.</param>
		/// <returns>Probable trades.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides orderDirection, decimal volume, decimal price)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return depth.GetTheoreticalTrades(new Order
			{
				Direction = orderDirection,
				Type = price == 0 ? OrderTypes.Market : OrderTypes.Limit,
				Security = depth.Security,
				Price = price,
				Volume = volume
			});
		}

		/// <summary>
		/// To change the direction to opposite.
		/// </summary>
		/// <param name="side">The initial direction.</param>
		/// <returns>The opposite direction.</returns>
		public static Sides Invert(this Sides side)
		{
			return side == Sides.Buy ? Sides.Sell : Sides.Buy;
		}

		/// <summary>
		/// To get the order direction for the position.
		/// </summary>
		/// <param name="position">The position value.</param>
		/// <returns>Order side.</returns>
		/// <remarks>
		/// A positive value equals <see cref="Sides.Buy"/>, a negative - <see cref="Sides.Sell"/>, zero - <see langword="null" />.
		/// </remarks>
		public static Sides? GetDirection(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			return position.CurrentValue?.GetDirection();
		}

		/// <summary>
		/// To get the order direction for the position.
		/// </summary>
		/// <param name="position">The position value.</param>
		/// <returns>Order side.</returns>
		/// <remarks>
		/// A positive value equals <see cref="Sides.Buy"/>, a negative - <see cref="Sides.Sell"/>, zero - <see langword="null" />.
		/// </remarks>
		public static Sides? GetDirection(this decimal position)
		{
			if (position == 0)
				return null;

			return position > 0 ? Sides.Buy : Sides.Sell;
		}

		/// <summary>
		/// Cancel orders by filter.
		/// </summary>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="orders">The group of orders, from which the required orders shall be found and cancelled.</param>
		/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
		/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
		/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
		/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
		/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
		/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
		public static void CancelOrders(this IConnector connector, IEnumerable<Order> orders, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			orders = orders
				.Where(order => order.State != OrderStates.Done && order.State != OrderStates.Failed)
				.Where(order => isStopOrder == null || (order.Type == OrderTypes.Conditional) == isStopOrder.Value)
				.Where(order => portfolio == null || (order.Portfolio == portfolio))
				.Where(order => direction == null || order.Direction == direction.Value)
				.Where(order => board == null || order.Security.Board == board)
				.Where(order => security == null || order.Security == security)
				.Where(order => securityType == null || order.Security.Type == securityType.Value)
				;

			orders.ForEach(connector.CancelOrder);
		}

		/// <summary>
		/// To check whether specified instrument is used now.
		/// </summary>
		/// <param name="basketSecurity">Instruments basket.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="security">The instrument that should be checked.</param>
		/// <returns><see langword="true" />, if specified instrument is used now, otherwise <see langword="false" />.</returns>
		public static bool Contains(this BasketSecurity basketSecurity, ISecurityProvider securityProvider, Security security)
		{
			return basketSecurity.GetInnerSecurities(securityProvider).Any(innerSecurity =>
			{
				if (innerSecurity is BasketSecurity basket)
					return basket.Contains(securityProvider, security);
				
				return innerSecurity == security;
			});
		}

		/// <summary>
		/// Find inner security instances.
		/// </summary>
		/// <param name="security">Instruments basket.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <returns>Instruments, from which this basket is created.</returns>
		public static IEnumerable<Security> GetInnerSecurities(this BasketSecurity security, ISecurityProvider securityProvider)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (securityProvider == null)
				throw new ArgumentNullException(nameof(securityProvider));

			return security.InnerSecurityIds.Select(id =>
			{
				var innerSec = securityProvider.LookupById(id);

				if (innerSec == null)
					throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(id));

				return innerSec;
			}).ToArray();
		}

		/// <summary>
		/// To filter orders for the given instrument.
		/// </summary>
		/// <param name="orders">All orders, in which the required shall be searched for.</param>
		/// <param name="security">The instrument, for which the orders shall be filtered.</param>
		/// <returns>Filtered orders.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Security security)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var basket = security as BasketSecurity;
			return basket?.InnerSecurityIds.SelectMany(id => orders.Where(o => o.Security.ToSecurityId() == id)) ?? orders.Where(o => o.Security == security);
		}

		/// <summary>
		/// To filter orders for the given portfolio.
		/// </summary>
		/// <param name="orders">All orders, in which the required shall be searched for.</param>
		/// <param name="portfolio">The portfolio, for which the orders shall be filtered.</param>
		/// <returns>Filtered orders.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Portfolio portfolio)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return orders.Where(p => p.Portfolio == portfolio);
		}

		/// <summary>
		/// To filter orders for the given condition.
		/// </summary>
		/// <param name="orders">All orders, in which the required shall be searched for.</param>
		/// <param name="state">Order state.</param>
		/// <returns>Filtered orders.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, OrderStates state)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			return orders.Where(p => p.State == state);
		}

		/// <summary>
		/// To filter orders for the given direction.
		/// </summary>
		/// <param name="orders">All orders, in which the required shall be searched for.</param>
		/// <param name="direction">Order side.</param>
		/// <returns>Filtered orders.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Sides direction)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			return orders.Where(p => p.Direction == direction);
		}

		/// <summary>
		/// To filter orders for the given instrument.
		/// </summary>
		/// <param name="trades">All trades, in which the required shall be searched for.</param>
		/// <param name="security">The instrument, for which the trades shall be filtered.</param>
		/// <returns>Filtered trades.</returns>
		public static IEnumerable<Trade> Filter(this IEnumerable<Trade> trades, Security security)
		{
			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var basket = security as BasketSecurity;
			return basket?.InnerSecurityIds.SelectMany(id => trades.Where(o => o.Security.ToSecurityId() == id)) ?? trades.Where(t => t.Security == security);
		}

		/// <summary>
		/// To filter trades for the given time period.
		/// </summary>
		/// <param name="trades">All trades, in which the required shall be searched for.</param>
		/// <param name="from">The start date for trades searching.</param>
		/// <param name="to">The end date for trades searching.</param>
		/// <returns>Filtered trades.</returns>
		public static IEnumerable<Trade> Filter(this IEnumerable<Trade> trades, DateTimeOffset from, DateTimeOffset to)
		{
			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			return trades.Where(trade => trade.Time >= from && trade.Time < to);
		}

		/// <summary>
		/// To filter positions for the given instrument.
		/// </summary>
		/// <param name="positions">All positions, in which the required shall be searched for.</param>
		/// <param name="security">The instrument, for which positions shall be filtered.</param>
		/// <returns>Filtered positions.</returns>
		public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, Security security)
		{
			if (positions == null)
				throw new ArgumentNullException(nameof(positions));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var basket = security as BasketSecurity;
			return basket?.InnerSecurityIds.SelectMany(id => positions.Where(o => o.Security.ToSecurityId() == id)) ?? positions.Where(p => p.Security == security);
		}

		/// <summary>
		/// To filter positions for the given portfolio.
		/// </summary>
		/// <param name="positions">All positions, in which the required shall be searched for.</param>
		/// <param name="portfolio">The portfolio, for which positions shall be filtered.</param>
		/// <returns>Filtered positions.</returns>
		public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, Portfolio portfolio)
		{
			if (positions == null)
				throw new ArgumentNullException(nameof(positions));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return positions.Where(p => p.Portfolio == portfolio);
		}

		/// <summary>
		/// To filter own trades for the given instrument.
		/// </summary>
		/// <param name="myTrades">All own trades, in which the required shall be looked for.</param>
		/// <param name="security">The instrument, on which the trades shall be found.</param>
		/// <returns>Filtered trades.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Security security)
		{
			if (myTrades == null)
				throw new ArgumentNullException(nameof(myTrades));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var basket = security as BasketSecurity;
			return basket?.InnerSecurityIds.SelectMany(id => myTrades.Where(t => t.Order.Security.ToSecurityId() == id)) ?? myTrades.Where(t => t.Order.Security == security);
		}

		/// <summary>
		/// To filter own trades for the given portfolio.
		/// </summary>
		/// <param name="myTrades">All own trades, in which the required shall be looked for.</param>
		/// <param name="portfolio">The portfolio, for which the trades shall be filtered.</param>
		/// <returns>Filtered trades.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Portfolio portfolio)
		{
			if (myTrades == null)
				throw new ArgumentNullException(nameof(myTrades));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return myTrades.Where(t => t.Order.Portfolio == portfolio);
		}

		/// <summary>
		/// To filter own trades for the given order.
		/// </summary>
		/// <param name="myTrades">All own trades, in which the required shall be looked for.</param>
		/// <param name="order">The order, for which trades shall be filtered.</param>
		/// <returns>Filtered orders.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Order order)
		{
			if (myTrades == null)
				throw new ArgumentNullException(nameof(myTrades));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return myTrades.Where(t => t.Order == order);
		}

		/// <summary>
		/// To filter <see cref="Connector.Securities"/> by given criteria.
		/// </summary>
		/// <param name="connector">Securities.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> FilterSecurities(this Connector connector, SecurityLookupMessage criteria, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var security = connector.GetSecurityCriteria(criteria, exchangeInfoProvider);

			return connector.Securities.Filter(security);
		}

		/// <summary>
		/// To create the search criteria <see cref="Security"/> from <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Search criterion.</returns>
		public static Security GetSecurityCriteria(this Connector connector, SecurityLookupMessage criteria, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			var stocksharpId = criteria.SecurityId.SecurityCode.IsEmpty() || criteria.SecurityId.BoardCode.IsEmpty()
				                   ? string.Empty
				                   : connector.SecurityIdGenerator.GenerateId(criteria.SecurityId.SecurityCode, criteria.SecurityId.BoardCode);

			var secCriteria = new Security { Id = stocksharpId };
			secCriteria.ApplyChanges(criteria, exchangeInfoProvider);
			return secCriteria;
		}

		/// <summary>
		/// To filter instruments by the trading board.
		/// </summary>
		/// <param name="securities">Securities.</param>
		/// <param name="board">Trading board.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, ExchangeBoard board)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return securities.Where(s => s.Board == board);
		}

		/// <summary>
		/// To filter instruments by the given criteria.
		/// </summary>
		/// <param name="securities">Securities.</param>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<SecurityMessage> Filter(this IEnumerable<SecurityMessage> securities, SecurityLookupMessage criteria)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria.IsLookupAll())
				return securities.ToArray();

			//if (!criteria.SecurityId.IsDefault())
			//	return securities.Where(s => s.Id == criteria.Id).ToArray();

			var secId = criteria.SecurityId;

			return securities.Where(s =>
			{
				if (!secId.SecurityCode.IsEmpty() && !s.SecurityId.SecurityCode.ContainsIgnoreCase(secId.SecurityCode))
					return false;

				if (!secId.BoardCode.IsEmpty() && !s.SecurityId.BoardCode.CompareIgnoreCase(secId.BoardCode))
					return false;

				var secType = criteria.SecurityType;

				if (secType != null && s.SecurityType != secType)
					return false;

				var secTypes = criteria.SecurityTypes?.ToArray();

				if (secTypes != null && !secTypes.IsEmpty())
				{
					if (s.SecurityType == null)
						return false;

					if (!secTypes.Contains(s.SecurityType.Value))
						return false;
				}

				var underSecCode = criteria.UnderlyingSecurityCode;

				if (!underSecCode.IsEmpty() && s.UnderlyingSecurityCode != underSecCode)
					return false;

				if (criteria.Strike != null && s.Strike != criteria.Strike)
					return false;

				if (criteria.OptionType != null && s.OptionType != criteria.OptionType)
					return false;

				if (criteria.Currency != null && s.Currency != criteria.Currency)
					return false;

				if (!criteria.Class.IsEmptyOrWhiteSpace() && !s.Class.ContainsIgnoreCase(criteria.Class))
					return false;

				if (!criteria.Name.IsEmptyOrWhiteSpace() && !s.Name.ContainsIgnoreCase(criteria.Name))
					return false;

				if (!criteria.ShortName.IsEmptyOrWhiteSpace() && !s.ShortName.ContainsIgnoreCase(criteria.ShortName))
					return false;

				if (!criteria.CfiCode.IsEmptyOrWhiteSpace() && !s.CfiCode.ContainsIgnoreCase(criteria.CfiCode))
					return false;

				if (!secId.Bloomberg.IsEmptyOrWhiteSpace() && !s.SecurityId.Bloomberg.ContainsIgnoreCase(secId.Bloomberg))
					return false;

				if (!secId.Cusip.IsEmptyOrWhiteSpace() && !s.SecurityId.Cusip.ContainsIgnoreCase(secId.Cusip))
					return false;

				if (!secId.IQFeed.IsEmptyOrWhiteSpace() && !s.SecurityId.IQFeed.ContainsIgnoreCase(secId.IQFeed))
					return false;

				if (!secId.Isin.IsEmptyOrWhiteSpace() && !s.SecurityId.Isin.ContainsIgnoreCase(secId.Isin))
					return false;

				if (!secId.Ric.IsEmptyOrWhiteSpace() && !s.SecurityId.Ric.ContainsIgnoreCase(secId.Ric))
					return false;

				if (!secId.Sedol.IsEmptyOrWhiteSpace() && !s.SecurityId.Sedol.ContainsIgnoreCase(secId.Sedol))
					return false;

				if (criteria.ExpiryDate != null && s.ExpiryDate != null && s.ExpiryDate != criteria.ExpiryDate)
					return false;

				if (criteria.ExtensionInfo != null && criteria.ExtensionInfo.Count > 0)
				{
					if (s.ExtensionInfo == null)
						return false;

					foreach (var pair in criteria.ExtensionInfo)
					{
						var value = s.ExtensionInfo.TryGetValue(pair.Key);

						if (!pair.Value.Equals(value))
							return false;
					}
				}

				return true;
			}).ToArray();
		}

		/// <summary>
		/// To filter instruments by the given criteria.
		/// </summary>
		/// <param name="securities">Securities.</param>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, Security criteria)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria.IsLookupAll())
				return securities.ToArray();

			if (!criteria.Id.IsEmpty())
				return securities.Where(s => s.Id == criteria.Id).ToArray();

			return securities.Where(s =>
			{
				if (!criteria.Code.IsEmpty() && !s.Code.ContainsIgnoreCase(criteria.Code))
					return false;

				var board = criteria.Board;

				if (board != null && s.Board != board)
					return false;

				var type = criteria.Type;

				if (type != null && s.Type != type)
					return false;

				var underSecId = criteria.UnderlyingSecurityId;

				if (!underSecId.IsEmpty() && s.UnderlyingSecurityId != underSecId)
					return false;

				if (criteria.Strike != null && s.Strike != criteria.Strike)
					return false;

				if (criteria.OptionType != null && s.OptionType != criteria.OptionType)
					return false;

				if (criteria.Currency != null && s.Currency != criteria.Currency)
					return false;

				if (!criteria.Class.IsEmptyOrWhiteSpace() && !s.Class.ContainsIgnoreCase(criteria.Class))
					return false;

				if (!criteria.Name.IsEmptyOrWhiteSpace() && !s.Name.ContainsIgnoreCase(criteria.Name))
					return false;

				if (!criteria.ShortName.IsEmptyOrWhiteSpace() && !s.ShortName.ContainsIgnoreCase(criteria.ShortName))
					return false;

				if (!criteria.CfiCode.IsEmptyOrWhiteSpace() && !s.CfiCode.ContainsIgnoreCase(criteria.CfiCode))
					return false;

				if (!criteria.ExternalId.Bloomberg.IsEmptyOrWhiteSpace() && !s.ExternalId.Bloomberg.ContainsIgnoreCase(criteria.ExternalId.Bloomberg))
					return false;

				if (!criteria.ExternalId.Cusip.IsEmptyOrWhiteSpace() && !s.ExternalId.Cusip.ContainsIgnoreCase(criteria.ExternalId.Cusip))
					return false;

				if (!criteria.ExternalId.IQFeed.IsEmptyOrWhiteSpace() && !s.ExternalId.IQFeed.ContainsIgnoreCase(criteria.ExternalId.IQFeed))
					return false;

				if (!criteria.ExternalId.Isin.IsEmptyOrWhiteSpace() && !s.ExternalId.Isin.ContainsIgnoreCase(criteria.ExternalId.Isin))
					return false;

				if (!criteria.ExternalId.Ric.IsEmptyOrWhiteSpace() && !s.ExternalId.Ric.ContainsIgnoreCase(criteria.ExternalId.Ric))
					return false;

				if (!criteria.ExternalId.Sedol.IsEmptyOrWhiteSpace() && !s.ExternalId.Sedol.ContainsIgnoreCase(criteria.ExternalId.Sedol))
					return false;

				if (criteria.ExpiryDate != null && s.ExpiryDate != null && s.ExpiryDate != criteria.ExpiryDate)
					return false;

				if (criteria.ExtensionInfo != null && criteria.ExtensionInfo.Count > 0)
				{
					if (s.ExtensionInfo == null)
						return false;

					foreach (var pair in criteria.ExtensionInfo)
					{
						var value = s.ExtensionInfo.TryGetValue(pair.Key);

						if (!pair.Value.Equals(value))
							return false;
					}
				}

				return true;
			}).ToArray();
		}

		/// <summary>
		/// To determine, is the order book empty.
		/// </summary>
		/// <param name="depth">Market depth.</param>
		/// <returns><see langword="true" />, if order book is empty, otherwise, <see langword="false" />.</returns>
		public static bool IsFullEmpty(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return depth.Bids.Length ==0 && depth.Asks.Length == 0;
		}

		/// <summary>
		/// To determine, is the order book half-empty.
		/// </summary>
		/// <param name="depth">Market depth.</param>
		/// <returns><see langword="true" />, if the order book is half-empty, otherwise, <see langword="false" />.</returns>
		public static bool IsHalfEmpty(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return (depth.BestPair.Bid == null || depth.BestPair.Ask == null) && (depth.BestPair.Bid != depth.BestPair.Ask);
		}

		/// <summary>
		/// To get date of day T +/- of N trading days.
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="date">The start T date, to which are added or subtracted N trading days.</param>
		/// <param name="n">The N size. The number of trading days for the addition or subtraction.</param>
		/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
		/// <returns>The end T +/- N date.</returns>
		public static DateTimeOffset AddOrSubtractTradingDays(this ExchangeBoard board, DateTimeOffset date, int n, bool checkHolidays = true)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			while (n != 0)
			{
				//if need to Add
				if (n > 0)
				{
					date = date.AddDays(1);
					if (board.IsTradeDate(date, checkHolidays)) n--;
				}
				//if need to Subtract
				if (n < 0)
				{
					date = date.AddDays(-1);
					if (board.IsTradeDate(date, checkHolidays)) n++;
				}
			}

			return date;
		}

		/// <summary>
		/// To get the expiration date for <see cref="ExchangeBoard.Forts"/>.
		/// </summary>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		/// <returns>Expiration dates.</returns>
		public static IEnumerable<DateTimeOffset> GetExpiryDates(this DateTime from, DateTime to)
		{
			if (from > to)
				throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str1014.Put(from));

			for (var year = from.Year; year <= to.Year; year++)
			{
				var monthFrom = year == from.Year ? from.Month : 1;
				var monthTo = year == to.Year ? to.Month : 12;

				for (var month = monthFrom; month <= monthTo; month++)
				{
					switch (month)
					{
						case 3:
						case 6:
						case 9:
						case 12:
						{
							var dt = new DateTime(year, month, 15).ApplyTimeZone(ExchangeBoard.Forts.TimeZone);

							while (!ExchangeBoard.Forts.IsTradeDate(dt))
							{
								dt = dt.AddDays(1);
							}
							yield return dt;
							break;
						}
						
						default:
							continue;
					}
				}
			}
		}

		/// <summary>
		/// To get real expiration instruments for base part of the code.
		/// </summary>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		/// <param name="getSecurity">The function to get instrument by the code.</param>
		/// <param name="throwIfNotExists">To generate exception, if some of instruments are not available.</param>
		/// <returns>Expiration instruments.</returns>
		public static IEnumerable<Security> GetFortsJumps(this string baseCode, DateTime from, DateTime to, Func<string, Security> getSecurity, bool throwIfNotExists = true)
		{
			if (baseCode.IsEmpty())
				throw new ArgumentNullException(nameof(baseCode));

			if (from > to)
				throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str1014.Put(from));

			if (getSecurity == null)
				throw new ArgumentNullException(nameof(getSecurity));

			for (var year = from.Year; year <= to.Year; year++)
			{
				var monthFrom = year == from.Year ? from.Month : 1;
				var monthTo = year == to.Year ? to.Month : 12;

				for (var month = monthFrom; month <= monthTo; month++)
				{
					char monthCode;

					switch (month)
					{
						case 3:
							monthCode = 'H';
							break;
						case 6:
							monthCode = 'M';
							break;
						case 9:
							monthCode = 'U';
							break;
						case 12:
							monthCode = 'Z';
							break;
						default:
							continue;
					}

					var yearStr = year.To<string>();
					var code = baseCode + monthCode + yearStr.Substring(yearStr.Length - 1, 1);

					var security = getSecurity(code);

					if (security == null)
					{
						if (throwIfNotExists)
							throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(code));

						continue;
					}
					
					yield return security;
				}
			}
		}

		/// <summary>
		/// To get real expiration instruments for the continuous instrument.
		/// </summary>
		/// <param name="continuousSecurity">Continuous security.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		/// <param name="throwIfNotExists">To generate exception, if some of instruments for passed <paramref name="continuousSecurity" /> are not available.</param>
		/// <returns>Expiration instruments.</returns>
		public static IEnumerable<Security> GetFortsJumps(this ExpirationContinuousSecurity continuousSecurity, ISecurityProvider provider, string baseCode, DateTime from, DateTime to, bool throwIfNotExists = true)
		{
			if (continuousSecurity == null)
				throw new ArgumentNullException(nameof(continuousSecurity));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return baseCode.GetFortsJumps(from, to, code => provider.LookupByCode(code).FirstOrDefault(s => s.Code.CompareIgnoreCase(code)), throwIfNotExists);
		}

		/// <summary>
		/// To fill transitions <see cref="ExpirationContinuousSecurity.ExpirationJumps"/>.
		/// </summary>
		/// <param name="continuousSecurity">Continuous security.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		public static void FillFortsJumps(this ExpirationContinuousSecurity continuousSecurity, ISecurityProvider provider, string baseCode, DateTime from, DateTime to)
		{
			var securities = continuousSecurity.GetFortsJumps(provider, baseCode, from, to);

			foreach (var security in securities)
			{
				var expDate = security.ExpiryDate;

				if (expDate == null)
					throw new InvalidOperationException(LocalizedStrings.Str698Params.Put(security.Id));

				continuousSecurity.ExpirationJumps.Add(security.ToSecurityId(), expDate.Value);
			}
		}

		private sealed class NativePositionManager : IPositionManager
		{
			private readonly Position _position;

			public NativePositionManager(Position position)
			{
				_position = position ?? throw new ArgumentNullException(nameof(position));
			}

			/// <summary>
			/// The position aggregate value.
			/// </summary>
			decimal IPositionManager.Position
			{
				get => _position.CurrentValue ?? 0;
				set => throw new NotSupportedException();
			}

			SecurityId? IPositionManager.SecurityId
			{
				get => _position.Security.ToSecurityId();
				set => throw new NotSupportedException();
			}

			event Action<Tuple<SecurityId, string>, decimal> IPositionManager.NewPosition
			{
				add { }
				remove { }
			}

			event Action<Tuple<SecurityId, string>, decimal> IPositionManager.PositionChanged
			{
				add { }
				remove { }
			}

			IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> IPositionManager.Positions
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			void IPositionManager.Reset()
			{
				throw new NotSupportedException();
			}

			decimal? IPositionManager.ProcessMessage(Message message)
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Convert the position object to the type <see cref="IPositionManager"/>.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <returns>Position calc manager.</returns>
		public static IPositionManager ToPositionManager(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			return new NativePositionManager(position);
		}

		/// <summary>
		/// Write order info to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="order">Order.</param>
		/// <param name="operation">Order action name.</param>
		/// <param name="getAdditionalInfo">Extended order info.</param>
		public static void AddOrderInfoLog(this ILogReceiver receiver, Order order, string operation, Func<string> getAdditionalInfo = null)
		{
			receiver.AddOrderLog(LogLevels.Info, order, operation, getAdditionalInfo);
		}

		/// <summary>
		/// Write order error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="order">Order.</param>
		/// <param name="operation">Order action name.</param>
		/// <param name="getAdditionalInfo">Extended order info.</param>
		public static void AddOrderErrorLog(this ILogReceiver receiver, Order order, string operation, Func<string> getAdditionalInfo = null)
		{
			receiver.AddOrderLog(LogLevels.Error, order, operation, getAdditionalInfo);
		}

		private static void AddOrderLog(this ILogReceiver receiver, LogLevels type, Order order, string operation, Func<string> getAdditionalInfo)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var orderDescription = order.ToString();
			var additionalInfo = getAdditionalInfo == null ? string.Empty : getAdditionalInfo();

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, type, () => "{0}: {1} {2}".Put(operation, orderDescription, additionalInfo)));
		}

		/// <summary>
		/// Apply changes to the portfolio object.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="message">Portfolio change message.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public static void ApplyChanges(this Portfolio portfolio, PortfolioChangeMessage message, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			if (!message.BoardCode.IsEmpty())
				portfolio.Board = exchangeInfoProvider.GetOrCreateBoard(message.BoardCode);

			if (!message.ClientCode.IsEmpty())
				portfolio.ClientCode = message.ClientCode;

			foreach (var change in message.Changes)
			{
				switch (change.Key)
				{
					case PositionChangeTypes.Currency:
						portfolio.Currency = (CurrencyTypes)change.Value;
						break;
					case PositionChangeTypes.Leverage:
						portfolio.Leverage = (decimal)change.Value;
						break;
					case PositionChangeTypes.State:
						portfolio.State = (PortfolioStates)change.Value;
						break;
					case PositionChangeTypes.CommissionMaker:
						portfolio.CommissionMaker = (decimal)change.Value;
						break;
					case PositionChangeTypes.CommissionTaker:
						portfolio.CommissionTaker = (decimal)change.Value;
						break;
					default:
						ApplyChange(portfolio, change);
						break;
				}
			}

			portfolio.LocalTime = message.LocalTime;
			portfolio.LastChangeTime = message.ServerTime;
			message.CopyExtensionInfo(portfolio);
		}

		/// <summary>
		/// Apply changes to the position object.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="message">Position change message.</param>
		public static void ApplyChanges(this Position position, PositionChangeMessage message)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			foreach (var change in message.Changes)
				ApplyChange(position, change);

			position.LocalTime = message.LocalTime;
			position.LastChangeTime = message.ServerTime;
			message.CopyExtensionInfo(position);
		}

		private static void ApplyChange(this BasePosition position, KeyValuePair<PositionChangeTypes, object> change)
		{
			try
			{
				switch (change.Key)
				{
					case PositionChangeTypes.BeginValue:
						position.BeginValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentValue:
						position.CurrentValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.BlockedValue:
						position.BlockedValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentPrice:
						position.CurrentPrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.AveragePrice:
						position.AveragePrice = (decimal)change.Value;
						break;
					//case PositionChangeTypes.ExtensionInfo:
					//	var pair = change.Value.To<KeyValuePair<string, object>>();
					//	position.ExtensionInfo[pair.Key] = pair.Value;
					//	break;
					case PositionChangeTypes.RealizedPnL:
						position.RealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.UnrealizedPnL:
						position.UnrealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.Commission:
						position.Commission = (decimal)change.Value;
						break;
					case PositionChangeTypes.VariationMargin:
						position.VariationMargin = (decimal)change.Value;
						break;
					//case PositionChangeTypes.DepoName:
					//	position.ExtensionInfo[nameof(PositionChangeTypes.DepoName)] = change.Value;
					//	break;
					case PositionChangeTypes.Currency:
						position.Currency = (CurrencyTypes)change.Value;
						break;
					case PositionChangeTypes.ExpirationDate:
						position.ExpirationDate = (DateTimeOffset)change.Value;
						break;
					case PositionChangeTypes.SettlementPrice:
						position.SettlementPrice = (decimal)change.Value;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(change), change.Key, LocalizedStrings.Str1219);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.Str1220Params.Put(change.Key), ex);
			}
		}

		/// <summary>
		/// Apply change to the security object.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="changes">Changes.</param>
		/// <param name="serverTime">Change server time.</param>
		/// <param name="localTime">Local timestamp when a message was received/created.</param>
		public static void ApplyChanges(this Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (changes == null)
				throw new ArgumentNullException(nameof(changes));

			var bidChanged = false;
			var askChanged = false;
			var lastTradeChanged = false;
			var bestBid = security.BestBid != null ? security.BestBid.Clone() : new Quote(security, 0, 0, Sides.Buy);
			var bestAsk = security.BestAsk != null ? security.BestAsk.Clone() : new Quote(security, 0, 0, Sides.Sell);

			var lastTrade = new Trade { Security = security };

			if (security.LastTrade != null)
			{
				lastTrade.Price = security.LastTrade.Price;
				lastTrade.Volume = security.LastTrade.Volume;
			}

			foreach (var pair in changes)
			{
				var value = pair.Value;

				try
				{
					switch (pair.Key)
					{
						case Level1Fields.OpenPrice:
							security.OpenPrice = (decimal)value;
							break;
						case Level1Fields.HighPrice:
							security.HighPrice = (decimal)value;
							break;
						case Level1Fields.LowPrice:
							security.LowPrice = (decimal)value;
							break;
						case Level1Fields.ClosePrice:
							security.ClosePrice = (decimal)value;
							break;
						case Level1Fields.LastTrade:
						{
							lastTrade = (Trade)value;

							lastTrade.Security = security;
							//lastTrade.LocalTime = message.LocalTime;

							lastTradeChanged = true;
							break;
						}
						case Level1Fields.StepPrice:
							security.StepPrice = (decimal)value;
							break;
						case Level1Fields.PriceStep:
							security.PriceStep = (decimal)value;
							break;
						case Level1Fields.Decimals:
							security.Decimals = (int)value;
							break;
						case Level1Fields.VolumeStep:
							security.VolumeStep = (decimal)value;
							break;
						case Level1Fields.Multiplier:
							security.Multiplier = (decimal)value;
							break;
						case Level1Fields.BestBid:
							bestBid = (Quote)value;
							bidChanged = true;
							break;
						case Level1Fields.BestAsk:
							bestAsk = (Quote)value;
							askChanged = true;
							break;
						case Level1Fields.BestBidPrice:
							bestBid.Price = (decimal)value;
							bidChanged = true;
							break;
						case Level1Fields.BestBidVolume:
							bestBid.Volume = (decimal)value;
							bidChanged = true;
							break;
						case Level1Fields.BestAskPrice:
							bestAsk.Price = (decimal)value;
							askChanged = true;
							break;
						case Level1Fields.BestAskVolume:
							bestAsk.Volume = (decimal)value;
							askChanged = true;
							break;
						case Level1Fields.ImpliedVolatility:
							security.ImpliedVolatility = (decimal)value;
							break;
						case Level1Fields.HistoricalVolatility:
							security.HistoricalVolatility = (decimal)value;
							break;
						case Level1Fields.TheorPrice:
							security.TheorPrice = (decimal)value;
							break;
						case Level1Fields.Delta:
							security.Delta = (decimal)value;
							break;
						case Level1Fields.Gamma:
							security.Gamma = (decimal)value;
							break;
						case Level1Fields.Vega:
							security.Vega = (decimal)value;
							break;
						case Level1Fields.Theta:
							security.Theta = (decimal)value;
							break;
						case Level1Fields.Rho:
							security.Rho = (decimal)value;
							break;
						case Level1Fields.MarginBuy:
							security.MarginBuy = (decimal)value;
							break;
						case Level1Fields.MarginSell:
							security.MarginSell = (decimal)value;
							break;
						case Level1Fields.OpenInterest:
							security.OpenInterest = (decimal)value;
							break;
						case Level1Fields.MinPrice:
							security.MinPrice = (decimal)value;
							break;
						case Level1Fields.MaxPrice:
							security.MaxPrice = (decimal)value;
							break;
						case Level1Fields.BidsCount:
							security.BidsCount = (int)value;
							break;
						case Level1Fields.BidsVolume:
							security.BidsVolume = (decimal)value;
							break;
						case Level1Fields.AsksCount:
							security.AsksCount = (int)value;
							break;
						case Level1Fields.AsksVolume:
							security.AsksVolume = (decimal)value;
							break;
						case Level1Fields.State:
							security.State = (SecurityStates)value;
							break;
						case Level1Fields.LastTradePrice:
							lastTrade.Price = (decimal)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.LastTradeVolume:
							lastTrade.Volume = (decimal)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.LastTradeId:
							lastTrade.Id = (long)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.LastTradeTime:
							lastTrade.Time = (DateTimeOffset)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.LastTradeUpDown:
							lastTrade.IsUpTick = (bool)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.LastTradeOrigin:
							lastTrade.OrderDirection = (Sides?)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.IsSystem:
							lastTrade.IsSystem = (bool)value;
							lastTradeChanged = true;
							break;
						case Level1Fields.TradesCount:
							security.TradesCount = (int)value;
							break;
						case Level1Fields.HighBidPrice:
							security.HighBidPrice = (decimal)value;
							break;
						case Level1Fields.LowAskPrice:
							security.LowAskPrice = (decimal)value;
							break;
						case Level1Fields.Yield:
							security.Yield = (decimal)value;
							break;
						case Level1Fields.VWAP:
							security.VWAP = (decimal)value;
							break;
						case Level1Fields.SettlementPrice:
							security.SettlementPrice = (decimal)value;
							break;
						case Level1Fields.AveragePrice:
							security.AveragePrice = (decimal)value;
							break;
						case Level1Fields.Volume:
							security.Volume = (decimal)value;
							break;
						case Level1Fields.Turnover:
							security.Turnover = (decimal)value;
							break;
						case Level1Fields.BuyBackPrice:
							security.BuyBackPrice = (decimal)value;
							break;
						case Level1Fields.BuyBackDate:
							security.BuyBackDate = (DateTimeOffset)value;
							break;
						case Level1Fields.CommissionTaker:
							security.CommissionTaker = (decimal)value;
							break;
						case Level1Fields.CommissionMaker:
							security.CommissionMaker = (decimal)value;
							break;
						//default:
						//	throw new ArgumentOutOfRangeException();
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(LocalizedStrings.Str1220Params.Put(pair.Key), ex);
				}
			}

			if (bidChanged)
				security.BestBid = bestBid;

			if (askChanged)
				security.BestAsk = bestAsk;

			if (lastTradeChanged)
			{
				if (lastTrade.Time.IsDefault())
					lastTrade.Time = serverTime;

				lastTrade.LocalTime = localTime;

				security.LastTrade = lastTrade;
			}

			security.LocalTime = localTime;
			security.LastChangeTime = serverTime;
		}

		/// <summary>
		/// Apply change to the security object.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="message">Changes.</param>
		public static void ApplyChanges(this Security security, Level1ChangeMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			security.ApplyChanges(message.Changes, message.ServerTime, message.LocalTime);
		}

		/// <summary>
		/// Apply change to the security object.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="message">Meta info.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="isOverride">Override previous security data by new values.</param>
		public static void ApplyChanges(this Security security, SecurityMessage message, IExchangeInfoProvider exchangeInfoProvider, bool isOverride = true)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			var secId = message.SecurityId;

			if (!secId.SecurityCode.IsEmpty())
			{
				if (isOverride || security.Code.IsEmpty())
					security.Code = secId.SecurityCode;
			}

			if (!secId.BoardCode.IsEmpty())
			{
				if (isOverride || security.Board == null)
					security.Board = exchangeInfoProvider.GetOrCreateBoard(secId.BoardCode);
			}

			if (message.Currency != null)
			{
				if (isOverride || security.Currency == null)
					security.Currency = message.Currency;
			}

			if (message.ExpiryDate != null)
			{
				if (isOverride || security.ExpiryDate == null)
					security.ExpiryDate = message.ExpiryDate;
			}

			if (message.VolumeStep != null)
			{
				if (isOverride || security.VolumeStep == null)
					security.VolumeStep = message.VolumeStep.Value;
			}

			if (message.Multiplier != null)
			{
				if (isOverride || security.Multiplier == null)
					security.Multiplier = message.Multiplier.Value;
			}

			if (message.PriceStep != null)
			{
				if (isOverride || security.PriceStep == null)
					security.PriceStep = message.PriceStep.Value;

				if (message.Decimals == null && security.Decimals == null)
					security.Decimals = message.PriceStep.Value.GetCachedDecimals();
			}

			if (message.Decimals != null)
			{
				if (isOverride || security.Decimals == null)
					security.Decimals = message.Decimals.Value;

				if (message.PriceStep == null && security.PriceStep == null)
					security.PriceStep = message.Decimals.Value.GetPriceStep();
			}

			if (!message.Name.IsEmpty())
			{
				if (isOverride || security.Name.IsEmpty())
					security.Name = message.Name;
			}

			if (!message.Class.IsEmpty())
			{
				if (isOverride || security.Class.IsEmpty())
					security.Class = message.Class;
			}

			if (message.OptionType != null)
			{
				if (isOverride || security.OptionType == null)
					security.OptionType = message.OptionType;
			}

			if (message.Strike != null)
			{
				if (isOverride || security.Strike == null)
					security.Strike = message.Strike.Value;
			}

			if (!message.BinaryOptionType.IsEmpty())
			{
				if (isOverride || security.BinaryOptionType == null)
					security.BinaryOptionType = message.BinaryOptionType;
			}

			if (message.SettlementDate != null)
			{
				if (isOverride || security.SettlementDate == null)
					security.SettlementDate = message.SettlementDate;
			}

			if (!message.ShortName.IsEmpty())
			{
				if (isOverride || security.ShortName.IsEmpty())
					security.ShortName = message.ShortName;
			}

			if (message.SecurityType != null)
			{
				if (isOverride || security.Type == null)
					security.Type = message.SecurityType.Value;
			}

			if (!message.CfiCode.IsEmpty())
			{
				if (isOverride || security.CfiCode.IsEmpty())
					security.CfiCode = message.CfiCode;

				if (security.Type == null)
					security.Type = security.CfiCode.Iso10962ToSecurityType();

				if (security.Type == SecurityTypes.Option && security.OptionType == null)
				{
					security.OptionType = security.CfiCode.Iso10962ToOptionType();

					//if (security.CfiCode.Length > 2)
					//	security.BinaryOptionType = security.CfiCode.Substring(2);
				}
			}

			if (!message.UnderlyingSecurityCode.IsEmpty())
			{
				if (isOverride || security.UnderlyingSecurityId.IsEmpty())
					security.UnderlyingSecurityId = message.UnderlyingSecurityCode + "@" + secId.BoardCode;
			}

			if (secId.HasExternalId())
			{
				if (isOverride || security.ExternalId.Equals(new SecurityExternalId()))
					security.ExternalId = secId.ToExternalId();
			}

			if (message.IssueDate != null)
			{
				if (isOverride || security.IssueDate == null)
					security.IssueDate = message.IssueDate.Value;
			}

			if (message.IssueSize != null)
			{
				if (isOverride || security.IssueSize == null)
					security.IssueSize = message.IssueSize.Value;
			}

			if (message.UnderlyingSecurityType != null)
			{
				if (isOverride || security.UnderlyingSecurityType == null)
					security.UnderlyingSecurityType = message.UnderlyingSecurityType.Value;
			}

			if (!message.BasketCode.IsEmpty())
			{
				if (isOverride || security.BasketCode.IsEmpty())
					security.BasketCode = message.BasketCode;
			}

			if (!message.BasketExpression.IsEmpty())
			{
				if (isOverride || security.BasketExpression.IsEmpty())
					security.BasketExpression = message.BasketExpression;
			}

			message.CopyExtensionInfo(security);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, object value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, decimal value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, int value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, long value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, SecurityStates value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, SecurityStates? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, Sides value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, Sides? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, CurrencyTypes value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, CurrencyTypes? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, PortfolioStates value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, PortfolioStates? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, DateTimeOffset value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, DateTimeOffset? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Add change into collection.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, bool value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// To add a change to the collection, if value is other than <see langword="null"/>.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, bool? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal value, bool isZeroAcceptable = false)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == 0 && !isZeroAcceptable)
				return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal? value, bool isZeroAcceptable = false)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null)
				return message;

			return message.TryAdd(type, value.Value, isZeroAcceptable);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int value)
			where TMessage : BaseChangeMessage<TChange>
		{
			//if (value == 0)
			//	return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null/* || value == 0*/)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, long value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == 0)
				return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, long? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null || value == 0)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// To convert the currency type into the name in the MICEX format.
		/// </summary>
		/// <param name="type">Currency type.</param>
		/// <returns>The currency name in the MICEX format.</returns>
		public static string ToMicexCurrencyName(this CurrencyTypes type)
		{
			switch (type)
			{
				case CurrencyTypes.RUB:
					return "SUR";
				default:
					return type.GetName();
			}
		}

		/// <summary>
		/// To convert the currency name in the MICEX format into <see cref="CurrencyTypes"/>.
		/// </summary>
		/// <param name="name">The currency name in the MICEX format.</param>
		/// <param name="errorHandler">Error handler.</param>
		/// <returns>Currency type. If the value is empty, <see langword="null" /> will be returned.</returns>
		public static CurrencyTypes? FromMicexCurrencyName(this string name, Action<Exception> errorHandler = null)
		{
			if (name.IsEmpty())
				return null;

			switch (name)
			{
				case "SUR":
				case "RUR":
					return CurrencyTypes.RUB;
				case "PLD":
				case "PLT":
				case "GLD":
				case "SLV":
					return null;
				default:
				{
					try
					{
						return name.To<CurrencyTypes>();
					}
					catch (Exception ex)
					{
						errorHandler?.Invoke(ex);
						return null;
					}
				}
			}
		}

		/// <summary>
		/// To get the instrument description by the class.
		/// </summary>
		/// <param name="securityClassInfo">Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns>The instrument description. If the class is not found, then empty value is returned as instrument type.</returns>
		public static Tuple<SecurityTypes?, string> GetSecurityClassInfo(this IDictionary<string, RefPair<SecurityTypes, string>> securityClassInfo, string secClass)
		{
			var pair = securityClassInfo.TryGetValue(secClass);
			return Tuple.Create(pair?.First, pair == null ? secClass : pair.Second);
		}

		/// <summary>
		/// To get the board code for the instrument class.
		/// </summary>
		/// <param name="adapter">Adapter to the trading system.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns>Board code.</returns>
		public static string GetBoardCode(this IMessageAdapter adapter, string secClass)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SecurityClassInfo.GetSecurityClassInfo(secClass).Item2;
		}

		/// <summary>
		/// To get the price increment on the basis of accuracy.
		/// </summary>
		/// <param name="decimals">Decimals.</param>
		/// <returns>Price step.</returns>
		public static decimal GetPriceStep(this int decimals)
		{
			return 1m / 10.Pow(decimals);
		}

		/// <summary>
		/// The delimiter, replacing '/' in path for instruments with id like USD/EUR. Is equal to '__'.
		/// </summary>
		public const string SecurityPairSeparator = "__";

		/// <summary>
		/// The delimiter, replacing '*' in the path for instruments with id like C.BPO-*@CANADIAN. Is equal to '##STAR##'.
		/// </summary>
		public const string SecurityStarSeparator = "##STAR##";
		// http://stocksharp.com/forum/yaf_postst4637_API-4-2-2-18--System-ArgumentException--Illegal-characters-in-path.aspx

		/// <summary>
		/// The delimiter, replacing ':' in the path for instruments with id like AA-CA:SPB@SPBEX. Is equal to '##COLON##'.
		/// </summary>
		public const string SecurityColonSeparator = "##COLON##";

		/// <summary>
		/// The delimiter, replacing '|' in the path for instruments with id like AA-CA|SPB@SPBEX. Is equal to '##VBAR##'.
		/// </summary>
		public const string SecurityVerticalBarSeparator = "##VBAR##";

		/// <summary>
		/// The delimiter, replacing first '.' in the path for instruments with id like .AA-CA@SPBEX. Is equal to '##DOT##'.
		/// </summary>
		public const string SecurityFirstDot = "##DOT##";

		///// <summary>
		///// The delimiter, replacing first '..' in the path for instruments with id like ..AA-CA@SPBEX. Is equal to '##DDOT##'.
		///// </summary>
		//public const string SecurityFirst2Dots = "##DDOT##";

		private static readonly CachedSynchronizedDictionary<string, string> _securitySeparators = new CachedSynchronizedDictionary<string, string>
		{
			{ "/", SecurityPairSeparator },
			{ "*", SecurityStarSeparator },
			{ ":", SecurityColonSeparator },
			{ "|", SecurityVerticalBarSeparator },
		};

		// http://stackoverflow.com/questions/62771/how-check-if-given-string-is-legal-allowed-file-name-under-windows
		private static readonly string[] _reservedDos =
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		/// <summary>
		/// To convert the instrument identifier into the folder name, replacing reserved symbols.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Directory name.</returns>
		public static string SecurityIdToFolderName(this string id)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			var folderName = id;

			if (_reservedDos.Any(d => folderName.StartsWithIgnoreCase(d)))
				folderName = "_" + folderName;

			if (folderName.StartsWithIgnoreCase("."))
				folderName = SecurityFirstDot + folderName.Remove(0, 1);

			return _securitySeparators
				.CachedPairs
				.Aggregate(folderName, (current, pair) => current.Replace(pair.Key, pair.Value));
		}

		/// <summary>
		/// The inverse conversion from the <see cref="SecurityIdToFolderName"/> method.
		/// </summary>
		/// <param name="folderName">Directory name.</param>
		/// <returns>Security ID.</returns>
		public static string FolderNameToSecurityId(this string folderName)
		{
			if (folderName.IsEmpty())
				throw new ArgumentNullException(nameof(folderName));

			var id = folderName.ToUpperInvariant();

			if (id[0] == '_' && _reservedDos.Any(d => id.StartsWithIgnoreCase("_" + d)))
				id = id.Substring(1);

			if (id.StartsWithIgnoreCase(SecurityFirstDot))
				id = id.ReplaceIgnoreCase(SecurityFirstDot, ".");

			return _securitySeparators
				.CachedPairs
				.Aggregate(id, (current, pair) => current.ReplaceIgnoreCase(pair.Value, pair.Key));
		}

		/// <summary>
		/// Convert candle parameter into folder name replacing the reserved symbols.
		/// </summary>
		/// <param name="arg">Candle arg.</param>
		/// <returns>Directory name.</returns>
		public static string CandleArgToFolderName(object arg)
		{
			switch (arg)
			{
				case null:
					return string.Empty;
				case PnFArg pnf:
					return $"{pnf.BoxSize}_{pnf.ReversalAmount}";
				default:
					return arg.ToString().Replace(':', '-');
			}
		}

		/// <summary>
		/// To get the instrument by the identifier.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="id">Security ID.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static Security LookupById(this ISecurityProvider provider, SecurityId id)
		{
			return provider.LookupById(id.ToStringId());
		}

		/// <summary>
		/// To get the instrument by the identifier.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="id">Security ID.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static Security LookupById(this ISecurityProvider provider, string id)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			return provider.Lookup(new Security { Id = id }).SingleOrDefault();
		}

		/// <summary>
		/// To get the portfolio by the code name.
		/// </summary>
		/// <param name="provider">The provider of information about portfolios.</param>
		/// <param name="id">Portfolio code name.</param>
		/// <returns>The got portfolio. If there is no portfolio by given criteria, <see langword="null" /> is returned.</returns>
		public static Portfolio LookupByPortfolioName(this IPortfolioProvider provider, string id)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			return provider.Portfolios.SingleOrDefault(s => s.Name.CompareIgnoreCase(id));
		}

		/// <summary>
		/// To get the instrument by the system identifier.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="nativeIdStorage">Security native identifier storage.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static Security LookupByNativeId(this ISecurityProvider provider, INativeIdStorage nativeIdStorage, string storageName, object nativeId)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (nativeIdStorage == null)
				throw new ArgumentNullException(nameof(nativeIdStorage));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			var secId = nativeIdStorage.TryGetByNativeId(storageName, nativeId);

			return secId == null ? null : provider.LookupById(secId.Value);
		}

		/// <summary>
		/// To get the instrument by the instrument code.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="code">Security code.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static IEnumerable<Security> LookupByCode(this ISecurityProvider provider, string code)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return code.IsEmpty()
				? provider.LookupAll()
				: provider.Lookup(new Security { Code = code });
		}

		/// <summary>
		/// Lookup all securities predefined criteria.
		/// </summary>
		public static readonly Security LookupAllCriteria = new Security();

		/// <summary>
		/// Lookup all securities predefined criteria.
		/// </summary>
		public static readonly SecurityLookupMessage LookupAllCriteriaMessage = LookupAllCriteria.ToLookupMessage();

		/// <summary>
		/// Determine the <paramref name="criteria"/> contains lookup all filter.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Check result.</returns>
		public static bool IsLookupAll(this Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria == LookupAllCriteria)
				return true;

			return
				criteria.Id.IsEmpty() &&
				criteria.Code.IsEmpty() &&
				criteria.Board == null &&
				criteria.ExpiryDate == null &&
				criteria.Type == null &&
				criteria.OptionType == null &&
				criteria.Strike == null &&
				criteria.CfiCode.IsEmpty() &&
				criteria.Class.IsEmpty() &&
				criteria.Currency == null &&
				criteria.Decimals == null &&
				criteria.Name.IsEmpty() &&
				criteria.UnderlyingSecurityType == null &&
				criteria.UnderlyingSecurityId.IsEmpty() &&
				criteria.BinaryOptionType.IsEmpty();
		}

		/// <summary>
		/// Determine the <paramref name="criteria"/> contains lookup all filter.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Check result.</returns>
		public static bool IsLookupAll(this SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria == LookupAllCriteriaMessage)
				return true;

			return
				criteria.SecurityId.IsDefault() &&
				criteria.SecurityType == null &&
				criteria.Name.IsEmpty() &&
				criteria.ShortName.IsEmpty() &&
				criteria.UnderlyingSecurityCode.IsEmpty() &&
				criteria.UnderlyingSecurityType == null &&
				criteria.ExpiryDate == null &&
				criteria.OptionType == null &&
				criteria.Strike == null;
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <returns>All available instruments.</returns>
		public static IEnumerable<Security> LookupAll(this ISecurityProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Lookup(LookupAllCriteria);
		}

		/// <summary>
		/// To delete all instruments.
		/// </summary>
		/// <param name="storage">Securities meta info storage.</param>
		public static void DeleteAll(this ISecurityStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.DeleteBy(LookupAllCriteria);
		}

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <typeparam name="T">The type of the market data field value.</typeparam>
		/// <param name="provider">The market data provider.</param>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public static T GetSecurityValue<T>(this IMarketDataProvider provider, Security security, Level1Fields field)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return (T)provider.GetSecurityValue(security, field);
		}

		/// <summary>
		/// To get all market data values for the instrument.
		/// </summary>
		/// <param name="provider">The market data provider.</param>
		/// <param name="security">Security.</param>
		/// <returns>Filed values. If there is no data, <see langword="null" /> is returned.</returns>
		public static IDictionary<Level1Fields, object> GetSecurityValues(this IMarketDataProvider provider, Security security)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var fields = provider.GetLevel1Fields(security).ToArray();

			if (fields.IsEmpty())
				return null;

			return fields.ToDictionary(f => f, f => provider.GetSecurityValue(security, f));
		}

		///// <summary>
		///// To deduce the adapter to the <typeparamref name="T" /> type.
		///// </summary>
		///// <typeparam name="T">The adapter type.</typeparam>
		///// <param name="adapter">The initial adapter.</param>
		///// <returns>Adapter.</returns>
		//public static T To<T>(this IMessageAdapter adapter)
		//	where T : class, IMessageAdapter
		//{
		//	if (adapter == null)
		//		throw new ArgumentNullException(nameof(adapter));

		//	var outAdapter = adapter as T;

		//	if (outAdapter != null)
		//		return outAdapter;

		//	var managedAdapter = adapter as ManagedMessageAdapter;

		//	if (managedAdapter != null)
		//		return managedAdapter.InnerAdapter.To<T>();

		//	throw new InvalidCastException(LocalizedStrings.Str3843.Put(adapter.GetType(), typeof(T)));
		//}

		///// <summary>
		///// To convert the adapter into <see cref="ChannelMessageAdapter"/>.
		///// </summary>
		///// <param name="adapter">Adapter.</param>
		///// <param name="connector">The connection. It is used to determine the channel name.</param>
		///// <param name="name">The channel name.</param>
		///// <returns>Message adapter, forward messages through a transport channel <see cref="IMessageChannel"/>.</returns>
		//public static ChannelMessageAdapter ToChannel(this IMessageAdapter adapter, Connector connector, string name = null)
		//{
		//	name = name ?? connector.GetType().GetDisplayName();
		//	return new ChannelMessageAdapter(adapter, new InMemoryMessageChannel(name, connector.SendOutError), new PassThroughMessageChannel())
		//	{
		//		OwnInputChannel = true
		//	};
		//}

		private const double _minValue = (double)decimal.MinValue;
		private const double _maxValue = (double)decimal.MaxValue;

		/// <summary>
		/// To convert <see cref="double"/> into <see cref="decimal"/>. If the initial value is <see cref="double.NaN"/> or <see cref="double.IsInfinity"/>, <see langword="null" /> is returned.
		/// </summary>
		/// <param name="value"><see cref="double"/> value.</param>
		/// <returns><see cref="decimal"/> value.</returns>
		public static decimal? ToDecimal(this double value)
		{
			return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
		}

		/// <summary>
		/// To convert <see cref="float"/> into <see cref="decimal"/>. If the initial value is <see cref="float.NaN"/> or <see cref="float.IsInfinity"/>, <see langword="null" /> is returned.
		/// </summary>
		/// <param name="value"><see cref="float"/> value.</param>
		/// <returns><see cref="decimal"/> value.</returns>
		public static decimal? ToDecimal(this float value)
		{
			return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
		}

		/// <summary>
		/// To get the type for the instrument in the ISO 10962 standard.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Type in ISO 10962 standard.</returns>
		public static string Iso10962(this SecurityMessage security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			// https://en.wikipedia.org/wiki/ISO_10962

			switch (security.SecurityType)
			{
				case SecurityTypes.Stock:
					return "ESXXXX";
				case SecurityTypes.Future:
					return "FFXXXX";
				case SecurityTypes.Option:
				{
					switch (security.OptionType)
					{
						case OptionTypes.Call:
							return "OCXXXX";
						case OptionTypes.Put:
							return "OPXXXX";
						case null:
							return "OXXXXX";
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				case SecurityTypes.Index:
					return "MRIXXX";
				case SecurityTypes.Currency:
					return "MRCXXX";
				case SecurityTypes.Bond:
					return "DBXXXX";
				case SecurityTypes.Warrant:
					return "RWXXXX";
				case SecurityTypes.Forward:
					return "FFMXXX";
				case SecurityTypes.Swap:
					return "FFWXXX";
				case SecurityTypes.Commodity:
					return "MRTXXX";
				case SecurityTypes.Cfd:
					return "MMCXXX";
				case SecurityTypes.Adr:
					return "MMAXXX";
				case SecurityTypes.News:
					return "MMNXXX";
				case SecurityTypes.Weather:
					return "MMWXXX";
				case SecurityTypes.Fund:
					return "EUXXXX";
				case SecurityTypes.CryptoCurrency:
					return "MMBXXX";
				case null:
					return "XXXXXX";
				default:
					throw new ArgumentOutOfRangeException(nameof(security), security.SecurityType, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// To convert the type in the ISO 10962 standard into <see cref="SecurityTypes"/>.
		/// </summary>
		/// <param name="cfi">Type in ISO 10962 standard.</param>
		/// <returns>Security type.</returns>
		public static SecurityTypes? Iso10962ToSecurityType(this string cfi)
		{
			if (cfi.IsEmpty())
			{
				return null;
				//throw new ArgumentNullException(nameof(cfi));
			}

			if (cfi.Length != 6)
			{
				return null;
				//throw new ArgumentOutOfRangeException(nameof(cfi), cfi, LocalizedStrings.Str2117);
			}

			switch (cfi[0])
			{
				case 'E':
					return SecurityTypes.Stock;

				case 'D':
					return SecurityTypes.Bond;

				case 'R':
					return SecurityTypes.Warrant;

				case 'O':
					return SecurityTypes.Option;

				case 'F':
				{
					switch (cfi[2])
					{
						case 'W':
							return SecurityTypes.Swap;

						case 'M':
							return SecurityTypes.Forward;

						default:
							return SecurityTypes.Future;
					}
				}

				case 'M':
				{
					switch (cfi[1])
					{
						case 'R':
						{
							switch (cfi[2])
							{
								case 'I':
									return SecurityTypes.Index;

								case 'C':
									return SecurityTypes.Currency;

								case 'R':
									return SecurityTypes.Currency;

								case 'T':
									return SecurityTypes.Commodity;
							}

							break;
						}

						case 'M':
						{
							switch (cfi[2])
							{
								case 'B':
									return SecurityTypes.CryptoCurrency;

								case 'W':
									return SecurityTypes.Weather;

								case 'A':
									return SecurityTypes.Adr;

								case 'C':
									return SecurityTypes.Cfd;

								case 'N':
									return SecurityTypes.News;
							}

							break;
						}
					}

					break;
				}
			}

			return null;
		}

		/// <summary>
		/// To convert the type in the ISO 10962 standard into <see cref="OptionTypes"/>.
		/// </summary>
		/// <param name="cfi">Type in ISO 10962 standard.</param>
		/// <returns>Option type.</returns>
		public static OptionTypes? Iso10962ToOptionType(this string cfi)
		{
			if (cfi.IsEmpty())
				throw new ArgumentNullException(nameof(cfi));

			if (cfi[0] != 'O')
				return null;
				//throw new ArgumentOutOfRangeException(nameof(cfi), LocalizedStrings.Str1604Params.Put(cfi));

			if (cfi.Length < 2)
				throw new ArgumentOutOfRangeException(nameof(cfi), LocalizedStrings.Str1605Params.Put(cfi));

			switch (cfi[1])
			{
				case 'C':
					return OptionTypes.Call;
				case 'P':
					return OptionTypes.Put;
				case 'X':
				case ' ':
					return null;
				default:
					throw new ArgumentOutOfRangeException(nameof(cfi), LocalizedStrings.Str1606Params.Put(cfi));
			}
		}

		/// <summary>
		/// To get the number of operations, or discard the exception, if no information available.
		/// </summary>
		/// <param name="message">Operations.</param>
		/// <returns>Quantity.</returns>
		public static decimal SafeGetVolume(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var volume = message.OrderVolume ?? message.TradeVolume;

			if (volume != null)
				return volume.Value;

			var errorMsg = message.ExecutionType == ExecutionTypes.Tick || message.HasTradeInfo()
				? LocalizedStrings.Str1022Params.Put((object)message.TradeId ?? message.TradeStringId)
				: LocalizedStrings.Str927Params.Put((object)message.OrderId ?? message.OrderStringId);

			throw new ArgumentOutOfRangeException(nameof(message), null, errorMsg);
		}

		/// <summary>
		/// To get order identifier, or discard exception, if no information available.
		/// </summary>
		/// <param name="message">Operations.</param>
		/// <returns>Order ID.</returns>
		public static long SafeGetOrderId(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var orderId = message.OrderId;

			if (orderId != null)
				return orderId.Value;

			throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.Str925);
		}

		private class TickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
		{
			private class TickEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator;

				public TickEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator)
				{
					_level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));
				}

				public ExecutionMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_level1Enumerator.MoveNext())
					{
						var level1 = _level1Enumerator.Current;

						if (!level1.IsContainsTick())
							continue;

						Current = level1.ToTick();
						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_level1Enumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_level1Enumerator.Dispose();
				}
			}

			//private readonly IEnumerable<Level1ChangeMessage> _level1;

			public TickEnumerable(IEnumerable<Level1ChangeMessage> level1)
				: base(() => new TickEnumerator(level1.GetEnumerator()))
			{
				if (level1 == null)
					throw new ArgumentNullException(nameof(level1));

				//_level1 = level1;
			}

			//int IEnumerableEx.Count => _level1.Count;
		}

		/// <summary>
		/// To convert level1 data into tick data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Tick data.</returns>
		public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<Level1ChangeMessage> level1)
		{
			return new TickEnumerable(level1);
		}

		/// <summary>
		/// To check, are there tick data in the level1 data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>The test result.</returns>
		public static bool IsContainsTick(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return level1.Changes.ContainsKey(Level1Fields.LastTradePrice);
		}

		/// <summary>
		/// To convert level1 data into tick data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Tick data.</returns>
		public static ExecutionMessage ToTick(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = level1.SecurityId,
				TradeId = (long?)level1.Changes.TryGetValue(Level1Fields.LastTradeId),
				TradePrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.LastTradePrice),
				TradeVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.LastTradeVolume),
				OriginSide = (Sides?)level1.Changes.TryGetValue(Level1Fields.LastTradeOrigin),
				ServerTime = (DateTimeOffset?)level1.Changes.TryGetValue(Level1Fields.LastTradeTime) ?? level1.ServerTime,
				IsUpTick = (bool?)level1.Changes.TryGetValue(Level1Fields.LastTradeUpDown),
				LocalTime = level1.LocalTime,
			};
		}

		private class OrderBookEnumerable : SimpleEnumerable<QuoteChangeMessage>//, IEnumerableEx<QuoteChangeMessage>
		{
			private class OrderBookEnumerator : IEnumerator<QuoteChangeMessage>
			{
				private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator;

				private decimal? _prevBidPrice;
				private decimal? _prevBidVolume;
				private decimal? _prevAskPrice;
				private decimal? _prevAskVolume;

				public OrderBookEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator)
				{
					_level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));
				}

				public QuoteChangeMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_level1Enumerator.MoveNext())
					{
						var level1 = _level1Enumerator.Current;

						if (!level1.IsContainsQuotes())
							continue;

						var prevBidPrice = _prevBidPrice;
						var prevBidVolume = _prevBidVolume;
						var prevAskPrice = _prevAskPrice;
						var prevAskVolume = _prevAskVolume;

						_prevBidPrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestBidPrice) ?? _prevBidPrice;
						_prevBidVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? _prevBidVolume;
						_prevAskPrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestAskPrice) ?? _prevAskPrice;
						_prevAskVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? _prevAskVolume;

						if (_prevBidPrice == 0)
							_prevBidPrice = null;

						if (_prevAskPrice == 0)
							_prevAskPrice = null;

						if (prevBidPrice == _prevBidPrice && prevBidVolume == _prevBidVolume && prevAskPrice == _prevAskPrice && prevAskVolume == _prevAskVolume)
							continue;

						Current = new QuoteChangeMessage
						{
							SecurityId = level1.SecurityId,
							LocalTime = level1.LocalTime,
							ServerTime = level1.ServerTime,
							Bids = _prevBidPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Buy, _prevBidPrice.Value, _prevBidVolume ?? 0) },
							Asks = _prevAskPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Sell, _prevAskPrice.Value, _prevAskVolume ?? 0) },
						};

						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_level1Enumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_level1Enumerator.Dispose();
				}
			}

			//private readonly IEnumerable<Level1ChangeMessage> _level1;

			public OrderBookEnumerable(IEnumerable<Level1ChangeMessage> level1)
				: base(() => new OrderBookEnumerator(level1.GetEnumerator()))
			{
				if (level1 == null)
					throw new ArgumentNullException(nameof(level1));

				//_level1 = level1;
			}

			//int IEnumerableEx.Count => _level1.Count;
		}

		/// <summary>
		/// To convert level1 data into order books.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<Level1ChangeMessage> level1)
		{
			return new OrderBookEnumerable(level1);
		}

		/// <summary>
		/// To check, are there quotes in the level1.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Quotes.</returns>
		public static bool IsContainsQuotes(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return level1.Changes.ContainsKey(Level1Fields.BestBidPrice) || level1.Changes.ContainsKey(Level1Fields.BestAskPrice);
		}

		/// <summary>
		/// To check the specified date is today.
		/// </summary>
		/// <param name="date">The specified date.</param>
		/// <returns><see langword="true"/> if the specified date is today, otherwise, <see langword="false"/>.</returns>
		public static bool IsToday(this DateTimeOffset date)
		{
			return date.DateTime == DateTime.Today;
		}

		///// <summary>
		///// To check the specified date is GTC.
		///// </summary>
		///// <param name="date">The specified date.</param>
		///// <returns><see langword="true"/> if the specified date is GTC, otherwise, <see langword="false"/>.</returns>
		//public static bool IsGtc(this DateTimeOffset date)
		//{
		//	return date == DateTimeOffset.MaxValue;
		//}

		/// <summary>
		/// Extract <see cref="TimeInForce"/> from bits flag.
		/// </summary>
		/// <param name="status">Bits flag.</param>
		/// <returns><see cref="TimeInForce"/>.</returns>
		public static TimeInForce? GetPlazaTimeInForce(this long status)
		{
			if (status.HasBits(0x1))
				return TimeInForce.PutInQueue;
			else if (status.HasBits(0x2))
				return TimeInForce.CancelBalance;
			else if (status.HasBits(0x80000))
				return TimeInForce.MatchOrCancel;

			return null;
		}

		/// <summary>
		/// Extract system attribute from the bits flag.
		/// </summary>
		/// <param name="status">Bits flag.</param>
		/// <returns><see langword="true"/> if an order is system, otherwise, <see langword="false"/>.</returns>
		public static bool IsPlazaSystem(this long status)
		{
			return !status.HasBits(0x4);
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to readable string.
		/// </summary>
		/// <param name="dt"><see cref="DataType"/> instance.</param>
		/// <returns>Readable string.</returns>
		public static string ToReadableString(this DataType dt)
		{
			if (dt == null)
				throw new ArgumentNullException(nameof(dt));

			var tf = (TimeSpan)dt.Arg;

			var str = string.Empty;

			if (tf.Days > 0)
				str += LocalizedStrings.Str2918Params.Put(tf.Days);

			if (tf.Hours > 0)
				str = (str + " " + LocalizedStrings.Str2919Params.Put(tf.Hours)).Trim();

			if (tf.Minutes > 0)
				str = (str + " " + LocalizedStrings.Str2920Params.Put(tf.Minutes)).Trim();

			if (tf.Seconds > 0)
				str = (str + " " + LocalizedStrings.Seconds.Put(tf.Seconds)).Trim();

			if (str.IsEmpty())
				str = LocalizedStrings.Ticks;

			return str;
		}

		/// <summary>
		/// To get a board by its code. If board with the passed name does not exist, then it will be created.
		/// </summary>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="code">Board code.</param>
		/// <param name="createBoard">The handler creating a board, if it is not found. If the value is <see langword="null" />, then the board is created by default initialization.</param>
		/// <returns>Exchange board.</returns>
		public static ExchangeBoard GetOrCreateBoard(this IExchangeInfoProvider exchangeInfoProvider, string code, Func<string, ExchangeBoard> createBoard = null)
		{
			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			//if (code.CompareIgnoreCase("RTS"))
			//	return ExchangeBoard.Forts;

			var board = exchangeInfoProvider.GetExchangeBoard(code);

			if (board != null)
				return board;

			if (createBoard == null)
			{
				var exchange = exchangeInfoProvider.GetExchange(code);

				if (exchange == null)
				{
					exchange = new Exchange { Name = code };
					exchangeInfoProvider.Save(exchange);
				}

				board = new ExchangeBoard
				{
					Code = code,
					Exchange = exchange
				};
			}
			else
			{
				board = createBoard(code);

				if (exchangeInfoProvider.GetExchange(board.Exchange.Name) == null)
					exchangeInfoProvider.Save(board.Exchange);
			}

			exchangeInfoProvider.Save(board);

			return board;
		}

		/// <summary>
		/// Is MICEX board.
		/// </summary>
		/// <param name="board">Board to check.</param>
		/// <returns>Check result.</returns>
		public static bool IsMicex(this ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return board.Exchange == Exchange.Moex && board != ExchangeBoard.Forts;
		}

		/// <summary>
		/// Is the UX exchange stock market board.
		/// </summary>
		/// <param name="board">Board to check.</param>
		/// <returns>Check result.</returns>
		public static bool IsUxStock(this ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return board.Exchange == Exchange.Ux && board != ExchangeBoard.Ux;
		}

		/// <summary>
		/// Identifier of <see cref="AllSecurity"/>.
		/// </summary>
		public const string AllSecurityId = "ALL@ALL";

		/// <summary>
		/// "All securities" instance.
		/// </summary>
		public static Security AllSecurity { get; } = new Security
		{
			Id = AllSecurityId,
			Code = MessageAdapter.DefaultAssociatedBoardCode,
			//Class = task.GetDisplayName(),
			Name = LocalizedStrings.Str2835,
			Board = ExchangeBoard.Associated,
		};

		/// <summary>
		/// "News" security instance.
		/// </summary>
		public static readonly Security NewsSecurity = new Security { Id = "NEWS@NEWS" };

		/// <summary>
		/// Find <see cref="AllSecurity"/> instance in the specified provider.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <returns>Found instance.</returns>
		public static Security GetAllSecurity(this ISecurityProvider provider)
		{
			return provider.LookupById(AllSecurityId);
		}

		/// <summary>
		/// Check if the specified security is <see cref="AllSecurity"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns><see langword="true"/>, if the specified security is <see cref="AllSecurity"/>, otherwise, <see langword="false"/>.</returns>
		public static bool IsAllSecurity(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.Id.CompareIgnoreCase(AllSecurityId);
		}

		/// <summary>
		/// Check if the specified identifier is <see cref="AllSecurity"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns><see langword="true"/>, if the specified identifier is <see cref="AllSecurity"/>, otherwise, <see langword="false"/>.</returns>
		public static bool IsAllSecurity(this SecurityId securityId)
		{
			//if (security == null)
			//	throw new ArgumentNullException(nameof(security));

			return securityId.SecurityCode.CompareIgnoreCase(MessageAdapter.DefaultAssociatedBoardCode) && securityId.BoardCode.CompareIgnoreCase(MessageAdapter.DefaultAssociatedBoardCode);
		}

		/// <summary>
		/// To check the correctness of the entered identifier.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>An error message text, or <see langword="null" /> if no error.</returns>
		public static string ValidateId(ref string id)
		{
			// 
			// can be fixed via TraderHelper.SecurityIdToFolderName
			//
			//var invalidChars = Path.GetInvalidFileNameChars().Where(id.Contains).ToArray();
			//if (invalidChars.Any())
			//{
			//	return LocalizedStrings.Str1549Params
			//		.Put(id, invalidChars.Select(c => c.To<string>()).Join(", "));
			//}

			var firstIndex = id.IndexOf('@');

			if (firstIndex == -1)
			{
				id += "@ALL";
				//return LocalizedStrings.Str2926;
			}

			var lastIndex = id.LastIndexOf('@');

			//if (firstIndex != id.LastIndexOf('@'))
			//	return LocalizedStrings.Str1550;

			if (firstIndex != lastIndex)
				return null;

			if (firstIndex == 0)
				return LocalizedStrings.Str2923;
			else if (firstIndex == (id.Length - 1))
				return LocalizedStrings.Str2926;

			return null;
		}

		/// <summary>
		/// Convert <see cref="Level1Fields"/> to <see cref="Type"/> value.
		/// </summary>
		/// <param name="field"><see cref="Level1Fields"/> value.</param>
		/// <returns><see cref="Type"/> value.</returns>
		public static Type ToType(this Level1Fields field)
		{
			switch (field)
			{
				case Level1Fields.AsksCount:
				case Level1Fields.BidsCount:
				case Level1Fields.TradesCount:
				case Level1Fields.Decimals:
					return typeof(int);

				case Level1Fields.LastTradeId:
					return typeof(long);

				case Level1Fields.BestAskTime:
				case Level1Fields.BestBidTime:
				case Level1Fields.LastTradeTime:
				case Level1Fields.BuyBackDate:
					return typeof(DateTimeOffset);

				case Level1Fields.LastTradeUpDown:
				case Level1Fields.IsSystem:
					return typeof(bool);

				case Level1Fields.State:
					return typeof(SecurityStates);

				case Level1Fields.LastTradeOrigin:
					return typeof(Sides);

				default:
					return field.IsObsolete() ? null : typeof(decimal);
			}
		}

		/// <summary>
		/// Convert <see cref="QuoteChangeMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="QuoteChangeMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this QuoteChangeMessage message)
		{
			var bestBid = message.GetBestBid();
			var bestAsk = message.GetBestAsk();

			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.ServerTime,
			};

			if (bestBid != null)
			{
				level1.Add(Level1Fields.BestBidPrice, bestBid.Price);
				level1.Add(Level1Fields.BestBidVolume, bestBid.Volume);
			}

			if (bestAsk != null)
			{
				level1.Add(Level1Fields.BestAskPrice, bestAsk.Price);
				level1.Add(Level1Fields.BestAskVolume, bestAsk.Volume);
			}

			return level1;
		}

		/// <summary>
		/// Convert <see cref="CandleMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="CandleMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this CandleMessage message)
		{
			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.OpenTime,
			}
			.Add(Level1Fields.OpenPrice, message.OpenPrice)
			.Add(Level1Fields.HighPrice, message.HighPrice)
			.Add(Level1Fields.LowPrice, message.LowPrice)
			.Add(Level1Fields.ClosePrice, message.ClosePrice)
			.Add(Level1Fields.Volume, message.TotalVolume)
			.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true);

			return level1;
		}

		/// <summary>
		/// Convert <see cref="ExecutionMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="ExecutionMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this ExecutionMessage message)
		{
			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.ServerTime,
			}
			.TryAdd(Level1Fields.LastTradeId, message.TradeId)
			.TryAdd(Level1Fields.LastTradePrice, message.TradePrice)
			.TryAdd(Level1Fields.LastTradeVolume, message.TradeVolume)
			.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true)
			.TryAdd(Level1Fields.LastTradeOrigin, message.OriginSide);

			return level1;
		}

		/// <summary>
		/// Convert depths to quotes.
		/// </summary>
		/// <param name="messages">Depths.</param>
		/// <returns>Quotes.</returns>
		public static IEnumerable<TimeQuoteChange> ToTimeQuotes(this IEnumerable<QuoteChangeMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			return messages.SelectMany(d => d.Asks.Concat(d.Bids).OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d)));
		}

		/// <summary>
		/// Is specified security id associated with the board.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="board">Board info.</param>
		/// <returns><see langword="true" />, if associated, otherwise, <see langword="false"/>.</returns>
		public static bool IsAssociated(this SecurityId securityId, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return securityId.BoardCode.CompareIgnoreCase(board.Code);
		}

		/// <summary>
		/// Lookup securities, portfolios and orders.
		/// </summary>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="offlineMode">Offline mode handling message.</param>
		public static void LookupAll(this IConnector connector, MessageOfflineModes offlineMode = MessageOfflineModes.Cancel)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			connector.LookupBoards(new ExchangeBoard(), offlineMode: offlineMode);
			connector.LookupSecurities(LookupAllCriteria, offlineMode: offlineMode);
			connector.LookupPortfolios(new Portfolio(), offlineMode: offlineMode);
			connector.LookupOrders(new Order());
		}

		/// <summary>
		/// Truncate the specified order book by max depth value.
		/// </summary>
		/// <param name="depth">Order book.</param>
		/// <param name="maxDepth">The maximum depth of order book.</param>
		/// <returns>Truncated order book.</returns>
		public static MarketDepth Truncate(this MarketDepth depth, int maxDepth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			var result = depth.Clone();
			result.Update(result.Bids.Take(maxDepth), result.Asks.Take(maxDepth), true);
			return result;
		}

		/// <summary>
		/// Get adapter by portfolio.
		/// </summary>
		/// <param name="provider">The message adapter's provider.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>The found adapter.</returns>
		public static IMessageAdapter GetAdapter(this IPortfolioMessageAdapterProvider provider, Portfolio portfolio)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return provider.GetAdapter(portfolio.Name);
		}

		/// <summary>
		/// Get available candles types.
		/// </summary>
		/// <param name="dataTypes">Data types.</param>
		/// <returns>Candles types.</returns>
		public static IEnumerable<DataType> TimeFrameCandles(this IEnumerable<DataType> dataTypes)
		{
			return dataTypes.Where(t => t.MessageType == typeof(TimeFrameCandleMessage));
		}

		/// <summary>
		/// Convert inner securities messages to basket.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="innerSecMessages">Inner securities messages.</param>
		/// <param name="security">Basket security.</param>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <returns>Messages of basket securities.</returns>
		public static IEnumerable<TMessage> ToBasket<TMessage>(this IEnumerable<TMessage> innerSecMessages, Security security, IBasketSecurityProcessorProvider processorProvider)
			where TMessage : Message
		{
			var processor = processorProvider.CreateProcessor(security);

			return innerSecMessages.SelectMany(processor.Process).Cast<TMessage>();
		}

		/// <summary>
		/// Create market data processor for basket securities.
		/// </summary>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <param name="security">Basket security.</param>
		/// <returns>Market data processor for basket securities.</returns>
		public static IBasketSecurityProcessor CreateProcessor(this IBasketSecurityProcessorProvider processorProvider, Security security)
		{
			if (processorProvider == null)
				throw new ArgumentNullException(nameof(processorProvider));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return processorProvider.GetProcessorType(security.BasketCode).CreateInstance<IBasketSecurityProcessor>(security);
		}

		/// <summary>
		/// Is specified security is basket.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsBasket(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return !security.BasketCode.IsEmpty();
		}

		/// <summary>
		/// Is specified security is basket.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsBasket(this SecurityMessage security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return !security.BasketCode.IsEmpty();
		}
		
		/// <summary>
		/// Is specified security is index.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsIndex(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.BasketCode == "WI" || security.BasketCode == "EI";
		}

		/// <summary>
		/// Is specified security is index.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsIndex(this SecurityMessage security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.BasketCode == "WI" || security.BasketCode == "EI";
		}

		/// <summary>
		/// Is specified security is continuous.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsContinuous(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.BasketCode == "CE" || security.BasketCode == "CV";
		}

		/// <summary>
		/// Is specified security is continuous.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public static bool IsContinuous(this SecurityMessage security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.BasketCode == "CE" || security.BasketCode == "CV";
		}

		/// <summary>
		/// Convert <see cref="Security"/> to <see cref="BasketSecurity"/> value.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="processorProvider">Basket security processors provider.</param>
		/// <returns>Instruments basket.</returns>
		public static BasketSecurity ToBasket(this Security security, IBasketSecurityProcessorProvider processorProvider)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (processorProvider == null)
				throw new ArgumentNullException(nameof(processorProvider));

			var type = processorProvider.GetSecurityType(security.BasketCode);
			var basketSec = type.CreateInstance<BasketSecurity>();
			security.CopyTo(basketSec);
			return basketSec;
		}

		/// <summary>
		/// Convert <see cref="Security"/> to <see cref="BasketSecurity"/> value.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Instruments basket.</returns>
		/// <typeparam name="TBasketSecurity">Basket security type.</typeparam>
		public static TBasketSecurity ToBasket<TBasketSecurity>(this Security security)
			where TBasketSecurity : BasketSecurity, new()
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var basketSec = new TBasketSecurity();
			security.CopyTo(basketSec);
			return basketSec;
		}

		/// <summary>
		/// Filter boards by code criteria.
		/// </summary>
		/// <param name="provider">The exchange boards provider.</param>
		/// <param name="like">Criteria.</param>
		/// <returns>Found boards.</returns>
		public static IEnumerable<ExchangeBoard> LookupBoards(this IExchangeInfoProvider provider, string like)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Boards.Filter(like);
		}

		/// <summary>
		/// Filter boards by code criteria.
		/// </summary>
		/// <param name="boards">All boards.</param>
		/// <param name="like">Criteria.</param>
		/// <returns>Found boards.</returns>
		public static IEnumerable<ExchangeBoard> Filter(this IEnumerable<ExchangeBoard> boards, string like)
		{
			if (boards == null)
				throw new ArgumentNullException(nameof(boards));

			if (!like.IsEmpty())
				boards = boards.Where(b => b.Code.ContainsIgnoreCase(like));

			return boards;
		}

		/// <summary>
		/// Filter portfolios by the specified criteria.
		/// </summary>
		/// <param name="portfolios">All portfolios.</param>
		/// <param name="criteria">Criteria.</param>
		/// <returns>Found portfolios.</returns>
		public static IEnumerable<Portfolio> Filter(this IEnumerable<Portfolio> portfolios, PortfolioLookupMessage criteria)
		{
			if (portfolios == null)
				throw new ArgumentNullException(nameof(portfolios));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (!criteria.PortfolioName.IsEmpty())
				portfolios = portfolios.Where(p => p.Name.ContainsIgnoreCase(criteria.PortfolioName));

			if (criteria.Currency != null)
				portfolios = portfolios.Where(p => p.Currency == criteria.Currency);

			if (!criteria.BoardCode.IsEmpty())
				portfolios = portfolios.Where(p => p.Board?.Code.ContainsIgnoreCase(criteria.BoardCode) == true);

			return portfolios;
		}

		/// <summary>
		/// Filter positions the specified criteria.
		/// </summary>
		/// <param name="positions">All positions.</param>
		/// <param name="criteria">Criteria.</param>
		/// <returns>Found positions.</returns>
		public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, PortfolioLookupMessage criteria)
		{
			if (positions == null)
				throw new ArgumentNullException(nameof(positions));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (!criteria.PortfolioName.IsEmpty())
				positions = positions.Where(p => p.Portfolio.Name.ContainsIgnoreCase(criteria.PortfolioName));

			if (criteria.Currency != null)
				positions = positions.Where(p => p.Currency == criteria.Currency);

			if (!criteria.BoardCode.IsEmpty())
				positions = positions.Where(p => p.Security.ToSecurityId().BoardCode.ContainsIgnoreCase(criteria.BoardCode));

			return positions;
		}
	}
}