namespace StockSharp.Algo.Export;

using Ecng.Data;

/// <summary>
/// The export into database.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseExporter"/>.
/// </remarks>
/// <param name="dbProvider"><see cref="IDatabaseProvider"/></param>
/// <param name="dataType">Data type info.</param>
/// <param name="connection">The connection to DB.</param>
/// <param name="priceStep">Minimum price step.</param>
/// <param name="volumeStep">Minimum volume step.</param>
public class DatabaseExporter(IDatabaseProvider dbProvider, DataType dataType, DatabaseConnectionPair connection, decimal? priceStep = null, decimal? volumeStep = null) : BaseExporter(dataType)
{
	private readonly DatabaseConnectionPair _connection = connection ?? throw new ArgumentNullException(nameof(connection));
	private readonly IDatabaseProvider _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));

	private static Type ToNullableDbType(Type t)
	{
		if (t == typeof(string))
			return t;

		if (t.IsEnum)
			t = t.GetEnumBaseType();

		return t.MakeNullable();
	}

	/// <summary>
	/// Minimum price step.
	/// </summary>
	public decimal? PriceStep { get; } = priceStep;

	/// <summary>
	/// Minimum volume step.
	/// </summary>
	public decimal? VolumeStep { get; } = volumeStep;

	private int _batchSize = 50;

	/// <summary>
	/// The size of transmitted data package. The default is 50 elements.
	/// </summary>
	public int BatchSize
	{
		get => _batchSize;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_batchSize = value;
		}
	}

	/// <summary>
	/// To check uniqueness of data in the database. It effects performance. The default is enabled.
	/// </summary>
	public bool CheckUnique { get; set; }

	/// <summary>
	/// Drop existing table before export. The default is disabled.
	/// </summary>
	public bool DropExisting { get; set; }

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportOrderLogAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(ExecutionMessage).Remove(nameof(Message)), GetExecutionColumns, ToExecutionDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicksAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(ExecutionMessage).Remove(nameof(Message)), GetExecutionColumns, ToExecutionDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactionsAsync(IAsyncEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(ExecutionMessage).Remove(nameof(Message)), GetExecutionColumns, ToExecutionDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages.SelectMany(m => m.ToTimeQuotes()), nameof(TimeQuoteChange), GetMarketDepthQuoteColumns, ToMarketDepthQuoteDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(Level1ChangeMessage).Remove(nameof(Message)).Remove("Change"), GetLevel1Columns, ToLevel1Dict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(CandleMessage).Remove(nameof(Message)), GetCandleColumns, ToCandleDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(NewsMessage).Remove(nameof(Message)), GetNewsColumns, ToNewsDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(SecurityMessage).Remove(nameof(Message)), GetSecurityColumns, ToSecurityDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(PositionChangeMessage).Remove(nameof(Message)).Remove("Change"), GetPositionChangeColumns, ToPositionChangeDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
		=> DoAsync(values, nameof(IndicatorValue), GetIndicatorValueColumns, ToIndicatorValueDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(BoardStateMessage).Remove(nameof(Message)), GetBoardStateColumns, ToBoardStateDict, cancellationToken);

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IAsyncEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
		=> DoAsync(messages, nameof(BoardMessage).Remove(nameof(Message)), GetBoardColumns, ToBoardDict, cancellationToken);

	private async Task<(int, DateTime?)> DoAsync<TValue>(
		IAsyncEnumerable<TValue> values,
		string tableName,
		Func<IDictionary<string, Type>> getColumns,
		Func<TValue, IDictionary<string, object>> toDict,
		CancellationToken cancellationToken)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var count = 0;
		var lastTime = default(DateTime?);

		using var db = _dbProvider.CreateConnection(_connection);
		var table = _dbProvider.GetTable(db, tableName);

		if (DropExisting)
			await table.DropAsync(cancellationToken);

		await table.CreateAsync(getColumns(), cancellationToken);

		await foreach (var batch in values.Chunk(BatchSize).WithCancellation(cancellationToken))
		{
			var rows = batch.Select(toDict).ToList();

			if (CheckUnique)
			{
				foreach (var row in rows)
					await table.InsertAsync(row, cancellationToken);
			}
			else
				await table.BulkInsertAsync(rows, cancellationToken);

			count += batch.Length;

			if (batch.LastOrDefault() is IServerTimeMessage timeMsg)
				lastTime = timeMsg.ServerTime;
		}

		return (count, lastTime);
	}

	#region Column Definitions

	private IDictionary<string, Type> GetCandleColumns() => new Dictionary<string, Type>
	{
		["SecurityCode"] = typeof(string),
		["BoardCode"] = typeof(string),
		["Type"] = typeof(string),
		["Arg"] = typeof(string),
		["OpenTime"] = typeof(DateTime),
		["OpenPrice"] = typeof(decimal),
		["HighPrice"] = typeof(decimal),
		["LowPrice"] = typeof(decimal),
		["ClosePrice"] = typeof(decimal),
		["TotalVolume"] = typeof(decimal?),
		["OpenInterest"] = typeof(decimal?),
		["TotalTicks"] = typeof(int?),
		["UpTicks"] = typeof(int?),
		["DownTicks"] = typeof(int?),
		["SeqNum"] = typeof(long?),
	};

	private IDictionary<string, Type> GetIndicatorValueColumns() => new Dictionary<string, Type>
	{
		["SecurityCode"] = typeof(string),
		["BoardCode"] = typeof(string),
		["Time"] = typeof(DateTime),
		["Value1"] = typeof(decimal?),
		["Value2"] = typeof(decimal?),
		["Value3"] = typeof(decimal?),
		["Value4"] = typeof(decimal?),
	};

	private IDictionary<string, Type> GetPositionChangeColumns()
	{
		var columns = new Dictionary<string, Type>
		{
			["ServerTime"] = typeof(DateTime),
			["PortfolioName"] = typeof(string),
			["SecurityCode"] = typeof(string),
			["BoardCode"] = typeof(string),
		};

		foreach (var field in Enumerator.GetValues<PositionChangeTypes>().ExcludeObsolete())
		{
			var t = field.ToType();
			if (t != null)
				columns[field.To<string>()] = ToNullableDbType(t);
		}

		return columns;
	}

	private IDictionary<string, Type> GetSecurityColumns() => new Dictionary<string, Type>
	{
		["SecurityCode"] = typeof(string),
		["BoardCode"] = typeof(string),
		["Name"] = typeof(string),
		["ShortName"] = typeof(string),
		["PriceStep"] = typeof(decimal?),
		["VolumeStep"] = typeof(decimal?),
		["MinVolume"] = typeof(decimal?),
		["MaxVolume"] = typeof(decimal?),
		["Multiplier"] = typeof(decimal?),
		["Decimals"] = typeof(int?),
		["SecurityType"] = typeof(string),
		["OptionType"] = typeof(string),
		["BinaryOptionType"] = typeof(string),
		["Strike"] = typeof(decimal?),
		["UnderlyingSecurityCode"] = typeof(string),
		["UnderlyingBoardCode"] = typeof(string),
		["UnderlyingSecurityType"] = typeof(string),
		["UnderlyingSecurityMinVolume"] = typeof(decimal?),
		["ExpiryDate"] = typeof(DateTime?),
		["Currency"] = typeof(string),
		["SettlementDate"] = typeof(DateTime?),
		["IssueDate"] = typeof(DateTime?),
		["IssueSize"] = typeof(decimal?),
		["CfiCode"] = typeof(string),
		["Shortable"] = typeof(bool?),
		["BasketCode"] = typeof(string),
		["BasketExpression"] = typeof(string),
		["FaceValue"] = typeof(decimal?),
		["OptionStyle"] = typeof(int?),
		["SettlementType"] = typeof(int?),
		["Bloomberg"] = typeof(string),
		["Cusip"] = typeof(string),
		["IQFeed"] = typeof(string),
		["InteractiveBrokers"] = typeof(int?),
		["Isin"] = typeof(string),
		["Plaza"] = typeof(string),
		["Ric"] = typeof(string),
		["Sedol"] = typeof(string),
		["PrimarySecurityCode"] = typeof(string),
		["PrimaryBoardCode"] = typeof(string),
	};

	private IDictionary<string, Type> GetNewsColumns() => new Dictionary<string, Type>
	{
		["Id"] = typeof(string),
		["ServerTime"] = typeof(DateTime),
		["BoardCode"] = typeof(string),
		["Headline"] = typeof(string),
		["Story"] = typeof(string),
		["Source"] = typeof(string),
		["Url"] = typeof(string),
		["Priority"] = typeof(int?),
		["Language"] = typeof(string),
		["ExpiryDate"] = typeof(DateTime?),
		["SeqNum"] = typeof(long?),
	};

	private IDictionary<string, Type> GetLevel1Columns()
	{
		var columns = new Dictionary<string, Type>
		{
			["ServerTime"] = typeof(DateTime),
			["SecurityCode"] = typeof(string),
			["BoardCode"] = typeof(string),
		};

		foreach (var field in Enumerator.GetValues<Level1Fields>().ExcludeObsolete())
		{
			var t = field.ToType();
			if (t != null)
				columns[field.To<string>()] = ToNullableDbType(t);
		}

		return columns;
	}

	private IDictionary<string, Type> GetMarketDepthQuoteColumns() => new Dictionary<string, Type>
	{
		["SecurityCode"] = typeof(string),
		["BoardCode"] = typeof(string),
		["ServerTime"] = typeof(DateTime),
		["Price"] = typeof(decimal),
		["Volume"] = typeof(decimal),
		["Side"] = typeof(int),
		["OrdersCount"] = typeof(int?),
		["Condition"] = typeof(int?),
		["StartPosition"] = typeof(int?),
		["EndPosition"] = typeof(int?),
		["Action"] = typeof(int?),
	};

	private IDictionary<string, Type> GetExecutionColumns() => new Dictionary<string, Type>
	{
		["SecurityCode"] = typeof(string),
		["BoardCode"] = typeof(string),
		["ServerTime"] = typeof(DateTime),
		["TransactionId"] = typeof(long?),
		["OriginalTransactionId"] = typeof(long?),
		["OrderId"] = typeof(string),
		["OrderPrice"] = typeof(decimal?),
		["OrderVolume"] = typeof(decimal?),
		["VisibleVolume"] = typeof(decimal?),
		["Balance"] = typeof(decimal?),
		["Side"] = typeof(int?),
		["OrderType"] = typeof(int?),
		["OrderStatus"] = typeof(long?),
		["OrderState"] = typeof(int?),
		["TimeInForce"] = typeof(int?),
		["PortfolioName"] = typeof(string),
		["ClientCode"] = typeof(string),
		["BrokerCode"] = typeof(string),
		["DepoName"] = typeof(string),
		["ExpiryDate"] = typeof(DateTime?),
		["TradeId"] = typeof(string),
		["TradePrice"] = typeof(decimal?),
		["TradeVolume"] = typeof(decimal?),
		["OpenInterest"] = typeof(decimal?),
		["OriginSide"] = typeof(int?),
		["TradeStatus"] = typeof(long?),
		["IsUpTick"] = typeof(bool?),
		["HasOrderInfo"] = typeof(bool?),
		["IsSystem"] = typeof(bool?),
		["IsCancellation"] = typeof(bool?),
		["Currency"] = typeof(int?),
		["Comment"] = typeof(string),
		["SystemComment"] = typeof(string),
		["Error"] = typeof(string),
		["Commission"] = typeof(decimal?),
		["CommissionCurrency"] = typeof(string),
		["Slippage"] = typeof(decimal?),
		["Latency"] = typeof(long?),
		["Position"] = typeof(decimal?),
		["PnL"] = typeof(decimal?),
		["UserOrderId"] = typeof(string),
		["StrategyId"] = typeof(string),
		["MarginMode"] = typeof(int?),
		["IsMarketMaker"] = typeof(bool?),
		["IsManual"] = typeof(bool?),
		["AveragePrice"] = typeof(decimal?),
		["Yield"] = typeof(decimal?),
		["MinVolume"] = typeof(decimal?),
		["PositionEffect"] = typeof(int?),
		["PostOnly"] = typeof(bool?),
		["Initiator"] = typeof(bool?),
		["Leverage"] = typeof(int?),
		["SeqNum"] = typeof(long?),
		["MarketPrice"] = typeof(decimal?),
	};

	private IDictionary<string, Type> GetBoardStateColumns() => new Dictionary<string, Type>
	{
		["ServerTime"] = typeof(DateTime),
		["BoardCode"] = typeof(string),
		["State"] = typeof(int),
	};

	private IDictionary<string, Type> GetBoardColumns() => new Dictionary<string, Type>
	{
		["Code"] = typeof(string),
		["ExchangeCode"] = typeof(string),
		["ExpiryTime"] = typeof(TimeSpan?),
		["TimeZone"] = typeof(string),
	};

	#endregion

	#region Value Converters

	private IDictionary<string, object> ToCandleDict(CandleMessage m) => new Dictionary<string, object>
	{
		["SecurityCode"] = m.SecurityId.SecurityCode,
		["BoardCode"] = m.SecurityId.BoardCode,
		["Type"] = m.Type.To<string>(),
		["Arg"] = m.DataType.Arg switch { TimeSpan tf => tf.Ticks, Unit u => u.ToString(), PnFArg pnf => pnf.ToString(), var x => x?.ToString() },
		["OpenTime"] = m.OpenTime,
		["OpenPrice"] = m.OpenPrice,
		["HighPrice"] = m.HighPrice,
		["LowPrice"] = m.LowPrice,
		["ClosePrice"] = m.ClosePrice,
		["TotalVolume"] = m.TotalVolume,
		["OpenInterest"] = m.OpenInterest,
		["TotalTicks"] = m.TotalTicks,
		["UpTicks"] = m.UpTicks,
		["DownTicks"] = m.DownTicks,
		["SeqNum"] = m.SeqNum,
	};

	private IDictionary<string, object> ToIndicatorValueDict(IndicatorValue m) => new Dictionary<string, object>
	{
		["SecurityCode"] = m.SecurityId.SecurityCode,
		["BoardCode"] = m.SecurityId.BoardCode,
		["Time"] = m.Time,
		["Value1"] = m.Value1,
		["Value2"] = m.Value2,
		["Value3"] = m.Value3,
		["Value4"] = m.Value4,
	};

	private IDictionary<string, object> ToPositionChangeDict(PositionChangeMessage m)
	{
		var dict = new Dictionary<string, object>
		{
			["ServerTime"] = m.ServerTime,
			["PortfolioName"] = m.PortfolioName,
			["SecurityCode"] = m.SecurityId.SecurityCode,
			["BoardCode"] = m.SecurityId.BoardCode,
		};

		foreach (var change in m.Changes)
			dict[change.Key.To<string>()] = change.Value;

		return dict;
	}

	private IDictionary<string, object> ToSecurityDict(SecurityMessage m) => new Dictionary<string, object>
	{
		["SecurityCode"] = m.SecurityId.SecurityCode,
		["BoardCode"] = m.SecurityId.BoardCode,
		["Name"] = m.Name,
		["ShortName"] = m.ShortName,
		["PriceStep"] = m.PriceStep,
		["VolumeStep"] = m.VolumeStep,
		["MinVolume"] = m.MinVolume,
		["MaxVolume"] = m.MaxVolume,
		["Multiplier"] = m.Multiplier,
		["Decimals"] = m.Decimals,
		["SecurityType"] = m.SecurityType?.To<string>(),
		["OptionType"] = m.OptionType?.To<string>(),
		["BinaryOptionType"] = m.BinaryOptionType,
		["Strike"] = m.Strike,
		["UnderlyingSecurityCode"] = m.UnderlyingSecurityId.SecurityCode,
		["UnderlyingBoardCode"] = m.UnderlyingSecurityId.BoardCode,
		["UnderlyingSecurityType"] = m.UnderlyingSecurityType?.To<string>(),
		["UnderlyingSecurityMinVolume"] = m.UnderlyingSecurityMinVolume,
		["ExpiryDate"] = m.ExpiryDate,
		["Currency"] = m.Currency?.To<string>(),
		["SettlementDate"] = m.SettlementDate,
		["IssueDate"] = m.IssueDate,
		["IssueSize"] = m.IssueSize,
		["CfiCode"] = m.CfiCode,
		["Shortable"] = m.Shortable,
		["BasketCode"] = m.BasketCode,
		["BasketExpression"] = m.BasketExpression,
		["FaceValue"] = m.FaceValue,
		["OptionStyle"] = (int?)m.OptionStyle,
		["SettlementType"] = (int?)m.SettlementType,
		["Bloomberg"] = m.SecurityId.Bloomberg,
		["Cusip"] = m.SecurityId.Cusip,
		["IQFeed"] = m.SecurityId.IQFeed,
		["InteractiveBrokers"] = m.SecurityId.InteractiveBrokers,
		["Isin"] = m.SecurityId.Isin,
		["Plaza"] = m.SecurityId.Plaza,
		["Ric"] = m.SecurityId.Ric,
		["Sedol"] = m.SecurityId.Sedol,
		["PrimarySecurityCode"] = m.PrimaryId.SecurityCode,
		["PrimaryBoardCode"] = m.PrimaryId.BoardCode,
	};

	private IDictionary<string, object> ToNewsDict(NewsMessage m) => new Dictionary<string, object>
	{
		["Id"] = m.Id,
		["ServerTime"] = m.ServerTime,
		["BoardCode"] = m.BoardCode,
		["Headline"] = m.Headline,
		["Story"] = m.Story,
		["Source"] = m.Source,
		["Url"] = m.Url,
		["Priority"] = (int?)m.Priority,
		["Language"] = m.Language,
		["ExpiryDate"] = m.ExpiryDate,
		["SeqNum"] = m.SeqNum,
	};

	private IDictionary<string, object> ToLevel1Dict(Level1ChangeMessage m)
	{
		var dict = new Dictionary<string, object>
		{
			["ServerTime"] = m.ServerTime,
			["SecurityCode"] = m.SecurityId.SecurityCode,
			["BoardCode"] = m.SecurityId.BoardCode,
		};

		foreach (var change in m.Changes)
			dict[change.Key.To<string>()] = change.Value;

		return dict;
	}

	private IDictionary<string, object> ToMarketDepthQuoteDict(TimeQuoteChange m) => new Dictionary<string, object>
	{
		["SecurityCode"] = m.SecurityId.SecurityCode,
		["BoardCode"] = m.SecurityId.BoardCode,
		["ServerTime"] = m.ServerTime,
		["Price"] = m.Quote.Price,
		["Volume"] = m.Quote.Volume,
		["Side"] = (int)m.Side,
		["OrdersCount"] = m.Quote.OrdersCount,
		["Condition"] = (int?)m.Quote.Condition,
		["StartPosition"] = m.Quote.StartPosition,
		["EndPosition"] = m.Quote.EndPosition,
		["Action"] = (int?)m.Quote.Action,
	};

	private IDictionary<string, object> ToExecutionDict(ExecutionMessage m) => new Dictionary<string, object>
	{
		["SecurityCode"] = m.SecurityId.SecurityCode,
		["BoardCode"] = m.SecurityId.BoardCode,
		["ServerTime"] = m.ServerTime,
		["TransactionId"] = m.TransactionId,
		["OriginalTransactionId"] = m.OriginalTransactionId,
		["OrderId"] = m.OrderId?.To<string>(),
		["OrderPrice"] = m.OrderPrice,
		["OrderVolume"] = m.OrderVolume,
		["VisibleVolume"] = m.VisibleVolume,
		["Balance"] = m.Balance,
		["Side"] = (int?)m.Side,
		["OrderType"] = (int?)m.OrderType,
		["OrderStatus"] = m.OrderStatus,
		["OrderState"] = (int?)m.OrderState,
		["TimeInForce"] = (int?)m.TimeInForce,
		["PortfolioName"] = m.PortfolioName,
		["ClientCode"] = m.ClientCode,
		["BrokerCode"] = m.BrokerCode,
		["DepoName"] = m.DepoName,
		["ExpiryDate"] = m.ExpiryDate,
		["TradeId"] = m.TradeId?.To<string>(),
		["TradePrice"] = m.TradePrice,
		["TradeVolume"] = m.TradeVolume,
		["OpenInterest"] = m.OpenInterest,
		["OriginSide"] = (int?)m.OriginSide,
		["TradeStatus"] = m.TradeStatus,
		["IsUpTick"] = m.IsUpTick,
		["HasOrderInfo"] = m.HasOrderInfo,
		["IsSystem"] = m.IsSystem,
		["IsCancellation"] = m.IsCancellation,
		["Currency"] = (int?)m.Currency,
		["Comment"] = m.Comment,
		["SystemComment"] = m.SystemComment,
		["Error"] = m.Error?.Message,
		["Commission"] = m.Commission,
		["CommissionCurrency"] = m.CommissionCurrency,
		["Slippage"] = m.Slippage,
		["Latency"] = m.Latency?.Ticks,
		["Position"] = m.Position,
		["PnL"] = m.PnL,
		["UserOrderId"] = m.UserOrderId,
		["StrategyId"] = m.StrategyId,
		["MarginMode"] = (int?)m.MarginMode,
		["IsMarketMaker"] = m.IsMarketMaker,
		["IsManual"] = m.IsManual,
		["AveragePrice"] = m.AveragePrice,
		["Yield"] = m.Yield,
		["MinVolume"] = m.MinVolume,
		["PositionEffect"] = (int?)m.PositionEffect,
		["PostOnly"] = m.PostOnly,
		["Initiator"] = m.Initiator,
		["Leverage"] = m.Leverage,
		["SeqNum"] = m.SeqNum,
		["MarketPrice"] = m.MarketPrice,
	};

	private IDictionary<string, object> ToBoardStateDict(BoardStateMessage m) => new Dictionary<string, object>
	{
		["ServerTime"] = m.ServerTime,
		["BoardCode"] = m.BoardCode,
		["State"] = (int)m.State,
	};

	private IDictionary<string, object> ToBoardDict(BoardMessage m) => new Dictionary<string, object>
	{
		["Code"] = m.Code,
		["ExchangeCode"] = m.ExchangeCode,
		["ExpiryTime"] = m.ExpiryTime.Ticks,
		["TimeZone"] = m.TimeZone?.Id,
	};

	#endregion
}
