namespace StockSharp.Diagram.Elements;

/// <summary>
/// Strategy trades element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StrategyTradesKey,
	Description = LocalizedStrings.StrategyTradesElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html")]
public class StrategyTradesDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private Security _security;
	private IMarketRule _rule;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "7162155B-ECAA-4248-84FF-B1046E753562".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Deal";

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyTradesDiagramElement"/>.
	/// </summary>
	public StrategyTradesDiagramElement()
	{
		ProcessNullValues = false;

		AddInput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security, OnProcessSecurity);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Trades, DiagramSocketType.MyTrade);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rule = default;
		_security = default;
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		OnSubscribe();
		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		OnUnSubscribe();
		base.OnStop();
	}

	private void OnSubscribe()
	{
		_rule = Strategy
			.WhenOwnTradeReceived()
			.Do(trade =>
			{
				if (_security != null && trade.Order.Security != _security)
					return;

				RaiseProcessOutput(_outputSocket, trade.Trade.ServerTime, trade);
				Strategy.Flush(trade.Trade);
			})
			.Apply(Strategy);
	}

	private void OnUnSubscribe()
	{
		if (_rule != null)
			Strategy.TryRemoveRule(_rule);
	}

	private void OnProcessSecurity(DiagramSocketValue value)
	{
		_security = value.GetValue<Security>();
	}
}