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
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The news serializer in the CSV format.
	/// </summary>
	public class NewsCsvSerializer : CsvMarketDataSerializer<NewsMessage>
	{
		private const string _expiryFormat = "yyyyMMddHHmmssfff zzz";

		/// <inheritdoc />
		protected override void Write(CsvFileWriter writer, NewsMessage data, IMarketDataMetaInfo metaInfo)
		{
			writer.WriteRow(new[]
			{
				data.ServerTime.WriteTimeMls(),
				data.ServerTime.ToString("zzz"),
				data.Headline,
				data.Source,
				data.Url,
				data.Id,
				data.BoardCode,
				data.SecurityId?.SecurityCode,
				data.Priority?.To<string>(),
				data.Language,
				data.SecurityId?.BoardCode,
				data.ExpiryDate?.ToString(_expiryFormat),
				data.SeqNum.DefaultAsNull().ToString(),
			});

			metaInfo.LastTime = data.ServerTime.UtcDateTime;
		}

		/// <inheritdoc />
		protected override NewsMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
		{
			var news = new NewsMessage
			{
				ServerTime = reader.ReadTime(metaInfo.Date),
				Headline = reader.ReadString(),
				Source = reader.ReadString(),
				Url = reader.ReadString(),
				Id = reader.ReadString(),
				BoardCode = reader.ReadString(),
			};

			var secCode = reader.ReadString();

			if (!secCode.IsEmpty())
				news.SecurityId = new SecurityId { SecurityCode = secCode };

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				news.Priority = reader.ReadNullableEnum<NewsPriorities>();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				news.Language = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				var boardCode = reader.ReadString();

				if (news.SecurityId != null)
				{
					var secId = news.SecurityId.Value;
					secId.BoardCode = boardCode;
					news.SecurityId = secId;
				}
			}

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				news.ExpiryDate = reader.ReadString().TryToDateTimeOffset(_expiryFormat);

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				news.SeqNum = reader.ReadNullableLong() ?? 0L;

			return news;
		}
	}
}