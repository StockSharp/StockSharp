namespace StockSharp.Algo.Export;

using System.Xml;

/// <summary>
/// The export into xml.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XmlExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="isCancelled">The processor, returning process interruption sign.</param>
/// <param name="fileName">The path to file.</param>
public class XmlExporter(DataType dataType, Func<int, bool> isCancelled, string fileName) : BaseExporter(dataType, isCancelled, fileName)
{
	private const string _timeFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

	/// <summary>
	/// Gets or sets a value indicating whether to indent elements.
	/// </summary>
	/// <remarks>
	/// By default is <see langword="true"/>.
	/// </remarks>
	public bool Indent { get; set; } = true;

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, "orderLog", (writer, item) =>
		{
			writer.WriteStartElement("item");

			writer
				.WriteAttribute("id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>())
				.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat))
				.WriteAttribute("price", item.OrderPrice)
				.WriteAttribute("volume", item.OrderVolume)
				.WriteAttribute("side", item.Side)
				.WriteAttribute("state", item.OrderState)
				.WriteAttribute("timeInForce", item.TimeInForce)
				.WriteAttribute("isSystem", item.IsSystem);

			if (item.SeqNum != default)
				writer.WriteAttribute("seqNum", item.SeqNum);

			if (item.TradePrice != null)
			{
				writer
					.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>())
					.WriteAttribute("tradePrice", item.TradePrice);

