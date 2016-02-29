#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: NewsCsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;

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
		protected override void Write(CsvFileWriter writer, NewsMessage data)
		{
			writer.WriteRow(new[]
			{
				data.ServerTime.UtcDateTime.ToString(TimeFormat),
				data.ServerTime.ToString("zzz"),
				data.Headline,
				data.Source,
				data.Url?.ToString(),
				data.Id,
				data.BoardCode,
				data.SecurityId?.SecurityCode
			});
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