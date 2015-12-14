#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: Level1CsvSerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The level 1 serializer in the CSV format.
	/// </summary>
	public class Level1CsvSerializer : CsvMarketDataSerializer<Level1ChangeMessage>
	{
		private static readonly Level1Fields[] _level1Fields = Enumerator.GetValues<Level1Fields>().Where(l1 => l1 != Level1Fields.ExtensionInfo && l1 != Level1Fields.BestAsk && l1 != Level1Fields.BestBid && l1 != Level1Fields.LastTrade).OrderBy(l1 => (int)l1).ToArray();

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1CsvSerializer"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="encoding">Encoding.</param>
		public Level1CsvSerializer(SecurityId securityId, Encoding encoding = null)
			: base(securityId, encoding)
		{
		}

		/// <summary>
		/// Write data to the specified writer.
		/// </summary>
		/// <param name="writer">CSV writer.</param>
		/// <param name="data">Data.</param>
		protected override void Write(TextWriter writer, Level1ChangeMessage data)
		{
			writer.Write($"{data.ServerTime.UtcDateTime.ToString(TimeFormat)};{data.ServerTime.ToString("zzz")}");

			foreach (var field in _level1Fields)
			{
				writer.Write(";");

				switch (field)
				{
					case Level1Fields.BestAskTime:
					case Level1Fields.BestBidTime:
					case Level1Fields.LastTradeTime:
						var date = (DateTimeOffset?)data.Changes.TryGetValue(field);

						if (date != null)
							writer.Write($"{date.Value.UtcDateTime.ToString("yyyyMMdd")};{date.Value.UtcDateTime.ToString(TimeFormat)};{date.Value.ToString("zzz")}");

						break;
					default:
						writer.Write(data.Changes.TryGetValue(field));
						break;
                }
			}
		}

		/// <summary>
		/// Read data from the specified reader.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data.</returns>
		protected override Level1ChangeMessage Read(FastCsvReader reader, DateTime date)
		{
			var level1 = new Level1ChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = reader.ReadTime(date),
			};

			foreach (var field in _level1Fields)
			{
				switch (field)
				{
					case Level1Fields.BestAskTime:
					case Level1Fields.BestBidTime:
					case Level1Fields.LastTradeTime:
                        var dt = reader.ReadNullableDateTime("yyyyMMdd");

						if (dt != null)
						{
							level1.Changes.Add(field, (dt.Value + reader.ReadDateTime(TimeFormat).TimeOfDay).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Replace("+", string.Empty))));
						}

                        break;
					case Level1Fields.LastTradeId:
						var id = reader.ReadNullableLong();

						if (id != null)
							level1.Changes.Add(field, id);

						break;
					case Level1Fields.AsksCount:
					case Level1Fields.BidsCount:
					case Level1Fields.TradesCount:
						var count = reader.ReadNullableLong();

						if (count != null)
							level1.Changes.Add(field, count);

						break;
					case Level1Fields.LastTradeUpDown:
					case Level1Fields.IsSystem:
						var flag = reader.ReadNullableBool();

						if (flag != null)
							level1.Changes.Add(field, flag);

						break;
					default:
						var value = reader.ReadNullableDecimal();

						if (value != null)
							level1.Changes.Add(field, value);

						break;
				}
			}

			return level1;
		}
	}
}