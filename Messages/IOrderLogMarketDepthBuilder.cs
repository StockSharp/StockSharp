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
		private readonly Dictionary<long, decimal> _orders = new Dictionary<long, decimal>();

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
				if (item.OrderId != null)
				{
					var quote = quotes.SafeAdd(item.OrderPrice, key => new QuoteChange(key, 0));
					var id = item.OrderId.Value;

					if (item.OrderVolume != null)
					{
						var volume = item.OrderVolume.Value;

						if (_orders.TryGetValue(id, out var prevVolume))
						{
							quote.Volume += (volume - prevVolume);
							_orders[id] = volume;
						}
						else
						{
							quote.Volume += volume;
							_orders.Add(id, volume);
						}

						quotes[item.OrderPrice] = quote;
						changedQuote = quote;
					}
				}
			}
			else if (item.IsOrderLogMatched())
			{
				if (item.OrderId != null)
				{
					var id = item.OrderId.Value;
					var volume = item.TradeVolume ?? item.OrderVolume;

					if (volume != null)
					{
						if (_orders.TryGetValue(id, out var prevVolume))
						{
							_orders[id] = prevVolume - volume.Value;
								
							if (quotes.TryGetValue(item.OrderPrice, out var quote))
							{
								quote.Volume -= volume.Value;

								if (quote.Volume <= 0)
									quotes.Remove(item.OrderPrice);

								quotes[item.OrderPrice] = quote;
								changedQuote = quote;
							}
						}
					}
				}
			}
			else if (item.IsOrderLogCanceled())
			{
				if (item.OrderId != null)
				{
					var id = item.OrderId.Value;

					if (_orders.TryGetAndRemove(id, out var prevVolume))
					{
						if (quotes.TryGetValue(item.OrderPrice, out var quote))
						{
							quote.Volume -= prevVolume;

							if (quote.Volume <= 0)
								quotes.Remove(item.OrderPrice);

							quotes[item.OrderPrice] = quote;
							changedQuote = quote;
						}
					}
				}
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