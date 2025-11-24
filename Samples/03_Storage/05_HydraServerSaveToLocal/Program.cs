namespace StockSharp.Samples.Storage.HydraServerSaveToLocal;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Logging;
using Ecng.ComponentModel;

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

		await using var executor = new ChannelExecutor(ex => ConsoleHelper.ConsoleError(ex.ToString()));
		var registry = new CsvEntityRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Storage"), executor);
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
			await securityStorage.SaveAsync(secMsg.ToSecurity(exchangeInfoProvider), false, token);
			Console.WriteLine($"Downloaded [{secMsg.SecurityId}]");
		}

		var securities = securityStorage.LookupAll();

		foreach (var sec in securities)
		{
			Console.WriteLine(sec);
		}

		Console.ReadLine();
		
		var now = DateTime.UtcNow;
		var startDate = now.AddDays(-30);
		var endDate = now;

		var localDrive = new LocalMarketDataDrive(pathHistory);

		const StorageFormats format = StorageFormats.Binary;

		foreach (var dataType in await remoteDrive.GetAvailableDataTypesAsync(secId, format, token))
		{
			var localStorage = storageRegistry.GetStorage(secId, dataType, localDrive, format);
			var remoteStorage = remoteDrive.GetStorageDrive(secId, dataType, format);

			Console.WriteLine($"Remote {dataType}: {(await remoteStorage.GetDatesAsync(token)).FirstOrDefault()}-{(await remoteStorage.GetDatesAsync(token)).LastOrDefault()}");

			var dates = (await remoteStorage.GetDatesAsync(token)).Where(date => date >= startDate && date <= endDate).ToList();

			foreach (var dateTime in dates)
			{
				using (var stream = await remoteStorage.LoadStreamAsync(dateTime, cancellationToken: token))
				{
					if (stream == Stream.Null)
						continue;

					await localStorage.Drive.SaveStreamAsync(dateTime, stream, token);
				}

				Console.WriteLine($"{dataType}={dateTime}");

				var left = 100;
				var localStor = localStorage.LoadAsync(dateTime, token);
				await foreach (var marketDate in localStor)
				{
					Console.WriteLine(marketDate);

					if (--left < 0)
						break;
				}
			}
		}

		Console.ReadLine();
	}
}