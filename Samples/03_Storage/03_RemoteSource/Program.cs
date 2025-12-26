namespace StockSharp.Samples.Storage.RemoteSource;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Configuration;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Finam;
using StockSharp.Messages;
using StockSharp.Web.Api.Client;
using StockSharp.Web.Api.Interfaces;

static class Program
{
	private static async Task Main()
	{
		var ct = CancellationToken.None;

		ICredentialsProvider credProvider = new DefaultCredentialsProvider();

		string token;

		if (!credProvider.TryLoad(out var credentials))
		{
			Console.WriteLine("Enter token (visit https://stocksharp.com/profile/ ):");
			token = Console.ReadLine();
		}
		else
			token = credentials.Token.UnSecure();

		if (token.IsEmpty())
			throw new InvalidOperationException("Token is empty.");

		var webApiProvider = new ApiServiceProvider();
		ConfigManager.RegisterService(webApiProvider.GetService<IInstrumentInfoService>(token));

		var connector = new Connector();
		connector.SubscriptionsOnConnect.Clear();

		var messageAdapter = new FinamMessageAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(messageAdapter);
		connector.Connect();

		//--------------------------Security--------------------------------------------------------------------------------
		Console.WriteLine("Security:");
		connector.LookupSecuritiesResult += (message, securities, arg3) =>
		{
			foreach (var security1 in securities)
			{
				Console.WriteLine(security1);
			}
		};
		connector.Subscribe(new(new SecurityLookupMessage() { SecurityId = new() { SecurityCode = "BTCUSD" } }));
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var security = connector.GetSecurity(secId);

		Console.ReadLine();

		////--------------------------Candles--------------------------------------------------------------------------------
		Console.WriteLine("Candles:");
		var candles = new List<CandleMessage>();
		connector.CandleReceived += (series, candle) =>
		{
			Console.WriteLine(candle);
			candles.Add((CandleMessage)candle);
		};

		var now = DateTime.UtcNow;

		connector.Subscribe(new(TimeSpan.FromMinutes(15).TimeFrame(), security)
		{
			From = now.AddDays(-3),
			To = now.AddDays(-1),
		});

		Console.ReadLine();

		//---------------------------------------------------------------------------------------------
		const string pathHistory = "Storage";
		pathHistory.SafeDeleteDir();

		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, pathHistory)
		};

		//------------------------------Save---------------------------------------------------
		Console.WriteLine("Saving...");
		var candlesStorageCsv = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Csv);
		await candlesStorageCsv.SaveAsync(candles, ct);
		var candlesStorageBin = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Binary);
		await candlesStorageBin.SaveAsync(candles, ct);
		Console.WriteLine("Save done!");

		Console.ReadLine();

		//------------------------------Delete---------------------------------------------------
		Console.WriteLine("Deleting...");
		await candlesStorageCsv.DeleteAsync(candles.First().OpenTime, candles.Last().CloseTime, ct);
		await candlesStorageBin.DeleteAsync(candles.First().OpenTime, candles.Last().CloseTime, ct);
		Console.WriteLine("Delete done!");

		Console.ReadLine();
	}
}