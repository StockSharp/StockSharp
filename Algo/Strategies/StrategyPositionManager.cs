namespace StockSharp.Algo.Strategies;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Ecng.Common;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Manages strategy positions (per <see cref="Security"/> + <see cref="Portfolio"/>) and calculates position quantity, average price, realized PnL and commission incrementally from order executions.
/// Additionally maintains cached per-position aggregates (blocked volume and active buy/sell orders count) via incremental updates (O(1) per order update, no rescans).
/// </summary>
/// <param name="strategyIdGetter">Delegate returning strategy identifier to stamp into newly created <see cref="Position.StrategyId"/>.</param>
public class StrategyPositionManager(Func<string> strategyIdGetter)
{
	/// <summary>
	/// Order processing result codes.
	/// </summary>
	public enum OrderResults
	{
		/// <summary>
		/// Processed successfully.
		/// </summary>
		OK,

		/// <summary>
		/// Ignored as state is invalid.
		/// </summary>
		InvalidStatus,

		/// <summary>
		/// No existing position for the order.
		/// </summary>
		UnknownOrder,

		/// <summary>
		/// Cumulative matched volume decreased.
		/// </summary>
		Inconsistent,

		/// <summary>
		/// Ignored market order execution due to missing last price.
		/// </summary>
		NoMarketPrice,
	}

	private readonly SyncObject _lock = new();
	private readonly Dictionary<(SecurityId secId, Portfolio pf), Position> _positions = [];
	private readonly Dictionary<long, OrderExecInfo> _orderExecInfos = [];
	private readonly Dictionary<SecurityId, decimal> _lastPrices = [];

	/// <summary>
	/// Per-position aggregates cache (blocked volume and active orders counters).
	/// </summary>
	private class PosAgg
	{
		public decimal BlockedBuy;   // sum of remaining balances for active buy orders
		public decimal BlockedSell;  // sum of remaining balances for active sell orders
		public int BuyCount;         // active buy orders count
		public int SellCount;        // active sell orders count
	}

	private readonly Dictionary<(SecurityId secId, Portfolio pf), PosAgg> _posAggs = [];

	/// <summary>
	/// Per-order tracking entry for incremental aggregate adjustments.
	/// </summary>
	private class OrderTrack
	{
		public decimal LastBalance; // last seen remaining balance
		public bool Active;         // was order considered active
	}

	private readonly Dictionary<long, OrderTrack> _orderTracks = [];

	/// <summary>
	/// Per-order execution info used to reconstruct slices for PnL and average price calculations.
	/// </summary>
	private class OrderExecInfo
	{
		public decimal MatchedVolume; // cumulative executed volume (absolute)
		public decimal Cost;          // cumulative cost (sum executed price * volume)
		public decimal Commission;    // cumulative commission
	}

	/// <summary>
	/// Delegate returning strategy id to assign into new positions.
	/// </summary>
	public Func<string> StrategyIdGetter { get; } = strategyIdGetter ?? throw new ArgumentNullException(nameof(strategyIdGetter));

	/// <summary>
	/// Occurs after position was processed (created or updated by an order execution or order state change affecting aggregates).
	/// </summary>
	public event Action<Position, bool> PositionProcessed;

	/// <summary>
	/// </summary>
	[Browsable(false)] public int TrackedOrderExecInfosCount { get { lock (_lock) return _orderExecInfos.Count; } }
	/// <summary>
	/// </summary>
	[Browsable(false)] public int TrackedOrderTracksCount { get { lock (_lock) return _orderTracks.Count; } }
	/// <summary>
	/// </summary>
	[Browsable(false)] public int TrackedAggsCount { get { lock (_lock) return _posAggs.Count; } }

	/// <summary>
	/// Try get existing position instance for <paramref name="security"/> and <paramref name="portfolio"/>.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Existing <see cref="Position"/> or <see langword="null"/>.</returns>
	public Position TryGetPosition(Security security, Portfolio portfolio)
	{
		ArgumentNullException.ThrowIfNull(security);
		ArgumentNullException.ThrowIfNull(portfolio);

		var key = (security.ToSecurityId(), portfolio);

		lock (_lock)
			return _positions.TryGetValue(key);
	}

