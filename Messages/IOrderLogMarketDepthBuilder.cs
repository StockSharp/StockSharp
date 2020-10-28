#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IOrderLogMarketDepthBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Base interface for order book builder.
	/// </summary>
	public interface IOrderLogMarketDepthBuilder
	{
		/// <summary>
		/// Snapshot.
		/// </summary>
		QuoteChangeMessage Snapshot { get; }

		/// <summary>
		/// Process order log item.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Market depth.</returns>
		QuoteChangeMessage Update(ExecutionMessage item);
	}

	/// <summary>
	/// Default implementation of <see cref="IOrderLogMarketDepthBuilder"/>.
	/// </summary>
	public class OrderLogMarketDepthBuilder : IOrderLogMarketDepthBuilder
	{
		private readonly Dictionary<long, decimal> _ordersByNum = new Dictionary<long, decimal>();
		private readonly Dictionary<string, decimal> _ordersByString = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SortedList<decimal, QuoteChange> _bids = new SortedList<decimal, QuoteChange>(new BackwardComparer<decimal>());
		private readonly SortedList<decimal, QuoteChange> _asks = new SortedList<decimal, QuoteChange>();

		private readonly QuoteChangeMessage _depth;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogMarketDepthBuilder"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		public OrderLogMarketDepthBuilder(SecurityId securityId)
			: this(new QuoteChangeMessage { SecurityId = securityId, BuildFrom = DataType.OrderLog })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogMarketDepthBuilder"/>.
		/// </summary>
		/// <param name="depth">Messages containing quotes.</param>
		public OrderLogMarketDepthBuilder(QuoteChangeMessage depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			if (!depth.IsSorted)
				throw new ArgumentException(LocalizedStrings.Str942, nameof(depth));

			_depth = depth;
			_depth.State = QuoteChangeStates.SnapshotComplete;

			foreach (var bid in depth.Bids)
				_bids.Add(bid.Price, bid);

			foreach (var ask in depth.Asks)
				_asks.Add(ask.Price, ask);

			_depth.Bids = _bids.Values.ToArray();
			_depth.Asks = _asks.Values.ToArray();
		}

		QuoteChangeMessage IOrderLogMarketDepthBuilder.Snapshot => _depth.TypedClone();

		QuoteChangeMessage IOrderLogMarketDepthBuilder.Update(ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (item.ExecutionType != ExecutionTypes.OrderLog)
				throw new ArgumentException(nameof(item));

			if (item.OrderPrice == 0)
				return null;

			_depth.ServerTime = item.ServerTime;
			_depth.LocalTime = item.LocalTime;

			QuoteChange? changedQuote = null;

			var quotes = item.Side == Sides.Buy ? _bids : _asks;

			if (item.IsOrderLogRegistered())
			{
				if (item.OrderVolume != null)
				{
					QuoteChange ProcessRegister<T>(T id, Dictionary<T, decimal> orders)
					{
						var quote = quotes.SafeAdd(item.OrderPrice, key => new QuoteChange(key, 0));

						var volume = item.OrderVolume.Value;

						if (orders.TryGetValue(id, out var prevVolume))
						{
							quote.Volume += (volume - prevVolume);
							orders[id] = volume;
						}
						else
						{
							quote.Volume += volume;
							orders.Add(id, volume);
						}

						quotes[item.OrderPrice] = quote;
						return quote;
					}
				
					if (item.OrderId != null)
						changedQuote = ProcessRegister(item.OrderId.Value, _ordersByNum);
					else if (!item.OrderStringId.IsEmpty())
						changedQuote = ProcessRegister(item.OrderStringId, _ordersByString);
				}
			}
			else if (item.IsOrderLogMatched())
			{
				var volume = item.TradeVolume ?? item.OrderVolume;

				if (volume != null)
				{
					QuoteChange? ProcessMatched<T>(T id, Dictionary<T, decimal> orders)
					{
						if (orders.TryGetValue(id, out var prevVolume))
						{
							orders[id] = prevVolume - volume.Value;
								
							if (quotes.TryGetValue(item.OrderPrice, out var quote))
							{
								quote.Volume -= volume.Value;

								if (quote.Volume <= 0)
									quotes.Remove(item.OrderPrice);

								quotes[item.OrderPrice] = quote;
								return quote;
							}
						}

						return null;
					}

					if (item.OrderId != null)
						changedQuote = ProcessMatched(item.OrderId.Value, _ordersByNum);
					else if (!item.OrderStringId.IsEmpty())
						changedQuote = ProcessMatched(item.OrderStringId, _ordersByString);
				}
			}
			else if (item.IsOrderLogCanceled())
			{
				QuoteChange? ProcessCanceled<T>(T id, Dictionary<T, decimal> orders)
				{
					if (orders.TryGetAndRemove(id, out var prevVolume))
					{
						if (quotes.TryGetValue(item.OrderPrice, out var quote))
						{
							quote.Volume -= prevVolume;

							if (quote.Volume <= 0)
								quotes.Remove(item.OrderPrice);

							quotes[item.OrderPrice] = quote;
							return quote;
						}
					}

					return null;
				}

				if (item.OrderId != null)
					changedQuote = ProcessCanceled(item.OrderId.Value, _ordersByNum);
				else if (!item.OrderStringId.IsEmpty())
					changedQuote = ProcessCanceled(item.OrderStringId, _ordersByString);
			}

			if (changedQuote == null)
				return null;

			var increment = new QuoteChangeMessage
			{
				ServerTime = item.ServerTime,
				LocalTime = item.LocalTime,
				SecurityId = _depth.SecurityId,
				State = QuoteChangeStates.Increment,
			};

			var q = changedQuote.Value;

			if (item.Side == Sides.Buy)
				increment.Bids = new[] { q };
			else
				increment.Asks = new[] { q };

			return increment;
		}
	}
}