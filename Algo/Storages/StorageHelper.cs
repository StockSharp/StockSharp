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

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Вспомогательный класс для работы с внешнем хранилищем.
	/// </summary>
	public static class StorageHelper
	{
		private sealed class RangeEnumerable<TData> : SimpleEnumerable<TData>, IEnumerableEx<TData>
		{
			[DebuggerDisplay("From {_from} Cur {_currDate} To {_to}")]
			private sealed class RangeEnumerator : IEnumerator<TData>
			{
				private DateTime _currDate;
				private readonly IMarketDataStorage<TData> _storage;
				private readonly DateTimeOffset _from;
				private readonly DateTimeOffset _to;
				private readonly Func<TData, DateTimeOffset> _getTime;
				private IEnumerator<TData> _current;

				private bool _checkBounds;
				private readonly Range<DateTime> _bounds;

				public RangeEnumerator(IMarketDataStorage<TData> storage, DateTimeOffset from, DateTimeOffset to, Func<TData, DateTimeOffset> getTime)
				{
					if (storage == null)
						throw new ArgumentNullException("storage");

					if (getTime == null)
						throw new ArgumentNullException("getTime");

					if (from > to)
						throw new ArgumentOutOfRangeException("from");

					_storage = storage;
					_from = from;
					_to = to;
					_getTime = getTime;
					_currDate = from.UtcDateTime.Date;

					_checkBounds = true; // проверяем нижнюю границу
					_bounds = new Range<DateTime>(from.UtcDateTime, to.UtcDateTime);
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
							var time = _getTime(Current);

							if (_bounds.Contains(time.UtcDateTime))
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
					_currDate = _from.UtcDateTime.Date;
				}

				public TData Current
				{
					get { return _current.Current; }
				}

				object IEnumerator.Current
				{
					get { return Current; }
				}
			}

			private readonly IMarketDataStorage<TData> _storage;
			private readonly DateTimeOffset _from;
			private readonly DateTimeOffset _to;

			public RangeEnumerable(IMarketDataStorage<TData> storage, DateTimeOffset from, DateTimeOffset to, Func<TData, DateTimeOffset> getTime)
				: base(() => new RangeEnumerator(storage, from, to, getTime))
			{
				_storage = storage;
				_from = from;
				_to = to;
			}

			private int? _count;

			int IEnumerableEx.Count
			{
				get
				{
					if (_count == null)
					{
						// TODO
						//if (_from.TimeOfDay != TimeSpan.Zero || _to.TimeOfDay != TimeSpan.Zero)
						//	throw new InvalidOperationException("Невозможно вычислить количество элементов для диапазона со временем. Можно использовать только диапазон по датами.");

						var count = 0;

						for (var i = _from; i <= _to; i += TimeSpan.FromDays(1))
							count += _storage.Load(i.UtcDateTime).Count;

						_count = count;
					}

					return (int)_count;
				}
			}
		}

		/// <summary>
		/// Получить хранилище свечек.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечи.</typeparam>
		/// <typeparam name="TArg">Тип параметра свечи.</typeparam>
		/// <param name="storageRegistry">Внешнее хранилище.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		public static IMarketDataStorage<Candle> GetCandleStorage<TCandle, TArg>(this IStorageRegistry storageRegistry, Security security, TArg arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
			where TCandle : Candle
		{
			return storageRegistry.ThrowIfNull().GetCandleStorage(typeof(TCandle), security, arg, drive, format);
		}

		/// <summary>
		/// Получить хранилище свечек.
		/// </summary>
		/// <param name="storageRegistry">Внешнее хранилище.</param>
		/// <param name="series">Серия свечек.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		public static IMarketDataStorage<Candle> GetCandleStorage(this IStorageRegistry storageRegistry, CandleSeries series, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			return storageRegistry.ThrowIfNull().GetCandleStorage(series.CandleType, series.Security, series.Arg, drive, format);
		}

		private static IStorageRegistry ThrowIfNull(this IStorageRegistry storageRegistry)
		{
			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			return storageRegistry;
		}

		internal static IEnumerable<Range<DateTimeOffset>> GetRanges<TValue>(this IMarketDataStorage<TValue> storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			var range = GetRange(storage, null, null);

			if (range == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return storage.Dates.Select(d => (DateTimeOffset)d).GetRanges(range.Min, range.Max);
		}

		/// <summary>
		/// Создать итерационный загрузчик маркет-данных для диапазона времени.
		/// </summary>
		/// <typeparam name="TData">Тип данных.</typeparam>
		/// <param name="storage">Хранилище маркет-данных.</param>
		/// <param name="from">Время начала, с которого необходимо загружать данные. Если значение не указано, то будут загружены данные с начальной даты <see cref="GetFromDate"/>.</param>
		/// <param name="to">Время окончания, до которого включительно необходимо загружать данные. Если значение не указано, то будут загружены данные до конечной даты <see cref="GetToDate"/> включительно.</param>
		/// <returns>Итерационный загрузчик маркет-данных.</returns>
		public static IEnumerableEx<TData> Load<TData>(this IMarketDataStorage<TData> storage, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			var range = GetRange(storage, from, to);

			return range == null
				? Enumerable.Empty<TData>().ToEx()
				: new RangeEnumerable<TData>(storage, range.Min, range.Max, ((IMarketDataStorageInfo<TData>)storage).GetTime);
		}

		/// <summary>
		/// Удалить маркет-данные из хранилища для заданного периода.
		/// </summary>
		/// <param name="storage">Хранилище маркет-данных.</param>
		/// <param name="from">Время начала, с которого необходимо удалять данные. Если значение не указано, то будут удалены данные с начальной даты <see cref="GetFromDate"/>.</param>
		/// <param name="to">Время окончания, до которого включительно необходимо удалять данные. Если значение не указано, то будут удалены данные до конечной даты <see cref="GetToDate"/> включительно.</param>
		public static void Delete(this IMarketDataStorage storage, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			var range = GetRange(storage, from, to);

			if (range == null)
				return;

			var info = (IMarketDataStorageInfo)storage;

			var max = range.Max.Date.EndOfDay();

			for (var date = range.Min; date <= max; date = date.AddDays(1))
			{
				if (date == range.Min)
				{
					var metaInfo = storage.GetMetaInfo(date.Date);

					if (metaInfo.FirstTime >= date.UtcDateTime && range.Max.Date != range.Min.Date)
					{
						storage.Delete(date.Date);
					}
					else
					{
						var data = storage.Load(date.Date).Cast<object>().ToList();
						data.RemoveWhere(d => info.GetTime(d) < range.Min);
						storage.Delete(data);
					}
				}
				else if (date.Date < range.Max.Date)
					storage.Delete(date.Date);
				else
				{
					var data = storage.Load(date.Date).Cast<object>().ToList();
					data.RemoveWhere(d => info.GetTime(d) > range.Max);
					storage.Delete(data);
				}
			}
		}

		internal static Range<DateTimeOffset> GetRange(this IMarketDataStorage storage, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			if (from > to)
				throw new ArgumentOutOfRangeException("to", to, LocalizedStrings.Str1014);

			var dates = storage.Dates.ToArray();

			if (dates.IsEmpty())
				return null;

			var first = dates.First();
			var last = dates.Last().EndOfDay();

			return new Range<DateTimeOffset>(first, last).Intersect(new Range<DateTimeOffset>((from ?? first).Truncate(), (to ?? last).Truncate()));
		}

		/// <summary>
		/// Получить начальную дату, с которой храняться маркет-данные в хранилище.
		/// </summary>
		/// <param name="storage">Хранилище маркет-данных.</param>
		/// <returns>Начальная дата. Если значение не инициализировано, значит хранилище пустое.</returns>
		public static DateTime? GetFromDate(this IMarketDataStorage storage)
		{
			return storage.Dates.FirstOr();
		}

		/// <summary>
		/// Получить конечную дату, по которую храняться маркет-данные в хранилище.
		/// </summary>
		/// <param name="storage">Хранилище маркет-данных.</param>
		/// <returns>Конечная дата. Если значение не инициализировано, значит хранилище пустое.</returns>
		public static DateTime? GetToDate(this IMarketDataStorage storage)
		{
			return storage.Dates.LastOr();
		}

		/// <summary>
		/// Получить все даты, для которых записаны маркет-данные за указанный диапазон.
		/// </summary>
		/// <param name="storage">Хранилище маркет-данных.</param>
		/// <param name="from">Время начала диапазона. Если значение не указано, то будут загружены данные с начальной даты <see cref="GetFromDate"/>.</param>
		/// <param name="to">Время окончания диапазона. Если значение не указано, то будут загружены данные до конечной даты <see cref="GetToDate"/> включительно.</param>
		/// <returns>Все доступные даты внутри диапазона.</returns>
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
		/// Сконвертировать строковое представление аргумента свечи в типизированное.
		/// </summary>
		/// <param name="type">Тип сообщения свечи.</param>
		/// <param name="str">Строковое представление аргумента.</param>
		/// <returns>Аргумент.</returns>
		public static object ToCandleArg(this Type type, string str)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (str.IsEmpty())
				throw new ArgumentNullException("str");

			if (type == typeof(TimeFrameCandleMessage))
			{
				return str.Replace("-", ":").To<TimeSpan>();
			}
			else if (type == typeof(TickCandleMessage))
			{
				return str.To<int>();
			}
			else if (type == typeof(VolumeCandleMessage))
			{
				return str.To<decimal>();
			}
			else if (type == typeof(RangeCandleMessage) || type == typeof(RenkoCandleMessage))
			{
				return str.To<Unit>();
			}
			else if (type == typeof(PnFCandleMessage))
			{
				return str.To<PnFArg>();
			}
			else
				throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.WrongCandleType);
		}
	}
}