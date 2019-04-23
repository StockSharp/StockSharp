namespace StockSharp.Algo.History.Russian
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.IO;
	using System.Net;
	using System.Xml.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The class for access to the historical daily data of the FORTS market.
	/// </summary>
	public static class FortsDailyData
	{
		static FortsDailyData()
		{
			UsdRateMinAvailableTime = new DateTime(2009, 11, 2);
		}

		/// <summary>
		/// It returns yesterday's data at the end of day (EOD, End-Of-Day) by the selected instrument.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="securityName">Security name.</param>
		/// <returns>Yesterday's market-data.</returns>
		/// <remarks>
		/// Date is determined by the system time.
		/// </remarks>
		public static Level1ChangeMessage GetSecurityYesterdayEndOfDay(SecurityId securityId, string securityName)
		{
			var time = TimeHelper.Now;
			time -= TimeSpan.FromDays(1);
			return GetSecurityEndOfDay(securityId, securityName, time, time).FirstOrDefault();
		}

		/// <summary>
		/// It returns a list of the data at the end of day (EOD, End-Of-Day) by the selected instrument for the specified period.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="securityName">Security name.</param>
		/// <param name="fromDate">Begin period.</param>
		/// <param name="toDate">End period.</param>
		/// <returns>Historical market-data.</returns>
		public static IEnumerable<Level1ChangeMessage> GetSecurityEndOfDay(SecurityId securityId, string securityName, DateTime fromDate, DateTime toDate)
		{
			if (fromDate > toDate)
				throw new ArgumentOutOfRangeException(nameof(fromDate), fromDate, LocalizedStrings.Str2111Params.Put(fromDate, toDate));

			using (var client = new WebClient())
			{
				var csvUrl = "https://moex.com/en/derivatives/contractresults-exp.aspx?day1={0:yyyyMMdd}&day2={1:yyyyMMdd}&code={2}"
					.Put(fromDate.Date, toDate.Date, securityName);

				var stream = client.OpenRead(csvUrl);

				if (stream == null)
					throw new InvalidOperationException(LocalizedStrings.Str2112);

				return CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var message = new List<Level1ChangeMessage>();

					using (var reader = new StreamReader(stream, StringHelper.WindowsCyrillic))
					{
						reader.ReadLine();

						string newLine;
						while ((newLine = reader.ReadLine()) != null)
						{
							var row = newLine.Split(',');

							var time = row[0].ToDateTime("dd.MM.yyyy");

							message.Add(new Level1ChangeMessage
							{
								ServerTime = time.EndOfDay().ApplyTimeZone(TimeHelper.Moscow),
								SecurityId = securityId,
							}
							.TryAdd(Level1Fields.SettlementPrice, GetPart(row[1]))
							.TryAdd(Level1Fields.AveragePrice, GetPart(row[2]))
							.TryAdd(Level1Fields.OpenPrice, GetPart(row[3]))
							.TryAdd(Level1Fields.HighPrice, GetPart(row[4]))
							.TryAdd(Level1Fields.LowPrice, GetPart(row[5]))
							.TryAdd(Level1Fields.ClosePrice, GetPart(row[6]))
							.TryAdd(Level1Fields.Change, GetPart(row[7]))
							.TryAdd(Level1Fields.LastTradeVolume, GetPart(row[8]))
							.TryAdd(Level1Fields.Volume, GetPart(row[11]))
							.TryAdd(Level1Fields.OpenInterest, GetPart(row[13])));
						}
					}

					return message;
				});
			}
		}

		private static decimal GetPart(string item)
		{
			return !decimal.TryParse(item, out var pardesData) ? 0 : pardesData;
		}

		/// <summary>
		/// The earliest date for which there is an indicative rate of US dollar to the Russian ruble. It is 2 November 2009.
		/// </summary>
		public static DateTime UsdRateMinAvailableTime { get; }

		/// <summary>
		/// To get an indicative exchange rate of a currency pair.
		/// </summary>
		/// <param name="security">Currency pair.</param>
		/// <param name="fromDate">Begin period.</param>
		/// <param name="toDate">End period.</param>
		/// <returns>The indicative rate of US dollar to the Russian ruble.</returns>
		public static IDictionary<DateTimeOffset, decimal> GetRate(Security security, DateTime fromDate, DateTime toDate)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (fromDate > toDate)
				throw new ArgumentOutOfRangeException(nameof(fromDate), fromDate, LocalizedStrings.Str2111Params.Put(fromDate, toDate));

			using (var client = new WebClient())
			{
				var url = "https://moex.com/export/derivatives/currency-rate.aspx?language=en&currency={0}&moment_start={1:yyyy-MM-dd}&moment_end={2:yyyy-MM-dd}"
					.Put(security.Id.ToSecurityId().SecurityCode.Replace("/", TraderHelper.SecurityPairSeparator), fromDate, toDate);

				var stream = client.OpenRead(url);

				if (stream == null)
					throw new InvalidOperationException(LocalizedStrings.Str2112);

				return CultureInfo.InvariantCulture.DoInCulture(() =>
					(from rate in XDocument.Load(stream).Descendants("rate")
					select new KeyValuePair<DateTimeOffset, decimal>(
						rate.GetAttributeValue<string>("moment").ToDateTime("yyyy-MM-dd HH:mm:ss").ApplyTimeZone(TimeHelper.Moscow),
						rate.GetAttributeValue<decimal>("value"))).OrderBy(p => p.Key).ToDictionary());
			}
		}
	}
}