				if (item.OpenInterest != null)
					writer.WriteAttribute("openInterest", item.OpenInterest.Value);
			}

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, "ticks", (writer, trade) =>
		{
			writer.WriteStartElement("trade");

			writer
				.WriteAttribute("id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>())
				.WriteAttribute("serverTime", trade.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", trade.LocalTime.ToString(_timeFormat))
				.WriteAttribute("price", trade.TradePrice)
				.WriteAttribute("volume", trade.TradeVolume);

			if (trade.OriginSide != null)
				writer.WriteAttribute("originSide", trade.OriginSide.Value);

			if (trade.OpenInterest != null)
				writer.WriteAttribute("openInterest", trade.OpenInterest.Value);

			if (trade.IsUpTick != null)
				writer.WriteAttribute("isUpTick", trade.IsUpTick.Value);

			if (trade.Currency != null)
				writer.WriteAttribute("currency", trade.Currency.Value);

			if (trade.SeqNum != default)
				writer.WriteAttribute("seqNum", trade.SeqNum);

			if (trade.Yield != default)
				writer.WriteAttribute("yield", trade.Yield);

			if (trade.OrderBuyId != default)
				writer.WriteAttribute("buy", trade.OrderBuyId);

			if (trade.OrderSellId != default)
				writer.WriteAttribute("sell", trade.OrderSellId);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, "transactions", (writer, item) =>
		{
			writer.WriteStartElement("item");

			writer
				.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat))
				.WriteAttribute("portfolio", item.PortfolioName)
				.WriteAttribute("clientCode", item.ClientCode)
				.WriteAttribute("brokerCode", item.BrokerCode)
				.WriteAttribute("depoName", item.DepoName)
				.WriteAttribute("transactionId", item.TransactionId)
				.WriteAttribute("originalTransactionId", item.OriginalTransactionId)
				.WriteAttribute("orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>())
				.WriteAttribute("orderPrice", item.OrderPrice)
				.WriteAttribute("orderVolume", item.OrderVolume)
				.WriteAttribute("orderType", item.OrderType)
				.WriteAttribute("orderState", item.OrderState)
				.WriteAttribute("orderStatus", item.OrderStatus)
				.WriteAttribute("visibleVolume", item.VisibleVolume)
				.WriteAttribute("balance", item.Balance)
				.WriteAttribute("side", item.Side)
				.WriteAttribute("originSide", item.OriginSide)
				.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>())
				.WriteAttribute("tradePrice", item.TradePrice)
				.WriteAttribute("tradeVolume", item.TradeVolume)
				.WriteAttribute("tradeStatus", item.TradeStatus)
				.WriteAttribute("isOrder", item.HasOrderInfo)
				.WriteAttribute("commission", item.Commission)
				.WriteAttribute("commissionCurrency", item.CommissionCurrency)
				.WriteAttribute("pnl", item.PnL)
				.WriteAttribute("position", item.Position)
				.WriteAttribute("latency", item.Latency)
				.WriteAttribute("slippage", item.Slippage)
				.WriteAttribute("error", item.Error?.Message)
				.WriteAttribute("openInterest", item.OpenInterest)
				.WriteAttribute("isCancelled", item.IsCancellation)
				.WriteAttribute("isSystem", item.IsSystem)
				.WriteAttribute("isUpTick", item.IsUpTick)
				.WriteAttribute("userOrderId", item.UserOrderId)
				.WriteAttribute("strategyId", item.StrategyId)
				.WriteAttribute("currency", item.Currency)
				.WriteAttribute("marginMode", item.MarginMode)
				.WriteAttribute("isMarketMaker", item.IsMarketMaker)
				.WriteAttribute("isManual", item.IsManual)
				.WriteAttribute("averagePrice", item.AveragePrice)
				.WriteAttribute("yield", item.Yield)
				.WriteAttribute("minVolume", item.MinVolume)
				.WriteAttribute("positionEffect", item.PositionEffect)
				.WriteAttribute("postOnly", item.PostOnly)
				.WriteAttribute("initiator", item.Initiator)
				.WriteAttribute("seqNum", item.SeqNum)
				.WriteAttribute("leverage", item.Leverage);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
	{
		return Do(messages, "depths", (writer, depth) =>
		{
			writer.WriteStartElement("depth");

			writer
				.WriteAttribute("serverTime", depth.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", depth.LocalTime.ToString(_timeFormat));

			if (depth.State != null)
				writer.WriteAttribute("state", depth.State.Value);

			if (depth.HasPositions)
				writer.WriteAttribute("pos", true);

			if (depth.SeqNum != default)
				writer.WriteAttribute("seqNum", depth.SeqNum);

			var bids = new HashSet<QuoteChange>(depth.Bids);

			foreach (var quote in depth.Bids.Concat(depth.Asks).OrderByDescending(q => q.Price))
			{
				writer.WriteStartElement("quote");

				writer
					.WriteAttribute("price", quote.Price)
					.WriteAttribute("volume", quote.Volume)
					.WriteAttribute("side", bids.Contains(quote) ? Sides.Buy : Sides.Sell);

				if (quote.OrdersCount != default)
					writer.WriteAttribute("ordersCount", quote.OrdersCount.Value);

				if (quote.StartPosition != default)
					writer.WriteAttribute("startPos", quote.StartPosition.Value);

				if (quote.EndPosition != default)
					writer.WriteAttribute("endPos", quote.EndPosition.Value);

				if (quote.Action != default)
					writer.WriteAttribute("action", quote.Action.Value);

				if (quote.Condition != default)
					writer.WriteAttribute("condition", quote.Condition);

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
	{
		return Do(messages, "level1", (writer, message) =>
		{
			writer.WriteStartElement("change");

			writer
				.WriteAttribute("serverTime", message.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", message.LocalTime.ToString(_timeFormat));

			if (message.SeqNum != default)
				writer.WriteAttribute("seqNum", message.SeqNum);

			foreach (var pair in message.Changes)
				writer.WriteAttribute(pair.Key.ToString(), (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
	{
		return Do(messages, "positions", (writer, message) =>
		{
			writer.WriteStartElement("change");

			writer
				.WriteAttribute("serverTime", message.ServerTime.ToString(_timeFormat))
				.WriteAttribute("localTime", message.LocalTime.ToString(_timeFormat))

				.WriteAttribute("portfolio", message.PortfolioName)
				.WriteAttribute("clientCode", message.ClientCode)
				.WriteAttribute("depoName", message.DepoName)
				.WriteAttribute("limit", message.LimitType)
				.WriteAttribute("strategyId", message.StrategyId)
				.WriteAttribute("side", message.Side)
				;

			foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
				writer.WriteAttribute(pair.Key.ToString(), (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
	{
		return Do(values, "values", (writer, value) =>
		{
			writer.WriteStartElement("value");

			writer.WriteAttribute("time", value.Time.ToString(_timeFormat));

			var index = 1;
			foreach (var indVal in value.ValuesAsDecimal)
				writer.WriteAttribute($"value{index++}", indVal);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
	{
		return Do(messages, "candles", (writer, candle) =>
		{
			writer.WriteStartElement("candle");

			writer
				.WriteAttribute("openTime", candle.OpenTime.ToString(_timeFormat))
				.WriteAttribute("closeTime", candle.CloseTime.ToString(_timeFormat))

				.WriteAttribute("O", candle.OpenPrice)
				.WriteAttribute("H", candle.HighPrice)
				.WriteAttribute("L", candle.LowPrice)
				.WriteAttribute("C", candle.ClosePrice)
				.WriteAttribute("V", candle.TotalVolume);

			if (candle.OpenInterest != null)
				writer.WriteAttribute("openInterest", candle.OpenInterest.Value);

			if (candle.SeqNum != default)
				writer.WriteAttribute("seqNum", candle.SeqNum);

			if (candle.PriceLevels != null)
			{
				writer.WriteStartElement("levels");

				foreach (var level in candle.PriceLevels)
				{
					writer.WriteStartElement("level");

					writer
						.WriteAttribute("price", level.Price)
						.WriteAttribute("buyCount", level.BuyCount)
						.WriteAttribute("sellCount", level.SellCount)
						.WriteAttribute("buyVolume", level.BuyVolume)
						.WriteAttribute("sellVolume", level.SellVolume)
						.WriteAttribute("volume", level.TotalVolume);

					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
	{
		return Do(messages, "news", (writer, n) =>
		{
			writer.WriteStartElement("item");

			if (!n.Id.IsEmpty())
				writer.WriteAttribute("id", n.Id);

			writer.WriteAttribute("serverTime", n.ServerTime.ToString(_timeFormat));
			writer.WriteAttribute("localTime", n.LocalTime.ToString(_timeFormat));

			if (n.SecurityId != null)
				writer.WriteAttribute("securityCode", n.SecurityId.Value.SecurityCode);

			if (!n.BoardCode.IsEmpty())
				writer.WriteAttribute("boardCode", n.BoardCode);

			writer.WriteAttribute("headline", n.Headline);

			if (!n.Source.IsEmpty())
				writer.WriteAttribute("source", n.Source);

			if (!n.Url.IsEmpty())
				writer.WriteAttribute("url", n.Url);

			if (n.Priority != null)
				writer.WriteAttribute("priority", n.Priority.Value);

			if (!n.Language.IsEmpty())
				writer.WriteAttribute("language", n.Language);

			if (n.ExpiryDate != null)
				writer.WriteAttribute("expiry", n.ExpiryDate.Value);

			if (!n.Story.IsEmpty())
				writer.WriteCData(n.Story);

			if (n.SeqNum != default)
				writer.WriteAttribute("seqNum", n.SeqNum);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
	{
		return Do(messages, "securities", (writer, security) =>
		{
			writer.WriteStartElement("security");

			writer.WriteAttribute("code", security.SecurityId.SecurityCode);
			writer.WriteAttribute("board", security.SecurityId.BoardCode);

			if (!security.Name.IsEmpty())
				writer.WriteAttribute("name", security.Name);

			if (!security.ShortName.IsEmpty())
				writer.WriteAttribute("shortName", security.ShortName);

			if (security.PriceStep != null)
				writer.WriteAttribute("priceStep", security.PriceStep.Value);

			if (security.VolumeStep != null)
				writer.WriteAttribute("volumeStep", security.VolumeStep.Value);

			if (security.MinVolume != null)
				writer.WriteAttribute("minVolume", security.MinVolume.Value);

			if (security.MaxVolume != null)
				writer.WriteAttribute("maxVolume", security.MaxVolume.Value);

			if (security.Multiplier != null)
				writer.WriteAttribute("multiplier", security.Multiplier.Value);

			if (security.Decimals != null)
				writer.WriteAttribute("decimals", security.Decimals.Value);

			if (security.Currency != null)
				writer.WriteAttribute("currency", security.Currency.Value);

			if (security.SecurityType != null)
				writer.WriteAttribute("type", security.SecurityType.Value);
			
			if (!security.CfiCode.IsEmpty())
				writer.WriteAttribute("cfiCode", security.CfiCode);
			
			if (security.Shortable != null)
				writer.WriteAttribute("shortable", security.Shortable.Value);

			if (security.OptionType != null)
				writer.WriteAttribute("optionType", security.OptionType.Value);

			if (security.Strike != null)
				writer.WriteAttribute("strike", security.Strike.Value);

			if (!security.BinaryOptionType.IsEmpty())
				writer.WriteAttribute("binaryOptionType", security.BinaryOptionType);

			if (security.IssueSize != null)
				writer.WriteAttribute("issueSize", security.IssueSize.Value);

			if (security.IssueDate != null)
				writer.WriteAttribute("issueDate", security.IssueDate.Value);

			if (!security.GetUnderlyingCode().IsEmpty())
				writer.WriteAttribute("underlyingId", security.GetUnderlyingCode());

			if (security.UnderlyingSecurityType != null)
				writer.WriteAttribute("underlyingType", security.UnderlyingSecurityType);

			if (security.UnderlyingSecurityMinVolume != null)
				writer.WriteAttribute("underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value);

			if (security.ExpiryDate != null)
				writer.WriteAttribute("expiryDate", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

			if (security.SettlementDate != null)
				writer.WriteAttribute("settlementDate", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

			if (!security.BasketCode.IsEmpty())
				writer.WriteAttribute("basketCode", security.BasketCode);

			if (!security.BasketExpression.IsEmpty())
				writer.WriteAttribute("basketExpression", security.BasketExpression);

			if (security.FaceValue != null)
				writer.WriteAttribute("faceValue", security.FaceValue.Value);

			if (security.SettlementType != null)
				writer.WriteAttribute("settlementType", security.SettlementType.Value);

			if (security.OptionStyle != null)
				writer.WriteAttribute("optionStyle", security.OptionStyle.Value);

			if (!security.PrimaryId.SecurityCode.IsEmpty())
				writer.WriteAttribute("primaryCode", security.PrimaryId.SecurityCode);

			if (!security.PrimaryId.BoardCode.IsEmpty())
				writer.WriteAttribute("primaryBoard", security.PrimaryId.BoardCode);

			if (!security.SecurityId.Bloomberg.IsEmpty())
				writer.WriteAttribute("bloomberg", security.SecurityId.Bloomberg);

			if (!security.SecurityId.Cusip.IsEmpty())
				writer.WriteAttribute("cusip", security.SecurityId.Cusip);

			if (!security.SecurityId.IQFeed.IsEmpty())
				writer.WriteAttribute("iqfeed", security.SecurityId.IQFeed);

			if (security.SecurityId.InteractiveBrokers != null)
				writer.WriteAttribute("ib", security.SecurityId.InteractiveBrokers);

			if (!security.SecurityId.Isin.IsEmpty())
				writer.WriteAttribute("isin", security.SecurityId.Isin);

			if (!security.SecurityId.Plaza.IsEmpty())
				writer.WriteAttribute("plaza", security.SecurityId.Plaza);

			if (!security.SecurityId.Ric.IsEmpty())
				writer.WriteAttribute("ric", security.SecurityId.Ric);

			if (!security.SecurityId.Sedol.IsEmpty())
				writer.WriteAttribute("sedol", security.SecurityId.Sedol);

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
	{
		return Do(messages, "boardStates", (writer, msg) =>
		{
			writer.WriteStartElement("boardState");

			writer.WriteAttribute("serverTime", msg.ServerTime.ToString(_timeFormat));
			writer.WriteAttribute("boardCode", msg.BoardCode);
			writer.WriteAttribute("state", msg.State.ToString());

			writer.WriteEndElement();
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
	{
		return Do(messages, "boards", (writer, msg) =>
		{
			writer.WriteStartElement("board");

			writer.WriteAttribute("code", msg.Code);
			writer.WriteAttribute("exchangeCode", msg.ExchangeCode);
			writer.WriteAttribute("expiryTime", msg.ExpiryTime.ToString());
			writer.WriteAttribute("timeZone", msg.TimeZone?.Id);

			writer.WriteEndElement();
		});
	}

	private (int, DateTimeOffset?) Do<TValue>(IEnumerable<TValue> values, string rootElem, Action<XmlWriter, TValue> action)
	{
		var count = 0;
		var lastTime = default(DateTimeOffset?);
		
		using (var writer = XmlWriter.Create(Path, new XmlWriterSettings { Indent = Indent }))
		{
			writer.WriteStartElement(rootElem);

			foreach (var value in values)
			{
				if (!CanProcess())
					break;

				action(writer, value);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			writer.WriteEndElement();
		}

		return (count, lastTime);
	}
}