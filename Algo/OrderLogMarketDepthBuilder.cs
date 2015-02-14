namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Построитель стакана из лога заявок.
	/// </summary>
	public class OrderLogMarketDepthBuilder
	{
		private DateTime? _lastUpdateTime;

		// TODO надо сделать это задаваемым
		private readonly TimeSpan _clearingBeginTime = new TimeSpan(18, 45, 01);

		//private readonly Dictionary<long, OrderLogItem> _trades = new Dictionary<long, OrderLogItem>();
		private ExecutionMessage _matchingOrder;
		private readonly int _maxDepth;

		private readonly SortedDictionary<decimal, QuoteChange> _bids = new SortedDictionary<decimal, QuoteChange>(new BackwardComparer<decimal>());
		private readonly SortedDictionary<decimal, QuoteChange> _asks = new SortedDictionary<decimal, QuoteChange>(new BackwardComparer<decimal>());

		private readonly ExchangeBoard _exchange;

		/// <summary>
		/// Создать <see cref="OrderLogMarketDepthBuilder"/>.
		/// </summary>
		/// <param name="depth">Стакан.</param>
		/// <param name="maxDepth">Максимальная глубина стакана. По-умолчанию равно <see cref="int.MaxValue"/>, что означает бесконечную глубину.</param>
		public OrderLogMarketDepthBuilder(QuoteChangeMessage depth, int maxDepth = int.MaxValue)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (!depth.IsSorted)
				throw new ArgumentException(LocalizedStrings.Str942, "depth");

			_depth = depth;
			_maxDepth = maxDepth;

			foreach (var bid in depth.Bids)
				_bids.Add(bid.Price, bid);

			foreach (var ask in depth.Asks)
				_asks.Add(ask.Price, ask);

			_exchange = depth.SecurityId.BoardCode.IsEmpty()
				? ExchangeBoard.Forts
				: ExchangeBoard.GetBoard(depth.SecurityId.BoardCode) ?? ExchangeBoard.Forts;
		}

		private readonly QuoteChangeMessage _depth;

		/// <summary>
		/// Стакан.
		/// </summary>
		public QuoteChangeMessage Depth
		{
			get { return _depth; }
		}

		/// <summary>
		/// Добавить новую строчку из лога заявок к стакану.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Был ли изменен стакан.</returns>
		public bool Update(ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			if (item.ExecutionType != ExecutionTypes.OrderLog)
				throw new ArgumentException("item");

			var changed = false;

			try
			{
				// Очистить стакан в вечерний клиринг
				if (item.ServerTime.TimeOfDay >= _clearingBeginTime)
				{
					// Garic - переделал
					// Очищаем только в рабочие дни поскольку в субботу/воскресенье допустима отмена заявок

					if (_lastUpdateTime != null && _lastUpdateTime.Value.TimeOfDay < _clearingBeginTime && _exchange.WorkingTime.IsTradeDate(item.ServerTime.LocalDateTime, true))
					{
						_depth.ServerTime = item.ServerTime;
						_depth.Bids = Enumerable.Empty<QuoteChange>();
						_depth.Asks = Enumerable.Empty<QuoteChange>();

						_matchingOrder = null;
						changed = true;
					}
				}

				_lastUpdateTime = item.ServerTime.LocalDateTime;

				if (!item.IsSystem || item.TradePrice != 0 || item.Price == 0 /* нулевая цена может появится при поставке опционов */)
					return changed;

				if (item.IsOrderLogRegistered())
				{
					changed = TryApplyTrades(null);

					if	(
							(item.Side == Sides.Buy && (_depth.Asks.IsEmpty() || item.Price < _depth.Asks.First().Price)) ||
							(item.Side == Sides.Sell && (_depth.Bids.IsEmpty() || item.Price > _depth.Bids.First().Price))
						)
					{
						if (item.TimeInForce == TimeInForce.PutInQueue)
						{
							var quotes = (item.Side == Sides.Buy ? _bids : _asks);
							var quote = quotes.TryGetValue(item.Price);

							if (quote == null)
							{
								quote = new QuoteChange
								{
									Side = item.Side,
									Price = item.Price,
									Volume = item.Volume,
								};

								quotes.Add(item.Price, quote);

								if (item.Side == Sides.Buy)
									_depth.Bids = GetArray(quotes);
								else
									_depth.Asks = GetArray(quotes);
							}
							else
								quote.Volume += item.Volume;

							changed = true;
						}
					}
					else
					{
						_matchingOrder = (ExecutionMessage)item.Clone();

						// mika
						// из-за того, что могут быть кросс-сделки, матчинг только по заявкам невозможен
						// (сначала идет регистрация вглубь стакана, затем отмена по причине кросс-сделки)
						// http://forum.rts.ru/viewtopic.asp?t=24197
						// 
					}
				}
				else if (item.IsOrderLogCanceled())
				{
					var isSame = _matchingOrder != null && _matchingOrder.OrderId == item.OrderId;

					changed = TryApplyTrades(item);

					if (!isSame && item.TimeInForce == TimeInForce.PutInQueue)
					{
						// http://forum.rts.ru/viewtopic.asp?t=24197
						if (item.GetOrderLogCancelReason() != OrderLogCancelReasons.CrossTrade)
						{
							var quotes = (item.Side == Sides.Buy ? _bids : _asks);
							var quote = quotes.TryGetValue(item.Price);

							if (quote != null)
							{
								quote.Volume -= item.Volume;

								if (quote.Volume <= 0)
								{
									quotes.Remove(item.Price);

									if (item.Side == Sides.Buy)
										_depth.Bids = GetArray(quotes);
									else
										_depth.Asks = GetArray(quotes);
								}
							}
						}

						changed = true;
					}
				}
				else
				{
					throw new ArgumentException(LocalizedStrings.Str943Params.Put(item), "item");

					// для одной сделки соответствуют две строчки в ОЛ
					//_trades[item.Trade.Id] = item;
				}
			}
			finally
			{
				if (changed)
					_depth.ServerTime = item.ServerTime;
			}

			return changed;
		}

		private bool TryApplyTrades(ExecutionMessage item)
		{
			if (_matchingOrder == null)
				return false;

			try
			{
				var volume = _matchingOrder.Volume;

				if (item != null && _matchingOrder.OrderId == item.OrderId)
					volume -= item.Volume;

				// если заявка была вся отменена. например, по причине http://forum.rts.ru/viewtopic.asp?t=24197
				if (volume == 0)
					return false;

				var removingQuotes = new List<Tuple<QuoteChange, decimal>>();

				foreach (var quote in (_matchingOrder.Side == Sides.Buy ? _depth.Asks : _depth.Bids))
				{
					if ((_matchingOrder.Side == Sides.Buy && _matchingOrder.Price < quote.Price) || (_matchingOrder.Side == Sides.Sell && _matchingOrder.Price > quote.Price))
						break;

					if (volume >= quote.Volume)
					{
						removingQuotes.Add(Tuple.Create(quote, quote.Volume));

						volume -= quote.Volume;

						if (volume == 0)
							break;
					}
					else
					{
						removingQuotes.Add(Tuple.Create(quote, volume));

						volume = 0;
						break;
					}
				}

				// в текущей момент Плаза не транслирует признак MatchOrCancel через ОЛ, поэтому сделано на будущее
				if (_matchingOrder.TimeInForce == TimeInForce.MatchOrCancel && volume > 0)
					return false;

				foreach (var removingQuote in removingQuotes)
				{
					var quotes = (_matchingOrder.Side.Invert() == Sides.Buy ? _bids : _asks);

					var quote = quotes.TryGetValue(removingQuote.Item1.Price);

					if (quote != null)
					{
						quote.Volume -= removingQuote.Item2;

						if (quote.Volume <= 0)
						{
							quotes.Remove(removingQuote.Item1.Price);
						}
					}
				}
				
				if (volume > 0)
				{
					if (_matchingOrder.TimeInForce == TimeInForce.PutInQueue)
					{
						var quote = new QuoteChange
						{
							Side = _matchingOrder.Side,
							Price = _matchingOrder.Price,
							Volume = volume,
						};

						(quote.Side == Sides.Buy ? _bids : _asks).Add(quote.Price, quote);
					}
				}

				_depth.Bids = GetArray(_bids);
				_depth.Asks = GetArray(_asks);

				return true;
			}
			finally
			{
				_matchingOrder = null;
			}
		}

		private IEnumerable<QuoteChange> GetArray(SortedDictionary<decimal, QuoteChange> quotes)
		{
			return (_maxDepth == int.MaxValue ? quotes.Values : quotes.Values.Take(_maxDepth)).ToArray();
		}
	}
}