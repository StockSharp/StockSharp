namespace StockSharp.Diagram.Elements;

/// <summary>
/// Position element (for security and money) for the specified portfolio.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PnLChangeKey,
	Description = LocalizedStrings.PnLChangeKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html")]
public class StrategyPnLDiagramElement : DiagramElement
{
	private readonly DiagramSocket _pnlUnrealizedSocket;
	private readonly DiagramSocket _pnlRealizedSocket;
	private readonly DiagramSocket _commissionSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "C8DCDD4A-714D-45C2-AD32-93CA5C48F771".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Money";

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionDiagramElement"/>.
	/// </summary>
	public StrategyPnLDiagramElement()
	{
		_pnlUnrealizedSocket = AddOutput(StaticSocketIds.PnLUnreal, LocalizedStrings.PnLUnreal, DiagramSocketType.Unit);
		_pnlRealizedSocket = AddOutput(StaticSocketIds.PnLRealized, LocalizedStrings.PnLRealized, DiagramSocketType.Unit);
		_commissionSocket = AddOutput(StaticSocketIds.Commission, LocalizedStrings.Commission, DiagramSocketType.Unit);
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		Strategy.PnLReceived2 += OnStrategyPnLReceived;

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		Strategy.PnLReceived2 -= OnStrategyPnLReceived;

		base.OnStop();
	}

	private void OnStrategyPnLReceived(Subscription subscription, Portfolio pf, DateTimeOffset time, decimal realized, decimal? unrealized, decimal? commission)
	{
		RaiseProcessOutput(_pnlUnrealizedSocket, time, new Unit(unrealized ?? 0), null, subscription);
		RaiseProcessOutput(_pnlRealizedSocket, time, new Unit(realized), null, subscription);
		RaiseProcessOutput(_commissionSocket, time, new Unit(commission ?? 0), null, subscription);
	}
}