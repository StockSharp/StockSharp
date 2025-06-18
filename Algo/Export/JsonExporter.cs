namespace StockSharp.Algo.Export;

using Newtonsoft.Json;

/// <summary>
/// The export into json.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="isCancelled">The processor, returning process interruption sign.</param>
/// <param name="fileName">The path to file.</param>
public class JsonExporter(DataType dataType, Func<int, bool> isCancelled, string fileName) : BaseExporter(dataType, isCancelled, fileName)
{
	/// <summary>
	/// Gets or sets a value indicating whether to indent elements.
	/// </summary>
	/// <remarks>
	/// By default is <see langword="true"/>.
	/// </remarks>
	public bool Indent { get; set; } = true;

	private static JsonWriter WriteProperty(JsonWriter writer, string name, object value)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.WritePropertyName(name);
		writer.WriteValue(value);

		return writer;
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
	{
		return Do(messages, (writer, depth) =>
		{
			WriteProperty(writer, "s", depth.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", depth.LocalTime.UtcDateTime);

			if (depth.State != null)
				WriteProperty(writer, "st", depth.State.Value);

			if (depth.HasPositions)
				WriteProperty(writer, "pos", true);

			if (depth.SeqNum != default)
				WriteProperty(writer, "sn", depth.SeqNum);

			void WriteQuotes(string name, QuoteChange[] quotes)
			{
				writer.WritePropertyName(name);
				writer.WriteStartArray();

				foreach (var quote in quotes)
				{
					writer.WriteStartObject();

					WriteProperty(writer, "p", quote.Price);
					WriteProperty(writer, "v", quote.Volume);

					if (quote.OrdersCount != default)
						WriteProperty(writer, "cnt", quote.OrdersCount.Value);

					if (quote.StartPosition != default)
						WriteProperty(writer, "s", quote.StartPosition.Value);

					if (quote.EndPosition != default)
						WriteProperty(writer, "e", quote.EndPosition.Value);

					if (quote.Action != default)
						WriteProperty(writer, "a", quote.Action.Value);

					if (quote.Condition != default)
						WriteProperty(writer, "cond", quote.Condition);

					writer.WriteEndObject();
				}

				writer.WriteEndArray();
			}

			WriteQuotes("bids", depth.Bids);
			WriteQuotes("asks", depth.Asks);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<Level1ChangeMessage> messages)
	{
		return Do(messages, (writer, message) =>
		{
			WriteProperty(writer, "s", message.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", message.LocalTime.UtcDateTime);

			if (message.SeqNum != default)
				WriteProperty(writer, "sn", message.SeqNum);

			foreach (var pair in message.Changes)
				WriteProperty(writer, pair.Key.ToString(), pair.Value);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
	{
		return Do(messages, (writer, candle) =>
		{
			WriteProperty(writer, "open", candle.OpenTime.UtcDateTime);
			WriteProperty(writer, "close", candle.CloseTime.UtcDateTime);
			WriteProperty(writer, "O", candle.OpenPrice);
			WriteProperty(writer, "H", candle.HighPrice);
			WriteProperty(writer, "L", candle.LowPrice);
			WriteProperty(writer, "C", candle.ClosePrice);
			WriteProperty(writer, "V", candle.TotalVolume);

			if (candle.OpenInterest != null)
				WriteProperty(writer, "oi", candle.OpenInterest.Value);

			if (candle.SeqNum != default)
				WriteProperty(writer, "sn", candle.SeqNum);

			if (candle.PriceLevels != null)
			{
				writer.WritePropertyName("levels");
				writer.WriteStartArray();

				foreach (var level in candle.PriceLevels)
				{
					writer.WriteStartObject();
					WriteProperty(writer, "price", level.Price);
					WriteProperty(writer, "buyCount", level.BuyCount);
					WriteProperty(writer, "sellCount", level.SellCount);
					WriteProperty(writer, "buyVolume", level.BuyVolume);
					WriteProperty(writer, "sellVolume", level.SellVolume);
					WriteProperty(writer, "volume", level.TotalVolume);
					writer.WriteEndObject();
				}

				writer.WriteEndArray();
			}
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<NewsMessage> messages)
	{
		return Do(messages, (writer, n) =>
		{
			if (!n.Id.IsEmpty())
				WriteProperty(writer, "id", n.Id);

			WriteProperty(writer, "s", n.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", n.LocalTime.UtcDateTime);

			if (n.SecurityId != null)
				WriteProperty(writer, "secCode", n.SecurityId.Value.SecurityCode);

			if (!n.BoardCode.IsEmpty())
				WriteProperty(writer, "board", n.BoardCode);

			WriteProperty(writer, "headline", n.Headline);

			if (!n.Source.IsEmpty())
				WriteProperty(writer, "source", n.Source);

			if (!n.Url.IsEmpty())
				WriteProperty(writer, "url", n.Url);

			if (n.Priority != null)
				WriteProperty(writer, "priority", n.Priority.Value);

			if (!n.Language.IsEmpty())
				WriteProperty(writer, "language", n.Language);

			if (n.ExpiryDate != null)
				WriteProperty(writer, "expiry", n.ExpiryDate.Value);

			if (!n.Story.IsEmpty())
				WriteProperty(writer, "story", n.Story);

			if (n.SeqNum != default)
				WriteProperty(writer, "sn", n.SeqNum);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
	{
		return Do(messages, (writer, security) =>
		{
			WriteProperty(writer, "code", security.SecurityId.SecurityCode);
			WriteProperty(writer, "board", security.SecurityId.BoardCode);

			if (!security.Name.IsEmpty())
				WriteProperty(writer, "name", security.Name);

			if (!security.ShortName.IsEmpty())
				WriteProperty(writer, "shortName", security.ShortName);

			if (security.PriceStep != null)
				WriteProperty(writer, "priceStep", security.PriceStep.Value);

			if (security.VolumeStep != null)
				WriteProperty(writer, "volumeStep", security.VolumeStep.Value);

			if (security.MinVolume != null)
				WriteProperty(writer, "minVolume", security.MinVolume.Value);

			if (security.MaxVolume != null)
				WriteProperty(writer, "maxVolume", security.MaxVolume.Value);

			if (security.Multiplier != null)
				WriteProperty(writer, "multiplier", security.Multiplier.Value);

			if (security.Decimals != null)
				WriteProperty(writer, "decimals", security.Decimals.Value);

			if (security.Currency != null)
				WriteProperty(writer, "currency", security.Currency.Value);

			if (security.SecurityType != null)
				WriteProperty(writer, "type", security.SecurityType.Value);

			if (!security.CfiCode.IsEmpty())
				WriteProperty(writer, "cfiCode", security.CfiCode);

			if (security.Shortable != null)
				WriteProperty(writer, "shortable", security.Shortable.Value);

			if (security.OptionType != null)
				WriteProperty(writer, "optionType", security.OptionType.Value);

			if (security.Strike != null)
				WriteProperty(writer, "strike", security.Strike.Value);

			if (!security.BinaryOptionType.IsEmpty())
				WriteProperty(writer, "binaryOptionType", security.BinaryOptionType);

			if (security.IssueSize != null)
				WriteProperty(writer, "issueSize", security.IssueSize.Value);

			if (security.IssueDate != null)
				WriteProperty(writer, "issueDate", security.IssueDate.Value);

			if (!security.GetUnderlyingCode().IsEmpty())
				WriteProperty(writer, "underlyingId", security.GetUnderlyingCode());

			if (security.UnderlyingSecurityType != null)
				WriteProperty(writer, "underlyingType", security.UnderlyingSecurityType);

			if (security.UnderlyingSecurityMinVolume != null)
				WriteProperty(writer, "underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value);

			if (security.ExpiryDate != null)
				WriteProperty(writer, "expiry", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

			if (security.SettlementDate != null)
				WriteProperty(writer, "settlement", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

			if (!security.BasketCode.IsEmpty())
				WriteProperty(writer, "basketCode", security.BasketCode);

			if (!security.BasketExpression.IsEmpty())
				WriteProperty(writer, "basketExpression", security.BasketExpression);

			if (security.FaceValue != null)
				WriteProperty(writer, "faceValue", security.FaceValue.Value);

			if (security.SettlementType != null)
				WriteProperty(writer, "settlementType", security.SettlementType.Value);

			if (security.OptionStyle != null)
				WriteProperty(writer, "optionStyle", security.OptionStyle.Value);

			if (!security.PrimaryId.SecurityCode.IsEmpty())
				WriteProperty(writer, "primaryCode", security.PrimaryId.SecurityCode);

			if (!security.PrimaryId.BoardCode.IsEmpty())
				WriteProperty(writer, "primaryBoard", security.PrimaryId.BoardCode);

			if (!security.SecurityId.Bloomberg.IsEmpty())
				WriteProperty(writer, "bloomberg", security.SecurityId.Bloomberg);

			if (!security.SecurityId.Cusip.IsEmpty())
				WriteProperty(writer, "cusip", security.SecurityId.Cusip);

			if (!security.SecurityId.IQFeed.IsEmpty())
				WriteProperty(writer, "iqfeed", security.SecurityId.IQFeed);

			if (security.SecurityId.InteractiveBrokers != null)
				WriteProperty(writer, "ib", security.SecurityId.InteractiveBrokers);

			if (!security.SecurityId.Isin.IsEmpty())
				WriteProperty(writer, "isin", security.SecurityId.Isin);

			if (!security.SecurityId.Plaza.IsEmpty())
				WriteProperty(writer, "plaza", security.SecurityId.Plaza);

			if (!security.SecurityId.Ric.IsEmpty())
				WriteProperty(writer, "ric", security.SecurityId.Ric);

			if (!security.SecurityId.Sedol.IsEmpty())
				WriteProperty(writer, "sedol", security.SecurityId.Sedol);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
	{
		return Do(messages, (writer, message) =>
		{
			WriteProperty(writer, "s", message.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", message.LocalTime.UtcDateTime);
			WriteProperty(writer, "acc", message.PortfolioName);
			WriteProperty(writer, "client", message.ClientCode);
			WriteProperty(writer, "depo", message.DepoName);
			WriteProperty(writer, "limit", message.LimitType);
			WriteProperty(writer, "strategyId", message.StrategyId);
			WriteProperty(writer, "side", message.Side);

			foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
				WriteProperty(writer, pair.Key.ToString(), pair.Value);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
	{
		return Do(values, (writer, value) =>
		{
			WriteProperty(writer, "time", value.Time.UtcDateTime);

			var index = 1;
			foreach (var indVal in value.ValuesAsDecimal)
				WriteProperty(writer, $"value{index++}", indVal);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, (writer, item) =>
		{
			WriteProperty(writer, "id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
			WriteProperty(writer, "s", item.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", item.LocalTime.UtcDateTime);
			WriteProperty(writer, "p", item.OrderPrice);
			WriteProperty(writer, "v", item.OrderVolume);
			WriteProperty(writer, "side", item.Side);
			WriteProperty(writer, "state", item.OrderState);
			WriteProperty(writer, "tif", item.TimeInForce);
			WriteProperty(writer, "sys", item.IsSystem);

			if (item.SeqNum != default)
				WriteProperty(writer, "sn", item.SeqNum);

			if (item.TradePrice != null)
			{
				WriteProperty(writer, "tid", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
				WriteProperty(writer, "tp", item.TradePrice);

				if (item.OpenInterest != null)
					WriteProperty(writer, "oi", item.OpenInterest.Value);
			}
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, (writer, trade) =>
		{
			WriteProperty(writer, "id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>());
			WriteProperty(writer, "s", trade.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", trade.LocalTime.UtcDateTime);
			WriteProperty(writer, "p", trade.TradePrice);
			WriteProperty(writer, "v", trade.TradeVolume);

			if (trade.OriginSide != null)
				WriteProperty(writer, "side", trade.OriginSide.Value);

			if (trade.OpenInterest != null)
				WriteProperty(writer, "oi", trade.OpenInterest.Value);

			if (trade.IsUpTick != null)
				WriteProperty(writer, "up", trade.IsUpTick.Value);

			if (trade.Currency != null)
				WriteProperty(writer, "cur", trade.Currency.Value);

			if (trade.SeqNum != default)
				WriteProperty(writer, "sn", trade.SeqNum);

			if (trade.Yield != default)
				WriteProperty(writer, "yield", trade.Yield);

			if (trade.OrderBuyId != default)
				WriteProperty(writer, "buy", trade.OrderBuyId);

			if (trade.OrderSellId != default)
				WriteProperty(writer, "sell", trade.OrderSellId);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
	{
		return Do(messages, (writer, item) =>
		{
			WriteProperty(writer, "s", item.ServerTime.UtcDateTime);
			WriteProperty(writer, "l", item.LocalTime.UtcDateTime);
			WriteProperty(writer, "acc", item.PortfolioName);
			WriteProperty(writer, "client", item.ClientCode);
			WriteProperty(writer, "broker", item.BrokerCode);
			WriteProperty(writer, "depo", item.DepoName);
			WriteProperty(writer, "transactionId", item.TransactionId);
			WriteProperty(writer, "originalTransactionId", item.OriginalTransactionId);
			WriteProperty(writer, "orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
			WriteProperty(writer, "orderPrice", item.OrderPrice);
			WriteProperty(writer, "orderVolume", item.OrderVolume);
			WriteProperty(writer, "orderType", item.OrderType);
			WriteProperty(writer, "orderState", item.OrderState);
			WriteProperty(writer, "orderStatus", item.OrderStatus);
			WriteProperty(writer, "visibleVolume", item.VisibleVolume);
			WriteProperty(writer, "balance", item.Balance);
			WriteProperty(writer, "side", item.Side);
			WriteProperty(writer, "originSide", item.OriginSide);
			WriteProperty(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
			WriteProperty(writer, "tradePrice", item.TradePrice);
			WriteProperty(writer, "tradeVolume", item.TradeVolume);
			WriteProperty(writer, "tradeStatus", item.TradeStatus);
			WriteProperty(writer, "isOrder", item.HasOrderInfo);
			WriteProperty(writer, "commission", item.Commission);
			WriteProperty(writer, "commissionCurrency", item.CommissionCurrency);
			WriteProperty(writer, "pnl", item.PnL);
			WriteProperty(writer, "position", item.Position);
			WriteProperty(writer, "latency", item.Latency);
			WriteProperty(writer, "slippage", item.Slippage);
			WriteProperty(writer, "error", item.Error?.Message);
			WriteProperty(writer, "openInterest", item.OpenInterest);
			WriteProperty(writer, "isCancelled", item.IsCancellation);
			WriteProperty(writer, "isSystem", item.IsSystem);
			WriteProperty(writer, "isUpTick", item.IsUpTick);
			WriteProperty(writer, "userOrderId", item.UserOrderId);
			WriteProperty(writer, "strategyId", item.StrategyId);
			WriteProperty(writer, "currency", item.Currency);
			WriteProperty(writer, "marginMode", item.MarginMode);
			WriteProperty(writer, "isMarketMaker", item.IsMarketMaker);
			WriteProperty(writer, "isManual", item.IsManual);
			WriteProperty(writer, "averagePrice", item.AveragePrice);
			WriteProperty(writer, "yield", item.Yield);
			WriteProperty(writer, "minVolume", item.MinVolume);
			WriteProperty(writer, "positionEffect", item.PositionEffect);
			WriteProperty(writer, "postOnly", item.PostOnly);
			WriteProperty(writer, "initiator", item.Initiator);
			WriteProperty(writer, "sn", item.SeqNum);
			WriteProperty(writer, "leverage", item.Leverage);
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardStateMessage> messages)
	{
		return Do(messages, (writer, msg) =>
		{
			WriteProperty(writer, "serverTime", msg.ServerTime.UtcDateTime);
			WriteProperty(writer, "boardCode", msg.BoardCode);
			WriteProperty(writer, "state", msg.State.ToString());
		});
	}

	/// <inheritdoc />
	protected override (int, DateTimeOffset?) Export(IEnumerable<BoardMessage> messages)
	{
		return Do(messages, (writer, msg) =>
		{
			WriteProperty(writer, "code", msg.Code);
			WriteProperty(writer, "exchangeCode", msg.ExchangeCode);
			WriteProperty(writer, "expiryTime", msg.ExpiryTime.ToString());
			WriteProperty(writer, "timeZone", msg.TimeZone?.Id);
		});
	}

	private (int, DateTimeOffset?) Do<TValue>(IEnumerable<TValue> values, Action<JsonTextWriter, TValue> action)
	{
		var count = 0;
		var lastTime = default(DateTimeOffset?);

		using (var writer = new StreamWriter(Path))
		{
			var json = new JsonTextWriter(writer);

			if (Indent)
				json.Formatting = Formatting.Indented;

			json.WriteStartArray();

			foreach (var value in values)
			{
				if (!CanProcess())
					break;

				json.WriteStartObject();

				action(json, value);

				json.WriteEndObject();

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			json.WriteEndArray();
		}

		return (count, lastTime);
	}
}