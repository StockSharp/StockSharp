namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The transaction serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransactionCsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
public class TransactionCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<ExecutionMessage>(securityId, encoding)
{
	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, ExecutionMessage data, IMarketDataMetaInfo metaInfo)
	{
		var row = new[]
		{
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			data.TransactionId.ToString(),
			data.OriginalTransactionId.ToString(),
			data.OrderId.ToString(),
			data.OrderStringId,
			data.OrderBoardId,
			data.UserOrderId,
			data.OrderPrice.ToString(),
			data.OrderVolume.ToString(),
			data.Balance.ToString(),
			data.VisibleVolume.ToString(),
			data.Side.To<int>().ToString(),
			data.OriginSide.To<int?>().ToString(),
			data.OrderState.To<int?>().ToString(),
			data.OrderType.To<int?>().ToString(),
			data.TimeInForce.To<int?>().ToString(),
			data.TradeId.ToString(),
			data.TradeStringId,
			data.TradePrice.ToString(),
			data.TradeVolume.ToString(),
			data.PortfolioName,
			data.ClientCode,
			data.BrokerCode,
			data.DepoName,
			data.IsSystem.To<int?>().ToString(),
			data.HasOrderInfo.To<int>().ToString(),
			data.HasTradeInfo.To<int>().ToString(),
			data.Commission.ToString(),
			data.Currency.To<int?>().ToString(),
			data.Comment,
			data.SystemComment,
			/*data.DerivedOrderId.ToString()*/string.Empty,
			/*data.DerivedOrderStringId*/string.Empty,
			data.IsUpTick.To<int?>().ToString(),
			/*data.IsCancellation.ToString()*/string.Empty,
			data.OpenInterest.ToString(),
			data.PnL.ToString(),
			data.Position.ToString(),
			data.Slippage.ToString(),
			data.TradeStatus.ToString(),
			data.OrderStatus.ToString(),
			data.Latency?.Ticks.ToString(),
			data.Error?.Message,
			data.ExpiryDate?.WriteDate(),
			data.ExpiryDate?.WriteTime(),
			data.ExpiryDate?.ToString("zzz"),
			data.LocalTime.WriteTime(),
			data.LocalTime.ToString("zzz"),
			data.IsMarketMaker.To<int?>().ToString(),
			data.CommissionCurrency,
			data.MarginMode.To<int?>().ToString(),
			data.IsManual.To<int?>().ToString(),
			data.MinVolume.To<string>(),
			data.PositionEffect.To<int?>().ToString(),
			data.PostOnly.To<int?>().ToString(),
			data.Initiator.To<int?>().ToString(),
			data.SeqNum.To<string>(),
			data.StrategyId,
			data.Leverage.To<string>(),
		}.Concat(data.BuildFrom.ToCsv());
		writer.WriteRow(row);

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
	}

	/// <inheritdoc />
	protected override ExecutionMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var msg = new ExecutionMessage
		{
			SecurityId = SecurityId,
			DataTypeEx = DataType.Transactions,
			ServerTime = reader.ReadTime(metaInfo.Date),
			TransactionId = reader.ReadLong(),
			OriginalTransactionId = reader.ReadLong(),
			OrderId = reader.ReadNullableLong(),
			OrderStringId = reader.ReadString(),
			OrderBoardId = reader.ReadString(),
			UserOrderId = reader.ReadString(),
			OrderPrice = reader.ReadDecimal(),
			OrderVolume = reader.ReadNullableDecimal(),
			Balance = reader.ReadNullableDecimal(),
			VisibleVolume = reader.ReadNullableDecimal(),
			Side = reader.ReadEnum<Sides>(),
			OriginSide = reader.ReadNullableEnum<Sides>(),
			OrderState = reader.ReadNullableEnum<OrderStates>(),
			OrderType = reader.ReadNullableEnum<OrderTypes>(),
			TimeInForce = reader.ReadNullableEnum<TimeInForce>(),
			TradeId = reader.ReadNullableLong(),
			TradeStringId = reader.ReadString(),
			TradePrice = reader.ReadNullableDecimal(),
			TradeVolume = reader.ReadNullableDecimal(),
			PortfolioName = reader.ReadString(),
			ClientCode = reader.ReadString(),
			BrokerCode = reader.ReadString(),
			DepoName = reader.ReadString(),
			IsSystem = reader.ReadNullableBool(),
			HasOrderInfo = reader.ReadBool(),
		};

		/*msg.HasTradeInfo = */reader.Skip();
		msg.Commission = reader.ReadNullableDecimal();
		msg.Currency = reader.ReadNullableEnum<CurrencyTypes>();
		msg.Comment = reader.ReadString();
		msg.SystemComment = reader.ReadString();

		/*msg.DerivedOrderId = */reader.Skip();
		/*msg.DerivedOrderStringId = */reader.Skip();

		msg.IsUpTick = reader.ReadNullableBool();
		/*msg.IsCancellation = */reader.Skip();
		msg.OpenInterest = reader.ReadNullableDecimal();
		msg.PnL = reader.ReadNullableDecimal();
		msg.Position = reader.ReadNullableDecimal();
		msg.Slippage = reader.ReadNullableDecimal();
		msg.TradeStatus = reader.ReadNullableLong();
		msg.OrderStatus = reader.ReadNullableLong();
		msg.Latency = reader.ReadNullableLong().To<TimeSpan?>();

		var error = reader.ReadString();

		if (!error.IsEmpty())
			msg.Error = new InvalidOperationException(error);

		var dtStr = reader.ReadString();

		if (dtStr != null)
		{
			msg.ExpiryDate = (dtStr.ToDateTime() + reader.ReadString().ToTimeMls()).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Remove("+")));
		}
		else
			reader.Skip(2);

		msg.LocalTime = reader.ReadTime(metaInfo.Date);
		msg.IsMarketMaker = reader.ReadNullableBool();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			msg.CommissionCurrency = reader.ReadString();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			msg.MarginMode = reader.ReadNullableEnum<MarginModes>();
			msg.IsManual = reader.ReadNullableBool();
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			msg.MinVolume = reader.ReadNullableDecimal();
			msg.PositionEffect = reader.ReadNullableEnum<OrderPositionEffects>();
			msg.PostOnly = reader.ReadNullableBool();
			msg.Initiator = reader.ReadNullableBool();
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			msg.SeqNum = reader.ReadLong();
			msg.StrategyId = reader.ReadString();
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			msg.Leverage = reader.ReadNullableInt();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			msg.BuildFrom = reader.ReadBuildFrom();

		return msg;
	}
}