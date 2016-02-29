#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: QuoteCsvSerializer.cs
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
	/// The quote serializer in the CSV format.
	/// </summary>
	public class QuoteCsvSerializer : CsvMarketDataSerializer<TimeQuoteChange>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteCsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public QuoteCsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		protected override void Write(CsvFileWriter writer, TimeQuoteChange data)
		{
			writer.WriteRow(new[]
			{
				data.ServerTime.UtcDateTime.ToString(TimeFormat),
				data.ServerTime.ToString("zzz"),
				data.Price.ToString(),
				data.Volume.ToString(),
				data.Side.ToString()
			});
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override TimeQuoteChange Read(FastCsvReader reader, DateTime date)
		{
			return new TimeQuoteChange
			{
				ServerTime = reader.ReadTime(date),
				Price = reader.ReadDecimal(),
				Volume = reader.ReadDecimal(),
				Side = reader.ReadEnum<Sides>()
			};
		}
	}
}