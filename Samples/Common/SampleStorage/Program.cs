namespace SampleStorage
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	class Program
	{
		static void Main()
		{
			// создаем тестовый инструмент
			var security = new Security
			{
				Id = "TestId",
				PriceStep = 0.1m,
				Decimals = 1,
			};

			var trades = new List<Trade>();

			// генерируем 1000 произвольных сделок
			//

			for (var i = 0; i < 1000; i++)
			{
				var t = new Trade
				{
					Time = DateTime.Today + TimeSpan.FromMinutes(i),
					Id = i + 1,
					Security = security,
					Volume = RandomGen.GetInt(1, 10),
					Price = RandomGen.GetInt(1, 100) * security.PriceStep + 99
				};

				trades.Add(t);
			}

			var storage = new StorageRegistry();

			// получаем хранилище для тиковых сделок
			var tradeStorage = storage.GetTradeStorage(security);

			// сохраняем сделки
			tradeStorage.Save(trades);

			// загружаем сделки
			var loadedTrades = tradeStorage.Load(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));

			foreach (var trade in loadedTrades)
			{
				Console.WriteLine(LocalizedStrings.Str2968Params, trade.Id, trade);
			}

			Console.ReadLine();

			// удаляем сделки (очищаем файл)
			tradeStorage.Delete(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));
		}
	}
}