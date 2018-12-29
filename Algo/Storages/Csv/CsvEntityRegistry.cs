namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The CSV storage of trading objects.
	/// </summary>
	public class CsvEntityRegistry : IEntityRegistry
	{
		private class FakeStorage : IStorage
		{
			private readonly CsvEntityRegistry _registry;

			public FakeStorage(CsvEntityRegistry registry)
			{
				_registry = registry;
			}

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
				return _registry.Securities.ReadById(by[0].Value).To<TEntity>();
				//throw new NotSupportedException();
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

			public IBatchContext BeginBatch()
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

		private class ExchangeCsvList : CsvEntityList<Exchange>
		{
			public ExchangeCsvList(CsvEntityRegistry registry)
				: base(registry, "exchange.csv")
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
					//ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())
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
					//Serialize(data.ExtensionInfo)
				});
			}
		}

		private class ExchangeBoardCsvList : CsvEntityList<ExchangeBoard>
		{
			public ExchangeBoardCsvList(CsvEntityRegistry registry)
				: base(registry, "exchangeboard.csv")
			{
			}

			protected override object GetKey(ExchangeBoard item)
			{
				return item.Code;
			}

			private Exchange GetExchange(string exchangeCode)
			{
				var exchange = Registry.Exchanges.ReadById(exchangeCode);

				if (exchange == null)
					throw new InvalidOperationException(LocalizedStrings.Str1217Params.Put(exchangeCode));

				return exchange;
			}

			protected override ExchangeBoard Read(FastCsvReader reader)
			{
				var board = new ExchangeBoard
				{
					Code = reader.ReadString(),
					Exchange = GetExchange(reader.ReadString()),
					ExpiryTime = reader.ReadString().ToTime(),
					//IsSupportAtomicReRegister = reader.ReadBool(),
					//IsSupportMarketOrders = reader.ReadBool(),
					TimeZone = TimeZoneInfo.FindSystemTimeZoneById(reader.ReadString()),
				};

				var time = board.WorkingTime;

				if (reader.ColumnCount == 7)
				{
					time.Periods = Deserialize<List<WorkingTimePeriod>>(reader.ReadString());
					time.SpecialWorkingDays = Deserialize<IEnumerable<DateTime>>(reader.ReadString()).ToArray();
					time.SpecialHolidays = Deserialize<IEnumerable<DateTime>>(reader.ReadString()).ToArray();
				}
				else
				{
					time.Periods.AddRange(reader.ReadString().DecodeToPeriods());
					time.SpecialDays.AddRange(reader.ReadString().DecodeToSpecialDays());
				}

				//ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())

				return board;
			}

			protected override void Write(CsvFileWriter writer, ExchangeBoard data)
			{
				writer.WriteRow(new[]
				{
					data.Code,
					data.Exchange.Name,
					data.ExpiryTime.WriteTime(),
					//data.IsSupportAtomicReRegister.To<string>(),
					//data.IsSupportMarketOrders.To<string>(),
					data.TimeZone.Id,
					//Serialize(data.WorkingTime.Periods),
					data.WorkingTime.Periods.EncodeToString(),
					//Serialize(data.WorkingTime.SpecialWorkingDays),
					//Serialize(data.WorkingTime.SpecialHolidays),
					data.WorkingTime.SpecialDays.EncodeToString(),
					//Serialize(data.ExtensionInfo)
				});
			}

			private readonly SynchronizedDictionary<Type, IXmlSerializer> _serializers = new SynchronizedDictionary<Type, IXmlSerializer>();

			//private string Serialize<TItem>(TItem item)
			//	where TItem : class
			//{
			//	if (item == null)
			//		return null;

			//	var serializer = GetSerializer<TItem>();

			//	using (var stream = new MemoryStream())
			//	{
			//		serializer.Serialize(item, stream);
			//		return Registry.Encoding.GetString(stream.ToArray()).Remove(Environment.NewLine).Replace("\"", "'");
			//	}
			//}

			private TItem Deserialize<TItem>(string value)
				where TItem : class
			{
				if (value.IsEmpty())
					return null;

				var serializer = GetSerializer<TItem>();
				var bytes = Registry.Encoding.GetBytes(value.Replace("'", "\""));

				using (var stream = new MemoryStream(bytes))
					return serializer.Deserialize(stream);
			}

			private XmlSerializer<TItem> GetSerializer<TItem>()
			{
				return (XmlSerializer<TItem>)_serializers.SafeAdd(typeof(TItem), k => new XmlSerializer<TItem>(false));
			}
		}

		private class SecurityCsvList : CsvEntityList<Security>, IStorageSecurityList
		{
			public SecurityCsvList(CsvEntityRegistry registry)
				: base(registry, "security.csv")
			{
				((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
				((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
			}

			#region IStorageSecurityList

			void IDisposable.Dispose()
			{
			}

			private Action<IEnumerable<Security>> _added;

			event Action<IEnumerable<Security>> ISecurityProvider.Added
			{
				add => _added += value;
				remove => _added -= value;
			}

			private Action<IEnumerable<Security>> _removed;

			event Action<IEnumerable<Security>> ISecurityProvider.Removed
			{
				add => _removed += value;
				remove => _removed -= value;
			}

			IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
			{
				if (criteria.IsLookupAll())
					return ToArray();

				if (criteria.Id.IsEmpty())
					return this.Filter(criteria);

				var security = ((IStorageSecurityList)this).ReadById(criteria.Id);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}

			void ISecurityStorage.Delete(Security security)
			{
				Remove(security);
			}

			void ISecurityStorage.DeleteBy(Security criteria)
			{
				this.Filter(criteria).ForEach(s => Remove(s));
			}

			#endregion

			#region CsvEntityList

			protected override object GetKey(Security item)
			{
				return item.Id;
			}

			private class LiteSecurity
			{
				public string Id { get; set; }
				public string Name { get; set; }
				public string Code { get; set; }
				public string Class { get; set; }
				public string ShortName { get; set; }
				public string Board { get; set; }
				public string UnderlyingSecurityId { get; set; }
				public decimal? PriceStep { get; set; }
				public decimal? VolumeStep { get; set; }
				public decimal? Multiplier { get; set; }
				public int? Decimals { get; set; }
				public SecurityTypes? Type { get; set; }
				public DateTimeOffset? ExpiryDate { get; set; }
				public DateTimeOffset? SettlementDate { get; set; }
				public decimal? Strike { get; set; }
				public OptionTypes? OptionType { get; set; }
				public CurrencyTypes? Currency { get; set; }
				public SecurityExternalId ExternalId { get; set; }
				public SecurityTypes? UnderlyingSecurityType { get; set; }
				public string BinaryOptionType { get; set; }
				public string CfiCode { get; set; }
				public DateTimeOffset? IssueDate { get; set; }
				public decimal? IssueSize { get; set; }
				public string BasketCode { get; set; }
				public string BasketExpression { get; set; }

				public Security ToSecurity(SecurityCsvList list)
				{
					return new Security
					{
						Id = Id,
						Name = Name,
						Code = Code,
						Class = Class,
						ShortName = ShortName,
						Board = list.Registry.GetBoard(Board),
						UnderlyingSecurityId = UnderlyingSecurityId,
						PriceStep = PriceStep,
						VolumeStep = VolumeStep,
						Multiplier = Multiplier,
						Decimals = Decimals,
						Type = Type,
						ExpiryDate = ExpiryDate,
						SettlementDate = SettlementDate,
						Strike = Strike,
						OptionType = OptionType,
						Currency = Currency,
						ExternalId = ExternalId.Clone(),
						UnderlyingSecurityType = UnderlyingSecurityType,
						BinaryOptionType = BinaryOptionType,
						CfiCode = CfiCode,
						IssueDate = IssueDate,
						IssueSize = IssueSize,
						BasketCode = BasketCode,
						BasketExpression = BasketExpression,
					};
				}

				public void Update(Security security)
				{
					Name = security.Name;
					Code = security.Code;
					Class = security.Class;
					ShortName = security.ShortName;
					Board = security.Board.Code;
					UnderlyingSecurityId = security.UnderlyingSecurityId;
					PriceStep = security.PriceStep;
					VolumeStep = security.VolumeStep;
					Multiplier = security.Multiplier;
					Decimals = security.Decimals;
					Type = security.Type;
					ExpiryDate = security.ExpiryDate;
					SettlementDate = security.SettlementDate;
					Strike = security.Strike;
					OptionType = security.OptionType;
					Currency = security.Currency;
					ExternalId = security.ExternalId.Clone();
					UnderlyingSecurityType = security.UnderlyingSecurityType;
					BinaryOptionType = security.BinaryOptionType;
					CfiCode = security.CfiCode;
					IssueDate = security.IssueDate;
					IssueSize = security.IssueSize;
					BasketCode = security.BasketCode;
					BasketExpression = security.BasketExpression;
				}
			}

			private readonly Dictionary<string, LiteSecurity> _cache = new Dictionary<string, LiteSecurity>(StringComparer.InvariantCultureIgnoreCase);

			private static bool IsChanged(string original, string cached, bool forced)
			{
				if (original.IsEmpty())
					return forced && !cached.IsEmpty();
				else
					return cached.IsEmpty() || (forced && !cached.CompareIgnoreCase(original));
			}

			private static bool IsChanged<T>(T? original, T? cached, bool forced)
				where T : struct
			{
				if (original == null)
					return forced && cached != null;
				else
					return cached == null || (forced && !original.Value.Equals(cached.Value));
			}

			protected override bool IsChanged(Security security, bool forced)
			{
				var liteSec = _cache.TryGetValue(security.Id);

				if (liteSec == null)
					throw new ArgumentOutOfRangeException(nameof(security), security.Id, LocalizedStrings.Str2736);

				if (IsChanged(security.Name, liteSec.Name, forced))
					return true;

				if (IsChanged(security.Code, liteSec.Code, forced))
					return true;

				if (IsChanged(security.Class, liteSec.Class, forced))
					return true;

				if (IsChanged(security.ShortName, liteSec.ShortName, forced))
					return true;

				if (IsChanged(security.UnderlyingSecurityId, liteSec.UnderlyingSecurityId, forced))
					return true;

				if (IsChanged(security.UnderlyingSecurityType, liteSec.UnderlyingSecurityType, forced))
					return true;

				if (IsChanged(security.PriceStep, liteSec.PriceStep, forced))
					return true;

				if (IsChanged(security.VolumeStep, liteSec.VolumeStep, forced))
					return true;

				if (IsChanged(security.Multiplier, liteSec.Multiplier, forced))
					return true;

				if (IsChanged(security.Decimals, liteSec.Decimals, forced))
					return true;

				if (IsChanged(security.Type, liteSec.Type, forced))
					return true;

				if (IsChanged(security.ExpiryDate, liteSec.ExpiryDate, forced))
					return true;

				if (IsChanged(security.SettlementDate, liteSec.SettlementDate, forced))
					return true;

				if (IsChanged(security.Strike, liteSec.Strike, forced))
					return true;

				if (IsChanged(security.OptionType, liteSec.OptionType, forced))
					return true;

				if (IsChanged(security.Currency, liteSec.Currency, forced))
					return true;

				if (IsChanged(security.BinaryOptionType, liteSec.BinaryOptionType, forced))
					return true;

				if (IsChanged(security.CfiCode, liteSec.CfiCode, forced))
					return true;

				if (IsChanged(security.IssueDate, liteSec.IssueDate, forced))
					return true;

				if (IsChanged(security.IssueSize, liteSec.IssueSize, forced))
					return true;

				if (security.Board == null)
				{
					if (!liteSec.Board.IsEmpty() && forced)
						return true;
				}
				else
				{
					if (liteSec.Board.IsEmpty() || (forced && !liteSec.Board.CompareIgnoreCase(security.Board.Code)))
						return true;
				}

				if (forced && security.ExternalId != liteSec.ExternalId)
					return true;

				if (IsChanged(security.BasketCode, liteSec.BasketCode, forced))
					return true;

				if (IsChanged(security.BasketExpression, liteSec.BasketExpression, forced))
					return true;

				return false;
			}

			protected override void ClearCache()
			{
				_cache.Clear();
			}

			protected override void AddCache(Security item)
			{
				var sec = new LiteSecurity { Id = item.Id };
				sec.Update(item);
				_cache.Add(item.Id, sec);
			}

			protected override void RemoveCache(Security item)
			{
				_cache.Remove(item.Id);
			}

			protected override void UpdateCache(Security item)
			{
				_cache[item.Id].Update(item);
			}

			//protected override void WriteMany(Security[] values)
			//{
			//	base.WriteMany(_cache.Values.Select(l => l.ToSecurity(this)).ToArray());
			//}

			protected override Security Read(FastCsvReader reader)
			{
				var liteSec = new LiteSecurity
				{
					Id = reader.ReadString(),
					Name = reader.ReadString(),
					Code = reader.ReadString(),
					Class = reader.ReadString(),
					ShortName = reader.ReadString(),
					Board = reader.ReadString(),
					UnderlyingSecurityId = reader.ReadString(),
					PriceStep = reader.ReadNullableDecimal(),
					VolumeStep = reader.ReadNullableDecimal(),
					Multiplier = reader.ReadNullableDecimal(),
					Decimals = reader.ReadNullableInt(),
					Type = reader.ReadNullableEnum<SecurityTypes>(),
					ExpiryDate = ReadNullableDateTime(reader),
					SettlementDate = ReadNullableDateTime(reader),
					Strike = reader.ReadNullableDecimal(),
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
				};

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					liteSec.UnderlyingSecurityType = reader.ReadNullableEnum<SecurityTypes>();
					liteSec.BinaryOptionType = reader.ReadString();
					liteSec.CfiCode = reader.ReadString();
					liteSec.IssueDate = ReadNullableDateTime(reader);
					liteSec.IssueSize = reader.ReadNullableDecimal();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					liteSec.BasketCode = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					liteSec.BasketExpression = reader.ReadString();

				return liteSec.ToSecurity(this);
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
					data.ExpiryDate?.UtcDateTime.ToString(_dateTimeFormat),
					data.SettlementDate?.UtcDateTime.ToString(_dateTimeFormat),
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
					data.UnderlyingSecurityType.To<string>(),
					data.BinaryOptionType,
					data.CfiCode,
					data.IssueDate?.UtcDateTime.ToString(_dateTimeFormat),
					data.IssueSize.To<string>(),
					data.BasketCode,
					data.BasketExpression,
				});
			}

			public override void Save(Security entity, bool forced)
			{
				lock (Registry.Exchanges.SyncRoot)
					Registry.Exchanges.TryAdd(entity.Board.Exchange);

				lock (Registry.ExchangeBoards.SyncRoot)
					Registry.ExchangeBoards.TryAdd(entity.Board);

				base.Save(entity, forced);
			}

			#endregion
		}

		private class PortfolioCsvList : CsvEntityList<Portfolio>
		{
			public PortfolioCsvList(CsvEntityRegistry registry)
				: base(registry, "portfolio.csv")
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
					Board = GetBoard(reader.ReadString()),
					Leverage = reader.ReadNullableDecimal(),
					BeginValue = reader.ReadNullableDecimal(),
					CurrentValue = reader.ReadNullableDecimal(),
					BlockedValue = reader.ReadNullableDecimal(),
					VariationMargin = reader.ReadNullableDecimal(),
					Commission = reader.ReadNullableDecimal(),
					Currency = reader.ReadNullableEnum<CurrencyTypes>(),
					State = reader.ReadNullableEnum<PortfolioStates>(),
					Description = reader.ReadString(),
					LastChangeTime = _dateTimeParser.Parse(reader.ReadString()).ChangeKind(DateTimeKind.Utc),
					LocalTime = _dateTimeParser.Parse(reader.ReadString()).ChangeKind(DateTimeKind.Utc)
				};

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					portfolio.ClientCode = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					portfolio.Currency = reader.ReadString().To<CurrencyTypes?>();

					var str = reader.ReadString();
					portfolio.ExpirationDate = str.IsEmpty() ? (DateTimeOffset?)null : _dateTimeParser.Parse(str).ChangeKind(DateTimeKind.Utc);
				}

				return portfolio;
			}

			private ExchangeBoard GetBoard(string boardCode)
			{
				return boardCode.IsEmpty() ? null : Registry.GetBoard(boardCode);
			}

			protected override void Write(CsvFileWriter writer, Portfolio data)
			{
				writer.WriteRow(new[]
				{
					data.Name,
					data.Board?.Code,
					data.Leverage.To<string>(),
					data.BeginValue.To<string>(),
					data.CurrentValue.To<string>(),
					data.BlockedValue.To<string>(),
					data.VariationMargin.To<string>(),
					data.Commission.To<string>(),
					data.Currency.To<string>(),
					data.State.To<string>(),
					data.Description,
					data.LastChangeTime.UtcDateTime.ToString(_dateTimeFormat),
					data.LocalTime.UtcDateTime.ToString(_dateTimeFormat),
					data.ClientCode,
					data.Currency?.To<string>(),
					data.ExpirationDate?.UtcDateTime.ToString(_dateTimeFormat),
				});
			}
		}

		private class PositionCsvList : CsvEntityList<Position>, IStoragePositionList
		{
			public PositionCsvList(CsvEntityRegistry registry)
				: base(registry, "position.csv")
			{
			}

			protected override object GetKey(Position item)
			{
				return Tuple.Create(item.Portfolio, item.Security);
			}

			private Portfolio GetPortfolio(string id)
			{
				var portfolio = Registry.Portfolios.ReadById(id);

				if (portfolio == null)
					throw new InvalidOperationException(LocalizedStrings.Str3622Params.Put(id));

				return portfolio;
			}

			private Security GetSecurity(string id)
			{
				var security = Registry.Securities.ReadById(id);

				if (security == null)
					throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(id));

				return security;
			}

			protected override Position Read(FastCsvReader reader)
			{
				var pfName = reader.ReadString();
				var secId = reader.ReadString();

				var position = new Position
				{
					Portfolio = GetPortfolio(pfName),
					Security = GetSecurity(secId),
					DepoName = reader.ReadString(),
					LimitType = reader.ReadNullableEnum<TPlusLimits>(),
					BeginValue = reader.ReadNullableDecimal(),
					CurrentValue = reader.ReadNullableDecimal(),
					BlockedValue = reader.ReadNullableDecimal(),
					VariationMargin = reader.ReadNullableDecimal(),
					Commission = reader.ReadNullableDecimal(),
					Currency = reader.ReadNullableEnum<CurrencyTypes>(),
					LastChangeTime = _dateTimeParser.Parse(reader.ReadString()).ChangeKind(DateTimeKind.Utc),
					LocalTime = _dateTimeParser.Parse(reader.ReadString()).ChangeKind(DateTimeKind.Utc),
				};

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					position.ClientCode = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					position.Currency = reader.ReadString().To<CurrencyTypes?>();

					var str = reader.ReadString();
					position.ExpirationDate = str.IsEmpty() ? (DateTimeOffset?)null : _dateTimeParser.Parse(str).ChangeKind(DateTimeKind.Utc);
				}

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
					data.LastChangeTime.UtcDateTime.ToString(_dateTimeFormat),
					data.LocalTime.UtcDateTime.ToString(_dateTimeFormat),
					data.ClientCode,
					data.Currency?.To<string>(),
					data.ExpirationDate?.UtcDateTime.ToString(_dateTimeFormat),
				});
			}

			public Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "")
			{
				return ((IStorageEntityList<Position>)this).ReadById(Tuple.Create(portfolio, security));
			}
		}

		private const string _dateTimeFormat = "yyyyMMddHHmmss";
		private static readonly FastDateTimeParser _dateTimeParser = new FastDateTimeParser(_dateTimeFormat);

		private static DateTimeOffset? ReadNullableDateTime(FastCsvReader reader)
		{
			var str = reader.ReadString();

			if (str == null)
				return null;

			return _dateTimeParser.Parse(str).ChangeKind(DateTimeKind.Utc);
		}

		private readonly ExchangeCsvList _exchanges;
		private readonly ExchangeBoardCsvList _exchangeBoards;
		private readonly SecurityCsvList _securities;
		private readonly PortfolioCsvList _portfolios;
		private readonly PositionCsvList _positions;

		private readonly List<ICsvEntityList> _csvLists = new List<ICsvEntityList>();

		/// <summary>
		/// The path to data directory.
		/// </summary>
		public string Path { get; set; }

		/// <inheritdoc />
		public IStorage Storage { get; }

		private Encoding _encoding = Encoding.UTF8;

		/// <summary>
		/// Encoding.
		/// </summary>
		public Encoding Encoding
		{
			get => _encoding;
			set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
		}

		private DelayAction _delayAction = new DelayAction(ex => ex.LogError());

		/// <inheritdoc />
		public virtual DelayAction DelayAction
		{
			get => _delayAction;
			set
			{
				_delayAction = value ?? throw new ArgumentNullException(nameof(value));
				UpdateDelayAction();
			}
		}

		private void UpdateDelayAction()
		{
			_exchanges.DelayAction = _delayAction;
			_exchangeBoards.DelayAction = _delayAction;
			_securities.DelayAction = _delayAction;
			_positions.DelayAction = _delayAction;
			_portfolios.DelayAction = _delayAction;
		}

		/// <inheritdoc />
		public IStorageEntityList<Exchange> Exchanges => _exchanges;

		/// <inheritdoc />
		public IStorageEntityList<ExchangeBoard> ExchangeBoards => _exchangeBoards;

		/// <inheritdoc />
		public IStorageSecurityList Securities => _securities;

		/// <inheritdoc />
		public IStorageEntityList<Portfolio> Portfolios => _portfolios;

		/// <inheritdoc />
		public IStoragePositionList Positions => _positions;

		/// <inheritdoc />
		public IPositionStorage PositionStorage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvEntityRegistry"/>.
		/// </summary>
		/// <param name="path">The path to data directory.</param>
		public CsvEntityRegistry(string path)
		{
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Storage = new FakeStorage(this);

			Add(_exchanges = new ExchangeCsvList(this));
			Add(_exchangeBoards = new ExchangeBoardCsvList(this));
			Add(_securities = new SecurityCsvList(this));
			Add(_portfolios = new PortfolioCsvList(this));
			Add(_positions = new PositionCsvList(this));

			UpdateDelayAction();

			PositionStorage = new PositionStorage(this);
		}

		/// <summary>
		/// Add list of trade objects.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="list">List of trade objects.</param>
		public void Add<T>(CsvEntityList<T> list)
			where T : class
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));

			_csvLists.Add(list);
		}

		/// <inheritdoc />
		public IDictionary<object, Exception> Init()
		{
			Directory.CreateDirectory(Path);

			var errors = new Dictionary<object, Exception>();

			foreach (var list in _csvLists)
			{
				try
				{
					var listErrors = new List<Exception>();
					list.Init(listErrors);

					if (listErrors.Count > 0)
						errors.Add(list, new AggregateException(listErrors));
				}
				catch (Exception ex)
				{
					errors.Add(list, ex);
				}
			}

			return errors;
		}

		internal ExchangeBoard GetBoard(string boardCode)
		{
			var board = ExchangeBoards.ReadById(boardCode);

			if (board != null)
				return board;

			board = ServicesRegistry.EnsureGetExchangeInfoProvider().GetExchangeBoard(boardCode);

			if (board == null)
				throw new InvalidOperationException(LocalizedStrings.Str1217Params.Put(boardCode));

			return board;
		}
	}
}