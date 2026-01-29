namespace StockSharp.Tests;

class PassThroughMessageAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
{
	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		SendOutMessage(message);
		return default;
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
}

