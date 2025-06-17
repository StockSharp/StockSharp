namespace StockSharp.Algo.Storages.Binary;

class OrderLogMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override object LastId => LastTransactionId;

	public long FirstOrderId { get; set; } = -1;
	public long LastOrderId { get; set; }

	public long FirstTradeId { get; set; }
	public long LastTradeId { get; set; }

	public long FirstTransactionId { get; set; }
	public long LastTransactionId { get; set; }

	public decimal FirstOrderPrice { get; set; }
	public decimal LastOrderPrice { get; set; }

	public IList<string> Portfolios { get; } = [];

	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(FirstOrderId);
		stream.WriteEx(FirstTradeId);
		stream.WriteEx(LastOrderId);
		stream.WriteEx(LastTradeId);
		stream.WriteEx(FirstPrice);
		stream.WriteEx(LastPrice);

		if (Version < MarketDataVersions.Version34)
			return;

		stream.WriteEx(FirstTransactionId);
		stream.WriteEx(LastTransactionId);

		if (Version < MarketDataVersions.Version40)
			return;

		stream.WriteEx(Portfolios.Count);

		foreach (var portfolio in Portfolios)
			stream.WriteEx(portfolio);

		WriteFractionalPrice(stream);
		WriteFractionalVolume(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		stream.WriteEx(FirstOrderPrice);
		stream.WriteEx(LastOrderPrice);

		WriteLocalTime(stream, MarketDataVersions.Version46);

		if (Version < MarketDataVersions.Version48)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version52)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version56)
			return;

		WriteSeqNums(stream);
	}

	public override void Read(Stream stream)
	{
		base.Read(stream);

		FirstOrderId = stream.Read<long>();
		FirstTradeId = stream.Read<long>();
		LastOrderId = stream.Read<long>();
		LastTradeId = stream.Read<long>();
		FirstPrice = stream.Read<decimal>();
		LastPrice = stream.Read<decimal>();

		if (Version < MarketDataVersions.Version34)
			return;

		FirstTransactionId = stream.Read<long>();
		LastTransactionId = stream.Read<long>();

		if (Version < MarketDataVersions.Version40)
			return;

		var count = stream.Read<int>();

		for (var i = 0; i < count; i++)
			Portfolios.Add(stream.Read<string>());

		ReadFractionalPrice(stream);
		ReadFractionalVolume(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		FirstOrderPrice = stream.Read<decimal>();
		LastOrderPrice = stream.Read<decimal>();

		ReadLocalTime(stream, MarketDataVersions.Version46);

		if (Version < MarketDataVersions.Version48)
			return;

		ServerOffset = stream.Read<TimeSpan>();

		if (Version < MarketDataVersions.Version52)
			return;

		ReadOffsets(stream);

		if (Version < MarketDataVersions.Version56)
			return;

		ReadSeqNums(stream);
	}

	public override void CopyFrom(BinaryMetaInfo src)
	{
		base.CopyFrom(src);

		var olInfo = (OrderLogMetaInfo)src;

		FirstOrderId = olInfo.FirstOrderId;
		FirstTradeId = olInfo.FirstTradeId;
		LastOrderId = olInfo.LastOrderId;
		LastTradeId = olInfo.LastTradeId;

		FirstTransactionId = olInfo.FirstTransactionId;
		LastTransactionId = olInfo.LastTransactionId;

		FirstOrderPrice = olInfo.FirstOrderPrice;
		LastOrderPrice = olInfo.LastOrderPrice;

		Portfolios.Clear();
		Portfolios.AddRange(olInfo.Portfolios);
	}
}

class OrderLogBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<ExecutionMessage, OrderLogMetaInfo>(securityId, DataType.OrderLog, 200, MarketDataVersions.Version59, exchangeInfoProvider)
{
	protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, OrderLogMetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty() && !messages.IsEmpty())
		{
			var item = messages.First();

			metaInfo.FirstOrderId = metaInfo.LastOrderId = item.OrderId ?? default;
			metaInfo.FirstTransactionId = metaInfo.LastTransactionId = item.TransactionId;
			metaInfo.ServerOffset = item.ServerTime.Offset;
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = item.SeqNum;
		}

		writer.WriteInt(messages.Count());

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;
		var useBalance = metaInfo.Version >= MarketDataVersions.Version54;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version55;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version56;
		var useLong = metaInfo.Version >= MarketDataVersions.Version57;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version57;
		var stringId = metaInfo.Version >= MarketDataVersions.Version58;
		var splitVol = metaInfo.Version >= MarketDataVersions.Version59;

		foreach (var message in messages)
		{
			var hasTrade = message.TradeId != null || message.TradePrice != null || !message.TradeStringId.IsEmpty();
			var orderId = message.OrderId;

			if (orderId is null)
			{
				if (!stringId)
					throw new ArgumentOutOfRangeException(nameof(messages), message.TransactionId, LocalizedStrings.TransactionId);
			}

			if (message.DataType != DataType.OrderLog)
				throw new ArgumentOutOfRangeException(nameof(messages), message.DataType, LocalizedStrings.UnknownType.Put(message));

			// sell market orders has zero price (if security do not have min allowed price)
			// execution ticks (like option execution) may be a zero cost
			// ticks for spreads may be a zero cost or less than zero
			//if (item.Price < 0)
			//	throw new ArgumentOutOfRangeException();

			long? tradeId = null;

			if (hasTrade)
			{
				tradeId = message.TradeId;

				if (tradeId is null or <= 0)
				{
					if (!stringId)
						throw new ArgumentOutOfRangeException(nameof(messages), tradeId, LocalizedStrings.TradeId);
				}

				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (item.TradePrice <= 0)
				//	throw new ArgumentOutOfRangeException();
			}

			metaInfo.LastOrderId = writer.SerializeId(orderId ?? 0, metaInfo.LastOrderId);

			var orderPrice = message.OrderPrice;

			if (metaInfo.Version < MarketDataVersions.Version45)
				writer.WritePriceEx(orderPrice, metaInfo, SecurityId, false, false);
			else
			{
				var isAligned = (orderPrice % metaInfo.LastPriceStep) == 0;
				writer.Write(isAligned);

				if (isAligned)
				{
					if (metaInfo.FirstOrderPrice == 0)
						metaInfo.FirstOrderPrice = metaInfo.LastOrderPrice = orderPrice;

					var prevPrice = metaInfo.LastOrderPrice;
					writer.WritePrice(orderPrice, ref prevPrice, metaInfo, SecurityId, true);
					metaInfo.LastOrderPrice = prevPrice;
				}
				else
				{
					if (!metaInfo.IsFirstFractionalPriceSet)
						metaInfo.FirstFractionalPrice = metaInfo.LastFractionalPrice = orderPrice;

					metaInfo.LastFractionalPrice = writer.WriteDecimal(orderPrice, metaInfo.LastFractionalPrice);
				}
			}

			if (splitVol)
			{
				if (message.OrderVolume is decimal ov)
				{
					writer.Write(true);
					writer.WriteVolume(ov, metaInfo, largeDecimal);
				}
				else
					writer.Write(false);

				if (message.TradeVolume is decimal tv)
				{
					writer.Write(true);
					writer.WriteVolume(tv, metaInfo, largeDecimal);
				}
				else
					writer.Write(false);
			}
			else
			{
				var volume = message.SafeGetVolume();
				if (volume <= 0 && message.OrderState != OrderStates.Done)
					throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.WrongOrderVolume.Put(message.TransactionId));

				writer.WriteVolume(volume, metaInfo, largeDecimal);
			}
			
			writer.Write(message.Side == Sides.Buy);

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, LocalizedStrings.Orders, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			if (hasTrade)
			{
				writer.Write(true);

				if (metaInfo.FirstTradeId == 0)
				{
					metaInfo.FirstTradeId = metaInfo.LastTradeId = tradeId ?? default;
				}

				metaInfo.LastTradeId = writer.SerializeId(tradeId ?? default, metaInfo.LastTradeId);

				writer.WritePriceEx(message.GetTradePrice(), metaInfo, SecurityId, useLong, largeDecimal);

				if (metaInfo.Version >= MarketDataVersions.Version54)
					writer.WriteInt((int)message.OrderState);
			}
			else
			{
				writer.Write(false);

				if (metaInfo.Version >= MarketDataVersions.Version54)
					writer.WriteInt((int)message.OrderState);
				else
					writer.Write(message.OrderState == OrderStates.Active);
			}

			if (metaInfo.Version < MarketDataVersions.Version31)
				continue;

			writer.WriteNullableInt((int?)message.OrderStatus);

			if (metaInfo.Version < MarketDataVersions.Version33)
				continue;

			if (metaInfo.Version < MarketDataVersions.Version50)
				writer.WriteInt((int)(message.TimeInForce ?? TimeInForce.PutInQueue));
			else
			{
				writer.Write(message.TimeInForce != null);

				if (message.TimeInForce != null)
					writer.WriteInt((int)message.TimeInForce.Value);
			}

			if (metaInfo.Version >= MarketDataVersions.Version49)
			{
				writer.Write(message.IsSystem != null);

				if (message.IsSystem != null)
					writer.Write(message.IsSystem.Value);
			}
			else
				writer.Write(message.IsSystem ?? true);

			if (metaInfo.Version < MarketDataVersions.Version34)
				continue;

			metaInfo.LastTransactionId = writer.SerializeId(message.TransactionId, metaInfo.LastTransactionId);

			if (metaInfo.Version < MarketDataVersions.Version40)
				continue;

			if (metaInfo.Version < MarketDataVersions.Version46)
				writer.WriteLong(0/*item.Latency.Ticks*/);

			var portfolio = message.PortfolioName;
			var isEmptyPf = portfolio == null;
			var isAnonymous = !isEmptyPf && portfolio == Portfolio.AnonymousPortfolio.Name;

			if (isEmptyPf)
			{
				writer.Write(false);
			}
			else
			{
				if (isAnonymous)
				{
					if (metaInfo.Version < MarketDataVersions.Version54)
						writer.Write(false);
					else
					{
						writer.Write(true);
						writer.Write(true); // is anonymous
					}
				}
				else
				{
					writer.Write(true);

					if (metaInfo.Version > MarketDataVersions.Version54)
						writer.Write(false); // not anonymous

					metaInfo.Portfolios.TryAdd(message.PortfolioName);
					writer.WriteInt(metaInfo.Portfolios.IndexOf(message.PortfolioName));
				}
			}

			if (metaInfo.Version < MarketDataVersions.Version51)
				continue;

			writer.WriteNullableInt((int?)message.Currency);

			if (!useBalance)
				continue;

			if (message.Balance == null)
				writer.Write(false);
			else
			{
				writer.Write(true);

				if (message.Balance.Value == 0)
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteDecimal(message.Balance.Value, 0);
				}
			}

			if (!buildFrom)
				continue;

			writer.WriteBuildFrom(message.BuildFrom);

			if (!seqNum)
				continue;

			writer.WriteSeqNum(message, metaInfo);

			if (!stringId)
				continue;

			writer.Write(orderId is null);
			writer.WriteStringEx(message.OrderStringId);

			writer.Write(tradeId is null);
			writer.WriteStringEx(message.TradeStringId);

			if (message.OrderBuyId != null)
			{
				writer.Write(true);
				metaInfo.LastOrderId = writer.SerializeId(message.OrderBuyId.Value, metaInfo.LastOrderId);
			}
			else
				writer.Write(false);

			if (message.OrderSellId != null)
			{
				writer.Write(true);
				metaInfo.LastOrderId = writer.SerializeId(message.OrderSellId.Value, metaInfo.LastOrderId);
			}
			else
				writer.Write(false);

			writer.WriteNullableBool(message.IsUpTick);
			writer.WriteNullableDecimal(message.Yield);
			writer.WriteNullableInt((int?)message.TradeStatus);
			writer.WriteNullableDecimal(message.OpenInterest);
			writer.WriteNullableInt((int?)message.OriginSide);
		}
	}

	public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;
		var useBalance = metaInfo.Version >= MarketDataVersions.Version54;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version55;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version56;
		var useLong = metaInfo.Version >= MarketDataVersions.Version57;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version57;
		var stringId = metaInfo.Version >= MarketDataVersions.Version58;
		var splitVol = metaInfo.Version >= MarketDataVersions.Version59;

		metaInfo.FirstOrderId += reader.ReadLong();

		decimal price;

		if (metaInfo.Version < MarketDataVersions.Version45)
		{
			price = reader.ReadPriceEx(metaInfo, false, false);
		}
		else
		{
			if (reader.Read())
			{
				var prevPrice = metaInfo.FirstOrderPrice;
				price = reader.ReadPrice(ref prevPrice, metaInfo, true);
				metaInfo.FirstOrderPrice = prevPrice;
			}
			else
				price = metaInfo.FirstFractionalPrice = reader.ReadDecimal(metaInfo.FirstFractionalPrice);
		}

		decimal? orderVolume = null;
		decimal? tradeVolume = null;

		if (splitVol)
		{
			if (reader.Read())
				orderVolume = reader.ReadVolume(metaInfo, largeDecimal);

			if (reader.Read())
				tradeVolume = reader.ReadVolume(metaInfo, largeDecimal);
		}
		else
		{
			orderVolume = reader.ReadVolume(metaInfo, largeDecimal);
		}

		var orderDirection = reader.Read() ? Sides.Buy : Sides.Sell;

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		var execMsg = new ExecutionMessage
		{
			//LocalTime = metaInfo.FirstTime,
			DataTypeEx = DataType.OrderLog,
			SecurityId = SecurityId,
			OrderId = metaInfo.FirstOrderId,
			OrderVolume = orderVolume,
			TradeVolume = tradeVolume,
			Side = orderDirection,
			ServerTime = serverTime,
			OrderPrice = price,
		};

		if (reader.Read())
		{
			metaInfo.FirstTradeId += reader.ReadLong();
			price = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);

			execMsg.TradeId = metaInfo.FirstTradeId;
			execMsg.TradePrice = price;

			if (metaInfo.Version >= MarketDataVersions.Version54)
				execMsg.OrderState = (OrderStates)reader.ReadInt();
			else
				execMsg.OrderState = OrderStates.Done;
		}
		else
		{
			if (metaInfo.Version >= MarketDataVersions.Version54)
				execMsg.OrderState = (OrderStates)reader.ReadInt();
			else
				execMsg.OrderState = reader.Read() ? OrderStates.Active : OrderStates.Done;
		}

		if (metaInfo.Version >= MarketDataVersions.Version31)
		{
			execMsg.OrderStatus = reader.ReadNullableInt();

			if (execMsg.OrderStatus != null)
			{
				execMsg.TimeInForce = execMsg.OrderStatus.Value.GetPlazaTimeInForce();
			}

			// Лучше ExecCond писать отдельным полем так как возможно только Плаза пишет это в статус
			if (metaInfo.Version >= MarketDataVersions.Version33)
			{
				if (metaInfo.Version < MarketDataVersions.Version50)
					execMsg.TimeInForce = (TimeInForce)reader.ReadInt();
				else
					execMsg.TimeInForce = reader.Read() ? (TimeInForce)reader.ReadInt() : null;

				execMsg.IsSystem = metaInfo.Version < MarketDataVersions.Version49
					? reader.Read()
					: (reader.Read() ? reader.Read() : null);

				if (metaInfo.Version >= MarketDataVersions.Version34)
				{
					metaInfo.FirstTransactionId += reader.ReadLong();
					execMsg.TransactionId = metaInfo.FirstTransactionId;
				}
			}
			else
			{
				if (execMsg.OrderStatus != null)
					execMsg.IsSystem = execMsg.OrderStatus.Value.IsPlazaSystem();
			}
		}

		if (metaInfo.Version >= MarketDataVersions.Version40)
		{
			if (metaInfo.Version < MarketDataVersions.Version46)
				/*item.Latency =*/reader.ReadLong();//.To<TimeSpan>();

			if (reader.Read())
			{
				if (metaInfo.Version >= MarketDataVersions.Version54)
				{
					if (reader.Read())
						execMsg.PortfolioName = Portfolio.AnonymousPortfolio.Name;
					else
						execMsg.PortfolioName = metaInfo.Portfolios[reader.ReadInt()];
				}
				else
					execMsg.PortfolioName = metaInfo.Portfolios[reader.ReadInt()];
			}
		}

		//if (order.Portfolio == null)
		//	order.Portfolio = Portfolio.AnonymousPortfolio;

		if (metaInfo.Version >= MarketDataVersions.Version51)
		{
			if (reader.Read())
				execMsg.Currency = (CurrencyTypes)reader.ReadInt();
		}

		if (!useBalance)
			return execMsg;

		if (reader.Read())
			execMsg.Balance = reader.Read() ? reader.ReadDecimal(0) : 0M;

		if (!buildFrom)
			return execMsg;
		
		execMsg.BuildFrom = reader.ReadBuildFrom();

		if (!seqNum)
			return execMsg;

		reader.ReadSeqNum(execMsg, metaInfo);

		if (!stringId)
			return execMsg;

		if (reader.Read())
			execMsg.OrderId = null;

		execMsg.OrderStringId = reader.ReadStringEx();

		if (reader.Read())
			execMsg.TradeId = null;

		execMsg.TradeStringId = reader.ReadStringEx();

		if (reader.Read())
		{
			metaInfo.FirstOrderId += reader.ReadLong();
			execMsg.OrderBuyId = metaInfo.FirstOrderId;
		}

		if (reader.Read())
		{
			metaInfo.FirstOrderId += reader.ReadLong();
			execMsg.OrderSellId = metaInfo.FirstOrderId;
		}

		execMsg.IsUpTick = reader.ReadNullableBool();
		execMsg.Yield = reader.ReadNullableDecimal();
		execMsg.TradeStatus = reader.ReadNullableInt();
		execMsg.OpenInterest = reader.ReadNullableDecimal();
		execMsg.OriginSide = (Sides?)reader.ReadNullableInt();

		return execMsg;
	}
}