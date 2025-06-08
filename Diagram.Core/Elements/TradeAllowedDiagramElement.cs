namespace StockSharp.Diagram.Elements;

/// <summary>
/// Is trade allowed verification element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IsTradeAllowedKey,
	Description = LocalizedStrings.IsTradeAllowedElementDescriptionKey,
	GroupName = LocalizedStrings.TimeKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html")]
public class TradeAllowedDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "E28C9FD2-B1C4-46E4-8178-585E7D6841AF".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Verify";

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeAllowedDiagramElement"/>.
	/// </summary>
	public TradeAllowedDiagramElement()
	{
		AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, OnProcessTrigger, int.MaxValue);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.Bool);
	}

	private void OnProcessTrigger(DiagramSocketValue value)
	{
		if (!Strategy.IsFormedAndOnlineAndAllowTrading())
		{
			RaiseProcessOutput(_outputSocket, value.Time, false, value);
			return;
		}

		if (!Strategy.IsBacktesting && value.Time < Strategy.StartedTime)
		{
			RaiseProcessOutput(_outputSocket, value.Time, false, value);
			return;
		}

		RaiseProcessOutput(_outputSocket, value.Time, true, value);
	}
}