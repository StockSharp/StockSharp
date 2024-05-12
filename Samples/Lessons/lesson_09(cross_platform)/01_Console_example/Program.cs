using System;
using System.Linq;
using System.Security;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace Console_example;

static class Program
{
	private static void Main()
	{
		var connector = new Connector();

		//
		// !!! IMPORTANT !!!
		// The code show how to connect to Binance.
		// Use your own adapter and proper setup
		// !!! DO NOT FORGET ADD NUGET PACKAGE WITH REQUIRED CONNECTOR !!!
		// https://stocksharp.com/products/nuget_manual/#privateserver
		//
		//var messageAdapter = new StockSharp.Binance.BinanceMessageAdapter(connector.TransactionIdGenerator)
		//{
		//	Key = "<Your key>".Secure(),
		//	Secret = "<Your secret>".Secure(),
		//	IsDemo = true
		//};

		//connector.Adapter.InnerAdapters.Add(messageAdapter);

		connector.ConnectionError += Console.WriteLine;
		connector.Error += Console.WriteLine;

		connector.Connect();

		//--------------------------Security--------------------------------------------------------------------------------
		Console.WriteLine("Securities:");
		Security security = null;
		connector.LookupSecuritiesResult += (message, securities, arg3) =>
		{
			foreach (var security1 in securities)
			{
				Console.WriteLine(security1);
			}

			security = securities.First();
		};
		connector.LookupSecurities(new Security() { Code = "BTCUSD_PERP" });
		Console.ReadLine();

		//--------------------------Portfolio--------------------------------------------------------------------------------
		Console.WriteLine("Portfolios:");
		if (!connector.Portfolios.Any())
		{
			connector.PositionReceived += (subscription, position) =>
			{
				Console.WriteLine(position);
				connector.UnSubscribe(subscription);
			};

			connector.SubscribePositions(security);
		}
		else
		{
			foreach (var connectorPortfolio in connector.Portfolios)
			{
				Console.WriteLine(connectorPortfolio);
			}
		}
		Console.ReadLine();

		IOrderBookMessage lastDepth = null;
		//--------------------------MarketDepth--------------------------------------------------------------------------------
		Console.WriteLine("MarketDepth (wait for prices):");
		connector.OrderBookReceived += (subscription, depth) =>
		{
			Console.WriteLine(depth.GetBestBid());
			Console.WriteLine(depth.GetBestAsk());

			connector.UnSubscribe(subscription);
			lastDepth = depth;
		};

		connector.SubscribeMarketDepth(security);
		Console.ReadLine();

		////--------------------------Order--------------------------------------------------------------------------------
		Console.WriteLine("Order:");
		Console.Write("Do you want to buy 1? (Y/N)");

		var str = Console.ReadLine();
		if (str != null && str.ToUpper() != "Y") return;

		var bestBidPrice = lastDepth?.GetBestBid()?.Price;

		var order = new Order
		{
			Security = security,
			Portfolio = connector.Portfolios.First(),
			Price = bestBidPrice ?? 0,
			Type = bestBidPrice == null ? OrderTypes.Market : OrderTypes.Limit,
			Volume = 1m,
			Side = Sides.Buy,
		};

		connector.OrderChanged += Console.WriteLine;
		connector.RegisterOrder(order);

		Console.ReadLine();
		Console.ReadLine();
	}
}
