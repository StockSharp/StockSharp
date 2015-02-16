namespace StockSharp.InteractiveBrokers.Web
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;

	using xNet.Net;
	using xNet.Text;
	using StockSharp.Localization;

	/// <summary>
	/// Загрузчик информации с сайта InteractiveBrokers.
	/// </summary>
	public class Downloader
	{
		private readonly CookieDictionary _cookie = new CookieDictionary();

		/// <summary>
		/// Создать <see cref="Downloader"/>.
		/// </summary>
		public Downloader()
		{
		}

		//private const int KolRet = 5;

		private string MakeRequest(string url) // Get запрос
		{
			using (var request = new HttpRequest { UserAgent = HttpHelper.ChromeUserAgent(), Cookies = _cookie })
			{
				//for (var i = 0; i < KolRet; i++)
				//{
				return request.Get(url).ToString();
				//}

				//return "err ";	
			}
		}

		//private static bool IsMarket(string marketId, IEnumerable<MarketCenter> markets)
		//{
		//	return markets.Any(market => market.Id == marketId);
		//}

		/// <summary>
		/// Скачать информацию по биржам.
		/// </summary>
		/// <returns>Биржи.</returns>
		public IEnumerable<Market> DownloadMarkets()
		{
			const string startUrl = "https://www.interactivebrokers.com.hk/en/index.php?f=products&p=stk";

			var restxt = MakeRequest(startUrl);
			var typeProducts = restxt.Substrings("<ul class=\"ui-tabs-nav\">", "</ul>")[0].Substrings("href=\"", "\"");
			
			foreach (var t2 in typeProducts)
			{
				var urltype = "https://www.interactivebrokers.com.hk" + t2;
				restxt = MakeRequest(urltype);
				//cururl = urltype;

				string[] regions;

				if (restxt.ContainsIgnoreCase("class=\"subtabsmenu\">"))
					regions = restxt.Substrings("<ul class=\"subtabsmenu\">", "</ul>")[0].Substrings("href=\"", "\"");
				else
				{
					regions = new string[1];
					regions[0] = urltype;
				}

				foreach (var region in regions)
				{
					var urlRegion = "https://www.interactivebrokers.com.hk" + region;
					
					//cururl = urlRegion;
					restxt = MakeRequest(urlRegion);
					
					var market = restxt.Substrings("<tr class='linebottom'>", "</tr>"); // Ищем сссылки   Market Center Details
					
					foreach (var t in market)
						yield return DownloadMarket(t, region);
				}
			}
		}

		//private string cururl;

		private static Market DownloadMarket(string marketPage, string country) //Страница  Market
		{
			if (marketPage.ContainsIgnoreCase("comm_table_content lineRightGray"))
				country = marketPage.Substrings("<br><b>", "</b>")[0];

			var market = new Market
			{
				Country = country,
				Name = marketPage.Substrings("ib_entity=hk\">", "</a>")[0],
				//Products = marketPage.Replace("\n", "").Substrings("</a></td><td align='left' valign='top' class='lineRightGray comm_table_content'>", "<br>")[0],
				Id = marketPage.ContainsIgnoreCase("showcategories")
					? marketPage.Substrings("exchanges.php?exch=", "&showcategories")[0]
					: marketPage.Substrings("etfs.php?exch=", "&ib_entity")[0],
				Hours = marketPage.Substrings("comm_table_content'>", "</td>")[2]
			};

			return market;
			//if (!IsMarket(market.Id, markets))
			//	markets.Add(market);
		}

		/// <summary>
		/// Скачать информацию об инструментах биржи.
		/// </summary>
		/// <param name="market">Биржа.</param>
		/// <returns>Инструменты.</returns>
		public IEnumerable<Product> DownloadProducts(Market market) //Страница  Market
		{
			if (market == null)
				throw new ArgumentNullException("market");

			var page = 1;

			while (true)
			{
				var url = "https://www.interactivebrokers.com.hk/en/?f=%2Fen%2Ftrading%2Fexchanges.php%3Fexch%3D"
					+ market.Id + "%26amp%3Bshowcategories%3DSTK%26amp%3Bshowproducts%3D%26amp%3Bsequence_idx%3D"
					+ page + "00%26amp%3Bsortproducts%3D%26amp%3Bib_entity%3Dhk#show";

				var restxt = MakeRequest(url);

				if (restxt.ContainsIgnoreCase("No result for this combination"))
					yield break;

				page++;

				var body = restxt.Substrings("<h2>Stocks</h2>", "</table>")[0];
				var stocks = body.Substrings("<tr class=\"linebottom\">", "</tr>");

				foreach (var t in stocks)
				{
					yield return new Product
					{
						ContractId = t.Substrings("&conid=", "'")[0].To<long>(),
						Name = t.Substrings(";\">", "</a>")[0]
					};
				}
			}
		}

		/// <summary>
		/// Скачать детальную информацию об инструменте.
		/// </summary>
		/// <param name="product">Инструмент.</param>
		/// <returns>Детальная информация.</returns>
		public ProductDescripton DownloadDescription(Product product) // Страница с описание акции  Specifications
		{
			if (product == null)
				throw new ArgumentNullException("product");

			var url = "http://www1.interactivebrokers.ch/contract_info/v3.8/index.php?action=Details&site=GEN&conid="
				+ product.ContractId;

			var restxt = MakeRequest(url);

			// если картинка типа каптча ))
			if (restxt.ContainsIgnoreCase("To continue please enter the text from the image below"))
			{
				var captha = restxt.Substrings("image.php?str=", "\"")[0];
				url = "http://www1.interactivebrokers.ch/contract_info/v3.8/index.php?filter=" + captha
					+ "&action=Conid+Info&lang=en&wlId=GEN&conid=" + product.ContractId;
				restxt = MakeRequest(url);
			}

			restxt = restxt.Replace("\n", string.Empty);

			return CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var description = new ProductDescripton
				{
					ClosingPrice = restxt.Substrings("Closing Price</td><td>", "<")[0].To<decimal>(),
					Name = restxt.Substrings("Description/Name</td><td>", "</td>")[1],
					Symbol = restxt.Substrings("class=\"rowhead\">Symbol</td><td>", "</td>")[0],
					Type = ToSecurityType(restxt.Substrings("class=\"white\"><td class=\"rowhead\">Contract Type</td><td>", "</td>")[0]),
					Country = restxt.Substrings("Country/Region</td>", "/td></tr>")[0].Substrings("\">", "<")[0],
					Currency = restxt.Substrings("Currency</td><td>", "<")[0],
					AssetId = restxt.Substrings("ASSETID</td><td>", "</td>")[0],
					StockType = restxt.Substrings("Stock Type</td><td>", "</td>")[0],
					InitialMargin = restxt.Substrings("Initial Margin</td><td>", "</td>")[0],
					MaintenanceMargin = restxt.Substrings("Maintenance Margin</td><td>", "</td>")[0],
					ShortMargin = restxt.Substrings("Short Margin</td><td>", "</td>")[0]
				};

				return description;
			});
		}

		private static SecurityTypes ToSecurityType(string type)
		{
			switch (type)
			{
				case "Stock":
					return SecurityTypes.Stock;
				case "Option":
					return SecurityTypes.Option;
				case "Futures":
					return SecurityTypes.Future;
				case "Forex":
					return SecurityTypes.Currency;
				case "Commodity (Physical)":
					return SecurityTypes.Commodity;
				case "Indices/ETFs":
					return SecurityTypes.Index;
				case "Contract for Difference (CFD)":
					return SecurityTypes.Cfd;
				default:
					throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.Str1603);
			}
		}

		/// <summary>
		/// Скачать площадки.
		/// </summary>
		/// <param name="product">Инструмент.</param>
		/// <returns>Площадки.</returns>
		public IEnumerable<ProductBoard> DownloadBoards(Product product)
		{
			if (product == null)
				throw new ArgumentNullException("product");

			var url = "http://www1.interactivebrokers.ch/contract_info/v3.8/index.php?action=Details&site=GEN&conid="
				+ product.ContractId;

			var restxt = MakeRequest(url);

			// если картинка типа каптча ))
			if (restxt.ContainsIgnoreCase("To continue please enter the text from the image below"))
			{
				var captha = restxt.Substrings("image.php?str=", "\"")[0];

				url = "http://www1.interactivebrokers.ch/contract_info/v3.8/index.php?filter=" + captha
					+ "&action=Conid+Info&lang=en&wlId=GEN&conid=" + product.ContractId;

				restxt = MakeRequest(url);
			}

			var seller = restxt
				.Replace("\n", string.Empty)
				.Substrings("<a name=", "/center></td></tr></table>");

			return seller.Select(WorkSeller);
		}

		private static ProductBoard WorkSeller(string sel) // Продавец
		{
			var seller = new ProductBoard
			{
				Markets = sel.Substrings("\"", "\"")[0],
				Id = sel.Substrings("\"", "\"")[0],
				Name = sel.Substrings("Local Name</td><td>", "</td>")[0],
				Class = sel.Substrings("Local Class</td><td>", "</td>")[0],
				SettlementMethod = sel.Substrings("Settlement Method</td><td>", "</td>")[0],
				ExchangeWebsite = sel.ContainsIgnoreCase("Exchange Website</td><td><a href=\"")
					? sel.Substrings("Exchange Website</td><td><a href=\"", "\"")[0]
					: string.Empty,
				TradingHours = sel.Substrings("Sat</th></tr><tr>", "</tr>")[0].Substrings("<td>", "</td>")
			};

			var range1 = sel.Substrings("Increment</th></tr><tr align=\"center\">", "</tr></table>")[0];
			var range2 = sel.Substrings("Increment</th></tr><tr align=\"center\">", "</tr></table>")[1];

			seller.PriceRange1 = range1.Substrings("<td>", "</td>")[0];
			seller.PriceRange2 = range1.Substrings("<td>", "</td>")[2];
			seller.PriceRange3 = range2.Substrings("<td>", "</td>")[0];

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				seller.VolumeRange1 = range1.Substrings("<td>", "</td>")[1].To<decimal>();
				seller.VolumeRange2 = range1.Substrings("<td>", "</td>")[3].To<decimal>();
				seller.VolumeRange3 = range2.Substrings("<td>", "</td>")[1].To<decimal>();
			});

			return seller;
		}

		//public int calcMarket(ArrayList ProductList)
		// {
		//   HashSet <string> marketSet = new HashSet<string>();
		// foreach (ProductDescripton p in ProductList)
		// {
		//   for (int i = 0; i < p.market.Length; i++) marketSet.Add(p.market[i].uniqueMarket);
		// }
		// return marketSet.Count;
		// }
	}
}