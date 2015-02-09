namespace StockSharp.Algo
{
	using System;
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
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Wintellect.PowerCollections;

	/// <summary>
	/// Правила округления цены.
	/// </summary>
	public enum ShrinkRules
	{
		/// <summary>
		/// Автоматически определять, к меньшему или большему значению округлять.
		/// </summary>
		Auto,

		/// <summary>
		/// Округлять к меньшему значению.
		/// </summary>
		Less,

		/// <summary>
		/// Округлять к большему значению.
		/// </summary>
		More,
	}

	/// <summary>
	/// Поставщик информации об инструментах, получающий данные из коллекции.
	/// </summary>
	public class CollectionSecurityProvider : ISecurityProvider
	{
		/// <summary>
		/// Создать <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		/// <param name="securities">Коллекция инструментов.</param>
		public CollectionSecurityProvider(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			_securities = securities;
		}

		private readonly IEnumerable<Security> _securities;

		/// <summary>
		/// Коллекция инструментов.
		/// </summary>
		protected virtual IEnumerable<Security> Securities
		{
			get { return _securities; }
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			var provider = Securities as ISecurityProvider;
			return provider == null ? Securities.Filter(criteria) : provider.Lookup(criteria);
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}
	}

	/// <summary>
	/// Поставщик информации об инструментах, получающий данные из <see cref="IConnector"/>.
	/// </summary>
	public class ConnectorSecurityProvider : CollectionSecurityProvider
	{
		private readonly IConnector _connector;

		/// <summary>
		/// Создать <see cref="ConnectorSecurityProvider"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		public ConnectorSecurityProvider(IConnector connector)
			: base(Enumerable.Empty<Security>())
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			_connector = connector;
		}

		/// <summary>
		/// Коллекция инструментов.
		/// </summary>
		protected override IEnumerable<Security> Securities
		{
			get { return _connector.Securities; }
		}
	}

	/// <summary>
	/// Вспомогательный класс для предоставления различной алгоритмической функциональности.
	/// </summary>
	public static class TraderHelper
	{
		/// <summary>
		/// Отфильтровать стакан от собственных заявок.
		/// </summary>
		/// <param name="quotes">Исходный стакан, который необходимо отфильтровать.</param>
		/// <param name="ownOrders">Активные заявки по данному инструменту.</param>
		/// <param name="orders">Заявки, которые необходимо игнорировать.</param>
		/// <returns>Отфильтрованный стакан.</returns>
		public static IEnumerable<Quote> GetFilteredQuotes(this IEnumerable<Quote> quotes, IEnumerable<Order> ownOrders, IEnumerable<Order> orders)
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (ownOrders == null)
				throw new ArgumentNullException("ownOrders");

			if (orders == null)
				throw new ArgumentNullException("orders");

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
		/// Получить рыночную цену для инструмента по максимально и минимально возможным ценам.
		/// </summary>
		/// <param name="security">Инструмент, по которому вычисляется рыночная цена.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="side">Направление заявки.</param>
		/// <returns>Рыночная цена. Если нет информации о максимально и минимально возможных ценах, то будет возвращено <see langword="null"/>.</returns>
		public static decimal? GetMarketPrice(this Security security, IMarketDataProvider provider, Sides side)
		{
			var board = security.CheckExchangeBoard();

			if (board.IsSupportMarketOrders)
				throw new ArgumentException(LocalizedStrings.Str1210Params.Put(board.Code), "security");

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
		/// Высчитать текущую цену по инструменту в зависимости от направления заявки.
		/// </summary>
		/// <param name="security">Инструмент, по которому вычисляется текущая цена.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="direction">Направление заявки.</param>
		/// <param name="priceType">Тип рыночной цены.</param>
		/// <param name="orders">Заявки, которые необходимо игнорировать.</param>
		/// <returns>Текущая цена. Если информации в стакане недостаточно, будет возвращено <see langword="null"/>.</returns>
		public static Unit GetCurrentPrice(this Security security, IMarketDataProvider provider, Sides? direction = null, MarketPriceTypes priceType = MarketPriceTypes.Following, IEnumerable<Order> orders = null)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

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
		/// Высчитать текущую цену по стакану в зависимости от направления заявки.
		/// </summary>
		/// <remarks>Для корректной работы метода необходимо запустить экспорт стакана.</remarks>
		/// <param name="depth">Стакан, по которому нужно высчитать текущую цену.</param>
		/// <param name="side">Направление заявки. Если это покупка, то будет использоваться
		/// значение <see cref="MarketDepth.BestAsk"/>, иначе <see cref="MarketDepth.BestBid"/>.</param>
		/// <param name="priceType">Тип текущей цены.</param>
		/// <param name="orders">Заявки, которые необходимо игнорировать.</param>
		/// <returns>Текущая цена. Если информации в стакане недостаточно, будет возвращено <see langword="null"/>.</returns>
		public static Unit GetCurrentPrice(this MarketDepth depth, Sides side, MarketPriceTypes priceType = MarketPriceTypes.Following, IEnumerable<Order> orders = null)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (orders != null)
			{
				var quotes = depth.GetFilteredQuotes(Enumerable.Empty<Order>(), orders);
				depth = new MarketDepth(depth.Security).Update(quotes, depth.LastChangeTime);
			}

			var pair = depth.BestPair;
			return pair == null ? null : pair.GetCurrentPrice(side, priceType);
		}

		/// <summary>
		/// Высчитать текущую цену по лучшей паре котировок в зависимости от направления заявки.
		/// </summary>
		/// <remarks>Для корректной работы метода необходимо запустить экспорт стакана.</remarks>
		/// <param name="bestPair">Лучшая пара котировок, по которой вычисляется текущая цена.</param>
		/// <param name="side">Направление заявки. Если это покупка, то будет использоваться
		/// значение <see cref="MarketDepthPair.Ask"/>, иначе <see cref="MarketDepthPair.Bid"/>.</param>
		/// <param name="priceType">Тип текущей цены.</param>
		/// <returns>Текущая цена. Если информации в стакане недостаточно, будет возвращено <see langword="null"/>.</returns>
		public static Unit GetCurrentPrice(this MarketDepthPair bestPair, Sides side, MarketPriceTypes priceType = MarketPriceTypes.Following)
		{
			if (bestPair == null)
				throw new ArgumentNullException("bestPair");

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
					throw new ArgumentOutOfRangeException("priceType");
			}

			return currentPrice == null
				? null
				: new Unit(currentPrice.Value).SetSecurity(bestPair.Security);
		}

		/// <summary>
		/// Применить для цены сдвиг в зависимости от направления <paramref name="side"/>.
		/// </summary>
		/// <param name="price">Цена.</param>
		/// <param name="side">Направление заявки, которое используется в качестве направления для сдвига (для покупки сдвиг прибавляется, для продажи - вычитается).</param>
		/// <param name="offset">Сдвиг цены.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Новая цена.</returns>
		public static decimal ApplyOffset(this Unit price, Sides side, Unit offset, Security security)
		{
			if (price == null)
				throw new ArgumentNullException("price");

			if (security == null)
				throw new ArgumentNullException("security");

			if (price.GetTypeValue == null)
				price.SetSecurity(security);

			if (offset.GetTypeValue == null)
				offset.SetSecurity(security);

			return security.ShrinkPrice((decimal)(side == Sides.Buy ? price + offset : price - offset));
		}

		/// <summary>
		/// Обрезать цену для заявки, чтобы она стала кратной минимальному шагу, а так же ограничить количество знаков после запятой.
		/// </summary>
		/// <param name="order">Заявка, для которой будет обрезана цена <see cref="Order.Price"/>.</param>
		/// <param name="rule">Правило округления цены.</param>
		public static void ShrinkPrice(this Order order, ShrinkRules rule = ShrinkRules.Auto)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			order.Price = order.Security.ShrinkPrice(order.Price, rule);
		}

		/// <summary>
		/// Обрезать цену, чтобы она стала кратной минимальному шагу, а так же ограничить количество знаков после запятой.
		/// </summary>
		/// <param name="security">Инструмент, из которого берется значения <see cref="Security.PriceStep"/> и <see cref="Security.Decimals"/>.</param>
		/// <param name="price">Цена, которую нужно сделать кратной.</param>
		/// <param name="rule">Правило округления цены.</param>
		/// <returns>Кратная цена.</returns>
		public static decimal ShrinkPrice(this Security security, decimal price, ShrinkRules rule = ShrinkRules.Auto)
		{
			security.CheckPriceStep();

			return price.Round(security.PriceStep, security.Decimals,
				rule == ShrinkRules.Auto
					? (MidpointRounding?)null
					: (rule == ShrinkRules.Less ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven)).RemoveTrailingZeros();
		}

		/// <summary>
		/// Получить позицию по Моей сделке.
		/// </summary>
		/// <param name="trade">Моя сделка, по которой рассчитывается позиция. При покупке объем сделки <see cref="Trade.Volume"/>
		/// берется с положительным знаком, при продаже - с отрицательным.</param>
		/// <returns>Позиция.</returns>
		public static decimal GetPosition(this MyTrade trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			return trade.Order.Direction == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}

		/// <summary>
		/// Получить позицию по Моей сделке.
		/// </summary>
		/// <param name="message">Моя сделка, по которой рассчитывается позиция. При покупке объем сделки <see cref="ExecutionMessage.Volume"/>
		/// берется с положительным знаком, при продаже - с отрицательным.</param>
		/// <returns>Позиция.</returns>
		public static decimal GetPosition(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return message.Side == Sides.Buy ? message.Volume : -message.Volume;
		}

		/// <summary>
		/// Получить позицию по заявке.
		/// </summary>
		/// <param name="order">Заявка, по которой рассчитывается позиция. При покупке позиция берется с положительным знаком, при продаже - с отрицательным.</param>
		/// <returns>Позиция.</returns>
		public static decimal GetPosition(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var volume = order.GetMatchedVolume();

			return order.Direction == Sides.Buy ? volume : -volume;
		}

		/// <summary>
		/// Получить позицию по портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель, для которого необходимо получить позицию.</param>
		/// <returns>Позиция по портфелю.</returns>
		public static decimal GetPosition(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (portfolio.Connector == null)
				throw new ArgumentException(LocalizedStrings.Str1039);

			return portfolio.Connector.Positions.Filter(portfolio).Sum(p => p.CurrentValue);
		}

		/// <summary>
		/// Получить позицию по Моим сделкам.
		/// </summary>
		/// <param name="trades">Мои сделки, по которым рассчитывается позиция через метод <see cref="GetPosition(StockSharp.BusinessEntities.MyTrade)"/>.</param>
		/// <returns>Позиция.</returns>
		public static decimal GetPosition(this IEnumerable<MyTrade> trades)
		{
			return trades.Sum(t => t.GetPosition());
		}

		/// <summary>
		/// Получить объем заявки, сопоставимый с размером позиции.
		/// </summary>
		/// <param name="position">Позиция по инструменту.</param>
		/// <returns>Объем заявки.</returns>
		public static decimal GetOrderVolume(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			return (position.CurrentValue / position.Security.VolumeStep).Abs();
		}

		/// <summary>
		/// Сгруппировать заявки по инструменту и портфелю.
		/// </summary>
		/// <remarks>Рекомендуется использовать для уменьшения транзакционных издержек.</remarks>
		/// <param name="orders">Исходные заявки.</param>
		/// <returns>Сгруппированные заявки.</returns>
		public static IEnumerable<Order> Join(this IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

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
		/// Рассчитать прибыль-убыток на основе сделок.
		/// </summary>
		/// <param name="trades">Сделки, по которым необходимо рассчитывать прибыль-убыток.</param>
		/// <returns>Прибыль-убыток.</returns>
		public static decimal GetPnL(this IEnumerable<MyTrade> trades)
		{
			return trades.Select(t => t.ToMessage()).GetPnL();
		}

		/// <summary>
		/// Рассчитать прибыль-убыток на основе сделок.
		/// </summary>
		/// <param name="trades">Сделки, по которым необходимо рассчитывать прибыль-убыток.</param>
		/// <returns>Прибыль-убыток.</returns>
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
		/// Рассчитать прибыль-убыток для сделки.
		/// </summary>
		/// <param name="trade">Сделка, для которой необходимо рассчитывать прибыль-убыток.</param>
		/// <param name="currentPrice">Текущая цена инструмента.</param>
		/// <returns>Прибыль-убыток.</returns>
		public static decimal GetPnL(this MyTrade trade, decimal currentPrice)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			return trade.ToMessage().GetPnL(currentPrice);
		}

		/// <summary>
		/// Рассчитать прибыль-убыток для сделки.
		/// </summary>
		/// <param name="trade">Сделка, для которой необходимо рассчитывать прибыль-убыток.</param>
		/// <param name="currentPrice">Текущая цена инструмента.</param>
		/// <returns>Прибыль-убыток.</returns>
		public static decimal GetPnL(this ExecutionMessage trade, decimal currentPrice)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			return GetPnL(trade.TradePrice, trade.Volume, trade.Side, currentPrice);
		}

		internal static decimal GetPnL(decimal price, decimal volume, Sides side, decimal marketPrice)
		{
			return (price - marketPrice) * volume * (side == Sides.Sell ? 1 : -1);
		}

		/// <summary>
		/// Рассчитать прибыль-убыток на основе портфеля.
		/// </summary>
		/// <param name="portfolio">Портфель, для которого необходимо расcчитать прибыль-убыток.</param>
		/// <returns>Прибыль-убыток.</returns>
		public static decimal GetPnL(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			return portfolio.CurrentValue - portfolio.BeginValue;
		}

		/// <summary>
		/// Рассчитать стоимость позиции.
		/// </summary>
		/// <param name="position">Позиция.</param>
		/// <param name="currentPrice">Текущая цена инструмента.</param>
		/// <returns>Стоимость позиции.</returns>
		public static decimal GetPrice(this Position position, decimal currentPrice)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			var security = position.Security;

			return currentPrice * position.CurrentValue * security.StepPrice / security.PriceStep;
		}

		/// <summary>
		/// Получить текущее время с учетом часового пояса торговой площадки инструмента.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Текущее время.</returns>
		public static DateTime GetMarketTime(this IConnector connector, Security security)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (security == null)
				throw new ArgumentNullException("security");

			var localTime = connector.CurrentTime;

			return security.ToExchangeTime(localTime);
		}

		/// <summary>
		/// Проверить, является ли время торгуемым (началась ли сессия, не закончилась ли, нет ли клиринга).
		/// </summary>
		/// <param name="board">Информация о площадке.</param>
		/// <param name="time">Передаваемое время, которое нужно проверить.</param>
		/// <returns><see langword="true"/>, если торгуемое время, иначе, неторгуемое.</returns>
		public static bool IsTradeTime(this ExchangeBoard board, DateTimeOffset time)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			return board.WorkingTime.IsTradeTime(time.DateTime);
		}

		/// <summary>
		/// Проверить, является ли время торгуемым (началась ли сессия, не закончилась ли, нет ли клиринга).
		/// </summary>
		/// <param name="workingTime">Информация о режиме работы биржи. Например, для FORTS будут значения 10:00-13:59, 14:03-18:49 и 19:00-23:49.</param>
		/// <param name="time">Передаваемое время, которое нужно проверить.</param>
		/// <returns><see langword="true"/>, если торгуемое время, иначе, неторгуемое.</returns>
		public static bool IsTradeTime(this WorkingTime workingTime, DateTime time)
		{
			var isWorkingDay = workingTime.IsTradeDate(time.Date);

			if (!isWorkingDay)
				return false;

			var period = workingTime.GetPeriod(time);

			var tod = time.TimeOfDay;
			return period == null || period.Times.IsEmpty() || period.Times.Any(r => r.Contains(tod));
		}

		/// <summary>
		/// Проверить, является ли дата торгуемой.
		/// </summary>
		/// <param name="board">Информация о площадке.</param>
		/// <param name="date">Передаваемая дата, которую необходимо проверить.</param>
		/// <param name="checkHolidays">Проверять ли переданную дату на день недели (суббота и воскресенье являются выходными и для них будет возвращено <see langword="false"/>).</param>
		/// <returns><see langword="true"/>, если торгуемая дата, иначе, неторгуемая.</returns>
		public static bool IsTradeDate(this ExchangeBoard board, DateTimeOffset date, bool checkHolidays = false)
		{
			return board.WorkingTime.IsTradeDate(date.DateTime, checkHolidays);
		}

		/// <summary>
		/// Проверить, является ли дата торгуемой.
		/// </summary>
		/// <param name="workingTime">Информация о режиме работы биржи.</param>
		/// <param name="date">Передаваемая дата, которую необходимо проверить.</param>
		/// <param name="checkHolidays">Проверять ли переданную дату на день недели (суббота и воскресенье являются выходными и для них будет возвращено <see langword="false"/>).</param>
		/// <returns><see langword="true"/>, если торгуемая дата, иначе, неторгуемая.</returns>
		public static bool IsTradeDate(this WorkingTime workingTime, DateTime date, bool checkHolidays = false)
		{
			if (workingTime == null)
				throw new ArgumentNullException("workingTime");

			var period = workingTime.GetPeriod(date);

			if ((period == null || period.Times.Length == 0) && workingTime.SpecialWorkingDays.Length == 0 && workingTime.SpecialHolidays.Length == 0)
				return true;

			bool isWorkingDay;

			if (checkHolidays && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
				isWorkingDay = workingTime.SpecialWorkingDays.Contains(date.Date);
			else
				isWorkingDay = !workingTime.SpecialHolidays.Contains(date.Date);

			return isWorkingDay;
		}

		/// <summary>
		/// Создать копию заявки для перерегистрации.
		/// </summary>
		/// <param name="oldOrder">Оригинальная заявка.</param>
		/// <param name="newPrice">Цена новой заявки.</param>
		/// <param name="newVolume">Объем новой заявки.</param>
		/// <returns>Новая заявка.</returns>
		public static Order ReRegisterClone(this Order oldOrder, decimal? newPrice = null, decimal? newVolume = null)
		{
			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			return new Order
			{
				Portfolio = oldOrder.Portfolio,
				Direction = oldOrder.Direction,
				TimeInForce = oldOrder.TimeInForce,
				Security = oldOrder.Security,
				Type = oldOrder.Type,
				Price = newPrice ?? oldOrder.Price,
				Volume = newVolume ?? oldOrder.Volume
			};
		}

		private static readonly ChannelFactory<IDailyInfoSoap> _dailyInfoFactory = new ChannelFactory<IDailyInfoSoap>(new BasicHttpBinding(), new EndpointAddress("http://www.cbr.ru/dailyinfowebserv/dailyinfo.asmx"));
		private static readonly Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>> _rateInfo = new Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>>();

		/// <summary>
		/// Сконвертировать одну валюту в другую.
		/// </summary>
		/// <param name="currencyFrom">Валюта, из которой нужно произвести конвертацию.</param>
		/// <param name="currencyTypeTo">Код валюты, в которую нужно произвести конвертацию.</param>
		/// <returns>Сконвертированная валюта.</returns>
		public static Currency Convert(this Currency currencyFrom, CurrencyTypes currencyTypeTo)
		{
			if (currencyFrom == null)
				throw new ArgumentNullException("currencyFrom");

			return new Currency { Type = currencyTypeTo, Value = currencyFrom.Value * currencyFrom.Type.Convert(currencyTypeTo) };
		}

		/// <summary>
		/// Получить курс конвертации одной валюту в другую.
		/// </summary>
		/// <param name="from">Код валюты, из которой нужно произвести конвертацию.</param>
		/// <param name="to">Код валюты, в которую нужно произвести конвертацию.</param>
		/// <returns>Курс.</returns>
		public static decimal Convert(this CurrencyTypes from, CurrencyTypes to)
		{
			return from.Convert(to, DateTime.Today);
		}

		/// <summary>
		/// Получить курс конвертации одной валюту в другую на определенную дату.
		/// </summary>
		/// <param name="from">Код валюты, из которой нужно произвести конвертацию.</param>
		/// <param name="to">Код валюты, в которую нужно произвести конвертацию.</param>
		/// <param name="date">Дата курса.</param>
		/// <returns>Курс.</returns>
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
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(from), "from");

			if (to != CurrencyTypes.RUB && !info.ContainsKey(to))
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(to), "to");

			if (from == CurrencyTypes.RUB)
				return 1 / info[to];
			else if (to == CurrencyTypes.RUB)
				return info[from];
			else
				return info[from] / info[to];
		}

		/// <summary>
		/// Создать из обычного стакана разреженный с минимальным шагом цены равный <see cref="Security.PriceStep"/>.
		/// <remarks>
		/// В разреженном стакане показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		/// </remarks>
		/// </summary>
		/// <param name="depth">Обычный стакан.</param>
		/// <returns>Разреженный стакан.</returns>
		public static MarketDepth Sparse(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			return depth.Sparse(depth.Security.PriceStep);
		}

		/// <summary>
		/// Создать из обычного стакана разреженный.
		/// <remarks>
		/// В разреженном стакане показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		/// </remarks>
		/// </summary>
		/// <param name="depth">Обычный стакан.</param>
		/// <param name="priceStep">Минимальный шаг цены.</param>
		/// <returns>Разреженный стакан.</returns>
		public static MarketDepth Sparse(this MarketDepth depth, decimal priceStep)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

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
		/// Создать из пары котировок разреженную коллекцию котировок, которая будет входить в диапазон между парой.
		/// <remarks>
		/// В разреженной коллекции показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		/// </remarks>
		/// </summary>
		/// <param name="pair">Пара обычных котировок.</param>
		/// <param name="priceStep">Минимальный шаг цены.</param>
		/// <returns>Разреженная коллекция котировок.</returns>
		public static IEnumerable<Quote> Sparse(this MarketDepthPair pair, decimal priceStep)
		{
			if (pair == null)
				throw new ArgumentNullException("pair");

			if (priceStep <= 0)
				throw new ArgumentOutOfRangeException("priceStep", priceStep, LocalizedStrings.Str1213);

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
		/// Создать из обычных котировок разреженную коллекцию котировок.
		/// <remarks>
		/// В разреженной коллекции показаны котировки на те цены, по которым не выставлены заявки. Объем таких котировок равен 0.
		/// </remarks>
		/// </summary>
		/// <param name="quotes">Обычные котировки. Коллекция должна содержать одинаково направленные котировки (только биды или только оффера).</param>
		/// <param name="priceStep">Минимальный шаг цены.</param>
		/// <returns>Разреженная коллекция котировок.</returns>
		public static IEnumerable<Quote> Sparse(this IEnumerable<Quote> quotes, decimal priceStep)
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (priceStep <= 0)
				throw new ArgumentOutOfRangeException("priceStep", priceStep, LocalizedStrings.Str1213);

			var list = quotes.OrderBy(q => q.Price).ToList();

			if (list.Count < 2)
				return ArrayHelper<Quote>.EmptyArray;

			var firstQuote = list[0];

			var retVal = new List<Quote>();

			for (var i = 0; i < (list.Count - 1); i++)
			{
				var from = list[i];

				if (from.OrderDirection != firstQuote.OrderDirection)
					throw new ArgumentException(LocalizedStrings.Str1214, "quotes");

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
		/// Объединить первоначальный стакан, и его разреженное представление.
		/// </summary>
		/// <param name="original">Первоначальный стакан.</param>
		/// <param name="rare">Разреженный стакан.</param>
		/// <returns>Объединенный стакан.</returns>
		public static MarketDepth Join(this MarketDepth original, MarketDepth rare)
		{
			if (original == null)
				throw new ArgumentNullException("original");

			if (rare == null)
				throw new ArgumentNullException("rare");

			return new MarketDepth(original.Security).Update(original.Concat(rare), original.LastChangeTime);
		}

		/// <summary>
		/// Сгруппировать стакан по ценовому диапазону.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо сгруппировать.</param>
		/// <param name="priceRange">Ценовой диапазон, по которому необходимо произвести группировку.</param>
		/// <returns>Сгруппированный стакан.</returns>
		public static MarketDepth Group(this MarketDepth depth, Unit priceRange)
		{
			return new MarketDepth(depth.Security).Update(depth.Bids.Group(priceRange), depth.Asks.Group(priceRange), true, depth.LastChangeTime);
		}

		/// <summary>
		/// Разгруппировать стакан, сгруппированный через метод <see cref="Group(StockSharp.BusinessEntities.MarketDepth,Unit)"/>.
		/// </summary>
		/// <param name="depth">Сгруппированный стакан.</param>
		/// <returns>Разгруппированный стакан.</returns>
		public static MarketDepth UnGroup(this MarketDepth depth)
		{
			return new MarketDepth(depth.Security).Update(
				depth.Bids.Cast<AggregatedQuote>().SelectMany(gq => gq.InnerQuotes),
				depth.Asks.Cast<AggregatedQuote>().SelectMany(gq => gq.InnerQuotes),
				false, depth.LastChangeTime);
		}

		/// <summary>
		/// Удалить в стакане те уровни, которые должны исчезнуть в случае появления сделок <paramref name="trades"/>.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо очистить.</param>
		/// <param name="trades">Сделки.</param>
		public static void EmulateTrades(this MarketDepth depth, IEnumerable<ExecutionMessage> trades)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (trades == null)
				throw new ArgumentNullException("trades");

			var changedVolume = new Dictionary<decimal, decimal>();

			var maxTradePrice = decimal.MinValue;
			var minTradePrice = decimal.MaxValue;

			foreach (var trade in trades)
			{
				minTradePrice = minTradePrice.Min(trade.TradePrice);
				maxTradePrice = maxTradePrice.Max(trade.TradePrice);

				var quote = depth.GetQuote(trade.TradePrice);

				if (null == quote)
					continue;

				decimal vol;
				if (!changedVolume.TryGetValue(trade.TradePrice, out vol))
					vol = quote.Volume;

				vol -= trade.Volume;
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
		/// Сгруппировать котировки по ценовому диапазону.
		/// </summary>
		/// <param name="quotes">Котировки, которые необходимо сгруппировать.</param>
		/// <param name="priceRange">Ценовой диапазон, по которому необходимо произвести группировку.</param>
		/// <returns>Сгруппированные котировки.</returns>
		public static IEnumerable<AggregatedQuote> Group(this IEnumerable<Quote> quotes, Unit priceRange)
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (priceRange == null)
				throw new ArgumentNullException("priceRange");

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
				throw new ArgumentNullException("priceRange");

			decimal priceLevel;

			if (priceRange.Type == UnitTypes.Percent)
				priceLevel = (decimal)(firstPrice + MathHelper.Floor((((price - firstPrice) * 100) / firstPrice), priceRange.Value).Percents());
			else
				priceLevel = MathHelper.Floor(price, (decimal)priceRange);

			return priceLevel;
		}

		/// <summary>
		/// Вычислить изменение между стаканами.
		/// </summary>
		/// <param name="from">Первый стакан.</param>
		/// <param name="to">Второй стакан.</param>
		/// <returns>Стакан, хранящий только приращения.</returns>
		public static QuoteChangeMessage GetDelta(this QuoteChangeMessage from, QuoteChangeMessage to)
		{
			if (from == null)
				throw new ArgumentNullException("from");

			if (to == null)
				throw new ArgumentNullException("to");

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
		/// Вычислить изменение между котировками.
		/// </summary>
		/// <param name="from">Первые котировки.</param>
		/// <param name="to">Вторые котировки.</param>
		/// <param name="side">Направление, показывающее тип котировок.</param>
		/// <returns>Изменения.</returns>
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
		/// Прибавить изменение к первому стакану.
		/// </summary>
		/// <param name="from">Первый стакан.</param>
		/// <param name="delta">Изменение.</param>
		/// <returns>Измененный стакан.</returns>
		public static QuoteChangeMessage AddDelta(this QuoteChangeMessage from, QuoteChangeMessage delta)
		{
			if (from == null)
				throw new ArgumentNullException("from");

			if (delta == null)
				throw new ArgumentNullException("delta");

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
		/// Прибавить изменение к котировки.
		/// </summary>
		/// <param name="fromQuotes">Котировки.</param>
		/// <param name="deltaQuotes">Изменения.</param>
		/// <param name="isBids">Признак направления котировок.</param>
		/// <returns>Измененные котировки.</returns>
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
		/// Вычислить приращение между котировками. 
		/// </summary>
		/// <param name="from">Первые котировки.</param>
		/// <param name="to">Вторые котировки.</param>
		/// <param name="side">Направление, показывающее тип котировок.</param>
		/// <param name="isSorted">Отсортированы ли котировки.</param>
		/// <returns>Изменения.</returns>
		public static IEnumerable<QuoteChange> GetDiff(this IEnumerable<QuoteChange> from, IEnumerable<QuoteChange> to, Sides side, bool isSorted)
		{
			if (!isSorted)
			{
				if (side == Sides.Sell)
				{
					from = from.OrderBy(q => q.Price);
					to = to.OrderBy(q => q.Price);
				}
				else
				{
					from = from.OrderByDescending(q => q.Price);
					to = to.OrderByDescending(q => q.Price);
				}
			}

			var diff = new List<QuoteChange>();

			var canProcessFrom = true;
			var canProcessTo = true;

			QuoteChange currFrom = null;
			QuoteChange currTo = null;

			var mult = side == Sides.Buy ? -1 : 1;

			using (var fromEnum = from.GetEnumerator())
			using (var toEnum = to.GetEnumerator())
			{
				while (true)
				{
					if (canProcessFrom && currFrom == null)
					{
						if (!fromEnum.MoveNext())
							canProcessFrom = false;
						else
							currFrom = fromEnum.Current;
					}

					if (canProcessTo && currTo == null)
					{
						if (!toEnum.MoveNext())
							canProcessTo = false;
						else
							currTo = toEnum.Current;
					}

					if (currFrom == null)
					{
						if (currTo == null)
							break;
						else
						{
							diff.Add(currTo.Clone());
							currTo = null;
						}
					}
					else
					{
						if (currTo == null)
						{
							var clone = currFrom.Clone();
							clone.Volume = -clone.Volume;
							diff.Add(clone);
							currFrom = null;
						}
						else
						{
							if (currFrom.Price == currTo.Price)
							{
								if (currFrom.Volume != currTo.Volume)
								{
									var clone = currTo.Clone();
									clone.Volume -= currFrom.Volume;
									diff.Add(clone);
								}

								currFrom = currTo = null;
							}
							else if (currFrom.Price * mult > currTo.Price * mult)
							{
								diff.Add(currTo.Clone());
								currTo = null;
							}
							else
							{
								var clone = currFrom.Clone();
								clone.Volume = -clone.Volume;
								diff.Add(clone);
								currFrom = null;
							}
						}
					}
				}
			}

			return diff;
		}

		/// <summary>
		/// Проверить, отменена ли заявка.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если заявка отменена, иначе, <see langword="false"/>.</returns>
		public static bool IsCanceled(this Order order)
		{
			return order.ToMessage().IsCanceled();
		}

		/// <summary>
		/// Проверить, исполнена ли полностью заявка.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если заявка полностью исполнена, иначе, <see langword="false"/>.</returns>
		public static bool IsMatched(this Order order)
		{
			return order.ToMessage().IsMatched();
		}

		/// <summary>
		/// Проверить, реализована ли часть объема в заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если часть объема реализована, иначе, <see langword="false"/>.</returns>
		public static bool IsMatchedPartially(this Order order)
		{
			return order.ToMessage().IsMatchedPartially();
		}

		/// <summary>
		/// Проверить, что не реализован ни один контракт в заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если ни один контракт не реализована, иначе, <see langword="false"/>.</returns>
		public static bool IsMatchedEmpty(this Order order)
		{
			return order.ToMessage().IsMatchedEmpty();
		}

		/// <summary>
		/// Проверить, отменена ли заявка.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если заявка отменена, иначе, <see langword="false"/>.</returns>
		public static bool IsCanceled(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (order.OrderState != OrderStates.Done)	// для ускорения в эмуляторе
				return false;

			return order.OrderState == OrderStates.Done && order.Balance != 0;
		}

		/// <summary>
		/// Проверить, исполнена ли полностью заявка.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если заявка полностью исполнена, иначе, <see langword="false"/>.</returns>
		public static bool IsMatched(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return order.OrderState == OrderStates.Done && order.Balance == 0;
		}

		/// <summary>
		/// Проверить, реализована ли часть объема в заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если часть объема реализована, иначе, <see langword="false"/>.</returns>
		public static bool IsMatchedPartially(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return order.Balance > 0 && order.Balance != order.Volume;
		}

		/// <summary>
		/// Проверить, что не реализован ни один контракт в заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо проверить.</param>
		/// <returns><see langword="true"/>, если ни один контракт не реализована, иначе, <see langword="false"/>.</returns>
		public static bool IsMatchedEmpty(this ExecutionMessage order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return order.Balance > 0 && order.Balance == order.Volume;
		}

		/// <summary>
		/// Получить сделки заявки.
		/// </summary>
		/// <param name="order">Заявки.</param>
		/// <returns>Сделки.</returns>
		public static IEnumerable<MyTrade> GetTrades(this Order order)
		{
			return order.CheckTrader().MyTrades.Filter(order);
		}

		/// <summary>
		/// Расcчитать реализованную часть объема для заявки.
		/// </summary>
		/// <param name="order">Заявка, для которой необходимо расcчитать реализованную часть объема.</param>
		/// <param name="byOrder">Проверять реализованный объем по балансу заявке (<see cref="Order.Balance"/>) или по полученным сделкам.
		/// По-умолчанию проверяется по заявке.</param>
		/// <returns>Реализованная часть объема.</returns>
		public static decimal GetMatchedVolume(this Order order, bool byOrder = true)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (order.Type == OrderTypes.Conditional)
			{
				//throw new ArgumentException("Стоп-заявки не могут иметь реализованный объем.", "order");

				order = order.DerivedOrder;

				if (order == null)
					return 0;
			}

			return order.Volume - (byOrder ? order.Balance : order.GetTrades().Sum(o => o.Trade.Volume));
		}

		/// <summary>
		/// Получить средневзрешанную цену исполнения заявки.
		/// </summary>
		/// <param name="order">Заявка, для которой необходимо получить средневзрешанную цену исполнения.</param>
		/// <returns>Средневзвешанная цена. Если заявка не существует ни одной сделки, то возвращается 0.</returns>
		public static decimal GetAveragePrice(this Order order)
		{
			return order.GetTrades().GetAveragePrice();
		}

		/// <summary>
		/// Получить средневзрешанную цену исполнения по собственным сделкам.
		/// </summary>
		/// <param name="trades">Сделки, для которых необходимо получить средневзрешанную цену исполнения.</param>
		/// <returns>Средневзвешанная цена. Если сделки отсутствуют, то возвращается 0.</returns>
		public static decimal GetAveragePrice(this IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

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
		/// Получить вероятные сделки по стакану для заданной заявки.
		/// </summary>
		/// <param name="depth">Стакан, который в момент вызова функции отражает ситуацию на рынке.</param>
		/// <param name="order">Заявку, для которой необходимо расcчитать вероятные сделки.</param>
		/// <returns>Вероятные сделки.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Order order)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (order == null)
				throw new ArgumentNullException("order");

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

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Order:
							if (execMsg.Error != null)
								errors.Add(execMsg.Error);

							break;
						case ExecutionTypes.Trade:
						{
							trades.Add(new MyTrade
							{
								Order = order,
								Trade = execMsg.ToTrade(new Trade { Security = order.Security })
							});

							break;
						}
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
		/// Получить вероятные сделки по стакану для рыночной цены и заданного объема.
		/// </summary>
		/// <param name="depth">Стакан, который в момент вызова функции отражает ситуацию на рынке.</param>
		/// <param name="orderDirection">Направление заявки.</param>
		/// <param name="volume">Объем, который предполагается реализовать.</param>
		/// <returns>Вероятные сделки.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides orderDirection, decimal volume)
		{
			return depth.GetTheoreticalTrades(orderDirection, volume, 0);
		}

		/// <summary>
		/// Получить вероятные сделки по стакану для заданных цены и объема.
		/// </summary>
		/// <param name="depth">Стакан, который в момент вызова функции отражает ситуацию на рынке.</param>
		/// <param name="orderDirection">Направление заявки.</param>
		/// <param name="volume">Объем, который предполагается реализовать.</param>
		/// <param name="price">Цена, по которой предполагает выставить заявку. Если она равна 0, то будет рассматриваться вариант рыночной заявки.</param>
		/// <returns>Вероятные сделки.</returns>
		public static IEnumerable<MyTrade> GetTheoreticalTrades(this MarketDepth depth, Sides orderDirection, decimal volume, decimal price)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

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
		/// Поменять направление на противоположное.
		/// </summary>
		/// <param name="side">Первоначальное направление.</param>
		/// <returns>Противоположное направление.</returns>
		public static Sides Invert(this Sides side)
		{
			return side == Sides.Buy ? Sides.Sell : Sides.Buy;
		}

		/// <summary>
		/// Получить направление заявки для позиции.
		/// </summary>
		/// <remarks>
		/// Положительное значение равно <see cref="Sides.Buy"/>, отрицательное - <see cref="Sides.Sell"/>, нулевое - null.
		/// </remarks>
		/// <param name="position">Значение позиции.</param>
		/// <returns>Направление заявки.</returns>
		public static Sides? GetDirection(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			return position.CurrentValue.GetDirection();
		}

		/// <summary>
		/// Получить направление заявки для позиции.
		/// </summary>
		/// <remarks>
		/// Положительное значение равно <see cref="Sides.Buy"/>, отрицательное - <see cref="Sides.Sell"/>, нулевое - null.
		/// </remarks>
		/// <param name="position">Значение позиции.</param>
		/// <returns>Направление заявки.</returns>
		public static Sides? GetDirection(this decimal position)
		{
			if (position == 0)
				return null;

			return position > 0 ? Sides.Buy : Sides.Sell;
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="connector">Подключение взаимодействия с торговыми системами.</param>
		/// <param name="orders">Группа заявок, из которой необходимо найти требуемые заявки и отменить их.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно null, то инструмент не попадает в фильтр снятия заявок.</param>
		public static void CancelOrders(this IConnector connector, IEnumerable<Order> orders, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (orders == null)
				throw new ArgumentNullException("orders");

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
		/// Отфильтровать заявки для заданного инструмента.
		/// </summary>
		/// <param name="orders">Все заявки, в которых необходимо искать требуемые.</param>
		/// <param name="security">Инструмент, для которого нужно отфильтровать заявки.</param>
		/// <returns>Отфильтрованные заявки.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Security security)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			if (security == null)
				throw new ArgumentNullException("security");

			var basket = security as BasketSecurity;
			return basket == null ? orders.Where(o => o.Security == security) : basket.InnerSecurities.SelectMany(s => Filter(orders, s));
		}

		/// <summary>
		/// Отфильтровать заявки для заданного портфеля.
		/// </summary>
		/// <param name="orders">Все заявки, в которых необходимо искать требуемые.</param>
		/// <param name="portfolio">Портфель, для которого нужно отфильтровать заявки.</param>
		/// <returns>Отфильтрованные заявки.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Portfolio portfolio)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			return orders.Where(p => p.Portfolio == portfolio);
		}

		/// <summary>
		/// Отфильтровать заявки для заданного состояния.
		/// </summary>
		/// <param name="orders">Все заявки, в которых необходимо искать требуемые.</param>
		/// <param name="state">Состояние заявки.</param>
		/// <returns>Отфильтрованные заявки.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, OrderStates state)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			return orders.Where(p => p.State == state);
		}

		/// <summary>
		/// Отфильтровать заявки для заданного направления.
		/// </summary>
		/// <param name="orders">Все заявки, в которых необходимо искать требуемые.</param>
		/// <param name="direction">Направление заявки.</param>
		/// <returns>Отфильтрованные заявки.</returns>
		public static IEnumerable<Order> Filter(this IEnumerable<Order> orders, Sides direction)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			return orders.Where(p => p.Direction == direction);
		}

		/// <summary>
		/// Отфильтровать сделки для заданного инструмента.
		/// </summary>
		/// <param name="trades">Все сделки, в которых необходимо искать требуемые.</param>
		/// <param name="security">Инструмент, для которого нужно отфильтровать сделки.</param>
		/// <returns>Отфильтрованные сделки.</returns>
		public static IEnumerable<Trade> Filter(this IEnumerable<Trade> trades, Security security)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			if (security == null)
				throw new ArgumentNullException("security");

			var basket = security as BasketSecurity;
			return basket == null ? trades.Where(t => t.Security == security) : basket.InnerSecurities.SelectMany(s => Filter(trades, s));
		}

		/// <summary>
		/// Отфильтровать сделки для заданного временного периода.
		/// </summary>
		/// <param name="trades">Все сделки, в которых необходимо искать требуемые.</param>
		/// <param name="from">Дата, с которой нужно искать сделки.</param>
		/// <param name="to">Дата, до которой нужно искать сделки.</param>
		/// <returns>Отфильтрованные сделки.</returns>
		public static IEnumerable<Trade> Filter(this IEnumerable<Trade> trades, DateTime from, DateTime to)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			return trades.Where(trade => trade.Time >= from && trade.Time < to);
		}

		/// <summary>
		/// Отфильтровать позиции для заданного инструмента.
		/// </summary>
		/// <param name="positions">Все позиции, в которых необходимо искать требуемые.</param>
		/// <param name="security">Инструмент, для которого нужно отфильтровать позиции.</param>
		/// <returns>Отфильтрованные позиции.</returns>
		public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, Security security)
		{
			if (positions == null)
				throw new ArgumentNullException("positions");

			if (security == null)
				throw new ArgumentNullException("security");

			var basket = security as BasketSecurity;
			return basket == null ? positions.Where(p => p.Security == security) : basket.InnerSecurities.SelectMany(s => Filter(positions, s));
		}

		/// <summary>
		/// Отфильтровать позиции для заданного портфеля.
		/// </summary>
		/// <param name="positions">Все позиции, в которых необходимо искать требуемые.</param>
		/// <param name="portfolio">Портфель, для которого нужно отфильтровать позиции.</param>
		/// <returns>Отфильтрованные позиции.</returns>
		public static IEnumerable<Position> Filter(this IEnumerable<Position> positions, Portfolio portfolio)
		{
			if (positions == null)
				throw new ArgumentNullException("positions");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			return positions.Where(p => p.Portfolio == portfolio);
		}

		/// <summary>
		/// Отфильтровать собственные сделки для заданного инструмента.
		/// </summary>
		/// <param name="myTrades">Все собственные сделки, в которых необходимо искать требуемые.</param>
		/// <param name="security">Инструмент, по которому нужно найти сделки.</param>
		/// <returns>Отфильтрованные сделки.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Security security)
		{
			if (myTrades == null)
				throw new ArgumentNullException("myTrades");

			if (security == null)
				throw new ArgumentNullException("security");

			var basket = security as BasketSecurity;
			return basket == null ? myTrades.Where(t => t.Order.Security == security) : basket.InnerSecurities.SelectMany(s => Filter(myTrades, s));
		}

		/// <summary>
		/// Отфильтровать собственные сделки для заданного портфеля.
		/// </summary>
		/// <param name="myTrades">Все собственные сделки, в которых необходимо искать требуемые.</param>
		/// <param name="portfolio">Портфель, для которого нужно отфильтровать сделки.</param>
		/// <returns>Отфильтрованные сделки.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Portfolio portfolio)
		{
			if (myTrades == null)
				throw new ArgumentNullException("myTrades");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			return myTrades.Where(t => t.Order.Portfolio == portfolio);
		}

		/// <summary>
		/// Отфильтровать собственные сделки для заданной заявки.
		/// </summary>
		/// <param name="myTrades">Все собственные сделки, в которых необходимо искать требуемые.</param>
		/// <param name="order">Заявка, для которой нужно отфильтровать сделки.</param>
		/// <returns>Отфильтрованные заявки.</returns>
		public static IEnumerable<MyTrade> Filter(this IEnumerable<MyTrade> myTrades, Order order)
		{
			if (myTrades == null)
				throw new ArgumentNullException("myTrades");

			if (order == null)
				throw new ArgumentNullException("order");

			return myTrades.Where(t => t.Order == order);
		}

		/// <summary>
		/// Отфильтровать <see cref="Connector.Securities"/> по заданному критерию.
		/// </summary>
		/// <param name="connector">Инструменты.</param>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Отфильтрованные инструменты.</returns>
		public static IEnumerable<Security> FilterSecurities(this Connector connector, SecurityLookupMessage criteria)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var security = connector.GetSecurityCriteria(criteria);

			return connector.Securities.Filter(security);
		}

		/// <summary>
		/// Создать критерий поиска <see cref="Security"/> из <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Критерий поиска.</returns>
		public static Security GetSecurityCriteria(this Connector connector, SecurityLookupMessage criteria)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var stocksharpId = criteria.SecurityId.SecurityCode.IsEmpty() || criteria.SecurityId.BoardCode.IsEmpty()
				                   ? string.Empty
				                   : connector.SecurityIdGenerator.GenerateId(criteria.SecurityId.SecurityCode, criteria.SecurityId.BoardCode);

			return new Security
			{
				Id = stocksharpId,
				Name = criteria.Name,
				Code = criteria.SecurityId.SecurityCode,
				Type = criteria.SecurityType,
				ExpiryDate = criteria.ExpiryDate,
				ExternalId = new SecurityExternalId
				{
					Bloomberg = criteria.SecurityId.Bloomberg,
					Cusip = criteria.SecurityId.Cusip,
					IQFeed = criteria.SecurityId.IQFeed,
					Isin = criteria.SecurityId.Isin,
					Ric = criteria.SecurityId.Ric,
					Sedol = criteria.SecurityId.Sedol,
				},
				Board = criteria.SecurityId.BoardCode.IsEmpty() ? null : ExchangeBoard.GetOrCreateBoard(criteria.SecurityId.BoardCode),
				ShortName = criteria.ShortName,
				VolumeStep = criteria.VolumeStep,
				Multiplier = criteria.Multiplier,
				PriceStep = criteria.PriceStep,
				OptionType = criteria.OptionType,
				Strike = criteria.Strike,
				BinaryOptionType = criteria.BinaryOptionType,
				Currency = criteria.Currency,
				SettlementDate = criteria.SettlementDate,
				UnderlyingSecurityId = (criteria.UnderlyingSecurityCode.IsEmpty() || criteria.SecurityId.BoardCode.IsEmpty())
					? null
					: connector.SecurityIdGenerator.GenerateId(criteria.UnderlyingSecurityCode, criteria.SecurityId.BoardCode),
			};
		}

		/// <summary>
		/// Отфильтровать инструменты по торговой площадке.
		/// </summary>
		/// <param name="securities">Инструменты.</param>
		/// <param name="board">Торговая площадка.</param>
		/// <returns>Отфильтрованные инструменты.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, ExchangeBoard board)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			if (board == null)
				throw new ArgumentNullException("board");

			return securities.Where(s => s.Board == board);
		}

		/// <summary>
		/// Отфильтровать инструменты по заданному критерию.
		/// </summary>
		/// <param name="securities">Инструменты.</param>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Отфильтрованные инструменты.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, Security criteria)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var id = criteria.Id;

			if (!id.IsEmpty())
				return securities.Where(s => s.Id == criteria.Id).ToArray();

			var code = criteria.Code;

			if (code == "*")
				return securities.ToArray();

			return securities.Where(s =>
			{
				if (!code.IsEmpty() && !s.Code.ContainsIgnoreCase(code))
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

				if (criteria.Strike != 0 && s.Strike != criteria.Strike)
					return false;

				if (criteria.OptionType != null && s.OptionType != criteria.OptionType)
					return false;

				if (criteria.Currency != CurrencyTypes.RUB && s.Currency != criteria.Currency)
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

				return true;
			}).ToArray();
		}

		/// <summary>
		/// Определить, является ли стакан пустым.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		/// <returns><see langword="true"/>, если стакан пустой, иначе, <see langword="false"/>.</returns>
		public static bool IsFullEmpty(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			return depth.Bids.Length ==0 && depth.Asks.Length == 0;
		}

		/// <summary>
		/// Определить, является ли стакан пустым на половину.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		/// <returns><see langword="true"/>, если стакан пустой на половину, иначе, <see langword="false"/>.</returns>
		public static bool IsHalfEmpty(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			return (depth.BestPair.Bid == null || depth.BestPair.Ask == null) && (depth.BestPair.Bid != depth.BestPair.Ask);
		}

		/// <summary>
		/// Получить T+N дату.
		/// </summary>
		/// <param name="time">Информация о режиме работы биржи.</param>
		/// <param name="date">Начальная дата T.</param>
		/// <param name="n">Размер N.</param>
		/// <returns>Конечная дата T+N.</returns>
		public static DateTime GetTPlusNDate(this WorkingTime time, DateTime date, int n)
		{
			if (time == null)
				throw new ArgumentNullException("time");

			date = date.Date;

			while (n > 0)
			{
				if (time.IsTradeDate(date))
					n--;

				date = date.AddDays(1);
			}

			return date;
		}

		/// <summary>
		/// Перевести локальное время в биржевое.
		/// </summary>
		/// <param name="exchange">Информация о бирже.</param>
		/// <param name="time">Локальное время.</param>
		/// <returns>Время с биржевым сдвигом.</returns>
		public static DateTime ToExchangeTime(this Exchange exchange, DateTimeOffset time)
		{
			if (exchange == null)
				throw new ArgumentNullException("exchange");

			return time.ToLocalTime(exchange.TimeZoneInfo);
		}

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

		/// <summary>
		/// Перевести локальное время в биржевое.
		/// </summary>
		/// <param name="security">Информация о инструменте.</param>
		/// <param name="localTime">Локальное время.</param>
		/// <returns>Время с биржевым сдвигом.</returns>
		public static DateTime ToExchangeTime(this Security security, DateTimeOffset localTime)
		{
			if (security == null) 
				throw new ArgumentNullException("security");

			if (security.Board == null)
				throw new ArgumentException(LocalizedStrings.Str1215Params.Put(security.Id), "security");

			if (security.Board.Exchange == null)
				throw new ArgumentException(LocalizedStrings.Str1216Params.Put(security.Id), "security");

			return security.Board.Exchange.ToExchangeTime(localTime);
		}

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

		/// <summary>
		/// Перевести биржевое время в UTC.
		/// </summary>
		/// <param name="exchange">Информация о бирже, из которой будет использоваться <see cref="Exchange.TimeZoneInfo"/>.</param>
		/// <param name="exchangeTime">Биржевое время.</param>
		/// <returns>Биржевое время в UTC.</returns>
		public static DateTime ToUtc(this Exchange exchange, DateTime exchangeTime)
		{
			if (exchange == null)
				throw new ArgumentNullException("exchange");

			return TimeZoneInfo.ConvertTimeToUtc(exchangeTime, exchange.TimeZoneInfo);
		}

		/// <summary>
		/// Вычислить задержку на основе разницы между серверным времени и локальным.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="serverTime">Серверное время.</param>
		/// <param name="localTime">Локальное время.</param>
		/// <returns>Задержка.</returns>
		public static TimeSpan GetLatency(this Security security, DateTimeOffset serverTime, DateTime localTime)
		{
			return localTime - serverTime.LocalDateTime;
		}

		/// <summary>
		/// Вычислить задержку на основе разницы между серверным времени и локальным.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="serverTime">Серверное время.</param>
		/// <param name="localTime">Локальное время.</param>
		/// <returns>Задержка.</returns>
		public static TimeSpan GetLatency(this SecurityId securityId, DateTimeOffset serverTime, DateTime localTime)
		{
			var board = ExchangeBoard.GetBoard(securityId.BoardCode);

			if (board == null)
				throw new ArgumentException(LocalizedStrings.Str1217Params.Put(securityId.BoardCode), "securityId");

			return localTime - serverTime.LocalDateTime;
		}

		/// <summary>
		/// Получить размер свободных денежных средств в портфеле.
		/// </summary>
		/// <param name="portfolio">Портфель</param>
		/// <param name="useLeverage">Использовать ли для рассчета размер плеча.</param>
		/// <returns>Размер свободных денежных средств.</returns>
		public static decimal GetFreeMoney(this Portfolio portfolio, bool useLeverage = false)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			var freeMoney = portfolio.Board == ExchangeBoard.Forts
				? portfolio.BeginValue - portfolio.CurrentValue + portfolio.VariationMargin
				: portfolio.CurrentValue;

			return useLeverage ? freeMoney * portfolio.Leverage : freeMoney;
		}

		/// <summary>
		/// Получить даты экспирации для <see cref="ExchangeBoard.Forts"/>.
		/// </summary>
		/// <param name="from">Начало диапазона экспираций.</param>
		/// <param name="to">Окончание диапазона экспираций.</param>
		/// <returns>Даты экспирации.</returns>
		public static IEnumerable<DateTime> GetExpiryDates(this DateTime from, DateTime to)
		{
			if (from > to)
				throw new ArgumentOutOfRangeException("from");

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
							var dt = new DateTime(year, month, 15);
							while (!ExchangeBoard.Forts.WorkingTime.IsTradeDate(dt))
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
		/// Получить для базовой части кода инструмента реальные экспирирующиеся инструменты.
		/// </summary>
		/// <param name="baseCode">Базовая часть кода инструмента.</param>
		/// <param name="from">Начало диапазона экспираций.</param>
		/// <param name="to">Окончание диапазона экспираций.</param>
		/// <param name="getSecurity">Функция для получения инструмента по коду.</param>
		/// <param name="throwIfNotExists">Сгенерировать исключение, если какой-либо из инструментов отсутствует.</param>
		/// <returns>Экспирирующиеся инструменты.</returns>
		public static IEnumerable<Security> GetFortsJumps(this string baseCode, DateTime from, DateTime to, Func<string, Security> getSecurity, bool throwIfNotExists = true)
		{
			if (baseCode.IsEmpty())
				throw new ArgumentNullException("baseCode");

			if (from > to)
				throw new ArgumentOutOfRangeException("from");

			if (getSecurity == null)
				throw new ArgumentNullException("getSecurity");

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
		/// Получить для непрерывного инструмента реальные экспирирующиеся инструменты.
		/// </summary>
		/// <param name="continuousSecurity">Непрерывный инструмент.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="baseCode">Базовая часть кода инструмента.</param>
		/// <param name="from">Начало диапазона экспираций.</param>
		/// <param name="to">Окончание диапазона экспираций.</param>
		/// <param name="throwIfNotExists">Сгенерировать исключение, если какой-либо из инструментов для переданного <paramref name="continuousSecurity"/> отсутствует.</param>
		/// <returns>Экспирирующиеся инструменты.</returns>
		public static IEnumerable<Security> GetFortsJumps(this ContinuousSecurity continuousSecurity, ISecurityProvider provider, string baseCode, DateTime from, DateTime to, bool throwIfNotExists = true)
		{
			if (continuousSecurity == null)
				throw new ArgumentNullException("continuousSecurity");

			if (provider == null)
				throw new ArgumentNullException("provider");

			return baseCode.GetFortsJumps(from, to, code => provider.LookupByCode(code).FirstOrDefault(), throwIfNotExists);
		}

		/// <summary>
		/// Заполнить переходы <see cref="ContinuousSecurity.ExpirationJumps"/>.
		/// </summary>
		/// <param name="continuousSecurity">Непрерывный инструмент.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="baseCode">Базовая часть кода инструмента.</param>
		/// <param name="from">Начало диапазона экспираций.</param>
		/// <param name="to">Окончание диапазона экспираций.</param>
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

			public CashPosition(Portfolio portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException("portfolio");

				_portfolio = portfolio;

				Portfolio = _portfolio;
				Security = new Security
				{
					Id = _portfolio.Name,
					Name = _portfolio.Name,
				};

				UpdatePosition();

				_portfolio.Connector.PortfoliosChanged += TraderOnPortfoliosChanged;
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
				_portfolio.Connector.PortfoliosChanged -= TraderOnPortfoliosChanged;
			}
		}

		/// <summary>
		/// Сконвертировать портфель в денежную позицию.
		/// </summary>
		/// <param name="portfolio">Портфель с торговым счетом.</param>
		/// <returns>Денежная позиция.</returns>
		public static Position ToCashPosition(this Portfolio portfolio)
		{
			return new CashPosition(portfolio);
		}

		private sealed class NativePositionManager : IPositionManager
		{
			private readonly Position _position;

			public NativePositionManager(Position position)
			{
				if (position == null)
					throw new ArgumentNullException("position");

				_position = position;
			}

			/// <summary>
			/// Суммарное значение позиции.
			/// </summary>
			decimal IPositionManager.Position
			{
				get { return _position.CurrentValue; }
				set { throw new NotSupportedException(); }
			}

			event Action<Position> IPositionManager.NewPosition
			{
				add { }
				remove { }
			}

			event Action<Position> IPositionManager.PositionChanged
			{
				add { }
				remove { }
			}

			/// <summary>
			/// Рассчитать позицию по заявке.
			/// </summary>
			/// <param name="order">Заявка.</param>
			/// <returns>Позиция по заявке.</returns>
			decimal IPositionManager.ProcessOrder(Order order)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Рассчитать позицию по сделке.
			/// </summary>
			/// <param name="trade">Сделка.</param>
			/// <returns>Позиция по сделке.</returns>
			decimal IPositionManager.ProcessMyTrade(MyTrade trade)
			{
				throw new NotSupportedException();
			}


			IEnumerable<Position> IPositionManager.Positions
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
		}

		/// <summary>
		/// Сконвертировать позицию в объект типа <see cref="IPositionManager"/>.
		/// </summary>
		/// <param name="position">Позиция.</param>
		/// <returns>Менеджера расчета позиции.</returns>
		public static IPositionManager ToPositionManager(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			return new NativePositionManager(position);
		}

		/// <summary>
		/// Записать сообщение о заявке в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="order">Заявка.</param>
		/// <param name="operation">Операция, которая проводится в заявокй.</param>
		/// <param name="getAdditionalInfo">Дополнительная информация о заявке.</param>
		public static void AddOrderInfoLog(this ILogReceiver receiver, Order order, string operation, Func<string> getAdditionalInfo = null)
		{
			receiver.AddOrderLog(LogLevels.Info, order, operation, getAdditionalInfo);
		}

		/// <summary>
		/// Записать ошибку о заявке в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="order">Заявка.</param>
		/// <param name="operation">Операция, которая проводится в заявокй.</param>
		/// <param name="getAdditionalInfo">Дополнительная информация о заявке.</param>
		public static void AddOrderErrorLog(this ILogReceiver receiver, Order order, string operation, Func<string> getAdditionalInfo = null)
		{
			receiver.AddOrderLog(LogLevels.Error, order, operation, getAdditionalInfo);
		}

		private static void AddOrderLog(this ILogReceiver receiver, LogLevels type, Order order, string operation, Func<string> getAdditionalInfo)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (order == null)
				throw new ArgumentNullException("order");

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
					throw new ArgumentNullException("connector");

				if (criteria == null)
					throw new ArgumentNullException("criteria");
				
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
		/// Выполнить блокирующий поиск инструментов, соответствующих фильтру criteria.
		/// </summary>
		/// <param name="connector">Подключение взаимодействия с торговой системой.</param>
		/// <param name="criteria">Критерий поиска инструментов.</param>
		/// <returns>Найденные инструменты.</returns>
		public static IEnumerable<Security> SyncLookupSecurities(this IConnector connector, Security criteria)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (criteria == null)
				throw new ArgumentNullException("criteria");

			using (var lsu = new LookupSecurityUpdate(connector, criteria, TimeSpan.FromSeconds(180)))
			{
				return lsu.Wait();
			}
		}

		/// <summary>
		/// Применить изменения к портфелю.
		/// </summary>
		/// <param name="portfolio">Портфель.</param>
		/// <param name="message">Сообщение об изменении портфеля.</param>
		public static void ApplyChanges(this Portfolio portfolio, PortfolioChangeMessage message)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (message == null)
				throw new ArgumentNullException("message");

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
		/// Применить изменения к позиции.
		/// </summary>
		/// <param name="position">Позиция.</param>
		/// <param name="message">Сообщение об изменении позиции.</param>
		public static void ApplyChanges(this Position position, PositionChangeMessage message)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			if (message == null)
				throw new ArgumentNullException("message");

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
						throw new ArgumentOutOfRangeException("change", change.Key, LocalizedStrings.Str1219);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.Str1220Params.Put(change.Key), ex);
			}
		}

		/// <summary>
		/// Применить изменения к инструменту.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="changes">Изменения.</param>
		/// <param name="serverTime">Серверное время изменения.</param>
		/// <param name="localTime">Метка локального времени, когда сообщение было получено/создано.</param>
		public static void ApplyChanges(this Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (changes == null)
				throw new ArgumentNullException("changes");

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
		/// Применить изменения к инструменту.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="message">Изменения.</param>
		public static void ApplyChanges(this Security security, Level1ChangeMessage message)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

			security.ApplyChanges(message.Changes, message.ServerTime, message.LocalTime);
		}

		/// <summary>
		/// Добавить изменение в коллекцию.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, object value)
			where TMessage : BaseChangeMessage<TChange>
		{
			message.Changes[type] = value;
			return message;
		}

		/// <summary>
		/// Добавить изменение в коллекцию.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, decimal value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, int value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, long value)
			where TMessage : BaseChangeMessage<TChange>
		{
			return message.Add(type, (object)value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию, если значение отлично от 0.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == 0)
				return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию, если значение отлично от 0 и null.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null || value == 0)
				return message;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию, если значение отлично от 0.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == 0)
				return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию, если значение отлично от 0 и null.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int? value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == null || value == 0)
				return null;

			return message.Add(type, value.Value);
		}

		/// <summary>
		/// Добавить изменение в коллекцию, если значение отлично от 0.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения с изменениями.</typeparam>
		/// <typeparam name="TChange">Тип изменения.</typeparam>
		/// <param name="message">Сообщение с изменениями.</param>
		/// <param name="type">Вид изменения.</param>
		/// <param name="value">Значение изменения.</param>
		/// <returns>Сообщение с изменениями.</returns>
		public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, long value)
			where TMessage : BaseChangeMessage<TChange>
		{
			if (value == 0)
				return message;

			return message.Add(type, value);
		}

		/// <summary>
		/// Преобразовать тип валюты в название в формате ММВБ.
		/// </summary>
		/// <param name="type">Тип валюты.</param>
		/// <returns>Название валюты в формате ММВБ.</returns>
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
		/// Преобразовать название валюты в формате ММВБ в <see cref="CurrencyTypes"/>.
		/// </summary>
		/// <param name="name">Название валюты в формате ММВБ.</param>
		/// <returns>Тип валюты. Если название валюты пустое, то будет возвращено <see langword="null"/>.</returns>
		public static CurrencyTypes? FromMicexCurrencyName(this string name)
		{
			if (name.IsEmpty())
				return null;

			switch (name)
			{
				case "SUR":
					return CurrencyTypes.RUB;
				default:
					return name.To<CurrencyTypes>();
			}
		}

		/// <summary>
		/// Получить период для режима.
		/// </summary>
		/// <param name="time">Режим торгов.</param>
		/// <param name="date">Дата во времени, для которой будет искать подходящий период.</param>
		/// <returns>Период расписания. Если ни один период не подходит, то будет возвращено <see langword="null"/>.</returns>
		public static WorkingTimePeriod GetPeriod(this WorkingTime time, DateTime date)
		{
			if (time == null)
				throw new ArgumentNullException("time");

			return time.Periods.FirstOrDefault(p => p.Till >= date);
		}

		/// <summary>
		/// Получить описание инструмента по классу из <see cref="IMessageSessionHolder.SecurityClassInfo"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		/// <param name="secClass">Класс инструмента.</param>
		/// <returns>Описание инструмента. Если класс не найден в <see cref="IMessageSessionHolder.SecurityClassInfo"/>,
		/// то будет возвращено значение <see langword="null"/> в качестве типа инструмента.</returns>
		public static Tuple<SecurityTypes?, string> GetSecurityClassInfo(this IMessageSessionHolder sessionHolder, string secClass)
		{
			var pair = sessionHolder.SecurityClassInfo.TryGetValue(secClass);
			return Tuple.Create(pair == null ? (SecurityTypes?)null : pair.First, pair == null ? secClass : pair.Second);
		}

		/// <summary>
		/// Получить код площадки для класса инструмента.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		/// <param name="secClass">Класс инструмента.</param>
		/// <returns>Код площадки.</returns>
		public static string GetBoardCode(this IMessageSessionHolder sessionHolder, string secClass)
		{
			return sessionHolder.GetSecurityClassInfo(secClass).Item2;
		}

		/// <summary>
		/// Получить шаг цены на основе точности.
		/// </summary>
		/// <param name="decimals">Точность.</param>
		/// <returns>Шан цены.</returns>
		public static decimal GetPriceStep(this int decimals)
		{
			return 1m / 10.Pow(decimals);
		}

		/// <summary>
		/// Разделитель, заменяющий '/' в пути для инструментов вида USD/EUR. Равен '__'.
		/// </summary>
		public const string SecurityPairSeparator = "__";

		/// <summary>
		/// Разделитель, заменяющий '*' в пути для инструментов вида C.BPO-*@CANADIAN. Равен '##STAR##'.
		/// </summary>
		public const string SecurityStarSeparator = "##STAR##";

		// http://stackoverflow.com/questions/62771/how-check-if-given-string-is-legal-allowed-file-name-under-windows
		private static readonly string[] _reservedDos =
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		/// <summary>
		/// Преобразовать идентификатор инструмента в название директории с заменой зарезервированных символов.
		/// </summary>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <returns>Название директории.</returns>
		public static string SecurityIdToFolderName(this string id)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException("id");

			var folderName = id;

			if (_reservedDos.Any(d => folderName.StartsWith(d, StringComparison.InvariantCultureIgnoreCase)))
				folderName = "_" + folderName;

			return folderName
				.Replace("/", SecurityPairSeparator) // для пар вида USD/EUR
				.Replace("*", SecurityStarSeparator) // http://stocksharp.com/forum/yaf_postst4637_API-4-2-2-18--System-ArgumentException--Illegal-characters-in-path.aspx
				;
		}

		/// <summary>
		/// Обратное преобразование от метода <see cref="SecurityIdToFolderName"/>.
		/// </summary>
		/// <param name="folderName">Название директории.</param>
		/// <returns>Идентификатор инструмента.</returns>
		public static string FolderNameToSecurityId(this string folderName)
		{
			if (folderName.IsEmpty())
				throw new ArgumentNullException("folderName");

			var id = folderName.ToUpperInvariant();

			if (id[0] == '_' && _reservedDos.Any(d => id.StartsWith("_" + d, StringComparison.InvariantCultureIgnoreCase)))
				id = id.Substring(1);

			return id
				.ReplaceIgnoreCase(SecurityPairSeparator, "/")
				.ReplaceIgnoreCase(SecurityStarSeparator, "*");
		}

		/// <summary>
		/// Преобразовать параметр свечи в название директории с заменой зарезервированных символов.
		/// </summary>
		/// <param name="arg">Параметр свечи.</param>
		/// <returns>Название директории.</returns>
		public static string CandleArgToFolderName(object arg)
		{
			return arg == null ? string.Empty : arg.ToString().Replace(":", "-");
		}

		/// <summary>
		/// Получить инструмент по идентификатору.
		/// </summary>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <returns>Полученный инструмент. Если инструмент по данным критериям отсутствует, то будет возвращено <see langword="null"/>.</returns>
		public static Security LookupById(this ISecurityProvider provider, string id)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			if (id.IsEmpty())
				throw new ArgumentNullException("id");

			return provider.Lookup(new Security { Id = id }).SingleOrDefault();
		}

		/// <summary>
		/// Получить инструмент по коду инструмента.
		/// </summary>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="code">Код инструмента.</param>
		/// <returns>Полученный инструмент. Если инструмент по данным критериям отсутствует, то будет возвращено <see langword="null"/>.</returns>
		public static IEnumerable<Security> LookupByCode(this ISecurityProvider provider, string code)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			if (code.IsEmpty())
				throw new ArgumentNullException("code");

			return provider.Lookup(new Security { Code = code });
		}

		/// <summary>
		/// Получить все доступные инструменты.
		/// </summary>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <returns>Все доступные инструменты.</returns>
		public static IEnumerable<Security> LookupAll(this ISecurityProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			return provider.Lookup(new Security { Code = "*" });
		}

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <typeparam name="T">Тип значения поля маркет-данных.</typeparam>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public static T GetSecurityValue<T>(this IMarketDataProvider provider, Security security, Level1Fields field)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			if (security == null)
				throw new ArgumentNullException("security");

			return (T)provider.GetSecurityValue(security, field);
		}

		/// <summary>
		/// Получить все значения маркет-данных для инструмента.
		/// </summary>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Значения полей. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public static IDictionary<Level1Fields, object> GetSecurityValues(this IMarketDataProvider provider, Security security)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			if (security == null)
				throw new ArgumentNullException("security");

			var fields = provider.GetLevel1Fields(security);

			if (fields.IsEmpty())
				return null;

			return fields.ToDictionary(f => f, f => provider.GetSecurityValue(security, f));
		}

		/// <summary>
		/// Привести адаптер к типу <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Тип адаптера.</typeparam>
		/// <param name="adapter">Исходный адаптер.</param>
		/// <returns>Адаптер.</returns>
		public static T To<T>(this IMessageAdapter adapter)
			where T : class, IMessageAdapter
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var outAdapter = adapter as T;

			if (outAdapter != null)
				return outAdapter;

			var managedAdapter = adapter as ManagedMessageAdapter;

			if (managedAdapter != null)
				return managedAdapter.InnerAdapter.To<T>();

			throw new InvalidCastException(LocalizedStrings.Str3843.Put(adapter.GetType(), typeof(T)));
		}
	}
}