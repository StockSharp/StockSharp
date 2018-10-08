#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Algo
File: XmlExporter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The export into xml.
	/// </summary>
	public class XmlExporter : BaseExporter
	{
		private const string _timeFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlExporter"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="arg">The data parameter.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <param name="fileName">The path to file.</param>
		public XmlExporter(Security security, object arg, Func<int, bool> isCancelled, string fileName)
			: base(security, arg, isCancelled, fileName)
		{
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<ExecutionMessage> messages)
		{
			switch ((ExecutionTypes)Arg)
			{
				case ExecutionTypes.Tick:
				{
					Do(messages, "ticks", (writer, trade) =>
					{
						writer.WriteStartElement("trade");

						writer.WriteAttribute("id", trade.TradeId == null ? trade.TradeStringId : trade.TradeId.To<string>());
						writer.WriteAttribute("serverTime", trade.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", trade.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("price", trade.TradePrice);
						writer.WriteAttribute("volume", trade.TradeVolume);

						if (trade.OriginSide != null)
							writer.WriteAttribute("originSide", trade.OriginSide.Value);

						if (trade.OpenInterest != null)
							writer.WriteAttribute("openInterest", trade.OpenInterest.Value);

						if (trade.IsUpTick != null)
							writer.WriteAttribute("isUpTick", trade.IsUpTick.Value);

						writer.WriteEndElement();
					});

					break;
				}
				case ExecutionTypes.OrderLog:
				{
					Do(messages, "orderLog", (writer, item) =>
					{
						writer.WriteStartElement("item");

						writer.WriteAttribute("id", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
						writer.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("price", item.OrderPrice);
						writer.WriteAttribute("volume", item.OrderVolume);
						writer.WriteAttribute("side", item.Side);
						writer.WriteAttribute("state", item.OrderState);
						writer.WriteAttribute("timeInForce", item.TimeInForce);
						writer.WriteAttribute("isSystem", item.IsSystem);

						if (item.TradePrice != null)
						{
							writer.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
							writer.WriteAttribute("tradePrice", item.TradePrice);

							if (item.OpenInterest != null)
								writer.WriteAttribute("openInterest", item.OpenInterest.Value);
						}

						writer.WriteEndElement();
					});

					break;
				}
				case ExecutionTypes.Transaction:
				{
					Do(messages, "transactions", (writer, item) =>
					{
						writer.WriteStartElement("item");

						writer.WriteAttribute("serverTime", item.ServerTime.ToString(_timeFormat));
						writer.WriteAttribute("localTime", item.LocalTime.ToString(_timeFormat));
						writer.WriteAttribute("portfolio", item.PortfolioName);
						writer.WriteAttribute("clientCode", item.ClientCode);
						writer.WriteAttribute("brokerCode", item.BrokerCode);
						writer.WriteAttribute("depoName", item.DepoName);
						writer.WriteAttribute("transactionId", item.TransactionId);
						writer.WriteAttribute("originalTransactionId", item.OriginalTransactionId);
						writer.WriteAttribute("orderId", item.OrderId == null ? item.OrderStringId : item.OrderId.To<string>());
						//writer.WriteAttribute("derivedOrderId", item.DerivedOrderId == null ? item.DerivedOrderStringId : item.DerivedOrderId.To<string>());
						writer.WriteAttribute("orderPrice", item.OrderPrice);
						writer.WriteAttribute("orderVolume", item.OrderVolume);
						writer.WriteAttribute("orderType", item.OrderType);
						writer.WriteAttribute("orderState", item.OrderState);
						writer.WriteAttribute("orderStatus", item.OrderStatus);
						writer.WriteAttribute("visibleVolume", item.VisibleVolume);
						writer.WriteAttribute("balance", item.Balance);
						writer.WriteAttribute("side", item.Side);
						writer.WriteAttribute("originSide", item.OriginSide);
						writer.WriteAttribute("tradeId", item.TradeId == null ? item.TradeStringId : item.TradeId.To<string>());
						writer.WriteAttribute("tradePrice", item.TradePrice);
						writer.WriteAttribute("tradeVolume", item.TradeVolume);
						writer.WriteAttribute("tradeStatus", item.TradeStatus);
						writer.WriteAttribute("isOrder", item.HasOrderInfo);
						writer.WriteAttribute("isTrade", item.HasTradeInfo);
						writer.WriteAttribute("commission", item.Commission);
						writer.WriteAttribute("commissionCurrency", item.CommissionCurrency);
						writer.WriteAttribute("pnl", item.PnL);
						writer.WriteAttribute("position", item.Position);
						writer.WriteAttribute("latency", item.Latency);
						writer.WriteAttribute("slippage", item.Slippage);
						writer.WriteAttribute("error", item.Error?.Message);
						writer.WriteAttribute("currency", item.Currency);
						writer.WriteAttribute("openInterest", item.OpenInterest);
						writer.WriteAttribute("isCancelled", item.IsCancelled);
						writer.WriteAttribute("isSystem", item.IsSystem);
						writer.WriteAttribute("isUpTick", item.IsUpTick);
						writer.WriteAttribute("isMargin", item.IsMargin);
						writer.WriteAttribute("isMarketMaker", item.IsMarketMaker);

						writer.WriteEndElement();
					});

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<QuoteChangeMessage> messages)
		{
			Do(messages, "depths", (writer, depth) =>
			{
				writer.WriteStartElement("depth");

				writer.WriteAttribute("serverTime", depth.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", depth.LocalTime.ToString(_timeFormat));

				foreach (var quote in depth.Bids.Concat(depth.Asks).OrderByDescending(q => q.Price))
				{
					writer.WriteStartElement("quote");

					writer.WriteAttribute("price", quote.Price);
					writer.WriteAttribute("volume", quote.Volume);
					writer.WriteAttribute("side", quote.Side);

					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			});
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<Level1ChangeMessage> messages)
		{
			Do(messages, "level1", (writer, message) =>
			{
				writer.WriteStartElement("change");

				writer.WriteAttribute("serverTime", message.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", message.LocalTime.ToString(_timeFormat));

				foreach (var pair in message.Changes)
					writer.WriteAttribute(pair.Key.ToString(), (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value);

				writer.WriteEndElement();
			});
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<PositionChangeMessage> messages)
		{
			Do(messages, "positions", (writer, message) =>
			{
				writer.WriteStartElement("change");

				writer.WriteAttribute("serverTime", message.ServerTime.ToString(_timeFormat));
				writer.WriteAttribute("localTime", message.LocalTime.ToString(_timeFormat));

				writer.WriteAttribute("portfolio", message.PortfolioName);
				writer.WriteAttribute("clientCode", message.ClientCode);
				writer.WriteAttribute("depoName", message.DepoName);
				writer.WriteAttribute("limit", message.LimitType);

				foreach (var pair in message.Changes.Where(c => !c.Key.IsObsolete()))
					writer.WriteAttribute(pair.Key.ToString(), (pair.Value as DateTime?)?.ToString(_timeFormat) ?? pair.Value);

				writer.WriteEndElement();
			});
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<IndicatorValue> values)
		{
			Do(values, "values", (writer, value) =>
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
		protected override void Export(IEnumerable<CandleMessage> messages)
		{
			Do(messages, "candles", (writer, candle) =>
			{
				writer.WriteStartElement("candle");

				writer.WriteAttribute("openTime", candle.OpenTime.ToString(_timeFormat));
				writer.WriteAttribute("closeTime", candle.CloseTime.ToString(_timeFormat));

				writer.WriteAttribute("O", candle.OpenPrice);
				writer.WriteAttribute("H", candle.HighPrice);
				writer.WriteAttribute("L", candle.LowPrice);
				writer.WriteAttribute("C", candle.ClosePrice);
				writer.WriteAttribute("V", candle.TotalVolume);

				if (candle.OpenInterest != null)
					writer.WriteAttribute("openInterest", candle.OpenInterest.Value);

				writer.WriteEndElement();
			});
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<NewsMessage> messages)
		{
			Do(messages, "news", (writer, n) =>
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

				if (n.Url != null)
					writer.WriteAttribute("board", n.Url);

				if (!n.Story.IsEmpty())
					writer.WriteCData(n.Story);

				writer.WriteEndElement();
			});
		}

		/// <inheritdoc />
		protected override void Export(IEnumerable<SecurityMessage> messages)
		{
			Do(messages, "securities", (writer, security) =>
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

				if (!security.UnderlyingSecurityCode.IsEmpty())
					writer.WriteAttribute("underlyingSecurityCode", security.UnderlyingSecurityCode);

				if (security.UnderlyingSecurityType != null)
					writer.WriteAttribute("underlyingSecurityType", security.UnderlyingSecurityType);

				if (security.ExpiryDate != null)
					writer.WriteAttribute("expiryDate", security.ExpiryDate.Value.ToString("yyyy-MM-dd"));

				if (security.SettlementDate != null)
					writer.WriteAttribute("settlementDate", security.SettlementDate.Value.ToString("yyyy-MM-dd"));

				if (!security.BasketCode.IsEmpty())
					writer.WriteAttribute("basketCode", security.BasketCode);

				if (!security.BasketExpression.IsEmpty())
					writer.WriteAttribute("basketExpression", security.BasketExpression);

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

		private void Do<TValue>(IEnumerable<TValue> values, string rootElem, Action<XmlWriter, TValue> action)
		{
			using (var writer = XmlWriter.Create(Path, new XmlWriterSettings { Indent = true }))
			{
				writer.WriteStartElement(rootElem);

				foreach (var value in values)
				{
					if (!CanProcess())
						break;

					action(writer, value);
				}

				writer.WriteEndElement();
			}
		}
	}
}