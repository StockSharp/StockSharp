namespace StockSharp.Diagram.Elements;

/// <summary>
/// The diagram element to send the final value (<see cref="CandleStates.Finished"/> or <see cref="IIndicatorValue.IsFinal"/>).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FinalKey,
	Description = LocalizedStrings.SendOnlyFinalKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/final_value.html")]
public class FinalValueDiagramElement : TypedDiagramElement<FinalValueDiagramElement>
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "0A3A0B71-3D60-45C4-9876-71972EF11650".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Finish";

	/// <summary>
	/// Initializes a new instance of the <see cref="FinalValueDiagramElement"/>.
	/// </summary>
	public FinalValueDiagramElement()
		: base(LocalizedStrings.Final)
	{
	}

	/// <inheritdoc />
	protected override void TypeChanged()
	{
		UpdateOutputSocketType();
	}

	/// <inheritdoc />
	protected override void OnProcess(DiagramSocketValue value)
	{
		if (value.IsFinal() == false)
			return;

		RaiseProcessOutput(OutputSocket, value.Time, value.Value, value);
	}
}