#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: CandleCsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp Algo Trading, LLC
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
	/// The candle serializer in the CSV format.
	/// </summary>
	public class CandleCsvSerializer<TCandleMessage> : CsvMarketDataSerializer<TCandleMessage>
		where TCandleMessage : CandleMessage, new()
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleCsvSerializer{TCandleMessage}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="encoding">Encoding.</param>
		public CandleCsvSerializer(SecurityId securityId, object arg, Encoding encoding = null)
			: base(securityId, encoding)
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			Arg = arg;
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public object Arg { get; set; }

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		protected override void Write(TextWriter writer, TCandleMessage data)
		{
			writer.Write($"{data.OpenTime.UtcDateTime.ToString(TimeFormat)};{data.OpenTime.ToString("zzz")};{data.OpenPrice};{data.HighPrice};{data.LowPrice};{data.ClosePrice};{data.TotalVolume}");
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override TCandleMessage Read(FastCsvReader reader, DateTime date)
		{
			return new TCandleMessage
			{
				SecurityId = SecurityId,
				Arg = Arg,
				OpenTime = reader.ReadTime(date),
				OpenPrice = reader.ReadDecimal(),
				HighPrice = reader.ReadDecimal(),
				LowPrice = reader.ReadDecimal(),
				ClosePrice = reader.ReadDecimal(),
				TotalVolume = reader.ReadDecimal(),
			};
		}
	}
}