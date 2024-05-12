using System;
using System.IO;
using System.Linq;
using System.Security;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Messages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.Fix;

namespace RemoteStorage_save_to_local_storage;

static class Program
{
	static void Main()
	{
		var storageRegistry = new StorageRegistry();

		var registry = new CsvEntityRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Storage"));
		var securityStorage = (ISecurityStorage)registry.Securities;

		var remoteDrive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, new FixMessageAdapter(new IncrementalIdGenerator()))
		{
			Credentials = { Email = "hydra_user", Password = "hydra_user".To<SecureString>() }
		};

		//----------------------------------Security------------------------------------------------------------------
		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		remoteDrive.LookupSecurities(Extensions.LookupAllCriteriaMessage, registry.Securities,
			s => securityStorage.Save(s.ToSecurity(exchangeInfoProvider), false), () => false,
			(c, t) => Console.WriteLine($"Downloaded [{c}]/[{t}]"));

		var securities = securityStorage.LookupAll();

		foreach (var sec in securities)
		{
			Console.WriteLine(sec);
		}

		Console.ReadLine();

		var secId = new SecurityId
		{
			SecurityCode = "BTCUSD_PERP",
			BoardCode = "BNBCN"
		};

		var startDate = DateTime.Now.AddDays(-30);
		var endDate = DateTime.Now;

		const string pathHistory = "Storage";
		pathHistory.SafeDeleteDir();

		var localDrive = new LocalMarketDataDrive(pathHistory);

		const StorageFormats format = StorageFormats.Binary;

		foreach (var dataType in remoteDrive.GetAvailableDataTypes(secId, format))
		{
			var localStorage = storageRegistry.GetStorage(secId, dataType.MessageType, dataType.Arg, localDrive, format);
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