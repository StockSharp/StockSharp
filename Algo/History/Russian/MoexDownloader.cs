namespace StockSharp.Algo.History.Russian
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using Newtonsoft.Json;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The helper class that filling fields in <see cref="Security.PriceStep"/> and <see cref="Security.VolumeStep"/> based on information from the site https://moex.com.
	/// </summary>
	public static class MoexDownloader
	{
		private static readonly HashSet<string> _ignoreCodes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		static MoexDownloader()
		{
			_ignoreCodes.AddRange(Properties.Resources.IgnoreCodes.Split());

			//DownloadInfo();
		}

		private static void DownloadInfo()
		{
			// https://iss.moex.com/iss/engines/stock/markets/shares/securities.json
			// https://iss.moex.com/iss/engines/futures/markets/forts/securities.json
			// https://iss.moex.com/iss/engines/futures/markets/options/securities.json
			// https://iss.moex.com/iss/securities/BRX0.jsonp

			var types = new[]
			{
				Tuple.Create("stock", "shares"),
				Tuple.Create("stock", "index"),
				Tuple.Create("stock", "bonds"),
				Tuple.Create("futures", "forts"),
				//Tuple.Create("futures", "options"),
				Tuple.Create("commodity", "futures"),
				Tuple.Create("currency", "futures"),
			};

			foreach (var tuple in types)
			{
				dynamic tikerInfo = JsonConvert.DeserializeObject(DownloadMicex($"https://iss.moex.com/iss/engines/{tuple.Item1}/markets/{tuple.Item2}/securities.json"));

				var codeInd = -1;
				var boardInd = -1;
				var shortNameInd = -1;
				var nameInd = -1;
				var lotSizeInd = -1;
				var isinInd = -1;
				var issueSizeInd = -1;
				var issueDateInd = -1;
				var priceStepInd = -1;
				var decimalsInd = -1;
				var lastTrdDateInd = -1;
				var settleDateInd = -1;
				var assetInd = -1;
				var currInd = -1;

				var ind = 0;

				foreach (string column in tikerInfo.securities.columns)
				{
					switch (column)
					{
						case "SECID":
							codeInd = ind;
							break;

						case "BOARDID":
							boardInd = ind;
							break;

						case "SHORTNAME":
							shortNameInd = ind;
							break;

						case "SECNAME":
							nameInd = ind;
							break;

						case "DECIMALS":
							decimalsInd = ind;
							break;

						case "MINSTEP":
							priceStepInd = ind;
							break;

						case "LASTTRADEDATE":
							lastTrdDateInd = ind;
							break;

						case "ASSETCODE":
							assetInd = ind;
							break;

						case "LOTVOLUME":
							lotSizeInd = ind;
							break;

						case "ISSUESIZE":
							issueSizeInd = ind;
							break;

						case "ISSUEDATE":
							issueDateInd = ind;
							break;

						case "ISIN":
							isinInd = ind;
							break;

						case "CURRENCYID":
							currInd = ind;
							break;

						case "SETTLEDATE":
							settleDateInd = ind;
							break;
					}

					ind++;
				}

				foreach (var item in tikerInfo.securities.data)
				{
					var code = (string)item[codeInd];
					var board = (string)item[boardInd];
					var shortName = (string)Get<string>(item, shortNameInd);

					var prevInfo = SecurityInfoCache.TryGetByCodeAndBoard(code, board);

					if (prevInfo?.Length > 0 && prevInfo.Any(i => i.ShortName.CompareIgnoreCase(shortName) || i.Name.CompareIgnoreCase(shortName)))
						continue;

					var type = tuple.Item2;

					if (type == "forts")
						type = "futures";

					SecurityInfoCache.Add(new SecurityInfo
					{
						Code = code,
						Board = board,
						Type = type,
						ShortName = shortName,
						Name = Get<string>(item, nameInd),
						Decimals = Get<int?>(item, decimalsInd),
						PriceStep = Get<decimal?>(item, priceStepInd),
						LastDate = ((string)Get<string>(item, lastTrdDateInd)).TryToDateTime("yyyy-MM-dd", CultureInfo.InvariantCulture),
						Asset = Get<string>(item, assetInd),
						Multiplier = Get<decimal?>(item, lotSizeInd),
						Currency = Get<string>(item, currInd),
						Isin = Get<string>(item, isinInd),
						IssueSize = Get<decimal?>(item, issueSizeInd),
						IssueDate = ((string)Get<string>(item, issueDateInd)).TryToDateTime("yyyy-MM-dd", CultureInfo.InvariantCulture),
						SettleDate = ((string)Get<string>(item, settleDateInd)).TryToDateTime("yyyy-MM-dd", CultureInfo.InvariantCulture),
					});
				}
			}
		}

		private static T Get<T>(dynamic item, int index)
		{
			if (index == -1)
				return default(T);

			return (T)item[index];
		}

		/// <summary>
		/// To get securities for the instrument code.
		/// </summary>
		/// <param name="code">Instrument code.</param>
		/// <param name="type">Security type.</param>
		/// <param name="isDownload">Whether to download information from the site, if it is not found locally.</param>
		/// <returns>Found securities.</returns>
		public static IEnumerable<SecurityInfo> GetSecurities(string code, SecurityTypes? type, bool isDownload = true)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException(nameof(code));

			if (_ignoreCodes.Contains(code))
				return Enumerable.Empty<SecurityInfo>();

			var info = SecurityInfoCache.TryGetByCode(code);

			if (info?.Length > 0)
			{
				if (type != null)
					info = info.Where(i => i.GetSecurityType() == type).ToArray();

				return info;
			}

			var si = SecurityInfoCache.TryGetByShortName(code);
			if (si != null)
			{
				return new[] { si };
			}

			if (!isDownload)
				return Enumerable.Empty<SecurityInfo>();

			var tplus = code.EndsWithIgnoreCase(".T+");
			if (tplus)
				code = code.Remove(code.Length - 3);

			var securities = EnsureDownload(code);

			return securities;
		}

		private static IEnumerable<SecurityInfo> EnsureDownload(string code)
		{
			var tiker = new SecurityInfo { Code = code };
			dynamic tikerInfo = JsonConvert.DeserializeObject(DownloadMicex($"https://iss.moex.com/iss/securities/{tiker.Code}.jsonp"));

			foreach (var item in tikerInfo.description.data)
			{
				var value = item[2];

				switch (((string)item[0]).ToUpperInvariant())
				{
					case "NAME":
						tiker.Name = value;
						break;
					case "SHORTNAME":
						tiker.ShortName = value;
						break;
					case "ASSETCODE":
						tiker.Asset = value;
						break;
					case "ISIN":
						tiker.Isin = value;
						break;
					case "LSTTRADE":
						tiker.LastDate = ((string)value).TryToDateTime("yyyy-MM-dd", CultureInfo.InvariantCulture);
						break;
					case "ISSUEDATE":
						tiker.IssueDate = ((string)value).TryToDateTime("yyyy-MM-dd", CultureInfo.InvariantCulture);
						break;
					case "ISSUESIZE":
						tiker.IssueSize = (decimal)value;
						break;
					case "TYPE":
						tiker.Type = value;
						break;
				}
			}

			var boardIdIdx = -1;
			var marketIdx = -1;
			var engineIdx = -1;

			var idx = 0;

			foreach (var item in tikerInfo.boards.columns)
			{
				switch (((string)item).ToLowerInvariant())
				{
					case "boardid":
						boardIdIdx = idx;
						break;
					case "market":
						marketIdx = idx;
						break;
					case "engine":
						engineIdx = idx;
						break;
				}

				idx++;
			}

			var securities = new List<SecurityInfo>();

			foreach (var boardItem in tikerInfo.boards.data)
			{
				dynamic secInfo = JsonConvert.DeserializeObject(DownloadMicex($"https://iss.moex.com/iss/engines/{boardItem[engineIdx]}/markets/{boardItem[marketIdx]}/boards/{boardItem[boardIdIdx]}/securities/{code}.jsonp"));

				var security = tiker.Clone();
				security.Board = (string)boardItem[boardIdIdx];

				if (secInfo.securities.data.Count > 0)
				{
					var data = secInfo.securities.data[0];

					idx = 0;

					foreach (var item in secInfo.securities.columns)
					{
						var value = data[idx];

						switch (((string)item).ToUpperInvariant())
						{
							case "LOTSIZE":
								security.Multiplier = (int)value;
								break;
							case "DECIMALS":
								security.Decimals = (int)value;
								break;
							case "NAME":
								if (security.Name.IsEmpty())
									security.Name = (string)value;
								break;
							case "SHORTNAME":
								if (security.ShortName.IsEmpty())
									security.ShortName = (string)value;
								break;
							case "ASSETCODE":
								if (security.Asset.IsEmpty())
									security.Asset = (string)value;
								break;
							case "MINSTEP":
								// http://stocksharp.com/forum/yaf_postsm8573_Gidra-i-Finam.aspx#post8573
								// can be either 0.1 or 1e-6. so parse it as a double
								security.PriceStep = (decimal)double.Parse((string)value, CultureInfo.InvariantCulture);
								break;
							case "CURRENCYID":
								security.Currency = (string)value;
								break;

						}

						idx++;
					}
				}

				SecurityInfoCache.Add(security);

				securities.Add(security);
			}

			//if (securities.Count == 0)
			//	System.Diagnostics.Debug.WriteLine(code);

			return securities;
		}

		private static string DownloadMicex(string url)
		{
			using (var client = new WebClient { Encoding = Encoding.UTF8 })
				return client.DownloadString(url);
		}
	}
}