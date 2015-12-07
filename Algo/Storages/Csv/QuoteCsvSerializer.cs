namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.IO;
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
		protected override void Write(TextWriter writer, TimeQuoteChange data)
		{
			writer.Write($"{data.ServerTime.UtcDateTime.ToString(TimeFormat)};{data.ServerTime.ToString("zzz")};{data.Price};{data.Volume};{data.Side}");
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