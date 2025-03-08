namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Candles;

/// <summary>
/// </summary>
public static class IStorageRegistryObsoleteExtensions
{
	[Obsolete]
	private class ConvertableStorage<TMessage, TEntity>(Security security, IMarketDataStorage<TMessage> messageStorage, IExchangeInfoProvider exchangeInfoProvider, Func<TEntity, TMessage> toMessage) : IEntityMarketDataStorage<TEntity, TMessage>, IMarketDataStorageInfo<TMessage>
		where TMessage : Message
	{
		private readonly IMarketDataStorage<TMessage> _messageStorage = messageStorage ?? throw new ArgumentNullException(nameof(messageStorage));
		private readonly IExchangeInfoProvider _exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		private readonly Func<TEntity, TMessage> _toMessage = toMessage ?? throw new ArgumentNullException(nameof(toMessage));

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date) => _messageStorage.GetMetaInfo(date);

		IMarketDataSerializer IMarketDataStorage.Serializer => _messageStorage.Serializer;

		IMarketDataSerializer<TMessage> IMarketDataStorage<TMessage>.Serializer => throw new NotSupportedException();

		IEnumerable<DateTime> IMarketDataStorage.Dates => _messageStorage.Dates;

		DataType IMarketDataStorage.DataType => _messageStorage.DataType;

		SecurityId IMarketDataStorage.SecurityId => _messageStorage.SecurityId;

		IMarketDataStorageDrive IMarketDataStorage.Drive => _messageStorage.Drive;

		bool IMarketDataStorage.AppendOnlyNew
		{
			get => _messageStorage.AppendOnlyNew;
			set => _messageStorage.AppendOnlyNew = value;
		}

		int IMarketDataStorage.Save(IEnumerable<Message> data)
		{
			return ((IMarketDataStorage<TMessage>)this).Save(data.Cast<TMessage>());
		}

		int IEntityMarketDataStorage<TEntity, TMessage>.Save(IEnumerable<TEntity> data)
		{
			return ((IMarketDataStorage<TMessage>)this).Save(data.Select(_toMessage));
		}

		int IMarketDataStorage<TMessage>.Save(IEnumerable<TMessage> data)
		{
			return _messageStorage.Save(data);
		}

		void IMarketDataStorage.Delete(IEnumerable<Message> data)
		{
			((IMarketDataStorage<TMessage>)this).Delete(data.Cast<TMessage>());
		}

		void IEntityMarketDataStorage<TEntity, TMessage>.Delete(IEnumerable<TEntity> data)
		{
			((IMarketDataStorage<TMessage>)this).Delete(data.Select(_toMessage));
		}

		void IMarketDataStorage<TMessage>.Delete(IEnumerable<TMessage> data)
		{
			_messageStorage.Delete(data);
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			_messageStorage.Delete(date);
		}

		IEnumerable<Message> IMarketDataStorage.Load(DateTime date)
		{
			return ((IMarketDataStorage<TMessage>)this).Load(date);
		}

		public IEnumerable<TEntity> Load(DateTime date)
		{
			return ((IMarketDataStorage<TMessage>)this).Load(date).ToEntities<TMessage, TEntity>(security, _exchangeInfoProvider);
		}

		IEnumerable<TMessage> IMarketDataStorage<TMessage>.Load(DateTime date)
		{
			return _messageStorage.Load(date);
		}

