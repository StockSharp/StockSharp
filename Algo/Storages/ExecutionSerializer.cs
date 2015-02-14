namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class ExecutionSerializerMetaInfo : BinaryMetaInfo<ExecutionSerializerMetaInfo>
	{
		public ExecutionSerializerMetaInfo(DateTime date)
			: base(date)
		{
			FirstOrderId = -1;
			FirstTransactionId = -1;
			FirstOriginalTransactionId = -1;
			FirstTradeId = -1;

			Portfolios = new List<string>();
			StrategyIds = new List<string>();
			Comments = new List<string>();
			Errors = new List<string>();
		}

		public long FirstOrderId { get; set; }
		public long LastOrderId { get; set; }

		public long FirstTradeId { get; set; }
		public long LastTradeId { get; set; }

		public long FirstTransactionId { get; set; }
		public long LastTransactionId { get; set; }

		public long FirstOriginalTransactionId { get; set; }
		public long LastOriginalTransactionId { get; set; }

		public decimal FirstCommission { get; set; }
		public decimal LastCommission { get; set; }

		public IList<string> Portfolios { get; private set; }

		public IList<string> StrategyIds { get; private set; }

		public IList<string> Comments { get; private set; }

		public IList<string> Errors { get; private set; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstOrderId);
			stream.Write(LastOrderId);
			stream.Write(FirstTradeId);
			stream.Write(LastTradeId);
			stream.Write(FirstTransactionId);
			stream.Write(LastTransactionId);
			stream.Write(FirstOriginalTransactionId);
			stream.Write(LastOriginalTransactionId);
			stream.Write(FirstPrice);
			stream.Write(LastPrice);
			stream.Write(FirstCommission);
			stream.Write(LastCommission);

			WriteList(stream, Portfolios);
			WriteList(stream, StrategyIds);
			WriteList(stream, Comments);
			WriteList(stream, Errors);

			WriteNonSystemPrice(stream);
			WriteFractionalVolume(stream);

			WriteLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version51)
				return;

			stream.Write(ServerOffset);
		}

		private static void WriteList(Stream stream, IList<string> list)
		{
			stream.Write(list.Count);

			foreach (var item in list)
				stream.Write(item);
		}

		private static void ReadList(Stream stream, IList<string> list)
		{
			var count = stream.Read<int>();

			for (var i = 0; i < count; i++)
				list.Add(stream.Read<string>());
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			FirstOrderId = stream.Read<long>();
			LastOrderId = stream.Read<long>();
			FirstTradeId = stream.Read<long>();
			LastTradeId = stream.Read<long>();
			FirstTransactionId = stream.Read<long>();
			LastTransactionId = stream.Read<long>();
			FirstOriginalTransactionId = stream.Read<long>();
			LastOriginalTransactionId = stream.Read<long>();
			FirstPrice = stream.Read<decimal>();
			LastPrice = stream.Read<decimal>();
			FirstCommission = stream.Read<decimal>();
			LastCommission = stream.Read<decimal>();

			ReadList(stream, Portfolios);
			ReadList(stream, StrategyIds);
			ReadList(stream, Comments);
			ReadList(stream, Errors);

			ReadNonSystemPrice(stream);
			ReadFractionalVolume(stream);

			ReadLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version51)
				return;

			ServerOffset = stream.Read<TimeSpan>();
		}

		protected override void CopyFrom(ExecutionSerializerMetaInfo src)
		{
			base.CopyFrom(src);

			FirstOrderId = src.FirstOrderId;
			LastOrderId = src.LastOrderId;
			FirstTradeId = src.FirstTradeId;
			LastTradeId = src.LastTradeId;
			FirstTransactionId = src.FirstTransactionId;
			LastTransactionId = src.LastTransactionId;
			FirstOriginalTransactionId = src.FirstOriginalTransactionId;
			LastOriginalTransactionId = src.LastOriginalTransactionId;
			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
			FirstCommission = src.FirstCommission;
			LastCommission = src.LastCommission;

			Portfolios.Clear();
			Portfolios.AddRange(src.Portfolios);

			StrategyIds.Clear();
			StrategyIds.AddRange(src.StrategyIds);

			Comments.Clear();
			Comments.AddRange(src.Comments);

			Errors.Clear();
			Errors.AddRange(src.Errors);
		}
	}

	class ExecutionSerializer : BinaryMarketDataSerializer<ExecutionMessage, ExecutionSerializerMetaInfo>
	{
		public ExecutionSerializer(SecurityId securityId)
			: base(securityId, 200)
		{
			Version = MarketDataVersions.Version51;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, ExecutionSerializerMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var msg = messages.First();

				metaInfo.FirstOrderId = metaInfo.LastOrderId = msg.OrderId;
				metaInfo.FirstTradeId = metaInfo.LastTradeId = msg.TradeId;
				metaInfo.FirstTransactionId = metaInfo.LastTransactionId = msg.TransactionId;
				metaInfo.FirstOriginalTransactionId = metaInfo.LastOriginalTransactionId = msg.OriginalTransactionId;
				metaInfo.FirstCommission = metaInfo.LastCommission = msg.Commission ?? 0;
				metaInfo.ServerOffset = msg.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version51;

			foreach (var msg in messages)
			{
				var isTrade = msg.ExecutionType == ExecutionTypes.Trade;

				if (msg.ExecutionType != ExecutionTypes.Order && msg.ExecutionType != ExecutionTypes.Trade)
					throw new ArgumentOutOfRangeException("messages", msg.ExecutionType, LocalizedStrings.Str924);

				// нулевой номер заявки возможен при сохранении в момент регистрации
				if (msg.OrderId < 0)
					throw new ArgumentOutOfRangeException("messages", msg.OrderId, LocalizedStrings.Str925);

				// нулевая цена возможна, если идет "рыночная" продажа по инструменту без планок
				if (msg.Price < 0)
					throw new ArgumentOutOfRangeException("messages", msg.Price, LocalizedStrings.Str926Params.Put(msg.OrderId == 0 ? msg.OrderStringId : msg.OrderId.To<string>()));

				if (msg.Volume < 0)
					throw new ArgumentOutOfRangeException("messages", msg.Volume, LocalizedStrings.Str927Params.Put(msg.OrderId == 0 ? msg.OrderStringId : msg.OrderId.To<string>()));

				if (isTrade)
				{
					if (msg.TradeId <= 0)
						throw new ArgumentOutOfRangeException("messages", msg.TradeId, LocalizedStrings.Str928Params.Put(msg.TransactionId));

					if (msg.TradePrice <= 0)
						throw new ArgumentOutOfRangeException("messages", msg.TradePrice, LocalizedStrings.Str929Params.Put(msg.TradeId, msg.OrderId));
				}

				writer.WriteInt((int)msg.ExecutionType);

				metaInfo.LastTransactionId = writer.SerializeId(msg.TransactionId, metaInfo.LastTransactionId);
				metaInfo.LastOriginalTransactionId = writer.SerializeId(msg.OriginalTransactionId, metaInfo.LastOriginalTransactionId);

				if (!isTrade)
				{
					if (metaInfo.Version < MarketDataVersions.Version50)
						metaInfo.LastOrderId = writer.SerializeId(msg.OrderId, metaInfo.LastOrderId);
					else
					{
						writer.Write(msg.OrderId > 0);

						if (msg.OrderId > 0)
						{
							metaInfo.LastOrderId = writer.SerializeId(msg.OrderId, metaInfo.LastOrderId);
						}
						else
						{
							writer.Write(!msg.OrderStringId.IsEmpty());

							if (!msg.OrderStringId.IsEmpty())
								writer.WriteString(msg.OrderStringId);
						}

						writer.Write(!msg.OrderBoardId.IsEmpty());

						if (!msg.OrderBoardId.IsEmpty())
							writer.WriteString(msg.OrderBoardId);
					}
				}
				else
				{
					if (metaInfo.Version < MarketDataVersions.Version50)
						metaInfo.LastTradeId = writer.SerializeId(msg.TradeId, metaInfo.LastTradeId);
					else
					{
						writer.Write(msg.TradeId > 0);

						if (msg.TradeId > 0)
						{
							metaInfo.LastTradeId = writer.SerializeId(msg.TradeId, metaInfo.LastTradeId);
						}
						else
						{
							writer.Write(!msg.TradeStringId.IsEmpty());

							if (!msg.TradeStringId.IsEmpty())
								writer.WriteString(msg.TradeStringId);
						}
					}
				}

				writer.Write(msg.Side == Sides.Buy);
				writer.WritePriceEx(!isTrade ? msg.Price : msg.TradePrice, metaInfo, SecurityId);

				writer.WriteVolume(msg.Volume, metaInfo, SecurityId);
				writer.WriteVolume(msg.VisibleVolume, metaInfo, SecurityId);
				writer.WriteVolume(msg.Balance, metaInfo, SecurityId);

				metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str930, allowNonOrdered, isUtc, metaInfo.ServerOffset);

				writer.WriteInt((int)msg.OrderType);

				WriteNullableInt(writer, msg.OrderState);
				WriteNullableInt(writer, msg.OrderStatus);

				writer.WriteInt(msg.TradeStatus);
				
				writer.WriteInt((int)msg.TimeInForce);
				writer.Write(msg.IsSystem);

				writer.WriteLong(msg.ExpiryDate.Ticks);

				WriteCommission(writer, metaInfo, msg.Commission);

				WriteString(writer, metaInfo.Portfolios, msg.PortfolioName);
				WriteString(writer, metaInfo.StrategyIds, msg.UserOrderId);
				WriteString(writer, metaInfo.Comments, msg.Comment);
				WriteString(writer, metaInfo.Errors, msg.Error != null ? msg.Error.Message : null);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			var execType = (ExecutionTypes)reader.ReadInt();
			var isTrade = execType == ExecutionTypes.Trade;

			metaInfo.FirstTransactionId += reader.ReadLong();
			metaInfo.FirstOriginalTransactionId += reader.ReadLong();

			string orderBoardId = null;
			string orderStringId = null;
			string tradeStringId = null;

			if (!isTrade)
			{
				if (metaInfo.Version < MarketDataVersions.Version50)
					metaInfo.FirstOrderId += reader.ReadLong();
				else
				{
					if (reader.Read())
						metaInfo.FirstOrderId += reader.ReadLong();
					else
					{
						if (reader.Read())
							orderStringId = reader.ReadString();
					}

					if (reader.Read())
						orderBoardId = reader.ReadString();
				}
			}
			else
			{
				if (metaInfo.Version < MarketDataVersions.Version50)
					metaInfo.FirstTradeId += reader.ReadLong();
				else
				{
					if (reader.Read())
						metaInfo.FirstTradeId += reader.ReadLong();
					else
					{
						if (reader.Read())
							tradeStringId = reader.ReadString();
					}
				}
			}

			var side = reader.Read() ? Sides.Buy : Sides.Sell;

			var price = reader.ReadPriceEx(metaInfo);
			var volume = reader.ReadVolume(metaInfo);
			var visibleVolume = reader.ReadVolume(metaInfo);
			var balance = reader.ReadVolume(metaInfo);

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version51;

			var prevTime = metaInfo.FirstTime;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId));
			metaInfo.FirstTime = prevTime;

			var type = reader.ReadInt().To<OrderTypes>();

			var state = ReadNullableInt<OrderStates>(reader);
			var status = ReadNullableInt<OrderStatus>(reader);

			var tradeStatus = reader.ReadInt();

			var timeInForce = reader.ReadInt().To<TimeInForce>();
			var isSystem = reader.Read();

			var expDate = reader.ReadLong();

			var commission = ReadCommission(reader, metaInfo);

			var portfolio = ReadString(reader, metaInfo.Portfolios);
			var userOrderId = ReadString(reader, metaInfo.StrategyIds);
			var comment = ReadString(reader, metaInfo.Comments);
			var error = ReadString(reader, metaInfo.Errors);

			var msg = new ExecutionMessage
			{
				ExecutionType = execType,
				SecurityId = SecurityId,

				ServerTime = serverTime,

				TransactionId = metaInfo.FirstTransactionId,
				OriginalTransactionId = metaInfo.FirstOriginalTransactionId,

				Side = side,
				Volume = volume,
				VisibleVolume = visibleVolume,
				Balance = balance,

				OrderType = type,
				OrderState = state,
				OrderStatus = status,
				TimeInForce = timeInForce,
				IsSystem = isSystem,
				ExpiryDate = expDate.To<DateTimeOffset>(),
				Commission = commission,
				PortfolioName = portfolio,
				UserOrderId = userOrderId,
				Comment = comment,

				TradeStatus = tradeStatus,
			};

			if (!isTrade)
			{
				if (orderStringId == null)
					msg.OrderId = metaInfo.FirstOrderId;
				else
					msg.OrderStringId = orderStringId;

				msg.OrderBoardId = orderBoardId;
				msg.Price = price;
			}
			else
			{
				if (tradeStringId == null)
					msg.TradeId = metaInfo.FirstTradeId;
				else
					msg.TradeStringId = tradeStringId;

				msg.TradePrice = price;
			}

			if (!error.IsEmpty())
				msg.Error = new InvalidOperationException(error);

			return msg;
		}

		private static void WriteNullableInt<T>(BitArrayWriter writer, T? value)
			where T : struct
		{
			if (value == null)
				writer.Write(false);
			else
			{
				writer.Write(true);
				writer.WriteInt(value.To<int>());
			}
		}

		private static void WriteCommission(BitArrayWriter writer, ExecutionSerializerMetaInfo metaInfo, decimal? value)
		{
			if (value == null)
				writer.Write(false);
			else
			{
				writer.Write(true);
				writer.WriteDecimal((decimal)value, metaInfo.LastCommission);

				metaInfo.LastCommission = (decimal)value;
			}
		}

		private static void WriteString(BitArrayWriter writer, IList<string> items, string value)
		{
			if (value.IsEmpty())
				writer.Write(false);
			else
			{
				writer.Write(true);

				items.TryAdd(value);
				writer.WriteInt(items.IndexOf(value));
			}
		}

		private static T? ReadNullableInt<T>(BitArrayReader reader)
			where T : struct
		{
			if (!reader.Read())
				return null;

			return reader.ReadInt().To<T?>();
		}

		private static decimal? ReadCommission(BitArrayReader reader, ExecutionSerializerMetaInfo metaInfo)
		{
			if (!reader.Read())
				return null;

			return metaInfo.FirstCommission = reader.ReadDecimal(metaInfo.FirstCommission);
		}

		private static string ReadString(BitArrayReader reader, IList<string> items)
		{
			if (!reader.Read())
				return null;

			return items[reader.ReadInt()];
		}
	}
}
