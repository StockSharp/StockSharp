namespace StockSharp.Algo.Export;

using System.Globalization;
using System.Xml;

/// <summary>
/// The export into xml.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XmlExporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="stream">The stream to write to.</param>
public class XmlExporter(DataType dataType, Stream stream) : BaseExporter(dataType)
{
	private const string _timeFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

	/// <summary>
	/// Gets or sets a value indicating whether to indent elements.
	/// </summary>
	/// <remarks>
	/// By default is <see langword="true"/>.
	/// </remarks>
	public bool Indent { get; set; } = true;

	private static Task WriteAttrAsync(XmlWriter writer, string name, object value, CancellationToken _)
	{
		if (value is null)
			return Task.CompletedTask;

		string str = value switch
		{
			DateTimeOffset dto => dto.ToString(_timeFormat, CultureInfo.InvariantCulture),
			DateTime dt => dt.ToString(_timeFormat, CultureInfo.InvariantCulture),
			IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};

		return writer.WriteAttributeStringAsync(null, name, null, str);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "orderLog", async (writer, item, t) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			await WriteAttrAsync(writer, "id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>(), t);
			await WriteAttrAsync(writer, "serverTime", item.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", item.LocalTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "price", item.OrderPrice, t);
			await WriteAttrAsync(writer, "volume", item.OrderVolume, t);
			await WriteAttrAsync(writer, "side", item.Side, t);
			await WriteAttrAsync(writer, "state", item.OrderState, t);
			await WriteAttrAsync(writer, "timeInForce", item.TimeInForce, t);
			await WriteAttrAsync(writer, "isSystem", item.IsSystem, t);

			if (item.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", item.SeqNum, t);

			if (item.TradePrice != null)
			{
				await WriteAttrAsync(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>(), t);
				await WriteAttrAsync(writer, "tradePrice", item.TradePrice, t);

				if (item.OpenInterest != null)
					await WriteAttrAsync(writer, "openInterest", item.OpenInterest.Value, t);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "ticks", async (writer, trade, t) =>
		{
			await writer.WriteStartElementAsync(null, "trade", null);

			await WriteAttrAsync(writer, "id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>(), t);
			await WriteAttrAsync(writer, "serverTime", trade.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", trade.LocalTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "price", trade.TradePrice, t);
			await WriteAttrAsync(writer, "volume", trade.TradeVolume, t);

			if (trade.OriginSide != null)
				await WriteAttrAsync(writer, "originSide", trade.OriginSide.Value, t);

			if (trade.OpenInterest != null)
				await WriteAttrAsync(writer, "openInterest", trade.OpenInterest.Value, t);

			if (trade.IsUpTick != null)
				await WriteAttrAsync(writer, "isUpTick", trade.IsUpTick.Value, t);

			if (trade.Currency != null)
				await WriteAttrAsync(writer, "currency", trade.Currency.Value, t);

			if (trade.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", trade.SeqNum, t);

			if (trade.Yield != default)
				await WriteAttrAsync(writer, "yield", trade.Yield, t);

			if (trade.OrderBuyId != default)
				await WriteAttrAsync(writer, "buy", trade.OrderBuyId, t);

			if (trade.OrderSellId != default)
				await WriteAttrAsync(writer, "sell", trade.OrderSellId, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "transactions", async (writer, item, t) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			await WriteAttrAsync(writer, "serverTime", item.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", item.LocalTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "portfolio", item.PortfolioName, t);
			await WriteAttrAsync(writer, "clientCode", item.ClientCode, t);
			await WriteAttrAsync(writer, "brokerCode", item.BrokerCode, t);
			await WriteAttrAsync(writer, "depoName", item.DepoName, t);
			await WriteAttrAsync(writer, "transactionId", item.TransactionId, t);
			await WriteAttrAsync(writer, "originalTransactionId", item.OriginalTransactionId, t);
			await WriteAttrAsync(writer, "orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>(), t);
			await WriteAttrAsync(writer, "orderPrice", item.OrderPrice, t);
			await WriteAttrAsync(writer, "orderVolume", item.OrderVolume, t);
			await WriteAttrAsync(writer, "orderType", item.OrderType, t);
			await WriteAttrAsync(writer, "orderState", item.OrderState, t);
			await WriteAttrAsync(writer, "orderStatus", item.OrderStatus, t);
			await WriteAttrAsync(writer, "visibleVolume", item.VisibleVolume, t);
			await WriteAttrAsync(writer, "balance", item.Balance, t);
			await WriteAttrAsync(writer, "side", item.Side, t);
			await WriteAttrAsync(writer, "originSide", item.OriginSide, t);
			await WriteAttrAsync(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>(), t);
			await WriteAttrAsync(writer, "tradePrice", item.TradePrice, t);
			await WriteAttrAsync(writer, "tradeVolume", item.TradeVolume, t);
			await WriteAttrAsync(writer, "tradeStatus", item.TradeStatus, t);
			await WriteAttrAsync(writer, "isOrder", item.HasOrderInfo, t);
			await WriteAttrAsync(writer, "commission", item.Commission, t);
			await WriteAttrAsync(writer, "commissionCurrency", item.CommissionCurrency, t);
			await WriteAttrAsync(writer, "pnl", item.PnL, t);
			await WriteAttrAsync(writer, "position", item.Position, t);
			await WriteAttrAsync(writer, "latency", item.Latency, t);
			await WriteAttrAsync(writer, "slippage", item.Slippage, t);
			await WriteAttrAsync(writer, "error", item.Error?.Message, t);
			await WriteAttrAsync(writer, "openInterest", item.OpenInterest, t);
			await WriteAttrAsync(writer, "isCancelled", item.IsCancellation, t);
			await WriteAttrAsync(writer, "isSystem", item.IsSystem, t);
			await WriteAttrAsync(writer, "isUpTick", item.IsUpTick, t);
			await WriteAttrAsync(writer, "userOrderId", item.UserOrderId, t);
			await WriteAttrAsync(writer, "strategyId", item.StrategyId, t);
			await WriteAttrAsync(writer, "currency", item.Currency, t);
			await WriteAttrAsync(writer, "marginMode", item.MarginMode, t);
			await WriteAttrAsync(writer, "isMarketMaker", item.IsMarketMaker, t);
			await WriteAttrAsync(writer, "isManual", item.IsManual, t);
			await WriteAttrAsync(writer, "averagePrice", item.AveragePrice, t);
			await WriteAttrAsync(writer, "yield", item.Yield, t);
			await WriteAttrAsync(writer, "minVolume", item.MinVolume, t);
			await WriteAttrAsync(writer, "positionEffect", item.PositionEffect, t);
			await WriteAttrAsync(writer, "postOnly", item.PostOnly, t);
			await WriteAttrAsync(writer, "initiator", item.Initiator, t);
			await WriteAttrAsync(writer, "seqNum", item.SeqNum, t);
			await WriteAttrAsync(writer, "leverage", item.Leverage, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "depths", async (writer, depth, t) =>
		{
			await writer.WriteStartElementAsync(null, "depth", null);

			await WriteAttrAsync(writer, "serverTime", depth.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", depth.LocalTime.ToString(_timeFormat), t);

			if (depth.State != null)
				await WriteAttrAsync(writer, "state", depth.State.Value, t);

			if (depth.HasPositions)
				await WriteAttrAsync(writer, "pos", true, t);

			if (depth.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", depth.SeqNum, t);

			var bids = new HashSet<QuoteChange>(depth.Bids);

			foreach (var quote in depth.Bids.Concat(depth.Asks).OrderByDescending(q => q.Price))
			{
				await writer.WriteStartElementAsync(null, "quote", null);

				await WriteAttrAsync(writer, "price", quote.Price, t);
				await WriteAttrAsync(writer, "volume", quote.Volume, t);
				await WriteAttrAsync(writer, "side", bids.Contains(quote) ? Sides.Buy : Sides.Sell, t);

				if (quote.OrdersCount != default)
					await WriteAttrAsync(writer, "ordersCount", quote.OrdersCount.Value, t);

				if (quote.StartPosition != default)
					await WriteAttrAsync(writer, "startPos", quote.StartPosition.Value, t);

				if (quote.EndPosition != default)
					await WriteAttrAsync(writer, "endPos", quote.EndPosition.Value, t);

				if (quote.Action != default)
					await WriteAttrAsync(writer, "action", quote.Action.Value, t);

				if (quote.Condition != default)
					await WriteAttrAsync(writer, "condition", quote.Condition, t);

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "level1", async (writer, message, t) =>
		{
			await writer.WriteStartElementAsync(null, "change", null);

			await WriteAttrAsync(writer, "serverTime", message.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", message.LocalTime.ToString(_timeFormat), t);

			if (message.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", message.SeqNum, t);

			foreach (var pair in message.Changes)
			{
				var val = (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value;
				await WriteAttrAsync(writer, pair.Key.ToString(), val, t);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "positions", async (writer, message, t) =>
		{
			await writer.WriteStartElementAsync(null, "change", null);

			await WriteAttrAsync(writer, "serverTime", message.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", message.LocalTime.ToString(_timeFormat), t);

			await WriteAttrAsync(writer, "portfolio", message.PortfolioName, t);
			await WriteAttrAsync(writer, "clientCode", message.ClientCode, t);
			await WriteAttrAsync(writer, "depoName", message.DepoName, t);
			await WriteAttrAsync(writer, "limit", message.LimitType, t);
			await WriteAttrAsync(writer, "strategyId", message.StrategyId, t);
			await WriteAttrAsync(writer, "side", message.Side, t);

			foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
			{
				var val = (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value;
				await WriteAttrAsync(writer, pair.Key.ToString(), val, t);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
	{
		return Do(values, "values", async (writer, value, t) =>
		{
			await writer.WriteStartElementAsync(null, "value", null);

			await WriteAttrAsync(writer, "time", value.Time.ToString(_timeFormat), t);

			var index =1;
			foreach (var indVal in value.ValuesAsDecimal)
				await WriteAttrAsync(writer, $"value{index++}", indVal, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "candles", async (writer, candle, t) =>
		{
			await writer.WriteStartElementAsync(null, "candle", null);

			await WriteAttrAsync(writer, "openTime", candle.OpenTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "closeTime", candle.CloseTime.ToString(_timeFormat), t);

			await WriteAttrAsync(writer, "O", candle.OpenPrice, t);
			await WriteAttrAsync(writer, "H", candle.HighPrice, t);
			await WriteAttrAsync(writer, "L", candle.LowPrice, t);
			await WriteAttrAsync(writer, "C", candle.ClosePrice, t);
			await WriteAttrAsync(writer, "V", candle.TotalVolume, t);

			if (candle.OpenInterest != null)
				await WriteAttrAsync(writer, "openInterest", candle.OpenInterest.Value, t);

			if (candle.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", candle.SeqNum, t);

			if (candle.PriceLevels != null)
			{
				await writer.WriteStartElementAsync(null, "levels", null);

				foreach (var level in candle.PriceLevels)
				{
					await writer.WriteStartElementAsync(null, "level", null);

					await WriteAttrAsync(writer, "price", level.Price, t);
					await WriteAttrAsync(writer, "buyCount", level.BuyCount, t);
					await WriteAttrAsync(writer, "sellCount", level.SellCount, t);
					await WriteAttrAsync(writer, "buyVolume", level.BuyVolume, t);
					await WriteAttrAsync(writer, "sellVolume", level.SellVolume, t);
					await WriteAttrAsync(writer, "volume", level.TotalVolume, t);

					await writer.WriteEndElementAsync();
				}

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "news", async (writer, n, t) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			if (!n.Id.IsEmpty())
				await WriteAttrAsync(writer, "id", n.Id, t);

			await WriteAttrAsync(writer, "serverTime", n.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "localTime", n.LocalTime.ToString(_timeFormat), t);

			if (n.SecurityId != null)
				await WriteAttrAsync(writer, "securityCode", n.SecurityId.Value.SecurityCode, t);

			if (!n.BoardCode.IsEmpty())
				await WriteAttrAsync(writer, "boardCode", n.BoardCode, t);

			await WriteAttrAsync(writer, "headline", n.Headline, t);

			if (!n.Source.IsEmpty())
				await WriteAttrAsync(writer, "source", n.Source, t);

			if (!n.Url.IsEmpty())
				await WriteAttrAsync(writer, "url", n.Url, t);

			if (n.Priority != null)
				await WriteAttrAsync(writer, "priority", n.Priority.Value, t);

			if (!n.Language.IsEmpty())
				await WriteAttrAsync(writer, "language", n.Language, t);

			if (n.ExpiryDate != null)
				await WriteAttrAsync(writer, "expiry", n.ExpiryDate.Value, t);

			if (!n.Story.IsEmpty())
				await writer.WriteCDataAsync(n.Story);

			if (n.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", n.SeqNum, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "securities", async (writer, security, t) =>
		{
			await writer.WriteStartElementAsync(null, "security", null);

			await WriteAttrAsync(writer, "code", security.SecurityId.SecurityCode, t);
			await WriteAttrAsync(writer, "board", security.SecurityId.BoardCode, t);

			if (!security.Name.IsEmpty())
				await WriteAttrAsync(writer, "name", security.Name, t);

			if (!security.ShortName.IsEmpty())
				await WriteAttrAsync(writer, "shortName", security.ShortName, t);

			if (security.PriceStep != null)
				await WriteAttrAsync(writer, "priceStep", security.PriceStep.Value, t);

			if (security.VolumeStep != null)
				await WriteAttrAsync(writer, "volumeStep", security.VolumeStep.Value, t);

			if (security.MinVolume != null)
				await WriteAttrAsync(writer, "minVolume", security.MinVolume.Value, t);

			if (security.MaxVolume != null)
				await WriteAttrAsync(writer, "maxVolume", security.MaxVolume.Value, t);

			if (security.Multiplier != null)
				await WriteAttrAsync(writer, "multiplier", security.Multiplier.Value, t);

			if (security.Decimals != null)
				await WriteAttrAsync(writer, "decimals", security.Decimals.Value, t);

			if (security.Currency != null)
				await WriteAttrAsync(writer, "currency", security.Currency.Value, t);

			if (security.SecurityType != null)
				await WriteAttrAsync(writer, "type", security.SecurityType.Value, t);
			
			if (!security.CfiCode.IsEmpty())
				await WriteAttrAsync(writer, "cfiCode", security.CfiCode, t);
			
			if (security.Shortable != null)
				await WriteAttrAsync(writer, "shortable", security.Shortable.Value, t);

			if (security.OptionType != null)
				await WriteAttrAsync(writer, "optionType", security.OptionType.Value, t);

			if (security.Strike != null)
				await WriteAttrAsync(writer, "strike", security.Strike.Value, t);

			if (!security.BinaryOptionType.IsEmpty())
				await WriteAttrAsync(writer, "binaryOptionType", security.BinaryOptionType, t);

			if (security.IssueSize != null)
				await WriteAttrAsync(writer, "issueSize", security.IssueSize.Value, t);

			if (security.IssueDate != null)
				await WriteAttrAsync(writer, "issueDate", security.IssueDate.Value, t);

			if (!security.GetUnderlyingCode().IsEmpty())
				await WriteAttrAsync(writer, "underlyingId", security.GetUnderlyingCode(), t);

			if (security.UnderlyingSecurityType != null)
				await WriteAttrAsync(writer, "underlyingType", security.UnderlyingSecurityType, t);

			if (security.UnderlyingSecurityMinVolume != null)
				await WriteAttrAsync(writer, "underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value, t);

			if (security.ExpiryDate != null)
				await WriteAttrAsync(writer, "expiryDate", security.ExpiryDate.Value.ToString("yyyy-MM-dd"), t);

			if (security.SettlementDate != null)
				await WriteAttrAsync(writer, "settlementDate", security.SettlementDate.Value.ToString("yyyy-MM-dd"), t);

			if (!security.BasketCode.IsEmpty())
				await WriteAttrAsync(writer, "basketCode", security.BasketCode, t);

			if (!security.BasketExpression.IsEmpty())
				await WriteAttrAsync(writer, "basketExpression", security.BasketExpression, t);

			if (security.FaceValue != null)
				await WriteAttrAsync(writer, "faceValue", security.FaceValue.Value, t);

			if (security.SettlementType != null)
				await WriteAttrAsync(writer, "settlementType", security.SettlementType.Value, t);

			if (security.OptionStyle != null)
				await WriteAttrAsync(writer, "optionStyle", security.OptionStyle.Value, t);

			if (!security.PrimaryId.SecurityCode.IsEmpty())
				await WriteAttrAsync(writer, "primaryCode", security.PrimaryId.SecurityCode, t);

			if (!security.PrimaryId.BoardCode.IsEmpty())
				await WriteAttrAsync(writer, "primaryBoard", security.PrimaryId.BoardCode, t);

			if (!security.SecurityId.Bloomberg.IsEmpty())
				await WriteAttrAsync(writer, "bloomberg", security.SecurityId.Bloomberg, t);

			if (!security.SecurityId.Cusip.IsEmpty())
				await WriteAttrAsync(writer, "cusip", security.SecurityId.Cusip, t);

			if (!security.SecurityId.IQFeed.IsEmpty())
				await WriteAttrAsync(writer, "iqfeed", security.SecurityId.IQFeed, t);

			if (security.SecurityId.InteractiveBrokers != null)
				await WriteAttrAsync(writer, "ib", security.SecurityId.InteractiveBrokers, t);

			if (!security.SecurityId.Isin.IsEmpty())
				await WriteAttrAsync(writer, "isin", security.SecurityId.Isin, t);

			if (!security.SecurityId.Plaza.IsEmpty())
				await WriteAttrAsync(writer, "plaza", security.SecurityId.Plaza, t);

			if (!security.SecurityId.Ric.IsEmpty())
				await WriteAttrAsync(writer, "ric", security.SecurityId.Ric, t);

			if (!security.SecurityId.Sedol.IsEmpty())
				await WriteAttrAsync(writer, "sedol", security.SecurityId.Sedol, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "boardStates", async (writer, msg, t) =>
		{
			await writer.WriteStartElementAsync(null, "boardState", null);

			await WriteAttrAsync(writer, "serverTime", msg.ServerTime.ToString(_timeFormat), t);
			await WriteAttrAsync(writer, "boardCode", msg.BoardCode, t);
			await WriteAttrAsync(writer, "state", msg.State.ToString(), t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTimeOffset?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "boards", async (writer, msg, t) =>
		{
			await writer.WriteStartElementAsync(null, "board", null);

			await WriteAttrAsync(writer, "code", msg.Code, t);
			await WriteAttrAsync(writer, "exchangeCode", msg.ExchangeCode, t);
			await WriteAttrAsync(writer, "expiryTime", msg.ExpiryTime.ToString(), t);
			await WriteAttrAsync(writer, "timeZone", msg.TimeZone?.Id, t);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	private async Task<(int, DateTimeOffset?)> Do<TValue>(IEnumerable<TValue> values, string rootElem, Func<XmlWriter, TValue, CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		var count = 0;
		var lastTime = default(DateTimeOffset?);

		var settings = new XmlWriterSettings
		{
			Indent = Indent,
			CloseOutput = false,
			Async = true
		};

		using (var writer = XmlWriter.Create(new StreamWriter(stream, Encoding, leaveOpen: true), settings))
		{
			await writer.WriteStartElementAsync(null, rootElem, null);

			foreach (var value in values)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await action(writer, value, cancellationToken);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			await writer.WriteEndElementAsync();
		}

		return (count, lastTime);
	}
}