		DateTimeOffset IMarketDataStorageInfo<TMessage>.GetTime(TMessage message)
		{
			return ((IMarketDataStorageInfo)this).GetTime(message);
		}

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return ((IMarketDataStorageInfo<TMessage>)_messageStorage).GetTime((TMessage)data);
		}
	}

	/// <summary>
	/// To get the storage of candles.
	/// </summary>
	/// <typeparam name="TCandle">The candle type.</typeparam>
	/// <typeparam name="TArg">The type of candle parameter.</typeparam>
	/// <param name="storageRegistry">The external storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The candles storage.</returns>
	[Obsolete("Use GetCandleMessageStorage method.")]
	public static IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage<TCandle, TArg>(this IStorageRegistry storageRegistry, Security security, TArg arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		where TCandle : Candle
	{
		return storageRegistry.GetCandleStorage(typeof(TCandle), security, arg, drive, format);
	}

	/// <summary>
	/// To get the storage of candles.
	/// </summary>
	/// <param name="storageRegistry">The external storage.</param>
	/// <param name="series">Candles series.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The candles storage.</returns>
	[Obsolete("Use GetCandleMessageStorage method.")]
	public static IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage(this IStorageRegistry storageRegistry, CandleSeries series, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		return storageRegistry.GetCandleStorage(series.CandleType, series.Security, series.Arg, drive, format);
	}

	/// <summary>
	/// To get news storage.
	/// </summary>
	/// <param name="registry">The external storage.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The news storage.</returns>
	[Obsolete("Use GetNewsMessageStorage method.")]
	public static IEntityMarketDataStorage<News, NewsMessage> GetNewsStorage(this IStorageRegistry registry, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return registry.CheckOnNull(nameof(registry)).GetNewsMessageStorage(drive, format).ToEntityStorage<NewsMessage, News>(null, registry.ExchangeInfoProvider);
	}

	/// <summary>
	/// To get the storage of tick trades for the specified instrument.
	/// </summary>
	/// <param name="registry">The external storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The storage of tick trades.</returns>
	[Obsolete("Use GetTickMessageStorage method.")]
	public static IEntityMarketDataStorage<Trade, ExecutionMessage> GetTradeStorage(this IStorageRegistry registry, Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return registry.CheckOnNull(nameof(registry)).GetTickMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<ExecutionMessage, Trade>(security, registry.ExchangeInfoProvider);
	}

	/// <summary>
	/// To get the storage of order books for the specified instrument.
	/// </summary>
	/// <param name="registry">The external storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The order books storage.</returns>
	[Obsolete("Use GetQuoteMessageStorage method.")]
	public static IEntityMarketDataStorage<MarketDepth, QuoteChangeMessage> GetMarketDepthStorage(this IStorageRegistry registry, Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return registry.CheckOnNull(nameof(registry)).GetQuoteMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<QuoteChangeMessage, MarketDepth>(security, registry.ExchangeInfoProvider);
	}

	/// <summary>
	/// To get the storage of orders log for the specified instrument.
	/// </summary>
	/// <param name="registry">The external storage.</param>
	/// <param name="security">Security.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The storage of orders log.</returns>
	[Obsolete("Use GetOrderLogMessageStorage method.")]
	public static IEntityMarketDataStorage<OrderLogItem, ExecutionMessage> GetOrderLogStorage(this IStorageRegistry registry, Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return registry.CheckOnNull(nameof(registry)).GetOrderLogMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<ExecutionMessage, OrderLogItem>(security, registry.ExchangeInfoProvider);
	}

	/// <summary>
	/// To get the candles storage for the specified instrument.
	/// </summary>
	/// <param name="registry">The external storage.</param>
	/// <param name="candleType">The candle type.</param>
	/// <param name="security">Security.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
	/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
	/// <returns>The candles storage.</returns>
	[Obsolete("Use GetCandleMessageStorage method.")]
	public static IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage(this IStorageRegistry registry, Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return registry.CheckOnNull(nameof(registry)).GetCandleMessageStorage(candleType.ToCandleMessageType(), security.ToSecurityId(), arg, drive, format).ToEntityStorage<CandleMessage, Candle>(security, registry.ExchangeInfoProvider);
	}

	private static readonly SynchronizedDictionary<IMarketDataStorage, IMarketDataStorage> _convertedStorages = [];

	[Obsolete]
	private static IEntityMarketDataStorage<TEntity, TMessage> ToEntityStorage<TMessage, TEntity>(this IMarketDataStorage<TMessage> storage, Security security, IExchangeInfoProvider exchangeInfoProvider = null)
		where TMessage : Message
	{
		Func<TEntity, TMessage> toMessage;

		if (typeof(TEntity) == typeof(MarketDepth))
		{
			Func<MarketDepth, QuoteChangeMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(Trade))
		{
			Func<Trade, ExecutionMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(OrderLogItem))
		{
			Func<OrderLogItem, ExecutionMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(News))
		{
			Func<News, NewsMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(Security))
		{
			Func<Security, SecurityMessage> converter = s => s.ToMessage();
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(Position))
		{
			Func<Position, PositionChangeMessage> converter = p => p.ToChangeMessage();
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(Order))
		{
			Func<Order, ExecutionMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(MyTrade))
		{
			Func<MyTrade, ExecutionMessage> converter = EntitiesExtensions.ToMessage;
			toMessage = converter.To<Func<TEntity, TMessage>>();
		}
		else if (typeof(TEntity) == typeof(Candle) || typeof(TEntity).IsCandle())
		{
			Func<Candle, CandleMessage> converter = EntitiesExtensions.ToMessage;

			if (typeof(TEntity) == typeof(Candle) && typeof(TMessage) == typeof(CandleMessage))
				toMessage = converter.To<Func<TEntity, TMessage>>();
			else
				toMessage = e => converter(e.To<Candle>()).To<TMessage>();
		}
		else
			throw new ArgumentOutOfRangeException(nameof(TEntity), typeof(TEntity), LocalizedStrings.InvalidValue);

		return (IEntityMarketDataStorage<TEntity, TMessage>)_convertedStorages.SafeAdd(storage, key => new ConvertableStorage<TMessage, TEntity>(security, storage, exchangeInfoProvider ?? new InMemoryExchangeInfoProvider(), toMessage));
	}
}