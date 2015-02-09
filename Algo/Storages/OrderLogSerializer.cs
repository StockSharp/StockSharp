namespace StockSharp.Algo.Storages
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

	class OrderLogMetaInfo : BinaryMetaInfo<OrderLogMetaInfo>
	{
		public OrderLogMetaInfo(DateTime date)
			: base(date)
		{
			FirstOrderId = -1;
			Portfolios = new List<string>();
		}

		public long FirstOrderId { get; set; }
		public long LastOrderId { get; set; }

		public long FirstTradeId { get; set; }
		public long LastTradeId { get; set; }

		public long FirstTransactionId { get; set; }
		public long LastTransactionId { get; set; }

		public decimal FirstOrderPrice { get; set; }
		public decimal LastOrderPrice { get; set; }

		public IList<string> Portfolios { get; private set; }

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

			WriteNonSystemPrice(stream);
			WriteFractionalVolume(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			stream.Write(FirstOrderPrice);
			stream.Write(LastOrderPrice);

			WriteLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version48)
				return;

			stream.Write(ServerOffset);
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

			ReadNonSystemPrice(stream);
			ReadFractionalVolume(stream);

			if (Version < MarketDataVersions.Version45)
				return;

			FirstOrderPrice = stream.Read<decimal>();
			LastOrderPrice = stream.Read<decimal>();

			ReadLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version48)
				return;

			ServerOffset = stream.Read<TimeSpan>();
		}

		protected override void CopyFrom(OrderLogMetaInfo src)
		{
			base.CopyFrom(src);

			FirstOrderId = src.FirstOrderId;
			FirstTradeId = src.FirstTradeId;
			LastOrderId = src.LastOrderId;
			LastTradeId = src.LastTradeId;

			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;

			FirstTransactionId = src.FirstTransactionId;
			LastTransactionId = src.LastTransactionId;

			FirstOrderPrice = src.FirstOrderPrice;
			LastOrderPrice = src.LastOrderPrice;

			Portfolios.Clear();
			Portfolios.AddRange(src.Portfolios);
		}
	}

	class OrderLogSerializer : BinaryMarketDataSerializer<ExecutionMessage, OrderLogMetaInfo>
	{
		public OrderLogSerializer(SecurityId securityId)
			: base(securityId, 200)
		{
			Version = MarketDataVersions.Version48;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> items, OrderLogMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty() && !items.IsEmpty())
			{
				var item = items.First();

				metaInfo.FirstOrderId = metaInfo.LastOrderId = item.OrderId;
				metaInfo.FirstTransactionId = metaInfo.LastTransactionId = item.TransactionId;
				metaInfo.ServerOffset = item.ServerTime.Offset;
			}

			writer.WriteInt(items.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;

			foreach (var item in items)
			{
				var hasTrade = item.TradeId != 0 || item.TradePrice != 0;

				if (item.OrderId <= 0)
					throw new ArgumentOutOfRangeException("items", item.OrderId, LocalizedStrings.Str925);

				// sell market orders has zero price (if security do not have min allowed price)
				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (item.Price < 0)
				//	throw new ArgumentOutOfRangeException("items", item.Price, LocalizedStrings.Str926Params.Put(item.OrderId));

				if (item.Volume <= 0)
					throw new ArgumentOutOfRangeException("items", item.Volume, LocalizedStrings.Str927Params.Put(item.OrderId));

				if (hasTrade)
				{
					if (item.TradeId <= 0)
						throw new ArgumentOutOfRangeException("items", item.TradeId, LocalizedStrings.Str1012Params.Put(item.OrderId));

					// execution ticks (like option execution) may be a zero cost
					// ticks for spreads may be a zero cost or less than zero
					//if (item.TradePrice <= 0)
					//	throw new ArgumentOutOfRangeException("items", item.TradePrice, LocalizedStrings.Str929Params.Put(item.TradeId, item.OrderId));
				}

				metaInfo.LastOrderId = writer.SerializeId(item.OrderId, metaInfo.LastOrderId);

				var orderPrice = item.Price;

				if (metaInfo.Version < MarketDataVersions.Version45)
					writer.WritePriceEx(orderPrice, metaInfo, SecurityId);
				else
				{
					var isAligned = (orderPrice % metaInfo.PriceStep) == 0;
					writer.Write(isAligned);

					if (isAligned)
					{
						if (metaInfo.FirstOrderPrice == 0)
							metaInfo.FirstOrderPrice = metaInfo.LastOrderPrice = orderPrice;

						writer.WritePrice(orderPrice, metaInfo.LastOrderPrice, metaInfo, SecurityId, true);
						metaInfo.LastOrderPrice = orderPrice;
					}
					else
					{
						if (metaInfo.FirstNonSystemPrice == 0)
							metaInfo.FirstNonSystemPrice = metaInfo.LastNonSystemPrice = orderPrice;

						metaInfo.LastNonSystemPrice = writer.WriteDecimal(orderPrice, metaInfo.LastNonSystemPrice);
					}
				}

				writer.WriteVolume(item.Volume, metaInfo, SecurityId);

				writer.Write(item.Side == Sides.Buy);

				metaInfo.LastTime = writer.WriteTime(item.ServerTime, metaInfo.LastTime, LocalizedStrings.Str1013, allowNonOrdered, isUtc, metaInfo.ServerOffset);

				if (hasTrade)
				{
					writer.Write(true);

					if (metaInfo.FirstTradeId == 0)
					{
						metaInfo.FirstTradeId = metaInfo.LastTradeId = item.TradeId;
					}

					metaInfo.LastTradeId = writer.SerializeId(item.TradeId, metaInfo.LastTradeId);

					writer.WritePriceEx(item.TradePrice, metaInfo, SecurityId);
				}
				else
				{
					writer.Write(false);
					writer.Write(item.OrderState == OrderStates.Active);
				}

				if (metaInfo.Version < MarketDataVersions.Version31)
					continue;

				var status = item.OrderStatus;

				if (status == null)
					writer.Write(false);
				else
				{
					writer.Write(true);
					writer.WriteInt((int)status);
				}

				if (metaInfo.Version < MarketDataVersions.Version33)
					continue;

				writer.WriteInt((int)item.TimeInForce);
				writer.Write(item.IsSystem);

				if (metaInfo.Version < MarketDataVersions.Version34)
					continue;

				metaInfo.LastTransactionId = writer.SerializeId(item.TransactionId, metaInfo.LastTransactionId);

				if (metaInfo.Version < MarketDataVersions.Version40)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version46)
					writer.WriteLong(0/*item.Latency.Ticks*/);

				var portfolio = item.PortfolioName;
				var isEmptyPf = portfolio == null || portfolio == Portfolio.AnonymousPortfolio.Name;

				writer.Write(!isEmptyPf);

				if (isEmptyPf)
					continue;

				metaInfo.Portfolios.TryAdd(item.PortfolioName);
				writer.WriteInt(metaInfo.Portfolios.IndexOf(item.PortfolioName));
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
					price = metaInfo.FirstOrderPrice = reader.ReadPrice(metaInfo.FirstOrderPrice, metaInfo, true);
				else
					price = metaInfo.FirstNonSystemPrice = reader.ReadDecimal(metaInfo.FirstNonSystemPrice);
			}

			var volume = reader.ReadVolume(metaInfo);

			var orderDirection = reader.Read() ? Sides.Buy : Sides.Sell;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version48;

			var prevTime = metaInfo.FirstTime;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId));
			metaInfo.FirstTime = prevTime;

			var execMsg = new ExecutionMessage
			{
				//LocalTime = metaInfo.FirstTime,
				ExecutionType = ExecutionTypes.OrderLog,
				SecurityId = SecurityId,
				OrderId = metaInfo.FirstOrderId,
				Volume = volume,
				Side = orderDirection,
				ServerTime = serverTime,
				Price = price,
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
				execMsg.OrderState =  active ? OrderStates.Active : OrderStates.Done;
				execMsg.IsCancelled = !active;
			}

			if (metaInfo.Version >= MarketDataVersions.Version31)
			{
				if (reader.Read())
				{
					var status = reader.ReadInt();
					execMsg.OrderStatus = (OrderStatus?)status;

					if (status.HasBits(0x01))
						execMsg.TimeInForce = TimeInForce.PutInQueue;
					else if (status.HasBits(0x02))
						execMsg.TimeInForce = TimeInForce.CancelBalance;
				}

				// Лучше ExecCond писать отдельным полем так как возможно только Плаза пишет это в статус
				if (metaInfo.Version >= MarketDataVersions.Version33)
				{
					execMsg.TimeInForce = (TimeInForce)reader.ReadInt();
					execMsg.IsSystem = reader.Read();

					if (metaInfo.Version >= MarketDataVersions.Version34)
					{
						metaInfo.FirstTransactionId += reader.ReadLong();
						execMsg.TransactionId = metaInfo.FirstTransactionId;
					}
				}
				else
				{
					if (execMsg.OrderStatus != null)
						execMsg.IsSystem = !((int)execMsg.OrderStatus).HasBits(0x04);
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

			return execMsg;
		}
	}
}