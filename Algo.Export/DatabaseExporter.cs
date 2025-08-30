namespace StockSharp.Algo.Export;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using Ecng.Data;
using Ecng.Collections;

using StockSharp.Messages;
using StockSharp.Localization;
using DataType = StockSharp.Messages.DataType;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

/// <summary>
/// The export into database.
/// </summary>
public class DatabaseExporter : BaseExporter
{
	private readonly DatabaseConnectionPair _connection;

	/// <summary>
	/// Initializes a new instance of the <see cref="DatabaseExporter"/>.
	/// </summary>
	/// <param name="priceStep">Minimum price step.</param>
	/// <param name="volumeStep">Minimum volume step.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="isCancelled">The processor, returning process interruption sign.</param>
	/// <param name="connection">The connection to DB.</param>
	public DatabaseExporter(decimal? priceStep, decimal? volumeStep, DataType dataType, Func<int, bool> isCancelled, DatabaseConnectionPair connection)
		: base(dataType, isCancelled, nameof(DatabaseExporter))
	{
		PriceStep = priceStep;
		VolumeStep = volumeStep;
		_connection = connection ?? throw new ArgumentNullException(nameof(connection));
	}

	/// <summary>
	/// Minimum price step.
	/// </summary>
	public decimal? PriceStep { get; }

