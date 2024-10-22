using Custom;
using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Newtonsoft.Json.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

internal class Program
{
	private static Connector? connector;

	#region Methods

	private static async Task Main()
	{


		var symbol = "BTCEUR";  // Das Währungspaar, z.B. Bitcoin in Euro
		var interval = "1m";    // Zeitintervall: 1 Minute



		// Creating StorageRegistry with the path to data from the NuGet package
		var pathHistory = Paths.HistoryDataPath; // path to data from the NuGet package
		var localDrive = new LocalMarketDataDrive(pathHistory);
		var storageRegistry = new StorageRegistry()
		{
			DefaultDrive = localDrive,
		};

		


		var candles = await GetBinanceCandles(symbol, interval);
		foreach (var candle in candles)
		{
			Console.WriteLine(candle);
		}

	





	}
	public static async Task<JArray> GetBinanceCandles(string symbol, string interval)
	{
		var client = new HttpClient();
		var endpoint = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}";

		HttpResponseMessage response = await client.GetAsync(endpoint);
		if (response.IsSuccessStatusCode)
		{
			var json = await response.Content.ReadAsStringAsync();
			return JArray.Parse(json);
		}
		else
		{
			throw new Exception("Failed to retrieve data from Binance API.");
		}
	}
    private static void Connector_Connected()
    {
	    connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);
    }

    private static void ListingAvailableSecurities()
	{
		var pathHistory = Paths.HistoryDataPath;
		var localDrive = new LocalMarketDataDrive(pathHistory);
		var securities = localDrive.AvailableSecurities;
		foreach (var sec in securities)
		{
			Console.WriteLine(sec);
		}
		Console.ReadLine();
	}

	#endregion
}