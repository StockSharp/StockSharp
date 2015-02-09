namespace StockSharp.Algo
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Причины отмены заявок в логе заявок.
	/// </summary>
	public enum OrderLogCancelReasons
	{
		/// <summary>
		/// Перерегистрация заявки.
		/// </summary>
		ReRegistered,

		/// <summary>
		/// Отмена заявки.
		/// </summary>
		Canceled,

		/// <summary>
		/// Групповая отмена заявок.
		/// </summary>
		GroupCanceled,

		/// <summary>
		/// Признак удаления остатка заявки по причине кросс-сделки.
		/// </summary>
		CrossTrade,
	}

	/// <summary>
	/// Построение стакана по логу заявок.
	/// </summary>
	public static class OrderLogHelper
	{
		/// <summary>
		/// Проверить, содержит ли строчка регистрацию заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит регистрацию заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsOrderLogRegistered(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			return item.OrderState == OrderStates.Active && item.TradePrice == 0;
		}

		/// <summary>
		/// Проверить, содержит ли строчка регистрацию заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит регистрацию заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsRegistered(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogRegistered();
		}

		/// <summary>
		/// Проверить, содержит ли строчка отменену заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит отменену заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsOrderLogCanceled(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			return item.OrderState == OrderStates.Done && item.TradePrice == 0;
		}

		/// <summary>
		/// Проверить, содержит ли строчка отменену заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит отменену заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsCanceled(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogCanceled();
		}

		/// <summary>
		/// Проверить, содержит ли строчка исполнение заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит исполнение заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsOrderLogMatched(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			return item.TradeId != 0;
		}

		/// <summary>
		/// Проверить, содержит ли строчка исполнение заявки.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns><see langword="true"/>, если строчка содержит исполнение заявки, иначе, <see langword="false"/>.</returns>
		public static bool IsMatched(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogMatched();
		}

		/// <summary>
		/// Получить причину отмены заявки в логе заявок.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Причина отмены заявки в логе заявок.</returns>
		public static OrderLogCancelReasons GetOrderLogCancelReason(this ExecutionMessage item)
		{
			if (!item.IsOrderLogCanceled())
				throw new ArgumentException(LocalizedStrings.Str937, "item");

			if (item.OrderStatus == null)
				throw new ArgumentException(LocalizedStrings.Str938, "item");

			var status = (int)item.OrderStatus;

			if (status.HasBits(0x100000))
				return OrderLogCancelReasons.ReRegistered;
			else if (status.HasBits(0x200000))
				return OrderLogCancelReasons.Canceled;
			else if (status.HasBits(0x400000))
				return OrderLogCancelReasons.GroupCanceled;
			else if (status.HasBits(0x800000))
				return OrderLogCancelReasons.CrossTrade;
			else
				throw new ArgumentOutOfRangeException("item", status, LocalizedStrings.Str939);
		}

		/// <summary>
		/// Получить причину отмены заявки в логе заявок.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Причина отмены заявки в логе заявок.</returns>
		public static OrderLogCancelReasons GetCancelReason(this OrderLogItem item)
		{
			return item.ToMessage().GetOrderLogCancelReason();
		}

		private sealed class DepthEnumerable : SimpleEnumerable<QuoteChangeMessage>, IEnumerableEx<QuoteChangeMessage>
		{
			private sealed class DepthEnumerator : IEnumerator<QuoteChangeMessage>
			{
				private readonly TimeSpan _interval;
				private readonly int _maxDepth;
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;
				private OrderLogMarketDepthBuilder _builder;

				public DepthEnumerator(IEnumerable<ExecutionMessage> items, TimeSpan interval, int maxDepth)
				{
					if (items == null)
						throw new ArgumentNullException("items");

					_itemsEnumerator = items.GetEnumerator();
					_interval = interval;
					_maxDepth = maxDepth;
				}

				public QuoteChangeMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var item = _itemsEnumerator.Current;

						if (_builder == null)
							_builder = new OrderLogMarketDepthBuilder(new QuoteChangeMessage { SecurityId = item.SecurityId, IsSorted = true }, _maxDepth);

						if (!_builder.Update(item))
							continue;

						if (Current != null && (_builder.Depth.ServerTime - Current.ServerTime) < _interval)
							continue;

						Current = (QuoteChangeMessage)_builder.Depth.Clone();
						//Current.MaxDepth = _maxDepth;
						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_itemsEnumerator.Reset();
					_builder = null;
					Current = null;
				}

				object IEnumerator.Current
				{
					get { return Current; }
				}

				void IDisposable.Dispose()
				{
					Reset();
					_itemsEnumerator.Dispose();
				}
			}

			private readonly IEnumerableEx<ExecutionMessage> _items;

			public DepthEnumerable(IEnumerableEx<ExecutionMessage> items, TimeSpan interval, int maxDepth)
				: base(() => new DepthEnumerator(items, interval, maxDepth))
			{
				if (items == null)
					throw new ArgumentNullException("items");

				if (interval < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("interval", interval, LocalizedStrings.Str940);

				if (maxDepth < 1)
					throw new ArgumentOutOfRangeException("maxDepth", maxDepth, LocalizedStrings.Str941);

				_items = items;
			}

			int IEnumerableEx.Count
			{
				get { return _items.Count; }
			}
		}

		/// <summary>
		/// Построить стаканы из лога заявок.
		/// </summary>
		/// <param name="items">Строчки лога заявок.</param>
		/// <param name="interval">Интервал генерации стакана. По-умолчанаю равен <see cref="TimeSpan.Zero"/>, что означает генерацию стаканов при каждой новой строчке лога заявок.</param>
		/// <param name="maxDepth">Максимальная глубина стакана. По-умолчанию равно <see cref="int.MaxValue"/>, что означает бесконечную глубину.</param>
		/// <returns>Стаканы.</returns>
		public static IEnumerableEx<MarketDepth> ToMarketDepths(this IEnumerableEx<OrderLogItem> items, TimeSpan interval = default(TimeSpan), int maxDepth = int.MaxValue)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<MarketDepth>().ToEx();

			return items
				.ToMessages<OrderLogItem, ExecutionMessage>()
				.ToMarketDepths(interval, maxDepth)
				.ToEntities<QuoteChangeMessage, MarketDepth>(first.Order.Security);
		}

		/// <summary>
		/// Построить стаканы из лога заявок.
		/// </summary>
		/// <param name="items">Строчки лога заявок.</param>
		/// <param name="interval">Интервал генерации стакана. По-умолчанаю равен <see cref="TimeSpan.Zero"/>, что означает генерацию стаканов при каждой новой строчке лога заявок.</param>
		/// <param name="maxDepth">Максимальная глубина стакана. По-умолчанию равно <see cref="int.MaxValue"/>, что означает бесконечную глубину.</param>
		/// <returns>Стаканы.</returns>
		public static IEnumerableEx<QuoteChangeMessage> ToMarketDepths(this IEnumerableEx<ExecutionMessage> items, TimeSpan interval = default(TimeSpan), int maxDepth = int.MaxValue)
		{
			return new DepthEnumerable(items, interval, maxDepth);
		}

		private sealed class OrderLogTickEnumerable : SimpleEnumerable<ExecutionMessage>, IEnumerableEx<ExecutionMessage>
		{
			private sealed class OrderLogTickEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;
				private readonly Dictionary<long, Tuple<long, Sides>> _trades = new Dictionary<long, Tuple<long, Sides>>();

				public OrderLogTickEnumerator(IEnumerable<ExecutionMessage> items)
				{
					if (items == null)
						throw new ArgumentNullException("items");

					_itemsEnumerator = items.GetEnumerator();
				}

				public ExecutionMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var currItem = _itemsEnumerator.Current;

						var tradeId = currItem.TradeId;

						if (tradeId == 0)
							continue;

						var prevItem = _trades.TryGetValue(tradeId);

						if (prevItem == null)
						{
							_trades.Add(tradeId, Tuple.Create(currItem.OrderId, currItem.Side));
						}
						else
						{
							_trades.Remove(tradeId);

							Current = new ExecutionMessage
							{
								ExecutionType = ExecutionTypes.Tick,
								SecurityId = currItem.SecurityId,
								TradeId = tradeId,
								TradePrice = currItem.TradePrice,
								TradeStatus = currItem.TradeStatus,
								Volume = currItem.Volume,
								ServerTime = currItem.ServerTime,
								LocalTime = currItem.LocalTime,
								OpenInterest = currItem.OpenInterest,
								OriginSide = prevItem.Item2 == Sides.Buy
									? (prevItem.Item1 > currItem.OrderId ? Sides.Buy : Sides.Sell)
									: (prevItem.Item1 > currItem.OrderId ? Sides.Sell : Sides.Buy),
							};

							return true;
						}
					}

					Current = null;
					return false;
				}

				void IEnumerator.Reset()
				{
					_itemsEnumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current
				{
					get { return Current; }
				}

				void IDisposable.Dispose()
				{
					_itemsEnumerator.Dispose();
				}
			}

			private readonly IEnumerableEx<ExecutionMessage> _items;

			public OrderLogTickEnumerable(IEnumerableEx<ExecutionMessage> items)
				: base(() => new OrderLogTickEnumerator(items))
			{
				if (items == null)
					throw new ArgumentNullException("items");

				_items = items;
			}

			int IEnumerableEx.Count
			{
				get { return _items.Count; }
			}
		}

		/// <summary>
		/// Построить тиковые сделки из лога заявок.
		/// </summary>
		/// <param name="items">Строчки лога заявок.</param>
		/// <returns>Тиковые сделки.</returns>
		public static IEnumerableEx<Trade> ToTrades(this IEnumerableEx<OrderLogItem> items)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<Trade>().ToEx(0);

			var ticks = items
				.Select(i => i.ToMessage())
				.ToEx(items.Count)
				.ToTicks();

			return ticks.Select(m => m.ToTrade(first.Order.Security)).ToEx(ticks.Count);
		}

		/// <summary>
		/// Построить тиковые сделки из лога заявок.
		/// </summary>
		/// <param name="items">Строчки лога заявок.</param>
		/// <returns>Тиковые сделки.</returns>
		public static IEnumerableEx<ExecutionMessage> ToTicks(this IEnumerableEx<ExecutionMessage> items)
		{
			return new OrderLogTickEnumerable(items);
		}
	}
}