#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: DatabaseExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Data;

	using MoreLinq;

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
			CheckUnique = true;
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
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

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

		private (int, DateTimeOffset?) Do<TValue>(IEnumerable<TValue> values, Action<FluentMappingBuilder> createTable)
			where TValue : class
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			if (createTable is null)
				throw new ArgumentNullException(nameof(createTable));

			var count = 0;
			var lastTime = default(DateTimeOffset?);

			using (var db = _connection.CreateConnection())
			{
				//provider.CheckUnique = CheckUnique;

				var tableName = typeof(TValue).Name;

				var sp = db.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(db);

				ITable<TValue> table;

				if (!dbSchema.Tables.Any(t => t.TableName == tableName))
				{
					var builder = db.MappingSchema.GetFluentMappingBuilder();
					createTable(builder);
					table = db.CreateTable<TValue>();
				}
				else
					table = db.GetTable<TValue>();

				foreach (var batch in values.Batch(BatchSize).Select(b => b.ToArray()))
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
			}

			return (count, lastTime);
		}

		private int GetPriceScale() => (PriceStep ?? 1m).GetCachedDecimals();
		private int GetVolumeScale() => VolumeStep?.GetCachedDecimals() ?? 1;

		private void CreateCandleTable(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			builder
				.Entity<CandleMessage>()
				.Property(m => m.SecurityId.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
				.Property(m => m.Type).HasLength(32)
				.Property(m => m.Arg).HasLength(100)
				.Property(m => m.OpenTime)
				.Property(m => m.CloseTime)
				.Property(m => m.HighTime)
				.Property(m => m.LowTime)
				.Property(m => m.CloseTime)
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

		private void CreateIndicatorValueTable(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			builder
				.Entity<IndicatorValue>()
				.Property(m => m.SecurityId.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
				.Property(m => m.Time)
				//.Property(m => m.OpenPrice).HasScale(priceScale)
				//.Property(m => m.HighPrice).HasScale(priceScale)
				//.Property(m => m.LowPrice).HasScale(priceScale)
				//.Property(m => m.ClosePrice).HasScale(priceScale)
				//.Property(m => m.TotalVolume).HasScale(volScale)
				//.Property(m => m.OpenInterest).HasScale(volScale)
				//.Property(m => m.TotalTicks)
				//.Property(m => m.UpTicks)
				//.Property(m => m.DownTicks)
				//.Property(m => m.SeqNum)
			;

			//for (var i = 0; i < _maxInnerValue; i++)
			//{
			//	yield return new ColumnDescription(nameof(IndicatorValue.Value) + (i + 1))
			//	{
			//		DbType = typeof(decimal?),
			//		ValueRestriction = new DecimalRestriction { Precision = 10, Scale = 6 }
			//	};
			//}
		}

		private void CreatePositionChangeTable(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			//yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(SecurityId.BoardCode))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.PortfolioName))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.ClientCode))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.DepoName))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.LimitType))
			//{
			//	DbType = typeof(int?),
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.StrategyId))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(32)
			//};
			//yield return new ColumnDescription(nameof(PositionChangeMessage.Side))
			//{
			//	DbType = typeof(int?),
			//};
			//yield return new ColumnDescription(nameof(Level1ChangeMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			//yield return new ColumnDescription(nameof(Level1ChangeMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };

			//foreach (var type in Enumerator.GetValues<PositionChangeTypes>().Where(t => !t.IsObsolete()))
			//{
			//	var columnType = GetDbType(type);

			//	if (columnType == null)
			//		continue;

			//	var step = 0.000001m;

			//	switch (type)
			//	{
			//		case PositionChangeTypes.State:
			//		case PositionChangeTypes.Currency:
			//			break;
			//			//default:
			//			//	step = security.Multiplier ?? 1;
			//			//	break;
			//	}

			//	yield return new ColumnDescription(type.ToString())
			//	{
			//		DbType = columnType.IsNullable() || columnType.IsClass ? columnType : typeof(Nullable<>).Make(columnType),
			//		ValueRestriction = columnType == typeof(decimal) ? new DecimalRestriction { Scale = step.GetCachedDecimals() } : null,
			//	};
			//}
		}

		private void CreateSecurityTable(FluentMappingBuilder builder)
		{
			builder
				.Entity<SecurityMessage>()
				.Property(m => m.SecurityId.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
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
				.Property(m => m.UnderlyingSecurityCode).HasLength(256)
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

		private void CreateNewsTable(FluentMappingBuilder builder)
		{
			builder
				.Entity<NewsMessage>()
				.Property(m => m.Id).HasLength(32)
				.Property(m => m.ServerTime)
				.Property(m => m.LocalTime)
				.Property(m => m.SecurityId.Value.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.Value.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
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

		private void CreateLevel1Table(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			//yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(SecurityId.BoardCode))
			//{
			//	DbType = typeof(string),
			//	ValueRestriction = new StringRestriction(256)
			//};
			//yield return new ColumnDescription(nameof(Level1ChangeMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			//yield return new ColumnDescription(nameof(Level1ChangeMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };
			//yield return new ColumnDescription(nameof(Level1ChangeMessage.SeqNum)) { DbType = typeof(long?) };

			//foreach (var field in Enumerator.GetValues<Level1Fields>().ExcludeObsolete())
			//{
			//	var columnType = GetDbType(field);

			//	if (columnType == null)
			//		continue;

			//	var step = 0.000001m;

			//	switch (field)
			//	{
			//		case Level1Fields.OpenPrice:
			//		case Level1Fields.HighPrice:
			//		case Level1Fields.LowPrice:
			//		case Level1Fields.ClosePrice:
			//		case Level1Fields.MinPrice:
			//		case Level1Fields.MaxPrice:
			//		case Level1Fields.PriceStep:
			//		case Level1Fields.LastTradePrice:
			//		case Level1Fields.BestBidPrice:
			//		case Level1Fields.BestAskPrice:
			//		case Level1Fields.HighBidPrice:
			//		case Level1Fields.LowAskPrice:
			//			step = priceStep ?? 1;
			//			break;
			//		case Level1Fields.OpenInterest:
			//		case Level1Fields.BidsVolume:
			//		case Level1Fields.AsksVolume:
			//		case Level1Fields.VolumeStep:
			//		case Level1Fields.LastTradeVolume:
			//		case Level1Fields.Volume:
			//		case Level1Fields.BestBidVolume:
			//		case Level1Fields.BestAskVolume:
			//			step = volumeStep ?? 1;
			//			break;
			//	}

			//	yield return new ColumnDescription(field.ToString())
			//	{
			//		DbType = columnType.IsNullable() ? columnType : typeof(Nullable<>).Make(columnType),
			//		ValueRestriction = columnType == typeof(decimal) ? new DecimalRestriction { Scale = step.GetCachedDecimals() } : null,
			//	};
			//}
		}

		private void CreateMarketDepthQuoteTable(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			builder
				.Entity<TimeQuoteChange>()
				.Property(m => m.SecurityId.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
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

		private void CreateExecutionTable(FluentMappingBuilder builder)
		{
			var priceScale = GetPriceScale();
			var volScale = GetVolumeScale();

			builder
				.Entity<ExecutionMessage>()
				.Property(m => m.SecurityId.SecurityCode).HasLength(256)
				.Property(m => m.SecurityId.BoardCode).HasLength(256)
				.Property(m => m.SecurityId).IsNotColumn()
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
				.Property(m => m.TradeVolume).HasScale(priceScale)
				.Property(m => m.OpenInterest).HasScale(volScale)
				.Property(m => m.OriginSide)
				.Property(m => m.TradeStatus)
				.Property(m => m.IsUpTick)

				.Property(m => m.HasOrderInfo)
				.Property(m => m.HasTradeInfo)

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

				.Property(m => m.IsMargin)
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
	}
}
