namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The CSV storage of trading objects.
/// </summary>
public class CsvEntityRegistry : IEntityRegistry
{
	/// <summary>
	/// </summary>
	[Obsolete("This property exists only for backward compatibility.")]
	public object Storage => throw new NotSupportedException();

	private class ExchangeCsvList(CsvEntityRegistry registry) : CsvEntityList<string, Exchange>(registry, "exchange.csv")
	{
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
			};

			var engName = reader.ReadString();

			reader.Skip();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				board.FullNameLoc = reader.ReadString();
			else
				board.FullNameLoc = engName;

			return board;
		}

		protected override void Write(CsvFileWriter writer, Exchange data)
		{
			writer.WriteRow(
			[
				data.Name,
				data.CountryCode.To<string>(),
				string.Empty/*data.EngName*/,
				string.Empty/*data.RusName*/,
				data.FullNameLoc,
			]);
		}
	}

	private class ExchangeBoardCsvList(CsvEntityRegistry registry) : CsvEntityList<string, ExchangeBoard>(registry, "exchangeboard.csv")
	{
		protected override string GetKey(ExchangeBoard item)
		{
			return item.Code;
		}

		private Exchange GetExchange(string exchangeCode)
		{
			return Registry.Exchanges.ReadById(exchangeCode) ?? throw new InvalidOperationException(LocalizedStrings.BoardNotFound.Put(exchangeCode));
		}

		protected override ExchangeBoard Read(FastCsvReader reader)
		{
			var msg = reader.ReadBoard(Registry.Encoding);

			var board = msg.ToBoard();
			board.Exchange = GetExchange(msg.ExchangeCode);
			return board;
		}

		protected override void Write(CsvFileWriter writer, ExchangeBoard data)
		{
			writer.WriteRow(
			[
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
			]);
		}
	}

	private class SecurityCsvList : CsvEntityList<SecurityId, Security>, IStorageSecurityList
	{
		public SecurityCsvList(CsvEntityRegistry registry)
			: base(registry, "security.csv")
		{
			AddedRange += s => _added?.Invoke(s);
			RemovedRange += s => _removed?.Invoke(s);
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
			{
				lock (SyncRoot)
					return this.Filter(criteria);
			}

			var security = GetById(secId);
			return security == null ? [] : [security];
		}

		SecurityMessage ISecurityMessageProvider.LookupMessageById(SecurityId id)
			=> GetById(id)?.ToMessage();

		IEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessages(SecurityLookupMessage criteria)
			=> ((ISecurityProvider)this).Lookup(criteria).Select(s => s.ToMessage());

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

		private static void UpdateCache(Security cache, Security security)
		{
			if (cache is null)		throw new ArgumentNullException(nameof(cache));
			if (security is null)	throw new ArgumentNullException(nameof(security));

			var boardCode = security.Board?.Code;

			cache.Name = security.Name;
			cache.Code = security.Code;
			cache.Class = security.Class;
			cache.ShortName = security.ShortName;
			cache.Board = boardCode.IsEmpty() ? null : new() { Code = boardCode };
			cache.UnderlyingSecurityId = security.UnderlyingSecurityId;
			cache.PriceStep = security.PriceStep;
			cache.VolumeStep = security.VolumeStep;
			cache.MinVolume = security.MinVolume;
			cache.MaxVolume = security.MaxVolume;
			cache.Multiplier = security.Multiplier;
			cache.Decimals = security.Decimals;
			cache.Type = security.Type;
			cache.ExpiryDate = security.ExpiryDate;
			cache.SettlementDate = security.SettlementDate;
			cache.Strike = security.Strike;
			cache.OptionType = security.OptionType;
			cache.Currency = security.Currency;
			cache.ExternalId = security.ExternalId.Clone();
			cache.UnderlyingSecurityType = security.UnderlyingSecurityType;
			cache.UnderlyingSecurityMinVolume = security.UnderlyingSecurityMinVolume;
			cache.BinaryOptionType = security.BinaryOptionType;
			cache.CfiCode = security.CfiCode;
			cache.IssueDate = security.IssueDate;
			cache.IssueSize = security.IssueSize;
			cache.Shortable = security.Shortable;
			cache.BasketCode = security.BasketCode;
			cache.BasketExpression = security.BasketExpression;
			cache.PrimaryId = security.PrimaryId;
			cache.OptionStyle = security.OptionStyle;
			cache.SettlementType = security.SettlementType;
		}

		private readonly Dictionary<SecurityId, Security> _cache = [];

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
			var cache = _cache.TryGetValue(security.ToSecurityId())
				?? throw new InvalidOperationException(LocalizedStrings.SecurityNoFound.Put(security.Id));

			if (IsChanged(security.Name, cache.Name, forced))
				return true;

			if (IsChanged(security.Code, cache.Code, forced))
				return true;

			if (IsChanged(security.Class, cache.Class, forced))
				return true;

			if (IsChanged(security.ShortName, cache.ShortName, forced))
				return true;

			if (IsChanged(security.UnderlyingSecurityId, cache.UnderlyingSecurityId, forced))
				return true;

			if (IsChanged(security.UnderlyingSecurityType, cache.UnderlyingSecurityType, forced))
				return true;

			if (IsChanged(security.UnderlyingSecurityMinVolume, cache.UnderlyingSecurityMinVolume, forced))
				return true;

			if (IsChanged(security.PriceStep, cache.PriceStep, forced))
				return true;

			if (IsChanged(security.VolumeStep, cache.VolumeStep, forced))
				return true;

			if (IsChanged(security.MinVolume, cache.MinVolume, forced))
				return true;

			if (IsChanged(security.MaxVolume, cache.MaxVolume, forced))
				return true;

			if (IsChanged(security.Multiplier, cache.Multiplier, forced))
				return true;

			if (IsChanged(security.Decimals, cache.Decimals, forced))
				return true;

			if (IsChanged(security.Type, cache.Type, forced))
				return true;

			if (IsChanged(security.ExpiryDate, cache.ExpiryDate, forced))
				return true;

			if (IsChanged(security.SettlementDate, cache.SettlementDate, forced))
				return true;

			if (IsChanged(security.Strike, cache.Strike, forced))
				return true;

			if (IsChanged(security.OptionType, cache.OptionType, forced))
				return true;

			if (IsChanged(security.Currency, cache.Currency, forced))
				return true;

			if (IsChanged(security.BinaryOptionType, cache.BinaryOptionType, forced))
				return true;

			if (IsChanged(security.CfiCode, cache.CfiCode, forced))
				return true;

			if (IsChanged(security.Shortable, cache.Shortable, forced))
				return true;

			if (IsChanged(security.IssueDate, cache.IssueDate, forced))
				return true;

			if (IsChanged(security.IssueSize, cache.IssueSize, forced))
				return true;

			var cacheBoard = cache.Board;

			if (security.Board == null)
			{
				if (cacheBoard != null && forced)
					return true;
			}
			else
			{
				if (cacheBoard is null || (forced && !cacheBoard.Code.EqualsIgnoreCase(security.Board?.Code)))
					return true;
			}

			if (forced && security.ExternalId != cache.ExternalId)
				return true;

			if (IsChanged(security.BasketCode, cache.BasketCode, forced))
				return true;

			if (IsChanged(security.BasketExpression, cache.BasketExpression, forced))
				return true;

			if (IsChanged(security.PrimaryId, cache.PrimaryId, forced))
				return true;

			if (IsChanged(security.SettlementType, cache.SettlementType, forced))
				return true;

			if (IsChanged(security.OptionStyle, cache.OptionStyle, forced))
				return true;

			return false;
		}

		protected override void ClearCache()
		{
			_cache.Clear();
		}

		protected override void AddCache(Security item)
		{
			var cache = new Security { Id = item.Id };
			UpdateCache(cache, item);
			_cache.Add(item.ToSecurityId(), cache);
		}

		protected override void RemoveCache(Security item)
		{
			_cache.Remove(item.ToSecurityId());
		}

		protected override void UpdateCache(Security item)
		{
			UpdateCache(_cache[item.ToSecurityId()], item);
		}

		protected override Security Read(FastCsvReader reader)
		{
			var msg = reader.ReadSecurity();

			if (msg.IsAllSecurity())
				return EntitiesExtensions.AllSecurity;

			var secId = msg.SecurityId;

			return new()
			{
				Id = secId.ToStringId(),
				Name = msg.Name,
				Code = secId.SecurityCode,
				Class = msg.Class,
				ShortName = msg.ShortName,
				Board = Registry.GetBoard(secId.BoardCode),
				UnderlyingSecurityId = msg.UnderlyingSecurityId.ToStringId(nullIfEmpty: true),
				PriceStep = msg.PriceStep,
				VolumeStep = msg.VolumeStep,
				MinVolume = msg.MinVolume,
				MaxVolume = msg.MaxVolume,
				Multiplier = msg.Multiplier,
				Decimals = msg.Decimals,
				Type = msg.SecurityType,
				ExpiryDate = msg.ExpiryDate,
				SettlementDate = msg.SettlementDate,
				Strike = msg.Strike,
				OptionType = msg.OptionType,
				Currency = msg.Currency,
				ExternalId = secId.ToExternalId(),
				UnderlyingSecurityType = msg.UnderlyingSecurityType,
				UnderlyingSecurityMinVolume = msg.UnderlyingSecurityMinVolume,
				BinaryOptionType = msg.BinaryOptionType,
				CfiCode = msg.CfiCode,
				IssueDate = msg.IssueDate,
				IssueSize = msg.IssueSize,
				Shortable = msg.Shortable,
				BasketCode = msg.BasketCode,
				BasketExpression = msg.BasketExpression,
				PrimaryId = msg.PrimaryId.ToStringId(nullIfEmpty: true),
				OptionStyle = msg.OptionStyle,
				SettlementType = msg.SettlementType,
			};
		}

		protected override void Write(CsvFileWriter writer, Security data)
		{
			writer.WriteRow(
			[
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
				data.ExpiryDate?.WriteDateTime(),
				data.SettlementDate?.WriteDateTime(),
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
				data.IssueDate?.WriteDateTime(),
				data.IssueSize.To<string>(),
				data.BasketCode,
				data.BasketExpression,
				data.MinVolume.To<string>(),
				data.Shortable.To<string>(),
				data.UnderlyingSecurityMinVolume.To<string>(),
				data.MaxVolume.To<string>(),
				data.PrimaryId,
				data.SettlementType.To<string>(),
				data.OptionStyle.To<string>(),
			]);
		}

		public override void Save(Security entity, bool forced)
		{
			var board = entity.Board;

			if (board is not null)
			{
				lock (Registry.Exchanges.SyncRoot)
					Registry.Exchanges.TryAdd(board.Exchange);

				lock (Registry.ExchangeBoards.SyncRoot)
					Registry.ExchangeBoards.TryAdd(board);
			}

			base.Save(entity, forced);
		}

		#endregion
	}

	private class PortfolioCsvList(CsvEntityRegistry registry) : CsvEntityList<string, Portfolio>(registry, "portfolio.csv")
	{
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
				LastChangeTime = reader.ReadDateTime(),
				LocalTime = reader.ReadDateTime(),
			};

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				portfolio.ClientCode = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				portfolio.Currency = reader.ReadString().To<CurrencyTypes?>();
				portfolio.ExpirationDate = reader.ReadNullableDateTime();
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
			writer.WriteRow(
			[
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
				data.LastChangeTime.WriteDateTime(),
				data.LocalTime.WriteDateTime(),
				data.ClientCode,
				data.Currency?.To<string>(),
				data.ExpirationDate?.WriteDateTime(),
				data.CommissionMaker.To<string>(),
				data.CommissionTaker.To<string>(),
				/*data.InternalId.To<string>()*/string.Empty,
			]);
		}
	}

	private class PositionCsvList(CsvEntityRegistry registry) : CsvEntityList<(Portfolio, Security, string, Sides?), Position>(registry, "position.csv"), IStoragePositionList
	{
		protected override (Portfolio, Security, string, Sides?) GetKey(Position item)
			=> CreateKey(item.Portfolio, item.Security, item.StrategyId, item.Side);

		private Portfolio GetPortfolio(string id)
		{
			return Registry.Portfolios.ReadById(id)
				?? throw new InvalidOperationException(LocalizedStrings.PortfolioNotFound.Put(id));
		}

		private Security GetSecurity(string id)
		{
			var secId = id.ToSecurityId();
			var security = secId.IsMoney() ? EntitiesExtensions.MoneySecurity : Registry.Securities.ReadById(secId);

			return security ?? throw new InvalidOperationException(LocalizedStrings.SecurityNoFound.Put(id));
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
				LastChangeTime = reader.ReadDateTime(),
				LocalTime = reader.ReadDateTime(),
			};

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				position.ClientCode = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				position.Currency = reader.ReadString().To<CurrencyTypes?>();
				position.ExpirationDate = reader.ReadNullableDateTime();
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

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				position.LiquidationPrice = reader.ReadNullableDecimal();

			return position;
		}

		protected override void Write(CsvFileWriter writer, Position data)
		{
			writer.WriteRow(
			[
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
				data.LastChangeTime.WriteDateTime(),
				data.LocalTime.WriteDateTime(),
				data.ClientCode,
				data.Currency.To<string>(),
				data.ExpirationDate?.WriteDateTime(),
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
				data.LiquidationPrice.To<string>(),
			]);
		}

		public Position GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode = "", string depoName = "", TPlusLimits? limit = null)
			=> ((IStorageEntityList<Position>)this).ReadById(CreateKey(portfolio, security, strategyId, side));

		private static (Portfolio, Security, string, Sides?) CreateKey(Portfolio portfolio, Security security, string strategyId, Sides? side)
			=> (portfolio, security, strategyId?.ToLowerInvariant() ?? string.Empty, side);
	}

	private class SubscriptionCsvList(CsvEntityRegistry registry) : CsvEntityList<(SecurityId, DataType), MarketDataMessage>(registry, "subscription.csv")
	{
		protected override (SecurityId, DataType) GetKey(MarketDataMessage item)
			=> (item.SecurityId, item.DataType2);

		protected override void Write(CsvFileWriter writer, MarketDataMessage data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (!data.IsSubscribe)
				throw new ArgumentException("Must be subscribe request.");

			var (type, arg) = data.DataType2.FormatToString();
			var buildFromTuples = data.BuildFrom?.FormatToString();

			writer.WriteRow(
			[
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
				data.From?.WriteDateTime(),
				data.To?.WriteDateTime(),
				data.Count.To<string>(),
				data.BuildMode.To<string>(),
				null,
				data.BuildField.To<string>(),
				data.IsFinishedOnly.To<string>(),
				false.To<string>(),
				buildFromTuples?.type,
				buildFromTuples?.arg,
				data.Skip.To<string>(),
				data.DoNotBuildOrderBookIncrement.To<string>(),
				data.Fields?.Select(f => ((int)f).To<string>()).JoinComma(),
				data.FillGaps.To<string>(),
			]);
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
				IsRegularTradingHours = reader.ReadNullableBool(),

				MaxDepth = reader.ReadNullableInt(),
				NewsId = reader.ReadString(),

				From = reader.ReadNullableDateTime(),
				To = reader.ReadNullableDateTime(),
				Count = reader.ReadNullableLong(),

				BuildMode = reader.ReadEnum<MarketDataBuildModes>(),
			};

			reader.ReadString();
			message.BuildField = reader.ReadNullableEnum<Level1Fields>();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				message.IsFinishedOnly = reader.ReadBool();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				/*message.FillGaps =*/reader.ReadBool();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				var typeStr = reader.ReadString();
				var argStr = reader.ReadString();

				message.BuildFrom = typeStr.IsEmpty() ? null : typeStr.ToDataType(argStr);
			}

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				message.Skip = reader.ReadNullableLong();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				message.DoNotBuildOrderBookIncrement = reader.ReadBool();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				var str = reader.ReadString();

				if (!str.IsEmpty())
					message.Fields = [.. str.SplitByComma().Select(s => (Level1Fields)s.To<int>())];
			}

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				message.FillGaps = reader.ReadNullableEnum<FillGapsDays>();

			return message;
		}
	}

	private readonly List<ICsvEntityList> _csvLists = [];

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

		board = ServicesRegistry.EnsureGetExchangeInfoProvider().TryGetExchangeBoard(boardCode);

		if (board == null)
			throw new InvalidOperationException(LocalizedStrings.BoardNotFound.Put(boardCode));

		return board;
	}
}