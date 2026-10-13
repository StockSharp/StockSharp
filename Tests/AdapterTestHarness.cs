namespace StockSharp.Tests;

class PassThroughMessageAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(message, cancellationToken);
	}
}

class RecordingPassThroughMessageAdapter : PassThroughMessageAdapter
{
	private readonly DataType[] _supportedMarketDataTypes;
	private readonly IEnumerable<int> _supportedOrderBookDepths;
	private readonly Func<SecurityId, IOrderLogMarketDepthBuilder> _createOrderLogMarketDepthBuilder;

	public RecordingPassThroughMessageAdapter(
		IEnumerable<DataType> supportedMarketDataTypes = null,
		IEnumerable<int> supportedOrderBookDepths = null,
		Func<SecurityId, IOrderLogMarketDepthBuilder> createOrderLogMarketDepthBuilder = null)
		: base(new IncrementalIdGenerator())
	{
		_supportedMarketDataTypes = supportedMarketDataTypes?.ToArray() ?? [];
		_supportedOrderBookDepths = supportedOrderBookDepths ?? [];
		_createOrderLogMarketDepthBuilder = createOrderLogMarketDepthBuilder;
	}

	public List<Message> InMessages { get; } = [];

	public override IAsyncEnumerable<DataType> GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
		=> _supportedMarketDataTypes.ToAsyncEnumerable();

	public override IEnumerable<int> SupportedOrderBookDepths
		=> _supportedOrderBookDepths;

	public override IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> _createOrderLogMarketDepthBuilder?.Invoke(securityId) ?? base.CreateOrderLogMarketDepthBuilder(securityId);

	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		InMessages.Add(message);
		return base.SendInMessageAsync(message, cancellationToken);
	}

	// The base implementation reconstructs the adapter from its (IdGenerator) constructor, which
	// this one does not have; a copy gets the same configuration and a recording list of its own.
	public override ValueTask<IMessageAdapter> CloneAsync(CancellationToken cancellationToken)
		=> new(new RecordingPassThroughMessageAdapter(_supportedMarketDataTypes, _supportedOrderBookDepths, _createOrderLogMarketDepthBuilder));
}

