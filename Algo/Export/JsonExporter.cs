namespace StockSharp.Algo.Export;

using Newtonsoft.Json;

/// <summary>
/// The export into json.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="stream">The stream to write to.</param>
public class JsonExporter(DataType dataType, Stream stream) : BaseExporter(dataType)
{
	/// <summary>
	/// Gets or sets a value indicating whether to indent elements.
	/// </summary>
	/// <remarks>
	/// By default is <see langword="true"/>.
	/// </remarks>
	public bool Indent { get; set; } = true;

	private static async Task WriteProperty(JsonWriter writer, string name, object value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		await writer.WritePropertyNameAsync(name, cancellationToken);
		await writer.WriteValueAsync(value, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, depth, t) =>
		{
			await WriteProperty(writer, "s", depth.ServerTime, t);
			await WriteProperty(writer, "l", depth.LocalTime, t);

			if (depth.State != null)
				await WriteProperty(writer, "st", depth.State.Value, t);

			if (depth.HasPositions)
				await WriteProperty(writer, "pos", true, t);

			if (depth.SeqNum != default)
				await WriteProperty(writer, "sn", depth.SeqNum, t);

			async Task WriteQuotes(string name, QuoteChange[] quotes, CancellationToken ct)
			{
				await writer.WritePropertyNameAsync(name, ct);
				await writer.WriteStartArrayAsync(ct);

				foreach (var quote in quotes)
				{
					await writer.WriteStartObjectAsync(ct);

					await WriteProperty(writer, "p", quote.Price, ct);
					await WriteProperty(writer, "v", quote.Volume, ct);

					if (quote.OrdersCount != default)
						await WriteProperty(writer, "cnt", quote.OrdersCount.Value, ct);

					if (quote.StartPosition != default)
						await WriteProperty(writer, "s", quote.StartPosition.Value, ct);

					if (quote.EndPosition != default)
						await WriteProperty(writer, "e", quote.EndPosition.Value, ct);

					if (quote.Action != default)
						await WriteProperty(writer, "a", quote.Action.Value, ct);

					if (quote.Condition != default)
						await WriteProperty(writer, "cond", quote.Condition, ct);

					await writer.WriteEndObjectAsync(ct);
				}

				await writer.WriteEndArrayAsync(ct);
			}

			await WriteQuotes("bids", depth.Bids, t);
			await WriteQuotes("asks", depth.Asks, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, message, t) =>
		{
			await WriteProperty(writer, "s", message.ServerTime, t);
			await WriteProperty(writer, "l", message.LocalTime, t);

			if (message.SeqNum != default)
				await WriteProperty(writer, "sn", message.SeqNum, t);

			foreach (var pair in message.Changes)
				await WriteProperty(writer, pair.Key.ToString(), pair.Value, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, candle, t) =>
		{
			await WriteProperty(writer, "open", candle.OpenTime, t);
			await WriteProperty(writer, "close", candle.CloseTime, t);
			await WriteProperty(writer, "O", candle.OpenPrice, t);
			await WriteProperty(writer, "H", candle.HighPrice, t);
			await WriteProperty(writer, "L", candle.LowPrice, t);
			await WriteProperty(writer, "C", candle.ClosePrice, t);
			await WriteProperty(writer, "V", candle.TotalVolume, t);

			if (candle.OpenInterest != null)
				await WriteProperty(writer, "oi", candle.OpenInterest.Value, t);

			if (candle.SeqNum != default)
				await WriteProperty(writer, "sn", candle.SeqNum, t);

			if (candle.PriceLevels != null)
			{
				await writer.WritePropertyNameAsync("levels", t);
				await writer.WriteStartArrayAsync(t);

				foreach (var level in candle.PriceLevels)
				{
					await writer.WriteStartObjectAsync(t);
					await WriteProperty(writer, "price", level.Price, t);
					await WriteProperty(writer, "buyCount", level.BuyCount, t);
					await WriteProperty(writer, "sellCount", level.SellCount, t);
					await WriteProperty(writer, "buyVolume", level.BuyVolume, t);
					await WriteProperty(writer, "sellVolume", level.SellVolume, t);
					await WriteProperty(writer, "volume", level.TotalVolume, t);
					await writer.WriteEndObjectAsync(t);
				}

				await writer.WriteEndArrayAsync(t);
			}
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, n, t) =>
		{
			if (!n.Id.IsEmpty())
				await WriteProperty(writer, "id", n.Id, t);

			await WriteProperty(writer, "s", n.ServerTime, t);
			await WriteProperty(writer, "l", n.LocalTime, t);

			if (n.SecurityId != null)
				await WriteProperty(writer, "secCode", n.SecurityId.Value.SecurityCode, t);

			if (!n.BoardCode.IsEmpty())
				await WriteProperty(writer, "board", n.BoardCode, t);

			await WriteProperty(writer, "headline", n.Headline, t);

			if (!n.Source.IsEmpty())
				await WriteProperty(writer, "source", n.Source, t);

			if (!n.Url.IsEmpty())
				await WriteProperty(writer, "url", n.Url, t);

			if (n.Priority != null)
				await WriteProperty(writer, "priority", n.Priority.Value, t);

			if (!n.Language.IsEmpty())
				await WriteProperty(writer, "language", n.Language, t);

			if (n.ExpiryDate != null)
				await WriteProperty(writer, "expiry", n.ExpiryDate.Value, t);

			if (!n.Story.IsEmpty())
				await WriteProperty(writer, "story", n.Story, t);

			if (n.SeqNum != default)
				await WriteProperty(writer, "sn", n.SeqNum, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, security, t) =>
		{
			await WriteProperty(writer, "code", security.SecurityId.SecurityCode, t);
			await WriteProperty(writer, "board", security.SecurityId.BoardCode, t);

			if (!security.Name.IsEmpty())
				await WriteProperty(writer, "name", security.Name, t);

			if (!security.ShortName.IsEmpty())
				await WriteProperty(writer, "shortName", security.ShortName, t);

			if (security.PriceStep != null)
				await WriteProperty(writer, "priceStep", security.PriceStep.Value, t);

			if (security.VolumeStep != null)
				await WriteProperty(writer, "volumeStep", security.VolumeStep.Value, t);

			if (security.MinVolume != null)
				await WriteProperty(writer, "minVolume", security.MinVolume.Value, t);

			if (security.MaxVolume != null)
				await WriteProperty(writer, "maxVolume", security.MaxVolume.Value, t);

			if (security.Multiplier != null)
				await WriteProperty(writer, "multiplier", security.Multiplier.Value, t);

			if (security.Decimals != null)
				await WriteProperty(writer, "decimals", security.Decimals.Value, t);

			if (security.Currency != null)
				await WriteProperty(writer, "currency", security.Currency.Value, t);

			if (security.SecurityType != null)
				await WriteProperty(writer, "type", security.SecurityType.Value, t);

			if (!security.CfiCode.IsEmpty())
				await WriteProperty(writer, "cfiCode", security.CfiCode, t);

			if (security.Shortable != null)
				await WriteProperty(writer, "shortable", security.Shortable.Value, t);

			if (security.OptionType != null)
				await WriteProperty(writer, "optionType", security.OptionType.Value, t);

			if (security.Strike != null)
				await WriteProperty(writer, "strike", security.Strike.Value, t);

			if (!security.BinaryOptionType.IsEmpty())
				await WriteProperty(writer, "binaryOptionType", security.BinaryOptionType, t);

			if (security.IssueSize != null)
				await WriteProperty(writer, "issueSize", security.IssueSize.Value, t);

			if (security.IssueDate != null)
				await WriteProperty(writer, "issueDate", security.IssueDate.Value, t);

			if (!security.GetUnderlyingCode().IsEmpty())
				await WriteProperty(writer, "underlyingId", security.GetUnderlyingCode(), t);

			if (security.UnderlyingSecurityType != null)
				await WriteProperty(writer, "underlyingType", security.UnderlyingSecurityType, t);

			if (security.UnderlyingSecurityMinVolume != null)
				await WriteProperty(writer, "underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value, t);

			if (security.ExpiryDate != null)
				await WriteProperty(writer, "expiry", security.ExpiryDate.Value.ToString("yyyy-MM-dd"), t);

			if (security.SettlementDate != null)
				await WriteProperty(writer, "settlement", security.SettlementDate.Value.ToString("yyyy-MM-dd"), t);

			if (!security.BasketCode.IsEmpty())
				await WriteProperty(writer, "basketCode", security.BasketCode, t);

			if (!security.BasketExpression.IsEmpty())
				await WriteProperty(writer, "basketExpression", security.BasketExpression, t);

			if (security.FaceValue != null)
				await WriteProperty(writer, "faceValue", security.FaceValue.Value, t);

			if (security.SettlementType != null)
				await WriteProperty(writer, "settlementType", security.SettlementType.Value, t);

			if (security.OptionStyle != null)
				await WriteProperty(writer, "optionStyle", security.OptionStyle.Value, t);

			if (!security.PrimaryId.SecurityCode.IsEmpty())
				await WriteProperty(writer, "primaryCode", security.PrimaryId.SecurityCode, t);

			if (!security.PrimaryId.BoardCode.IsEmpty())
				await WriteProperty(writer, "primaryBoard", security.PrimaryId.BoardCode, t);

			if (!security.SecurityId.Bloomberg.IsEmpty())
				await WriteProperty(writer, "bloomberg", security.SecurityId.Bloomberg, t);

			if (!security.SecurityId.Cusip.IsEmpty())
				await WriteProperty(writer, "cusip", security.SecurityId.Cusip, t);

			if (!security.SecurityId.IQFeed.IsEmpty())
				await WriteProperty(writer, "iqfeed", security.SecurityId.IQFeed, t);

			if (security.SecurityId.InteractiveBrokers != null)
				await WriteProperty(writer, "ib", security.SecurityId.InteractiveBrokers, t);

			if (!security.SecurityId.Isin.IsEmpty())
				await WriteProperty(writer, "isin", security.SecurityId.Isin, t);

			if (!security.SecurityId.Plaza.IsEmpty())
				await WriteProperty(writer, "plaza", security.SecurityId.Plaza, t);

			if (!security.SecurityId.Ric.IsEmpty())
				await WriteProperty(writer, "ric", security.SecurityId.Ric, t);

			if (!security.SecurityId.Sedol.IsEmpty())
				await WriteProperty(writer, "sedol", security.SecurityId.Sedol, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, message, t) =>
		{
			await WriteProperty(writer, "s", message.ServerTime, t);
			await WriteProperty(writer, "l", message.LocalTime, t);
			await WriteProperty(writer, "acc", message.PortfolioName, t);
			await WriteProperty(writer, "client", message.ClientCode, t);
			await WriteProperty(writer, "depo", message.DepoName, t);
			await WriteProperty(writer, "limit", message.LimitType, t);
			await WriteProperty(writer, "strategyId", message.StrategyId, t);
			await WriteProperty(writer, "side", message.Side, t);

			foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
				await WriteProperty(writer, pair.Key.ToString(), pair.Value, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
	{
		return Do(values, async (writer, value, t) =>
		{
			await WriteProperty(writer, "time", value.Time, t);

			var index =1;
			foreach (var indVal in value.ValuesAsDecimal)
				await WriteProperty(writer, $"value{index++}", indVal, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, item, t) =>
		{
			await WriteProperty(writer, "id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>(), t);
			await WriteProperty(writer, "s", item.ServerTime, t);
			await WriteProperty(writer, "l", item.LocalTime, t);
			await WriteProperty(writer, "p", item.OrderPrice, t);
			await WriteProperty(writer, "v", item.OrderVolume, t);
			await WriteProperty(writer, "side", item.Side, t);
			await WriteProperty(writer, "state", item.OrderState, t);
			await WriteProperty(writer, "tif", item.TimeInForce, t);
			await WriteProperty(writer, "sys", item.IsSystem, t);

			if (item.SeqNum != default)
				await WriteProperty(writer, "sn", item.SeqNum, t);

			if (item.TradePrice != null)
			{
				await WriteProperty(writer, "tid", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>(), t);
				await WriteProperty(writer, "tp", item.TradePrice, t);

				if (item.OpenInterest != null)
					await WriteProperty(writer, "oi", item.OpenInterest.Value, t);
			}
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, trade, t) =>
		{
			await WriteProperty(writer, "id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>(), t);
			await WriteProperty(writer, "s", trade.ServerTime, t);
			await WriteProperty(writer, "l", trade.LocalTime, t);
			await WriteProperty(writer, "p", trade.TradePrice, t);
			await WriteProperty(writer, "v", trade.TradeVolume, t);

			if (trade.OriginSide != null)
				await WriteProperty(writer, "side", trade.OriginSide.Value, t);

			if (trade.OpenInterest != null)
				await WriteProperty(writer, "oi", trade.OpenInterest.Value, t);

			if (trade.IsUpTick != null)
				await WriteProperty(writer, "up", trade.IsUpTick.Value, t);

			if (trade.Currency != null)
				await WriteProperty(writer, "cur", trade.Currency.Value, t);

			if (trade.SeqNum != default)
				await WriteProperty(writer, "sn", trade.SeqNum, t);

			if (trade.Yield != default)
				await WriteProperty(writer, "yield", trade.Yield, t);

			if (trade.OrderBuyId != default)
				await WriteProperty(writer, "buy", trade.OrderBuyId, t);

			if (trade.OrderSellId != default)
				await WriteProperty(writer, "sell", trade.OrderSellId, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, item, t) =>
		{
			await WriteProperty(writer, "s", item.ServerTime, t);
			await WriteProperty(writer, "l", item.LocalTime, t);
			await WriteProperty(writer, "acc", item.PortfolioName, t);
			await WriteProperty(writer, "client", item.ClientCode, t);
			await WriteProperty(writer, "broker", item.BrokerCode, t);
			await WriteProperty(writer, "depo", item.DepoName, t);
			await WriteProperty(writer, "transactionId", item.TransactionId, t);
			await WriteProperty(writer, "originalTransactionId", item.OriginalTransactionId, t);
			await WriteProperty(writer, "orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>(), t);
			await WriteProperty(writer, "orderPrice", item.OrderPrice, t);
			await WriteProperty(writer, "orderVolume", item.OrderVolume, t);
			await WriteProperty(writer, "orderType", item.OrderType, t);
			await WriteProperty(writer, "orderState", item.OrderState, t);
			await WriteProperty(writer, "orderStatus", item.OrderStatus, t);
			await WriteProperty(writer, "visibleVolume", item.VisibleVolume, t);
			await WriteProperty(writer, "balance", item.Balance, t);
			await WriteProperty(writer, "side", item.Side, t);
			await WriteProperty(writer, "originSide", item.OriginSide, t);
			await WriteProperty(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>(), t);
			await WriteProperty(writer, "tradePrice", item.TradePrice, t);
			await WriteProperty(writer, "tradeVolume", item.TradeVolume, t);
			await WriteProperty(writer, "tradeStatus", item.TradeStatus, t);
			await WriteProperty(writer, "isOrder", item.HasOrderInfo, t);
			await WriteProperty(writer, "commission", item.Commission, t);
			await WriteProperty(writer, "commissionCurrency", item.CommissionCurrency, t);
			await WriteProperty(writer, "pnl", item.PnL, t);
			await WriteProperty(writer, "position", item.Position, t);
			await WriteProperty(writer, "latency", item.Latency, t);
			await WriteProperty(writer, "slippage", item.Slippage, t);
			await WriteProperty(writer, "error", item.Error?.Message, t);
			await WriteProperty(writer, "openInterest", item.OpenInterest, t);
			await WriteProperty(writer, "isCancelled", item.IsCancellation, t);
			await WriteProperty(writer, "isSystem", item.IsSystem, t);
			await WriteProperty(writer, "isUpTick", item.IsUpTick, t);
			await WriteProperty(writer, "userOrderId", item.UserOrderId, t);
			await WriteProperty(writer, "strategyId", item.StrategyId, t);
			await WriteProperty(writer, "currency", item.Currency, t);
			await WriteProperty(writer, "marginMode", item.MarginMode, t);
			await WriteProperty(writer, "isMarketMaker", item.IsMarketMaker, t);
			await WriteProperty(writer, "isManual", item.IsManual, t);
			await WriteProperty(writer, "averagePrice", item.AveragePrice, t);
			await WriteProperty(writer, "yield", item.Yield, t);
			await WriteProperty(writer, "minVolume", item.MinVolume, t);
			await WriteProperty(writer, "positionEffect", item.PositionEffect, t);
			await WriteProperty(writer, "postOnly", item.PostOnly, t);
			await WriteProperty(writer, "initiator", item.Initiator, t);
			await WriteProperty(writer, "sn", item.SeqNum, t);
			await WriteProperty(writer, "leverage", item.Leverage, t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, msg, t) =>
		{
			await WriteProperty(writer, "serverTime", msg.ServerTime, t);
			await WriteProperty(writer, "boardCode", msg.BoardCode, t);
			await WriteProperty(writer, "state", msg.State.ToString(), t);
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, async (writer, msg, t) =>
		{
			await WriteProperty(writer, "code", msg.Code, t);
			await WriteProperty(writer, "exchangeCode", msg.ExchangeCode, t);
			await WriteProperty(writer, "expiryTime", msg.ExpiryTime.ToString(), t);
			await WriteProperty(writer, "timeZone", msg.TimeZone?.Id, t);
		}, cancellationToken);
	}

	private async Task<(int, DateTime?)> Do<TValue>(IEnumerable<TValue> values, Func<JsonTextWriter, TValue, CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		var count =0;
		var lastTime = default(DateTime?);

		using (var writer = new StreamWriter(stream, Encoding, leaveOpen: true))
		{
			var json = new JsonTextWriter(writer);

			if (Indent)
				json.Formatting = Formatting.Indented;

			await json.WriteStartArrayAsync(cancellationToken);

			foreach (var value in values)
			{
				await json.WriteStartObjectAsync(cancellationToken);

				await action(json, value, cancellationToken);

				await json.WriteEndObjectAsync(cancellationToken);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			await json.WriteEndArrayAsync(cancellationToken);
		}

		return (count, lastTime);
	}
}