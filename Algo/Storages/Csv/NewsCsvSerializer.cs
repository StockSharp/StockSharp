namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.IO;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The news serializer in the CSV format.
	/// </summary>
	public class NewsCsvSerializer : CsvMarketDataSerializer<NewsMessage>
	{
		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		protected override void Write(TextWriter writer, NewsMessage data)
		{
			writer.Write($"{data.ServerTime.UtcDateTime.ToString(TimeFormat)};{data.ServerTime.ToString("zzz")};{data.Headline};{data.Source};{data.Url};{data.Id};{data.BoardCode};{data.SecurityId?.SecurityCode}");
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override NewsMessage Read(FastCsvReader reader, DateTime date)
		{
			var news = new NewsMessage
			{
				ServerTime = reader.ReadTime(date),
				Headline = reader.ReadString(),
				Source = reader.ReadString(),
				Url = reader.ReadString().To<Uri>(),
				Id = reader.ReadString(),
				BoardCode = reader.ReadString(),
			};

			var secCode = reader.ReadString();

			if (!secCode.IsEmpty())
				news.SecurityId = new SecurityId { SecurityCode = secCode };

			return news;
		}
	}
}