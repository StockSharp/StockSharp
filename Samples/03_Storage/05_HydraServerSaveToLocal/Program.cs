namespace StockSharp.Samples.Storage.HydraServerSaveToLocal;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Messages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.Fix;
using StockSharp.BusinessEntities;

static class Program
{
	static async Task Main()
	{
		var token = CancellationToken.None;

		const string pathHistory = "Storage";

		if (Directory.Exists(pathHistory))
			IOHelper.ClearDirectory(pathHistory);
		else
			Directory.CreateDirectory(pathHistory);

		var logger = new LogManager();
		logger.Listeners.Add(new ConsoleLogListener());

		var storageRegistry = new StorageRegistry();

		var registry = new CsvEntityRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Storage"));
		var securityStorage = (ISecurityStorage)registry.Securities;

		var remoteDrive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, new FixMessageAdapter(new IncrementalIdGenerator()))
		{
			Logs = logger.Application,

			Credentials =
			{
				Email = "hydra_user",

				//
				// required for non anonymous access
				//
				//Password = "hydra_user".To<SecureString>()
			},

			//
			// uncomment to enable binary mode
			//
			//IsBinaryEnabled = true,
		};

		var secId = new SecurityId
		{
			SecurityCode = "BTCUSDT",
			BoardCode = BoardCodes.BinanceFut,
		};

		//----------------------------------Security------------------------------------------------------------------
		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		await foreach (var secMsg in remoteDrive.LookupSecuritiesAsync(new() { SecurityId = secId }, registry.Securities, token).WithEnforcedCancellation(token))
		{
			securityStorage.Save(secMsg.ToSecurity(exchangeInfoProvider), false);
			Console.WriteLine($"Downloaded [{secMsg.SecurityId}]");
		}

		var securities = securityStorage.LookupAll();

		foreach (var sec in securities)
		{
			Console.WriteLine(sec);
		}

		Console.ReadLine();
		
		var now = DateTimeOffset.UtcNow;
		var startDate = now.AddDays(-30);
		var endDate = now;

		var localDrive = new LocalMarketDataDrive(pathHistory);

		const StorageFormats format = StorageFormats.Binary;

		foreach (var dataType in await remoteDrive.GetAvailableDataTypesAsync(secId, format, token))
		{
			var localStorage = storageRegistry.GetStorage(secId, dataType, localDrive, format);
			var remoteStorage = remoteDrive.GetStorageDrive(secId, dataType, format);

			Console.WriteLine($"Remote {dataType}: {remoteStorage.Dates.FirstOrDefault()}-{remoteStorage.Dates.LastOrDefault()}");

			var dates = remoteStorage.Dates.Where(date => date >= startDate && date <= endDate).ToList();

			foreach (var dateTime in dates)
			{
				using (var stream = remoteStorage.LoadStream(dateTime))
				{
					if (stream == Stream.Null)
						continue;

					localStorage.Drive.SaveStream(dateTime, stream);
				}

				Console.WriteLine($"{dataType}={dateTime}");

				var localStor = localStorage.Load(dateTime);
				foreach (var marketDate in localStor.Take(100))
				{
					Console.WriteLine(marketDate);
				}
			}
		}

		Console.ReadLine();
	}
}