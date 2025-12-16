namespace StockSharp.Tinkoff.Native;

/// <summary>
/// Lightweight facade over generated gRPC service clients.
/// </summary>
internal sealed class InvestApiClient
{
	/// <summary>
	/// Initializes a new instance of <see cref="InvestApiClient"/>.
	/// </summary>
	/// <param name="callInvoker">gRPC call invoker.</param>
	public InvestApiClient(CallInvoker callInvoker)
	{
		Users = new UsersService.UsersServiceClient(callInvoker);
		Instruments = new InstrumentsService.InstrumentsServiceClient(callInvoker);
		MarketData = new MarketDataService.MarketDataServiceClient(callInvoker);
		MarketDataStream = new MarketDataStreamService.MarketDataStreamServiceClient(callInvoker);
		Orders = new OrdersService.OrdersServiceClient(callInvoker);
		OrdersStream = new OrdersStreamService.OrdersStreamServiceClient(callInvoker);
		Operations = new OperationsService.OperationsServiceClient(callInvoker);
		OperationsStream = new OperationsStreamService.OperationsStreamServiceClient(callInvoker);
		StopOrders = new StopOrdersService.StopOrdersServiceClient(callInvoker);
		Sandbox = new SandboxService.SandboxServiceClient(callInvoker);
	}

	/// <summary>
	/// Users service client.
	/// </summary>
	public UsersService.UsersServiceClient Users { get; }

	/// <summary>
	/// Instruments service client.
	/// </summary>
	public InstrumentsService.InstrumentsServiceClient Instruments { get; }

	/// <summary>
	/// Market data service client.
	/// </summary>
	public MarketDataService.MarketDataServiceClient MarketData { get; }

	/// <summary>
	/// Market data stream service client.
	/// </summary>
	public MarketDataStreamService.MarketDataStreamServiceClient MarketDataStream { get; }

	/// <summary>
	/// Orders service client.
	/// </summary>
	public OrdersService.OrdersServiceClient Orders { get; }

	/// <summary>
	/// Orders stream service client.
	/// </summary>
	public OrdersStreamService.OrdersStreamServiceClient OrdersStream { get; }

	/// <summary>
	/// Operations service client.
	/// </summary>
	public OperationsService.OperationsServiceClient Operations { get; }

	/// <summary>
	/// Operations stream service client.
	/// </summary>
	public OperationsStreamService.OperationsStreamServiceClient OperationsStream { get; }

	/// <summary>
	/// Stop orders service client.
	/// </summary>
	public StopOrdersService.StopOrdersServiceClient StopOrders { get; }

	/// <summary>
	/// Sandbox service client.
	/// </summary>
	public SandboxService.SandboxServiceClient Sandbox { get; }
}
