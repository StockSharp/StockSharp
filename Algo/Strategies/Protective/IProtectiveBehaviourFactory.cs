namespace StockSharp.Algo.Strategies.Protective;

/// <summary>
/// <see cref="IProtectiveBehaviour"/> factory.
/// </summary>
public interface IProtectiveBehaviourFactory
{
	/// <summary>
	/// Create <see cref="IProtectiveBehaviour"/>
	/// </summary>
	/// <param name="takeValue">Take offset.</param>
	/// <param name="stopValue">Stop offset.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	/// <returns><see cref="IProtectiveBehaviour"/></returns>
	IProtectiveBehaviour Create(
		Unit takeValue,
		Unit stopValue,
		bool isStopTrailing,
		TimeSpan takeTimeout,
		TimeSpan stopTimeout,
		bool useMarketOrders);
}

/// <summary>
/// <see cref="IProtectiveBehaviourFactory"/> uses server stop-orders.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServerProtectiveBehaviourFactory"/>.
/// </remarks>
/// <param name="adapter"><see cref="IMessageAdapter"/> for creation server stop-orders.</param>
public class ServerProtectiveBehaviourFactory(IMessageAdapter adapter) : IProtectiveBehaviourFactory
{
	private class ServerProtectiveBehaviour : BaseProtectiveBehaviour
	{
		private readonly IMessageAdapter _adapter;

