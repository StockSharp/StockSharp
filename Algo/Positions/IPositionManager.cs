namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс для менеджера расчета позиции.
	/// </summary>
	public interface IPositionManager
	{
		/// <summary>
		/// Суммарное значение позиции.
		/// </summary>
		decimal Position { get; set; }

		/// <summary>
		/// Позиции, сгруппированные по инструментам и портфелям.
		/// </summary>
		IEnumerable<Position> Positions { get; set; }

		/// <summary>
		/// Событие появления новой позиций в <see cref="Positions"/>.
		/// </summary>
		event Action<Position> NewPosition;

		/// <summary>
		/// Событие изменения позиции в <see cref="Positions"/>.
		/// </summary>
		event Action<Position> PositionChanged;

		/// <summary>
		/// Обнулить позицию.
		/// </summary>
		void Reset();

		/// <summary>
		/// Рассчитать позицию по заявке.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Позиция по заявке.</returns>
		decimal ProcessOrder(Order order);

		/// <summary>
		/// Рассчитать позицию по сделке.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Позиция по сделке.</returns>
		decimal ProcessMyTrade(MyTrade trade);
	}
}