namespace StockSharp.Algo.Strategies.Protective;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Localization;
using StockSharp.Messages;

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
	/// <param name="isTakeTrailing">Whether to use a trailing technique.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	/// <returns><see cref="IProtectiveBehaviour"/></returns>
	IProtectiveBehaviour Create(
		Unit takeValue, Unit stopValue,
		bool isTakeTrailing, bool isStopTrailing,
		TimeSpan takeTimeout, TimeSpan stopTimeout,
		bool useMarketOrders);
}

/// <summary>
/// <see cref="IProtectiveBehaviourFactory"/> uses server stop-orders.
/// </summary>
public class ServerProtectiveBehaviourFactory : IProtectiveBehaviourFactory
{
	private class ServerProtectiveBehaviour : BaseProtectiveBehaviour
	{
		private readonly IMessageAdapter _adapter;

		public ServerProtectiveBehaviour(
			IMessageAdapter adapter,
			Unit takeValue, Unit stopValue,
			bool isTakeTrailing, bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
			: base(takeValue, stopValue, isTakeTrailing, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders)
		{
			if (StopValue.IsSet() && !adapter.IsSupportStopLoss())
				throw new ArgumentException($"{nameof(IStopLossOrderCondition)} not supported.");

			if (TakeValue.IsSet() && !adapter.IsSupportTakeProfit())
				throw new ArgumentException($"{nameof(ITakeProfitOrderCondition)} not supported.");

			_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
		}

		public override (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time)
			=> default;

		public override (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? Update(decimal price, decimal value)
		{
			var condition = _adapter.CreateOrderCondition() ?? throw new NotSupportedException();

			var protectiveSide = value > 0 ? Sides.Buy : Sides.Sell;
			var protectivePrice = price;

			if (TakeValue.IsSet() && condition is ITakeProfitOrderCondition take)
			{
				take.IsTrailing = IsTakeTrailing;
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

		public override void Clear()
		{
		}
	}

	private readonly IMessageAdapter _adapter;

	/// <summary>
	/// Initializes a new instance of the <see cref="ServerProtectiveBehaviourFactory"/>.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/> for creation server stop-orders.</param>
	public ServerProtectiveBehaviourFactory(IMessageAdapter adapter)
	{
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	IProtectiveBehaviour IProtectiveBehaviourFactory.Create(Unit takeValue, Unit stopValue, bool isTakeTrailing, bool isStopTrailing, TimeSpan takeTimeout, TimeSpan stopTimeout, bool useMarketOrders)
		=> new ServerProtectiveBehaviour(_adapter, takeValue, stopValue, isTakeTrailing, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
}

/// <summary>
/// <see cref="IProtectiveBehaviourFactory"/> uses local (=emulated) stop-orders.
/// </summary>
public class LocalProtectiveBehaviourFactory : IProtectiveBehaviourFactory
{
	private class LocalProtectiveBehaviour : BaseProtectiveBehaviour
	{
		private readonly decimal? _priceStep;
		private readonly int? _decimals;

		private ProtectiveProcessor _take;
		private ProtectiveProcessor _stop;

		private decimal _posValue;
		private readonly LinkedList<decimal> _posPrices = new();
		private decimal _posPrice;

		public LocalProtectiveBehaviour(
			decimal? priceStep, int? decimals,
			Unit takeValue, Unit stopValue,
			bool isTakeTrailing, bool isStopTrailing,
			TimeSpan takeTimeout, TimeSpan stopTimeout,
			bool useMarketOrders)
			: base(takeValue, stopValue, isTakeTrailing, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders)
		{
			_priceStep = priceStep;
			_decimals = decimals;
		}

		public override (bool, Sides, decimal, decimal, OrderCondition)? TryActivate(decimal price, DateTimeOffset time)
		{
			if (_posValue == 0)
				return null;

			(bool, Sides, decimal, decimal, OrderCondition)? TryActivate(ProtectiveProcessor proc, bool isTake)
			{
				var activationPrice = proc?.GetActivationPrice(price, time);
				if (activationPrice is null)
					return null;

				return (
					isTake,
					_posValue > 0 ? Sides.Sell : Sides.Buy,
					activationPrice.Value,
					_posValue.Abs(),
					null
				);
			}

			return TryActivate(_take, true) ?? TryActivate(_stop, false);
		}

		public override (bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition)? Update(decimal price, decimal value)
		{
			if (price <= 0)
				return null;

			if (value == default)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			var wasZero = _posValue == 0;
			var wasPositive = _posValue > 0;

			_posValue += value;

			ResetProcessors();

			if (_posValue == 0)
			{
				_posPrices.Clear();
				_posPrice = default;
			}
			else
			{
				var protectiveSide = _posValue > 0 ? Sides.Buy : Sides.Sell;

				if (wasZero || wasPositive != (_posValue > 0))
				{
					_posPrices.Clear();
					_posPrices.AddLast(price);
					_posPrice = price;
				}
				else
				{
					if (_posPrices.Count == 0)
						throw new InvalidOperationException();

					_posPrice *= _posPrices.Count;

					var isPosReduced = wasPositive == (value < 0);

					if (isPosReduced)
					{
						_posPrice -= _posPrices.First.Value;
						_posPrices.RemoveFirst();
					}
					else
					{
						_posPrice += price;
						_posPrices.AddLast(price);
					}

					_posPrice /= _posPrices.Count;

					if (_priceStep is not null)
						_posPrice = _posPrice.ShrinkPrice(_priceStep, _decimals);
				}

				_take = TakeValue.IsSet() ? new ProtectiveProcessor(protectiveSide, _posPrice, protectiveSide == Sides.Buy, IsTakeTrailing, TakeValue, UseMarketOrders, new(), TakeTimeout) : null;
				_stop = StopValue.IsSet() ? new ProtectiveProcessor(protectiveSide, _posPrice, protectiveSide == Sides.Sell, IsStopTrailing, StopValue, UseMarketOrders, new(), StopTimeout) : null;
			}

			return null;
		}

		private void ResetProcessors()
		{
			_take?.Dispose();
			_stop?.Dispose();
			_take = _stop = default;
		}

		public override void Clear()
		{
			ResetProcessors();
			_posPrices.Clear();
			_posPrice = default;
			_posValue = default;
		}
	}

	private readonly decimal? _priceStep;
	private readonly int? _decimals;

	/// <summary>
	/// Initializes a new instance of the <see cref="LocalProtectiveBehaviourFactory"/>.
	/// </summary>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <param name="decimals"><see cref="SecurityMessage.Decimals"/></param>
	public LocalProtectiveBehaviourFactory(decimal? priceStep, int? decimals)
    {
		_priceStep = priceStep;
		_decimals = decimals;
	}

    IProtectiveBehaviour IProtectiveBehaviourFactory.Create(Unit takeValue, Unit stopValue, bool isTakeTrailing, bool isStopTrailing, TimeSpan takeTimeout, TimeSpan stopTimeout, bool useMarketOrders)
		=> new LocalProtectiveBehaviour(_priceStep, _decimals, takeValue, stopValue, isTakeTrailing, isStopTrailing, takeTimeout, stopTimeout, useMarketOrders);
}