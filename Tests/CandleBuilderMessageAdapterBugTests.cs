namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

[TestClass]
public class CandleBuilderMessageAdapterBugTests : BaseTestClass
{
	[TestMethod]
	public async Task Unsubscribe_AllSecurityChild_ShouldNotForwardToInner()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.Ticks]);
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		using var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = default,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, token);

		inner.InMessages.Clear();

		var exec = new ExecutionMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 1m,
			TradeVolume = 1m,
		};
		exec.SetSubscriptionIds([1]);

		inner.SendOutMessage(exec);
		await Task.Delay(50, token);

		var child = output.OfType<SubscriptionSecurityAllMessage>().FirstOrDefault();
		IsNotNull(child);

		await adapter.SendInMessageAsync(child, token);
		await Task.Delay(10, token);

		inner.InMessages.Clear();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = child.TransactionId,
		}, token);

		inner.InMessages.Count.AssertEqual(0);
	}
}
