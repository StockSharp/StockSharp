namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// Базовый реализация интерфейса <see cref="ICandleSource{TValue}"/>.
	/// </summary>
	/// <typeparam name="TValue">Тип данных.</typeparam>
	public abstract class BaseCandleSource<TValue> : Disposable, ICandleSource<TValue>
	{
		/// <summary>
		/// Инициализировать <see cref="BaseCandleSource{TValue}"/>.
		/// </summary>
		protected BaseCandleSource()
		{
		}

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public abstract int SpeedPriority { get; }

		/// <summary>
		/// Событие появления нового значения для обработки.
		/// </summary>
		public event Action<CandleSeries, TValue> Processing;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Событие ошибки транслирования данных.
		/// </summary>
		public event Action<Exception> ProcessDataError;

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public abstract IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series);

		/// <summary>
		/// Запросить получение данных.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать данные.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public abstract void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to);

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public abstract void Stop(CandleSeries series);

		/// <summary>
		/// Вызвать событие <see cref="Processing"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="values">Новые данные.</param>
		protected virtual void RaiseProcessing(CandleSeries series, TValue values)
		{
			Processing.SafeInvoke(series, values);
		}

		/// <summary>
		/// Вызвать событие <see cref="ProcessDataError"/>.
		/// </summary>
		/// <param name="error">Описание ошибки.</param>
		protected void RaiseProcessDataError(Exception error)
		{
			ProcessDataError.SafeInvoke(error);
		}

		/// <summary>
		/// Вызвать событие <see cref="Stopped"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		protected void RaiseStopped(CandleSeries series)
		{
			Stopped.SafeInvoke(series);
		}
	}
}