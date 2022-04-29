namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The CSV storage of trading objects.
	/// </summary>
	public class CsvEntityRegistry : IEntityRegistry
	{
		/// <summary>
		/// </summary>
		[Obsolete("This property exists only for backward compatibility.")]
		public object Storage => throw new NotSupportedException();

		private class ExchangeCsvList : CsvEntityList<string, Exchange>
		{
			public ExchangeCsvList(CsvEntityRegistry registry)
				: base(registry, "exchange.csv")
			{
			}

			protected override string GetKey(Exchange item)
			{
				return item.Name;
			}

			protected override Exchange Read(FastCsvReader reader)
			{
				var board = new Exchange
				{
					Name = reader.ReadString(),
					CountryCode = reader.ReadNullableEnum<CountryCodes>(),
					//EngName = reader.ReadString(),
					//RusName = reader.ReadString(),
					//ExtensionInfo = Deserialize<Dictionary<object, object>>(reader.ReadString())
				};

				var engName = reader.ReadString();

				reader.Skip();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					board.FullNameLoc = reader.ReadString();
				else
				{
					board.FullNameLoc = LocalizedStrings.LocalizationManager.GetResourceId(engName) ?? engName;
				}

				return board;
			}

			protected override void Write(CsvFileWriter writer, Exchange data)
			{
				writer.WriteRow(new[]
				{
					data.Name,
					data.CountryCode.To<string>(),
					string.Empty/*data.EngName*/,
					string.Empty/*data.RusName*/,
					//Serialize(data.ExtensionInfo),
					data.FullNameLoc,
				});
			}
		}

		private class ExchangeBoardCsvList : CsvEntityList<string, ExchangeBoard>
		{
			public ExchangeBoardCsvList(CsvEntityRegistry registry)
				: base(registry, "exchangeboard.csv")
			{
			}

			protected override string GetKey(ExchangeBoard item)
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
					TimeZone = reader.ReadString().To<TimeZoneInfo>(),
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

					if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					{
						reader.Skip();

						time.IsEnabled = reader.ReadBool();
					}
				}

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
					string.Empty,
					data.WorkingTime.IsEnabled.ToString(),
				});
			}

			private readonly SynchronizedDictionary<Type, ISerializer> _serializers = new();

			private TItem Deserialize<TItem>(string value)
				where TItem : class
			{
				if (value.IsEmpty())
					return null;

				var serializer = GetSerializer<TItem>();
				var bytes = Registry.Encoding.GetBytes(value.Replace("'", "\""));

				return serializer.Deserialize(bytes);
			}

			private ISerializer<TItem> GetSerializer<TItem>()
				=> (ISerializer<TItem>)_serializers.SafeAdd(typeof(TItem), k => new JsonSerializer<TItem> { Indent = false, EnumAsString = true });
		}

		private class SecurityCsvList : CsvEntityList<SecurityId, Security>, IStorageSecurityList
		{
			public SecurityCsvList(CsvEntityRegistry registry)
				: base(registry, "security.csv")
			{
				((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
				((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
			}

			#region IStorageSecurityList

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

			private Security GetById(SecurityId id) => ((IStorageSecurityList)this).ReadById(id);

			Security ISecurityProvider.LookupById(SecurityId id) => GetById(id);

			IEnumerable<Security> ISecurityProvider.Lookup(SecurityLookupMessage criteria)
			{
				var secId = criteria.SecurityId;

				if (secId == default || secId.BoardCode.IsEmpty())
					return this.Filter(criteria);

				var security = GetById(secId);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}

			void ISecurityStorage.Delete(Security security)
			{
				Remove(security);
			}

			void ISecurityStorage.DeleteBy(SecurityLookupMessage criteria)
			{
				this.Filter(criteria).ForEach(s => Remove(s));
			}

			void ISecurityStorage.DeleteRange(IEnumerable<Security> securities)
			{
				RemoveRange(securities);
				OnRemovedRange(securities);
			}

			#endregion

			#region CsvEntityList

			protected override SecurityId GetKey(Security item) => item.ToSecurityId();

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
				public decimal? MinVolume { get; set; }
				public decimal? MaxVolume { get; set; }
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
				public decimal? UnderlyingSecurityMinVolume { get; set; }
				public string BinaryOptionType { get; set; }
				public string CfiCode { get; set; }
				public DateTimeOffset? IssueDate { get; set; }
				public decimal? IssueSize { get; set; }
				public bool? Shortable { get; set; }
				public string BasketCode { get; set; }
				public string BasketExpression { get; set; }
				public string PrimaryId { get; set; }

				public Security ToSecurity(SecurityCsvList list)
				{
					if (Id.EqualsIgnoreCase(TraderHelper.AllSecurity.Id))
						return TraderHelper.AllSecurity;

					var board = Board;

					if (board.IsEmpty())
						board = Id.ToSecurityId().BoardCode;

					return new Security
					{
						Id = Id,
						Name = Name,
						Code = Code,
						Class = Class,
						ShortName = ShortName,
						Board = list.Registry.GetBoard(board),
						UnderlyingSecurityId = UnderlyingSecurityId,
						PriceStep = PriceStep,
						VolumeStep = VolumeStep,
						MinVolume = MinVolume,
						MaxVolume = MaxVolume,
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
						UnderlyingSecurityMinVolume = UnderlyingSecurityMinVolume,
						BinaryOptionType = BinaryOptionType,
						CfiCode = CfiCode,
						IssueDate = IssueDate,
						IssueSize = IssueSize,
						Shortable = Shortable,
						BasketCode = BasketCode,
						BasketExpression = BasketExpression,
						PrimaryId = PrimaryId
					};
				}

				public void Update(Security security)
				{
					Name = security.Name;
					Code = security.Code;
					Class = security.Class;
					ShortName = security.ShortName;
					Board = security.Board?.Code;
					UnderlyingSecurityId = security.UnderlyingSecurityId;
					PriceStep = security.PriceStep;
					VolumeStep = security.VolumeStep;
					MinVolume = security.MinVolume;
					MaxVolume = security.MaxVolume;
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
					UnderlyingSecurityMinVolume = security.UnderlyingSecurityMinVolume;
					BinaryOptionType = security.BinaryOptionType;
					CfiCode = security.CfiCode;
					IssueDate = security.IssueDate;
					IssueSize = security.IssueSize;
					Shortable = security.Shortable;
					BasketCode = security.BasketCode;
					BasketExpression = security.BasketExpression;
					PrimaryId = security.PrimaryId;
				}
			}

			private readonly Dictionary<SecurityId, LiteSecurity> _cache = new();

			private static bool IsChanged(string original, string cached, bool forced)
			{
				if (original.IsEmpty())
					return forced && !cached.IsEmpty();
				else
					return cached.IsEmpty() || (forced && !cached.EqualsIgnoreCase(original));
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
				var liteSec = _cache.TryGetValue(security.ToSecurityId());

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

				if (IsChanged(security.UnderlyingSecurityMinVolume, liteSec.UnderlyingSecurityMinVolume, forced))
					return true;

				if (IsChanged(security.PriceStep, liteSec.PriceStep, forced))
					return true;

				if (IsChanged(security.VolumeStep, liteSec.VolumeStep, forced))
					return true;

				if (IsChanged(security.MinVolume, liteSec.MinVolume, forced))
					return true;

				if (IsChanged(security.MaxVolume, liteSec.MaxVolume, forced))
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

				if (IsChanged(security.Shortable, liteSec.Shortable, forced))
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
					if (liteSec.Board.IsEmpty() || (forced && !liteSec.Board.EqualsIgnoreCase(security.Board?.Code)))
						return true;
				}

				if (forced && security.ExternalId != liteSec.ExternalId)
					return true;

				if (IsChanged(security.BasketCode, liteSec.BasketCode, forced))
					return true;

				if (IsChanged(security.BasketExpression, liteSec.BasketExpression, forced))
					return true;

				if (IsChanged(security.PrimaryId, liteSec.PrimaryId, forced))
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
				_cache.Add(item.ToSecurityId(), sec);
			}

			protected override void RemoveCache(Security item)
			{
				_cache.Remove(item.ToSecurityId());
			}

			protected override void UpdateCache(Security item)
			{
				_cache[item.ToSecurityId()].Update(item);
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

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					liteSec.MinVolume = reader.ReadNullableDecimal();
					liteSec.Shortable = reader.ReadNullableBool();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					liteSec.UnderlyingSecurityMinVolume = reader.ReadNullableDecimal();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					liteSec.MaxVolume = reader.ReadNullableDecimal();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					liteSec.PrimaryId = reader.ReadString();

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
					data.Board?.Code,
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
					data.MinVolume.To<string>(),
					data.Shortable.To<string>(),
					data.UnderlyingSecurityMinVolume.To<string>(),
					data.MaxVolume.To<string>(),
					data.PrimaryId,
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

		private class PortfolioCsvList : CsvEntityList<string, Portfolio>
		{
			public PortfolioCsvList(CsvEntityRegistry registry)
				: base(registry, "portfolio.csv")
			{
			}

			protected override string GetKey(Portfolio item)
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
					LastChangeTime = _dateTimeParser.Parse(reader.ReadString()).UtcKind(),
					LocalTime = _dateTimeParser.Parse(reader.ReadString()).UtcKind()
				};

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					portfolio.ClientCode = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					portfolio.Currency = reader.ReadString().To<CurrencyTypes?>();

					var str = reader.ReadString();
					portfolio.ExpirationDate = str.IsEmpty() ? null : _dateTimeParser.Parse(str).UtcKind();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					portfolio.CommissionMaker = reader.ReadNullableDecimal();
					portfolio.CommissionTaker = reader.ReadNullableDecimal();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					/*portfolio.InternalId = */reader.ReadString().To<Guid?>();

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
					data.CommissionMaker.To<string>(),
					data.CommissionTaker.To<string>(),
					/*data.InternalId.To<string>()*/string.Empty,
				});
			}
		}

		private class PositionCsvList : CsvEntityList<Tuple<Portfolio, Security, string, Sides?>, Position>, IStoragePositionList
		{
			public PositionCsvList(CsvEntityRegistry registry)
				: base(registry, "position.csv")
			{
			}

			protected override Tuple<Portfolio, Security, string, Sides?> GetKey(Position item)
				=> CreateKey(item.Portfolio, item.Security, item.StrategyId, item.Side);

			private Portfolio GetPortfolio(string id)
			{
				var portfolio = Registry.Portfolios.ReadById(id);

				if (portfolio == null)
					throw new InvalidOperationException(LocalizedStrings.Str3622Params.Put(id));

				return portfolio;
			}

			private Security GetSecurity(string id)
			{
				var secId = id.ToSecurityId();
				var security = secId.IsMoney() ? TraderHelper.MoneySecurity : Registry.Securities.ReadById(secId);

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
					LastChangeTime = _dateTimeParser.Parse(reader.ReadString()).UtcKind(),
					LocalTime = _dateTimeParser.Parse(reader.ReadString()).UtcKind(),
				};

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					position.ClientCode = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					position.Currency = reader.ReadString().To<CurrencyTypes?>();

					var str = reader.ReadString();
					position.ExpirationDate = str.IsEmpty() ? null : _dateTimeParser.Parse(str).UtcKind();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					position.Leverage = reader.ReadNullableDecimal();
					position.CommissionMaker = reader.ReadNullableDecimal();
					position.CommissionTaker = reader.ReadNullableDecimal();
					position.AveragePrice = reader.ReadNullableDecimal();
					position.RealizedPnL = reader.ReadNullableDecimal();
					position.UnrealizedPnL = reader.ReadNullableDecimal();
					position.CurrentPrice = reader.ReadNullableDecimal();
					position.SettlementPrice = reader.ReadNullableDecimal();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					position.BuyOrdersCount = reader.ReadNullableInt();
					position.SellOrdersCount = reader.ReadNullableInt();
					position.BuyOrdersMargin = reader.ReadNullableDecimal();
					position.SellOrdersMargin = reader.ReadNullableDecimal();
					position.OrdersMargin = reader.ReadNullableDecimal();
					position.OrdersCount = reader.ReadNullableInt();
					position.TradesCount = reader.ReadNullableInt();
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					position.StrategyId = reader.ReadString();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					position.Side = reader.ReadNullableEnum<Sides>();

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
					data.Currency.To<string>(),
					data.ExpirationDate?.UtcDateTime.ToString(_dateTimeFormat),
					data.Leverage.To<string>(),
					data.CommissionMaker.To<string>(),
					data.CommissionTaker.To<string>(),
					data.AveragePrice.To<string>(),
					data.RealizedPnL.To<string>(),
					data.UnrealizedPnL.To<string>(),
					data.CurrentPrice.To<string>(),
					data.SettlementPrice.To<string>(),
					data.BuyOrdersCount.To<string>(),
					data.SellOrdersCount.To<string>(),
					data.BuyOrdersMargin.To<string>(),
					data.SellOrdersMargin.To<string>(),
					data.OrdersMargin.To<string>(),
					data.OrdersCount.To<string>(),
					data.TradesCount.To<string>(),
					data.StrategyId,
					data.Side.To<string>(),
				});
			}

			public Position GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode = "", string depoName = "", TPlusLimits? limit = null)
				=> ((IStorageEntityList<Position>)this).ReadById(CreateKey(portfolio, security, strategyId, side));

			private Tuple<Portfolio, Security, string, Sides?> CreateKey(Portfolio portfolio, Security security, string strategyId, Sides? side)
				=> Tuple.Create(portfolio, security, strategyId?.ToLowerInvariant() ?? string.Empty, side);
		}

		private class SubscriptionCsvList : CsvEntityList<Tuple<SecurityId, DataType>, MarketDataMessage>
		{
			public SubscriptionCsvList(CsvEntityRegistry registry)
				: base(registry, "subscription.csv")
			{
			}

			protected override Tuple<SecurityId, DataType> GetKey(MarketDataMessage item) => Tuple.Create(item.SecurityId, item.DataType2);

			protected override void Write(CsvFileWriter writer, MarketDataMessage data)
			{
				if (data == null)
					throw new ArgumentNullException(nameof(data));

				if (!data.IsSubscribe)
					throw new ArgumentException(nameof(data));

				var (type, arg) = data.DataType2.FormatToString();
				var buildFromTuples = data.BuildFrom?.FormatToString();

				writer.WriteRow(new[]
				{
					string.Empty,//data.TransactionId.To<string>(),
					data.SecurityId.SecurityCode,
					data.SecurityId.BoardCode,
					type,
					arg,
					data.IsCalcVolumeProfile.To<string>(),
					data.AllowBuildFromSmallerTimeFrame.To<string>(),
					data.IsRegularTradingHours.To<string>(),
					data.MaxDepth.To<string>(),
					data.NewsId,
					data.From?.UtcDateTime.ToString(_dateTimeFormat),
					data.To?.UtcDateTime.ToString(_dateTimeFormat),
					data.Count.To<string>(),
					data.BuildMode.To<string>(),
					null,
					data.BuildField.To<string>(),
					data.IsFinishedOnly.To<string>(),
					data.FillGaps.To<string>(),
					buildFromTuples?.type,
					buildFromTuples?.arg,
					data.Skip.To<string>(),
					data.DoNotBuildOrderBookInrement.To<string>(),
				});
			}

			protected override MarketDataMessage Read(FastCsvReader reader)
			{
				reader.Skip();

				var message = new MarketDataMessage
				{
					//TransactionId = reader.ReadLong(),
					SecurityId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString(),
					},

					IsSubscribe = true,

					DataType2 = reader.ReadString().ToDataType(reader.ReadString()),
					IsCalcVolumeProfile = reader.ReadBool(),
					AllowBuildFromSmallerTimeFrame = reader.ReadBool(),
					IsRegularTradingHours = reader.ReadBool(),

					MaxDepth = reader.ReadNullableInt(),
					NewsId = reader.ReadString(),
				};

				var str = reader.ReadString();
				message.From = str.IsEmpty() ? null : _dateTimeParser.Parse(str).UtcKind();

				str = reader.ReadString();
				message.To = str.IsEmpty() ? null : _dateTimeParser.Parse(str).UtcKind();

				message.Count = reader.ReadNullableLong();

				message.BuildMode = reader.ReadEnum<MarketDataBuildModes>();
				reader.ReadString();
				message.BuildField = reader.ReadNullableEnum<Level1Fields>();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					message.IsFinishedOnly = reader.ReadBool();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					message.FillGaps = reader.ReadBool();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					var typeStr = reader.ReadString();
					var argStr = reader.ReadString();

					message.BuildFrom = typeStr.IsEmpty() ? null : typeStr.ToDataType(argStr);
				}

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					message.Skip = reader.ReadNullableLong();

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
					message.DoNotBuildOrderBookInrement = reader.ReadBool();

				return message;
			}
		}

		private const string _dateTimeFormat = "yyyyMMddHHmmss";
		private static readonly FastDateTimeParser _dateTimeParser = new(_dateTimeFormat);

		private static DateTimeOffset? ReadNullableDateTime(FastCsvReader reader)
		{
			var str = reader.ReadString();

			if (str == null)
				return null;

			return _dateTimeParser.Parse(str).UtcKind();
		}

		private readonly List<ICsvEntityList> _csvLists = new();

		/// <summary>
		/// The path to data directory.
		/// </summary>
		public string Path { get; set; }

		private Encoding _encoding = Encoding.UTF8;

		/// <summary>
		/// Encoding.
		/// </summary>
		public Encoding Encoding
		{
			get => _encoding;
			set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
		}

		private DelayAction _delayAction = new(ex => ex.LogError());

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
			foreach (var csvList in _csvLists)
			{
				csvList.DelayAction = _delayAction;
			}
		}

		private readonly ExchangeCsvList _exchanges;

		/// <inheritdoc />
		public IStorageEntityList<Exchange> Exchanges => _exchanges;

		private readonly ExchangeBoardCsvList _exchangeBoards;

		/// <inheritdoc />
		public IStorageEntityList<ExchangeBoard> ExchangeBoards => _exchangeBoards;

		private readonly SecurityCsvList _securities;

		/// <inheritdoc />
		public IStorageSecurityList Securities => _securities;

		private readonly PortfolioCsvList _portfolios;

		/// <inheritdoc />
		public IStorageEntityList<Portfolio> Portfolios => _portfolios;

		private readonly PositionCsvList _positions;

		/// <inheritdoc />
		public IStoragePositionList Positions => _positions;

		/// <inheritdoc />
		public IPositionStorage PositionStorage { get; }

		private readonly SubscriptionCsvList _subscriptions;

		/// <inheritdoc />
		public IStorageEntityList<MarketDataMessage> Subscriptions => _subscriptions;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvEntityRegistry"/>.
		/// </summary>
		/// <param name="path">The path to data directory.</param>
		public CsvEntityRegistry(string path)
		{
			Path = path ?? throw new ArgumentNullException(nameof(path));

			Add(_exchanges = new ExchangeCsvList(this));
			Add(_exchangeBoards = new ExchangeBoardCsvList(this));
			Add(_securities = new SecurityCsvList(this));
			Add(_portfolios = new PortfolioCsvList(this));
			Add(_positions = new PositionCsvList(this));
			Add(_subscriptions = new SubscriptionCsvList(this));

			UpdateDelayAction();

			PositionStorage = new PositionStorage(this);
		}

		/// <summary>
		/// Add list of trade objects.
		/// </summary>
		/// <typeparam name="TKey">Key type.</typeparam>
		/// <typeparam name="TEntity">Entity type.</typeparam>
		/// <param name="list">List of trade objects.</param>
		public void Add<TKey, TEntity>(CsvEntityList<TKey, TEntity> list)
			where TEntity : class
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
						errors.Add(list, listErrors.SingleOrAggr());
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