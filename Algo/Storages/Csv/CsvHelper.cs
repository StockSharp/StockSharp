namespace StockSharp.Algo.Storages.Csv
{
	using System;

	using Ecng.Common;

	static class CsvHelper
	{
		/// <summary>
		/// <see cref="DateTime"/> format.
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