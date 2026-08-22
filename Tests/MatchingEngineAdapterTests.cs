namespace StockSharp.Tests;

using StockSharp.MatchingEngine;

/// <summary>
/// Tests for <see cref="MatchingEngineAdapter"/> - the book the internal (B-Book) venue is built on.
/// Every test drives the real engine through its own transport and reads back what it emitted.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class MatchingEngineAdapterTests : BaseTestClass
{
	private static readonly SecurityId _securityId = new() { SecurityCode = "BTCUSDT", BoardCode = "IMEX" };
	private static readonly DateTime _start = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

	#region Test helpers

	/// <summary>
	/// Feeds an engine through <see cref="IMessageTransport"/> and keeps everything it emits, in order.
	/// </summary>
	private sealed class EngineRun
	{
		public EngineRun(MatchingEngineAdapter engine)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));

			Engine.NewOutMessageAsync += (message, ct) =>
			{
				Out.Add(message);
				return default;
			};
		}

		/// <summary>The engine under test.</summary>
		public MatchingEngineAdapter Engine { get; }

		/// <summary>Everything the engine has emitted so far.</summary>
		public List<Message> Out { get; } = [];

		/// <summary>The transactional rows among them.</summary>
		public IEnumerable<ExecutionMessage> Executions => Out.OfType<ExecutionMessage>();

		/// <summary>Sends one message in the way the venue module sends it.</summary>
		public ValueTask SendAsync(Message message, CancellationToken cancellationToken)
			=> ((IMessageTransport)Engine).SendInMessageAsync(message, cancellationToken);
	}

	private static QuoteChangeMessage VenueBook(SecurityId securityId, DateTime time, QuoteChange[] bids, QuoteChange[] asks)
		=> new()
		{
			SecurityId = securityId,
			LocalTime = time,
			ServerTime = time,
			Bids = bids,
			Asks = asks,
		};

	private static PositionChangeMessage MoneyRow(string account, decimal money, DateTime time)
		=> new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = account,
			LocalTime = time,
			ServerTime = time,
		}
		.Add(PositionChangeTypes.BeginValue, money);

	private static OrderRegisterMessage NewOrder(long transactionId, string account, Sides side, OrderTypes type, decimal price, decimal volume, DateTime time)
		=> new()
		{
			TransactionId = transactionId,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = side,
			OrderType = type,
			Price = price,
			Volume = volume,
			LocalTime = time,
		};

	private static decimal PositionOf(MatchingEngineAdapter engine, string account)
		=> engine.PortfolioManager.GetPortfolio(account).GetPosition(_securityId)?.CurrentValue ?? 0m;

	#endregion

	/// <summary>
	/// An account holding no cash must have its market buy rejected: a market order names no price,
	/// but it still costs what the book charges for it.
	/// </summary>
	[TestMethod]
	public async Task AZeroCashAccountCannotBuyAtMarket()
	{
		const string account = "ZeroCash";
		const long tx = 1001;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 0m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(NewOrder(tx, account, Sides.Buy, OrderTypes.Market, 0m, 5m, _start.AddSeconds(1)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == tx).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"a market buy of 5 at an ask of 101 costs 505 and the account holds nothing, so it must be rejected; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}");

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(0, fills.Length, "a rejected order must not trade");
		AreEqual(0m, PositionOf(engine, account), "a rejected order must not move the account's position");
	}
}
