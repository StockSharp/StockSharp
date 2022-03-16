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
	using System.IO;
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

			for (var time = min; time <= max; time = time.AddDays(1))
			{
				var date = time.Date;

				if (from == null && to == null)
				{
					storage.Delete(date);
					continue;
				}
				else if (from == null && date < to.Value.UtcDateTime.Date)
				{
					storage.Delete(date);
					continue;
				}
				else if (to == null && date > from.Value.UtcDateTime.Date)
				{
					storage.Delete(date);
					continue;
				}

				if (time == min)
				{
					var metaInfo = storage.GetMetaInfo(date);

					if (metaInfo is null)
						continue;

					if (metaInfo.FirstTime >= time && max.Date != min.Date)
					{
						storage.Delete(date);
					}
					else
					{
						var data = storage.Load(date).ToList();
						data.RemoveWhere(d =>
						{
							var t = info.GetTime(d);
							return t.UtcDateTime < min || t > range.Max;
						});
						storage.Delete(data);
					}
				}
				else if (date < max.Date)
					storage.Delete(date);
				else
				{
					var data = storage.Load(date).ToList();
					data.RemoveWhere(d => info.GetTime(d) > range.Max);
					storage.Delete(data);
				}
			}
		}

		/// <summary>
		/// Get available date range for the specified storage.
		/// </summary>
		/// <param name="storage">Storage.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Date range</returns>
		public static Range<DateTimeOffset> GetRange(this IMarketDataStorage storage, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			if (from > to)
			{
				return null;
				//throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str1014.Put(from));
			}

			var dates = storage.Dates.ToArray();

			if (dates.IsEmpty())
				return null;

			var first = dates.First().UtcKind();
			var last = dates.Last().UtcKind();

			if (from > last.EndOfDay() || to < first)
				return null;

			var firstInfo = storage.GetMetaInfo(first);
			var lastInfo = first == last ? firstInfo : storage.GetMetaInfo(last);

			if (firstInfo is null)
			{
				GlobalLogReceiver.Instance.AddWarningLog(LocalizedStrings.Str1702Params.Put(first));
				return null;
			}

			if (lastInfo is null)
			{
				GlobalLogReceiver.Instance.AddWarningLog(LocalizedStrings.Str1702Params.Put(last));
				return null;
			}

			first = firstInfo.FirstTime;
			last = lastInfo.LastTime;

			// chech bounds again after time part loaded
			if (from > last || to < first)
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
								.GetStorageDrive(secId, dataType, format)
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
			securityStorage.DeleteById(securityId.ToSecurityId());
		}

		/// <summary>
		/// Delete instrument by identifier.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="securityId">Identifier.</param>
		public static void DeleteById(this ISecurityStorage securityStorage, SecurityId securityId)
		{
			if (securityStorage == null)
				throw new ArgumentNullException(nameof(securityStorage));

			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			securityStorage.DeleteBy(new SecurityLookupMessage { SecurityId = securityId });
		}

		private class CandleMessageBuildableStorage : IMarketDataStorage<CandleMessage>, IMarketDataStorageInfo<CandleMessage>
		{
			private readonly IMarketDataStorage<CandleMessage> _original;
			private readonly Func<TimeSpan, IMarketDataStorage<CandleMessage>> _getStorage;
			private readonly Dictionary<TimeSpan, BiggerTimeFrameCandleCompressor> _compressors;
			private readonly TimeSpan _timeFrame;
			private DateTime _prevDate;

			public CandleMessageBuildableStorage(CandleBuilderProvider provider, IStorageRegistry registry, SecurityId securityId, TimeSpan timeFrame, IMarketDataDrive drive, StorageFormats format)
			{
				if (registry == null)
					throw new ArgumentNullException(nameof(registry));

				_getStorage = tf => registry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), securityId, tf, drive, format);
				_original = _getStorage(timeFrame);

				_timeFrame = timeFrame;

				_compressors = GetSmallerTimeFrames().ToDictionary(tf => tf, tf => new BiggerTimeFrameCandleCompressor(new MarketDataMessage
				{
					SecurityId = securityId,
					DataType2 = DataType.TimeFrame(timeFrame),
					IsSubscribe = true,
				}, provider.Get(typeof(TimeFrameCandleMessage))));

				_dataType = DataType.Create(typeof(TimeFrameCandleMessage), _original.DataType.Arg);
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
				=> new[] { _original }.Concat(GetSmallerTimeFrames().Select(_getStorage));

			IEnumerable<DateTime> IMarketDataStorage.Dates => GetStorages().SelectMany(s => s.Dates).OrderBy().Distinct();

			private readonly DataType _dataType;
			DataType IMarketDataStorage.DataType => _dataType;

			SecurityId IMarketDataStorage.SecurityId => _original.SecurityId;

			IMarketDataStorageDrive IMarketDataStorage.Drive => _original.Drive;

			bool IMarketDataStorage.AppendOnlyNew
			{
				get => _original.AppendOnlyNew;
				set => _original.AppendOnlyNew = value;
			}

			int IMarketDataStorage.Save(IEnumerable<Message> data) => Save(data.Cast<CandleMessage>());

			void IMarketDataStorage.Delete(IEnumerable<Message> data) => Delete(data.Cast<CandleMessage>());

			void IMarketDataStorage.Delete(DateTime date) => _original.Delete(date);

			IEnumerable<Message> IMarketDataStorage.Load(DateTime date) => Load(date);

			IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
			{
				foreach (var storage in GetStorages())
				{
					var info = storage.GetMetaInfo(date);

					if (info != null)
						return new BuildableCandleInfo(info, _timeFrame);
				}

				return null;
			}

			IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<CandleMessage>)this).Serializer;

			private DateTimeOffset _nextCandleMinTime;

			public IEnumerable<CandleMessage> Load(DateTime date)
			{
				if (date <= _prevDate)
				{
					_compressors.Values.ForEach(c => c.Reset());
					_nextCandleMinTime = DateTimeOffset.MinValue;
				}

				_prevDate = date;

				var enumerators = GetStorages().Where(s => s.Dates.Contains(date)).Select(s =>
				{
					var data = s.Load(date);

					if (s == _original)
						return data;

					var compressor = _compressors.TryGetValue((TimeSpan)s.DataType.Arg);

					if (compressor == null)
						return Enumerable.Empty<CandleMessage>();

					return data.SelectMany(message => compressor.Process(message));
				}).Select(e => e.GetEnumerator()).ToList();

				if (enumerators.Count == 0)
					yield break;

				var tf = (TimeSpan)_dataType.Arg;
				var toRemove = new List<IEnumerator<CandleMessage>>(enumerators.Count);

				while (true)
				{
					foreach (var enumerator in enumerators)
					{
						while (enumerator.Current == null || enumerator.Current.OpenTime < _nextCandleMinTime)
						{
							if (!enumerator.MoveNext())
							{
								toRemove.Add(enumerator);
								break;
							}
						}
					}

					toRemove.ForEach(e =>
					{
						e.Dispose();
						enumerators.Remove(e);
					});

					toRemove.Clear();

					if (enumerators.Count == 0)
						yield break;

					var nextCandleCompressor = enumerators.OrderBy(e => e.Current!.OpenTime).First();
					var candle = nextCandleCompressor.Current;

					while ((candle!.OpenTime < _nextCandleMinTime || candle.State != CandleStates.Finished) && nextCandleCompressor.MoveNext())
					{
						/* compress until candle is finished OR no more data */
						candle = nextCandleCompressor.Current;
					}

					if (candle.State != CandleStates.Finished)
					{
						candle = candle.TypedClone();
						candle.State = CandleStates.Finished;
					}

					_nextCandleMinTime = candle.OpenTime + tf;
					yield return candle;
				}
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => _original.Serializer;

			public int Save(IEnumerable<CandleMessage> data) => _original.Save(data);

			public void Delete(IEnumerable<CandleMessage> data) => _original.Delete(data);

			DateTimeOffset IMarketDataStorageInfo<CandleMessage>.GetTime(CandleMessage data) => ((IMarketDataStorageInfo<CandleMessage>)_original).GetTime(data);

			DateTimeOffset IMarketDataStorageInfo.GetTime(object data) => ((IMarketDataStorageInfo<CandleMessage>)_original).GetTime(data);

			private class BuildableCandleInfo : IMarketDataMetaInfo
			{
				private readonly TimeSpan _tf;
				private readonly IMarketDataMetaInfo _info;

				public BuildableCandleInfo(IMarketDataMetaInfo info, TimeSpan tf)
				{
					_info = info ?? throw new ArgumentNullException(nameof(info));
					_tf = tf;
				}

				public DateTime Date              => _info.Date;
				public bool IsOverride            => _info.IsOverride;

				public int Count             { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
				public object LastId         { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

				public decimal PriceStep     { get => _info.PriceStep;  set => throw new NotSupportedException(); }
				public decimal VolumeStep    { get => _info.VolumeStep; set => throw new NotSupportedException(); }

				public DateTime FirstTime
				{
					get => _tf.GetCandleBounds(_info.FirstTime).Min.UtcDateTime;
					set => throw new NotSupportedException();
				}

				public DateTime LastTime
				{
					get => _tf.GetCandleBounds(_info.LastTime).Max.UtcDateTime;
					set => throw new NotSupportedException();
				}

				public void Write(Stream stream)  => throw new NotSupportedException();
				public void Read(Stream stream)   => throw new NotSupportedException();
			}
		}

		/// <summary>
		/// To get the candles storage for the specified instrument. The storage will build candles from smaller time-frames if original time-frames is not exist.
		/// </summary>
		/// <param name="provider">Candle builders provider.</param>
		/// <param name="registry">Market-data storage.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="timeFrame">Time-frame.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		public static IMarketDataStorage<CandleMessage> GetCandleMessageBuildableStorage(this CandleBuilderProvider provider, IStorageRegistry registry, SecurityId securityId, TimeSpan timeFrame, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return new CandleMessageBuildableStorage(provider, registry, securityId, timeFrame, drive, format);
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
					var dates = drive.GetStorageDrive(securityId, DataType.Create(candleType, arg), format).Dates;

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

		private static readonly SynchronizedDictionary<IMarketDataStorage, IMarketDataStorage> _convertedStorages = new();

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


		/// <summary>
		/// The delimiter, replacing '/' in path for instruments with id like USD/EUR. Is equal to '__'.
		/// </summary>
		public const string SecurityPairSeparator = "__";

		/// <summary>
		/// The delimiter, replacing '*' in the path for instruments with id like C.BPO-*@CANADIAN. Is equal to '##STAR##'.
		/// </summary>
		public const string SecurityStarSeparator = "##STAR##";
		// http://stocksharp.com/forum/yaf_postst4637_API-4-2-2-18--System-ArgumentException--Illegal-characters-in-path.aspx

		/// <summary>
		/// The delimiter, replacing ':' in the path for instruments with id like AA-CA:SPB@SPBEX. Is equal to '##COLON##'.
		/// </summary>
		public const string SecurityColonSeparator = "##COLON##";

		/// <summary>
		/// The delimiter, replacing '|' in the path for instruments with id like AA-CA|SPB@SPBEX. Is equal to '##VBAR##'.
		/// </summary>
		public const string SecurityVerticalBarSeparator = "##VBAR##";

		/// <summary>
		/// The delimiter, replacing '?' in the path for instruments with id like AA-CA?SPB@SPBEX. Is equal to '##QSTN##'.
		/// </summary>
		public const string SecurityQuestionSeparator = "##QSTN##";

		/// <summary>
		/// The delimiter, replacing first '.' in the path for instruments with id like .AA-CA@SPBEX. Is equal to '##DOT##'.
		/// </summary>
		public const string SecurityFirstDot = "##DOT##";

		///// <summary>
		///// The delimiter, replacing first '..' in the path for instruments with id like ..AA-CA@SPBEX. Is equal to '##DDOT##'.
		///// </summary>
		//public const string SecurityFirst2Dots = "##DDOT##";

		private static readonly CachedSynchronizedDictionary<string, string> _securitySeparators = new()
		{
			{ "/", SecurityPairSeparator },
			{ "*", SecurityStarSeparator },
			{ ":", SecurityColonSeparator },
			{ "|", SecurityVerticalBarSeparator },
			{ "?", SecurityQuestionSeparator },
		};

		// http://stackoverflow.com/questions/62771/how-check-if-given-string-is-legal-allowed-file-name-under-windows
		private static readonly string[] _reservedDos =
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		/// <summary>
		/// To convert the instrument identifier into the folder name, replacing reserved symbols.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <returns>Directory name.</returns>
		public static string SecurityIdToFolderName(this string id)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			var folderName = id;

			if (_reservedDos.Any(d => folderName.StartsWithIgnoreCase(d)))
				folderName = "_" + folderName;

			if (folderName.StartsWithIgnoreCase("."))
				folderName = SecurityFirstDot + folderName.Remove(0, 1);

			return _securitySeparators
				.CachedPairs
				.Aggregate(folderName, (current, pair) => current.Replace(pair.Key, pair.Value));
		}

		/// <summary>
		/// The inverse conversion from the <see cref="SecurityIdToFolderName"/> method.
		/// </summary>
		/// <param name="folderName">Directory name.</param>
		/// <returns>Security ID.</returns>
		public static string FolderNameToSecurityId(this string folderName)
		{
			if (folderName.IsEmpty())
				throw new ArgumentNullException(nameof(folderName));

			var id = folderName.ToUpperInvariant();

			if (id[0] == '_' && _reservedDos.Any(d => id.StartsWithIgnoreCase("_" + d)))
				id = id.Substring(1);

			if (id.StartsWithIgnoreCase(SecurityFirstDot))
				id = id.ReplaceIgnoreCase(SecurityFirstDot, ".");

			return _securitySeparators
				.CachedPairs
				.Aggregate(id, (current, pair) => current.ReplaceIgnoreCase(pair.Value, pair.Key));
		}

		/// <summary>
		/// Load messages.
		/// </summary>
		/// <param name="settings">Storage settings.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		/// <param name="subscription">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="newOutMessage">New message event.</param>
		/// <returns>Last date.</returns>
		public static DateTimeOffset? LoadMessages(this StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider, MarketDataMessage subscription, Action<Message> newOutMessage)
		{
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));

			if (candleBuilderProvider is null)
				throw new ArgumentNullException(nameof(candleBuilderProvider));

			if (subscription is null)
				throw new ArgumentNullException(nameof(subscription));

			if (newOutMessage is null)
				throw new ArgumentNullException(nameof(newOutMessage));

			void SendReply() => newOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = subscription.TransactionId });
			void SendOut(Message message)
			{
				message.OfflineMode = MessageOfflineModes.Ignore;
				newOutMessage(message);
			}

			IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
				where TMessage : Message
			{
				return (IMarketDataStorage<TMessage>)settings.GetStorage(securityId, typeof(TMessage), arg);
			}

			DateTimeOffset? lastTime = null;

			if (subscription.From == null && subscription.To == null)
				return lastTime;

			var secId = subscription.SecurityId;

			if (subscription.DataType2 == DataType.Level1)
			{
				if (subscription.BuildMode != MarketDataBuildModes.Build)
				{
					if (settings.IsMode(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<Level1ChangeMessage>(secId, null), subscription, TimeSpan.Zero, SendReply, SendOut);
				}
				else
				{
					if (subscription.BuildFrom == DataType.OrderLog)
					{
						var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToLevel1(subscription.DepthBuilder, subscription.RefreshSpeed ?? default), range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
					else if (subscription.BuildFrom == DataType.MarketDepth)
					{
						var storage = GetStorage<QuoteChangeMessage>(secId, null);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToLevel1(), range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
				}
			}
			else if (subscription.DataType2 == DataType.MarketDepth)
			{
				if (subscription.BuildMode != MarketDataBuildModes.Build)
				{
					if (settings.IsMode(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<QuoteChangeMessage>(secId, null), subscription, TimeSpan.Zero, SendReply, SendOut);
				}
				else
				{
					if (subscription.BuildFrom == DataType.OrderLog)
					{
						var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToOrderBooks(subscription.DepthBuilder, subscription.RefreshSpeed ?? default, subscription.MaxDepth ?? int.MaxValue)
								.BuildIfNeed(),
							range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
					else if (subscription.BuildFrom == DataType.Level1)
					{
						var storage = GetStorage<Level1ChangeMessage>(secId, null);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToOrderBooks(), range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
				}
			}
			else if (subscription.DataType2 == DataType.Ticks)
			{
				if (subscription.BuildMode != MarketDataBuildModes.Build)
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick), subscription, settings.DaysLoad, SendReply, SendOut);
				else
				{
					if (subscription.BuildFrom == DataType.OrderLog)
					{
						var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToTicks(), range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
					else if (subscription.BuildFrom == DataType.Level1)
					{
						var storage = GetStorage<Level1ChangeMessage>(secId, null);

						var range = GetRange(storage, subscription, TimeSpan.Zero);

						if (range != null)
						{
							lastTime = LoadMessages(storage
								.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
								.ToTicks(), range.Item1, subscription.TransactionId, SendReply, SendOut);
						}
					}
				}
			}
			else if (subscription.DataType2 == DataType.OrderLog)
			{
				lastTime = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog), subscription, settings.DaysLoad, SendReply, SendOut);
			}
			else if (subscription.DataType2 == DataType.News)
			{
				lastTime = LoadMessages(GetStorage<NewsMessage>(default, null), subscription, settings.DaysLoad, SendReply, SendOut);
			}
			else if (subscription.DataType2 == DataType.BoardState)
			{
				lastTime = LoadMessages(GetStorage<BoardStateMessage>(default, null), subscription, settings.DaysLoad, SendReply, SendOut);
			}
			else if (subscription.DataType2.IsCandles)
			{
				if (subscription.DataType2.MessageType == typeof(TimeFrameCandleMessage))
				{
					var tf = subscription.GetTimeFrame();

					DateTimeOffset? TryBuildCandles()
					{
						IMarketDataStorage storage;

						var buildFrom = subscription.BuildFrom;

						if (buildFrom == null || buildFrom == DataType.Ticks)
							storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick);
						else if (buildFrom == DataType.OrderLog)
							storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);
						else if (buildFrom == DataType.Level1)
							storage = GetStorage<Level1ChangeMessage>(secId, null);
						else if (buildFrom == DataType.MarketDepth)
							storage = GetStorage<QuoteChangeMessage>(secId, null);
						else
							throw new ArgumentOutOfRangeException(nameof(subscription), buildFrom, LocalizedStrings.Str1219);

						var range = GetRange(storage, subscription, TimeSpan.FromDays(2));

						if (range != null && buildFrom == null)
							buildFrom = DataType.Ticks;
						else if (range == null && buildFrom == null)
						{
							storage = GetStorage<Level1ChangeMessage>(secId, null);
							range = GetRange(storage, subscription, TimeSpan.FromDays(2));

							if (range != null)
								buildFrom = DataType.Level1;
						}

						if (range != null)
						{
							var mdMsg = subscription.TypedClone();
							mdMsg.From = mdMsg.To = null;

							if (buildFrom == DataType.Ticks)
							{
								return LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
												.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
												.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);
							}
							else if (buildFrom == DataType.OrderLog)
							{
								switch (subscription.BuildField)
								{
									case null:
									case Level1Fields.LastTradePrice:
										return LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
															.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
															.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);
											
									// TODO
									//case Level1Fields.SpreadMiddle:
									//	lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
									//	    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									//		.ToOrderBooks(OrderLogBuilders.Plaza2.CreateBuilder(security.ToSecurityId()))
									//	    .ToCandles(mdMsg, false, exchangeInfoProvider: exchangeInfoProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);
									//	break;
								}
							}
							else if (buildFrom == DataType.Level1)
							{
								switch (subscription.BuildField)
								{
									case null:
									case Level1Fields.LastTradePrice:
										return LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
															.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
															.ToTicks()
															.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);

									case Level1Fields.BestBidPrice:
									case Level1Fields.BestAskPrice:
									case Level1Fields.SpreadMiddle:
										return LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
															.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
															.ToOrderBooks()
															.ToCandles(mdMsg, subscription.BuildField.Value, candleBuilderProvider: candleBuilderProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);
								}
							}
							else if (buildFrom == DataType.MarketDepth)
							{
								return LoadMessages(((IMarketDataStorage<QuoteChangeMessage>)storage)
													.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
													.ToCandles(mdMsg, subscription.BuildField ?? Level1Fields.SpreadMiddle, candleBuilderProvider: candleBuilderProvider), range.Item1, subscription.TransactionId, SendReply, SendOut);
							}
							else
								throw new ArgumentOutOfRangeException(nameof(subscription), subscription.BuildFrom, LocalizedStrings.Str1219);
						}

						return null;
					}

					if (subscription.BuildMode == MarketDataBuildModes.Build)
					{
						lastTime = TryBuildCandles();
					}
					else
					{
						IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(SecurityId securityId, TimeSpan timeFrame, bool allowBuildFromSmallerTimeFrame)
						{
							if (!allowBuildFromSmallerTimeFrame)
								return (IMarketDataStorage<CandleMessage>)settings.GetStorage(securityId, typeof(TimeFrameCandleMessage), timeFrame);

							return candleBuilderProvider.GetCandleMessageBuildableStorage(settings.StorageRegistry, securityId, timeFrame, settings.Drive, settings.Format);
						}

						var filter = subscription.IsCalcVolumeProfile
							? (Func<CandleMessage, bool>)(c => c.PriceLevels != null)
							: null;

						lastTime = LoadMessages(GetTimeFrameCandleMessageStorage(secId, tf, subscription.AllowBuildFromSmallerTimeFrame), subscription, settings.DaysLoad, SendReply, SendOut, filter);

						if (lastTime == null && subscription.BuildMode == MarketDataBuildModes.LoadAndBuild)
							lastTime = TryBuildCandles();
					}
				}
				else
				{
					var storage = (IMarketDataStorage<CandleMessage>)settings.GetStorage(secId, subscription.DataType2.MessageType, subscription.GetArg());

					var range = GetRange(storage, subscription, settings.DaysLoad);

					if (range != null)
					{
						var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());
						lastTime = LoadMessages(messages, range.Item1, subscription.TransactionId, SendReply, SendOut);
					}
				}
			}

			return lastTime;
		}

		private static Tuple<DateTimeOffset, DateTimeOffset> GetRange(IMarketDataStorage storage, ISubscriptionMessage subscription, TimeSpan daysLoad)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			if (subscription is null)
				throw new ArgumentNullException(nameof(subscription));

			var last = storage.Dates.LastOr();

			if (last == null)
				return null;

			var to = subscription.To;

			if (to == null)
				to = last.Value;

			var from = subscription.From;

			if (from == null)
				from = to.Value - daysLoad;

			return Tuple.Create(from.Value, to.Value);
		}

		private static DateTimeOffset? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, ISubscriptionMessage subscription, TimeSpan daysLoad, Action sendReply, Action<Message> newOutMessage, Func<TMessage, bool> filter = null)
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			var range = GetRange(storage, subscription, daysLoad);

			if (range == null)
				return null;

			var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());

			if (subscription.Skip != default)
				messages = messages.Skip((int)subscription.Skip.Value);

			if (subscription.Count != default)
				messages = messages.Take((int)subscription.Count.Value);

			return LoadMessages(messages, range.Item1, subscription.TransactionId, sendReply, newOutMessage, filter);
		}

		private static DateTimeOffset LoadMessages<TMessage>(IEnumerable<TMessage> messages, DateTimeOffset lastTime, long transactionId, Action sendReply, Action<Message> newOutMessage, Func<TMessage, bool> filter = null)
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			if (sendReply == null)
				throw new ArgumentNullException(nameof(sendReply));

			if (filter != null)
				messages = messages.Where(filter);

			var replySent = false;

			foreach (var message in messages)
			{
				if (!replySent)
				{
					sendReply();
					replySent = true;
				}

				message.OriginalTransactionId = transactionId;
				message.SetSubscriptionIds(subscriptionId: transactionId);

				lastTime = message.ServerTime;

				newOutMessage(message);
			}

			return lastTime;
		}

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="registry">Market-data storage.</param>
		/// <param name="security">Security.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>Market-data storage.</returns>
		public static IMarketDataStorage GetStorage(this IStorageRegistry registry, Security security, DataType dataType, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return registry.GetStorage(security, dataType.MessageType, dataType.Arg, drive, format);
		}

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="registry">Market-data storage.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>Market-data storage.</returns>
		public static IMarketDataStorage GetStorage(this IStorageRegistry registry, SecurityId securityId, DataType dataType, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return registry.GetStorage(securityId, dataType.MessageType, dataType.Arg, drive, format);
		}

		/// <summary>
		/// Try build books by <see cref="OrderBookIncrementBuilder"/> in case of <paramref name="books"/> is incremental changes.
		/// </summary>
		/// <param name="books">Order books.</param>
		/// <param name="logs">Logs.</param>
		/// <returns>Order books.</returns>
		public static IEnumerable<QuoteChangeMessage> BuildIfNeed(this IEnumerable<QuoteChangeMessage> books, ILogReceiver logs = null)
		{
			if (books is null)
				throw new ArgumentNullException(nameof(books));

			var builders = new Dictionary<SecurityId, OrderBookIncrementBuilder>();

			foreach (var book in books)
			{
				if (book.State != null)
				{
					var builder = builders.SafeAdd(book.SecurityId, key => new OrderBookIncrementBuilder(key) { Parent = logs ?? GlobalLogReceiver.Instance });
					var change = builder.TryApply(book);

					if (change != null)
						yield return change;
				}
				else
					yield return book;
			}
		}

		/// <summary>
		/// To get the snapshot storage.
		/// </summary>
		/// <param name="registry">Snapshot storage registry.</param>
		/// <param name="dataType">Data type info.</param>
		/// <returns>The snapshot storage.</returns>
		public static ISnapshotStorage GetSnapshotStorage(this SnapshotRegistry registry, DataType dataType)
		{
			if (registry is null)
				throw new ArgumentNullException(nameof(registry));

			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return registry.GetSnapshotStorage(dataType.MessageType, dataType.Arg);
		}

		internal static (int messageType, long arg1, decimal arg2, int arg3) Extract(this DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			var messageType = (int)dataType.MessageType.ToMessageType();

			var arg1 = 0L;
			var arg2 = 0M;
			var arg3 = 0;

			if (dataType.Arg is ExecutionTypes execType)
				arg1 = (int)execType;
			else if (dataType.Arg is TimeSpan tf)
				arg1 = tf.Ticks;
			else if (dataType.Arg is Unit unit)
			{
				arg1 = (int)unit.Type;
				arg2 = unit.Value;
			}
			else if (dataType.Arg is int i)
				arg1 = i;
			else if (dataType.Arg is long l)
				arg1 = l;
			else if (dataType.Arg is decimal d)
				arg2 = d;
			else if (dataType.Arg is PnFArg pnf)
			{
				arg1 = (int)pnf.BoxSize.Type;
				arg2 = pnf.BoxSize.Value;
				arg3 = pnf.ReversalAmount;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);

			return (messageType, arg1, arg2, arg3);
		}

		internal static DataType ToDataType(this int messageType, long arg1, decimal arg2, int arg3)
		{
			var type = ((MessageTypes)messageType).ToMessageType();

			object arg;

			if (type == typeof(ExecutionMessage))
				arg = (ExecutionTypes)arg1;
			else if (type.IsCandleMessage())
			{
				var candleArg = type.CreateInstance<CandleMessage>().Arg;

				if (candleArg is TimeSpan)
					arg = arg1.To<TimeSpan>();
				else if (candleArg is Unit)
					arg = new Unit(arg2, (UnitTypes)arg1);
				else if (candleArg is int)
					arg = (int)arg1;
				else if (candleArg is long)
					arg = arg1;
				else if (candleArg is decimal)
					arg = arg2;
				else if (candleArg is PnFArg)
				{
					arg = new PnFArg
					{
						BoxSize = new Unit(arg2, (UnitTypes)arg1),
						ReversalAmount = arg3,
					};
				}
				else
					throw new ArgumentOutOfRangeException(nameof(messageType), candleArg, LocalizedStrings.Str1219);
			}
			else
				throw new ArgumentOutOfRangeException(nameof(messageType), type, LocalizedStrings.Str1219);

			return DataType.Create(type, arg);
		}

		/// <summary>
		/// Make association with adapter.
		/// </summary>
		/// <param name="provider">Message adapter's provider interface.</param>
		/// <param name="key">Key.</param>
		/// <param name="adapter">Adapter.</param>
		/// <returns><see langword="true"/> if the association is successfully changed, otherwise, <see langword="false"/>.</returns>
		public static bool SetAdapter<TKey>(this IMappingMessageAdapterProvider<TKey> provider, TKey key, IMessageAdapter adapter)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (adapter is null)
				throw new ArgumentNullException(nameof(adapter));

			return provider.SetAdapter(key, adapter.Id);
		}
	}
}