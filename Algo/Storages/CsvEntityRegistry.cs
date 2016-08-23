namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The CSV storage of trading objects.
	/// </summary>
	public class CsvEntityRegistry : IEntityRegistry
	{
		private class FakeStorage : IStorage
		{
			public long GetCount<TEntity>()
			{
				return 0;
			}

			public TEntity Add<TEntity>(TEntity entity)
			{
				Added?.Invoke(entity);
				return entity;
			}

			public TEntity GetBy<TEntity>(SerializationItemCollection by)
			{
				throw new NotSupportedException();
			}

			public TEntity GetById<TEntity>(object id)
			{
				throw new NotSupportedException();
			}

			public IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction)
			{
				throw new NotSupportedException();
			}

			public TEntity Update<TEntity>(TEntity entity)
			{
				Updated?.Invoke(entity);
				return entity;
			}

			public void Remove<TEntity>(TEntity entity)
			{
				Removed?.Invoke(entity);
			}

			public void Clear<TEntity>()
			{
			}

			public void ClearCache()
			{
			}

			public BatchContext BeginBatch()
			{
				return new BatchContext(this);
			}

			public void CommitBatch()
			{
			}

			public void EndBatch()
			{
			}

			public event Action<object> Added;

			public event Action<object> Updated;

			public event Action<object> Removed;
		}

		private abstract class CsvEntityList<T> : SynchronizedList<T>, IStorageEntityList<T>
			where T : class
		{
			private readonly string _fileName;
			private readonly Encoding _encoding;

			private readonly SynchronizedDictionary<object, object> _serializers = new SynchronizedDictionary<object, object>();
			private readonly CachedSynchronizedDictionary<object, T> _items = new CachedSynchronizedDictionary<object, T>();
			private readonly List<T> _addedItems = new List<T>();
			private readonly SyncObject _syncRoot = new SyncObject();

			private bool _isChanged;
			private bool _isFullChanged;

			protected CsvEntityRegistry Registry { get; }

			protected CsvEntityList(CsvEntityRegistry registry, string fileName, Encoding encoding)
			{
				if (registry == null)
					throw new ArgumentNullException(nameof(registry));
				
				if (fileName == null)
					throw new ArgumentNullException(nameof(fileName));

				if (encoding == null)
					throw new ArgumentNullException(nameof(encoding));

				Registry = registry;

				_fileName = System.IO.Path.Combine(Registry.Path, fileName);
				_encoding = encoding;

				ReadItems();
			}

			#region IStorageEntityList

			public DelayAction DelayAction { get; set; }

			public T ReadById(object id)
			{
				return _items.TryGetValue(id);
			}

			public IEnumerable<T> ReadLasts(int count)
			{
				return _items.CachedValues.Skip(Count - count).Take(count);
			}

			public void Save(T entity)
			{
				var key = GetKey(entity);
				var item = _items.TryGetValue(key);

				if (item == null)
				{
					Add(entity);
				}
				else
					Write();
			}

			#endregion

			protected abstract object GetKey(T item);

			protected abstract void Write(CsvFileWriter writer, T data);

			protected abstract T Read(FastCsvReader reader);

			protected override void OnAdded(T item)
			{
				base.OnAdded(item);

				if (_items.TryAdd(GetKey(item), item))
					Write(item);
			}

			protected override void OnRemoved(T item)
			{
				base.OnRemoved(item);

				_items.Remove(GetKey(item));
				Write();
			}

			protected override void OnCleared()
			{
				base.OnCleared();

				_items.Clear();
				Write();
			}

			private void ReadItems()
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
					{
						var reader = new FastCsvReader(stream, _encoding);

						while (reader.NextLine())
						{
							var item = Read(reader);
							var key = GetKey(item);

							_items.Add(key, item);
							Add(item);
						}
					}

				});
			}

			private void Write()
			{
				lock (_syncRoot)
				{
					_isChanged = true;
					_isFullChanged = true;
				}

				Registry.TryCreateTimer();
			}

			private void Write(T entity)
			{
				lock (_syncRoot)
				{
					_isChanged = true;
					_addedItems.Add(entity);
				}

				Registry.TryCreateTimer();
			}

			public bool Flush()
			{
				bool isChanged;
				bool isFullChanged;

				var addedItems = ArrayHelper.Empty<T>();

				lock (_syncRoot)
				{
					isChanged = _isChanged;
					isFullChanged = _isFullChanged;

					_isChanged = false;

					if (!isChanged)
					{
						_isFullChanged = false;
						addedItems = _addedItems.CopyAndClear();
					}
				}

				if (isChanged)
					return false;

				if (isFullChanged)
				{
					Write(_items.CachedValues, false, true);
				}
				else if (addedItems.Length > 0)
				{
					Write(addedItems, true, false);
				}

				return true;
			}

			private void Write(IEnumerable<T> items, bool append, bool clear)
			{
				using (var stream = new FileStream(_fileName, FileMode.OpenOrCreate))
				{
					if (clear)
						stream.SetLength(0);

					if (append)
						stream.Position = stream.Length;

					using (var writer = new CsvFileWriter(stream, _encoding))
					{
						foreach (var item in items)
							Write(writer, item);
					}
				}
			}

			protected string Serialize<TItem>(TItem item)
			{
				if (item == null)
					return null;

				var serializer = (XmlSerializer<TItem>)_serializers.SafeAdd(typeof(TItem), k => new XmlSerializer<TItem>());

				using (var stream = new MemoryStream())
				{
					serializer.Serialize(item, stream);
					return Encoding.UTF8.GetString(stream.ToArray()).Replace(Environment.NewLine, string.Empty).Replace("\"", "'");
				}
			}

			protected TItem Deserialize<TItem>(string value)
				where TItem : class
			{
				if (value.IsEmpty())
					return null;

				var serializer = (XmlSerializer<TItem>)_serializers.SafeAdd(typeof(TItem), k => new XmlSerializer<TItem>());
				var bytes = Encoding.UTF8.GetBytes(value.Replace("'", "\""));

				using (var stream = new MemoryStream(bytes))
					return serializer.Deserialize(stream);
			}
		}

		sealed class ExchangeCsvList : CsvEntityList<Exchange>
		{
			public ExchangeCsvList(CsvEntityRegistry registry)
				: base(registry, "exchange.csv", Encoding.UTF8)
			{
			}

			protected override object GetKey(Exchange item)
			{
				return item.Name;
			}

			protected override Exchange Read(FastCsvReader reader)
			{
				var board = new Exchange
				{
					Name = reader.ReadString(),
					CountryCode = reader.ReadNullableEnum<CountryCodes>(),
					EngName = reader.ReadString(),
					RusName = reader.ReadString(),
					ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())
				};

				return board;
			}

			protected override void Write(CsvFileWriter writer, Exchange data)
			{
				writer.WriteRow(new[]
				{
					data.Name,
					data.CountryCode.To<string>(),
					data.EngName,
					data.RusName,
					Serialize(data.ExtensionInfo)
				});
			}
		}

		sealed class ExchangeBoardCsvList : CsvEntityList<ExchangeBoard>
		{
			private const string _timeSpanFormat = "hh\\:mm\\:ss";

			public ExchangeBoardCsvList(CsvEntityRegistry registry)
				: base(registry, "exchangeboard.csv", Encoding.UTF8)
			{
			}

			protected override object GetKey(ExchangeBoard item)
			{
				return item.Code;
			}

			protected override ExchangeBoard Read(FastCsvReader reader)
			{
				var board = new ExchangeBoard
				{
					Code = reader.ReadString(),
					Exchange = Registry.Exchanges.ReadById(reader.ReadString()),
					ExpiryTime = reader.ReadTimeSpan(_timeSpanFormat),
					IsSupportAtomicReRegister = reader.ReadBool(),
					IsSupportMarketOrders = reader.ReadBool(),
					TimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.ReadString()),
					WorkingTime =
					{
						Periods = Deserialize<List<WorkingTimePeriod>>(reader.ReadString()),
						SpecialWorkingDays = Deserialize<List<DateTime>>(reader.ReadString()),
						SpecialHolidays = Deserialize<List<DateTime>>(reader.ReadString())
					},
					ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())
				};

				return board;
			}

			protected override void Write(CsvFileWriter writer, ExchangeBoard data)
			{
				writer.WriteRow(new[]
				{
					data.Code,
					data.Exchange.Name,
					data.ExpiryTime.ToString(_timeSpanFormat),
					data.IsSupportAtomicReRegister.To<string>(),
					data.IsSupportMarketOrders.To<string>(),
					data.TimeZone.Id,
					Serialize(data.WorkingTime.Periods),
					Serialize(data.WorkingTime.SpecialWorkingDays),
					Serialize(data.WorkingTime.SpecialHolidays),
					Serialize(data.ExtensionInfo)
				});
			}
		}

		sealed class SecurityCsvList : CsvEntityList<Security>, IStorageSecurityList
		{
			private const string _dateTimeFormat = "dd.MM.yyyy HH:mm:ss";

			public SecurityCsvList(CsvEntityRegistry registry)
				: base(registry, "security.csv", Encoding.UTF8)
			{
				((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
				((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
			}

			#region IStorageSecurityList

			public void Dispose()
			{
			}

			private Action<IEnumerable<Security>> _added;

			event Action<IEnumerable<Security>> ISecurityProvider.Added
			{
				add { _added += value; }
				remove { _added -= value; }
			}

			private Action<IEnumerable<Security>> _removed;

			event Action<IEnumerable<Security>> ISecurityProvider.Removed
			{
				add { _removed += value; }
				remove { _removed -= value; }
			}

			public IEnumerable<Security> Lookup(Security criteria)
			{
				if (criteria.IsLookupAll())
					return ToArray();

				if (criteria.Id.IsEmpty())
					return this.Filter(criteria);

				var security = ReadById(criteria.Id);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}

			public object GetNativeId(Security security)
			{
				return null;
			}

			public void Delete(Security security)
			{
				Remove(security);
			}

			public void DeleteBy(Security criteria)
			{
				this.Filter(criteria).ForEach(s => Remove(s));
			}

			public IEnumerable<string> GetSecurityIds()
			{
				return this.Select(s => s.Id);
			}

			#endregion

			#region CsvEntityList

			protected override object GetKey(Security item)
			{
				return item.Id;
			}

			protected override Security Read(FastCsvReader reader)
			{
				var security = new Security
				{
					Id = reader.ReadString(),
					Name = reader.ReadString(),
					Code = reader.ReadString(),
					Class = reader.ReadString(),
					ShortName = reader.ReadString(),
					Board = Registry.ExchangeBoards.ReadById(reader.ReadString()),
					UnderlyingSecurityId = reader.ReadString(),
					PriceStep = reader.ReadNullableDecimal(),
					VolumeStep = reader.ReadNullableInt(),
					Multiplier = reader.ReadNullableInt(),
					Decimals = reader.ReadNullableInt(),
					Type = reader.ReadNullableEnum<SecurityTypes>(),
					ExpiryDate = reader.ReadNullableDateTime(_dateTimeFormat),
					SettlementDate = reader.ReadNullableDateTime(_dateTimeFormat),
					Strike = reader.ReadNullableInt(),
					OptionType = reader.ReadNullableEnum<OptionTypes>(),
					Currency = reader.ReadNullableEnum<CurrencyTypes>(),
					ExternalId = new SecurityExternalId
					{
						Sedol = reader.ReadString(),
						Cusip = reader.ReadString(),
						Isin = reader.ReadString(),
						Ric = reader.ReadString(),
						Bloomberg = reader.ReadString(),
						IQFeed = reader.ReadString(),
						InteractiveBrokers = reader.ReadNullableInt(),
						Plaza = reader.ReadString()
					},
					ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())
				};

				return security;
			}

			protected override void Write(CsvFileWriter writer, Security data)
			{
				writer.WriteRow(new[]
				{
					data.Id,
					data.Name,
					data.Code,
					data.Class,
					data.ShortName,
					data.Board.Code,
					data.UnderlyingSecurityId,
					data.PriceStep.To<string>(),
					data.VolumeStep.To<string>(),
					data.Multiplier.To<string>(),
					data.Decimals.To<string>(),
					data.Type.To<string>(),
					data.ExpiryDate?.ToString(_dateTimeFormat),
					data.SettlementDate?.ToString(_dateTimeFormat),
					data.Strike.To<string>(),
					data.OptionType.To<string>(),
					data.Currency.To<string>(),
					data.ExternalId.Sedol,
					data.ExternalId.Cusip,
					data.ExternalId.Isin,
					data.ExternalId.Ric,
					data.ExternalId.Bloomberg,
					data.ExternalId.IQFeed,
					data.ExternalId.InteractiveBrokers.To<string>(),
					data.ExternalId.Plaza,
					Serialize(data.ExtensionInfo)
				});
			}

			#endregion
		}

		sealed class PortfolioCsvList : CsvEntityList<Portfolio>
		{
			private const string _dateTimeFormat = "dd.MM.yyyy HH:mm:ss";

			public PortfolioCsvList(CsvEntityRegistry registry)
				: base(registry, "portfolio.csv", Encoding.UTF8)
			{
			}

			protected override object GetKey(Portfolio item)
			{
				return item.Name;
			}

			protected override Portfolio Read(FastCsvReader reader)
			{
				var portfolio = new Portfolio
				{
					Name = reader.ReadString(),
					Board = Registry.ExchangeBoards.ReadById(reader.ReadString()),
					Leverage = reader.ReadNullableDecimal(),
					BeginValue = reader.ReadNullableDecimal(),
					CurrentValue = reader.ReadNullableDecimal(),
					BlockedValue = reader.ReadNullableDecimal(),
					VariationMargin = reader.ReadNullableDecimal(),
					Commission = reader.ReadNullableDecimal(),
					Currency = reader.ReadNullableEnum<CurrencyTypes>(),
					State = reader.ReadNullableEnum<PortfolioStates>(),
					LastChangeTime = reader.ReadDateTime(_dateTimeFormat),
					LocalTime = reader.ReadDateTime(_dateTimeFormat)
				};

				return portfolio;
			}

			protected override void Write(CsvFileWriter writer, Portfolio data)
			{
				writer.WriteRow(new[]
				{
					data.Name,
					data.Board.Code,
					data.Leverage.To<string>(),
					data.BeginValue.To<string>(),
					data.CurrentValue.To<string>(),
					data.BlockedValue.To<string>(),
					data.VariationMargin.To<string>(),
					data.Commission.To<string>(),
					data.Currency.To<string>(),
					data.State.To<string>(),
					data.Description,
					data.LastChangeTime.ToString(_dateTimeFormat),
					data.LocalTime.ToString(_dateTimeFormat)
				});
			}
		}

		sealed class PositionCsvList : CsvEntityList<Position>, IStoragePositionList
		{
			private const string _dateTimeFormat = "dd.MM.yyyy HH:mm:ss";

			public PositionCsvList(CsvEntityRegistry registry)
				: base(registry, "position.csv", Encoding.UTF8)
			{
			}

			protected override object GetKey(Position item)
			{
				return Tuple.Create(item.Portfolio, item.Security);
			}

			protected override Position Read(FastCsvReader reader)
			{
				var position = new Position
				{
					Portfolio = Registry.Portfolios.ReadById(reader.ReadString()),
					Security = Registry.Securities.ReadById(reader.ReadString()),
					DepoName = reader.ReadString(),
					LimitType = reader.ReadNullableEnum<TPlusLimits>(),
					BeginValue = reader.ReadNullableDecimal(),
					CurrentValue = reader.ReadNullableDecimal(),
					BlockedValue = reader.ReadNullableDecimal(),
					VariationMargin = reader.ReadNullableDecimal(),
					Commission = reader.ReadNullableDecimal(),
					Currency = reader.ReadNullableEnum<CurrencyTypes>(),
					LastChangeTime = reader.ReadDateTime(_dateTimeFormat),
					LocalTime = reader.ReadDateTime(_dateTimeFormat)
				};

				return position;
			}

			protected override void Write(CsvFileWriter writer, Position data)
			{
				writer.WriteRow(new[]
				{
					data.Portfolio.Name,
					data.Security.Id,
					data.DepoName,
					data.LimitType.To<string>(),
					data.BeginValue.To<string>(),
					data.CurrentValue.To<string>(),
					data.BlockedValue.To<string>(),
					data.VariationMargin.To<string>(),
					data.Commission.To<string>(),
					data.Description,
					data.LastChangeTime.ToString(_dateTimeFormat),
					data.LocalTime.ToString(_dateTimeFormat)
				});
			}

			public Position ReadBySecurityAndPortfolio(Security security, Portfolio portfolio)
			{
				return ReadById(Tuple.Create(portfolio, security));
			}
		}

		private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly ExchangeCsvList _exchanges;
		private readonly ExchangeBoardCsvList _exchangeBoards;
		private readonly SecurityCsvList _securities;
		private readonly PortfolioCsvList _portfolios;
		private readonly PositionCsvList _positions;

		private DelayAction _delayAction;
		private Timer _flushTimer;
		private bool _isFlushing;

		/// <summary>
		/// The path to data directory.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// The special interface for direct access to the storage.
		/// </summary>
		public IStorage Storage { get; }

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get { return _delayAction; }
			set
			{
				_delayAction = value;

				Exchanges.DelayAction = _delayAction;
				ExchangeBoards.DelayAction = _delayAction;
				Securities.DelayAction = _delayAction;
				//Trades.DelayAction = _delayAction;
				//MyTrades.DelayAction = _delayAction;
				//Orders.DelayAction = _delayAction;
				//OrderFails.DelayAction = _delayAction;
				//Portfolios.DelayAction = _delayAction;
				//Positions.DelayAction = _delayAction;
				//News.DelayAction = _delayAction;
			}
		}

		/// <summary>
		/// List of exchanges.
		/// </summary>
		public IStorageEntityList<Exchange> Exchanges => _exchanges;

		/// <summary>
		/// The list of stock boards.
		/// </summary>
		public IStorageEntityList<ExchangeBoard> ExchangeBoards => _exchangeBoards;

		/// <summary>
		/// The list of instruments.
		/// </summary>
		public IStorageSecurityList Securities => _securities;

		/// <summary>
		/// The list of portfolios.
		/// </summary>
		public IStorageEntityList<Portfolio> Portfolios => _portfolios;

		/// <summary>
		/// The list of positions.
		/// </summary>
		public IStoragePositionList Positions => _positions;

		/// <summary>
		/// The list of own trades.
		/// </summary>
		public IStorageEntityList<MyTrade> MyTrades { get { throw new NotSupportedException(); } }

		/// <summary>
		/// The list of tick trades.
		/// </summary>
		public IStorageEntityList<Trade> Trades { get { throw new NotSupportedException(); } }

		/// <summary>
		/// The list of orders.
		/// </summary>
		public IStorageEntityList<Order> Orders { get { throw new NotSupportedException(); } }

		/// <summary>
		/// The list of orders registration and cancelling errors.
		/// </summary>
		public IStorageEntityList<OrderFail> OrderFails { get { throw new NotSupportedException(); } }

		/// <summary>
		/// The list of news.
		/// </summary>
		public IStorageEntityList<News> News { get { throw new NotSupportedException(); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvEntityRegistry"/>.
		/// </summary>
		public CsvEntityRegistry(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			Path = path;
			Storage = new FakeStorage();

			_exchanges = new ExchangeCsvList(this);
			_exchangeBoards = new ExchangeBoardCsvList(this);
			_securities = new SecurityCsvList(this);
			_portfolios = new PortfolioCsvList(this);
			_positions = new PositionCsvList(this);
		}

		private void TryCreateTimer()
		{
			lock (_syncRoot)
			{
				if (_isFlushing || _flushTimer != null)
					return;

				_flushTimer = ThreadingHelper
					.Timer(() => CultureInfo.InvariantCulture.DoInCulture(OnFlush))
					.Interval(_flushInterval);
			}
		}

		private void OnFlush()
		{
			try
			{
				lock (_syncRoot)
				{
					if (_isFlushing)
						return;

					_isFlushing = true;
				}

				var canStop = true;

				try
				{
					canStop &= _exchanges.Flush();
					canStop &= _exchangeBoards.Flush();
					canStop &= _securities.Flush();
					canStop &= _portfolios.Flush();
					canStop &= _positions.Flush();
				}
				finally
				{
					lock (_syncRoot)
					{
						_isFlushing = false;

						if (canStop)
						{
							if (_flushTimer != null)
							{
								_flushTimer.Dispose();
								_flushTimer = null;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ex.LogError("Flush CSV entity registry error.");
			}
		}
	}
}