namespace StockSharp.Tinkoff.Native;

/// <summary>
/// Lightweight facade over generated gRPC service clients.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="InvestApiClient"/>.
/// </remarks>
/// <param name="callInvoker">gRPC call invoker.</param>
internal sealed class InvestApiClient(CallInvoker callInvoker)
{
	/// <summary>
	/// Users service client.
	/// </summary>
	public UsersService.UsersServiceClient Users { get; } = new UsersService.UsersServiceClient(callInvoker);

	/// <summary>
	/// Instruments service client.
	/// </summary>
	public InstrumentsService.InstrumentsServiceClient Instruments { get; } = new InstrumentsService.InstrumentsServiceClient(callInvoker);

	/// <summary>
	/// Market data service client.
	/// </summary>
	public MarketDataService.MarketDataServiceClient MarketData { get; } = new MarketDataService.MarketDataServiceClient(callInvoker);

	/// <summary>
	/// Market data stream service client.
	/// </summary>
	public MarketDataStreamService.MarketDataStreamServiceClient MarketDataStream { get; } = new MarketDataStreamService.MarketDataStreamServiceClient(callInvoker);

	/// <summary>
	/// Orders service client.
	/// </summary>
	public OrdersService.OrdersServiceClient Orders { get; } = new OrdersService.OrdersServiceClient(callInvoker);

	/// <summary>
	/// Orders stream service client.
	/// </summary>
	public OrdersStreamService.OrdersStreamServiceClient OrdersStream { get; } = new OrdersStreamService.OrdersStreamServiceClient(callInvoker);

	/// <summary>
	/// Operations service client.
	/// </summary>
	public OperationsService.OperationsServiceClient Operations { get; } = new OperationsService.OperationsServiceClient(callInvoker);

	/// <summary>
	/// Operations stream service client.
	/// </summary>
	public OperationsStreamService.OperationsStreamServiceClient OperationsStream { get; } = new OperationsStreamService.OperationsStreamServiceClient(callInvoker);

	/// <summary>
	/// Stop orders service client.
	/// </summary>
	public StopOrdersService.StopOrdersServiceClient StopOrders { get; } = new StopOrdersService.StopOrdersServiceClient(callInvoker);

	/// <summary>
	/// Sandbox service client.
	/// </summary>
	public SandboxService.SandboxServiceClient Sandbox { get; } = new SandboxService.SandboxServiceClient(callInvoker);
}
