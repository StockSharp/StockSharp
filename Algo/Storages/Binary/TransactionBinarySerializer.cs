#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: TransactionBinarySerializer.cs
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

	using StockSharp.Messages;
	using StockSharp.Localization;

	class TransactionSerializerMetaInfo : BinaryMetaInfo
	{
		public TransactionSerializerMetaInfo(DateTime date)
			: base(date)
		{
			FirstOrderId = -1;
			FirstTransactionId = -1;
			FirstOriginalTransactionId = -1;
			FirstTradeId = -1;

			Portfolios = new List<string>();
			ClientCodes = new List<string>();
			BrokerCodes = new List<string>();
			DepoNames = new List<string>();
			UserOrderIds = new List<string>();
			Comments = new List<string>();
			SystemComments = new List<string>();
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

		public decimal FirstPnL { get; set; }
		public decimal LastPnL { get; set; }

		public decimal FirstPosition { get; set; }
		public decimal LastPosition { get; set; }

		public decimal FirstSlippage { get; set; }
		public decimal LastSlippage { get; set; }

		public IList<string> Portfolios { get; }
		public IList<string> ClientCodes { get; }
		public IList<string> BrokerCodes { get; }
		public IList<string> DepoNames { get; }

		public IList<string> UserOrderIds { get; }

		public IList<string> Comments { get; }
		public IList<string> SystemComments { get; }

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
			stream.Write(FirstPnL);
			stream.Write(LastPnL);
			stream.Write(FirstPosition);
			stream.Write(LastPosition);
			stream.Write(FirstSlippage);
			stream.Write(LastSlippage);

			WriteList(stream, Portfolios);
			WriteList(stream, ClientCodes);
			WriteList(stream, BrokerCodes);
			WriteList(stream, DepoNames);
			WriteList(stream, UserOrderIds);
			WriteList(stream, Comments);
			WriteList(stream, SystemComments);
			WriteList(stream, Errors);

			WriteFractionalPrice(stream);
			WriteFractionalVolume(stream);

			WriteLocalTime(stream, MarketDataVersions.Version47);

			stream.Write(ServerOffset);

			WriteOffsets(stream);

			WriteItemLocalTime(stream, MarketDataVersions.Version59);
			WriteItemLocalOffset(stream, MarketDataVersions.Version59);
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
			FirstPnL = stream.Read<decimal>();
			LastPnL = stream.Read<decimal>();
			FirstPosition = stream.Read<decimal>();
			LastPosition = stream.Read<decimal>();
			FirstSlippage = stream.Read<decimal>();
			LastSlippage = stream.Read<decimal>();

			ReadList(stream, Portfolios);
			ReadList(stream, ClientCodes);
			ReadList(stream, BrokerCodes);
			ReadList(stream, DepoNames);
			ReadList(stream, UserOrderIds);
			ReadList(stream, Comments);
			ReadList(stream, SystemComments);
			ReadList(stream, Errors);

			ReadFractionalPrice(stream);
			ReadFractionalVolume(stream);

			ReadLocalTime(stream, MarketDataVersions.Version47);

			ServerOffset = stream.Read<TimeSpan>();

			ReadOffsets(stream);

			ReadItemLocalTime(stream, MarketDataVersions.Version59);
			ReadItemLocalOffset(stream, MarketDataVersions.Version59);
		}

		public override void CopyFrom(BinaryMetaInfo src)
		{
			base.CopyFrom(src);

			var tsInfo = (TransactionSerializerMetaInfo)src;

			FirstOrderId = tsInfo.FirstOrderId;
			LastOrderId = tsInfo.LastOrderId;
			FirstTradeId = tsInfo.FirstTradeId;
			LastTradeId = tsInfo.LastTradeId;
			FirstTransactionId = tsInfo.FirstTransactionId;
			LastTransactionId = tsInfo.LastTransactionId;
			FirstOriginalTransactionId = tsInfo.FirstOriginalTransactionId;
			LastOriginalTransactionId = tsInfo.LastOriginalTransactionId;
			FirstCommission = tsInfo.FirstCommission;
			LastCommission = tsInfo.LastCommission;
			FirstPnL = tsInfo.FirstPnL;
			LastPnL = tsInfo.LastPnL;
			FirstPosition = tsInfo.FirstPosition;
			LastPosition = tsInfo.LastPosition;
			FirstSlippage = tsInfo.FirstSlippage;
			LastSlippage = tsInfo.LastSlippage;

			Portfolios.Clear();
			Portfolios.AddRange(tsInfo.Portfolios);

			ClientCodes.Clear();
			ClientCodes.AddRange(tsInfo.ClientCodes);

			BrokerCodes.Clear();
			BrokerCodes.AddRange(tsInfo.BrokerCodes);

			DepoNames.Clear();
			DepoNames.AddRange(tsInfo.DepoNames);

			UserOrderIds.Clear();
			UserOrderIds.AddRange(tsInfo.UserOrderIds);

			Comments.Clear();
			Comments.AddRange(tsInfo.Comments);

			SystemComments.Clear();
			SystemComments.AddRange(tsInfo.SystemComments);

			Errors.Clear();
			Errors.AddRange(tsInfo.Errors);
		}
	}

	class TransactionBinarySerializer : BinaryMarketDataSerializer<ExecutionMessage, TransactionSerializerMetaInfo>
	{
		public TransactionBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 200, MarketDataVersions.Version63, exchangeInfoProvider)
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
				metaInfo.FirstPnL = metaInfo.LastPnL = msg.PnL ?? 0;
				metaInfo.FirstPosition = metaInfo.LastPosition = msg.Position ?? 0;
				metaInfo.FirstSlippage = metaInfo.LastSlippage = msg.Slippage ?? 0;
				metaInfo.FirstItemLocalTime = metaInfo.LastItemLocalTime = msg.LocalTime.UtcDateTime;
				metaInfo.FirstItemLocalOffset = metaInfo.LastItemLocalOffset = msg.LocalTime.Offset;
				metaInfo.ServerOffset = msg.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version51;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version56;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version60;

			foreach (var msg in messages)
			{
				if (msg.ExecutionType != ExecutionTypes.Transaction)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.ExecutionType, LocalizedStrings.Str1695Params.Put(msg));

				// нулевой номер заявки возможен при сохранении в момент регистрации
				if (msg.OrderId < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderId, LocalizedStrings.Str925);

				// нулевая цена возможна, если идет "рыночная" продажа по инструменту без планок
				if (msg.OrderPrice < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), msg.OrderPrice, LocalizedStrings.Str926Params.Put(msg.OrderId == null ? msg.OrderStringId : msg.OrderId.To<string>()));

				//var volume = msg.Volume;

				//if (volume < 0)
				//	throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.Str927Params.Put(msg.OrderId == null ? msg.OrderStringId : msg.OrderId.To<string>()));

				if (msg.HasTradeInfo())
				{
					//if ((msg.TradeId == null && msg.TradeStringId.IsEmpty()) || msg.TradeId <= 0)
					//	throw new ArgumentOutOfRangeException(nameof(messages), msg.TradeId, LocalizedStrings.Str928Params.Put(msg.TransactionId));

					if (msg.TradePrice == null || msg.TradePrice <= 0)
						throw new ArgumentOutOfRangeException(nameof(messages), msg.TradePrice, LocalizedStrings.Str929Params.Put(msg.TradeId, msg.OrderId));
				}

				metaInfo.LastTransactionId = writer.SerializeId(msg.TransactionId, metaInfo.LastTransactionId);
				metaInfo.LastOriginalTransactionId = writer.SerializeId(msg.OriginalTransactionId, metaInfo.LastOriginalTransactionId);

				writer.Write(msg.HasOrderInfo);
				writer.Write(msg.HasTradeInfo);

				writer.Write(msg.OrderId != null);

				if (msg.OrderId != null)
				{
					metaInfo.LastOrderId = writer.SerializeId(msg.OrderId.Value, metaInfo.LastOrderId);
				}
				else
				{
					writer.WriteStringEx(msg.OrderStringId);
				}

				writer.WriteStringEx(msg.OrderBoardId);

				writer.Write(msg.TradeId != null);

				if (msg.TradeId != null)
				{
					metaInfo.LastTradeId = writer.SerializeId(msg.TradeId.Value, metaInfo.LastTradeId);
				}
				else
				{
					writer.WriteStringEx(msg.TradeStringId);
				}

				if (msg.OrderPrice != 0)
				{
					writer.Write(true);
					writer.WritePriceEx(msg.OrderPrice, metaInfo, SecurityId);
				}
				else
					writer.Write(false);

				if (msg.TradePrice != null)
				{
					writer.Write(true);
					writer.WritePriceEx(msg.TradePrice.Value, metaInfo, SecurityId);
				}
				else
					writer.Write(false);

				writer.Write(msg.Side == Sides.Buy);

				writer.Write(msg.OrderVolume != null);

				if (msg.OrderVolume != null)
					writer.WriteVolume(msg.OrderVolume.Value, metaInfo, SecurityId);

				writer.Write(msg.TradeVolume != null);

				if (msg.TradeVolume != null)
					writer.WriteVolume(msg.TradeVolume.Value, metaInfo, SecurityId);

				writer.Write(msg.VisibleVolume != null);

				if (msg.VisibleVolume != null)
					writer.WriteVolume(msg.VisibleVolume.Value, metaInfo, SecurityId);

				writer.Write(msg.Balance != null);

				if (msg.Balance != null)
					writer.WriteVolume(msg.Balance.Value, metaInfo, SecurityId);

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str930, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				writer.WriteNullableInt((int?)msg.OrderType);
				writer.WriteNullableInt((int?)msg.OrderState);
				writer.WriteNullableLong(msg.OrderStatus);
				writer.WriteNullableInt(msg.TradeStatus);
				writer.WriteNullableInt((int?)msg.TimeInForce);

				writer.Write(msg.IsSystem != null);

				if (msg.IsSystem != null)
					writer.Write(msg.IsSystem.Value);

				writer.Write(msg.IsUpTick != null);

				if (msg.IsUpTick != null)
					writer.Write(msg.IsUpTick.Value);

				writer.WriteDto(msg.ExpiryDate);

				metaInfo.LastCommission = Write(writer, msg.Commission, metaInfo.LastCommission);
				metaInfo.LastPnL = Write(writer, msg.PnL, metaInfo.LastPnL);
				metaInfo.LastPosition = Write(writer, msg.Position, metaInfo.LastPosition);
				metaInfo.LastSlippage = Write(writer, msg.Slippage, metaInfo.LastSlippage);

				WriteString(writer, metaInfo.Portfolios, msg.PortfolioName);
				WriteString(writer, metaInfo.ClientCodes, msg.ClientCode);
				WriteString(writer, metaInfo.BrokerCodes, msg.BrokerCode);
				WriteString(writer, metaInfo.DepoNames, msg.DepoName);
				WriteString(writer, metaInfo.UserOrderIds, msg.UserOrderId);
				WriteString(writer, metaInfo.Comments, msg.Comment);
				WriteString(writer, metaInfo.SystemComments, msg.SystemComment);
				WriteString(writer, metaInfo.Errors, msg.Error?.Message);

				writer.WriteNullableInt((int?)msg.Currency);

				writer.Write(msg.Latency != null);

				if (msg.Latency != null)
					writer.WriteLong(msg.Latency.Value.Ticks);

				writer.Write(msg.OriginSide != null);

				if (msg.OriginSide != null)
					writer.Write(msg.OriginSide.Value == Sides.Buy);

				if (metaInfo.Version < MarketDataVersions.Version59)
					continue;

				WriteItemLocalTime(writer, metaInfo, msg, isTickPrecision);

				if (metaInfo.Version < MarketDataVersions.Version61)
					continue;

				writer.Write(msg.IsMarketMaker != null);

				if (msg.IsMarketMaker != null)
					writer.Write(msg.IsMarketMaker.Value);

				if (metaInfo.Version < MarketDataVersions.Version62)
					continue;

				writer.Write(msg.IsMargin != null);

				if (msg.IsMargin != null)
					writer.Write(msg.IsMargin.Value);

				if (metaInfo.Version < MarketDataVersions.Version63)
					continue;

				writer.WriteStringEx(msg.CommissionCurrency);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			metaInfo.FirstTransactionId += reader.ReadLong();
			metaInfo.FirstOriginalTransactionId += reader.ReadLong();

			var hasOrderInfo = reader.Read();
			var hasTradeInfo = reader.Read();

			long? orderId = null;
			long? tradeId = null;

			string orderStringId = null;
			string tradeStringId = null;

			if (reader.Read())
			{
				metaInfo.FirstOrderId += reader.ReadLong();
				orderId = metaInfo.FirstOrderId;
			}
			else
			{
				orderStringId = reader.ReadStringEx();
			}

			var orderBoardId = reader.ReadStringEx();

			if (reader.Read())
			{
				metaInfo.FirstTradeId += reader.ReadLong();
				tradeId = metaInfo.FirstTradeId;
			}
			else
			{
				tradeStringId = reader.ReadStringEx();
			}

			var orderPrice = reader.Read() ? reader.ReadPriceEx(metaInfo) : (decimal?)null;
			var tradePrice = reader.Read() ? reader.ReadPriceEx(metaInfo) : (decimal?)null;

			var side = reader.Read() ? Sides.Buy : Sides.Sell;

			var orderVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
			var tradeVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
			var visibleVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
			var balance = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;

			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version60;

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, true, true, metaInfo.GetTimeZone(true, SecurityId, ExchangeInfoProvider), true, isTickPrecision, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var type = (OrderTypes?)reader.ReadNullableInt();
			var state = (OrderStates?)reader.ReadNullableInt();
			var status = reader.ReadNullableLong();
			var tradeStatus = reader.ReadNullableInt();
			var timeInForce = (TimeInForce?)reader.ReadNullableInt();

			var isSystem = reader.Read() ? reader.Read() : (bool?)null;
			var isUpTick = reader.Read() ? reader.Read() : (bool?)null;

			var expDate = reader.ReadDto();

			var commission = reader.Read() ? metaInfo.FirstCommission = reader.ReadDecimal(metaInfo.FirstCommission) : (decimal?)null;
			var pnl = reader.Read() ? metaInfo.FirstPnL = reader.ReadDecimal(metaInfo.FirstPnL) : (decimal?)null;
			var position = reader.Read() ? metaInfo.FirstPosition = reader.ReadDecimal(metaInfo.FirstPosition) : (decimal?)null;
			var slippage = reader.Read() ? metaInfo.FirstSlippage = reader.ReadDecimal(metaInfo.FirstSlippage) : (decimal?)null;

			var portfolio = ReadString(reader, metaInfo.Portfolios);
			var clientCode = ReadString(reader, metaInfo.ClientCodes);
			var brokerCode = ReadString(reader, metaInfo.BrokerCodes);
			var depoName = ReadString(reader, metaInfo.DepoNames);
			var userOrderId = ReadString(reader, metaInfo.UserOrderIds);
			var comment = ReadString(reader, metaInfo.Comments);
			var sysComment = ReadString(reader, metaInfo.SystemComments);

			var msg = new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Transaction,
				SecurityId = SecurityId,

				ServerTime = serverTime,

				TransactionId = metaInfo.FirstTransactionId,
				OriginalTransactionId = metaInfo.FirstOriginalTransactionId,

				Side = side,
				OrderVolume = orderVolume,
				TradeVolume = tradeVolume,
				VisibleVolume = visibleVolume,
				Balance = balance,

				OrderType = type,
				OrderState = state,
				OrderStatus = status,
				TimeInForce = timeInForce,
				IsSystem = isSystem,
				IsUpTick = isUpTick,
				ExpiryDate = expDate,
				Commission = commission,
				PnL = pnl,
				Position = position,
				Slippage = slippage,
				PortfolioName = portfolio,
				ClientCode = clientCode,
				BrokerCode = brokerCode,
				DepoName = depoName,
				UserOrderId = userOrderId,
				Comment = comment,
				SystemComment = sysComment,

				TradeStatus = tradeStatus,

				HasOrderInfo = hasOrderInfo,
				HasTradeInfo = hasTradeInfo,

				OrderPrice = orderPrice ?? 0,
				TradePrice = tradePrice,

				OrderId = orderId,
				TradeId = tradeId,

				OrderBoardId = orderBoardId,
				OrderStringId = orderStringId,
				TradeStringId = tradeStringId,
			};

			var error = ReadString(reader, metaInfo.Errors);

			if (!error.IsEmpty())
				msg.Error = new InvalidOperationException(error);

			msg.Currency = (CurrencyTypes?)reader.ReadNullableInt();

			if (reader.Read())
				msg.Latency = reader.ReadLong().To<TimeSpan>();

			if (reader.Read())
				msg.OriginSide = reader.Read() ? Sides.Buy : Sides.Sell;

			if (metaInfo.Version < MarketDataVersions.Version59)
				return msg;

			msg.LocalTime = ReadItemLocalTime(reader, metaInfo, isTickPrecision);

			if (metaInfo.Version < MarketDataVersions.Version61)
				return msg;

			if (reader.Read())
				msg.IsMarketMaker = reader.Read();

			if (metaInfo.Version < MarketDataVersions.Version62)
				return msg;

			if (reader.Read())
				msg.IsMargin = reader.Read();

			if (metaInfo.Version < MarketDataVersions.Version63)
				return msg;
				
			msg.CommissionCurrency = reader.ReadStringEx();

			return msg;
		}

		private static decimal Write(BitArrayWriter writer, decimal? value, decimal last)
		{
			if (value == null)
			{
				writer.Write(false);
				return last;
			}
			else
			{
				writer.Write(true);
				writer.WriteDecimal((decimal)value, last);

				return value.Value;
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

		private static string ReadString(BitArrayReader reader, IList<string> items)
		{
			if (!reader.Read())
				return null;

			return items[reader.ReadInt()];
		}
	}
}