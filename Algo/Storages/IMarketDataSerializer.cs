namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;

	/// <summary>
	/// Сериализатор.
	/// </summary>
	public interface IMarketDataSerializer
	{
		/// <summary>
		/// Создать пустую мета-информацию.
		/// </summary>
		/// <param name="date">Дата.</param>
		/// <returns>Мета-информация о данных за один день.</returns>
		IMarketDataMetaInfo CreateMetaInfo(DateTime date);

		/// <summary>
		/// Преобразовать данные в поток байтов.
		/// </summary>
		/// <param name="data">Данные.</param>
		/// <param name="metaInfo">Мета-информация о данных за один день.</param>
		/// <returns>Поток байтов.</returns>
		byte[] Serialize(IEnumerable data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// Загрузить данные из потока.
		/// </summary>
		/// <param name="stream">Потока.</param>
		/// <param name="metaInfo">Мета-информация о данных за один день.</param>
		/// <returns>Данные.</returns>
		IEnumerableEx Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}

	/// <summary>
	/// Сериализатор.
	/// </summary>
	/// <typeparam name="TData">Тип данных.</typeparam>
	public interface IMarketDataSerializer<TData> : IMarketDataSerializer
	{
		/// <summary>
		/// Преобразовать данные в поток байтов.
		/// </summary>
		/// <param name="data">Данные.</param>
		/// <param name="metaInfo">Мета-информация о данных за один день.</param>
		/// <returns>Поток байт.</returns>
		byte[] Serialize(IEnumerable<TData> data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// Загрузить данные из потока.
		/// </summary>
		/// <param name="stream">Поток.</param>
		/// <param name="metaInfo">Мета-информация о данных за один день.</param>
		/// <returns>Данные.</returns>
		new IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// Загрузить данные из потока.
		/// </summary>
		/// <param name="reader">Считыватель данных.</param>
		/// <returns>Данные.</returns>
		IEnumerableEx<TData> Deserialize(IDataStorageReader<TData> reader);
	}
}