	/// <summary>
	/// Set current position value explicitly (utility for manual restoration / overrides).
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="value">New signed quantity.</param>
	/// <param name="time">Timestamp to assign into <see cref="Position.LocalTime"/> and <see cref="Position.ServerTime"/> if position is created anew.</param>
	public void SetPosition(Security security, Portfolio portfolio, decimal value, DateTimeOffset time)
	{
		lock (_lock)
			GetOrCreate(security, portfolio, time, out _).CurrentValue = value;
	}

	private Position GetOrCreate(Security security, Portfolio portfolio, DateTimeOffset time, out bool isNew)
	{
		ArgumentNullException.ThrowIfNull(security);
		ArgumentNullException.ThrowIfNull(portfolio);

		return _positions.SafeAdd((security.ToSecurityId(), portfolio), _ => new()
		{
			Security = security,
			Portfolio = portfolio,
			StrategyId = StrategyIdGetter?.Invoke(),
			// initialize calculated fields to non-null defaults
			CurrentValue = 0m,
			RealizedPnL = 0m,
			Commission = 0m,
			BlockedValue = 0m,
			BuyOrdersCount = 0,
			SellOrdersCount = 0,
			AveragePrice = 0m,
			// timestamps
			LocalTime = time,
			ServerTime = time,
		}, out isNew);
	}

	/// <summary>
	/// Snapshot array of managed positions (thread-safe copy).
	/// </summary>
	[Browsable(false)]
	public Position[] Positions
	{
		get { lock (_lock) return [.. _positions.Values]; }
	}

	/// <summary>
	/// Reset all internal caches (positions, execution info, aggregates, order tracks).
	/// </summary>
	public void Reset()
	{
		lock (_lock)
		{
			_positions.Clear();
			_orderExecInfos.Clear();
			_posAggs.Clear();
			_orderTracks.Clear();
			_lastPrices.Clear();
		}
	}

	/// <summary>
	/// Incrementally update aggregates for the order (blocked volume and counts) and push them into the <paramref name="position"/>.
	/// </summary>
	private void UpdateAggregates(Order order, Position position)
	{
		ArgumentNullException.ThrowIfNull(order);
		ArgumentNullException.ThrowIfNull(position);

		var txId = order.TransactionId;

		var key = (order.Security.ToSecurityId(), order.Portfolio);
		var agg = _posAggs.SafeAdd(key, _ => new());

		var balance = order.Balance; // non-nullable balance
		var newActive = order.State == OrderStates.Active && balance > 0;

		if (!_orderTracks.TryGetValue(txId, out var track))
		{
			if (newActive)
			{
				track = new() { LastBalance = balance, Active = true };
				_orderTracks[txId] = track;

				if (order.Side == Sides.Buy)
				{
					agg.BlockedBuy += balance;
					agg.BuyCount++;
				}
				else
				{
					agg.BlockedSell += balance;
					agg.SellCount++;
				}
			}
		}
		else
		{
			if (track.Active && newActive)
			{
				var diff = balance - track.LastBalance;

				if (diff != 0)
				{
					if (order.Side == Sides.Buy)
						agg.BlockedBuy += diff;
					else
						agg.BlockedSell += diff;

					track.LastBalance = balance;
				}
			}
			else if (track.Active && !newActive)
			{
				if (order.Side == Sides.Buy)
				{
					agg.BlockedBuy -= track.LastBalance;
					agg.BuyCount--;
				}
				else
				{
					agg.BlockedSell -= track.LastBalance;
					agg.SellCount--;
				}

				_orderTracks.Remove(txId);
			}
			else if (!track.Active && newActive)
			{
				track.Active = true;
				track.LastBalance = balance;

				if (order.Side == Sides.Buy)
				{
					agg.BlockedBuy += balance;
					agg.BuyCount++;
				}
				else
				{
					agg.BlockedSell += balance;
					agg.SellCount++;
				}
			}
		}

		// do not net active orders across sides; show total blocked on both sides
		var blockedTotal = agg.BlockedBuy + agg.BlockedSell;
		position.BlockedValue = blockedTotal;
		position.BuyOrdersCount = agg.BuyCount;
		position.SellOrdersCount = agg.SellCount;

		// cleanup empty aggregates to avoid leaking keys
		if (agg.BuyCount == 0 && agg.SellCount == 0 && agg.BlockedBuy == 0 && agg.BlockedSell == 0)
			_posAggs.Remove(key);
	}

