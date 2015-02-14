namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий хранилище, ассоциированное с <see cref="IMarketDataStorage"/>.
	/// </summary>
	public interface IMarketDataStorageDrive
	{
		/// <summary>
		/// Хранилище (база данных, файл и т.д.).
		/// </summary>
		IMarketDataDrive Drive { get; }

		/// <summary>
		/// Получить все даты, для которых записаны маркет-данные.
		/// </summary>
		IEnumerable<DateTime> Dates { get; }

		/// <summary>
		/// Удалить кэш-файлы, хранящие в себе информацию о доступных диапазонах времени.
		/// </summary>
		void ClearDatesCache();

		/// <summary>
		/// Удалить маркет-данные из хранилища за указанную дату.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо удалить все данные.</param>
		void Delete(DateTime date);

		/// <summary>
		/// Сохранить данные в формате хранилища StockSharp.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо сохранить данные.</param>
		/// <param name="stream">Данные в формате хранилища StockSharp.</param>
		void SaveStream(DateTime date, Stream stream);

		/// <summary>
		/// Загрузить данные в формате хранилища StockSharp.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные в формате хранилища StockSharp. Если данных не существует, то будет возвращено <see cref="Stream.Null"/>.</returns>
		Stream LoadStream(DateTime date);
	}

	/// <summary>
	/// Интерфейс, описывающий хранилище (база данных, файл и т.д.).
	/// </summary>
	public interface IMarketDataDrive : IPersistable, IDisposable
	{
		/// <summary>
		/// Путь к данными.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// Получить для инструмента доступные типы свечек с параметрами.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Доступные типы свечек с параметрами.</returns>
		IEnumerable<Tuple<Type, object[]>> GetCandleTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// Получить хранилище для <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Хранилище для <see cref="IMarketDataStorage"/>.</returns>
		IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);
	}

	/// <summary>
	/// Базовая реализация <see cref="IMarketDataDrive"/>.
	/// </summary>
	public abstract class BaseMarketDataDrive : Disposable, IMarketDataDrive
	{
		/// <summary>
		/// Инициализировать <see cref="BaseMarketDataDrive"/>.
		/// </summary>
		protected BaseMarketDataDrive()
		{
		}

		/// <summary>
		/// Путь к данными.
		/// </summary>
		public abstract string Path { get; set; }

		/// <summary>
		/// Получить для инструмента доступные типы свечек с параметрами.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Доступные типы свечек с параметрами.</returns>
		public abstract IEnumerable<Tuple<Type, object[]>> GetCandleTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// Создать хранилище для <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Хранилище для <see cref="IMarketDataStorage"/>.</returns>
		public abstract IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format);

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Path = storage.GetValue<string>("Path");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Path", Path);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Path;
		}
	}
}