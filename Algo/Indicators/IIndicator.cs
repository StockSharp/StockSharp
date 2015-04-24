namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Интерфейс, описывающий индикатор.
	/// </summary>
	public interface IIndicator : IPersistable, ICloneable<IIndicator>
	{
		/// <summary>
		/// Уникальный идентификатор.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Название индикатора.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		bool IsFormed { get; }

		/// <summary>
		/// Контейнер, хранящий данные индикатора.
		/// </summary>
		IIndicatorContainer Container { get; }

		/// <summary>
		/// Событие об изменении индикатора (например, добавлено новое значение).
		/// </summary>
		event Action<IIndicatorValue, IIndicatorValue> Changed;

		/// <summary>
		/// Событие о сбросе состояния индикатора на первоначальное. Событие вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		event Action Reseted;

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Новое значение индикатора.</returns>
		IIndicatorValue Process(IIndicatorValue input);

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		void Reset();
	}
}
