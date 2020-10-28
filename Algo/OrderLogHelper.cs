#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: OrderLogHelper.cs
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

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Storages;

	/// <summary>
	/// Reasons for orders cancelling in the orders log.
	/// </summary>
	public enum OrderLogCancelReasons
	{
		/// <summary>
		/// The order re-registration.
		/// </summary>
		ReRegistered,

		/// <summary>
		/// Cancel order.
		/// </summary>
		Canceled,

		/// <summary>
		/// Group canceling of orders.
		/// </summary>
		GroupCanceled,

		/// <summary>
		/// The sign of deletion of order residual due to cross-trade.
		/// </summary>
		CrossTrade,
	}

	/// <summary>
	/// Building order book by the orders log.
	/// </summary>
	public static class OrderLogHelper
	{
		/// <summary>
		/// To check, does the order log contain the order registration.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the order log contains the order registration, otherwise, <see langword="false" />.</returns>
		public static bool IsRegistered(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogRegistered();
		}

		/// <summary>
		/// To check, does the order log contain the cancelled order.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the order log contain the cancelled order, otherwise, <see langword="false" />.</returns>
		public static bool IsCanceled(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogCanceled();
		}

		/// <summary>
		/// To check, does the order log contain the order matching.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the order log contains order matching, otherwise, <see langword="false" />.</returns>
		public static bool IsMatched(this OrderLogItem item)
		{
			return item.ToMessage().IsOrderLogMatched();
		}

		/// <summary>
		/// To get the reason for cancelling order in orders log.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>The reason for order cancelling in order log.</returns>
		public static OrderLogCancelReasons GetOrderLogCancelReason(this ExecutionMessage item)
		{
			if (!item.IsOrderLogCanceled())
				throw new ArgumentException(LocalizedStrings.Str937, nameof(item));

			if (item.OrderStatus == null)
				throw new ArgumentException(LocalizedStrings.Str938, nameof(item));

			var status = item.OrderStatus.Value;

			if (status.HasBits(0x100000))
				return OrderLogCancelReasons.ReRegistered;
			else if (status.HasBits(0x200000))
				return OrderLogCancelReasons.Canceled;
			else if (status.HasBits(0x400000))
				return OrderLogCancelReasons.GroupCanceled;
			else if (status.HasBits(0x800000))
				return OrderLogCancelReasons.CrossTrade;
			else
				throw new ArgumentOutOfRangeException(nameof(item), status, LocalizedStrings.Str939);
		}

		/// <summary>
		/// To get the reason for cancelling order in orders log.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>The reason for order cancelling in order log.</returns>
		public static OrderLogCancelReasons GetCancelReason(this OrderLogItem item)
		{
			return item.ToMessage().GetOrderLogCancelReason();
		}

		/// <summary>
		/// Build market depths from order log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <param name="builder">Order log to market depth builder.</param>
		/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
		/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerable<MarketDepth> ToOrderBooks(this IEnumerable<OrderLogItem> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<MarketDepth>();

			return items.ToMessages<OrderLogItem, ExecutionMessage>()
				.ToOrderBooks(builder, interval)
				.BuildIfNeed()
				.ToEntities<QuoteChangeMessage, MarketDepth>(first.Order.Security);
		}

		/// <summary>
		/// Build market depths from order log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <param name="builder">Order log to market depth builder.</param>
		/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
		/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
		{
			var snapshotSent = false;
			var prevTime = default(DateTimeOffset?);

			foreach (var item in items)
			{
				if (!snapshotSent)
				{
					yield return builder.Snapshot.TypedClone();
					snapshotSent = true;
				}

				var depth = builder.Update(item);
				if (depth is null)
					continue;

				if (prevTime != null && (depth.ServerTime - prevTime.Value) < interval)
					continue;

				depth = depth.TypedClone();

				if (maxDepth < int.MaxValue)
				{
					depth.Bids = depth.Bids.Take(maxDepth).ToArray();
					depth.Asks = depth.Asks.Take(maxDepth).ToArray();
				}

				yield return depth;

				prevTime = depth.ServerTime;
			}
		}

		private sealed class OrderLogTickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
		{
			private sealed class OrderLogTickEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

				private readonly HashSet<long> _tradesByNum = new HashSet<long>();
				private readonly HashSet<string> _tradesByString = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

				public OrderLogTickEnumerator(IEnumerable<ExecutionMessage> items)
				{
					if (items == null)
						throw new ArgumentNullException(nameof(items));

					_itemsEnumerator = items.GetEnumerator();
				}

				public ExecutionMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var currItem = _itemsEnumerator.Current;

						if (currItem.TradeId != null)
						{
							if (TryProcess(currItem.TradeId.Value, _tradesByNum, currItem))
								return true;
						}
						else if (!currItem.TradeStringId.IsEmpty())
						{
							if (TryProcess(currItem.TradeStringId, _tradesByString, currItem))
								return true;
						}
					}

					Current = null;
					return false;
				}

				private bool TryProcess<T>(T tradeId, HashSet<T> trades, ExecutionMessage currItem)
				{
					if (!trades.Add(tradeId))
						return false;

					trades.Remove(tradeId);
					Current = currItem.ToTick();
					return true;
				}

				void IEnumerator.Reset()
				{
					_itemsEnumerator.Reset();
					
					_tradesByNum.Clear();
					_tradesByString.Clear();

					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_itemsEnumerator.Dispose();
				}
			}

			//private readonly IEnumerable<ExecutionMessage> _items;

			public OrderLogTickEnumerable(IEnumerable<ExecutionMessage> items)
				: base(() => new OrderLogTickEnumerator(items))
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				//_items = items;
			}

			//int IEnumerableEx.Count => _items.Count;
		}

		/// <summary>
		/// To build tick trades from the orders log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerable<Trade> ToTrades(this IEnumerable<OrderLogItem> items)
		{
			var first = items.FirstOrDefault();

			if (first == null)
				return Enumerable.Empty<Trade>();

			var ticks = items
				.Select(i => i.ToMessage())
				.ToTicks();

			return ticks.Select(m => m.ToTrade(first.Order.Security));
		}

		/// <summary>
		/// To tick trade from the order log.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Tick trade.</returns>
		public static ExecutionMessage ToTick(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (item.ExecutionType != ExecutionTypes.OrderLog)
				throw new ArgumentException(nameof(item));

			return new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = item.SecurityId,
				TradeId = item.TradeId,
				TradeStringId = item.TradeStringId,
				TradePrice = item.TradePrice,
				TradeStatus = item.TradeStatus,
				TradeVolume = item.OrderVolume,
				ServerTime = item.ServerTime,
				LocalTime = item.LocalTime,
				IsSystem = item.IsSystem,
				OpenInterest = item.OpenInterest,
				OriginSide = item.OriginSide,
				//OriginSide = prevItem.Item2 == Sides.Buy
				//	? (prevItem.Item1 > item.OrderId ? Sides.Buy : Sides.Sell)
				//	: (prevItem.Item1 > item.OrderId ? Sides.Sell : Sides.Buy),
				BuildFrom = DataType.OrderLog,
			};
		}

		/// <summary>
		/// To build tick trades from the orders log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<ExecutionMessage> items)
		{
			return new OrderLogTickEnumerable(items);
		}

		private sealed class TickLevel1Enumerable : SimpleEnumerable<Level1ChangeMessage>
		{
			private sealed class TickLevel1Enumerator : IEnumerator<Level1ChangeMessage>
			{
				private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

				public TickLevel1Enumerator(IEnumerable<ExecutionMessage> items)
				{
					if (items is null)
						throw new ArgumentNullException(nameof(items));

					_itemsEnumerator = items.GetEnumerator();
				}

				public Level1ChangeMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_itemsEnumerator.MoveNext())
					{
						var tick = _itemsEnumerator.Current;

						var l1Msg = new Level1ChangeMessage
						{
							SecurityId = tick.SecurityId,
							ServerTime = tick.ServerTime,
							LocalTime = tick.LocalTime,
						}
						.TryAdd(Level1Fields.LastTradeId, tick.TradeId)
						.TryAdd(Level1Fields.LastTradeStringId, tick.TradeStringId)
						.TryAdd(Level1Fields.LastTradePrice, tick.TradePrice)
						.TryAdd(Level1Fields.LastTradeVolume, tick.TradeVolume)
						.TryAdd(Level1Fields.LastTradeUpDown, tick.IsUpTick)
						.TryAdd(Level1Fields.LastTradeOrigin, tick.OriginSide)
						;

						if (l1Msg.Changes.Count == 0)
							continue;

						Current = l1Msg;
						return true;
					}

					Current = null;
					return false;
				}

				void IEnumerator.Reset()
				{
					_itemsEnumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_itemsEnumerator.Dispose();
				}
			}

			public TickLevel1Enumerable(IEnumerable<ExecutionMessage> items)
				: base(() => new TickLevel1Enumerator(items))
			{
				if (items is null)
					throw new ArgumentNullException(nameof(items));
			}
		}

		/// <summary>
		/// To build level1 from the orders log.
		/// </summary>
		/// <param name="items">Orders log lines.</param>
		/// <param name="builder">Order log to market depth builder.</param>
		/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
		/// <returns>Tick trades.</returns>
		public static IEnumerable<Level1ChangeMessage> ToLevel1(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default)
		{
			if (builder == null)
				return new TickLevel1Enumerable(items);
			else
				return items.ToOrderBooks(builder, interval, 1).BuildIfNeed().ToLevel1();
		}
	}
}