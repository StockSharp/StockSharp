#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleStorage.SampleStoragePublic
File: Program.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleStorage
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	class Program
	{
		static void Main()
		{
			// creating AAPL security
			var security = new Security
			{
				Id = "AAPL@NASDAQ",
				PriceStep = 0.1m,
				Decimals = 1,
			};

			var trades = new List<Trade>();

			// generation 1000 random ticks
			//

			const int count = 1000;

			var begin = DateTime.Today;
			var end = begin + TimeSpan.FromMinutes(count);

			for (var i = 0; i < count; i++)
			{
				var t = new Trade
				{
					Time = begin + TimeSpan.FromMinutes(i),
					Id = i + 1,
					Security = security,
					Volume = RandomGen.GetInt(1, 10),
					Price = RandomGen.GetInt(1, 100) * security.PriceStep ?? 1m + 99
				};

				trades.Add(t);
			}

			using (var drive = new LocalMarketDataDrive())
			{
				// get AAPL storage
				var aaplStorage = drive.GetSecurityDrive(security);

				// get tick storage
				var tradeStorage = aaplStorage.GetTickStorage(new TickCsvSerializer(aaplStorage.SecurityId)).ToEntityStorage<ExecutionMessage, Trade>(security);

				// saving ticks
				tradeStorage.Save(trades);

				for (var d = begin; d < end; d += TimeSpan.FromDays(1))
				{
					// loading ticks
					var loadedTrades = tradeStorage.Load(d);

					foreach (var trade in loadedTrades)
					{
						Console.WriteLine(LocalizedStrings.Str2968Params, trade.Id, trade);
					}	
				}

				Console.ReadLine();

				// deleting ticks (and removing file)
				tradeStorage.Delete(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));	
			}
		}
	}
}