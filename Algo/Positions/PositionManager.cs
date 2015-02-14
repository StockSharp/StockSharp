namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Менеджер расчета позиции.
	/// </summary>
	public class PositionManager : IPositionManager
	{
		private readonly object _syncRoot = new object();
		private readonly Dictionary<Order, decimal> _byOrderPositions = new Dictionary<Order, decimal>();

		/// <summary>
		/// Создать <see cref="PositionManager"/>.
		/// </summary>
		/// <param name="byOrders">Расчитывать позицию по реализованному объему у заявок (true) или по сделкам (false).</param>
		public PositionManager(bool byOrders)
		{
			ByOrders = byOrders;
		}

		/// <summary>
		/// Расчитывать позицию по реализованному объему у заявок (true) или по сделкам (false).
		/// </summary>
		public bool ByOrders { get; private set; }

		/// <summary>
		/// Суммарное значение позиции.
		/// </summary>
		public virtual decimal Position { get; set; }

		private readonly CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position> _positions = new CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position>();

		/// <summary>
		/// Позиции, сгруппированные по инструментам и портфелям.
		/// </summary>
		public IEnumerable<Position> Positions
		{
			get { return _positions.CachedValues; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				lock (_syncRoot)
					Position = value.Sum(p => p.CurrentValue);

				Dictionary<Tuple<Security, Portfolio>, decimal> positions;

				lock (_positions.SyncRoot)
				{
					positions = _positions.ToDictionary(p => p.Key, p => -p.Value.CurrentValue);

					foreach (var position in value)
					{
						var key = Tuple.Create(position.Security, position.Portfolio);

						var pos = positions.TryGetValue2(key);

						if (pos.HasValue)
							positions[key] = pos.Value + position.CurrentValue;
						else
							positions[key] = position.CurrentValue;
					}
				}

				positions.ForEach(p => ChangePosition(p.Key.Item1, p.Key.Item2, p.Value));
			}
		}

		/// <summary>
		/// Событие появления новой позиций в <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<Position> NewPosition;

		/// <summary>
		/// Событие изменения позиции в <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<Position> PositionChanged;

		/// <summary>
		/// Рассчитать позицию по заявке.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Позиция по заявке.</returns>
		public decimal ProcessOrder(Order order)
		{
			if (!ByOrders)
				return 0;

			decimal position;

			lock (_syncRoot)
			{
				var newPosition = order.GetPosition();

				decimal oldPosition;

				if (_byOrderPositions.TryGetValue(order, out oldPosition))
				{
					if (newPosition != oldPosition)
						_byOrderPositions[order] = newPosition;

					position = newPosition - oldPosition;
				}
				else
				{
					_byOrderPositions.Add(order, newPosition);
					position = newPosition;
				}
				
				//TODO:PYH: Если для Done-заявки ProcessOrder придет 2 раза(в Квике) то будет сделка-дубль.
				//Избежать можно храня где-то oldState заявки и исключая такие варианты.
				//
				// mika: пока подключения не доделаны, временно отключил
				//
				//if (order.State == OrderStates.Done)
				//    _byOrderPositions.Remove(order);
				
				Position += position;
			}
			
			ChangePosition(order.Security, order.Portfolio, position);
			return position;
		}

		/// <summary>
		/// Обнулить позицию.
		/// </summary>
		public virtual void Reset()
		{
			_positions.Clear();

			lock (_syncRoot)
			{
				_byOrderPositions.Clear();
				Position = 0;
			}
		}

		/// <summary>
		/// Рассчитать позицию по сделке.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Позиция по сделке.</returns>
		public virtual decimal ProcessMyTrade(MyTrade trade)
		{
			if (ByOrders)
				return 0;

			var position = trade.GetPosition();

			lock (_syncRoot)
				Position += position;

			ChangePosition(trade.Order.Security, trade.Order.Portfolio, position);

			return position;
		}

		private void ChangePosition(Security security, Portfolio portfolio, decimal diff)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			bool isNew;
			var position = _positions.SafeAdd(Tuple.Create(security, portfolio),
			    key => new Position { Security = key.Item1, Portfolio = key.Item2 }, out isNew);

			position.CurrentValue += diff;

			if (isNew)
				NewPosition.SafeInvoke(position);
			else
				PositionChanged.SafeInvoke(position);
		}
	}
}