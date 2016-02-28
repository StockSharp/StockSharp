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
	using System.Data;
	using System.Linq;
	using System.ServiceModel;

	using Ecng.Net;
	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Positions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Wintellect.PowerCollections;

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
	/// The supplier of information on instruments, getting data from the collection.
	/// </summary>
	public class CollectionSecurityProvider : SynchronizedList<Security>, ISecurityProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		public CollectionSecurityProvider()
			: this(Enumerable.Empty<Security>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		/// <param name="securities">The instruments collection.</param>
		public CollectionSecurityProvider(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			AddRange(securities);

			AddedRange += s => _added.SafeInvoke(s);
			RemovedRange += s => _removed.SafeInvoke(s);
		}

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add { _added += value; }
			remove { _added -= value; }
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add { _removed += value; }
			remove { _removed -= value; }
		}

		void IDisposable.Dispose()
		{
		}

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			//var provider = Securities as ISecurityProvider;
			//return provider == null ? Securities.Filter(criteria) : provider.Lookup(criteria);
			return this.Filter(criteria);
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}
	}

	/// <summary>
	/// The auxiliary class for provision of various algorithmic functionalities.
	/// </summary>
	public static class TraderHelper
	{
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

			var dict = new MultiDictionary<Tuple<Sides, decimal>, Order>(false);

			foreach (var order in ownOrders)
			{
				dict.Add(Tuple.Create(order.Direction, order.Price), order);
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

		/// <summary>
		/// To get market price for the instrument by maximal and minimal possible prices.
		/// </summary>
		/// <param name="security">The instrument used for the market price calculation.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="side">Order side.</param>
		/// <returns>The market price. If there is no information on maximal and minimal possible prices, then <see langword="null" /> will be returned.</returns>
		public static decimal? GetMarketPrice(this Security security, IMarketDataProvider provider, Sides side)
		{
			var board = security.CheckExchangeBoard();

			if (board.IsSupportMarketOrders)
				throw new ArgumentException(LocalizedStrings.Str1210Params.Put(board.Code), nameof(security));

			var minPrice = (decimal?)provider.GetSecurityValue(security, Level1Fields.MinPrice);
			var maxPrice = (decimal?)provider.GetSecurityValue(security, Level1Fields.MaxPrice);

			if (side == Sides.Buy && maxPrice != null)
				return maxPrice.Value;
			else if (side == Sides.Sell && minPrice != null)
				return minPrice.Value;
			else
				return null;
				//throw new ArgumentException("У инструмента {0} отсутствует информация о планках.".Put(security), "security");
		}

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
			return pair == null ? null : pair.GetCurrentPrice(side, priceType);
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
					currentPrice = quote == null ? (decimal?)null : quote.Price;
					break;
				}
				case MarketPriceTypes.Following:
				{
					var quote = (side == Sides.Buy ? bestPair.Bid : bestPair.Ask);
					currentPrice = quote == null ? (decimal?)null : quote.Price;
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
					throw new ArgumentOutOfRangeException(nameof(priceType));
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
		/// To get the position on My trade.
		/// </summary>
		/// <param name="trade">My trade, used for position calculation. At buy the trade volume <see cref="Trade.Volume"/> is taken with positive sign, at sell - with negative.</param>
		/// <returns>Position.</returns>
		public static decimal GetPosition(this MyTrade trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			return trade.Order.Direction == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}

		/// <summary>
		/// To get the position on My trade.
		/// </summary>
		/// <param name="message">My trade, used for position calculation. At buy the trade volume <see cref="ExecutionMessage.TradeVolume"/> is taken with positive sign, at sell - with negative.</param>
		/// <returns>Position.</returns>
		public static decimal GetPosition(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return (message.Side == Sides.Buy ? message.TradeVolume : -message.TradeVolume) ?? 0;
		}

		/// <summary>
		/// To get the position by the order.
		/// </summary>
		/// <param name="order">The order, used for the position calculation. At buy the position is taken with positive sign, at sell - with negative.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Position.</returns>
		public static decimal GetPosition(this Order order, IConnector connector)
		{
			var volume = order.GetMatchedVolume(connector);

			return order.Direction == Sides.Buy ? volume : -volume;
		}

		/// <summary>
		/// To get the position by the portfolio.
		/// </summary>
		/// <param name="portfolio">The portfolio, for which the position needs to be got.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>The position by the portfolio.</returns>
		public static decimal GetPosition(this Portfolio portfolio, IConnector connector)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			return connector.Positions.Filter(portfolio).Sum(p => p.CurrentValue);
		}

		/// <summary>
		/// To get the position by My trades.
		/// </summary>
		/// <param name="trades">My trades, used for the position calculation using the <see cref="GetPosition(StockSharp.BusinessEntities.MyTrade)"/> method.</param>
		/// <returns>Position.</returns>
		public static decimal GetPosition(this IEnumerable<MyTrade> trades)
		{
			return trades.Sum(t => t.GetPosition());
		}

		/// <summary>
		/// To get the trade volume, collatable with the position size.
		/// </summary>
		/// <param name="position">The position by the instrument.</param>
		/// <returns>Order volume.</returns>
		public static decimal GetOrderVolume(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			return (position.CurrentValue / position.Security.VolumeStep ?? 1m).Abs();
		}

		/// <summary>
		/// To group orders by instrument and portfolio.
		/// </summary>
		/// <param name="orders">Initial orders.</param>
		/// <returns>Grouped orders.</returns>
		/// <remarks>
		/// Recommended to use to reduce trade costs.
		/// </remarks>
		public static IEnumerable<Order> Join(this IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			return orders.GroupBy(o => Tuple.Create(o.Security, o.Portfolio)).Select(g =>
			{
				Order firstOrder = null;

				foreach (var order in g)
				{
					if (firstOrder == null)
					{
						firstOrder = order;
					}
					else
					{
						var sameDir = firstOrder.Direction == order.Direction;

						firstOrder.Volume += (sameDir ? 1 : -1) * order.Volume;

						if (firstOrder.Volume < 0)
						{
							firstOrder.Direction = firstOrder.Direction.Invert();
							firstOrder.Volume = firstOrder.Volume.Abs();
						}

						firstOrder.Price = sameDir ? firstOrder.Price.GetMiddle(order.Price) : order.Price;
					}
				}

				if (firstOrder == null)
					throw new InvalidOperationException(LocalizedStrings.Str1211);

				if (firstOrder.Volume == 0)
					return null;

				firstOrder.ShrinkPrice();
				return firstOrder;
			})
			.Where(o => o != null);
		}

		/// <summary>
		/// To calculate profit-loss based on trades.
		/// </summary>
		/// <param name="trades">Trades, for which the profit-loss shall be calculated.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal GetPnL(this IEnumerable<MyTrade> trades)
		{
			return trades.Select(t => t.ToMessage()).GetPnL();
		}

		/// <summary>
		/// To calculate profit-loss based on trades.
		/// </summary>
		/// <param name="trades">Trades, for which the profit-loss shall be calculated.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal GetPnL(this IEnumerable<ExecutionMessage> trades)
		{
			return trades.GroupBy(t => t.SecurityId).Sum(g =>
			{
				var queue = new PnLQueue(g.Key);

				g.OrderBy(t => t.ServerTime).ForEach(t => queue.Process(t));

				return queue.RealizedPnL + queue.UnrealizedPnL;
			});
		}

		/// <summary>
		/// To calculate profit-loss for trade.
		/// </summary>
		/// <param name="trade">The trade for which the profit-loss shall be calculated.</param>
		/// <param name="currentPrice">The current price of the instrument.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal GetPnL(this MyTrade trade, decimal currentPrice)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			return trade.ToMessage().GetPnL(currentPrice);
		}

		/// <summary>
		/// To calculate profit-loss for trade.
		/// </summary>
		/// <param name="trade">The trade for which the profit-loss shall be calculated.</param>
		/// <param name="currentPrice">The current price of the instrument.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal GetPnL(this ExecutionMessage trade, decimal currentPrice)
		{
			return GetPnL(trade.GetTradePrice(), trade.SafeGetVolume(), trade.Side, currentPrice);
		}

		internal static decimal GetPnL(decimal price, decimal volume, Sides side, decimal marketPrice)
		{
			return (price - marketPrice) * volume * (side == Sides.Sell ? 1 : -1);
		}

		/// <summary>
		/// To calculate profit-loss based on the portfolio.
		/// </summary>
		/// <param name="portfolio">The portfolio, for which the profit-loss shall be calculated.</param>
		/// <returns>Profit-loss.</returns>
		public static decimal GetPnL(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return portfolio.CurrentValue - portfolio.BeginValue;
		}

		/// <summary>
		/// To calculate the position cost.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="currentPrice">The current price of the instrument.</param>
		/// <returns>Position price.</returns>
		public static decimal GetPrice(this Position position, decimal currentPrice)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			var security = position.Security;

			return currentPrice * position.CurrentValue * security.StepPrice / security.PriceStep ?? 1;
		}

		///// <summary>
		///// Получить текущее время с учетом часового пояса торговой площадки инструмента.
		///// </summary>
		///// <param name="connector">Подключение к торговой системе.</param>
		///// <param name="security">Инструмент.</param>
		///// <returns>Текущее время.</returns>
		//public static DateTime GetMarketTime(this IConnector connector, Security security)
		//{
		//	if (connector == null)
		//		throw new ArgumentNullException("connector");

		//	if (security == null)
		//		throw new ArgumentNullException("security");

		//	var localTime = connector.CurrentTime;

		//	return security.ToExchangeTime(localTime);
		//}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this ExchangeBoard board, DateTimeOffset time)
		{
			return board.ToMessage().IsTradeTime(time);
		}

		/// <summary>
		/// To check, whether the time is traded (has the session started, ended, is there a clearing).
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="time">The passed time to be checked.</param>
		/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
		public static bool IsTradeTime(this BoardMessage board, DateTimeOffset time)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			var exchangeTime = time.ToLocalTime(board.TimeZone);
			var workingTime = board.WorkingTime;

			var isWorkingDay = board.IsTradeDate(time);

			if (!isWorkingDay)
				return false;

			var period = workingTime.GetPeriod(exchangeTime);

			var tod = exchangeTime.TimeOfDay;
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

			var period = workingTime.GetPeriod(exchangeTime);

			if ((period == null || period.Times.Length == 0) && workingTime.SpecialWorkingDays.Length == 0 && workingTime.SpecialHolidays.Length == 0)
				return true;

			bool isWorkingDay;

			if (checkHolidays && (exchangeTime.DayOfWeek == DayOfWeek.Saturday || exchangeTime.DayOfWeek == DayOfWeek.Sunday))
				isWorkingDay = workingTime.SpecialWorkingDays.Contains(exchangeTime.Date);
			else
				isWorkingDay = !workingTime.SpecialHolidays.Contains(exchangeTime.Date);

			return isWorkingDay;
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

		private static readonly ChannelFactory<IDailyInfoSoap> _dailyInfoFactory = new ChannelFactory<IDailyInfoSoap>(new BasicHttpBinding(), new EndpointAddress("http://www.cbr.ru/dailyinfowebserv/dailyinfo.asmx"));
		private static readonly Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>> _rateInfo = new Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>>();

		/// <summary>
		/// To convert one currency to another.
		/// </summary>
		/// <param name="currencyFrom">The currency to be converted.</param>
		/// <param name="currencyTypeTo">The code of the target currency.</param>
		/// <returns>Converted currency.</returns>
		public static Currency Convert(this Currency currencyFrom, CurrencyTypes currencyTypeTo)
		{
			if (currencyFrom == null)
				throw new ArgumentNullException(nameof(currencyFrom));

			return new Currency { Type = currencyTypeTo, Value = currencyFrom.Value * currencyFrom.Type.Convert(currencyTypeTo) };
		}

		/// <summary>
		/// To get the conversion rate for converting one currency to another.
		/// </summary>
		/// <param name="from">The code of currency to be converted.</param>
		/// <param name="to">The code of the target currency.</param>
		/// <returns>The rate.</returns>
		public static decimal Convert(this CurrencyTypes from, CurrencyTypes to)
		{
			return from.Convert(to, DateTime.Today);
		}

		/// <summary>
		/// To get the conversion rate for the specified date.
		/// </summary>
		/// <param name="from">The code of currency to be converted.</param>
		/// <param name="to">The code of the target currency.</param>
		/// <param name="date">The rate date.</param>
		/// <returns>The rate.</returns>
		public static decimal Convert(this CurrencyTypes from, CurrencyTypes to, DateTime date)
		{
			if (from == to)
				return 1;

			var info = _rateInfo.SafeAdd(date, key =>
			{
				var i = _dailyInfoFactory.Invoke(c => c.GetCursOnDate(key));
				return i.Tables[0].Rows.Cast<DataRow>().ToDictionary(r => r[4].To<CurrencyTypes>(), r => r[2].To<decimal>());
			});

			if (from != CurrencyTypes.RUB && !info.ContainsKey(from))
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(from), nameof(@from));

			if (to != CurrencyTypes.RUB && !info.ContainsKey(to))
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(to), nameof(to));

			if (from == CurrencyTypes.RUB)
				return 1 / info[to];
			else if (to == CurrencyTypes.RUB)
				return info[from];
			else
				return info[from] / info[to];
		}

		/// <summary>
		/// To create from regular order book a sparse on, with minimal price step of <see cref="Security.PriceStep"/>. <remarks>
		///             В разреженном стакане показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		///             </remarks>.
		/// </summary>
		/// <param name="depth">The regular order book.</param>
		/// <returns>The sparse order book.</returns>
		public static MarketDepth Sparse(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return depth.Sparse(depth.Security.PriceStep ?? 1m);
		}

		/// <summary>
		/// To create from regular order book a sparse one. <remarks>
		///             В разреженном стакане показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		///             </remarks>.
		/// </summary>
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
			var spreadQuotes = pair == null ? Enumerable.Empty<Quote>() : pair.Sparse(priceStep).ToArray();

			return new MarketDepth(depth.Security).Update(
				bids.Concat(spreadQuotes.Where(q => q.OrderDirection == Sides.Buy)),
				asks.Concat(spreadQuotes.Where(q => q.OrderDirection == Sides.Sell)),
				false, depth.LastChangeTime);
		}

		/// <summary>
		/// To create form pair of quotes a sparse collection of quotes, which will be included into the range between the pair. <remarks>
		///             В разреженной коллекции показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		///             </remarks>.
		/// </summary>
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
		/// To create the sparse collection of quotes from regular quotes. <remarks>
		///             В разреженной коллекции показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		///             </remarks>.
		/// </summary>
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

				decimal vol;
				if (!changedVolume.TryGetValue(price, out vol))
					vol = quote.Volume;

				vol -= trade.SafeGetVolume();
				changedVolume[quote.Price] = vol;
			}

			var bids = new Quote[depth.Bids.Length];
			Action a1 = () =>
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
						decimal vol;
						if (changedVolume.TryGetValue(price, out vol))
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
			};

			a1();

			var asks = new Quote[depth.Asks.Length];
			Action a2 = () =>
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
						decimal vol;
						if (changedVolume.TryGetValue(price, out vol))
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
			};

			a2();

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
			//	throw new ArgumentOutOfRangeException("priceRange", priceRange, "Размер группировки меньше допустимого.");

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
				throw new ArgumentNullException(nameof(@from));

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
			var mapTo = to.ToDictionary(q => q.Price);
			var mapFrom = from.ToDictionary(q => q.Price);

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
					empty.Volume = 0;				// была а теперь нет
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
				throw new ArgumentNullException(nameof(@from));

			if (delta == null)
				throw new ArgumentNullException(nameof(delta));

			if (!from.IsSorted)
				throw new ArgumentException("from");

			if (!delta.IsSorted)
				throw new ArgumentException("delta");

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
		public static decimal GetMatchedVolume(this Order order, IConnector connector, bool byOrder = true)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (order.Type == OrderTypes.Conditional)
			{
				//throw new ArgumentException("Стоп-заявки не могут иметь реализованный объем.", "order");

				order = order.DerivedOrder;

				if (order == null)
					return 0;
			}

			return order.Volume - (byOrder ? order.Balance : order.GetTrades(connector).Sum(o => o.Trade.Volume));
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
					var execMsg = msg as ExecutionMessage;

					if (execMsg == null)
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
				var regMsg = order.CreateRegisterMessage(order.Security.ToSecurityId());
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

			return position.CurrentValue.GetDirection();
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
		public static void CancelOrders(this IConnector connector, IEnumerable<Order> orders, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			foreach (var order in orders.Where(o => o.State != OrderStates.Done).ToArray())
			{
				if (isStopOrder == null || (order.Type == OrderTypes.Conditional) == isStopOrder)
				{
					if (portfolio == null || (order.Portfolio == portfolio))
					{
						if (direction == null || order.Direction == direction)
						{
							if (board == null || order.Security.Board == board)
							{
								if (security == null || order.Security == security)
								{
									connector.CancelOrder(order);
								}
							}
						}
					}
				}
			}
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
			return basket?.InnerSecurities.SelectMany(s => Filter(orders, s)) ?? orders.Where(o => o.Security == security);
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
		/// To ilter orders for the given condition.
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
			return basket?.InnerSecurities.SelectMany(s => Filter(trades, s)) ?? trades.Where(t => t.Security == security);
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
			return basket?.InnerSecurities.SelectMany(s => Filter(positions, s)) ?? positions.Where(p => p.Security == security);
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
			return basket == null ? myTrades.Where(t => t.Order.Security == security) : basket.InnerSecurities.SelectMany(s => Filter(myTrades, s));
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
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> FilterSecurities(this Connector connector, SecurityLookupMessage criteria)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var security = connector.GetSecurityCriteria(criteria);

			return connector.Securities.Filter(security);
		}

		/// <summary>
		/// To create the search criteria <see cref="Security"/> from <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <returns>Search criterion.</returns>
		public static Security GetSecurityCriteria(this Connector connector, SecurityLookupMessage criteria)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var stocksharpId = criteria.SecurityId.SecurityCode.IsEmpty() || criteria.SecurityId.BoardCode.IsEmpty()
				                   ? string.Empty
				                   : connector.SecurityIdGenerator.GenerateId(criteria.SecurityId.SecurityCode, criteria.SecurityId.BoardCode);

			var security = new Security
			{
				Id = stocksharpId,
				Name = criteria.Name,
				Code = criteria.SecurityId.SecurityCode,
				Type = criteria.SecurityType,
				ExpiryDate = criteria.ExpiryDate,
				ExternalId = criteria.SecurityId.ToExternalId(),
				Board = criteria.SecurityId.BoardCode.IsEmpty() ? null : ExchangeBoard.GetOrCreateBoard(criteria.SecurityId.BoardCode),
				ShortName = criteria.ShortName,
				Decimals = criteria.Decimals,
				PriceStep = criteria.PriceStep,
				VolumeStep = criteria.VolumeStep,
				Multiplier = criteria.Multiplier,
				OptionType = criteria.OptionType,
				Strike = criteria.Strike,
				BinaryOptionType = criteria.BinaryOptionType,
				Currency = criteria.Currency,
				SettlementDate = criteria.SettlementDate,
				UnderlyingSecurityId = (criteria.UnderlyingSecurityCode.IsEmpty() || criteria.SecurityId.BoardCode.IsEmpty())
					? null
					: connector.SecurityIdGenerator.GenerateId(criteria.UnderlyingSecurityCode, criteria.SecurityId.BoardCode),
			};

			return security;
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
		/// To get the T+N date.
		/// </summary>
		/// <param name="board">Board info.</param>
		/// <param name="date">The start T date.</param>
		/// <param name="n">The N size.</param>
		/// <returns>The end T+N date.</returns>
		public static DateTimeOffset GetTPlusNDate(this ExchangeBoard board, DateTimeOffset date, int n)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			date = date.Date.ApplyTimeZone(date.Offset);

			while (n > 0)
			{
				if (board.IsTradeDate(date))
					n--;

				date = date.AddDays(1);
			}

			return date;
		}

		///// <summary>
		///// Перевести локальное время в биржевое.
		///// </summary>
		///// <param name="exchange">Информация о бирже.</param>
		///// <param name="time">Локальное время.</param>
		///// <returns>Время с биржевым сдвигом.</returns>
		//public static DateTimeOffset ToExchangeTime(this Exchange exchange, DateTime time)
		//{
		//	if (exchange == null)
		//		throw new ArgumentNullException("exchange");

		//	return time.ToLocalTime(exchange.TimeZoneInfo).ApplyTimeZone(exchange.TimeZoneInfo);
		//}

		///// <summary>
		///// Перевести локальное время в биржевое.
		///// </summary>
		///// <param name="exchange">Информация о бирже.</param>
		///// <param name="time">Локальное время.</param>
		///// <param name="sourceZone">Времемнная зона, в которой записано значение <paramref name="time"/>.</param>
		///// <returns>Время с биржевым сдвигом.</returns>
		//public static DateTime ToExchangeTime(this Exchange exchange, DateTime time, TimeZoneInfo sourceZone)
		//{
		//	if (exchange == null)
		//		throw new ArgumentNullException("exchange");

		//	return time.To(sourceZone, exchange.TimeZoneInfo);
		//}

		///// <summary>
		///// Перевести локальное время в биржевое.
		///// </summary>
		///// <param name="security">Информация о инструменте.</param>
		///// <param name="localTime">Локальное время.</param>
		///// <returns>Время с биржевым сдвигом.</returns>
		//public static DateTimeOffset ToExchangeTime(this Security security, DateTimeOffset localTime)
		//{
		//	if (security == null) 
		//		throw new ArgumentNullException("security");

		//	if (security.Board == null)
		//		throw new ArgumentException(LocalizedStrings.Str903Params.Put(security.Id), "security");

		//	if (security.Board.Exchange == null)
		//		throw new ArgumentException(LocalizedStrings.Str1216Params.Put(security.Id), "security");

		//	return security.Board.Exchange.ToExchangeTime(localTime);
		//}

		///// <summary>
		///// Перевести биржевое время в локальное.
		///// </summary>
		///// <param name="exchange">Информация о бирже, из которой будет использоваться <see cref="Exchange.TimeZoneInfo"/>.</param>
		///// <param name="exchangeTime">Биржевое время.</param>
		///// <returns>Локальное время.</returns>
		//public static DateTime ToLocalTime(this Exchange exchange, DateTimeOffset exchangeTime)
		//{
		//	if (exchange == null)
		//		throw new ArgumentNullException("exchange");

		//	return exchangeTime.ToLocalTime(exchange.TimeZoneInfo);

		//	//if (exchangeTime.Kind == DateTimeKind.Local)
		//	//	return exchangeTime;

		//	//if (exchange.TimeZoneInfo.Id == TimeZoneInfo.Local.Id)
		//	//	return exchangeTime;

		//	//// http://stackoverflow.com/questions/11872980/converting-datetime-now-to-a-different-time-zone
		//	//exchangeTime = exchangeTime.To(destination: exchange.TimeZoneInfo);

		//	//return exchangeTime.To(exchange.TimeZoneInfo, TimeZoneInfo.Local);
		//}

		///// <summary>
		///// Перевести биржевое время в UTC.
		///// </summary>
		///// <param name="exchange">Информация о бирже, из которой будет использоваться <see cref="Exchange.TimeZoneInfo"/>.</param>
		///// <param name="exchangeTime">Биржевое время.</param>
		///// <returns>Биржевое время в UTC.</returns>
		//public static DateTime ToUtc(this Exchange exchange, DateTime exchangeTime)
		//{
		//	if (exchange == null)
		//		throw new ArgumentNullException("exchange");

		//	return TimeZoneInfo.ConvertTimeToUtc(exchangeTime, exchange.TimeZoneInfo);
		//}

		/// <summary>
		/// To calculate delay based on difference between the server and local time.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="serverTime">Server time.</param>
		/// <param name="localTime">Local time.</param>
		/// <returns>Latency.</returns>
		public static TimeSpan GetLatency(this Security security, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			return localTime - serverTime;
		}

		/// <summary>
		/// To calculate delay based on difference between the server and local time.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="serverTime">Server time.</param>
		/// <param name="localTime">Local time.</param>
		/// <returns>Latency.</returns>
		public static TimeSpan GetLatency(this SecurityId securityId, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			var board = ExchangeBoard.GetBoard(securityId.BoardCode);

			if (board == null)
				throw new ArgumentException(LocalizedStrings.Str1217Params.Put(securityId.BoardCode), nameof(securityId));

			return localTime - serverTime;
		}

		/// <summary>
		/// To get the size of clear funds in the portfolio.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="useLeverage">Whether to use shoulder size for calculation.</param>
		/// <returns>The size of clear funds.</returns>
		public static decimal GetFreeMoney(this Portfolio portfolio, bool useLeverage = false)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			var freeMoney = portfolio.Board == ExchangeBoard.Forts
				? portfolio.BeginValue - portfolio.CurrentValue + portfolio.VariationMargin
				: portfolio.CurrentValue;

			return useLeverage ? freeMoney * portfolio.Leverage : freeMoney;
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
				throw new ArgumentOutOfRangeException(nameof(@from));

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
		/// To get real expirating instruments for base part of the code.
		/// </summary>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		/// <param name="getSecurity">The function to get instrument by the code.</param>
		/// <param name="throwIfNotExists">To generate exception, if some of instruments are not available.</param>
		/// <returns>Expirating instruments.</returns>
		public static IEnumerable<Security> GetFortsJumps(this string baseCode, DateTime from, DateTime to, Func<string, Security> getSecurity, bool throwIfNotExists = true)
		{
			if (baseCode.IsEmpty())
				throw new ArgumentNullException(nameof(baseCode));

			if (from > to)
				throw new ArgumentOutOfRangeException(nameof(@from));

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
							throw new InvalidOperationException(LocalizedStrings.Str1218Params.Put(code));

						continue;
					}
					
					yield return security;
				}
			}
		}

		/// <summary>
		/// To get real expirating instruments for the continuous instrument.
		/// </summary>
		/// <param name="continuousSecurity">Continuous security.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		/// <param name="throwIfNotExists">To generate exception, if some of instruments for passed <paramref name="continuousSecurity" /> are not available.</param>
		/// <returns>Expirating instruments.</returns>
		public static IEnumerable<Security> GetFortsJumps(this ContinuousSecurity continuousSecurity, ISecurityProvider provider, string baseCode, DateTime from, DateTime to, bool throwIfNotExists = true)
		{
			if (continuousSecurity == null)
				throw new ArgumentNullException(nameof(continuousSecurity));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return baseCode.GetFortsJumps(from, to, code => provider.LookupByCode(code).FirstOrDefault(), throwIfNotExists);
		}

		/// <summary>
		/// To fill transitions <see cref="ContinuousSecurity.ExpirationJumps"/>.
		/// </summary>
		/// <param name="continuousSecurity">Continuous security.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="baseCode">The base part of the instrument code.</param>
		/// <param name="from">The start of the expiration range.</param>
		/// <param name="to">The end of the expiration range.</param>
		public static void FillFortsJumps(this ContinuousSecurity continuousSecurity, ISecurityProvider provider, string baseCode, DateTime from, DateTime to)
		{
			var securities = continuousSecurity.GetFortsJumps(provider, baseCode, from, to);

			foreach (var security in securities)
			{
				if (security.ExpiryDate == null)
					throw new InvalidOperationException(LocalizedStrings.Str698Params.Put(security.Id));

				continuousSecurity.ExpirationJumps.Add(security, (DateTimeOffset)security.ExpiryDate);
			}
		}

		private sealed class CashPosition : Position, IDisposable
		{
			private readonly Portfolio _portfolio;
			private readonly IConnector _connector;

			public CashPosition(Portfolio portfolio, IConnector connector)
			{
				if (portfolio == null)
					throw new ArgumentNullException(nameof(portfolio));

				if (connector == null)
					throw new ArgumentNullException(nameof(connector));

				_portfolio = portfolio;
				_connector = connector;

				Portfolio = _portfolio;
				Security = new Security
				{
					Id = _portfolio.Name,
					Name = _portfolio.Name,
				};

				UpdatePosition();

				_connector.PortfoliosChanged += TraderOnPortfoliosChanged;
			}

			private void UpdatePosition()
			{
				BeginValue = _portfolio.BeginValue;
				CurrentValue = _portfolio.CurrentValue;
				BlockedValue = _portfolio.Commission;
			}

			private void TraderOnPortfoliosChanged(IEnumerable<Portfolio> portfolios)
			{
				if (portfolios.Contains(_portfolio))
					UpdatePosition();
			}

			void IDisposable.Dispose()
			{
				_connector.PortfoliosChanged -= TraderOnPortfoliosChanged;
			}
		}

		/// <summary>
		/// To convert portfolio into the monetary position.
		/// </summary>
		/// <param name="portfolio">Portfolio with trading account.</param>
		/// <param name="connector">The connection of interaction with trading system.</param>
		/// <returns>Money position.</returns>
		public static Position ToCashPosition(this Portfolio portfolio, IConnector connector)
		{
			return new CashPosition(portfolio, connector);
		}

		private sealed class NativePositionManager : IPositionManager
		{
			private readonly Position _position;

			public NativePositionManager(Position position)
			{
				if (position == null)
					throw new ArgumentNullException(nameof(position));

				_position = position;
			}

			/// <summary>
			/// The position aggregate value.
			/// </summary>
			decimal IPositionManager.Position
			{
				get { return _position.CurrentValue; }
				set { throw new NotSupportedException(); }
			}

			event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> IPositionManager.NewPosition
			{
				add { }
				remove { }
			}

			event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> IPositionManager.PositionChanged
			{
				add { }
				remove { }
			}

			IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> IPositionManager.Positions
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
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

		private sealed class LookupSecurityUpdate : Disposable
		{
			private readonly IConnector _connector;
			private TimeSpan _timeOut;
			private readonly SyncObject _syncRoot = new SyncObject();

			private readonly SynchronizedList<Security> _securities;

			public LookupSecurityUpdate(IConnector connector, Security criteria, TimeSpan timeOut)
			{
				if (connector == null)
					throw new ArgumentNullException(nameof(connector));

				if (criteria == null)
					throw new ArgumentNullException(nameof(criteria));
				
				_securities = new SynchronizedList<Security>();

				_connector = connector;
				_timeOut = timeOut;

				_connector.LookupSecuritiesResult += OnLookupSecuritiesResult;
				_connector.LookupSecurities(criteria);
			}

			public IEnumerable<Security> Wait()
			{
				while (true)
				{
					if (!_syncRoot.Wait(_timeOut))
						break;
				}

				return _securities;
			}

			private void OnLookupSecuritiesResult(IEnumerable<Security> securities)
			{
				_securities.AddRange(securities);

				_timeOut = securities.Any()
					           ? TimeSpan.FromSeconds(10)
					           : TimeSpan.Zero;

				_syncRoot.Pulse();
			}

			protected override void DisposeManaged()
			{
				_connector.LookupSecuritiesResult -= OnLookupSecuritiesResult;
			}
		}

		/// <summary>
		/// To perform blocking search of instruments, corresponding to the criteria filter.
		/// </summary>
		/// <param name="connector">The connection of interaction with trading system.</param>
		/// <param name="criteria">Instruments search criteria.</param>
		/// <returns>Found instruments.</returns>
		public static IEnumerable<Security> SyncLookupSecurities(this IConnector connector, Security criteria)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			using (var lsu = new LookupSecurityUpdate(connector, criteria, TimeSpan.FromSeconds(180)))
			{
				return lsu.Wait();
			}
		}

		/// <summary>
		/// Apply changes to the portfolio object.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="message">Portfolio change message.</param>
		public static void ApplyChanges(this Portfolio portfolio, PortfolioChangeMessage message)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.BoardCode.IsEmpty())
				portfolio.Board = ExchangeBoard.GetOrCreateBoard(message.BoardCode);

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
					case PositionChangeTypes.ExtensionInfo:
						var pair = change.Value.To<KeyValuePair<object, object>>();
						position.ExtensionInfo[pair.Key] = pair.Value;
						break;
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
					case PositionChangeTypes.DepoName:
						position.ExtensionInfo[change.Key] = change.Value;
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
		/// <param name="localTime">Local time label when a message was received/created.</param>
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
		public static void ApplyChanges(this Security security, SecurityMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.SecurityId.SecurityCode.IsEmpty())
				security.Code = message.SecurityId.SecurityCode;

			if (message.Currency != null)
				security.Currency = message.Currency;

			security.Board = ExchangeBoard.GetOrCreateBoard(message.SecurityId.BoardCode);

			if (message.ExpiryDate != null)
				security.ExpiryDate = message.ExpiryDate;

			if (message.VolumeStep != null)
				security.VolumeStep = message.VolumeStep.Value;

			if (message.Multiplier != null)
				security.Multiplier = message.Multiplier.Value;

			if (message.PriceStep != null)
			{
				security.PriceStep = message.PriceStep.Value;

				if (message.Decimals == null && security.Decimals == null)
					security.Decimals = message.PriceStep.Value.GetCachedDecimals();
			}

			if (message.Decimals != null)
			{
				security.Decimals = message.Decimals.Value;

				if (message.PriceStep == null)
					security.PriceStep = message.Decimals.Value.GetPriceStep();
			}

			if (!message.Name.IsEmpty())
				security.Name = message.Name;

			if (!message.Class.IsEmpty())
				security.Class = message.Class;

			if (message.OptionType != null)
				security.OptionType = message.OptionType;

			if (message.Strike != null)
				security.Strike = message.Strike.Value;

			if (!message.BinaryOptionType.IsEmpty())
				security.BinaryOptionType = message.BinaryOptionType;

			if (message.SettlementDate != null)
				security.SettlementDate = message.SettlementDate;

			if (!message.ShortName.IsEmpty())
				security.ShortName = message.ShortName;

			if (message.SecurityType != null)
				security.Type = message.SecurityType.Value;

			if (!message.UnderlyingSecurityCode.IsEmpty())
				security.UnderlyingSecurityId = message.UnderlyingSecurityCode + "@" + message.SecurityId.BoardCode;

			if (message.SecurityId.HasExternalId())
				security.ExternalId = message.SecurityId.ToExternalId();

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
		/// To add a change to the collection, if value is other than 0.
		/// </summary>
		/// <typeparam name="TMessage">Change message type.</typeparam>
		/// <typeparam name="TChange">Change type.</typeparam>
		/// <param name="message">Change message.</param>
		/// <param name="type">Change type.</param>
		/// <param name="value">Change value.</param>
		/// <returns>Change message.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal value)
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
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null || value == 0)
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
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int value)
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
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null || value == 0)
				return null;

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
		/// To �onvert the currency name in the MICEX format into <see cref="CurrencyTypes"/>.
		/// </summary>
		/// <param name="name">The currency name in the MICEX format.</param>
		/// <returns>Currency type. If the value is empty, <see langword="null" /> will be returned.</returns>
		public static CurrencyTypes? FromMicexCurrencyName(this string name)
		{
			if (name.IsEmpty())
				return null;

			switch (name)
			{
				case "SUR":
				case "RUR":
					return CurrencyTypes.RUB;
				default:
					return name.To<CurrencyTypes>();
			}
		}

		/// <summary>
		/// Get period for schedule.
		/// </summary>
		/// <param name="time">Trading schedule.</param>
		/// <param name="date">The date in time for search of appropriate period.</param>
		/// <returns>The schedule period. If no period is appropriate, <see langword="null" /> is returned.</returns>
		public static WorkingTimePeriod GetPeriod(this WorkingTime time, DateTime date)
		{
			if (time == null)
				throw new ArgumentNullException(nameof(time));

			return time.Periods.FirstOrDefault(p => p.Till >= date);
		}

		/// <summary>
		/// To get the instrument description by the class.
		/// </summary>
		/// <param name="securityClassInfo">Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns>The instrument description. If the class is not found, than <see langword="null" /> value is returned as instrument type.</returns>
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

		private static readonly CachedSynchronizedDictionary<string, string> _securitySeparators = new CachedSynchronizedDictionary<string, string>
		{
			{ "/", SecurityPairSeparator },
			{ "*", SecurityStarSeparator },
			{ ":", SecurityColonSeparator }
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

			if (_reservedDos.Any(d => folderName.StartsWith(d, StringComparison.InvariantCultureIgnoreCase)))
				folderName = "_" + folderName;

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

			if (id[0] == '_' && _reservedDos.Any(d => id.StartsWith("_" + d, StringComparison.InvariantCultureIgnoreCase)))
				id = id.Substring(1);

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
			return arg?.ToString().Replace(":", "-") ?? string.Empty;
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
		/// To get the instrument by the instrument code.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="code">Security code.</param>
		/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
		public static IEnumerable<Security> LookupByCode(this ISecurityProvider provider, string code)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			return provider.Lookup(new Security { Code = code });
		}

		/// <summary>
		/// Lookup all securities predefined criteria.
		/// </summary>
		public static readonly Security LookupAllCriteria = new Security { Code = "*" };

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
				criteria.Code == "*" &&
				criteria.Type == null;
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

			var fields = provider.GetLevel1Fields(security);

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
		//		throw new ArgumentNullException("adapter");

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
		/// To convert <see cref="Double"/> into <see cref="Decimal"/>. If the initial value is <see cref="double.NaN"/> or <see cref="double.IsInfinity"/>, <see langword="null" /> is returned.
		/// </summary>
		/// <param name="value"><see cref="Double"/> value.</param>
		/// <returns><see cref="Decimal"/> value.</returns>
		public static decimal? ToDecimal(this double value)
		{
			return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
		}

		/// <summary>
		/// To convert <see cref="Single"/> into <see cref="Decimal"/>. If the initial value is <see cref="float.NaN"/> or <see cref="float.IsInfinity"/>, <see langword="null" /> is returned.
		/// </summary>
		/// <param name="value"><see cref="Single"/> value.</param>
		/// <returns><see cref="Decimal"/> value.</returns>
		public static decimal? ToDecimal(this float value)
		{
			return value.IsInfinity() || value.IsNaN() || value < _minValue || value > _maxValue ? (decimal?)null : (decimal)value;
		}

		/// <summary>
		/// To get the type for the instrument in the ISO 10962 standard.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Type in ISO 10962 standard.</returns>
		public static string GetIso10962(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			// https://en.wikipedia.org/wiki/ISO_10962

			switch (security.Type)
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
					throw new ArgumentOutOfRangeException(nameof(security));
			}
		}

		/// <summary>
		/// To convert the type in the ISO 10962 standard into <see cref="SecurityTypes"/>.
		/// </summary>
		/// <param name="type">Type in ISO 10962 standard.</param>
		/// <returns>Security type.</returns>
		public static SecurityTypes? FromIso10962(string type)
		{
			if (type.IsEmpty())
				throw new ArgumentNullException(nameof(type));

			if (type.Length != 6)
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str2117);

			switch (type[0])
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
					switch (type[2])
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
					switch (type[1])
					{
						case 'R':
						{
							switch (type[2])
							{
								case 'I':
									return SecurityTypes.Index;

								case 'C':
									return SecurityTypes.Currency;

								case 'T':
									return SecurityTypes.Commodity;
							}

							break;
						}

						case 'M':
						{
							switch (type[2])
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
					if (level1Enumerator == null)
						throw new ArgumentNullException(nameof(level1Enumerator));

					_level1Enumerator = level1Enumerator;
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
					Reset();
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
					if (level1Enumerator == null)
						throw new ArgumentNullException(nameof(level1Enumerator));

					_level1Enumerator = level1Enumerator;
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
					Reset();
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

		/// <summary>
		/// To check the specified date is GTC.
		/// </summary>
		/// <param name="date">The specified date.</param>
		/// <returns><see langword="true"/> if the specified date is GTC, otherwise, <see langword="false"/>.</returns>
		public static bool IsGtc(this DateTimeOffset date)
		{
			return date == DateTimeOffset.MaxValue;
		}
	}
}