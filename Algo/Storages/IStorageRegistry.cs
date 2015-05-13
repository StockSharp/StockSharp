namespace StockSharp.Algo.Storages
{
	using System;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий хранилище маркет-данных.
	/// </summary>
	public interface IStorageRegistry
	{
		/// <summary>
		/// Хранилище, которое используется по-умолчанию.
		/// </summary>
		IMarketDataDrive DefaultDrive { get; }

		/// <summary>
		/// Получить хранилище новостей.
		/// </summary>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище новостей.</returns>
		IMarketDataStorage<News> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище новостей.
		/// </summary>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище новостей.</returns>
		IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище тиковых сделок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище тиковых сделок.</returns>
		IMarketDataStorage<Trade> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище стаканов для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище стаканов.</returns>
		IMarketDataStorage<MarketDepth> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище лога заявок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище лога заявок.</returns>
		IMarketDataStorage<OrderLogItem> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище свечек для заданного инструмента.
		/// </summary>
		/// <param name="candleType">Тип свечи.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		IMarketDataStorage<Candle> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище тиковых сделок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище тиковых сделок.</returns>
		IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище стаканов для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище стаканов.</returns>
		IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище лога заявок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище лога заявок.</returns>
		IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище level1 данных.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище level1 данных.</returns>
		IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище свечек для заданного инструмента.
		/// </summary>
		/// <param name="candleType">Тип свечи.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище транзакций для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="type">Тип данных, информация о которых содержится <see cref="ExecutionMessage"/>.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище транзакций.</returns>
		IMarketDataStorage<ExecutionMessage> GetExecutionStorage(Security security, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Получить хранилище маркет-данных.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище маркет-данных.</returns>
		IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary);

		/// <summary>
		/// Зарегистрировать хранилище тиковых сделок.
		/// </summary>
		/// <param name="storage">Хранилище тиковых сделок</param>
		void RegisterTradeStorage(IMarketDataStorage<Trade> storage);

		/// <summary>
		/// Зарегистрировать хранилище стаканов.
		/// </summary>
		/// <param name="storage">Хранилище стаканов.</param>
		void RegisterMarketDepthStorage(IMarketDataStorage<MarketDepth> storage);

		/// <summary>
		/// Зарегистрировать хранилище лога заявок.
		/// </summary>
		/// <param name="storage">Хранилище лога заявок.</param>
		void RegisterOrderLogStorage(IMarketDataStorage<OrderLogItem> storage);

		/// <summary>
		/// Зарегистрировать хранилище свечек.
		/// </summary>
		/// <param name="storage">Хранилище свечек.</param>
		void RegisterCandleStorage(IMarketDataStorage<Candle> storage);

		/// <summary>
		/// Зарегистрировать хранилище тиковых сделок.
		/// </summary>
		/// <param name="storage">Хранилище тиковых сделок</param>
		void RegisterTradeStorage(IMarketDataStorage<ExecutionMessage> storage);

		/// <summary>
		/// Зарегистрировать хранилище стаканов.
		/// </summary>
		/// <param name="storage">Хранилище стаканов.</param>
		void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage);

		/// <summary>
		/// Зарегистрировать хранилище лога заявок.
		/// </summary>
		/// <param name="storage">Хранилище лога заявок.</param>
		void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage);

		/// <summary>
		/// Зарегистрировать хранилище level1 данных.
		/// </summary>
		/// <param name="storage">Хранилище level1 данных.</param>
		void RegisterLevel1Storage(IMarketDataStorage<Level1ChangeMessage> storage);

		/// <summary>
		/// Зарегистрировать хранилище свечек.
		/// </summary>
		/// <param name="storage">Хранилище свечек.</param>
		void RegisterCandleStorage(IMarketDataStorage<CandleMessage> storage);
	}
}