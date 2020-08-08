#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ISecurityMarketDataDrive.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing the storage for instrument.
	/// </summary>
	public interface ISecurityMarketDataDrive
	{
		/// <summary>
		/// Instrument identifier.
		/// </summary>
		SecurityId SecurityId { get; }

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of tick trades.</returns>
		IMarketDataStorage<ExecutionMessage> GetTickStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The order books storage.</returns>
		IMarketDataStorage<QuoteChangeMessage> GetQuoteStorage(IMarketDataSerializer<QuoteChangeMessage> serializer);

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of orders log.</returns>
		IMarketDataStorage<ExecutionMessage> GetOrderLogStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the storage of level1 data.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of level1 data.</returns>
		IMarketDataStorage<Level1ChangeMessage> GetLevel1Storage(IMarketDataSerializer<Level1ChangeMessage> serializer);

		/// <summary>
		/// To get the storage of position changes data.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of position changes data.</returns>
		IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(IMarketDataSerializer<PositionChangeMessage> serializer);

		/// <summary>
		/// To get the candles storage for the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The candles storage.</returns>
		IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer);

		/// <summary>
		/// To get the transactions storage for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The transactions storage.</returns>
		IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>Market-data storage.</returns>
		IMarketDataStorage GetStorage(Type dataType, object arg, IMarketDataSerializer serializer);

		///// <summary>
		///// To get available candles types with parameters for the instrument.
		///// </summary>
		///// <param name="serializer">The serializer.</param>
		///// <returns>Available candles types with parameters.</returns>
		//IEnumerable<Tuple<Type, object[]>> GetCandleTypes(IMarketDataSerializer<CandleMessage> serializer);
	}

	/// <summary>
	/// The storage for the instrument.
	/// </summary>
	public class SecurityMarketDataDrive : ISecurityMarketDataDrive
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMarketDataDrive"/>.
		/// </summary>
		/// <param name="drive">The storage (database, file etc.).</param>
		/// <param name="security">Security.</param>
		public SecurityMarketDataDrive(IMarketDataDrive drive, Security security)
		{
			Drive = drive ?? throw new ArgumentNullException(nameof(drive));
			Security = security ?? throw new ArgumentNullException(nameof(security));
			SecurityId = security.ToSecurityId();
		}

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive { get; }

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; }

		private static StorageFormats ToFormat(IMarketDataSerializer serializer)
		{
			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			return serializer.GetType().GetGenericType(typeof(CsvMarketDataSerializer<>)) != null
				? StorageFormats.Csv
				: StorageFormats.Binary;
		}

		private IMarketDataStorageDrive GetStorageDrive<TMessage>(IMarketDataSerializer<TMessage> serializer, object arg = null)
			where TMessage : Message
		{
			return GetStorageDrive(serializer, typeof(TMessage), arg);
		}

		private IMarketDataStorageDrive GetStorageDrive(IMarketDataSerializer serializer, Type messageType, object arg = null)
		{
			return Drive.GetStorageDrive(SecurityId, DataType.Create(messageType, arg), ToFormat(serializer));
		}

		/// <inheritdoc />
		public SecurityId SecurityId { get; }

		private IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		/// <summary>
		/// Exchanges and trading boards provider.
		/// </summary>
		public IExchangeInfoProvider ExchangeInfoProvider
		{
			get => _exchangeInfoProvider;
			set => _exchangeInfoProvider = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTickStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TradeStorage(SecurityId, GetStorageDrive(serializer, ExecutionTypes.Tick), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteStorage(IMarketDataSerializer<QuoteChangeMessage> serializer)
		{
			return new MarketDepthStorage(SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetOrderLogStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new OrderLogStorage(SecurityId, GetStorageDrive(serializer, ExecutionTypes.OrderLog), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1Storage(IMarketDataSerializer<Level1ChangeMessage> serializer)
		{
			return new Level1Storage(SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(IMarketDataSerializer<PositionChangeMessage> serializer)
		{
			return new PositionChangeStorage(SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			if (!candleType.IsCandleMessage())
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			return typeof(CandleStorage<>).Make(candleType).CreateInstance<IMarketDataStorage<CandleMessage>>(SecurityId, arg, GetStorageDrive(serializer, candleType, arg), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TransactionStorage(SecurityId, GetStorageDrive(serializer, ExecutionTypes.Transaction), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage GetStorage(Type dataType, object arg, IMarketDataSerializer serializer)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (!dataType.IsSubclassOf(typeof(Message)))
				dataType = dataType.ToMessageType(ref arg);

			if (dataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						return GetTickStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					case ExecutionTypes.Transaction:
						return GetTransactionStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					case ExecutionTypes.OrderLog:
						return GetTransactionStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					default:
						throw new ArgumentOutOfRangeException(nameof(arg), arg, LocalizedStrings.Str1219);
				}
			}
			else if (dataType == typeof(Level1ChangeMessage))
				return GetLevel1Storage((IMarketDataSerializer<Level1ChangeMessage>)serializer);
			else if (dataType == typeof(QuoteChangeMessage))
				return GetQuoteStorage((IMarketDataSerializer<QuoteChangeMessage>)serializer);
			else if (dataType.IsCandleMessage())
				return GetCandleStorage(dataType, arg, (IMarketDataSerializer<CandleMessage>)serializer);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);
		}

		///// <summary>
		///// To get available candles types with parameters for the instrument.
		///// </summary>
		///// <param name="serializer">The serializer.</param>
		///// <returns>Available candles types with parameters.</returns>
		//public IEnumerable<Tuple<Type, object[]>> GetCandleTypes(IMarketDataSerializer<CandleMessage> serializer)
		//{
		//	return Drive.GetCandleTypes(Security.ToSecurityId(), ToFormat(serializer));
		//}
	}
}