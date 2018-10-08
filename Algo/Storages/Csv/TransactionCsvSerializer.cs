#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: TransactionCsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Text;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The transaction serializer in the CSV format.
	/// </summary>
	public class TransactionCsvSerializer : CsvMarketDataSerializer<ExecutionMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionCsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public TransactionCsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		protected override void Write(CsvFileWriter writer, ExecutionMessage data, IMarketDataMetaInfo metaInfo)
		{
			var row = new[]
			{
				data.ServerTime.WriteTimeMls(),
				data.ServerTime.ToString("zzz"),
				data.TransactionId.ToString(),
				data.OriginalTransactionId.ToString(),
				data.OrderId.ToString(),
				data.OrderStringId,
				data.OrderBoardId,
				data.UserOrderId,
				data.OrderPrice.ToString(),
				data.OrderVolume.ToString(),
				data.Balance.ToString(),
				data.VisibleVolume.ToString(),
				data.Side.ToString(),
				data.OriginSide.ToString(),
				data.OrderState.ToString(),
				data.OrderType.ToString(),
				data.TimeInForce.ToString(),
				data.TradeId.ToString(),
				data.TradeStringId,
				data.TradePrice.ToString(),
				data.TradeVolume.ToString(),
				data.PortfolioName,
				data.ClientCode,
				data.BrokerCode,
				data.DepoName,
				data.IsSystem.ToString(),
				data.HasOrderInfo.ToString(),
				data.HasTradeInfo.ToString(),
				data.Commission.ToString(),
				data.Currency.ToString(),
				data.Comment,
				data.SystemComment,
				/*data.DerivedOrderId.ToString()*/string.Empty,
				/*data.DerivedOrderStringId*/string.Empty,
				data.IsUpTick.ToString(),
				data.IsCancelled.ToString(),
				data.OpenInterest.ToString(),
				data.PnL.ToString(),
				data.Position.ToString(),
				data.Slippage.ToString(),
				data.TradeStatus.ToString(),
				data.OrderStatus.ToString(),
				data.Latency?.Ticks.ToString(),
				data.Error?.Message,
				data.ExpiryDate?.WriteDate(),
				data.ExpiryDate?.WriteTimeMls(),
				data.ExpiryDate?.ToString("zzz"),
				data.LocalTime.WriteTimeMls(),
				data.LocalTime.ToString("zzz"),
				data.IsMarketMaker.ToString(),
				data.CommissionCurrency,
			};
			writer.WriteRow(row);

			metaInfo.LastTime = data.ServerTime.UtcDateTime;
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		protected override ExecutionMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
		{
			var msg = new ExecutionMessage
			{
				SecurityId = SecurityId,
				ExecutionType = ExecutionTypes.Transaction,
				ServerTime = reader.ReadTime(metaInfo.Date),
				TransactionId = reader.ReadLong(),
				OriginalTransactionId = reader.ReadLong(),
				OrderId = reader.ReadNullableLong(),
				OrderStringId = reader.ReadString(),
				OrderBoardId = reader.ReadString(),
				UserOrderId = reader.ReadString(),
				OrderPrice = reader.ReadDecimal(),
				OrderVolume = reader.ReadNullableDecimal(),
				Balance = reader.ReadNullableDecimal(),
				VisibleVolume = reader.ReadNullableDecimal(),
				Side = reader.ReadEnum<Sides>(),
				OriginSide = reader.ReadNullableEnum<Sides>(),
				OrderState = reader.ReadNullableEnum<OrderStates>(),
				OrderType = reader.ReadNullableEnum<OrderTypes>(),
				TimeInForce = reader.ReadNullableEnum<TimeInForce>(),
				TradeId = reader.ReadNullableLong(),
				TradeStringId = reader.ReadString(),
				TradePrice = reader.ReadNullableDecimal(),
				TradeVolume = reader.ReadNullableDecimal(),
				PortfolioName = reader.ReadString(),
				ClientCode = reader.ReadString(),
				BrokerCode = reader.ReadString(),
				DepoName = reader.ReadString(),
				IsSystem = reader.ReadNullableBool(),
				HasOrderInfo = reader.ReadBool(),
				HasTradeInfo = reader.ReadBool(),
				Commission = reader.ReadNullableDecimal(),
				Currency = reader.ReadNullableEnum<CurrencyTypes>(),
				Comment = reader.ReadString(),
				SystemComment = reader.ReadString(),
				//DerivedOrderId = reader.ReadNullableLong(),
				//DerivedOrderStringId = reader.ReadString(),
			};

			reader.ReadNullableLong();
			reader.ReadString();

			msg.IsUpTick = reader.ReadNullableBool();
			msg.IsCancelled = reader.ReadBool();
			msg.OpenInterest = reader.ReadNullableDecimal();
			msg.PnL = reader.ReadNullableDecimal();
			msg.Position = reader.ReadNullableDecimal();
			msg.Slippage = reader.ReadNullableDecimal();
			msg.TradeStatus = reader.ReadNullableInt();
			msg.OrderStatus = reader.ReadNullableLong();
			msg.Latency = reader.ReadNullableLong().To<TimeSpan?>();

			var error = reader.ReadString();

			if (!error.IsEmpty())
				msg.Error = new InvalidOperationException(error);

			var dtStr = reader.ReadString();

			if (dtStr != null)
			{
				msg.ExpiryDate = (dtStr.ToDateTime() + reader.ReadString().ToTimeMls()).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Remove("+")));
			}
			else
				reader.Skip(2);

			msg.LocalTime = reader.ReadTime(metaInfo.Date);
			msg.IsMarketMaker = reader.ReadNullableBool();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				msg.CommissionCurrency = reader.ReadString();

			return msg;
		}
	}
}