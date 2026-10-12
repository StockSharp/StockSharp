namespace StockSharp.Tests;

using System.Net;
using System.Text;

using StockSharp.Fix;
using StockSharp.Fix.Dialects;
using StockSharp.Fix.Native;

/// <summary>
/// Session state tests for the FIX dialect: the correlation it keeps between the requests it
/// sends and the answers that come back, and the order books it builds from the wire.
/// </summary>
[TestClass]
public class FixDialectTests : BaseTestClass
{
	private const string _beginString = "8=" + FixVersions.Fix44 + "|";
	private const string _sendingTime = "20240615-10:30:00.000";
	private const string _transactTime = "20240615-10:30:01.000";

	private static readonly DateTime _sendingTimeUtc = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
	private static readonly DateTime _transactTimeUtc = new(2024, 6, 15, 10, 30, 1, DateTimeKind.Utc);

	private static readonly SecurityId _aapl = new() { SecurityCode = "AAPL", BoardCode = "NYSE" };
	private static readonly SecurityId _msft = new() { SecurityCode = "MSFT", BoardCode = "NYSE" };

	private static readonly EndPoint _address = new IPEndPoint(IPAddress.Loopback, 5001);

	private IncrementalIdGenerator _idGen;
	private IFixDialect _dialect;
	private MemoryStream _outStream;
	private IFixWriter _writer;
	private MemoryStream _inStream;
	private IFixReader _reader;

	private void CreateDialect()
	{
		_idGen = new IncrementalIdGenerator();

		InitSession(new DefaultFixDialect(_idGen));
	}

	private void InitSession(IFixDialect dialect)
	{
		_dialect = dialect;
		_dialect.SenderCompId = "CLIENT";
		_dialect.TargetCompId = "BROKER";

		_outStream = new MemoryStream();
		_writer = new TextFixWriter(_outStream, Encoding.UTF8);

		_inStream = new MemoryStream();
		_reader = new TextFixReader(_inStream, Encoding.UTF8);

		_dialect.Init(_writer, _reader, _address);
	}

	private ValueTask SendAsync(Message message)
		=> _dialect.SendInMessageAsync(message, CancellationToken);

	/// <summary>
	/// Appends one well formed frame to the incoming stream. BodyLength counts the bytes from
	/// MsgType up to and including the SOH before the checksum field, and CheckSum is the sum of
	/// every preceding byte modulo 256 - both counted here over the very bytes handed to the
	/// reader (all fields are ASCII, so one char is one byte).
	/// </summary>
	private void Feed(string msgType, long seqNum, string fields)
	{
		var body = $"35={msgType}|49=BROKER|56=CLIENT|34={seqNum}|52={_sendingTime}|{fields}";
		var head = $"{_beginString}9={body.Length}|";
		var soh = (char)AsciiSymbols.Soh;

		// The sum is over the bytes the reader is handed, so a separator counts as SOH rather than as
		// the placeholder the frame is written with.
		var prefix = (head + body).Replace('|', soh);
		var checkSum = prefix.Sum(c => (int)c) % 256;

		var frame = $"{prefix}10={checkSum:000}{soh}";
		var bytes = Encoding.UTF8.GetBytes(frame);

		var pos = _inStream.Position;
		_inStream.Position = _inStream.Length;
		_inStream.Write(bytes, 0, bytes.Length);
		_inStream.Position = pos;
	}

	private async Task<Message[]> ReadNextAsync()
	{
		var messages = new List<Message>();

		await foreach (var msg in _dialect.ReadAsync(CancellationToken))
			messages.Add(msg);

		// The body loop stops on the CheckSum tag itself; reading the trailer consumes its value
		// (and verifies the frame) so the reader sits at the first byte of the next message.
		await _reader.ReadTrailerAsync(CancellationToken);

		return [.. messages];
	}

	private string[] Written()
	{
		var text = Encoding.UTF8.GetString(_outStream.ToArray()).Replace((char)AsciiSymbols.Soh, '|');

		return [.. text
			.Split(_beginString, StringSplitOptions.RemoveEmptyEntries)
			.Select(p => _beginString + p)];
	}

	private static string Tag(string message, FixTags tag)
	{
		var prefix = $"|{(int)tag}=";
		var start = message.IndexOf(prefix, StringComparison.Ordinal);

		if (start == -1)
			return null;

		start += prefix.Length;

		return message[start..message.IndexOf('|', start)];
	}

	private static OrderRegisterMessage CreateRegister(long transId)
		=> new()
		{
			TransactionId = transId,
			SecurityId = _aapl,
			PortfolioName = "PF",
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 150.25m,
			Volume = 100,
		};

	#region Transaction correlation

	/// <summary>
	/// Pins that every order command names itself in ClOrdID and the order it acts on in
	/// OrigClOrdID, and that the session numbers its messages from one in send order.
	/// </summary>
	[TestMethod]
	public async Task Dialect_OrderCommands_CarryTheirOwnIdsAndNameTheOrderTheyActOn()
	{
		CreateDialect();

		await SendAsync(CreateRegister(1001));

		await SendAsync(new OrderReplaceMessage
		{
			TransactionId = 1002,
			OriginalTransactionId = 1001,
			SecurityId = _aapl,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 151m,
			Volume = 100,
		});

		await SendAsync(new OrderCancelMessage
		{
			TransactionId = 1003,
			OriginalTransactionId = 1001,
			SecurityId = _aapl,
		});

		var written = Written();

		AreEqual(3, written.Length);

		AreEqual(FixMessages.NewOrderSingle, Tag(written[0], FixTags.MsgType));
		AreEqual("1001", Tag(written[0], FixTags.ClOrdID));
		AreEqual("1", Tag(written[0], FixTags.MsgSeqNum));

		AreEqual(FixMessages.OrderCancelReplaceRequest, Tag(written[1], FixTags.MsgType));
		AreEqual("1002", Tag(written[1], FixTags.ClOrdID));
		AreEqual("1001", Tag(written[1], FixTags.OrigClOrdID));
		AreEqual("2", Tag(written[1], FixTags.MsgSeqNum));

		AreEqual(FixMessages.OrderCancelRequest, Tag(written[2], FixTags.MsgType));
		AreEqual("1003", Tag(written[2], FixTags.ClOrdID));
		AreEqual("1001", Tag(written[2], FixTags.OrigClOrdID));
		AreEqual("3", Tag(written[2], FixTags.MsgSeqNum));
	}

