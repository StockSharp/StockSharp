namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Контейнер, хранящий данные индикаторов.
	/// </summary>
	public class IndicatorContainer : IIndicatorContainer
	{
		private readonly FixedSynchronizedList<Tuple<IIndicatorValue, IIndicatorValue>> _values = new FixedSynchronizedList<Tuple<IIndicatorValue, IIndicatorValue>>();

		/// <summary>
		/// Максимальное количество значений индикаторов.
		/// </summary>
		public int MaxValueCount
		{
			get { return _values.BufferSize; }
			set { _values.BufferSize = value; }
		}

		/// <summary>
		/// Текущее количество сохраненных значений.
		/// </summary>
		public int Count
		{
			get { return _values.Count; }
		}

		/// <summary>
		/// Добавить новые значения.
		/// </summary>
		/// <param name="input">Входное значение индикатора.</param>
		/// <param name="result">Результирующее значение индикатора.</param>
		public virtual void AddValue(IIndicatorValue input, IIndicatorValue result)
		{
			_values.Add(Tuple.Create(input, result));
		}

		/// <summary>
		/// Получить все значения индикатора.
		/// </summary>
		/// <returns>Все значения индикатора. Пустое множество, если значений нет.</returns>
		public virtual IEnumerable<Tuple<IIndicatorValue, IIndicatorValue>> GetValues()
		{
			return _values.SyncGet(c => c.Reverse().ToArray());
		}

		/// <summary>
		/// Получить значения индикатора по индексу.
		/// </summary>
		/// <param name="index">Порядковый номер значения с конца.</param>
		/// <returns>Входное и результирующие значения индикатора.</returns>
		public virtual Tuple<IIndicatorValue, IIndicatorValue> GetValue(int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.Str912);

			lock (_values.SyncRoot)
			{
				if (index >= _values.Count)
					throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.Str913);

				return _values[_values.Count - 1 - index];
			}
		}

		/// <summary>
		/// Удалить все значения индикатора.
		/// </summary>
		public virtual void ClearValues()
		{
			_values.Clear();
		}
	}
}