#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: OrderLogBinarySerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class OrderLogMetaInfo : BinaryMetaInfo
	{
		public OrderLogMetaInfo(DateTime date)
			: base(date)
		{
			FirstOrderId = -1;
			Portfolios = new List<string>();
		}

		public override object LastId => LastTransactionId;

		public long FirstOrderId { get; set; }
		public long LastOrderId { get; set; }

		public long FirstTradeId { get; set; }
		public long LastTradeId { get; set; }

		public long FirstTransactionId { get; set; }
		public long LastTransactionId { get; set; }

		public decimal FirstOrderPrice { get; set; }
		public decimal LastOrderPrice { get; set; }

		public IList<string> Portfolios { get; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstOrderId);
			stream.Write(FirstTradeId);
			stream.Write(LastOrderId);
			stream.Write(LastTradeId);
			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			if (Version < MarketDataVersions.Version34)
				return;

			stream.Write(FirstTransactionId);
			stream.Write(LastTransactionId);

			if (Version < MarketDataVersions.Version40)
				return;

			stream.Write(Portfolios.Count);

			foreach (var portfolio in Portfolios)
				stream.Write(portfolio);

			WriteFractionalPrice(stream);
			WriteFractionalVolume(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			stream.Write(FirstOrderPrice);
			stream.Write(LastOrderPrice);

			WriteLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version48)
				return;

			stream.Write(ServerOffset);

			if (Version < MarketDataVersions.Version52)
				return;

			WriteOffsets(stream);
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

	class OrderLogBinarySerializer : BinaryMarketDataSerializer<ExecutionMessage, OrderLogMetaInfo>
	{
		public OrderLogBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 200, MarketDataVersions.Version53, exchangeInfoProvider)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, OrderLogMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty() && !messages.IsEmpty())
			{
				var item = messages.First();

				metaInfo.FirstOrderId = metaInfo.LastOrderId = item.SafeGetOrderId();
				metaInfo.FirstTransactionId = metaInfo.LastTransactionId = item.TransactionId;
				metaInfo.ServerOffset = item.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;

			foreach (var message in messages)
			{
				var hasTrade = message.TradeId != null || message.TradePrice != null;

				var orderId = message.SafeGetOrderId();
				if (orderId < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), orderId, LocalizedStrings.Str925);

				if (message.ExecutionType != ExecutionTypes.OrderLog)
					throw new ArgumentOutOfRangeException(nameof(messages), message.ExecutionType, LocalizedStrings.Str1695Params.Put(message));

				// sell market orders has zero price (if security do not have min allowed price)
				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (item.Price < 0)
				//	throw new ArgumentOutOfRangeException(nameof(messages), item.Price, LocalizedStrings.Str926Params.Put(item.OrderId));

				var volume = message.SafeGetVolume();
				if (volume <= 0 && message.OrderState != OrderStates.Done)
					throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.Str927Params.Put(message.OrderId));

				long? tradeId = null;

				if (hasTrade)
				{
					tradeId = message.GetTradeId();

					if (tradeId <= 0)
						throw new ArgumentOutOfRangeException(nameof(messages), tradeId, LocalizedStrings.Str1012Params.Put(message.OrderId));

					// execution ticks (like option execution) may be a zero cost
					// ticks for spreads may be a zero cost or less than zero
					//if (item.TradePrice <= 0)
					//	throw new ArgumentOutOfRangeException(nameof(messages), item.TradePrice, LocalizedStrings.Str929Params.Put(item.TradeId, item.OrderId));
				}

				metaInfo.LastOrderId = writer.SerializeId(orderId, metaInfo.LastOrderId);

				var orderPrice = message.OrderPrice;

				if (metaInfo.Version < MarketDataVersions.Version45)
					writer.WritePriceEx(orderPrice, metaInfo, SecurityId);
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
						if (metaInfo.FirstFractionalPrice == 0)
							metaInfo.FirstFractionalPrice = metaInfo.LastFractionalPrice = orderPrice;

						metaInfo.LastFractionalPrice = writer.WriteDecimal(orderPrice, metaInfo.LastFractionalPrice);
					}
				}

				writer.WriteVolume(volume, metaInfo, SecurityId);

				writer.Write(message.Side == Sides.Buy);

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, LocalizedStrings.Str1013, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				if (hasTrade)
				{
					writer.Write(true);

					if (metaInfo.FirstTradeId == 0)
					{
						metaInfo.FirstTradeId = metaInfo.LastTradeId = tradeId.Value;
					}

					metaInfo.LastTradeId = writer.SerializeId(tradeId.Value, metaInfo.LastTradeId);

					writer.WritePriceEx(message.GetTradePrice(), metaInfo, SecurityId);
				}
				else
				{
					writer.Write(false);
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
				var isEmptyPf = portfolio == null || portfolio == Portfolio.AnonymousPortfolio.Name;

				writer.Write(!isEmptyPf);

				if (!isEmptyPf)
				{
					metaInfo.Portfolios.TryAdd(message.PortfolioName);
					writer.WriteInt(metaInfo.Portfolios.IndexOf(message.PortfolioName));
				}

				if (metaInfo.Version < MarketDataVersions.Version51)
					continue;

				writer.Write(message.Currency != null);

				if (message.Currency != null)
					writer.WriteInt((int)message.Currency.Value);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			metaInfo.FirstOrderId += reader.ReadLong();

			decimal price;

			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				price = reader.ReadPriceEx(metaInfo);
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

			var volume = reader.ReadVolume(metaInfo);

			var orderDirection = reader.Read() ? Sides.Buy : Sides.Sell;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var execMsg = new ExecutionMessage
			{
				//LocalTime = metaInfo.FirstTime,
				ExecutionType = ExecutionTypes.OrderLog,
				SecurityId = SecurityId,
				OrderId = metaInfo.FirstOrderId,
				OrderVolume = volume,
				Side = orderDirection,
				ServerTime = serverTime,
				OrderPrice = price,
			};

			if (reader.Read())
			{
				metaInfo.FirstTradeId += reader.ReadLong();
				price = reader.ReadPriceEx(metaInfo);

				execMsg.TradeId = metaInfo.FirstTradeId;
				execMsg.TradePrice = price;

				execMsg.OrderState = OrderStates.Done;
			}
			else
			{
				var active = reader.Read();
				execMsg.OrderState = active ? OrderStates.Active : OrderStates.Done;
				execMsg.IsCancelled = !active;
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
						execMsg.TimeInForce = reader.Read() ? (TimeInForce)reader.ReadInt() : (TimeInForce?)null;

					execMsg.IsSystem = metaInfo.Version < MarketDataVersions.Version49
						? reader.Read()
						: (reader.Read() ? reader.Read() : (bool?)null);

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

			return execMsg;
		}
	}
}