	/// <summary>
	/// Pins that an acknowledgement and a fill come back on the transaction that registered the
	/// order, and that the balance is what is left of it: 100 ordered less 40 filled is 60.
	/// </summary>
	[TestMethod]
	public async Task Dialect_ExecutionAndFill_ComeBackOnTheRegisteringTransaction()
	{
		CreateDialect();

		await SendAsync(CreateRegister(1001));

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=1001|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|40={OrdType.Limit}|44=150.25|38=100|151=100|60={_transactTime}|");

		var ack = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(DataType.Transactions, ack.DataTypeEx);
		IsTrue(ack.HasOrderInfo);
		AreEqual(1001L, ack.OriginalTransactionId);
		AreEqual(OrderStates.Active, ack.OrderState);
		AreEqual(_aapl, ack.SecurityId);
		AreEqual("ORD-9", ack.OrderStringId);
		AreEqual(100m, ack.Balance);
		AreEqual(Sides.Buy, ack.Side);
		AreEqual(_transactTimeUtc, ack.ServerTime);

		Feed(FixMessages.ExecutionReport, 2, $"150={ExecType.Trade}|39={OrdStatus.PartiallyFilled}|11=1001|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|14=40|151=60|31=150.25|32=40|17=TRD-1|60={_transactTime}|");

		var fill = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1001L, fill.OriginalTransactionId);
		AreEqual(150.25m, fill.TradePrice);
		AreEqual(40m, fill.TradeVolume);
		AreEqual("TRD-1", fill.TradeStringId);
		AreEqual(60m, fill.Balance);
	}

	/// <summary>
	/// Pins that a session level Reject and a business level Reject are answered as the failure of
	/// the very order they refuse, found by the sequence number that order went out with.
	/// </summary>
	[TestMethod]
	public async Task Dialect_RejectAndBusinessReject_FailTheOrderTheyRefuse()
	{
		CreateDialect();

		await SendAsync(CreateRegister(1001));
		await SendAsync(CreateRegister(1002));

		Feed(FixMessages.Reject, 1, "45=1|371=11|372=D|373=5|58=Value is incorrect|");

		var rejected = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1001L, rejected.OriginalTransactionId);
		AreEqual(OrderStates.Failed, rejected.OrderState);
		IsTrue(rejected.HasOrderInfo);
		IsNotNull(rejected.Error);

		Feed(FixMessages.BusinessMessageReject, 2, "45=2|372=D|380=3|58=Unsupported order type|");

		var refused = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1002L, refused.OriginalTransactionId);
		AreEqual(OrderStates.Failed, refused.OrderState);
		IsTrue(refused.HasOrderInfo);
		IsNotNull(refused.Error);
	}

	/// <summary>
	/// Pins that a reset ends the session: the same rejection that failed an order before it is
	/// afterwards attached to no order at all, rather than to whatever now holds that number.
	/// </summary>
	[TestMethod]
	public async Task Dialect_RejectFromPreviousSession_IsNotAttachedAfterReset()
	{
		CreateDialect();

		await SendAsync(CreateRegister(1001));

		Feed(FixMessages.Reject, 1, "45=1|372=D|373=5|58=Too late|");

		var before = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1001L, before.OriginalTransactionId);
		AreEqual(OrderStates.Failed, before.OrderState);

		await SendAsync(new ResetMessage());
		await SendAsync(CreateRegister(2001));

		Feed(FixMessages.Reject, 2, "45=1|372=D|373=5|58=Too late|");

		var after = await ReadNextAsync();

		AreEqual(0, after.OfType<ExecutionMessage>().Count());

		var error = after.OfType<ErrorMessage>().Single();

		AreEqual(0L, error.OriginalTransactionId);
		IsNotNull(error.Error);
	}

	/// <summary>
	/// Pins the mapping for orders whose identifier is not ours: an unfamiliar ClOrdID is given one
	/// transaction id, the same ClOrdID seen again keeps it (no second id is taken), and a cancel
	/// for that transaction goes out under the broker's own identifier.
	/// </summary>
	[TestMethod]
	public async Task Dialect_UnknownStringClOrdId_GetsOneTransactionIdAndTravelsBack()
	{
		CreateDialect();
		_dialect.SupportUnknownExecutions = true;

		var before = _idGen.Current;

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var ack = (ExecutionMessage)(await ReadNextAsync()).Single();

		// One unknown order costs exactly one identifier, and the generator hands them out in order.
		AreEqual(before + 1, ack.OriginalTransactionId);
		AreEqual(before + 1, _idGen.Current);

		Feed(FixMessages.ExecutionReport, 2, $"150={ExecType.Trade}|39={OrdStatus.PartiallyFilled}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|14=40|151=60|31=150.25|32=40|17=TRD-1|60={_transactTime}|");

		var fill = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(ack.OriginalTransactionId, fill.OriginalTransactionId);
		AreEqual(before + 1, _idGen.Current);

		await SendAsync(new OrderCancelMessage
		{
			TransactionId = 5000,
			OriginalTransactionId = ack.OriginalTransactionId,
			SecurityId = _aapl,
		});

		var cancel = Written().Single();

		AreEqual("BROKER-7", Tag(cancel, FixTags.OrigClOrdID));
		AreEqual("5000", Tag(cancel, FixTags.ClOrdID));
	}

	/// <summary>
	/// Pins that a numeric ClOrdID is the transaction id itself - no identifier is invented for an
	/// order we already named, even while unknown executions are being tracked.
	/// </summary>
	[TestMethod]
	public async Task Dialect_NumericClOrdId_IsTheTransactionIdItself()
	{
		CreateDialect();
		_dialect.SupportUnknownExecutions = true;

		var before = _idGen.Current;

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=1001|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var ack = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1001L, ack.OriginalTransactionId);
		AreEqual(before, _idGen.Current);
	}

	/// <summary>
	/// Pins that a rejection naming only the broker's own identifier still reaches the transaction
	/// that identifier stands for.
	/// </summary>
	[TestMethod]
	public async Task Dialect_RejectedExecution_ResolvesStringClOrdId()
	{
		CreateDialect();
		_dialect.SupportUnknownExecutions = true;

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var ack = (ExecutionMessage)(await ReadNextAsync()).Single();

		Feed(FixMessages.ExecutionReport, 2, $"150={ExecType.Rejected}|39={OrdStatus.Rejected}|41=BROKER-7|37=ORD-9|58=Not enough money|");

		var rejected = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(ack.OriginalTransactionId, rejected.OriginalTransactionId);
		AreEqual(OrderStates.Failed, rejected.OrderState);
		IsTrue(rejected.HasOrderInfo);
		IsNotNull(rejected.Error);
	}

	/// <summary>
	/// Pins that a reset forgets the broker identifiers too: the same ClOrdID afterwards belongs to
	/// a new transaction, because the one it stood for died with the session.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Reset_ForgetsStringClOrdIds()
	{
		CreateDialect();
		_dialect.SupportUnknownExecutions = true;

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var first = (ExecutionMessage)(await ReadNextAsync()).Single();

		await SendAsync(new ResetMessage());

		Feed(FixMessages.ExecutionReport, 2, $"150={ExecType.New}|39={OrdStatus.New}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var second = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreNotEqual(first.OriginalTransactionId, second.OriginalTransactionId);
		AreEqual(_idGen.Current, second.OriginalTransactionId);
	}

	/// <summary>
	/// Pins that a gap fill is a session level repair and nothing more: the orders sent before it
	/// are still ours afterwards.
	/// </summary>
	[TestMethod]
	public async Task Dialect_SequenceReset_DoesNotLoseOrderCorrelation()
	{
		CreateDialect();

		await SendAsync(CreateRegister(1001));

		Feed(FixMessages.SequenceReset, 49, "123=Y|36=50|");

		var seqReset = (FixSeqResetMessage)(await ReadNextAsync()).Single();

		AreEqual(true, seqReset.GapFill);
		AreEqual(50L, seqReset.NewSeqNo);
		AreEqual(49L, seqReset.SeqNum);

		Feed(FixMessages.Reject, 50, "45=1|372=D|373=5|58=Value is incorrect|");

		var rejected = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(1001L, rejected.OriginalTransactionId);
		AreEqual(OrderStates.Failed, rejected.OrderState);
	}

	/// <summary>
	/// Pins the resend seam: an order status request that the counterparty cannot answer becomes a
	/// resend of messages 5..15 (skip 5, then 10 of them), and a replayed report arrives as the
	/// order it belongs to, delivered under the subscription that asked for the replay.
	/// </summary>
	[TestMethod]
	public async Task Dialect_ResendRequest_AttributesReplayedReportsToTheSubscription()
	{
		_idGen = new IncrementalIdGenerator();

		var dialect = new ResendFixDialect(_idGen) { IsResetCounter = false };
		InitSession(dialect);

		await SendAsync(new OrderStatusMessage
		{
			TransactionId = 7000,
			IsSubscribe = true,
			Skip = 5,
			Count = 10,
		});

		var request = Written().Single();

		AreEqual(FixMessages.ResendRequest, Tag(request, FixTags.MsgType));
		AreEqual("5", Tag(request, FixTags.BeginSeqNo));
		AreEqual("15", Tag(request, FixTags.EndSeqNo));

		Feed(FixMessages.ExecutionReport, 6, $"43=Y|150={ExecType.Canceled}|39={OrdStatus.Canceled}|11=1002|41=1001|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=0|60={_transactTime}|");

		var replay = (ExecutionMessage)(await ReadNextAsync()).Single();

		AreEqual(7000L, replay.OriginalTransactionId);
		AreEqual(1001L, replay.TransactionId);
		AreEqual(OrderStates.Done, replay.OrderState);
	}

	/// <summary>
	/// Pins what a second Init does and does not do: it moves the session onto the new transport,
	/// leaving the old one untouched, and it is not a reset - what the session already knows about
	/// its orders survives it.
	/// </summary>
	[TestMethod]
	public async Task Dialect_RepeatedInit_SwapsTransportsAndKeepsTheSession()
	{
		CreateDialect();
		_dialect.SupportUnknownExecutions = true;

		Feed(FixMessages.ExecutionReport, 1, $"150={ExecType.New}|39={OrdStatus.New}|11=BROKER-7|37=ORD-9|55=AAPL|100=NYSE|54={Side.Buy}|38=100|151=100|60={_transactTime}|");

		var ack = (ExecutionMessage)(await ReadNextAsync()).Single();

		await SendAsync(CreateRegister(1001));

		Throws<ArgumentNullException>(() => _dialect.Init(null, _reader, _address));
		Throws<ArgumentNullException>(() => _dialect.Init(_writer, null, _address));
		Throws<ArgumentNullException>(() => _dialect.Init(_writer, _reader, null));

		var firstStream = _outStream;
		var firstLength = firstStream.Length;

		AreEqual(1, Written().Length);

		_outStream = new MemoryStream();
		_writer = new TextFixWriter(_outStream, Encoding.UTF8);
		_dialect.Init(_writer, _reader, _address);

		await SendAsync(new OrderCancelMessage
		{
			TransactionId = 5000,
			OriginalTransactionId = ack.OriginalTransactionId,
			SecurityId = _aapl,
		});

		AreEqual(firstLength, firstStream.Length);

		var cancel = Written().Single();

		AreEqual(FixMessages.OrderCancelRequest, Tag(cancel, FixTags.MsgType));
		AreEqual("BROKER-7", Tag(cancel, FixTags.OrigClOrdID));
	}

	#endregion

	#region Order books

	/// <summary>
	/// Pins that a quote id names one quote: re-sent on the other side it moves there instead of
	/// standing on both sides of the book at once.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Quote_SameIdChangingSideMovesTheQuote()
	{
		CreateDialect();

		Feed(FixMessages.Quote, 1, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=150.25|134=10|");

		var bid = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, bid.SecurityId);
		AreEqual(_sendingTimeUtc, bid.ServerTime);
		AreEqual(1, bid.Bids.Length);
		AreEqual(150.25m, bid.Bids[0].Price);
		AreEqual(10m, bid.Bids[0].Volume);
		AreEqual(0, bid.Asks.Length);

		Feed(FixMessages.Quote, 2, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=151.75|135=20|");

		var ask = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, ask.SecurityId);
		AreEqual(0, ask.Bids.Length);
		AreEqual(1, ask.Asks.Length);
		AreEqual(151.75m, ask.Asks[0].Price);
		AreEqual(20m, ask.Asks[0].Volume);
	}

	/// <summary>
	/// Pins that withdrawing a quote that was never there changes nothing: the book still holds
	/// what was put in it, and the next quote lands on top of that.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Quote_RemovingUnknownIdLeavesTheBookIntact()
	{
		CreateDialect();

		Feed(FixMessages.Quote, 1, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=150.25|134=10|");

		await ReadNextAsync();

		Feed(FixMessages.Quote, 2, $"55=AAPL|207=NYSE|117=Q9|537={(int)QuoteType.Indicative}|54={Side.Buy}|");

		foreach (var book in (await ReadNextAsync()).OfType<QuoteChangeMessage>())
		{
			AreEqual(1, book.Bids.Length);
			AreEqual(150.25m, book.Bids[0].Price);
			AreEqual(0, book.Asks.Length);
		}

		Feed(FixMessages.Quote, 3, $"55=AAPL|207=NYSE|117=Q2|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=151.75|135=20|");

		var after = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(1, after.Bids.Length);
		AreEqual(150.25m, after.Bids[0].Price);
		AreEqual(1, after.Asks.Length);
		AreEqual(151.75m, after.Asks[0].Price);
	}

	/// <summary>
	/// Pins that a quote id is only unique within its security: the same id under another symbol is
	/// another quote in another book, and neither book overwrites the other.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Quote_SameIdUnderTwoSecuritiesAreTwoBooks()
	{
		CreateDialect();

		Feed(FixMessages.Quote, 1, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=150.25|134=10|");

		await ReadNextAsync();

		Feed(FixMessages.Quote, 2, $"55=MSFT|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=300.5|135=5|");

		var msft = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_msft, msft.SecurityId);
		AreEqual(0, msft.Bids.Length);
		AreEqual(1, msft.Asks.Length);
		AreEqual(300.5m, msft.Asks[0].Price);

		Feed(FixMessages.Quote, 3, $"55=AAPL|207=NYSE|117=Q2|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=151.75|135=20|");

		var aapl = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, aapl.SecurityId);
		AreEqual(1, aapl.Bids.Length);
		AreEqual(150.25m, aapl.Bids[0].Price);
		AreEqual(1, aapl.Asks.Length);
		AreEqual(151.75m, aapl.Asks[0].Price);
	}

	/// <summary>
	/// Pins that an empty book is a complete snapshot of nothing, delivered to the subscription
	/// that asked for the book - a book the subscriber cannot attribute is a book it cannot apply.
	/// </summary>
	[TestMethod]
	public async Task Dialect_EmptyBook_IsAnEmptySnapshotForItsSubscription()
	{
		CreateDialect();

		await SendAsync(new MarketDataMessage
		{
			TransactionId = 7100,
			SecurityId = _aapl,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});

		Feed(FixMessages.MarketDataSnapshotFullRefresh, 1, $"262=7100|55=AAPL|207=NYSE|268=1|269={MDEntryType.EmptyBook}|");

		var book = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, book.SecurityId);
		AreEqual(QuoteChangeStates.SnapshotComplete, book.State);
		AreEqual(0, book.Bids.Length);
		AreEqual(0, book.Asks.Length);
		AreEqual(7100L, book.OriginalTransactionId);
	}

	/// <summary>
	/// Pins that an empty book empties one book: told that AAPL has no quotes left, the session
	/// must not throw away what it knows about MSFT.
	/// </summary>
	[TestMethod]
	public async Task Dialect_EmptyBook_ClearsOnlyItsOwnSecurity()
	{
		CreateDialect();

		Feed(FixMessages.Quote, 1, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=150.25|134=10|");

		await ReadNextAsync();

		Feed(FixMessages.Quote, 2, $"55=MSFT|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=300.5|134=5|");

		await ReadNextAsync();

		Feed(FixMessages.MarketDataSnapshotFullRefresh, 3, $"55=AAPL|207=NYSE|268=1|269={MDEntryType.EmptyBook}|");

		var cleared = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, cleared.SecurityId);
		AreEqual(0, cleared.Bids.Length);
		AreEqual(0, cleared.Asks.Length);

		Feed(FixMessages.Quote, 4, $"55=MSFT|207=NYSE|117=Q2|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=301.5|135=7|");

		var msft = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_msft, msft.SecurityId);
		AreEqual(1, msft.Bids.Length);
		AreEqual(300.5m, msft.Bids[0].Price);
		AreEqual(1, msft.Asks.Length);
		AreEqual(301.5m, msft.Asks[0].Price);
	}

	/// <summary>
	/// Pins the difference between the two kinds of book: an increment carries the changes, a full
	/// refresh carries the whole book and is not merged with what came before it.
	/// </summary>
	[TestMethod]
	public async Task Dialect_FullRefreshAfterIncrement_ReplacesTheBook()
	{
		CreateDialect();

		await SendAsync(new MarketDataMessage
		{
			TransactionId = 7200,
			SecurityId = _aapl,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});

		Feed(FixMessages.MarketDataIncrementalRefresh, 1, $"262=7200|268=2|269={MDEntryType.Bid}|270=150.25|271=10|269={MDEntryType.Bid}|270=150.15|271=20|");

		var increment = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, increment.SecurityId);
		AreEqual(QuoteChangeStates.Increment, increment.State);
		AreEqual(7200L, increment.OriginalTransactionId);
		AreEqual(_sendingTimeUtc, increment.ServerTime);
		AreEqual(2, increment.Bids.Length);
		AreEqual(150.25m, increment.Bids[0].Price);
		AreEqual(10m, increment.Bids[0].Volume);
		AreEqual(150.15m, increment.Bids[1].Price);
		AreEqual(20m, increment.Bids[1].Volume);
		AreEqual(0, increment.Asks.Length);

		Feed(FixMessages.MarketDataSnapshotFullRefresh, 2, $"262=7200|268=2|269={MDEntryType.Bid}|270=150.35|271=5|269={MDEntryType.Offer}|270=151.75|271=8|");

		var snapshot = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(QuoteChangeStates.SnapshotComplete, snapshot.State);
		AreEqual(7200L, snapshot.OriginalTransactionId);
		AreEqual(1, snapshot.Bids.Length);
		AreEqual(150.35m, snapshot.Bids[0].Price);
		AreEqual(5m, snapshot.Bids[0].Volume);
		AreEqual(1, snapshot.Asks.Length);
		AreEqual(151.75m, snapshot.Asks[0].Price);
		AreEqual(8m, snapshot.Asks[0].Volume);
	}

	/// <summary>
	/// Pins that a reset forgets both halves of the market data state: the quotes already in a book
	/// and which security each subscription stands for.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Reset_ForgetsBooksAndRequestedSecurities()
	{
		CreateDialect();

		await SendAsync(new MarketDataMessage
		{
			TransactionId = 7300,
			SecurityId = _aapl,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});

		// The request id alone identifies the security, so a refresh need not repeat the symbol.
		Feed(FixMessages.MarketDataIncrementalRefresh, 1, $"262=7300|268=1|269={MDEntryType.Bid}|270=150.25|271=10|");

		var known = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, known.SecurityId);
		AreEqual(7300L, known.OriginalTransactionId);

		Feed(FixMessages.Quote, 2, $"55=AAPL|207=NYSE|117=Q1|537={(int)QuoteType.Tradeable}|54={Side.Buy}|132=150.25|134=10|");

		await ReadNextAsync();

		await SendAsync(new ResetMessage());

		Feed(FixMessages.MarketDataIncrementalRefresh, 3, $"262=7300|268=1|269={MDEntryType.Bid}|270=150.25|271=10|");

		AreEqual(0, (await ReadNextAsync()).OfType<QuoteChangeMessage>().Count());

		Feed(FixMessages.Quote, 4, $"55=AAPL|207=NYSE|117=Q2|537={(int)QuoteType.Tradeable}|54={Side.Sell}|133=151.75|135=20|");

		var rebuilt = (QuoteChangeMessage)(await ReadNextAsync()).Single();

		AreEqual(_aapl, rebuilt.SecurityId);
		AreEqual(0, rebuilt.Bids.Length);
		AreEqual(1, rebuilt.Asks.Length);
		AreEqual(151.75m, rebuilt.Asks[0].Price);
	}

	#endregion

	#region Default dialect wire fields

	/// <summary>
	/// Every value of a repeating tag, in wire order, so that the number of entries in a group can
	/// be checked against what the group announced.
	/// </summary>
	private static string[] Tags(string message, FixTags tag)
	{
		var prefix = $"|{(int)tag}=";
		var values = new List<string>();
		var pos = 0;

		while (true)
		{
			var start = message.IndexOf(prefix, pos, StringComparison.Ordinal);

			if (start == -1)
				break;

			start += prefix.Length;

			var end = message.IndexOf('|', start);

			values.Add(message[start..end]);
			pos = end;
		}

		return [.. values];
	}

	/// <summary>
	/// Pins the two questions order status asks and that they are not the same message: about one
	/// named order it is an Order Status Request, about the whole book of orders a mass one. Sending
	/// a mass request for a single order would answer with every order the account has.
	/// </summary>
	[TestMethod]
	public async Task Dialect_OrderStatus_OneOrderAndAllOrdersAreDifferentRequests()
	{
		CreateDialect();

		await SendAsync(new OrderStatusMessage
		{
			TransactionId = 2001,
			OriginalTransactionId = 1001,
			IsSubscribe = true,
		});

		await SendAsync(new OrderStatusMessage
		{
			TransactionId = 2002,
			IsSubscribe = true,
		});

		var written = Written();

		AreEqual(2, written.Length);

		AreEqual(FixMessages.OrderStatusRequest, Tag(written[0], FixTags.MsgType));
		AreEqual("2001", Tag(written[0], FixTags.OrdStatusReqID));
		AreEqual("1001", Tag(written[0], FixTags.ClOrdID));
		AreEqual(SubscriptionRequestType.SnapshotPlusUpdates.ToString(), Tag(written[0], FixTags.SubscriptionRequestType));

		AreEqual(FixMessages.OrderMassStatusRequest, Tag(written[1], FixTags.MsgType));
		AreEqual("2002", Tag(written[1], FixTags.MassStatusReqID));
		AreEqual(SubscriptionRequestType.SnapshotPlusUpdates.ToString(), Tag(written[1], FixTags.SubscriptionRequestType));
		AreEqual(((int)MassStatusReqType.StatusForAllOrders).To<string>(), Tag(written[1], FixTags.MassStatusReqType));
	}

	/// <summary>
	/// Pins that an unsubscribe says it is one and names the subscription it ends, so the
	/// counterparty stops the right stream instead of opening another.
	/// </summary>
	[TestMethod]
	public async Task Dialect_OrderStatus_Unsubscribe_StatesItAndNamesTheSubscription()
	{
		CreateDialect();

		await SendAsync(new OrderStatusMessage
		{
			TransactionId = 2002,
			IsSubscribe = true,
		});

		await SendAsync(new OrderStatusMessage
		{
			TransactionId = 2003,
			OriginalTransactionId = 2002,
			IsSubscribe = false,
		});

		var written = Written();

		AreEqual(2, written.Length);

		AreEqual(SubscriptionRequestType.DisablePreviousSnapshotPlusUpdateRequest.ToString(), Tag(written[1], FixTags.SubscriptionRequestType));
		AreEqual("2002", Tag(written[1], FixTags.OrigClOrdID));
		IsNull(Tag(written[1], FixTags.MassStatusReqType), "An unsubscribe asked for a flavour of status report.");
	}

	/// <summary>
	/// Pins the Parties block: one entry per code that is there, each naming its own role - FIX
	/// PartyRole 3 is the client the order is for, 7 the firm entering it - and the announced count
	/// matching the entries. An order with no codes opens no group at all.
	/// </summary>
	[TestMethod]
	public async Task Dialect_Parties_AreOneEntryPerCodeWithItsRole()
	{
		CreateDialect();

		var both = CreateRegister(1001);
		both.ClientCode = "CL1";
		both.BrokerCode = "BR1";

		await SendAsync(both);

		var clientOnly = CreateRegister(1002);
		clientOnly.ClientCode = "CL1";

		await SendAsync(clientOnly);

		await SendAsync(CreateRegister(1003));

		var written = Written();

		AreEqual(3, written.Length);

		AreEqual("2", Tag(written[0], FixTags.NoPartyIDs));
		IsTrue(new[] { "CL1", "BR1" }.SequenceEqual(Tags(written[0], FixTags.PartyID)), "Both party codes did not reach the wire.");
		IsTrue(new[] { "3", "7" }.SequenceEqual(Tags(written[0], FixTags.PartyRole)), "The party roles are not client then entering firm.");
		IsTrue(new[] { "C", "C" }.SequenceEqual(Tags(written[0], FixTags.PartyIDSource)), "A party entry left its id source unstated.");

		AreEqual("1", Tag(written[1], FixTags.NoPartyIDs));
		IsTrue(new[] { "CL1" }.SequenceEqual(Tags(written[1], FixTags.PartyID)), "The client only order carried another party.");
		IsTrue(new[] { "3" }.SequenceEqual(Tags(written[1], FixTags.PartyRole)), "The client only order named another role.");

		IsNull(Tag(written[2], FixTags.NoPartyIDs), "An order with no party codes still opened a Parties group.");
	}

	/// <summary>
	/// Pins that a stop order's own prices travel in their own tags and leave the limit price alone:
	/// 145 activates the order, 150.25 is what it is then worth, 160 closes it in profit.
	/// </summary>
	[TestMethod]
	public async Task Dialect_StopOrderCondition_TravelsInItsOwnTags()
	{
		CreateDialect();

		var reg = CreateRegister(1001);

		reg.OrderType = OrderTypes.Conditional;
		reg.Condition = new FixOrderCondition
		{
			Type = FixStopOrderTypes.StopLoss,
			StopLoss = 145m,
			TakeProfit = 160m,
			Offset = 0.5m,
		};

		await SendAsync(reg);

		var order = Written().Single();

		// A stop that also carries a price of its own is OrdType 4, Stop Limit.
		AreEqual(OrdType.StopLimit.ToString(), Tag(order, FixTags.OrdType));
		AreEqual("145", Tag(order, FixTags.StopPx));
		AreEqual("160", Tag(order, FixTags.TakeProfit));
		AreEqual("0.5", Tag(order, FixTags.PegOffsetValue));
		AreEqual("0", Tag(order, FixTags.PegOffsetType));
		AreEqual("150.25", Tag(order, FixTags.Price));
	}

	/// <summary>
	/// Pins that the comment a caller wrote is what the counterparty reads while transliteration is
	/// off - the dialect speaks UTF-8, so non latin text needs no rewriting to travel.
	/// </summary>
	[TestMethod]
	public async Task Dialect_ConvertToLatin_Off_SendsTheTextAsWritten()
	{
		CreateDialect();

		var reg = CreateRegister(1001);
		reg.Comment = "\u041F\u0440\u0438\u0432\u0435\u0442";

		await SendAsync(reg);

		AreEqual("\u041F\u0440\u0438\u0432\u0435\u0442", Tag(Written().Single(), FixTags.Text));
	}

	/// <summary>
	/// Pins what the switch is for: with it on, text a latin-only counterparty cannot display comes
	/// out as latin characters only.
	/// </summary>
	[TestMethod]
	public async Task Dialect_ConvertToLatin_On_RewritesNonLatinText()
	{
		CreateDialect();

		((DefaultFixDialect)_dialect).ConvertToLatin = true;

		var reg = CreateRegister(1001);
		reg.Comment = "\u041F\u0440\u0438\u0432\u0435\u0442";

		await SendAsync(reg);

		var text = Tag(Written().Single(), FixTags.Text);

		IsNotNullOrEmpty(text);
		IsTrue(text.All(c => c < 128), $"Text left the dialect non latin: {text}.");
	}

	/// <summary>
	/// Pins that transliteration only replaces what is not latin: text that is already latin is
	/// nothing to convert, and a counterparty that matches a comment back to its own record needs it
	/// character for character.
	/// </summary>
	[TestMethod]
	public async Task Dialect_ConvertToLatin_On_LeavesAlreadyLatinTextAlone()
	{
		CreateDialect();

		((DefaultFixDialect)_dialect).ConvertToLatin = true;

		var reg = CreateRegister(1001);
		reg.Comment = "Order ACME-7";

		await SendAsync(reg);

		AreEqual("Order ACME-7", Tag(Written().Single(), FixTags.Text));
	}

	/// <summary>
	/// Pins the count in front of the security list of a data type request: none named announces
	/// zero, one named announces one and spells it out. A count that does not match the entries is
	/// what makes a counterparty read the rest of the message from the wrong place.
	/// </summary>
	[TestMethod]
	public async Task Dialect_DataTypeLookup_CountsTheSecuritiesItNames()
	{
		CreateDialect();

		await SendAsync(new DataTypeLookupMessage { TransactionId = 3001 });
		await SendAsync(new DataTypeLookupMessage { TransactionId = 3002, SecurityId = _aapl });

		var written = Written();

		AreEqual(2, written.Length);

		AreEqual(FixExtendedMessages.DataTypeLookup, Tag(written[0], FixTags.MsgType));
		AreEqual("3001", Tag(written[0], FixTags.MDReqID));
		AreEqual("0", Tag(written[0], FixTags.NoRelatedSym));
		IsNull(Tag(written[0], FixTags.Symbol), "A request naming no security still carried one.");

		AreEqual(FixExtendedMessages.DataTypeLookup, Tag(written[1], FixTags.MsgType));
		AreEqual("3002", Tag(written[1], FixTags.MDReqID));
		AreEqual("1", Tag(written[1], FixTags.NoRelatedSym));
		AreEqual("AAPL", Tag(written[1], FixTags.Symbol));
		AreEqual("NYSE", Tag(written[1], FixTags.SecurityExchange));
	}

	// A stream that takes the bytes and gives control back before they are down, as a socket does.
	private sealed class SlowStream : MemoryStream
	{
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var taken = buffer.AsSpan(offset, count).ToArray();

			await Task.Yield();

			Write(taken, 0, taken.Length);
		}

		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			var taken = buffer.ToArray();

			await Task.Yield();

			Write(taken, 0, taken.Length);
		}
	}

	/// <summary>
	/// A session is one stream of frames, whoever sends: requests started together go out one whole
	/// frame after another. Written into each other they are a frame the counterparty cannot read -
	/// it drops the session, and none of them is ever answered.
	/// </summary>
	[TestMethod]
	public async Task Dialect_RequestsSentAtOnce_GoOutAsWholeFrames()
	{
		CreateDialect();

		_outStream = new SlowStream();
		_writer = new TextFixWriter(_outStream, Encoding.UTF8);
		_dialect.Init(_writer, _reader, _address);

		var ids = Enumerable.Range(3001, 5).ToArray();

		await Task.WhenAll(ids.Select(id => Task.Run(() => SendAsync(new DataTypeLookupMessage { TransactionId = id, SecurityId = _aapl }).AsTask(), CancellationToken)));

		var written = Written();
		var all = written.JoinNL();

		AreEqual(ids.Length, written.Length, $"The requests did not go out a frame each:{Environment.NewLine}{all}");

		var soh = (char)AsciiSymbols.Soh;

		foreach (var frame in written)
		{
			AreEqual(FixExtendedMessages.DataTypeLookup, Tag(frame, FixTags.MsgType), $"Not a frame of a request:{Environment.NewLine}{all}");

			// A whole frame says how long its body is and sums to its own checksum.
			var bytes = frame.Replace('|', soh);
			var trailer = bytes.LastIndexOf($"{soh}{(int)FixTags.CheckSum}=", StringComparison.Ordinal) + 1;
			var body = bytes.IndexOf(soh, bytes.IndexOf($"{soh}{(int)FixTags.BodyLength}=", StringComparison.Ordinal) + 1) + 1;

			IsTrue(trailer > 0 && body > 0 && body <= trailer, $"A frame without its length or its checksum:{Environment.NewLine}{all}");

			AreEqual(Tag(frame, FixTags.BodyLength), Encoding.UTF8.GetByteCount(bytes[body..trailer]).To<string>(), $"A frame of another length than it says:{Environment.NewLine}{all}");
			AreEqual($"{Encoding.UTF8.GetBytes(bytes[..trailer]).Sum(b => b) % 256:000}", Tag(frame, FixTags.CheckSum), $"A frame that does not sum to its checksum:{Environment.NewLine}{all}");
		}

		// Every request went out once, and the session numbered its frames in the order they left.
		IsTrue(written.Select(f => Tag(f, FixTags.MDReqID).To<int>()).Order().SequenceEqual(ids), $"Not every request went out once:{Environment.NewLine}{all}");
		IsTrue(written.Select(f => Tag(f, FixTags.MsgSeqNum).To<long>()).SequenceEqual(Enumerable.Range(1, ids.Length).Select(n => (long)n)), $"The frames are not numbered in the order they left:{Environment.NewLine}{all}");
	}

	#endregion

	#region FixTags

	/// <summary>
	/// A tag number is the only thing that identifies a field on the wire - the name is ours, the
	/// number is the protocol's. Two names sharing one number means the field written as one is read
	/// back as the other, and every reverse lookup - a log line, a dialect switching on the tag it
	/// just read - names an arbitrary one of the two. Nothing else would notice: duplicate values in
	/// an enum are legal C# and compile without a word.
	/// </summary>
	[TestMethod]
	public void FixTags_CarryNoDuplicateTagNumbers()
	{
		var duplicates = Enumerator.GetNames<FixTags>()
			.GroupBy(n => (int)Enum.Parse<FixTags>(n))
			.Where(g => g.Count() > 1)
			.Select(g => $"{g.Key} = {g.JoinComma()}")
			.ToArray();

		duplicates.IsEmpty().AssertTrue($"{duplicates.Length} FIX tag number(s) are claimed by more than one name:{Environment.NewLine}{duplicates.JoinN()}");
	}

	/// <summary>
	/// The numbers below are not ours to choose: FIX assigns them, and a counterparty reads the frame
	/// by number alone. A wrong one is not a mislabelled constant - the value goes out in another
	/// field's slot, so the far side either rejects the message or, worse, acts on the wrong field.
	/// The handful spelled out here are the ones every session and every order passes through.
	/// </summary>
	[TestMethod]
	public void FixTags_UseTheNumbersTheProtocolAssigns()
	{
		// Framing.
		AreEqual(8, (int)FixTags.BeginString);
		AreEqual(9, (int)FixTags.BodyLength);
		AreEqual(35, (int)FixTags.MsgType);
		AreEqual(10, (int)FixTags.CheckSum);

		// Session.
		AreEqual(34, (int)FixTags.MsgSeqNum);
		AreEqual(49, (int)FixTags.SenderCompID);
		AreEqual(56, (int)FixTags.TargetCompID);
		AreEqual(52, (int)FixTags.SendingTime);
		AreEqual(43, (int)FixTags.PossDupFlag);
		AreEqual(45, (int)FixTags.RefSeqNum);
		AreEqual(58, (int)FixTags.Text);
		AreEqual(98, (int)FixTags.EncryptMethod);
		AreEqual(108, (int)FixTags.HeartBtInt);
		AreEqual(112, (int)FixTags.TestReqID);
		AreEqual(141, (int)FixTags.ResetSeqNumFlag);

		// Order entry.
		AreEqual(11, (int)FixTags.ClOrdID);
		AreEqual(41, (int)FixTags.OrigClOrdID);
		AreEqual(48, (int)FixTags.SecurityID);
		AreEqual(55, (int)FixTags.Symbol);
		AreEqual(54, (int)FixTags.Side);
		AreEqual(38, (int)FixTags.OrderQty);
		AreEqual(40, (int)FixTags.OrdType);
		AreEqual(44, (int)FixTags.Price);
		AreEqual(59, (int)FixTags.TimeInForce);
		AreEqual(60, (int)FixTags.TransactTime);

		// Execution reports.
		AreEqual(39, (int)FixTags.OrdStatus);
		AreEqual(150, (int)FixTags.ExecType);
		AreEqual(151, (int)FixTags.LeavesQty);
		AreEqual(14, (int)FixTags.CumQty);
		AreEqual(6, (int)FixTags.AvgPx);

		// FIX numbers its fields from one, so a tag that is zero or negative could never be written
		// into a frame at all.
		var outOfRange = Enumerator.GetNames<FixTags>()
			.Where(n => (int)Enum.Parse<FixTags>(n) < 1)
			.ToArray();

		outOfRange.IsEmpty().AssertTrue($"These tags carry a number no FIX frame can express: {outOfRange.JoinComma()}");
	}

	#endregion

	#region Dialect type resolution

	[TestMethod]
	public void ToDialect_AssemblyQualifiedName()
	{
		var logs = new Mock<ILogReceiver>();

		"StockSharp.Fix.Dialects.DefaultFixDialect, StockSharp.Fix.Dialects"
			.ToDialect(logs.Object)
			.AssertEqual(typeof(DefaultFixDialect));
	}

	[TestMethod]
	public void ToDialect_NamesAnotherAssembly_ResolvesNothing()
	{
		var logs = new Mock<ILogReceiver>();

		// The name is looked up as written: a dialect named in an assembly that does not hold it
		// is not found, and the caller falls back to the default dialect.
		"StockSharp.Fix.Dialects.DefaultFixDialect, StockSharp.Fix.Core"
			.ToDialect(logs.Object)
			.AssertNull();
	}

	[TestMethod]
	public void ToDialect_UnknownType_ResolvesNothing()
	{
		var logs = new Mock<ILogReceiver>();

		"NonExistent.Type, NonExistent.Assembly"
			.ToDialect(logs.Object)
			.AssertNull();
	}

	[TestMethod]
	public void ToDialect_NoName_ResolvesNothing()
	{
		var logs = new Mock<ILogReceiver>();

		((string)null).ToDialect(logs.Object).AssertNull();
		string.Empty.ToDialect(logs.Object).AssertNull();
	}

	[TestMethod]
	public void ToDialect_NoLogs_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() =>
			"StockSharp.Fix.Dialects.DefaultFixDialect, StockSharp.Fix.Dialects".ToDialect(null));
	}

	#endregion

	/// <summary>
	/// A counterparty that answers order status by replaying its transaction log, which is what
	/// drives the dialect to ask for a resend instead of sending an order status request.
	/// </summary>
	private class ResendFixDialect(IdGenerator transactionIdGenerator) : DefaultFixDialect(transactionIdGenerator)
	{
		/// <inheritdoc />
		public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages { get; } =
		[
			MessageTypes.MarketData.ToInfo(),
			MessageTypes.OrderRegister.ToInfo(),
			MessageTypes.OrderCancel.ToInfo(),
		];
	}
}