	/// <summary>
	/// Minimum volume step.
	/// </summary>
	public decimal? VolumeStep { get; }

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

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
		=> Do(messages, CreateExecutionTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
		=> Do(messages, CreateExecutionTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
		=> Do(messages, CreateExecutionTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
		=> Do(messages.ToTimeQuotes(), CreateMarketDepthQuoteTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
		=> Do(messages, CreateLevel1Table);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
		=> Do(messages, CreateCandleTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
		=> Do(messages, CreateNewsTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
		=> Do(messages, CreateSecurityTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
		=> Do(messages, CreatePositionChangeTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
		=> Do(values, CreateIndicatorValueTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
		=> Do(messages, CreateBoardStateTable);

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
		=> Do(messages, CreateBoardTable);

	private (int, DateTimeOffset?) Do<TValue>(IEnumerable<TValue> values, Action<string, FluentMappingBuilder> createTable)
		where TValue : class
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		if (createTable is null)
			throw new ArgumentNullException(nameof(createTable));

		var count = 0;
		var lastTime = default(DateTimeOffset?);

		using var db = _connection.CreateConnection();
		
		//provider.CheckUnique = CheckUnique;

		var sp = db.DataProvider.GetSchemaProvider();
		var dbSchema = sp.GetSchema(db);

		var tableName = typeof(TValue).Name.Remove(nameof(Message)).Remove("Change");

		var schema = db.MappingSchema;

		schema.SetConverter<object, DataParameter>(obj => new()
		{
			Value = obj switch
			{
				TimeSpan tf => tf.Ticks,
				Unit u => u.ToString(),
				PnFArg pnf => pnf.ToString(),
				_ => obj,
			}
		});

		var builder = new FluentMappingBuilder(schema);
		createTable(tableName, builder);
		builder.Build();

		var exist = dbSchema.Tables.Any(t => t.TableName == tableName);

		var table = exist
			? db.GetTable<TValue>()
			: db.CreateTable<TValue>();

		foreach (var batch in values.Chunk(BatchSize).Select(b => b.ToArray()))
		{
			if (!CanProcess(batch.Length))
				break;

			if (CheckUnique)
			{
				foreach (var item in batch)
					table.Insert(() => item);
			}
			else
				table.BulkCopy(batch);

			count += batch.Length;

			if (batch.LastOrDefault() is IServerTimeMessage timeMsg)
				lastTime = timeMsg.ServerTime;
		}

		return (count, lastTime);
	}

	private int GetPriceScale() => (PriceStep ?? 1m).GetCachedDecimals();
	private int GetVolumeScale() => (VolumeStep ?? 1m).GetCachedDecimals();

	private void CreateCandleTable(string tableName, FluentMappingBuilder builder)
	{
		var priceScale = GetPriceScale();
		var volScale = GetVolumeScale();

		builder
			.Entity<CandleMessage>()
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(m => m.SecurityId.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.BoardCode).HasLength(256)
			.Property(m => m.Type).HasLength(32)
			.Property(m => m.DataType.Arg).HasLength(100)
			.Property(m => m.OpenTime)
			.Property(m => m.CloseTime)
			.Property(m => m.HighTime)
			.Property(m => m.LowTime)
			.Property(m => m.OpenPrice).HasScale(priceScale)
			.Property(m => m.HighPrice).HasScale(priceScale)
			.Property(m => m.LowPrice).HasScale(priceScale)
			.Property(m => m.ClosePrice).HasScale(priceScale)
			.Property(m => m.TotalVolume).HasScale(volScale)
			.Property(m => m.OpenInterest).HasScale(volScale)
			.Property(m => m.TotalTicks)
			.Property(m => m.UpTicks)
			.Property(m => m.DownTicks)
			.Property(m => m.SeqNum)
		;
	}

	private void CreateIndicatorValueTable(string tableName, FluentMappingBuilder builder)
	{
		builder
			.Entity<IndicatorValue>()
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(m => m.SecurityId.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.BoardCode).HasLength(256)
			.Property(m => m.Time)
			.Property(m => m.Value1)
			.Property(m => m.Value2)
			.Property(m => m.Value3)
			.Property(m => m.Value4)
		;
	}

	private void CreatePositionChangeTable(string tableName, FluentMappingBuilder builder)
	{
		var entityBuilder = builder
			.Entity<PositionChangeMessage>()
			.HasTableName(tableName)
			.IsColumnRequired()
			;

		entityBuilder
			.Property(m => m.ServerTime).IsNotNull()
			.Property(m => m.LocalTime).IsNotNull()
			.Property(m => m.PortfolioName)
			.Property(m => m.SecurityId.SecurityCode).HasLength(256).IsNotNull()
			.Property(m => m.SecurityId.BoardCode).HasLength(256).IsNotNull()
		;

		foreach (var item in Enumerator.GetValues<PositionChangeTypes>().ExcludeObsolete())
		{
			entityBuilder.Property(x => Sql.Property<object>(x, item.To<string>()));
		}

		entityBuilder.DynamicPropertyAccessors(
			(entity, fieldName, defaultValue) => entity.Changes.TryGetValue(fieldName.To<PositionChangeTypes>()),
			(entity, fieldName, value) => SetValue(entity, fieldName, value));
	}

	private void CreateSecurityTable(string tableName, FluentMappingBuilder builder)
	{
		builder
			.Entity<SecurityMessage>()
			.IsColumnRequired()
			.HasTableName(tableName)
			.Property(m => m.SecurityId.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.BoardCode).HasLength(256)
			.Property(m => m.Name).HasLength(256)
			.Property(m => m.ShortName).HasLength(64)
			.Property(m => m.PriceStep)
			.Property(m => m.VolumeStep)
			.Property(m => m.MinVolume).HasScale(1)
			.Property(m => m.MaxVolume).HasScale(1)
			.Property(m => m.Multiplier).HasScale(1)
			.Property(m => m.Decimals)
			.Property(m => m.SecurityType).HasLength(32)
			.Property(m => m.OptionType).HasLength(32)
			.Property(m => m.BinaryOptionType).HasLength(256)
			.Property(m => m.Strike)
			.Property(m => m.UnderlyingSecurityId.SecurityCode).HasLength(256)
			.Property(m => m.UnderlyingSecurityId.BoardCode).HasLength(256)
			.Property(m => m.UnderlyingSecurityType).HasLength(32)
			.Property(m => m.UnderlyingSecurityMinVolume).HasScale(1)
			.Property(m => m.ExpiryDate)
			.Property(m => m.Currency).HasLength(3)
			.Property(m => m.SettlementDate)
			.Property(m => m.IssueDate)
			.Property(m => m.IssueSize)
			.Property(m => m.CfiCode).HasLength(6)
			.Property(m => m.Shortable)
			.Property(m => m.BasketCode).HasLength(2)
			.Property(m => m.BasketExpression)
			.Property(m => m.FaceValue)
			.Property(m => m.OptionStyle)
			.Property(m => m.SettlementType)
			.Property(m => m.SecurityId.Bloomberg).HasLength(16)
			.Property(m => m.SecurityId.Cusip).HasLength(16)
			.Property(m => m.SecurityId.IQFeed).HasLength(16)
			.Property(m => m.SecurityId.InteractiveBrokers)
			.Property(m => m.SecurityId.Isin).HasLength(16)
			.Property(m => m.SecurityId.Plaza).HasLength(16)
			.Property(m => m.SecurityId.Ric).HasLength(16)
			.Property(m => m.SecurityId.Sedol).HasLength(16)
			.Property(m => m.PrimaryId.SecurityCode).HasColumnName(nameof(SecurityMessage.PrimaryId) + nameof(SecurityId.SecurityCode)).HasLength(64)
			.Property(m => m.PrimaryId.BoardCode).HasColumnName(nameof(SecurityMessage.PrimaryId) + nameof(SecurityId.BoardCode)).HasLength(32)
		;
	}

	private void CreateNewsTable(string tableName, FluentMappingBuilder builder)
	{
		builder
			.Entity<NewsMessage>()
			.IsColumnRequired()
			.HasTableName(tableName)
			.Property(m => m.Id).HasLength(32)
			.Property(m => m.ServerTime)
			.Property(m => m.LocalTime)
			.Property(m => m.SecurityId.Value.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.Value.BoardCode).HasLength(256)
			.Property(m => m.Headline).HasLength(256)
			.Property(m => m.Story)
			.Property(m => m.Source).HasLength(256)
			.Property(m => m.Url).HasLength(1024)
			.Property(m => m.Priority)
			.Property(m => m.Language).HasLength(8)
			.Property(m => m.ExpiryDate)
			.Property(m => m.SeqNum)
		;
	}

	private void CreateLevel1Table(string tableName, FluentMappingBuilder builder)
	{
		var entityBuilder = builder
			.Entity<Level1ChangeMessage>()
			.IsColumnRequired()
			.HasTableName(tableName)
		;

		entityBuilder
			.Property(m => m.ServerTime).IsNotNull()
			.Property(m => m.LocalTime).IsNotNull()
			.Property(m => m.SecurityId.SecurityCode).HasLength(256).IsNotNull()
			.Property(m => m.SecurityId.BoardCode).HasLength(256).IsNotNull()
		;

		foreach (var item in Enumerator.GetValues<Level1Fields>().ExcludeObsolete())
		{
			entityBuilder.Property(x => Sql.Property<object>(x, item.To<string>()));
		}

		entityBuilder.DynamicPropertyAccessors(
			(entity, fieldName, defaultValue) => entity.Changes.TryGetValue(fieldName.To<Level1Fields>()),
			(entity, fieldName, value) => SetValue(entity, fieldName, value));
	}

	private void CreateMarketDepthQuoteTable(string tableName, FluentMappingBuilder builder)
	{
		var priceScale = GetPriceScale();
		var volScale = GetVolumeScale();

		builder
			.Entity<TimeQuoteChange>()
			.IsColumnRequired()
			.HasTableName(tableName)
			.Property(m => m.SecurityId.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.BoardCode).HasLength(256)
			.Property(m => m.ServerTime)
			.Property(m => m.LocalTime)
			.Property(m => m.Quote.Price).HasScale(priceScale)
			.Property(m => m.Quote.Volume).HasScale(volScale)
			.Property(m => m.Side)
			.Property(m => m.Quote.OrdersCount)
			.Property(m => m.Quote.Condition)
			.Property(m => m.Quote.StartPosition)
			.Property(m => m.Quote.EndPosition)
			.Property(m => m.Quote.Action)
		;
	}

	private void CreateExecutionTable(string tableName, FluentMappingBuilder builder)
	{
		var priceScale = GetPriceScale();
		var volScale = GetVolumeScale();

		builder
			.Entity<ExecutionMessage>()
			.IsColumnRequired()
			.HasTableName(tableName)
			.Property(m => m.SecurityId.SecurityCode).HasLength(256)
			.Property(m => m.SecurityId.BoardCode).HasLength(256)
			.Property(m => m.ServerTime)
			.Property(m => m.LocalTime)

			.Property(m => m.TransactionId)
			.Property(m => m.OriginalTransactionId)

			.Property(m => m.OrderId).HasLength(32)
			.Property(m => m.OrderPrice).HasScale(priceScale)
			.Property(m => m.OrderVolume).HasScale(volScale)
			.Property(m => m.VisibleVolume).HasScale(volScale)
			.Property(m => m.Balance).HasScale(volScale)
			.Property(m => m.Side)
			.Property(m => m.OrderType)
			.Property(m => m.OrderStatus)
			.Property(m => m.OrderState)
			.Property(m => m.TimeInForce)
			.Property(m => m.PortfolioName).HasLength(32)
			.Property(m => m.ClientCode).HasLength(32)
			.Property(m => m.BrokerCode).HasLength(32)
			.Property(m => m.DepoName).HasLength(32)
			.Property(m => m.ExpiryDate)

			.Property(m => m.TradeId).HasLength(32)
			.Property(m => m.TradePrice).HasScale(priceScale)
			.Property(m => m.TradeVolume).HasScale(volScale)
			.Property(m => m.OpenInterest).HasScale(volScale)
			.Property(m => m.OriginSide)
			.Property(m => m.TradeStatus)
			.Property(m => m.IsUpTick)

			.Property(m => m.HasOrderInfo)

			.Property(m => m.IsSystem)
			.Property(m => m.IsCancellation)
			.Property(m => m.Currency)

			.Property(m => m.Comment).HasLength(1024)
			.Property(m => m.SystemComment).HasLength(1024)
			.Property(m => m.Error).HasDataType(LinqToDB.DataType.NVarChar).HasLength(1024)

			.Property(m => m.Commission)
			.Property(m => m.CommissionCurrency).HasLength(32)

			.Property(m => m.Slippage).HasScale(priceScale)
			.Property(m => m.Latency)
			.Property(m => m.Position).HasScale(volScale)
			.Property(m => m.PnL).HasScale(priceScale)

			.Property(m => m.UserOrderId).HasLength(32)
			.Property(m => m.StrategyId).HasLength(32)

			.Property(m => m.MarginMode)
			.Property(m => m.IsMarketMaker)
			.Property(m => m.IsManual)
			.Property(m => m.AveragePrice)
			.Property(m => m.Yield)
			.Property(m => m.MinVolume)
			.Property(m => m.PositionEffect)
			.Property(m => m.PostOnly)
			.Property(m => m.Initiator)
			.Property(m => m.Leverage)

			.Property(m => m.SeqNum)
		;
	}

	private void CreateBoardStateTable(string tableName, FluentMappingBuilder builder)
	{
		builder
			.Entity<BoardStateMessage>()
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(m => m.ServerTime)
			.Property(m => m.BoardCode).HasLength(256)
			.Property(m => m.State);
	}

	private void CreateBoardTable(string tableName, FluentMappingBuilder builder)
	{
		builder
			.Entity<BoardMessage>()
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(m => m.Code).HasLength(256)
			.Property(m => m.ExchangeCode).HasLength(256)
			.Property(m => m.ExpiryTime)
			.Property(m => m.TimeZone);
	}

	private static void SetValue<T>(T _, string _1, object _2)
		where T : Message
	{
	}
}