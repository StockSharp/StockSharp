#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: StorageHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Extension class for storage.
	/// </summary>
	public static class StorageHelper
	{
		private sealed class RangeEnumerable<TData> : SimpleEnumerable<TData>//, IEnumerableEx<TData>
			where TData : Message
		{
			[DebuggerDisplay("From {_from} Cur {_currDate} To {_to}")]
			private sealed class RangeEnumerator : IEnumerator<TData>
			{
				private DateTime _currDate;
				private readonly IMarketDataStorage<TData> _storage;
				private readonly DateTime _from;
				private readonly DateTime _to;
				private readonly Func<TData, DateTimeOffset> _getTime;
				private IEnumerator<TData> _current;

				private bool _checkBounds;
				private readonly Range<DateTime> _bounds;

				public RangeEnumerator(IMarketDataStorage<TData> storage, DateTimeOffset from, DateTimeOffset to, Func<TData, DateTimeOffset> getTime)
				{
					_storage = storage;
					_from = from.UtcDateTime;
					_to = to.UtcDateTime;
					_getTime = getTime;
					_currDate = from.UtcDateTime.Date;

					_checkBounds = true; // проверяем нижнюю границу
					_bounds = new Range<DateTime>(_from, _to);
				}

				void IDisposable.Dispose()
				{
					Reset();
				}

				bool IEnumerator.MoveNext()
				{
					if (_current == null)
					{
						_current = _storage.Load(_currDate).GetEnumerator();
					}

					while (true)
					{
						if (!_current.MoveNext())
						{
							_current.Dispose();

							var canMove = false;

							while (!canMove)
							{
								_currDate += TimeSpan.FromDays(1);

								if (_currDate > _to)
									break;

								_checkBounds = _currDate == _to.Date;

								_current = _storage.Load(_currDate).GetEnumerator();

								canMove = _current.MoveNext();
							}

							if (!canMove)
								return false;
						}

						if (!_checkBounds)
							break;

						do
						{
							var time = _getTime(Current).UtcDateTime;

							if (_bounds.Contains(time))
								return true;

							if (time > _to)
								return false;
						}
						while (_current.MoveNext());
					}

					return true;
				}

				public void Reset()
				{
					if (_current != null)
					{
						_current.Dispose();
						_current = null;
					}

					_checkBounds = true;
					_currDate = _from.Date;
				}

				public TData Current => _current.Current;

				object IEnumerator.Current => Current;
			}

			//private readonly IMarketDataStorage<TData> _storage;
			//private readonly DateTimeOffset _from;
			//private readonly DateTimeOffset _to;

			public RangeEnumerable(IMarketDataStorage<TData> storage, DateTimeOffset from, DateTimeOffset to, Func<TData, DateTimeOffset> getTime)
				: base(() => new RangeEnumerator(storage, from, to, getTime))
			{
				if (storage == null)
					throw new ArgumentNullException(nameof(storage));

				if (getTime == null)
					throw new ArgumentNullException(nameof(getTime));

				if (from > to)
					throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str1014.Put(from));

				//_storage = storage;
				//_from = from;
				//_to = to;
			}

			//private int? _count;

			//int IEnumerableEx.Count
			//{
			//	get
			//	{
			//		if (_count == null)
			//		{
			//			// TODO
			//			//if (_from.TimeOfDay != TimeSpan.Zero || _to.TimeOfDay != TimeSpan.Zero)
			//			//	throw new InvalidOperationException("Невозможно вычислить количество элементов для диапазона со временем. Можно использовать только диапазон по датами.");

			//			var count = 0;

			//			for (var i = _from; i <= _to; i += TimeSpan.FromDays(1))
			//				count += _storage.Load(i.UtcDateTime).Count;

			//			_count = count;
			//		}

			//		return (int)_count;
			//	}
			//}
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
		public static IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage<TCandle, TArg>(this IStorageRegistry storageRegistry, Security security, TArg arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
			where TCandle : Candle
		{
			return storageRegistry.ThrowIfNull().GetCandleStorage(typeof(TCandle), security, arg, drive, format);
		}

		/// <summary>
		/// To get the storage of candles.
		/// </summary>
		/// <param name="storageRegistry">The external storage.</param>
		/// <param name="series">Candles series.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		public static IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage(this IStorageRegistry storageRegistry, CandleSeries series, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return storageRegistry.ThrowIfNull().GetCandleStorage(series.CandleType, series.Security, series.Arg, drive, format);
		}

		private static IStorageRegistry ThrowIfNull(this IStorageRegistry storageRegistry)
		{
			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			return storageRegistry;
		}

		internal static IEnumerable<Range<DateTimeOffset>> GetRanges<TMessage>(this IMarketDataStorage<TMessage> storage)
			where TMessage : Message
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			var range = GetRange(storage, null, null);

			if (range == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return storage.Dates.Select(d => d.ApplyUtc()).GetRanges(range.Min, range.Max);
		}

		/// <summary>
		/// To create an iterative loader of market data for the time range.
		/// </summary>
		/// <typeparam name="TMessage">Data type.</typeparam>
		/// <param name="storage">Market-data storage.</param>
		/// <param name="from">The start time for data loading. If the value is not specified, data will be loaded from the starting time <see cref="GetFromDate"/>.</param>
		/// <param name="to">The end time for data loading. If the value is not specified, data will be loaded up to the <see cref="GetToDate"/> date, inclusive.</param>
		/// <returns>The iterative loader of market data.</returns>
		public static IEnumerable<TMessage> Load<TMessage>(this IMarketDataStorage<TMessage> storage, DateTimeOffset? from = null, DateTimeOffset? to = null)
			where TMessage : Message
		{
			var range = GetRange(storage, from, to);

			return range == null
				? Enumerable.Empty<TMessage>()
				: new RangeEnumerable<TMessage>(storage, range.Min, range.Max, ((IMarketDataStorageInfo<TMessage>)storage).GetTime);
		}

		/// <summary>
		/// To delete market data from the storage for the specified time period.
		/// </summary>
		/// <param name="storage">Market-data storage.</param>
		/// <param name="from">The start time for data deleting. If the value is not specified, the data will be deleted starting from the date <see cref="GetFromDate"/>.</param>
		/// <param name="to">The end time, up to which the data shall be deleted. If the value is not specified, data will be deleted up to the end date <see cref="GetToDate"/>, inclusive.</param>
		public static void Delete(this IMarketDataStorage storage, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			var range = GetRange(storage, from, to);

			if (range == null)
				return;

			var info = (IMarketDataStorageInfo)storage;

			var min = range.Min.UtcDateTime;
			var max = range.Max.UtcDateTime.EndOfDay();

			for (var date = min; date <= max; date = date.AddDays(1))
			{
				if (date == min)
				{
					var metaInfo = storage.GetMetaInfo(date.Date);

					if (metaInfo == null)
						continue;

					if (metaInfo.FirstTime >= date && max.Date != min.Date)
					{
						storage.Delete(date.Date);
					}
					else
					{
						var data = storage.Load(date.Date).ToList();
						data.RemoveWhere(d =>
						{
							var time = info.GetTime(d);
							return time.UtcDateTime < min || time > range.Max;
						});
						storage.Delete(data);
					}
				}
				else if (date.Date < max.Date)
					storage.Delete(date.Date);
				else
				{
					var data = storage.Load(date.Date).ToList();
					data.RemoveWhere(d => info.GetTime(d) > range.Max);
					storage.Delete(data);
				}
			}
		}

		internal static Range<DateTimeOffset> GetRange(this IMarketDataStorage storage, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			if (from > to)
			{
				return null;
				//throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str1014.Put(from));
			}

			var dates = storage.Dates.ToArray();

			if (dates.IsEmpty())
				return null;

			var first = dates.First().ApplyUtc();
			var last = dates.Last().EndOfDay().ApplyUtc();

			if (from > last)
				return null;

			var timePrecision = storage.Serializer.TimePrecision;
			return new Range<DateTimeOffset>(first, last).Intersect(new Range<DateTimeOffset>((from ?? first).StorageTruncate(timePrecision), (to ?? last).StorageTruncate(timePrecision)));
		}

		/// <summary>
		/// To get the start date for market data, stored in the storage.
		/// </summary>
		/// <param name="storage">Market-data storage.</param>
		/// <returns>The start date. If the value is not initialized, the storage is empty.</returns>
		public static DateTime? GetFromDate(this IMarketDataStorage storage)
		{
			return storage.Dates.FirstOr();
		}

		/// <summary>
		/// To get the end date for market data, stored in the storage.
		/// </summary>
		/// <param name="storage">Market-data storage.</param>
		/// <returns>The end date. If the value is not initialized, the storage is empty.</returns>
		public static DateTime? GetToDate(this IMarketDataStorage storage)
		{
			return storage.Dates.LastOr();
		}

		/// <summary>
		/// To get all dates for stored market data for the specified range.
		/// </summary>
		/// <param name="storage">Market-data storage.</param>
		/// <param name="from">The range start time. If the value is not specified, data will be loaded from the start date <see cref="GetFromDate"/>.</param>
		/// <param name="to">The range end time. If the value is not specified, data will be loaded up to the end date <see cref="GetToDate"/>, inclusive.</param>
		/// <returns>All available data within the range.</returns>
		public static IEnumerable<DateTime> GetDates(this IMarketDataStorage storage, DateTime? from, DateTime? to)
		{
			var dates = storage.Dates;

			if (from != null)
				dates = dates.Where(d => d >= from.Value);

			if (to != null)
				dates = dates.Where(d => d <= to.Value);

			return dates;
		}

		/// <summary>
		/// Read instrument by identifier.
		/// </summary>
		/// <param name="securities">Instrument storage collection.</param>
		/// <param name="securityId">Identifier.</param>
		/// <returns>Instrument.</returns>
		public static Security ReadBySecurityId(this IStorageEntityList<Security> securities, SecurityId securityId)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (securityId.IsDefault())
				throw new ArgumentNullException(nameof(securityId));

			return securities.ReadById(securityId.ToStringId());
		}

		internal static DateTimeOffset StorageTruncate(this DateTimeOffset time, TimeSpan precision)
		{
			var ticks = precision.Ticks;

			return ticks == 1 ? time : time.Truncate(ticks);
		}

		internal static DateTimeOffset StorageBinaryOldTruncate(this DateTimeOffset time)
		{
			return time.StorageTruncate(TimeSpan.FromMilliseconds(1));
		}

		/// <summary>
		/// Clear dates cache for storages.
		/// </summary>
		/// <param name="drives">Storage drives.</param>
		/// <param name="updateProgress">The handler through which a progress change will be passed.</param>
		/// <param name="isCancelled">The handler which returns an attribute of search cancel.</param>
		/// <param name="logsReceiver">Logs receiver.</param>
		public static void ClearDatesCache(this IEnumerable<IMarketDataDrive> drives, Action<int, int> updateProgress,
			Func<bool> isCancelled, ILogReceiver logsReceiver)
		{
			if (drives == null)
				throw new ArgumentNullException(nameof(drives));

			if (updateProgress == null)
				throw new ArgumentNullException(nameof(updateProgress));

			if (isCancelled == null)
				throw new ArgumentNullException(nameof(isCancelled));

			if (logsReceiver == null)
				throw new ArgumentNullException(nameof(logsReceiver));

			//var dataTypes = new[]
			//{
			//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Tick),
			//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.OrderLog),
			//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Order),
			//	Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Trade),
			//	Tuple.Create(typeof(QuoteChangeMessage), (object)null),
			//	Tuple.Create(typeof(Level1ChangeMessage), (object)null),
			//	Tuple.Create(typeof(NewsMessage), (object)null)
			//};

			var formats = Enumerator.GetValues<StorageFormats>().ToArray();
			var progress = 0;

			var marketDataDrives = drives as IMarketDataDrive[] ?? drives.ToArray();
			var iterCount = marketDataDrives.Sum(d => d.AvailableSecurities.Count()); // кол-во сбросов кэша дат

			updateProgress(progress, iterCount);

			foreach (var drive in marketDataDrives)
			{
				foreach (var secId in drive.AvailableSecurities)
				{
					foreach (var format in formats)
					{
						foreach (var dataType in drive.GetAvailableDataTypes(secId, format))
						{
							if (isCancelled())
								break;

							drive
								.GetStorageDrive(secId, dataType.MessageType, dataType.Arg, format)
								.ClearDatesCache();
						}
					}

					if (isCancelled())
						break;

					updateProgress(progress++, iterCount);

					logsReceiver.AddInfoLog(LocalizedStrings.Str2931Params, secId, drive.Path);
				}

				if (isCancelled())
					break;
			}
		}

		/// <summary>
		/// Delete instrument by identifier.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="securityId">Identifier.</param>
		public static void DeleteById(this ISecurityStorage securityStorage, string securityId)
		{
			if (securityStorage == null)
				throw new ArgumentNullException(nameof(securityStorage));

			if (securityId.IsEmpty())
				throw new ArgumentNullException(nameof(securityId));

			securityStorage.DeleteBy(new Security { Id = securityId });
		}

		private class CandleMessageBuildableStorage : IMarketDataStorage<CandleMessage>, IMarketDataStorageInfo<CandleMessage>
		{
			private readonly IMarketDataStorage<CandleMessage> _original;
			private readonly Func<TimeSpan, IMarketDataStorage<CandleMessage>> _getStorage;
			private readonly Dictionary<TimeSpan, BiggerTimeFrameCandleCompressor> _compressors;
			private readonly TimeSpan _timeFrame;
			private DateTime _prevDate;

			public CandleMessageBuildableStorage(IStorageRegistry registry, SecurityId securityId, TimeSpan timeFrame, IMarketDataDrive drive, StorageFormats format)
			{
				if (registry == null)
					throw new ArgumentNullException(nameof(registry));

				_getStorage = tf => registry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), securityId, tf, drive, format);
				_original = _getStorage(timeFrame);

				_timeFrame = timeFrame;

				_compressors = GetSmallerTimeFrames().ToDictionary(tf => tf, tf => new BiggerTimeFrameCandleCompressor(new MarketDataMessage
				{
					SecurityId = securityId,
					DataType = MarketDataTypes.CandleTimeFrame,
					Arg = timeFrame,
					IsSubscribe = true,
				}, new TimeFrameCandleBuilder(registry.ExchangeInfoProvider)));
			}

			private IEnumerable<TimeSpan> GetSmallerTimeFrames()
			{
				return _original.Drive.Drive
					.GetAvailableDataTypes(_original.SecurityId, ((IMarketDataStorage<CandleMessage>)this).Serializer.Format)
					.Where(t => t.MessageType == typeof(TimeFrameCandleMessage))
					.Select(t => (TimeSpan)t.Arg)
					.FilterSmallerTimeFrames(_timeFrame)
					.OrderByDescending();
			}

			private IEnumerable<IMarketDataStorage<CandleMessage>> GetStorages()
			{
				return new[] { _original }.Concat(GetSmallerTimeFrames().Select(_getStorage));
			}

			IEnumerable<DateTime> IMarketDataStorage.Dates => GetStorages().SelectMany(s => s.Dates).OrderBy().Distinct();

			Type IMarketDataStorage.DataType => typeof(TimeFrameCandleMessage);
			SecurityId IMarketDataStorage.SecurityId => _original.SecurityId;

			object IMarketDataStorage.Arg => _original.Arg;

			IMarketDataStorageDrive IMarketDataStorage.Drive => _original.Drive;

			bool IMarketDataStorage.AppendOnlyNew
			{
				get => _original.AppendOnlyNew;
				set => _original.AppendOnlyNew = value;
			}

			int IMarketDataStorage.Save(IEnumerable<Message> data) => ((IMarketDataStorage<CandleMessage>)this).Save(data);

			void IMarketDataStorage.Delete(IEnumerable<Message> data) => ((IMarketDataStorage<CandleMessage>)this).Delete(data);

			void IMarketDataStorage.Delete(DateTime date) => ((IMarketDataStorage<CandleMessage>)this).Delete(date);

			IEnumerable<Message> IMarketDataStorage.Load(DateTime date) => ((IMarketDataStorage<CandleMessage>)this).Load(date);

			IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date) =>  ((IMarketDataStorage<CandleMessage>)this).GetMetaInfo(date);

			IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<CandleMessage>)this).Serializer;

			IEnumerable<CandleMessage> IMarketDataStorage<CandleMessage>.Load(DateTime date)
			{
				if (date <= _prevDate)
					_compressors.Values.ForEach(c => c.Reset());

				_prevDate = date;

				var enumerators = GetStorages().Where(s => s.Dates.Contains(date)).Select(s =>
				{
					var data = s.Load(date);

					if (s == _original)
						return data;
					else
					{
						var compressor = _compressors.TryGetValue((TimeSpan)s.Arg);

						if (compressor == null)
							return Enumerable.Empty<CandleMessage>();

						return data.Compress(compressor, false);
					}
				}).Select(e => e.GetEnumerator()).ToList();

				if (enumerators.Count == 0)
					yield break;

				if (enumerators.Count == 1)
				{
					var enu = enumerators[0];

					while (enu.MoveNext())
						yield return enu.Current;

					enu.Dispose();
					yield break;
				}

				var needMove = enumerators.ToArray();

				while (true)
				{
					foreach (var enumerator in needMove)
					{
						if (enumerator.MoveNext())
							continue;

						enumerator.Dispose();
						enumerators.Remove(enumerator);
					}

					if (enumerators.Count == 0)
						yield break;

					var candle = enumerators.Select(e => e.Current).OrderBy(c => c.OpenTime).First();

					needMove = enumerators.Where(c => c.Current.OpenTime == candle.OpenTime).ToArray();

					yield return candle;
				}
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => _original.Serializer;

			int IMarketDataStorage<CandleMessage>.Save(IEnumerable<CandleMessage> data) => _original.Save(data);

			void IMarketDataStorage<CandleMessage>.Delete(IEnumerable<CandleMessage> data) => _original.Delete(data);

			DateTimeOffset IMarketDataStorageInfo<CandleMessage>.GetTime(CandleMessage data) => ((IMarketDataStorageInfo<CandleMessage>)_original).GetTime(data);

			DateTimeOffset IMarketDataStorageInfo.GetTime(object data) => ((IMarketDataStorageInfo<CandleMessage>)_original).GetTime(data);
		}

		/// <summary>
		/// To get the candles storage for the specified instrument. The storage will build candles from smaller time-frames if original time-frames is not exist.
		/// </summary>
		/// <param name="registry">Market-data storage.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="timeFrame">Time-frame.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		public static IMarketDataStorage<CandleMessage> GetCandleMessageBuildableStorage(this IStorageRegistry registry, SecurityId securityId, TimeSpan timeFrame, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return new CandleMessageBuildableStorage(registry, securityId, timeFrame, drive, format);
		}

		/// <summary>
		/// Get possible args for the specified candle type and instrument.
		/// </summary>
		/// <param name="drive">The storage (database, file etc.).</param>
		/// <param name="format">Format type.</param>
		/// <param name="candleType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Possible args.</returns>
		public static IEnumerable<object> GetCandleArgs(this IMarketDataDrive drive, StorageFormats format, Type candleType, SecurityId securityId = default, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			var dataTypes = drive.GetAvailableDataTypes(securityId, format);

			var args = new HashSet<object>();

			foreach (var dataType in dataTypes.Where(t => t.MessageType == candleType))
			{
				var arg = dataType.Arg;

				if (securityId.IsDefault())
					args.Add(arg);
				else if (from == null && to == null)
					args.Add(arg);
				else
				{
					var dates = drive.GetStorageDrive(securityId, candleType, arg, format).Dates;
					
					if (from != null)
						dates = dates.Where(d => d >= from.Value);

					if (to != null)
						dates = dates.Where(d => d <= to.Value);

					if (dates.Any())
						args.Add(arg);
				}
			}

			return args.OrderBy().ToArray();
		}

		private class ConvertableStorage<TMessage, TEntity> : IEntityMarketDataStorage<TEntity, TMessage>, IMarketDataStorageInfo<TMessage>
			where TMessage : Message
		{
			private readonly Security _security;
			private readonly IMarketDataStorage<TMessage> _messageStorage;
			private readonly IExchangeInfoProvider _exchangeInfoProvider;
			private readonly Func<TEntity, TMessage> _toMessage;

			public ConvertableStorage(Security security, IMarketDataStorage<TMessage> messageStorage, IExchangeInfoProvider exchangeInfoProvider, Func<TEntity, TMessage> toMessage)
			{
				_security = security;
				_messageStorage = messageStorage ?? throw new ArgumentNullException(nameof(messageStorage));
				_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
				_toMessage = toMessage ?? throw new ArgumentNullException(nameof(toMessage));
			}

			IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
			{
				return _messageStorage.GetMetaInfo(date);
			}

			IMarketDataSerializer IMarketDataStorage.Serializer => _messageStorage.Serializer;

			IMarketDataSerializer<TMessage> IMarketDataStorage<TMessage>.Serializer => throw new NotSupportedException();

			IEnumerable<DateTime> IMarketDataStorage.Dates => _messageStorage.Dates;

			Type IMarketDataStorage.DataType => _messageStorage.DataType;

			SecurityId IMarketDataStorage.SecurityId => _messageStorage.SecurityId;

			object IMarketDataStorage.Arg => _messageStorage.Arg;

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
				return ((IMarketDataStorage<TMessage>)this).Load(date).ToEntities<TMessage, TEntity>(_security, _exchangeInfoProvider);
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

		private static readonly SynchronizedDictionary<IMarketDataStorage, IMarketDataStorage> _convertedStorages = new SynchronizedDictionary<IMarketDataStorage, IMarketDataStorage>();

		/// <summary>
		/// Convert message storage to entity.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <typeparam name="TEntity">Entity type.</typeparam>
		/// <param name="storage">Message storage.</param>
		/// <param name="security">Security.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Entity storage.</returns>
		public static IEntityMarketDataStorage<TEntity, TMessage> ToEntityStorage<TMessage, TEntity>(this IMarketDataStorage<TMessage> storage, Security security, IExchangeInfoProvider exchangeInfoProvider = null)
			where TMessage : Message
		{
			Func<TEntity, TMessage> toMessage;

			if (typeof(TEntity) == typeof(MarketDepth))
			{
				Func<MarketDepth, QuoteChangeMessage> converter = MessageConverterHelper.ToMessage;
				toMessage = converter.To<Func<TEntity, TMessage>>();
			}
			else if (typeof(TEntity) == typeof(Trade))
			{
				Func<Trade, ExecutionMessage> converter = MessageConverterHelper.ToMessage;
				toMessage = converter.To<Func<TEntity, TMessage>>();
			}
			else if (typeof(TEntity) == typeof(OrderLogItem))
			{
				Func<OrderLogItem, ExecutionMessage> converter = MessageConverterHelper.ToMessage;
				toMessage = converter.To<Func<TEntity, TMessage>>();
			}
			else if (typeof(TEntity) == typeof(News))
			{
				Func<News, NewsMessage> converter = MessageConverterHelper.ToMessage;
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
				Func<Order, ExecutionMessage> converter = MessageConverterHelper.ToMessage;
				toMessage = converter.To<Func<TEntity, TMessage>>();
			}
			else if (typeof(TEntity) == typeof(MyTrade))
			{
				Func<MyTrade, ExecutionMessage> converter = MessageConverterHelper.ToMessage;
				toMessage = converter.To<Func<TEntity, TMessage>>();
			}
			else if (typeof(TEntity) == typeof(Candle) || typeof(TEntity).IsCandle())
			{
				Func<Candle, CandleMessage> converter = MessageConverterHelper.ToMessage;

				if (typeof(TEntity) == typeof(Candle) && typeof(TMessage) == typeof(CandleMessage))
					toMessage = converter.To<Func<TEntity, TMessage>>();
				else
					toMessage = e => converter(e.To<Candle>()).To<TMessage>();
			}
			else
				throw new ArgumentOutOfRangeException(nameof(TEntity), typeof(TEntity), LocalizedStrings.Str1219);

			return (IEntityMarketDataStorage<TEntity, TMessage>)_convertedStorages.SafeAdd(storage, key => new ConvertableStorage<TMessage, TEntity>(security, storage, exchangeInfoProvider ?? new InMemoryExchangeInfoProvider(), toMessage));
		}
	}
}