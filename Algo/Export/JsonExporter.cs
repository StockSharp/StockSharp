namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Net;
	
	using Newtonsoft.Json;

	using StockSharp.Messages;

	/// <summary>
	/// The export into json.
	/// </summary>
	public class JsonExporter : BaseExporter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonExporter"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="fileName">The path to file.</param>
		public JsonExporter(DataType dataType, Func<int, bool> isCancelled, string fileName)
			: base(dataType, isCancelled, fileName)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether to indent elements.
		/// </summary>
		/// <remarks>
		/// By default is <see langword="true"/>.
		/// </remarks>
		public bool Indent { get; set; } = true;

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<QuoteChangeMessage> messages)
		{
			return Do(messages, (writer, depth) =>
			{
				writer
					.WriteProperty("s", depth.ServerTime.UtcDateTime)
					.WriteProperty("l", depth.LocalTime.UtcDateTime);

				if (depth.State != null)
					writer.WriteProperty("st", depth.State.Value);

				if (depth.HasPositions)
					writer.WriteProperty("pos", true);

				if (depth.SeqNum != default)
					writer.WriteProperty("sn", depth.SeqNum);

				void WriteQuotes(string name, QuoteChange[] quotes)
				{
					writer.WritePropertyName(name);

					writer.WriteStartArray();

					foreach (var quote in quotes)
					{
						writer.WriteStartObject();

						writer
							.WriteProperty("p", quote.Price)
							.WriteProperty("v", quote.Volume);

						if (quote.OrdersCount != default)
							writer.WriteProperty("cnt", quote.OrdersCount.Value);

						if (quote.StartPosition != default)
							writer.WriteProperty("s", quote.StartPosition.Value);

						if (quote.EndPosition != default)
							writer.WriteProperty("e", quote.EndPosition.Value);

						if (quote.Action != default)
							writer.WriteProperty("a", quote.Action.Value);

						if (quote.Condition != default)
							writer.WriteProperty("cond", quote.Condition);

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
				writer
					.WriteProperty("s", message.ServerTime.UtcDateTime)
					.WriteProperty("l", message.LocalTime.UtcDateTime);

				if (message.SeqNum != default)
					writer.WriteProperty("sn", message.SeqNum);

				foreach (var pair in message.Changes)
					writer.WriteProperty(pair.Key.ToString(), pair.Value);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<CandleMessage> messages)
		{
			return Do(messages, (writer, candle) =>
			{
				writer
					.WriteProperty("open", candle.OpenTime.UtcDateTime)
					.WriteProperty("close", candle.CloseTime.UtcDateTime)

					.WriteProperty("O", candle.OpenPrice)
					.WriteProperty("H", candle.HighPrice)
					.WriteProperty("L", candle.LowPrice)
					.WriteProperty("C", candle.ClosePrice)
					.WriteProperty("V", candle.TotalVolume);

				if (candle.OpenInterest != null)
					writer.WriteProperty("oi", candle.OpenInterest.Value);

				if (candle.SeqNum != default)
					writer.WriteProperty("sn", candle.SeqNum);

				if (candle.PriceLevels != null)
				{
					writer.WritePropertyName("levels");

					writer.WriteStartArray();

					foreach (var level in candle.PriceLevels)
					{
						writer.WriteStartObject();

						writer
							.WriteProperty("price", level.Price)
							.WriteProperty("buyCount", level.BuyCount)
							.WriteProperty("sellCount", level.SellCount)
							.WriteProperty("buyVolume", level.BuyVolume)
							.WriteProperty("sellVolume", level.SellVolume)
							.WriteProperty("volume", level.TotalVolume);

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
					writer.WriteProperty("id", n.Id);

				writer.WriteProperty("s", n.ServerTime.UtcDateTime);
				writer.WriteProperty("l", n.LocalTime.UtcDateTime);

				if (n.SecurityId != null)
					writer.WriteProperty("secCode", n.SecurityId.Value.SecurityCode);

				if (!n.BoardCode.IsEmpty())
					writer.WriteProperty("board", n.BoardCode);

				writer.WriteProperty("headline", n.Headline);

				if (!n.Source.IsEmpty())
					writer.WriteProperty("source", n.Source);

				if (!n.Url.IsEmpty())
					writer.WriteProperty("url", n.Url);

				if (n.Priority != null)
					writer.WriteProperty("priority", n.Priority.Value);

				if (!n.Language.IsEmpty())
					writer.WriteProperty("language", n.Language);

				if (n.ExpiryDate != null)
					writer.WriteProperty("expiry", n.ExpiryDate.Value);

				if (!n.Story.IsEmpty())
					writer.WriteProperty("story", n.Story);

				if (n.SeqNum != default)
					writer.WriteProperty("sn", n.SeqNum);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<SecurityMessage> messages)
		{
			return Do(messages, (writer, security) =>
			{
				writer.WriteProperty("code", security.SecurityId.SecurityCode);
				writer.WriteProperty("board", security.SecurityId.BoardCode);

				if (!security.Name.IsEmpty())
					writer.WriteProperty("name", security.Name);

				if (!security.ShortName.IsEmpty())
					writer.WriteProperty("shortName", security.ShortName);

				if (security.PriceStep != null)
					writer.WriteProperty("priceStep", security.PriceStep.Value);

				if (security.VolumeStep != null)
					writer.WriteProperty("volumeStep", security.VolumeStep.Value);

				if (security.MinVolume != null)
					writer.WriteProperty("minVolume", security.MinVolume.Value);

				if (security.MaxVolume != null)
					writer.WriteProperty("maxVolume", security.MaxVolume.Value);

				if (security.Multiplier != null)
					writer.WriteProperty("multiplier", security.Multiplier.Value);

				if (security.Decimals != null)
					writer.WriteProperty("decimals", security.Decimals.Value);

				if (security.Currency != null)
					writer.WriteProperty("currency", security.Currency.Value);

				if (security.SecurityType != null)
					writer.WriteProperty("type", security.SecurityType.Value);
				
				if (!security.CfiCode.IsEmpty())
					writer.WriteProperty("cfiCode", security.CfiCode);
				
				if (security.Shortable != null)
					writer.WriteProperty("shortable", security.Shortable.Value);

				if (security.OptionType != null)
					writer.WriteProperty("optionType", security.OptionType.Value);

				if (security.Strike != null)
					writer.WriteProperty("strike", security.Strike.Value);

				if (!security.BinaryOptionType.IsEmpty())
					writer.WriteProperty("binaryOptionType", security.BinaryOptionType);

				if (security.IssueSize != null)
					writer.WriteProperty("issueSize", security.IssueSize.Value);

				if (security.IssueDate != null)
					writer.WriteProperty("issueDate", security.IssueDate.Value);

				if (!security.UnderlyingSecurityCode.IsEmpty())
					writer.WriteProperty("underlyingCode", security.UnderlyingSecurityCode);

				if (security.UnderlyingSecurityType != null)
					writer.WriteProperty("underlyingType", security.UnderlyingSecurityType);

				if (security.UnderlyingSecurityMinVolume != null)
					writer.WriteProperty("underlyingMinVolume", security.UnderlyingSecurityMinVolume.Value);

				if (security.ExpiryDate != null)
					writer.WriteProperty("expiry", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

				if (security.SettlementDate != null)
					writer.WriteProperty("settlement", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

				if (!security.BasketCode.IsEmpty())
					writer.WriteProperty("basketCode", security.BasketCode);

				if (!security.BasketExpression.IsEmpty())
					writer.WriteProperty("basketExpression", security.BasketExpression);

				if (security.FaceValue != null)
					writer.WriteProperty("faceValue", security.FaceValue.Value);

				if (!security.PrimaryId.SecurityCode.IsEmpty())
					writer.WriteProperty("primaryCode", security.PrimaryId.SecurityCode);

				if (!security.PrimaryId.BoardCode.IsEmpty())
					writer.WriteProperty("primaryBoard", security.PrimaryId.BoardCode);

				if (!security.SecurityId.Bloomberg.IsEmpty())
					writer.WriteProperty("bloomberg", security.SecurityId.Bloomberg);

				if (!security.SecurityId.Cusip.IsEmpty())
					writer.WriteProperty("cusip", security.SecurityId.Cusip);

				if (!security.SecurityId.IQFeed.IsEmpty())
					writer.WriteProperty("iqfeed", security.SecurityId.IQFeed);

				if (security.SecurityId.InteractiveBrokers != null)
					writer.WriteProperty("ib", security.SecurityId.InteractiveBrokers);

				if (!security.SecurityId.Isin.IsEmpty())
					writer.WriteProperty("isin", security.SecurityId.Isin);

				if (!security.SecurityId.Plaza.IsEmpty())
					writer.WriteProperty("plaza", security.SecurityId.Plaza);

				if (!security.SecurityId.Ric.IsEmpty())
					writer.WriteProperty("ric", security.SecurityId.Ric);

				if (!security.SecurityId.Sedol.IsEmpty())
					writer.WriteProperty("sedol", security.SecurityId.Sedol);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<PositionChangeMessage> messages)
		{
			return Do(messages, (writer, message) =>
			{
				writer
					.WriteProperty("s", message.ServerTime.UtcDateTime)
					.WriteProperty("l", message.LocalTime.UtcDateTime)

					.WriteProperty("acc", message.PortfolioName)
					.WriteProperty("client", message.ClientCode)
					.WriteProperty("depo", message.DepoName)
					.WriteProperty("limit", message.LimitType)
					.WriteProperty("strategyId", message.StrategyId)
					.WriteProperty("side", message.Side)
					;

				foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
					writer.WriteProperty(pair.Key.ToString(), pair.Value);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) Export(IEnumerable<IndicatorValue> values)
		{
			return Do(values, (writer, value) =>
			{
				writer.WriteProperty("time", value.Time.UtcDateTime);

				var index = 1;
				foreach (var indVal in value.ValuesAsDecimal)
					writer.WriteProperty($"value{index++}", indVal);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportOrderLog(IEnumerable<ExecutionMessage> messages)
		{
			return Do(messages, (writer, item) =>
			{
				writer
					.WriteProperty("id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>())
					.WriteProperty("s", item.ServerTime.UtcDateTime)
					.WriteProperty("l", item.LocalTime.UtcDateTime)
					.WriteProperty("p", item.OrderPrice)
					.WriteProperty("v", item.OrderVolume)
					.WriteProperty("side", item.Side)
					.WriteProperty("state", item.OrderState)
					.WriteProperty("tif", item.TimeInForce)
					.WriteProperty("sys", item.IsSystem);

				if (item.SeqNum != default)
					writer.WriteProperty("sn", item.SeqNum);

				if (item.TradePrice != null)
				{
					writer.WriteProperty("tid", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
					writer.WriteProperty("tp", item.TradePrice);

					if (item.OpenInterest != null)
						writer.WriteProperty("oi", item.OpenInterest.Value);
				}
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportTicks(IEnumerable<ExecutionMessage> messages)
		{
			return Do(messages, (writer, trade) =>
			{
				writer
					.WriteProperty("id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>())
					.WriteProperty("s", trade.ServerTime.UtcDateTime)
					.WriteProperty("l", trade.LocalTime.UtcDateTime)
					.WriteProperty("p", trade.TradePrice)
					.WriteProperty("v", trade.TradeVolume);

				if (trade.OriginSide != null)
					writer.WriteProperty("side", trade.OriginSide.Value);

				if (trade.OpenInterest != null)
					writer.WriteProperty("oi", trade.OpenInterest.Value);

				if (trade.IsUpTick != null)
					writer.WriteProperty("up", trade.IsUpTick.Value);

				if (trade.Currency != null)
					writer.WriteProperty("cur", trade.Currency.Value);

				if (trade.SeqNum != default)
					writer.WriteProperty("sn", trade.SeqNum);

				if (trade.Yield != default)
					writer.WriteProperty("yield", trade.Yield);

				if (trade.OrderBuyId != default)
					writer.WriteProperty("buy", trade.OrderBuyId);

				if (trade.OrderSellId != default)
					writer.WriteProperty("sell", trade.OrderSellId);
			});
		}

		/// <inheritdoc />
		protected override (int, DateTimeOffset?) ExportTransactions(IEnumerable<ExecutionMessage> messages)
		{
			return Do(messages, (writer, item) =>
			{
				writer
					.WriteProperty("s", item.ServerTime.UtcDateTime)
					.WriteProperty("l", item.LocalTime.UtcDateTime)
					.WriteProperty("acc", item.PortfolioName)
					.WriteProperty("client", item.ClientCode)
					.WriteProperty("broker", item.BrokerCode)
					.WriteProperty("depo", item.DepoName)
					.WriteProperty("transactionId", item.TransactionId)
					.WriteProperty("originalTransactionId", item.OriginalTransactionId)
					.WriteProperty("orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>())
					.WriteProperty("orderPrice", item.OrderPrice)
					.WriteProperty("orderVolume", item.OrderVolume)
					.WriteProperty("orderType", item.OrderType)
					.WriteProperty("orderState", item.OrderState)
					.WriteProperty("orderStatus", item.OrderStatus)
					.WriteProperty("visibleVolume", item.VisibleVolume)
					.WriteProperty("balance", item.Balance)
					.WriteProperty("side", item.Side)
					.WriteProperty("originSide", item.OriginSide)
					.WriteProperty("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>())
					.WriteProperty("tradePrice", item.TradePrice)
					.WriteProperty("tradeVolume", item.TradeVolume)
					.WriteProperty("tradeStatus", item.TradeStatus)
					.WriteProperty("isOrder", item.HasOrderInfo)
					.WriteProperty("isTrade", item.HasTradeInfo)
					.WriteProperty("commission", item.Commission)
					.WriteProperty("commissionCurrency", item.CommissionCurrency)
					.WriteProperty("pnl", item.PnL)
					.WriteProperty("position", item.Position)
					.WriteProperty("latency", item.Latency)
					.WriteProperty("slippage", item.Slippage)
					.WriteProperty("error", item.Error?.Message)
					.WriteProperty("openInterest", item.OpenInterest)
					.WriteProperty("isCancelled", item.IsCancellation)
					.WriteProperty("isSystem", item.IsSystem)
					.WriteProperty("isUpTick", item.IsUpTick)
					.WriteProperty("userOrderId", item.UserOrderId)
					.WriteProperty("strategyId", item.StrategyId)
					.WriteProperty("currency", item.Currency)
					.WriteProperty("isMargin", item.IsMargin)
					.WriteProperty("isMarketMaker", item.IsMarketMaker)
					.WriteProperty("isManual", item.IsManual)
					.WriteProperty("averagePrice", item.AveragePrice)
					.WriteProperty("yield", item.Yield)
					.WriteProperty("minVolume", item.MinVolume)
					.WriteProperty("positionEffect", item.PositionEffect)
					.WriteProperty("postOnly", item.PostOnly)
					.WriteProperty("initiator", item.Initiator)
					.WriteProperty("sn", item.SeqNum)
					.WriteProperty("leverage", item.Leverage);
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
}