using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using StockSharp.Algo.Storages;
using StockSharp.Configuration;
using StockSharp.Messages; // Annahme, dass du eine bestimmte Bibliothek für MarketData nutzt

class Program
{
	static async Task Main(string[] args)
	{
		var symbol = "BTCEUR";  // Das Währungspaar, z.B. Bitcoin in Euro
		var interval = "1d";    // Zeitintervall: 1 Minute
		string id = "BTC-EUR@CNBS";

		// Binance Candles abrufen
		var candles = await GetBinanceCandles(symbol, interval);

		// StorageRegistry und lokalen Speicher einrichten
		var pathHistory = Paths.HistoryDataPath; // Pfad zu den historischen Daten
		var localDrive = new LocalMarketDataDrive(pathHistory);
		var storageRegistry = new StorageRegistry()
		{
			DefaultDrive = localDrive,
		};

		// Umwandlung und Speicherung der Candle-Daten im Storage
		var securityId = id.ToSecurityId();  // Symbol-Konvertierung zu deinem Format
		var candleStorage = storageRegistry.GetTimeFrameCandleMessageStorage(securityId, TimeSpan.FromMinutes(1));

		// Liste für die zu speichernden Candles erstellen
		var candleList = new List<TimeFrameCandleMessage>();

		foreach (var candle in candles)
		{
			// Binance-Candle in dein Format umwandeln
			var convertedCandle = ConvertToCandleMessage(candle);
			candleList.Add(convertedCandle);
			Console.WriteLine(convertedCandle.OpenPrice+" "+convertedCandle.CloseTime);
		}

		// Candles speichern
		candleStorage.Save(candleList);
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

	// Methode, um eine Binance-Candle in dein Candle-Format umzuwandeln
	public static TimeFrameCandleMessage ConvertToCandleMessage(JToken binanceCandle)
	{
// Binance Candle-Format: 
		// [0] open time, [1] open price, [2] high price, [3] low price, [4] close price, [5] volume, [6] close time, etc.
	
		return new TimeFrameCandleMessage
		{
			OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)binanceCandle[0]),  // Candle open time
			CloseTime = DateTimeOffset.FromUnixTimeMilliseconds((long)binanceCandle[6]), // Candle close time
			OpenPrice = Convert.ToDecimal(binanceCandle[1]),   // Open price
			HighPrice = Convert.ToDecimal(binanceCandle[2]),   // High price
			LowPrice = Convert.ToDecimal(binanceCandle[3]),    // Low price
			ClosePrice = Convert.ToDecimal(binanceCandle[4]),  // Close price
			TotalVolume = Convert.ToDecimal(binanceCandle[5]), // Total volume during the candle
		
			// Optional: Set these fields if needed, or leave them with defaults
			HighTime = DateTimeOffset.FromUnixTimeMilliseconds((long)binanceCandle[6]), // Placeholder
			LowTime = DateTimeOffset.FromUnixTimeMilliseconds((long)binanceCandle[6]),  // Placeholder
			State = CandleStates.Finished,  // Assuming the candle is complete when fetched
			PriceLevels = null, // Binance API doesn't provide detailed price levels
			DataType = DataType.CandleTimeFrame, // Assuming it's time frame based
			OpenVolume = null, // Binance does not provide these details directly
			CloseVolume = Convert.ToDecimal(binanceCandle[5]), // Use volume as the close volume
			HighVolume = null, // No specific high-volume data in Binance candles
			LowVolume = null,
			RelativeVolume = null,
			BuyVolume = null,  // If you have this data, otherwise set to null
			SellVolume = null, // If you have this data, otherwise set to null
			TotalTicks = null, // Binance doesn't provide ticks directly
			UpTicks = null,
			DownTicks = null,
			OpenInterest = null
		};
	}
}
