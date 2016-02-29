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
		protected override void Write(CsvFileWriter writer, ExecutionMessage data)
		{
			var row = new[]
			{
				data.ServerTime.UtcDateTime.ToString(TimeFormat),
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
				data.DerivedOrderId.ToString(),
				data.DerivedOrderStringId,
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
				data.ExpiryDate?.UtcDateTime.ToString(DateFormat),
				data.ExpiryDate?.UtcDateTime.ToString(TimeFormat),
				data.ExpiryDate?.ToString("zzz")
			};
			writer.WriteRow(row);
        }

		/// <summary>
		/// Load data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override ExecutionMessage Read(FastCsvReader reader, DateTime date)
		{
			var msg = new ExecutionMessage
			{
				SecurityId = SecurityId,
				ExecutionType = ExecutionTypes.Transaction,
				ServerTime = reader.ReadTime(date),
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
				DerivedOrderId = reader.ReadNullableLong(),
				DerivedOrderStringId = reader.ReadString(),
				IsUpTick = reader.ReadNullableBool(),
				IsCancelled = reader.ReadBool(),
				OpenInterest = reader.ReadNullableDecimal(),
				PnL = reader.ReadNullableDecimal(),
				Position = reader.ReadNullableDecimal(),
				Slippage = reader.ReadNullableDecimal(),
				TradeStatus = reader.ReadNullableInt(),
				OrderStatus = reader.ReadNullableEnum<OrderStatus>(),
				Latency = reader.ReadNullableLong().To<TimeSpan?>(),
			};

			var error = reader.ReadString();

			if (!error.IsEmpty())
				msg.Error = new InvalidOperationException(error);

			var dt = reader.ReadNullableDateTime(DateFormat);

			if (dt != null)
			{
				msg.ExpiryDate = (dt.Value + reader.ReadDateTime(TimeFormat).TimeOfDay).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Replace("+", string.Empty)));
			}
			else
				reader.Skip(2);

			return msg;
		}
	}
}