namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс, описывающий хранилище маркет-данных (тики, стаканы и т.д.).
	/// </summary>
	public interface IMarketDataStorage
	{
		/// <summary>
		/// Все даты, для которых записаны маркет-данные.
		/// </summary>
		IEnumerable<DateTime> Dates { get; }

		/// <summary>
		/// Тип маркет-данных, с которыми работает данное хранилище.
		/// </summary>
		Type DataType { get; }

		/// <summary>
		/// Инструмент, с которым работает внешнее хранилище.
		/// </summary>
		Security Security { get; }

		/// <summary>
		/// Дополнительный аргумент, ассоциированный с данными. Например, <see cref="Candle.Arg"/>.
		/// </summary>
		object Arg { get; }

		/// <summary>
		/// Хранилище (база данных, файл и т.д.).
		/// </summary>
		IMarketDataStorageDrive Drive { get; }

		/// <summary>
		/// Добавлять ли только новые данные или пытаться записать все данные без фильтра.
		/// </summary>
		bool AppendOnlyNew { get; set; }

		/// <summary>
		/// Сохранить маркет-данные в хранилище.
		/// </summary>
		/// <param name="data">Маркет-данные.</param>
		void Save(IEnumerable data);

		/// <summary>
		/// Удалить маркет-данные из хранилища.
		/// </summary>
		/// <param name="data">Маркет-данные, которые необходимо удалить.</param>
		void Delete(IEnumerable data);

		/// <summary>
		/// Удалить маркет-данные из хранилища за указанную дату.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо удалить все данные.</param>
		void Delete(DateTime date);

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		IEnumerable Load(DateTime date);

		/// <summary>
		/// Получить мета-информация о данных.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо получить мета-информация о данных.</param>
		/// <returns>Мета-информация о данных. Если дня в истории не существует, то будет возвращено <see langword="null"/>.</returns>
		IMarketDataMetaInfo GetMetaInfo(DateTime date);

		/// <summary>
		/// Сериализатор.
		/// </summary>
		IMarketDataSerializer Serializer { get; }
	}

	/// <summary>
	/// Интерфейс, описывающий хранилище маркет-данных (тики, стаканы и т.д.).
	/// </summary>
	/// <typeparam name="TData">Тип маркет-данных.</typeparam>
	public interface IMarketDataStorage<TData> : IMarketDataStorage
	{
		/// <summary>
		/// Сохранить маркет-данные в хранилище.
		/// </summary>
		/// <param name="data">Маркет-данные.</param>
		void Save(IEnumerable<TData> data);

		/// <summary>
		/// Удалить маркет-данные из хранилища.
		/// </summary>
		/// <param name="data">Маркет-данные, которые необходимо удалить.</param>
		void Delete(IEnumerable<TData> data);

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		new IEnumerableEx<TData> Load(DateTime date);

		/// <summary>
		/// Получить считыватель данных за указанную дату.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Считыватель данных.</returns>
		IDataStorageReader<TData> GetReader(DateTime date);

		/// <summary>
		/// Сериализатор.
		/// </summary>
		new IMarketDataSerializer<TData> Serializer { get; }
	}
}