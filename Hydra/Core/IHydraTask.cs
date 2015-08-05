namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Candles;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Состояния задачи.
	/// </summary>
	public enum TaskStates
	{
		/// <summary>
		/// Остановлен.
		/// </summary>
		Stopped,

		/// <summary>
		/// Останавливается.
		/// </summary>
		Stopping,

		/// <summary>
		/// Запускается.
		/// </summary>
		Starting,

		/// <summary>
		/// Запущен.
		/// </summary>
		Started,
	}

	/// <summary>
	/// Интерфейс, описывающий задачу.
	/// </summary>
	public interface IHydraTask : ILogReceiver
	{
		/// <summary>
		/// Адрес иконки, для визуального обозначения.
		/// </summary>
		Uri Icon { get; }

		/// <summary>
		/// Настройки задачи <see cref="IHydraTask"/>.
		/// </summary>
		HydraTaskSettings Settings { get; }

		/// <summary>
		/// Инициализировать задачу.
		/// </summary>
		/// <param name="settings">Настройки задачи.</param>
		void Init(HydraTaskSettings settings);

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		void SaveSettings();

		/// <summary>
		/// Запустить.
		/// </summary>
		void Start();

		/// <summary>
		/// Остановить.
		/// </summary>
		void Stop();

		/// <summary>
		/// Поддерживаемые маркет-данные.
		/// </summary>
		IEnumerable<Type> SupportedMarketDataTypes { get; }

		/// <summary>
		/// Поддерживаемые серии свечек.
		/// </summary>
		IEnumerable<CandleSeries> SupportedCandleSeries { get; }

		/// <summary>
		/// Событие о загрузке маркет-данных.
		/// </summary>
		event Action<Security, Type, object, DateTimeOffset, int> DataLoaded;

		/// <summary>
		/// Событие запуска.
		/// </summary>
		event Action<IHydraTask> Started;

		/// <summary>
		/// Событие остановки.
		/// </summary>
		event Action<IHydraTask> Stopped;

		/// <summary>
		/// Текущее состояние задачи.
		/// </summary>
		TaskStates State { get; }
	}
}