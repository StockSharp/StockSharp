namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс контейнера, хранящего данные индикатора.
	/// </summary>
	public interface IIndicatorContainer
	{
		/// <summary>
		/// Текущее количество сохраненных значений.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Добавить новые значения.
		/// </summary>
		/// <param name="input">Входное значение индикатора.</param>
		/// <param name="result">Результирующее значение индикатора.</param>
		void AddValue(IIndicatorValue input, IIndicatorValue result);

		/// <summary>
		/// Получить все значения индикатора.
		/// </summary>
		/// <returns>Все значения индикатора. Пустое множество, если значений нет.</returns>
		IEnumerable<Tuple<IIndicatorValue, IIndicatorValue>> GetValues();

		/// <summary>
		/// Получить значения индикатора по индексу.
		/// </summary>
		/// <param name="index">Порядковый номер значения с конца.</param>
		/// <returns>Входное и результирующие значения индикатора.</returns>
		Tuple<IIndicatorValue, IIndicatorValue> GetValue(int index);

		/// <summary>
		/// Удалить все значения индикатора.
		/// </summary>
		void ClearValues();
	}
}