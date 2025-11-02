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

	private static Task WriteAttrAsync(XmlWriter writer, string name, object value)
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
	protected override Task<(int, DateTime?)> ExportOrderLog(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "orderLog", async (writer, item) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			await WriteAttrAsync(writer, "id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
			await WriteAttrAsync(writer, "serverTime", item.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", item.LocalTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "price", item.OrderPrice);
			await WriteAttrAsync(writer, "volume", item.OrderVolume);
			await WriteAttrAsync(writer, "side", item.Side);
			await WriteAttrAsync(writer, "state", item.OrderState);
			await WriteAttrAsync(writer, "timeInForce", item.TimeInForce);
			await WriteAttrAsync(writer, "isSystem", item.IsSystem);

			if (item.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", item.SeqNum);

			if (item.TradePrice != null)
			{
				await WriteAttrAsync(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
				await WriteAttrAsync(writer, "tradePrice", item.TradePrice);

				if (item.OpenInterest != null)
					await WriteAttrAsync(writer, "openInterest", item.OpenInterest.Value);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTicks(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "ticks", async (writer, trade) =>
		{
			await writer.WriteStartElementAsync(null, "trade", null);

			await WriteAttrAsync(writer, "id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>());
			await WriteAttrAsync(writer, "serverTime", trade.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", trade.LocalTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "price", trade.TradePrice);
			await WriteAttrAsync(writer, "volume", trade.TradeVolume);

			if (trade.OriginSide != null)
				await WriteAttrAsync(writer, "originSide", trade.OriginSide.Value);

			if (trade.OpenInterest != null)
				await WriteAttrAsync(writer, "openInterest", trade.OpenInterest.Value);

			if (trade.IsUpTick != null)
				await WriteAttrAsync(writer, "isUpTick", trade.IsUpTick.Value);

			if (trade.Currency != null)
				await WriteAttrAsync(writer, "currency", trade.Currency.Value);

			if (trade.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", trade.SeqNum);

			if (trade.Yield != default)
				await WriteAttrAsync(writer, "yield", trade.Yield);

			if (trade.OrderBuyId != default)
				await WriteAttrAsync(writer, "buy", trade.OrderBuyId);

			if (trade.OrderSellId != default)
				await WriteAttrAsync(writer, "sell", trade.OrderSellId);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> ExportTransactions(IEnumerable<ExecutionMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "transactions", async (writer, item) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			await WriteAttrAsync(writer, "serverTime", item.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", item.LocalTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "portfolio", item.PortfolioName);
			await WriteAttrAsync(writer, "clientCode", item.ClientCode);
			await WriteAttrAsync(writer, "brokerCode", item.BrokerCode);
			await WriteAttrAsync(writer, "depoName", item.DepoName);
			await WriteAttrAsync(writer, "transactionId", item.TransactionId);
			await WriteAttrAsync(writer, "originalTransactionId", item.OriginalTransactionId);
			await WriteAttrAsync(writer, "orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
			await WriteAttrAsync(writer, "orderPrice", item.OrderPrice);
			await WriteAttrAsync(writer, "orderVolume", item.OrderVolume);
			await WriteAttrAsync(writer, "orderType", item.OrderType);
			await WriteAttrAsync(writer, "orderState", item.OrderState);
			await WriteAttrAsync(writer, "orderStatus", item.OrderStatus);
			await WriteAttrAsync(writer, "visibleVolume", item.VisibleVolume);
			await WriteAttrAsync(writer, "balance", item.Balance);
			await WriteAttrAsync(writer, "side", item.Side);
			await WriteAttrAsync(writer, "originSide", item.OriginSide);
			await WriteAttrAsync(writer, "tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
			await WriteAttrAsync(writer, "tradePrice", item.TradePrice);
			await WriteAttrAsync(writer, "tradeVolume", item.TradeVolume);
			await WriteAttrAsync(writer, "tradeStatus", item.TradeStatus);
			await WriteAttrAsync(writer, "isOrder", item.HasOrderInfo);
			await WriteAttrAsync(writer, "commission", item.Commission);
			await WriteAttrAsync(writer, "commissionCurrency", item.CommissionCurrency);
			await WriteAttrAsync(writer, "pnl", item.PnL);
			await WriteAttrAsync(writer, "position", item.Position);
			await WriteAttrAsync(writer, "latency", item.Latency);
			await WriteAttrAsync(writer, "slippage", item.Slippage);
			await WriteAttrAsync(writer, "error", item.Error?.Message);
			await WriteAttrAsync(writer, "openInterest", item.OpenInterest);
			await WriteAttrAsync(writer, "isCancelled", item.IsCancellation);
			await WriteAttrAsync(writer, "isSystem", item.IsSystem);
			await WriteAttrAsync(writer, "isUpTick", item.IsUpTick);
			await WriteAttrAsync(writer, "userOrderId", item.UserOrderId);
			await WriteAttrAsync(writer, "strategyId", item.StrategyId);
			await WriteAttrAsync(writer, "currency", item.Currency);
			await WriteAttrAsync(writer, "marginMode", item.MarginMode);
			await WriteAttrAsync(writer, "isMarketMaker", item.IsMarketMaker);
			await WriteAttrAsync(writer, "isManual", item.IsManual);
			await WriteAttrAsync(writer, "averagePrice", item.AveragePrice);
			await WriteAttrAsync(writer, "yield", item.Yield);
			await WriteAttrAsync(writer, "minVolume", item.MinVolume);
			await WriteAttrAsync(writer, "positionEffect", item.PositionEffect);
			await WriteAttrAsync(writer, "postOnly", item.PostOnly);
			await WriteAttrAsync(writer, "initiator", item.Initiator);
			await WriteAttrAsync(writer, "seqNum", item.SeqNum);
			await WriteAttrAsync(writer, "leverage", item.Leverage);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<QuoteChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "depths", async (writer, depth) =>
		{
			await writer.WriteStartElementAsync(null, "depth", null);

			await WriteAttrAsync(writer, "serverTime", depth.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", depth.LocalTime.ToString(_timeFormat));

			if (depth.State != null)
				await WriteAttrAsync(writer, "state", depth.State.Value);

			if (depth.HasPositions)
				await WriteAttrAsync(writer, "pos", true);

			if (depth.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", depth.SeqNum);

			var bids = new HashSet<QuoteChange>(depth.Bids);

			foreach (var quote in depth.Bids.Concat(depth.Asks).OrderByDescending(q => q.Price))
			{
				await writer.WriteStartElementAsync(null, "quote", null);

				await WriteAttrAsync(writer, "price", quote.Price);
				await WriteAttrAsync(writer, "volume", quote.Volume);
				await WriteAttrAsync(writer, "side", bids.Contains(quote) ? Sides.Buy : Sides.Sell);

				if (quote.OrdersCount != default)
					await WriteAttrAsync(writer, "ordersCount", quote.OrdersCount.Value);

				if (quote.StartPosition != default)
					await WriteAttrAsync(writer, "startPos", quote.StartPosition.Value);

				if (quote.EndPosition != default)
					await WriteAttrAsync(writer, "endPos", quote.EndPosition.Value);

				if (quote.Action != default)
					await WriteAttrAsync(writer, "action", quote.Action.Value);

				if (quote.Condition != default)
					await WriteAttrAsync(writer, "condition", quote.Condition);

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<Level1ChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "level1", async (writer, message) =>
		{
			await writer.WriteStartElementAsync(null, "change", null);

			await WriteAttrAsync(writer, "serverTime", message.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", message.LocalTime.ToString(_timeFormat));

			if (message.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", message.SeqNum);

			foreach (var pair in message.Changes)
			{
				var val = (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value;
				await WriteAttrAsync(writer, pair.Key.ToString(), val);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<PositionChangeMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "positions", async (writer, message) =>
		{
			await writer.WriteStartElementAsync(null, "change", null);

			await WriteAttrAsync(writer, "serverTime", message.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", message.LocalTime.ToString(_timeFormat));

			await WriteAttrAsync(writer, "portfolio", message.PortfolioName);
			await WriteAttrAsync(writer, "clientCode", message.ClientCode);
			await WriteAttrAsync(writer, "depoName", message.DepoName);
			await WriteAttrAsync(writer, "limit", message.LimitType);
			await WriteAttrAsync(writer, "strategyId", message.StrategyId);
			await WriteAttrAsync(writer, "side", message.Side);

			foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
			{
				var val = (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value;
				await WriteAttrAsync(writer, pair.Key.ToString(), val);
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<IndicatorValue> values, CancellationToken cancellationToken)
	{
		return Do(values, "values", async (writer, value) =>
		{
			await writer.WriteStartElementAsync(null, "value", null);

			await WriteAttrAsync(writer, "time", value.Time.ToString(_timeFormat));

			var index =1;
			foreach (var indVal in value.ValuesAsDecimal)
				await WriteAttrAsync(writer, $"value{index++}", indVal);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<CandleMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "candles", async (writer, candle) =>
		{
			await writer.WriteStartElementAsync(null, "candle", null);

			await WriteAttrAsync(writer, "openTime", candle.OpenTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "closeTime", candle.CloseTime.ToString(_timeFormat));

			await WriteAttrAsync(writer, "O", candle.OpenPrice);
			await WriteAttrAsync(writer, "H", candle.HighPrice);
			await WriteAttrAsync(writer, "L", candle.LowPrice);
			await WriteAttrAsync(writer, "C", candle.ClosePrice);
			await WriteAttrAsync(writer, "V", candle.TotalVolume);

			if (candle.OpenInterest != null)
				await WriteAttrAsync(writer, "openInterest", candle.OpenInterest.Value);

			if (candle.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", candle.SeqNum);

			if (candle.PriceLevels != null)
			{
				await writer.WriteStartElementAsync(null, "levels", null);

				foreach (var level in candle.PriceLevels)
				{
					await writer.WriteStartElementAsync(null, "level", null);

					await WriteAttrAsync(writer, "price", level.Price);
					await WriteAttrAsync(writer, "buyCount", level.BuyCount);
					await WriteAttrAsync(writer, "sellCount", level.SellCount);
					await WriteAttrAsync(writer, "buyVolume", level.BuyVolume);
					await WriteAttrAsync(writer, "sellVolume", level.SellVolume);
					await WriteAttrAsync(writer, "volume", level.TotalVolume);

					await writer.WriteEndElementAsync();
				}

				await writer.WriteEndElementAsync();
			}

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<NewsMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "news", async (writer, n) =>
		{
			await writer.WriteStartElementAsync(null, "item", null);

			if (!n.Id.IsEmpty())
				await WriteAttrAsync(writer, "id", n.Id);

			await WriteAttrAsync(writer, "serverTime", n.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "localTime", n.LocalTime.ToString(_timeFormat));

			if (n.SecurityId != null)
				await WriteAttrAsync(writer, "securityCode", n.SecurityId.Value.SecurityCode);

			if (!n.BoardCode.IsEmpty())
				await WriteAttrAsync(writer, "boardCode", n.BoardCode);

			await WriteAttrAsync(writer, "headline", n.Headline);

			if (!n.Source.IsEmpty())
				await WriteAttrAsync(writer, "source", n.Source);

			if (!n.Url.IsEmpty())
				await WriteAttrAsync(writer, "url", n.Url);

			if (n.Priority != null)
				await WriteAttrAsync(writer, "priority", n.Priority.Value);

			if (!n.Language.IsEmpty())
				await WriteAttrAsync(writer, "language", n.Language);

			if (n.ExpiryDate != null)
				await WriteAttrAsync(writer, "expiry", n.ExpiryDate.Value);

			if (!n.Story.IsEmpty())
				await writer.WriteCDataAsync(n.Story);

			if (n.SeqNum != default)
				await WriteAttrAsync(writer, "seqNum", n.SeqNum);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<SecurityMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "securities", async (writer, security) =>
		{
			await writer.WriteStartElementAsync(null, "security", null);

			await WriteAttrAsync(writer, "code", security.SecurityId.SecurityCode);
			await WriteAttrAsync(writer, "board", security.SecurityId.BoardCode);

			if (!security.Name.IsEmpty())
				await WriteAttrAsync(writer, "name", security.Name);

			if (!security.ShortName.IsEmpty())
				await WriteAttrAsync(writer, "shortName", security.ShortName);

			if (security.PriceStep != null)
				await WriteAttrAsync(writer, "priceStep", security.PriceStep.Value);

			if (security.VolumeStep != null)
				await WriteAttrAsync(writer, "volumeStep", security.VolumeStep.Value);

			if (security.MinVolume != null)
				await WriteAttrAsync(writer, "minVolume", security.MinVolume.Value);

			if (security.MaxVolume != null)
				await WriteAttrAsync(writer, "maxVolume", security.MaxVolume.Value);

			if (security.Multiplier != null)
				await WriteAttrAsync(writer, "multiplier", security.Multiplier.Value);

			if (security.Decimals != null)
				await WriteAttrAsync(writer, "decimals", security.Decimals.Value);

			if (security.Currency != null)
				await WriteAttrAsync(writer, "currency", security.Currency.Value);

			if (security.SecurityType != null)
				await WriteAttrAsync(writer, "type", security.SecurityType.Value);
			
			if (!security.CfiCode.IsEmpty())
				await WriteAttrAsync(writer, "cfiCode", security.CfiCode);
			
			if (security.Shortable != null)
				await WriteAttrAsync(writer, "shortable", security.Shortable.Value);

			if (security.OptionType != null)
				await WriteAttrAsync(writer, "optionType", security.OptionType.Value);

			if (security.Strike != null)
				await WriteAttrAsync(writer, "strike", security.Strike.Value);

			if (!security.BinaryOptionType.IsEmpty())
				await WriteAttrAsync(writer, "binaryOptionType", security.BinaryOptionType);

			if (security.IssueSize != null)
				await WriteAttrAsync(writer, "issueSize", security.IssueSize.Value);

			if (security.IssueDate != null)
				await WriteAttrAsync(writer, "issueDate", security.IssueDate.Value);

			if (!security.GetUnderlyingCode().IsEmpty())
				await WriteAttrAsync(writer, "underlyingId", security.GetUnderlyingCode());

			if (security.UnderlyingSecurityType != null)
				await WriteAttrAsync(writer, "underlyingType", security.UnderlyingSecurityType);

			if (security.UnderlyingSecurityMinVolume != null)
				await WriteAttrAsync(writer, "underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value);

			if (security.ExpiryDate != null)
				await WriteAttrAsync(writer, "expiryDate", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

			if (security.SettlementDate != null)
				await WriteAttrAsync(writer, "settlementDate", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

			if (!security.BasketCode.IsEmpty())
				await WriteAttrAsync(writer, "basketCode", security.BasketCode);

			if (!security.BasketExpression.IsEmpty())
				await WriteAttrAsync(writer, "basketExpression", security.BasketExpression);

			if (security.FaceValue != null)
				await WriteAttrAsync(writer, "faceValue", security.FaceValue.Value);

			if (security.SettlementType != null)
				await WriteAttrAsync(writer, "settlementType", security.SettlementType.Value);

			if (security.OptionStyle != null)
				await WriteAttrAsync(writer, "optionStyle", security.OptionStyle.Value);

			if (!security.PrimaryId.SecurityCode.IsEmpty())
				await WriteAttrAsync(writer, "primaryCode", security.PrimaryId.SecurityCode);

			if (!security.PrimaryId.BoardCode.IsEmpty())
				await WriteAttrAsync(writer, "primaryBoard", security.PrimaryId.BoardCode);

			if (!security.SecurityId.Bloomberg.IsEmpty())
				await WriteAttrAsync(writer, "bloomberg", security.SecurityId.Bloomberg);

			if (!security.SecurityId.Cusip.IsEmpty())
				await WriteAttrAsync(writer, "cusip", security.SecurityId.Cusip);

			if (!security.SecurityId.IQFeed.IsEmpty())
				await WriteAttrAsync(writer, "iqfeed", security.SecurityId.IQFeed);

			if (security.SecurityId.InteractiveBrokers != null)
				await WriteAttrAsync(writer, "ib", security.SecurityId.InteractiveBrokers);

			if (!security.SecurityId.Isin.IsEmpty())
				await WriteAttrAsync(writer, "isin", security.SecurityId.Isin);

			if (!security.SecurityId.Plaza.IsEmpty())
				await WriteAttrAsync(writer, "plaza", security.SecurityId.Plaza);

			if (!security.SecurityId.Ric.IsEmpty())
				await WriteAttrAsync(writer, "ric", security.SecurityId.Ric);

			if (!security.SecurityId.Sedol.IsEmpty())
				await WriteAttrAsync(writer, "sedol", security.SecurityId.Sedol);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardStateMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "boardStates", async (writer, msg) =>
		{
			await writer.WriteStartElementAsync(null, "boardState", null);

			await WriteAttrAsync(writer, "serverTime", msg.ServerTime.ToString(_timeFormat));
			await WriteAttrAsync(writer, "boardCode", msg.BoardCode);
			await WriteAttrAsync(writer, "state", msg.State.ToString());

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	/// <inheritdoc />
	protected override Task<(int, DateTime?)> Export(IEnumerable<BoardMessage> messages, CancellationToken cancellationToken)
	{
		return Do(messages, "boards", async (writer, msg) =>
		{
			await writer.WriteStartElementAsync(null, "board", null);

			await WriteAttrAsync(writer, "code", msg.Code);
			await WriteAttrAsync(writer, "exchangeCode", msg.ExchangeCode);
			await WriteAttrAsync(writer, "expiryTime", msg.ExpiryTime.ToString());
			await WriteAttrAsync(writer, "timeZone", msg.TimeZone?.Id);

			await writer.WriteEndElementAsync();
		}, cancellationToken);
	}

	private async Task<(int, DateTime?)> Do<TValue>(IEnumerable<TValue> values, string rootElem, Func<XmlWriter, TValue, Task> action, CancellationToken cancellationToken)
	{
		var count = 0;
		var lastTime = default(DateTime?);

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

				await action(writer, value);

				count++;

				if (value is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;
			}

			await writer.WriteEndElementAsync();
		}

		return (count, lastTime);
	}
}