	/// <summary>
	/// Process order state change (registration, balance change, partial/full execution, cancellation, done).
	/// </summary>
	/// <param name="order">Order to process.</param>
	/// <returns><see cref="OrderResults"/></returns>
	public OrderResults ProcessOrder(Order order)
	{
		ArgumentNullException.ThrowIfNull(order);

		var txId = order.TransactionId;

		if (txId <= 0)
			throw new ArgumentException(LocalizedStrings.TransactionInvalid, nameof(order));

		if (order.Balance < 0)
			throw new ArgumentException(LocalizedStrings.OrderBalanceNotEnough.Put(order.TransactionId, order.Balance), nameof(order));

		if (order.Volume <= 0)
			throw new ArgumentException(LocalizedStrings.WrongOrderVolume.Put(order.TransactionId), nameof(order));

		if (order.State == OrderStates.Active && order.Balance == 0)
			throw new ArgumentException("Active order cannot have zero balance.", nameof(order));

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Failed)
			return OrderResults.InvalidStatus; // not yet active

		var matchedAbs = order.GetMatchedVolume() ?? 0m; // cumulative executed volume (absolute)
		var signSide = order.Side == Sides.Sell ? -1 : 1;

		Position position;
		bool isNew;

		var commission = order.Commission;

		lock (_lock)
		{
			var key = (order.Security.ToSecurityId(), order.Portfolio);
			var posExists = _positions.ContainsKey(key);

			// If there are no executions yet and the order state is not Active and no position exists yet, ignore.
			if (matchedAbs == 0 && order.State != OrderStates.Active && !posExists)
				return OrderResults.UnknownOrder;

			position = GetOrCreate(order.Security, order.Portfolio, order.ServerTime, out isNew);

			// ensure aggregates reflect current order state before fill handling (to close blocked on Done, etc.)
			UpdateAggregates(order, position);

			if (!_orderExecInfos.TryGetValue(txId, out var execInfo))
				_orderExecInfos[txId] = execInfo = new();

			var deltaMatchedAbs = matchedAbs - execInfo.MatchedVolume;
			if (deltaMatchedAbs < 0)
				return OrderResults.Inconsistent; // inconsistent snapshot delivered out-of-order

			// compute commission delta against last seen cumulative commission for this order
			var deltaCommission = (commission ?? 0m) - execInfo.Commission;

			if (deltaMatchedAbs > 0)
			{
				// determine execution price according to priority:
				// 1) AveragePrice
				// 2) Price (for non-market orders)
				// 3) Last instrument price for market orders
				// If still unknown -> ignore this execution and log a warning
				var effPrice = order.AveragePrice;
				var cumulativeBased = true; // when true, use cumulative cost; otherwise use incremental cost

				if (effPrice == null)
				{
					if (order.Type == OrderTypes.Market)
					{
						if (!_lastPrices.TryGetValue(order.Security.ToSecurityId(), out var lastPrice))
							return OrderResults.NoMarketPrice;

						effPrice = lastPrice;
						cumulativeBased = false; // last price may change between snapshots; use incremental cost to avoid skew
					}
					else
					{
						effPrice = order.Price;
						cumulativeBased = true;
					}
				}

				// reconstruct slice
				decimal deltaCost;
				if (cumulativeBased)
				{
					var currentTotalCost = effPrice.Value * matchedAbs;
					deltaCost = currentTotalCost - execInfo.Cost;
					execInfo.Cost = currentTotalCost;
				}
				else
				{
					deltaCost = effPrice.Value * deltaMatchedAbs;
					execInfo.Cost += deltaCost;
				}

				execInfo.MatchedVolume = matchedAbs;
				execInfo.Commission += deltaCommission;

				var posQty = position.CurrentValue ?? 0m;
				var posAvg = position.AveragePrice;
				var posRealized = position.RealizedPnL ?? 0m;
				var posCommission = position.Commission ?? 0m;

				var deltaQtySigned = signSide * deltaMatchedAbs;
				var slicePrice = deltaCost / deltaMatchedAbs;
				var newQty = posQty + deltaQtySigned;
				var sameDirection = posQty == 0 || posQty.Sign() == deltaQtySigned.Sign();

				if (sameDirection)
				{
					var posQtyAbs = posQty.Abs();
					var newQtyAbs = newQty.Abs();
					var sliceCost = slicePrice * deltaMatchedAbs;
					var totalCost = (posAvg ?? slicePrice) * posQtyAbs + sliceCost;
					posAvg = totalCost / newQtyAbs;
				}
				else
				{
					var closingVolume = posQty.Abs().Min(deltaMatchedAbs);
					posRealized += (slicePrice - (posAvg ?? slicePrice)) * closingVolume * posQty.Sign();
					if (newQty == 0)
						posAvg = 0m;            // fully flat -> keep non-null average price baseline
					else if (newQty.Sign() != posQty.Sign())
						posAvg = slicePrice;      // reversal opens new basis
				}

				position.CurrentValue = newQty;
				position.AveragePrice = posAvg;
				position.RealizedPnL = posRealized;

				position.Commission = posCommission + deltaCommission;

				// Recompute position market value if we know last price for this security
				if (_lastPrices.TryGetValue(order.Security.ToSecurityId(), out var lastPrice2))
				{
					var value = newQty.Abs() * lastPrice2;
					position.CurrentPrice = value;
				}
				else
				{
					position.CurrentPrice = null; // unknown market price
				}

				position.LocalTime = order.LocalTime;
				position.ServerTime = order.ServerTime;
			}
			else
			{
				// no new executions; still apply commission correction if provider adjusted cumulative commission
				if (deltaCommission != 0)
				{
					execInfo.Commission += deltaCommission;
					var posCommission = position.Commission ?? 0m;
					position.Commission = posCommission + deltaCommission;
					position.LocalTime = order.LocalTime;
					position.ServerTime = order.ServerTime;
				}
			}

			// update aggregates again in case balance changed after fill
			UpdateAggregates(order, position);

			// cleanup per-order exec info on terminal state to avoid leaks (Done covers both full fill and cancellation)
			if (order.State == OrderStates.Done)
				_orderExecInfos.Remove(txId);
		}

		PositionProcessed?.Invoke(position, isNew);
		return OrderResults.OK;
	}

	/// <summary>
	/// Update current (market) price for all positions of the specified <paramref name="secId"/>.
	/// </summary>
	/// <param name="secId">Security identifier whose positions need price update.</param>
	/// <param name="price">New market price.</param>
	/// <param name="serverTime">Server time of the price snapshot.</param>
	/// <param name="localTime">Local time when the price was processed.</param>
	public void UpdateCurrentPrice(SecurityId secId, decimal price, DateTimeOffset serverTime, DateTimeOffset localTime)
	{
		if (price < 0)
			throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

		List<Position> changed = null;

		lock (_lock)
		{
			_lastPrices[secId] = price;

			foreach (var ((kSecId, _), pos) in _positions)
			{
				if (!kSecId.Equals(secId))
					continue;

				var qty = pos.CurrentValue ?? 0m;
				pos.CurrentPrice = qty == 0 ? 0m : qty.Abs() * price;
				pos.ServerTime = serverTime;
				pos.LocalTime = localTime;
				(changed ??= []).Add(pos);
			}
		}

		if (changed != null)
		{
			foreach (var p in changed)
				PositionProcessed?.Invoke(p, false);
		}
	}
}
