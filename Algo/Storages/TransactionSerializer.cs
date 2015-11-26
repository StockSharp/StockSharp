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

	class TransactionSerializerMetaInfo : BinaryMetaInfo<TransactionSerializerMetaInfo>
	{
		public TransactionSerializerMetaInfo(DateTime date)
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

		public override object LastId => LastTransactionId;

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

		public IList<string> Portfolios { get; }

		public IList<string> StrategyIds { get; }

		public IList<string> Comments { get; }

		public IList<string> Errors { get; }

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

			if (Version < MarketDataVersions.Version56)
				return;

			WriteOffsets(stream);
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

			if (Version < MarketDataVersions.Version56)
				return;

			ReadOffsets(stream);
		}

		public override void CopyFrom(TransactionSerializerMetaInfo src)
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

	class TransactionSerializer : BinaryMarketDataSerializer<ExecutionMessage, TransactionSerializerMetaInfo>
	{
		public TransactionSerializer(SecurityId securityId)
			: base(securityId, 200, MarketDataVersions.Version57)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, TransactionSerializerMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var msg = messages.First();

				metaInfo.FirstOrderId = metaInfo.LastOrderId = msg.OrderId ?? 0;
				metaInfo.FirstTradeId = metaInfo.LastTradeId = msg.TradeId ?? 0;
				metaInfo.FirstTransactionId = metaInfo.LastTransactionId = msg.TransactionId;
				metaInfo.FirstOriginalTransactionId = metaInfo.LastOriginalTransactionId = msg.OriginalTransactionId;
				metaInfo.FirstCommission = metaInfo.LastCommission = msg.Commission ?? 0;
				metaInfo.ServerOffset = msg.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version51;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version56;

			foreach (var msg in messages)
			{
				var isTrade = msg.ExecutionType == ExecutionTypes.Trade;

				if (msg.ExecutionType != ExecutionTypes.Order && msg.ExecutionType != ExecutionTypes.Trade)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.ExecutionType, LocalizedStrings.Str1695Params.Put(msg.OrderId ?? msg.TradeId));

				// нулевой номер заявки возможен при сохранении в момент регистрации
				if (msg.OrderId < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderId, LocalizedStrings.Str925);

				// нулевая цена возможна, если идет "рыночная" продажа по инструменту без планок
				if (msg.OrderPrice < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderPrice, LocalizedStrings.Str926Params.Put(msg.OrderId == null ? msg.OrderStringId : msg.OrderId.To<string>()));

				var volume = msg.Volume;

				if (volume < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.Str927Params.Put(msg.OrderId == null ? msg.OrderStringId : msg.OrderId.To<string>()));

				if (isTrade)
				{
					if ((msg.TradeId == null && msg.TradeStringId.IsEmpty()) || msg.TradeId <= 0)
						throw new ArgumentOutOfRangeException(nameof(messages), msg.TradeId, LocalizedStrings.Str928Params.Put(msg.TransactionId));

					if (msg.TradePrice == null || msg.TradePrice <= 0)
						throw new ArgumentOutOfRangeException(nameof(messages), msg.TradePrice, LocalizedStrings.Str929Params.Put(msg.TradeId, msg.OrderId));
				}

				writer.WriteInt((int)msg.ExecutionType);

				metaInfo.LastTransactionId = writer.SerializeId(msg.TransactionId, metaInfo.LastTransactionId);
				metaInfo.LastOriginalTransactionId = writer.SerializeId(msg.OriginalTransactionId, metaInfo.LastOriginalTransactionId);

				if (!isTrade)
				{
					if (metaInfo.Version < MarketDataVersions.Version50)
						metaInfo.LastOrderId = writer.SerializeId(msg.OrderId ?? 0, metaInfo.LastOrderId);
					else
					{
						writer.Write(msg.OrderId != null);

						if (msg.OrderId != null)
						{
							metaInfo.LastOrderId = writer.SerializeId(msg.OrderId.Value, metaInfo.LastOrderId);
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
						metaInfo.LastTradeId = writer.SerializeId(msg.TradeId ?? 0, metaInfo.LastTradeId);
					else
					{
						writer.Write(msg.TradeId != null);

						if (msg.TradeId != null)
						{
							metaInfo.LastTradeId = writer.SerializeId(msg.TradeId.Value, metaInfo.LastTradeId);
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
				writer.WritePriceEx(!isTrade ? msg.OrderPrice : msg.GetTradePrice(), metaInfo, SecurityId);


				if (metaInfo.Version < MarketDataVersions.Version57)
				{
					writer.WriteVolume(volume ?? 0, metaInfo, SecurityId);
				}
				else
				{
					writer.Write(volume != null);

					if (volume != null)
						writer.WriteVolume(volume.Value, metaInfo, SecurityId);
				}

				if (metaInfo.Version < MarketDataVersions.Version54)
				{
					writer.WriteVolume(msg.VisibleVolume ?? 0, metaInfo, SecurityId);
					writer.WriteVolume(msg.Balance ?? 0, metaInfo, SecurityId);
				}
				else
				{
					writer.Write(msg.VisibleVolume != null);

					if (msg.VisibleVolume != null)
						writer.WriteVolume(msg.VisibleVolume.Value, metaInfo, SecurityId);

					writer.Write(msg.Balance != null);

					if (msg.Balance != null)
						writer.WriteVolume(msg.Balance.Value, metaInfo, SecurityId);
				}

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str930, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				writer.WriteInt((int)msg.OrderType);

				writer.WriteNullableInt(msg.OrderState);
				writer.WriteNullableInt(msg.OrderStatus);

				if (metaInfo.Version < MarketDataVersions.Version52)
					writer.WriteInt(msg.TradeStatus ?? 0);
				else
					writer.WriteNullableInt(msg.TradeStatus);
				
				if (metaInfo.Version < MarketDataVersions.Version53)
					writer.WriteInt((int)(msg.TimeInForce ?? TimeInForce.PutInQueue));
				else
				{
					writer.Write(msg.TimeInForce != null);

					if (msg.TimeInForce != null)
						writer.WriteInt((int)msg.TimeInForce.Value);
				}

				if (metaInfo.Version < MarketDataVersions.Version52)
					writer.Write(msg.IsSystem ?? true);
				else
				{
					writer.Write(msg.IsSystem != null);

					if (msg.IsSystem != null)
						writer.Write(msg.IsSystem.Value);
				}

				writer.WriteLong(msg.ExpiryDate != null ? msg.ExpiryDate.Value.Ticks : 0L);

				WriteCommission(writer, metaInfo, msg.Commission);

				WriteString(writer, metaInfo.Portfolios, msg.PortfolioName);
				WriteString(writer, metaInfo.StrategyIds, msg.UserOrderId);
				WriteString(writer, metaInfo.Comments, msg.Comment);
				WriteString(writer, metaInfo.Errors, msg.Error != null ? msg.Error.Message : null);

				if (metaInfo.Version < MarketDataVersions.Version55)
					continue;

				writer.Write(msg.Currency != null);

				if (msg.Currency != null)
					writer.WriteInt((int)msg.Currency.Value);
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
			var volume = (metaInfo.Version < MarketDataVersions.Version57 || reader.Read())
				? reader.ReadVolume(metaInfo) : (decimal?) null;

			var visibleVolume = (metaInfo.Version < MarketDataVersions.Version54 || reader.Read())
				? reader.ReadVolume(metaInfo) : (decimal?)null;

			var balance = (metaInfo.Version < MarketDataVersions.Version54 || reader.Read())
				? reader.ReadVolume(metaInfo) : (decimal?)null;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version51;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version56;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId), allowDiffOffsets, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var type = reader.ReadInt().To<OrderTypes>();

			var state = reader.ReadNullableInt<OrderStates>();
			var status = reader.ReadNullableInt<OrderStatus>();

			var tradeStatus = metaInfo.Version < MarketDataVersions.Version52
				? reader.ReadInt()
				: reader.ReadNullableInt<int>();

			var timeInForce = metaInfo.Version < MarketDataVersions.Version53
				? reader.ReadInt().To<TimeInForce>()
				: reader.Read() ? reader.ReadInt().To<TimeInForce>() : (TimeInForce?)null;

			var isSystem = metaInfo.Version < MarketDataVersions.Version52
						? reader.Read()
						: (reader.Read() ? reader.Read() : (bool?)null);

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
				ExpiryDate = expDate == 0 ? (DateTimeOffset?)null : expDate.To<DateTimeOffset>(),
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
				msg.OrderPrice = price;
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

			if (metaInfo.Version >= MarketDataVersions.Version55)
			{
				if (reader.Read())
					msg.Currency = (CurrencyTypes)reader.ReadInt();
			}

			return msg;
		}

		private static void WriteCommission(BitArrayWriter writer, TransactionSerializerMetaInfo metaInfo, decimal? value)
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

		private static decimal? ReadCommission(BitArrayReader reader, TransactionSerializerMetaInfo metaInfo)
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
