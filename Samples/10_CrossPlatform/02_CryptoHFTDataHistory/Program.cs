namespace StockSharp.Samples.CrossPlatform.CryptoHFTDataHistory;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using StockSharp.CryptoHFTData;
using StockSharp.Messages;

static class Program
{
	private static async Task Main()
	{
		var adapter = new CryptoHFTDataMessageAdapter(new IncrementalIdGenerator())
		{
			Exchange = "binance_futures",
			Token = Environment.GetEnvironmentVariable("CRYPTOHFTDATA_API_KEY")?.Secure(),
		};

		adapter.NewOutMessageAsync += (message, _) =>
		{
			switch (message)
			{
				case ExecutionMessage trade:
					Console.WriteLine($"trade {trade.ServerTime:O} {trade.TradePrice} x {trade.TradeVolume} {trade.OriginSide}");
					break;
				case QuoteChangeMessage depth:
					Console.WriteLine($"depth {depth.ServerTime:O} bids={depth.Bids.Length} asks={depth.Asks.Length} state={depth.State}");
					break;
			}
			return default;
		};

		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken.None);

		var securityId = new SecurityId
		{
			SecurityCode = "KAVAUSDT",
			BoardCode = adapter.Exchange,
		};
		var from = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
		var to = from.AddHours(1).AddTicks(-1);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = securityId,
			DataType2 = DataType.Ticks,
			From = from,
			To = to,
			Count = 10,
		}, CancellationToken.None);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = securityId,
			DataType2 = DataType.MarketDepth,
			From = from,
			To = to,
			Count = 10,
		}, CancellationToken.None);

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken.None);
	}
}
