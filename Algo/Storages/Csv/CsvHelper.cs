#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Csv.Algo
File: CsvHelper.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Csv
{
	using System;

	using Ecng.Common;

	static class CsvHelper
	{
		/// <summary>
		/// <see cref="TimeSpan"/> format.
		/// </summary>
		public const string TimeFormat = "HHmmssfff";

		/// <summary>
		/// Read <see cref="DateTimeOffset"/>.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns><see cref="DateTimeOffset"/>.</returns>
		public static DateTimeOffset ReadTime(this FastCsvReader reader, DateTime date)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			return (date + reader.ReadDateTime(TimeFormat).TimeOfDay).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Replace("+", string.Empty)));
		}
	}
}