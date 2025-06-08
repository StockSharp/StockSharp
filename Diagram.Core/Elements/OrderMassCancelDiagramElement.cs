namespace StockSharp.Diagram.Elements;

/// <summary>
/// Order mass cancelling element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderMassCancellingKey,
	Description = LocalizedStrings.OrderMassCancellingKey,
	GroupName = LocalizedStrings.OrdersKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html")]
public class OrderMassCancelDiagramElement : OrderBaseDiagramElement
{
	private readonly DiagramSocket _portfolioSocket;
	private readonly DiagramSocket _securitySocket;

	private readonly DiagramSocket _resultSocket;

	private long? _transactionId;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "5FC4B091-50F2-43A2-B17B-D5786807D674".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "ShoppingCartDelete";

	private readonly DiagramElementParam<Sides?> _direction;

	/// <summary>
	/// Direction.
	/// </summary>
	public Sides? Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderMassCancelDiagramElement"/>.
	/// </summary>
	public OrderMassCancelDiagramElement()
	{
		_portfolioSocket = AddInput(StaticSocketIds.Portfolio, LocalizedStrings.Portfolio, DiagramSocketType.Portfolio);
		_securitySocket = AddInput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security);

		_resultSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Result, DiagramSocketType.Bool);

		_direction = AddParam<Sides?>(nameof(Direction))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.Direction, LocalizedStrings.PosSide, 30);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_transactionId = default;
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		var provider = (ITransactionProvider)Strategy;

		provider.MassOrderCanceled2 += OnMassOrderCanceled;
		provider.MassOrderCancelFailed2 += OnMassOrderCancelFailed;

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		var provider = (ITransactionProvider)Strategy;

		provider.MassOrderCanceled2 -= OnMassOrderCanceled;
		provider.MassOrderCancelFailed2 -= OnMassOrderCancelFailed;

		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		if (!CanProcess(values))
			return;

		Portfolio portfolio = null;

		if (values.TryGetValue(_portfolioSocket, out var pfValue))
			portfolio = pfValue.GetValue<Portfolio>();

		Security security = null;

		if (values.TryGetValue(_securitySocket, out var secValue))
			security = secValue.GetValue<Security>();

		var provider = (ITransactionProvider)Strategy;

		provider.CancelOrders(
			portfolio: portfolio,
			direction: Direction,
			security: security,
			transactionId: (_transactionId = provider.TransactionIdGenerator.GetNextId()));
	}

	private void OnMassOrderCancelFailed(long transactionId, Exception error, DateTimeOffset time)
	{
		RaiseOutput(transactionId, false, time);
	}

	private void OnMassOrderCanceled(long transactionId, DateTimeOffset time)
	{
		RaiseOutput(transactionId, true, time);
	}

	private void RaiseOutput(long transactionId, bool result, DateTimeOffset time)
	{
		if (transactionId != _transactionId)
			return;

		RaiseProcessOutput(_resultSocket, time, result);
		_transactionId = null;

		Strategy.Flush(time);
	}
}