		public ServerProtectiveBehaviour(
			IMessageAdapter adapter,
			Unit takeValue, Unit stopValue,
			bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
			: base(takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders)
		{
			if (StopValue.IsSet() && !adapter.IsSupportStopLoss())
				throw new ArgumentException($"{nameof(IStopLossOrderCondition)} not supported.");

			if (TakeValue.IsSet() && !adapter.IsSupportTakeProfit())
				throw new ArgumentException($"{nameof(ITakeProfitOrderCondition)} not supported.");

			_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
		}

		public override decimal Position => 0;

		public override (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time)
			=> default;

		public override (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? Update(decimal price, decimal value, DateTimeOffset time)
		{
			var condition = _adapter.CreateOrderCondition() ?? throw new NotSupportedException();

			var protectiveSide = value > 0 ? Sides.Buy : Sides.Sell;
			var protectivePrice = price;

			if (TakeValue.IsSet() && condition is ITakeProfitOrderCondition take)
			{
				take.ActivationPrice = (decimal)(protectiveSide == Sides.Buy ? protectivePrice + TakeValue : protectivePrice - TakeValue);
				take.ClosePositionPrice = UseMarketOrders ? null : take.ActivationPrice;
			}

			if (StopValue.IsSet() && condition is IStopLossOrderCondition stop)
			{
				stop.IsTrailing = IsStopTrailing;
				stop.ActivationPrice = (decimal)(protectiveSide == Sides.Buy ? protectivePrice - StopValue : protectivePrice + StopValue);
				stop.ClosePositionPrice = UseMarketOrders ? null : stop.ActivationPrice;
			}

			return
			(
				condition is ITakeProfitOrderCondition, // TODO

				protectiveSide.Invert(),
				protectivePrice,
				value.Abs(),
				condition
			);
		}
	}

	private readonly IMessageAdapter _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

	IProtectiveBehaviour IProtectiveBehaviourFactory.Create(Unit takeValue, Unit stopValue, bool isStopTrailing, TimeSpan takeTimeout, TimeSpan stopTimeout, bool useMarketOrders)
		=> new ServerProtectiveBehaviour(_adapter, takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
}

/// <summary>
/// <see cref="IProtectiveBehaviourFactory"/> uses local (=emulated) stop-orders.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LocalProtectiveBehaviourFactory"/>.
/// </remarks>
/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
/// <param name="decimals"><see cref="SecurityMessage.Decimals"/></param>
public class LocalProtectiveBehaviourFactory(decimal? priceStep, int? decimals) : IProtectiveBehaviourFactory
{
	private class LocalProtectiveBehaviour : BaseProtectiveBehaviour
	{
		private ProtectiveProcessor _take;
		private ProtectiveProcessor _stop;

		private decimal _posValue;
		private decimal _posPrice;
		
		private decimal _totalVolume;
		private decimal _weightedPriceSum;
		private readonly LinkedList<(decimal price, decimal vol)> _trades = [];
		private readonly decimal? _priceStep;
		private readonly int? _decimals;

		public LocalProtectiveBehaviour(
			decimal? priceStep, int? decimals,
			Unit takeValue, Unit stopValue,
			bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
			: base(takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders)
		{
			_priceStep = priceStep;
			_decimals = decimals;

			if (isStopTrailing && stopValue.Type == UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.TrailingNotSupportLimitProtectiveLevel, nameof(stopValue));
		}

		public override decimal Position => _posValue;

		public override (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time)
		{
			if (_posValue == 0)
				return null;

			(bool, Sides, decimal, decimal, OrderCondition)? tryActivate(ProtectiveProcessor proc, bool isTake)
			{
				var activationPrice = proc?.GetActivationPrice(price, time);
				if (activationPrice is null)
					return null;

				return (
					isTake,
					_posValue > 0 ? Sides.Sell : Sides.Buy,
					activationPrice.Value,
					_totalVolume,
					null
				);
			}

			var info = tryActivate(_take, true) ?? tryActivate(_stop, false);

			if (info is not null)
				ResetProcessors();

			return info;
		}

		public override (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? Update(decimal price, decimal value, DateTimeOffset time)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

			if (value == default)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			var wasZero = _posValue == 0;
			var wasPositive = _posValue > 0;

			_posValue += value;

			ResetProcessors();

			if (_posValue == 0)
			{
				_trades.Clear();
				_posPrice = _totalVolume = _weightedPriceSum = default;
			}
			else
			{
				if (wasZero || wasPositive != (_posValue > 0))
				{
					var volume = _posValue.Abs();

					_totalVolume = volume;
					_weightedPriceSum = price * volume;

					_trades.Clear();
					_trades.AddLast((price, volume));

					_posPrice = price;
				}
				else
				{
					if (_trades.Count == 0)
						throw new InvalidOperationException();

					var isPosReduced = wasPositive == (value < 0);
					var volume = value.Abs();

					if (isPosReduced)
					{
						var left = volume;

						while (left > 0)
						{
							var firstNode = _trades.First ?? throw new InvalidOperationException("First node is null.");

							var (fp, fv) = firstNode.Value;
							var volumeToDeduct = left.Min(fv);

							_totalVolume -= volumeToDeduct;
							_weightedPriceSum -= fp * volumeToDeduct;

							if (fv <= volumeToDeduct)
								_trades.RemoveFirst();
							else
								firstNode.Value = new(fp, fv - volumeToDeduct);

							left -= volumeToDeduct;
						}
					}
					else
					{
						_totalVolume += volume;
						_weightedPriceSum += price * volume;

						_trades.AddLast((price, volume));
					}

					_posPrice = _weightedPriceSum / _totalVolume;

					if (_priceStep is not null)
						_posPrice = _posPrice.ShrinkPrice(_priceStep, _decimals);
				}

				var protectiveSide = _posValue > 0 ? Sides.Buy : Sides.Sell;

				_take = TakeValue.IsSet() ? new(protectiveSide, _posPrice, protectiveSide == Sides.Buy, false, TakeValue, UseMarketOrders, new(), TakeTimeout, time, this) : null;
				_stop = StopValue.IsSet() ? new(protectiveSide, _posPrice, protectiveSide == Sides.Sell, IsStopTrailing, StopValue, UseMarketOrders, new(), StopTimeout, time, this) : null;
			}

			return null;
		}

		private void ResetProcessors()
		{
			_take = _stop = default;
		}
	}

	IProtectiveBehaviour IProtectiveBehaviourFactory.Create(Unit takeValue, Unit stopValue, bool isStopTrailing, TimeSpan takeTimeout, TimeSpan stopTimeout, bool useMarketOrders)
		=> new LocalProtectiveBehaviour(priceStep, decimals, takeValue, stopValue, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
}