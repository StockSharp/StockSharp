namespace StockSharp.Tests;

[TestClass]
public class BoardMappingMessageAdapterTests : BaseTestClass
{
	private const string _defaultVenueBoard = "BNB";

	private sealed class VenueAdapter : MessageAdapter
	{
		public VenueAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public List<Message> Received { get; } = [];

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			Received.Add(message);
			return default;
		}

		public ValueTask SayAsync(Message message, CancellationToken cancellationToken)
			=> SendOutMessageAsync(message, cancellationToken);

		public override IMessageAdapter Clone() => new VenueAdapter();
	}

	private static (BoardMappingMessageAdapter mapped, VenueAdapter venue, List<Message> heard) Create()
	{
		var venue = new VenueAdapter();

		var mapped = new BoardMappingMessageAdapter(venue, new Dictionary<string, string>
		{
			{ "BNB", "SS" },
			{ "BNB_FUT", "SS_FUT" },
			{ "BNBCN", "SS_CN" },
		}, _defaultVenueBoard);

		var heard = new List<Message>();
		((IMessageAdapter)mapped).NewOutMessageAsync += (m, ct) => { heard.Add(m); return default; };

		return (mapped, venue, heard);
	}

	private static SecurityId Id(string code, string board) => new() { SecurityCode = code, BoardCode = board };

	[TestMethod]
	public async Task ClientOrder_ReachesTheVenueOnItsOwnBoard()
	{
		var (mapped, venue, _) = Create();

		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", "SS") }, CancellationToken);

		var sent = (OrderRegisterMessage)venue.Received.Single();
		sent.SecurityId.BoardCode.AssertEqual("BNB");
		sent.SecurityId.SecurityCode.AssertEqual("ETHBTC", "only the board is ours");
	}

	[TestMethod]
	public async Task VenueQuote_ReachesTheClientOnOurBoard()
	{
		var (_, venue, heard) = Create();

		await venue.SayAsync(new Level1ChangeMessage { SecurityId = Id("ETHBTC", "BNB") }, CancellationToken);

		var quote = (Level1ChangeMessage)heard.Single();
		quote.SecurityId.BoardCode.AssertEqual("SS");
		quote.SecurityId.SecurityCode.AssertEqual("ETHBTC");
	}

	[TestMethod]
	public async Task EveryBoardNamedKeepsItsOwnPair()
	{
		var (mapped, venue, heard) = Create();

		await venue.SayAsync(new Level1ChangeMessage { SecurityId = Id("ETHBTC", "BNB_FUT") }, CancellationToken);
		await venue.SayAsync(new Level1ChangeMessage { SecurityId = Id("ETHUSD", "BNBCN") }, CancellationToken);

		((Level1ChangeMessage)heard[0]).SecurityId.BoardCode.AssertEqual("SS_FUT");
		((Level1ChangeMessage)heard[1]).SecurityId.BoardCode.AssertEqual("SS_CN");

		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", "SS_FUT") }, CancellationToken);
		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHUSD", "SS_CN") }, CancellationToken);

		((OrderRegisterMessage)venue.Received[0]).SecurityId.BoardCode.AssertEqual("BNB_FUT");
		((OrderRegisterMessage)venue.Received[1]).SecurityId.BoardCode.AssertEqual("BNBCN");
	}

	[TestMethod]
	public async Task AnUnknownBoardGoesToTheVenueOnItsDefault()
	{
		var (mapped, venue, _) = Create();

		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", "SOMETHING") }, CancellationToken);
		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", string.Empty) }, CancellationToken);

		((OrderRegisterMessage)venue.Received[0]).SecurityId.BoardCode.AssertEqual(_defaultVenueBoard);
		((OrderRegisterMessage)venue.Received[1]).SecurityId.BoardCode.AssertEqual(_defaultVenueBoard);
	}

	[TestMethod]
	public async Task ABoardTheVenueNamesButWeDoNot_IsShownAsItIs()
	{
		var (_, venue, heard) = Create();

		// Naming it as one of ours would offer a market we cannot route back.
		await venue.SayAsync(new Level1ChangeMessage { SecurityId = Id("ETHBTC", "BNB_OPT") }, CancellationToken);

		((Level1ChangeMessage)heard.Single()).SecurityId.BoardCode.AssertEqual("BNB_OPT");
	}

	[TestMethod]
	public async Task SpellingOfTheBoardDoesNotMatter()
	{
		var (mapped, venue, heard) = Create();

		await venue.SayAsync(new Level1ChangeMessage { SecurityId = Id("ETHBTC", "bnb_fut") }, CancellationToken);
		((Level1ChangeMessage)heard.Single()).SecurityId.BoardCode.AssertEqual("SS_FUT");

		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", "ss") }, CancellationToken);
		((OrderRegisterMessage)venue.Received.Single()).SecurityId.BoardCode.AssertEqual("BNB");
	}

	[TestMethod]
	public async Task MessageWithoutAnInstrument_PassesBothWays()
	{
		var (mapped, venue, heard) = Create();

		await mapped.SendInMessageAsync(new TimeMessage(), CancellationToken);
		await venue.SayAsync(new ConnectMessage(), CancellationToken);

		venue.Received.Single().Type.AssertEqual(MessageTypes.Time);
		heard.Single().Type.AssertEqual(MessageTypes.Connect);
	}

	[TestMethod]
	public async Task EveryInstrumentCarryingMessageIsMapped()
	{
		var (_, venue, heard) = Create();

		await venue.SayAsync(new ExecutionMessage { DataTypeEx = DataType.Transactions, SecurityId = Id("ETHBTC", "BNB") }, CancellationToken);
		await venue.SayAsync(new SecurityMessage { SecurityId = Id("BTCUSDT", "BNB_FUT") }, CancellationToken);

		((ExecutionMessage)heard[0]).SecurityId.BoardCode.AssertEqual("SS");
		((SecurityMessage)heard[1]).SecurityId.BoardCode.AssertEqual("SS_FUT");
	}

	[TestMethod]
	public async Task WhatWentDownComesBackTheSame()
	{
		var (mapped, venue, heard) = Create();

		var original = Id("ETHBTC", "SS_FUT");

		await mapped.SendInMessageAsync(new OrderRegisterMessage { SecurityId = original }, CancellationToken);

		var asTheVenueSawIt = ((OrderRegisterMessage)venue.Received.Single()).SecurityId;
		await venue.SayAsync(new ExecutionMessage { DataTypeEx = DataType.Transactions, SecurityId = asTheVenueSawIt }, CancellationToken);

		((ExecutionMessage)heard.Single()).SecurityId.AssertEqual(original);
	}

	[TestMethod]
	public async Task ACopyMapsTheSameWay()
	{
		var (mapped, _, _) = Create();

		var clone = (BoardMappingMessageAdapter)mapped.Clone();

		await clone.SendInMessageAsync(new OrderRegisterMessage { SecurityId = Id("ETHBTC", "SS_FUT") }, CancellationToken);

		var venue = (VenueAdapter)clone.InnerAdapter;
		((OrderRegisterMessage)venue.Received.Single()).SecurityId.BoardCode.AssertEqual("BNB_FUT");
	}
}
