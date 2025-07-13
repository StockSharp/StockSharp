namespace StockSharp.Algo.Storages;

using System.Diagnostics;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;

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

				GC.SuppressFinalize(this);
			}

			bool IEnumerator.MoveNext()
			{
				_current ??= _storage.Load(_currDate).GetEnumerator();

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
				throw new InvalidOperationException(LocalizedStrings.StartCannotBeMoreEnd.Put(from, to));

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

	internal static IEnumerable<Range<DateTimeOffset>> GetRanges<TMessage>(this IMarketDataStorage<TMessage> storage)
		where TMessage : Message
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		var range = GetRange(storage, null, null);

		if (range == null)
			return [];

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
			? []
			: new RangeEnumerable<TMessage>(storage, range.Min, range.Max, ((IMarketDataStorageInfo<TMessage>)storage).GetTime);
	}

	/// <summary>
	/// To delete market data from the storage for the specified time period.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <param name="from">The start time for data deleting. If the value is not specified, the data will be deleted starting from the date <see cref="GetFromDate"/>.</param>
	/// <param name="to">The end time, up to which the data shall be deleted. If the value is not specified, data will be deleted up to the end date <see cref="GetToDate"/>, inclusive.</param>
	/// <returns><see langword="true"/> if data was deleted, <see langword="false"/> data not exist for the specified period.</returns>
	public static bool Delete(this IMarketDataStorage storage, DateTimeOffset? from = null, DateTimeOffset? to = null)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		var range = GetRange(storage, from, to);

		if (range == null)
			return false;

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

		return true;
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
			LogManager.Instance?.Application.AddWarningLog(LocalizedStrings.ElementNotFoundParams.Put(first));
			return null;
		}

		if (lastInfo is null)
		{
			LogManager.Instance?.Application.AddWarningLog(LocalizedStrings.ElementNotFoundParams.Put(last));
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
	public static void ClearDatesCache(this IEnumerable<IMarketDataDrive> drives,
		Action<int, int> updateProgress, Func<bool> isCancelled, ILogReceiver logsReceiver)
	{
		if (drives == null)
			throw new ArgumentNullException(nameof(drives));

		if (updateProgress == null)
			throw new ArgumentNullException(nameof(updateProgress));

		if (isCancelled == null)
			throw new ArgumentNullException(nameof(isCancelled));

		if (logsReceiver == null)
			throw new ArgumentNullException(nameof(logsReceiver));

		var formats = Enumerator.GetValues<StorageFormats>().ToArray();
		var progress = 0;

		var marketDataDrives = drives as IMarketDataDrive[] ?? [.. drives];
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

				logsReceiver.AddInfoLog(LocalizedStrings.DatesCacheResetted, secId, drive.Path);
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

			_getStorage = tf => registry.GetTimeFrameCandleMessageStorage(securityId, tf, drive, format);
			_original = _getStorage(timeFrame);

			_timeFrame = timeFrame;

			var origin = timeFrame.TimeFrame();

			_compressors = GetSmallerTimeFrames().ToDictionary(tf => tf, tf => new BiggerTimeFrameCandleCompressor(new MarketDataMessage
			{
				SecurityId = securityId,
				DataType2 = origin,
				IsSubscribe = true,
			}, provider.Get(typeof(TimeFrameCandleMessage)), tf.TimeFrame()));

			_dataType = DataType.Create<TimeFrameCandleMessage>(_original.DataType.Arg);
		}

		private IEnumerable<TimeSpan> GetSmallerTimeFrames()
		{
			return _original.Drive.Drive
				.GetAvailableDataTypes(_original.SecurityId, ((IMarketDataStorage<CandleMessage>)this).Serializer.Format)
				.FilterTimeFrames()
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

				var compressor = _compressors.TryGetValue(s.DataType.GetTimeFrame());

				if (compressor == null)
					return [];

				return data.SelectMany(message => compressor.Process(message));
			}).Select(e => e.GetEnumerator()).ToList();

			if (enumerators.Count == 0)
				yield break;

			var tf = _dataType.GetTimeFrame();
			var toRemove = new List<IEnumerator<CandleMessage>>(enumerators.Count);

			while (true)
			{
				foreach (var enumerator in enumerators)
				{
					while (true)
					{
						if (!enumerator.MoveNext())
						{
							toRemove.Add(enumerator);
							break;
						}
						else if (enumerator.Current.OpenTime >= _nextCandleMinTime)
							break;
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

				var nextCandleCompressor = enumerators.OrderBy(e => e.Current.OpenTime).First();
				var candle = nextCandleCompressor.Current;

				while ((candle.OpenTime < _nextCandleMinTime || candle.State != CandleStates.Finished) && nextCandleCompressor.MoveNext())
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

		private class BuildableCandleInfo(IMarketDataMetaInfo info, TimeSpan tf) : IMarketDataMetaInfo
		{
			private readonly IMarketDataMetaInfo _info = info ?? throw new ArgumentNullException(nameof(info));

			public DateTime Date              => _info.Date;
			public bool IsOverride            => _info.IsOverride;

			public int Count             { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
			public object LastId         { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

			public decimal PriceStep     { get => _info.PriceStep;  set => throw new NotSupportedException(); }
			public decimal VolumeStep    { get => _info.VolumeStep; set => throw new NotSupportedException(); }

			public DateTime FirstTime
			{
				get => tf.GetCandleBounds(_info.FirstTime).Min.UtcDateTime;
				set => throw new NotSupportedException();
			}

			public DateTime LastTime
			{
				get => tf.GetCandleBounds(_info.LastTime).Max.UtcDateTime;
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
	/// The delimiter, replacing '/' in path for instruments with id like USD/EUR. Is equal to '__'.
	/// </summary>
	public const string SecuritySlashSeparator = "__";

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

	/// <summary>
	/// The delimiter, replacing '\\' in path for instruments with id like USD\\EUR. Is equal to '##BS##'.
	/// </summary>
	public const string SecurityBackslashSeparator = "##BS##";

	private static readonly CachedSynchronizedDictionary<string, string> _securitySeparators = new()
	{
		{ "/", SecuritySlashSeparator },
		{ "*", SecurityStarSeparator },
		{ ":", SecurityColonSeparator },
		{ "|", SecurityVerticalBarSeparator },
		{ "?", SecurityQuestionSeparator },
		{ "\\", SecurityBackslashSeparator },
	};

	// http://stackoverflow.com/questions/62771/how-check-if-given-string-is-legal-allowed-file-name-under-windows
	private static readonly string[] _reservedDos =
	[
		"CON", "PRN", "AUX", "NUL",
		"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
		"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
	];

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
			id = id[1..];

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
	public static (DateTimeOffset lastDate, long? left)? LoadMessages(this StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider, MarketDataMessage subscription, Action<Message> newOutMessage)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		if (candleBuilderProvider is null)
			throw new ArgumentNullException(nameof(candleBuilderProvider));

		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (newOutMessage is null)
			throw new ArgumentNullException(nameof(newOutMessage));

		(DateTimeOffset lastTime, long? left)? retVal = default;

		if (subscription.From == null)
			return retVal;

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

		var secId = subscription.SecurityId;

		if (subscription.DataType2 == DataType.Level1)
		{
			if (subscription.BuildMode != MarketDataBuildModes.Build)
			{
				if (settings.IsMode(StorageModes.Incremental))
					retVal = LoadMessages(GetStorage<Level1ChangeMessage>(secId, null), subscription, SendReply, SendOut);
			}
			else
			{
				if (subscription.BuildFrom == DataType.OrderLog)
				{
					var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToLevel1(subscription.DepthBuilder, subscription.RefreshSpeed ?? default), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
				else if (subscription.BuildFrom == DataType.MarketDepth)
				{
					var storage = GetStorage<QuoteChangeMessage>(secId, null);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToLevel1(), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
			}
		}
		else if (subscription.DataType2 == DataType.MarketDepth)
		{
			if (subscription.BuildMode != MarketDataBuildModes.Build)
			{
				if (settings.IsMode(StorageModes.Incremental))
					retVal = LoadMessages(GetStorage<QuoteChangeMessage>(secId, null), subscription, SendReply, SendOut);
			}
			else
			{
				if (subscription.BuildFrom == DataType.OrderLog)
				{
					var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToOrderBooks(subscription.DepthBuilder, subscription.RefreshSpeed ?? default, subscription.MaxDepth ?? int.MaxValue)
							.BuildIfNeed(), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
				else if (subscription.BuildFrom == DataType.Level1)
				{
					var storage = GetStorage<Level1ChangeMessage>(secId, null);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToOrderBooks(), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
			}
		}
		else if (subscription.DataType2 == DataType.Ticks)
		{
			if (subscription.BuildMode != MarketDataBuildModes.Build)
				retVal = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick), subscription, SendReply, SendOut);
			else
			{
				if (subscription.BuildFrom == DataType.OrderLog)
				{
					var storage = GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToTicks(), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
				else if (subscription.BuildFrom == DataType.Level1)
				{
					var storage = GetStorage<Level1ChangeMessage>(secId, null);

					var range = GetRange(storage, subscription);

					if (range != null)
					{
						retVal = LoadMessages(storage
							.Load(range.Item1, range.Item2)
							.ToTicks(), subscription.Count, subscription.TransactionId, SendReply, SendOut);
					}
				}
			}
		}
		else if (subscription.DataType2 == DataType.OrderLog)
		{
			retVal = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog), subscription, SendReply, SendOut);
		}
		else if (subscription.DataType2 == DataType.News)
		{
			retVal = LoadMessages(GetStorage<NewsMessage>(default, null), subscription, SendReply, SendOut);
		}
		else if (subscription.DataType2 == DataType.BoardState)
		{
			retVal = LoadMessages(GetStorage<BoardStateMessage>(default, null), subscription, SendReply, SendOut);
		}
		else if (subscription.DataType2.IsCandles)
		{
			(DateTimeOffset lastDate, long? left)? TryBuildCandles(MarketDataMessage subscription)
			{
				if (subscription.Count <= 0)
					return null;

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
					throw new ArgumentOutOfRangeException(nameof(subscription), buildFrom, LocalizedStrings.InvalidValue);

				var range = GetRange(storage, subscription);

				if (range != null && buildFrom == null)
					buildFrom = DataType.Ticks;
				else if (range == null && buildFrom == null)
				{
					storage = GetStorage<Level1ChangeMessage>(secId, null);
					range = GetRange(storage, subscription);

					if (range != null)
						buildFrom = DataType.Level1;
				}

				if (range is null)
					return null;

				var from = range.Item1;
				var to = range.Item2;

				var count = subscription.Count;
				var transId = subscription.TransactionId;

				var mdMsg = subscription.TypedClone();
				mdMsg.From = mdMsg.To = null;

				if (buildFrom == DataType.Ticks)
				{
					return LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
							.Load(from, to)
							.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), count, transId, SendReply, SendOut);
				}
				else if (buildFrom == DataType.OrderLog)
				{
					switch (subscription.BuildField)
					{
						case null:
						case Level1Fields.LastTradePrice:
							return LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
									.Load(from, to)
									.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), count, transId, SendReply, SendOut);

							// TODO
							//case Level1Fields.SpreadMiddle:
							//	lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
							//	    .Load(from, to)
							//		.ToOrderBooks(OrderLogBuilders.Plaza2.CreateBuilder(security.ToSecurityId()))
							//	    .ToCandles(mdMsg, false, exchangeInfoProvider: exchangeInfoProvider), transId, SendReply, SendOut);
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
									.Load(from, to)
									.ToTicks()
									.ToCandles(mdMsg, candleBuilderProvider: candleBuilderProvider), count, transId, SendReply, SendOut);

						case Level1Fields.BestBidPrice:
						case Level1Fields.BestAskPrice:
						case Level1Fields.SpreadMiddle:
							return LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
									.Load(from, to)
									.ToOrderBooks()
									.ToCandles(mdMsg, subscription.BuildField.Value, candleBuilderProvider: candleBuilderProvider), count, transId, SendReply, SendOut);
					}
				}
				else if (buildFrom == DataType.MarketDepth)
				{
					return LoadMessages(((IMarketDataStorage<QuoteChangeMessage>)storage)
							.Load(from, to)
							.ToCandles(mdMsg, subscription.BuildField ?? Level1Fields.SpreadMiddle, candleBuilderProvider: candleBuilderProvider), count, transId, SendReply, SendOut);
				}
				else
					throw new ArgumentOutOfRangeException(nameof(subscription), subscription.BuildFrom, LocalizedStrings.InvalidValue);

				return null;
			}

			if (subscription.BuildMode == MarketDataBuildModes.Build)
			{
				retVal = TryBuildCandles(subscription);
			}
			else
			{
				IMarketDataStorage<CandleMessage> storage;

				if (subscription.DataType2.IsTFCandles)
				{
					var tf = subscription.GetTimeFrame();

					IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(SecurityId securityId, TimeSpan timeFrame, bool allowBuildFromSmallerTimeFrame)
					{
						if (!allowBuildFromSmallerTimeFrame)
							return (IMarketDataStorage<CandleMessage>)settings.GetStorage(securityId, typeof(TimeFrameCandleMessage), timeFrame);

						return candleBuilderProvider.GetCandleMessageBuildableStorage(settings.StorageRegistry, securityId, timeFrame, settings.Drive, settings.Format);
					}

					storage = GetTimeFrameCandleMessageStorage(secId, tf, subscription.AllowBuildFromSmallerTimeFrame);
				}
				else
				{
					storage = (IMarketDataStorage<CandleMessage>)settings.GetStorage(secId, subscription.DataType2.MessageType, subscription.GetArg());
				}

				var filter = subscription.IsCalcVolumeProfile
					? (Func<CandleMessage, bool>)(c => c.PriceLevels != null)
					: null;

				retVal = LoadMessages(storage, subscription, SendReply, SendOut, filter);

				if (subscription.BuildMode == MarketDataBuildModes.LoadAndBuild && (retVal is null || retVal.Value.lastTime < subscription.To))
				{
					var buildSubscription = subscription;

					if (retVal is not null)
					{
						buildSubscription = buildSubscription.TypedClone();
						buildSubscription.From = retVal.Value.lastTime;
						buildSubscription.Count = retVal.Value.left;
					}

					var buildInfo = TryBuildCandles(buildSubscription);

					if (buildInfo is not null)
						retVal = buildInfo;
				}
			}
		}

		return retVal;
	}

	private static Tuple<DateTimeOffset, DateTimeOffset> GetRange(IMarketDataStorage storage, ISubscriptionMessage subscription)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (subscription.From is not DateTimeOffset from)
			return null;

		var last = storage.Dates.LastOr();

		if (last == null)
			return null;

		var to = subscription.To ?? last.Value.EndOfDay();

		var first = storage.Dates.First();

		if (from < first)
			from = first;

		if (from >= to)
			return null;

		return Tuple.Create(from, to);
	}

	private static (DateTimeOffset lastTime, long? left)? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, ISubscriptionMessage subscription, Action sendReply, Action<Message> newOutMessage, Func<TMessage, bool> filter = null)
		where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
	{
		var range = GetRange(storage, subscription);

		if (range == null)
			return null;

		var messages = storage.Load(range.Item1, range.Item2);

		if (subscription.Skip != default)
			messages = messages.Skip((int)subscription.Skip.Value);

		return LoadMessages(messages, subscription.Count, subscription.TransactionId, sendReply, newOutMessage, filter);
	}

	private static (DateTimeOffset lastTime, long? left)? LoadMessages<TMessage>(IEnumerable<TMessage> messages, long? count, long transactionId, Action sendReply, Action<Message> newOutMessage, Func<TMessage, bool> filter = null)
		where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
	{
		if (messages == null)
			throw new ArgumentNullException(nameof(messages));

		if (sendReply == null)
			throw new ArgumentNullException(nameof(sendReply));

		if (newOutMessage is null)
			throw new ArgumentNullException(nameof(newOutMessage));

		if (count <= 0)
		{
			return null;
			//throw new ArgumentOutOfRangeException(nameof(count), count, LocalizedStrings.InvalidValue);
		}

		if (filter != null)
			messages = messages.Where(filter);

		var left = count ?? long.MaxValue;

		DateTimeOffset? lastTime = null;

		foreach (var message in messages)
		{
			if (lastTime is null)
			{
				sendReply();
				lastTime = message.ServerTime;
			}
			else if (message.ServerTime < lastTime)
				continue;

			message.OriginalTransactionId = transactionId;
			message.SetSubscriptionIds(subscriptionId: transactionId);

			lastTime = message.ServerTime;

			newOutMessage(message);

			if (--left <= 0)
				break;
		}

		return lastTime is null ? null : (lastTime.Value, count is null ? null : left);
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
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);

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
			var argType = type.CreateInstance<CandleMessage>().ArgType;

			if (argType.Is<TimeSpan>())
				arg = arg1.To<TimeSpan>();
			else if (argType.Is<Unit>())
				arg = new Unit(arg2, (UnitTypes)arg1);
			else if (argType.Is<int>())
				arg = (int)arg1;
			else if (argType.Is<long>())
				arg = arg1;
			else if (argType.Is<decimal>())
				arg = arg2;
			else if (argType.Is<PnFArg>())
			{
				arg = new PnFArg
				{
					BoxSize = new Unit(arg2, (UnitTypes)arg1),
					ReversalAmount = arg3,
				};
			}
			else
				throw new ArgumentOutOfRangeException(nameof(messageType), argType, LocalizedStrings.InvalidValue);
		}
		else
			throw new ArgumentOutOfRangeException(nameof(messageType), type, LocalizedStrings.InvalidValue);

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

	/// <summary>
	/// Determines the specified path is network.
	/// </summary>
	/// <param name="path">Path.</param>
	/// <returns>Check result.</returns>
	public static bool IsNetworkPath(this string path)
	{
		if (path.IsEmpty())
			throw new ArgumentNullException(nameof(path));

		if (path.Length < 3)
			throw new ArgumentOutOfRangeException(nameof(path), path, LocalizedStrings.WrongPath);

		return !(path[0] >= 'A' && path[1] <= 'z' && path[1] == ':' && path[2] == '\\');
	}

	/// <summary>
	/// To get the candles storage for the specified instrument.
	/// </summary>
	/// <param name="registry"><see cref="IMessageStorageRegistry"/>.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">Candle arg.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The candles storage.</returns>
	public static IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(this IMessageStorageRegistry registry, SecurityId securityId, TimeSpan arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		=> registry.CheckOnNull(nameof(registry)).GetCandleMessageStorage(typeof(TimeFrameCandleMessage), securityId, arg, drive, format);

	/// <summary>
	/// To get the candles storage for the specified instrument.
	/// </summary>
	/// <param name="registry"><see cref="IMessageStorageRegistry"/>.</param>
	/// <param name="subscription"><see cref="Subscription"/>.</param>
	/// <param name="drive">The storage.</param>
	/// <param name="format">The format type.</param>
	/// <returns>The candles storage.</returns>
	public static IMarketDataStorage<CandleMessage> GetCandleMessageStorage(this IMessageStorageRegistry registry, Subscription subscription, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (subscription.SecurityId is null)
			throw new ArgumentException(nameof(subscription));

		var dt = subscription.DataType;

		return registry.GetCandleMessageStorage(dt.MessageType, subscription.SecurityId.Value, dt.Arg, drive, format);
	}
}