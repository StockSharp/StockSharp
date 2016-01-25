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
	using System.IO;
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
		protected override void Write(TextWriter writer, ExecutionMessage data)
		{
			writer.Write($"{data.ServerTime.UtcDateTime.ToString(TimeFormat)};{data.ServerTime.ToString("zzz")};{data.TransactionId};{data.OrderId};{data.OrderPrice};{data.OrderVolume};{data.Side};{data.OrderState};{data.TimeInForce};{data.TradeId};{data.TradePrice};{data.TradeVolume};{data.PortfolioName};{data.IsSystem};{data.HasOrderInfo};{data.HasTradeInfo}");
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
				OrderId = reader.ReadLong(),
				OrderStringId = reader.ReadString(),
				OrderBoardId = reader.ReadString(),
				UserOrderId = reader.ReadString(),
				OrderPrice = reader.ReadDecimal(),
				OrderVolume = reader.ReadDecimal(),
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
				TradeVolume = reader.ReadDecimal(),
				PortfolioName = reader.ReadString(),
				ClientCode = reader.ReadString(),
				DepoName = reader.ReadString(),
				IsSystem = reader.ReadNullableBool(),
				HasOrderInfo = reader.ReadBool(),
				HasTradeInfo = reader.ReadBool(),
				Commission = reader.ReadNullableDecimal(),
				Currency = reader.ReadNullableEnum<CurrencyTypes>(),
				Comment = reader.ReadString(),
				DerivedOrderId = reader.ReadNullableLong(),
				DerivedOrderStringId = reader.ReadString(),
				IsUpTick = reader.ReadNullableBool(),
				IsCancelled = reader.ReadBool(),
				OpenInterest = reader.ReadNullableDecimal(),
				PnL = reader.ReadNullableDecimal(),
				Position = reader.ReadNullableDecimal(),
				Slippage = reader.ReadNullableDecimal(),
				SystemComment = reader.ReadString(),
				TradeStatus = reader.ReadNullableInt(),
				OrderStatus = reader.ReadNullableEnum<OrderStatus>(),
			};

			var error = reader.ReadString();

			if (!error.IsEmpty())
				msg.Error = new InvalidOperationException(error);

			return msg;
		}
	}
}