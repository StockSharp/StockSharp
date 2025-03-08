namespace StockSharp.Algo.Storages.Binary;

class TransactionSerializerMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override object LastId => LastTransactionId;

	public long FirstOrderId { get; set; } = -1;
	public long LastOrderId { get; set; }

	public long FirstTradeId { get; set; } = -1;
	public long LastTradeId { get; set; }

	public long FirstTransactionId { get; set; } = -1;
	public long LastTransactionId { get; set; }

	public long FirstOriginalTransactionId { get; set; } = -1;
	public long LastOriginalTransactionId { get; set; }

	public decimal FirstCommission { get; set; }
	public decimal LastCommission { get; set; }

	public decimal FirstPnL { get; set; }
	public decimal LastPnL { get; set; }

	public decimal FirstPosition { get; set; }
	public decimal LastPosition { get; set; }

	public decimal FirstSlippage { get; set; }
	public decimal LastSlippage { get; set; }

	public IList<string> Portfolios { get; } = [];
	public IList<string> ClientCodes { get; } = [];
	public IList<string> BrokerCodes { get; } = [];
	public IList<string> DepoNames { get; } = [];

	public IList<string> UserOrderIds { get; } = [];
	public IList<string> StrategyIds { get; } = [];

	public IList<string> Comments { get; } = [];
	public IList<string> SystemComments { get; } = [];

	public IList<string> Errors { get; } = [];

	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(FirstOrderId);
		stream.WriteEx(LastOrderId);
		stream.WriteEx(FirstTradeId);
		stream.WriteEx(LastTradeId);
		stream.WriteEx(FirstTransactionId);
		stream.WriteEx(LastTransactionId);
		stream.WriteEx(FirstOriginalTransactionId);
		stream.WriteEx(LastOriginalTransactionId);
		stream.WriteEx(FirstPrice);
		stream.WriteEx(LastPrice);
		stream.WriteEx(FirstCommission);
		stream.WriteEx(LastCommission);
		stream.WriteEx(FirstPnL);
		stream.WriteEx(LastPnL);
		stream.WriteEx(FirstPosition);
		stream.WriteEx(LastPosition);
		stream.WriteEx(FirstSlippage);
		stream.WriteEx(LastSlippage);

		WriteList(stream, Portfolios);
		WriteList(stream, ClientCodes);
		WriteList(stream, BrokerCodes);
		WriteList(stream, DepoNames);
		WriteList(stream, UserOrderIds);
		WriteList(stream, Comments);
		WriteList(stream, SystemComments);
		WriteList(stream, Errors);

		WriteFractionalPrice(stream);
		WriteFractionalVolume(stream);

		WriteLocalTime(stream, MarketDataVersions.Version47);

		stream.WriteEx(ServerOffset);

		WriteOffsets(stream);

		WriteItemLocalTime(stream, MarketDataVersions.Version59);
		WriteItemLocalOffset(stream, MarketDataVersions.Version59);

		if (Version < MarketDataVersions.Version66)
			return;

		WriteList(stream, StrategyIds);
	}

	private static void WriteList(Stream stream, IList<string> list)
	{
		stream.WriteEx(list.Count);

		foreach (var item in list)
			stream.WriteEx(item);
	}

	private static void ReadList(Stream stream, IList<string> list)
	{
		var count = stream.Read<int>();

		for (var i = 0; i < count; i++)
			list.Add(stream.Read<string>());
	}

	public override void Read(Stream stream)
	{
		base.Read(stream);

		FirstOrderId = stream.Read<long>();
		LastOrderId = stream.Read<long>();
		FirstTradeId = stream.Read<long>();
		LastTradeId = stream.Read<long>();
		FirstTransactionId = stream.Read<long>();
		LastTransactionId = stream.Read<long>();
		FirstOriginalTransactionId = stream.Read<long>();
		LastOriginalTransactionId = stream.Read<long>();
		FirstPrice = stream.Read<decimal>();
		LastPrice = stream.Read<decimal>();
		FirstCommission = stream.Read<decimal>();
		LastCommission = stream.Read<decimal>();
		FirstPnL = stream.Read<decimal>();
		LastPnL = stream.Read<decimal>();
		FirstPosition = stream.Read<decimal>();
		LastPosition = stream.Read<decimal>();
		FirstSlippage = stream.Read<decimal>();
		LastSlippage = stream.Read<decimal>();

		ReadList(stream, Portfolios);
		ReadList(stream, ClientCodes);
		ReadList(stream, BrokerCodes);
		ReadList(stream, DepoNames);
		ReadList(stream, UserOrderIds);
		ReadList(stream, Comments);
		ReadList(stream, SystemComments);
		ReadList(stream, Errors);

		ReadFractionalPrice(stream);
		ReadFractionalVolume(stream);

		ReadLocalTime(stream, MarketDataVersions.Version47);

		ServerOffset = stream.Read<TimeSpan>();

		ReadOffsets(stream);

		ReadItemLocalTime(stream, MarketDataVersions.Version59);
		ReadItemLocalOffset(stream, MarketDataVersions.Version59);

		if (Version < MarketDataVersions.Version66)
			return;

		ReadList(stream, StrategyIds);
	}

	public override void CopyFrom(BinaryMetaInfo src)
	{
		base.CopyFrom(src);

		var tsInfo = (TransactionSerializerMetaInfo)src;

		FirstOrderId = tsInfo.FirstOrderId;
		LastOrderId = tsInfo.LastOrderId;
		FirstTradeId = tsInfo.FirstTradeId;
		LastTradeId = tsInfo.LastTradeId;
		FirstTransactionId = tsInfo.FirstTransactionId;
		LastTransactionId = tsInfo.LastTransactionId;
		FirstOriginalTransactionId = tsInfo.FirstOriginalTransactionId;
		LastOriginalTransactionId = tsInfo.LastOriginalTransactionId;
		FirstCommission = tsInfo.FirstCommission;
		LastCommission = tsInfo.LastCommission;
		FirstPnL = tsInfo.FirstPnL;
		LastPnL = tsInfo.LastPnL;
		FirstPosition = tsInfo.FirstPosition;
		LastPosition = tsInfo.LastPosition;
		FirstSlippage = tsInfo.FirstSlippage;
		LastSlippage = tsInfo.LastSlippage;

		Portfolios.Clear();
		Portfolios.AddRange(tsInfo.Portfolios);

		ClientCodes.Clear();
		ClientCodes.AddRange(tsInfo.ClientCodes);

		BrokerCodes.Clear();
		BrokerCodes.AddRange(tsInfo.BrokerCodes);

		DepoNames.Clear();
		DepoNames.AddRange(tsInfo.DepoNames);

		UserOrderIds.Clear();
		UserOrderIds.AddRange(tsInfo.UserOrderIds);

		Comments.Clear();
		Comments.AddRange(tsInfo.Comments);

		SystemComments.Clear();
		SystemComments.AddRange(tsInfo.SystemComments);

		Errors.Clear();
		Errors.AddRange(tsInfo.Errors);

		StrategyIds.Clear();
		StrategyIds.AddRange(tsInfo.StrategyIds);
	}
}

class TransactionBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<ExecutionMessage, TransactionSerializerMetaInfo>(securityId, DataType.Transactions, 200, MarketDataVersions.Version70, exchangeInfoProvider)
{
	protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, TransactionSerializerMetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty())
		{
			var msg = messages.First();

			metaInfo.FirstOrderId = metaInfo.LastOrderId = msg.OrderId ?? 0;
			metaInfo.FirstTradeId = metaInfo.LastTradeId = msg.TradeId ?? 0;
			metaInfo.FirstTransactionId = metaInfo.LastTransactionId = msg.TransactionId;
			metaInfo.FirstOriginalTransactionId = metaInfo.LastOriginalTransactionId = msg.OriginalTransactionId;
			metaInfo.FirstCommission = metaInfo.LastCommission = msg.Commission ?? 0;
			metaInfo.FirstPnL = metaInfo.LastPnL = msg.PnL ?? 0;
			metaInfo.FirstPosition = metaInfo.LastPosition = msg.Position ?? 0;
			metaInfo.FirstSlippage = metaInfo.LastSlippage = msg.Slippage ?? 0;
			metaInfo.FirstItemLocalTime = metaInfo.LastItemLocalTime = msg.LocalTime.UtcDateTime;
			metaInfo.FirstItemLocalOffset = metaInfo.LastItemLocalOffset = msg.LocalTime.Offset;
			metaInfo.ServerOffset = msg.ServerTime.Offset;
		}

		writer.WriteInt(messages.Count());

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version51;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version56;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version60;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version67;
		var leverage = metaInfo.Version >= MarketDataVersions.Version68;
		var useLong = metaInfo.Version >= MarketDataVersions.Version69;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version69;

		foreach (var msg in messages)
		{
			if (msg.DataType != DataType.Transactions)
				throw new ArgumentOutOfRangeException(nameof(messages), msg.DataType, LocalizedStrings.UnknownType.Put(msg));

			// нулевой номер заявки возможен при сохранении в момент регистрации
			if (msg.OrderId < 0)
				throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderId, LocalizedStrings.OrderId);

			// нулевая цена возможна, если идет "рыночная" продажа по инструменту без планок
			if (msg.OrderPrice < 0)
				throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderPrice, LocalizedStrings.OrderPrice2);

			//var volume = msg.Volume;

			//if (volume < 0)
			//	throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.WrongOrderVolume.Put(msg.OrderId == null ? msg.OrderStringId : msg.OrderId.To<string>()));

			if (msg.HasTradeInfo())
			{
				if (msg.TradePrice is null or <= 0)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.TradePrice, LocalizedStrings.TradePrice);
			}

			metaInfo.LastTransactionId = writer.SerializeId(msg.TransactionId, metaInfo.LastTransactionId);
			metaInfo.LastOriginalTransactionId = writer.SerializeId(msg.OriginalTransactionId, metaInfo.LastOriginalTransactionId);

			writer.Write(msg.HasOrderInfo);
			writer.Write(msg.HasTradeInfo);

			writer.Write(msg.OrderId != null);

			if (msg.OrderId != null)
			{
				metaInfo.LastOrderId = writer.SerializeId(msg.OrderId.Value, metaInfo.LastOrderId);
			}
			else
			{
				writer.WriteStringEx(msg.OrderStringId);
			}

			writer.WriteStringEx(msg.OrderBoardId);

			writer.Write(msg.TradeId != null);

			if (msg.TradeId != null)
			{
				metaInfo.LastTradeId = writer.SerializeId(msg.TradeId.Value, metaInfo.LastTradeId);
			}
			else
			{
				writer.WriteStringEx(msg.TradeStringId);
			}

			if (msg.OrderPrice != 0)
			{
				writer.Write(true);
				writer.WritePriceEx(msg.OrderPrice, metaInfo, SecurityId, useLong, largeDecimal);
			}
			else
				writer.Write(false);

			if (msg.TradePrice != null)
			{
				writer.Write(true);
				writer.WritePriceEx(msg.TradePrice.Value, metaInfo, SecurityId, useLong, largeDecimal);
			}
			else
				writer.Write(false);

			writer.Write(msg.Side == Sides.Buy);

			writer.Write(msg.OrderVolume != null);

			if (msg.OrderVolume != null)
				writer.WriteVolume(msg.OrderVolume.Value, metaInfo, largeDecimal);

			writer.Write(msg.TradeVolume != null);

			if (msg.TradeVolume != null)
				writer.WriteVolume(msg.TradeVolume.Value, metaInfo, largeDecimal);

			writer.Write(msg.VisibleVolume != null);

			if (msg.VisibleVolume != null)
				writer.WriteVolume(msg.VisibleVolume.Value, metaInfo, largeDecimal);

			writer.Write(msg.Balance != null);

			if (msg.Balance != null)
				writer.WriteVolume(msg.Balance.Value, metaInfo, largeDecimal);

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Matching, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			writer.WriteNullableInt((int?)msg.OrderType);
			writer.WriteNullableInt((int?)msg.OrderState);
			writer.WriteNullableLong(msg.OrderStatus);
			writer.WriteNullableInt((int?)msg.TradeStatus);
			writer.WriteNullableInt((int?)msg.TimeInForce);

			writer.Write(msg.IsSystem != null);

			if (msg.IsSystem != null)
				writer.Write(msg.IsSystem.Value);

			writer.Write(msg.IsUpTick != null);

			if (msg.IsUpTick != null)
				writer.Write(msg.IsUpTick.Value);

			writer.WriteDto(msg.ExpiryDate);

			metaInfo.LastCommission = Write(writer, msg.Commission, metaInfo.LastCommission);
			metaInfo.LastPnL = Write(writer, msg.PnL, metaInfo.LastPnL);
			metaInfo.LastPosition = Write(writer, msg.Position, metaInfo.LastPosition);
			metaInfo.LastSlippage = Write(writer, msg.Slippage, metaInfo.LastSlippage);

			WriteString(writer, metaInfo.Portfolios, msg.PortfolioName);
			WriteString(writer, metaInfo.ClientCodes, msg.ClientCode);
			WriteString(writer, metaInfo.BrokerCodes, msg.BrokerCode);
			WriteString(writer, metaInfo.DepoNames, msg.DepoName);
			WriteString(writer, metaInfo.UserOrderIds, msg.UserOrderId);
			WriteString(writer, metaInfo.Comments, msg.Comment);
			WriteString(writer, metaInfo.SystemComments, msg.SystemComment);
			WriteString(writer, metaInfo.Errors, msg.Error?.Message);

			writer.WriteNullableInt((int?)msg.Currency);

			writer.Write(msg.Latency != null);

			if (msg.Latency != null)
				writer.WriteLong(msg.Latency.Value.Ticks);

			writer.WriteNullableSide(msg.OriginSide);

			if (metaInfo.Version < MarketDataVersions.Version59)
				continue;

			WriteItemLocalTime(writer, metaInfo, msg, isTickPrecision);

			if (metaInfo.Version < MarketDataVersions.Version61)
				continue;

			writer.Write(msg.IsMarketMaker != null);

			if (msg.IsMarketMaker != null)
				writer.Write(msg.IsMarketMaker.Value);

			if (metaInfo.Version < MarketDataVersions.Version62)
				continue;

			writer.Write(msg.MarginMode != null);

			if (msg.MarginMode != null)
				writer.Write(msg.MarginMode == MarginModes.Cross);

			if (metaInfo.Version < MarketDataVersions.Version63)
				continue;

			writer.WriteStringEx(msg.CommissionCurrency);

			if (metaInfo.Version < MarketDataVersions.Version64)
				continue;

			writer.Write(msg.IsManual != null);

			if (msg.IsManual != null)
				writer.Write(msg.IsManual.Value);

			if (metaInfo.Version < MarketDataVersions.Version65)
				continue;

			writer.Write(msg.PositionEffect != null);

			if (msg.PositionEffect != null)
				writer.WriteInt((int)msg.PositionEffect.Value);

			writer.Write(msg.PostOnly != null);

			if (msg.PostOnly != null)
				writer.Write(msg.PostOnly.Value);

			writer.Write(msg.Initiator != null);

			if (msg.Initiator != null)
				writer.Write(msg.Initiator.Value);

			if (metaInfo.Version < MarketDataVersions.Version66)
				continue;

			writer.WriteLong(msg.SeqNum);
			WriteString(writer, metaInfo.StrategyIds, msg.StrategyId);

			if (!buildFrom)
				continue;

			writer.WriteBuildFrom(msg.BuildFrom);

			if (!leverage)
				continue;

			writer.WriteNullableInt(msg.Leverage);

			if (metaInfo.Version < MarketDataVersions.Version70)
				continue;

			writer.WriteNullableDecimal(msg.MinVolume);
		}
	}

	public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var useLong = metaInfo.Version >= MarketDataVersions.Version69;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version69;

		metaInfo.FirstTransactionId += reader.ReadLong();
		metaInfo.FirstOriginalTransactionId += reader.ReadLong();

		var hasOrderInfo = reader.Read();
		/*var hasTradeInfo = */reader.Read();

		long? orderId = null;
		long? tradeId = null;

		string orderStringId = null;
		string tradeStringId = null;

		if (reader.Read())
		{
			metaInfo.FirstOrderId += reader.ReadLong();
			orderId = metaInfo.FirstOrderId;
		}
		else
		{
			orderStringId = reader.ReadStringEx();
		}

		var orderBoardId = reader.ReadStringEx();

		if (reader.Read())
		{
			metaInfo.FirstTradeId += reader.ReadLong();
			tradeId = metaInfo.FirstTradeId;
		}
		else
		{
			tradeStringId = reader.ReadStringEx();
		}

		var orderPrice = reader.Read() ? reader.ReadPriceEx(metaInfo, useLong, largeDecimal) : (decimal?)null;
		var tradePrice = reader.Read() ? reader.ReadPriceEx(metaInfo, useLong, largeDecimal) : (decimal?)null;

		var side = reader.Read() ? Sides.Buy : Sides.Sell;

		var orderVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : (decimal?)null;
		var tradeVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : (decimal?)null;
		var visibleVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : (decimal?)null;
		var balance = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : (decimal?)null;

		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version60;

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		var serverTime = reader.ReadTime(ref prevTime, true, true, metaInfo.GetTimeZone(true, SecurityId, ExchangeInfoProvider), true, isTickPrecision, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		var type = (OrderTypes?)reader.ReadNullableInt();
		var state = (OrderStates?)reader.ReadNullableInt();
		var status = reader.ReadNullableLong();
		var tradeStatus = reader.ReadNullableInt();
		var timeInForce = (TimeInForce?)reader.ReadNullableInt();

		var isSystem = reader.Read() ? reader.Read() : (bool?)null;
		var isUpTick = reader.Read() ? reader.Read() : (bool?)null;

		var expDate = reader.ReadDto();

		var commission = reader.Read() ? metaInfo.FirstCommission = reader.ReadDecimal(metaInfo.FirstCommission) : (decimal?)null;
		var pnl = reader.Read() ? metaInfo.FirstPnL = reader.ReadDecimal(metaInfo.FirstPnL) : (decimal?)null;
		var position = reader.Read() ? metaInfo.FirstPosition = reader.ReadDecimal(metaInfo.FirstPosition) : (decimal?)null;
		var slippage = reader.Read() ? metaInfo.FirstSlippage = reader.ReadDecimal(metaInfo.FirstSlippage) : (decimal?)null;

		var portfolio = ReadString(reader, metaInfo.Portfolios);
		var clientCode = ReadString(reader, metaInfo.ClientCodes);
		var brokerCode = ReadString(reader, metaInfo.BrokerCodes);
		var depoName = ReadString(reader, metaInfo.DepoNames);
		var userOrderId = ReadString(reader, metaInfo.UserOrderIds);
		var comment = ReadString(reader, metaInfo.Comments);
		var sysComment = ReadString(reader, metaInfo.SystemComments);

		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = SecurityId,

			ServerTime = serverTime,

			TransactionId = metaInfo.FirstTransactionId,
			OriginalTransactionId = metaInfo.FirstOriginalTransactionId,

			Side = side,
			OrderVolume = orderVolume,
			TradeVolume = tradeVolume,
			VisibleVolume = visibleVolume,
			Balance = balance,

			OrderType = type,
			OrderState = state,
			OrderStatus = status,
			TimeInForce = timeInForce,
			IsSystem = isSystem,
			IsUpTick = isUpTick,
			ExpiryDate = expDate,
			Commission = commission,
			PnL = pnl,
			Position = position,
			Slippage = slippage,
			PortfolioName = portfolio,
			ClientCode = clientCode,
			BrokerCode = brokerCode,
			DepoName = depoName,
			UserOrderId = userOrderId,
			Comment = comment,
			SystemComment = sysComment,

			TradeStatus = tradeStatus,

			HasOrderInfo = hasOrderInfo,

			OrderPrice = orderPrice ?? 0,
			TradePrice = tradePrice,

			OrderId = orderId,
			TradeId = tradeId,

			OrderBoardId = orderBoardId,
			OrderStringId = orderStringId,
			TradeStringId = tradeStringId,
		};

		var error = ReadString(reader, metaInfo.Errors);

		if (!error.IsEmpty())
			msg.Error = new InvalidOperationException(error);

		msg.Currency = (CurrencyTypes?)reader.ReadNullableInt();

		if (reader.Read())
			msg.Latency = reader.ReadLong().To<TimeSpan>();

		msg.OriginSide = reader.ReadNullableSide();

		if (metaInfo.Version < MarketDataVersions.Version59)
			return msg;

		msg.LocalTime = ReadItemLocalTime(reader, metaInfo, isTickPrecision);

		if (metaInfo.Version < MarketDataVersions.Version61)
			return msg;

		if (reader.Read())
			msg.IsMarketMaker = reader.Read();

		if (metaInfo.Version < MarketDataVersions.Version62)
			return msg;

		if (reader.Read())
			msg.MarginMode = reader.Read() ? MarginModes.Cross : MarginModes.Isolated;

		if (metaInfo.Version < MarketDataVersions.Version63)
			return msg;
			
		msg.CommissionCurrency = reader.ReadStringEx();

		if (metaInfo.Version < MarketDataVersions.Version64)
			return msg;

		if (reader.Read())
			msg.IsManual = reader.Read();

		if (metaInfo.Version < MarketDataVersions.Version65)
			return msg;

		if (reader.Read())
			msg.PositionEffect = (OrderPositionEffects)reader.ReadInt();

		if (reader.Read())
			msg.PostOnly = reader.Read();

		if (reader.Read())
			msg.Initiator = reader.Read();

		if (metaInfo.Version < MarketDataVersions.Version66)
			return msg;

		msg.SeqNum = reader.ReadLong();
		msg.StrategyId = ReadString(reader, metaInfo.StrategyIds);

		if (metaInfo.Version < MarketDataVersions.Version67)
			return msg;

		msg.BuildFrom = reader.ReadBuildFrom();

		if (metaInfo.Version < MarketDataVersions.Version68)
			return msg;

		msg.Leverage = reader.ReadNullableInt();

		if (metaInfo.Version < MarketDataVersions.Version70)
			return msg;

		msg.MinVolume = reader.ReadNullableDecimal();

		return msg;
	}

	private static decimal Write(BitArrayWriter writer, decimal? value, decimal last)
	{
		if (value == null)
		{
			writer.Write(false);
			return last;
		}
		else
		{
			writer.Write(true);
			writer.WriteDecimal((decimal)value, last);

			return value.Value;
		}
	}

	private static void WriteString(BitArrayWriter writer, IList<string> items, string value)
	{
		if (value.IsEmpty())
			writer.Write(false);
		else
		{
			writer.Write(true);

			items.TryAdd(value);
			writer.WriteInt(items.IndexOf(value));
		}
	}

	private static string ReadString(BitArrayReader reader, IList<string> items)
	{
		if (!reader.Read())
			return null;

		return items[reader.ReadInt()];
	}
}