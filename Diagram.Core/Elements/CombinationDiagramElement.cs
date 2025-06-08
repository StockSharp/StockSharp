namespace StockSharp.Diagram.Elements;

/// <summary>
/// Combined values element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CombinationKey,
	Description = LocalizedStrings.CombinationElementDescriptionKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/combination.html")]
public class CombinationDiagramElement : TypedDiagramElement<CombinationDiagramElement>
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "03BBD944-D369-4860-8D35-A642CB06FE5E".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Pipe";

	static CombinationDiagramElement() => SocketTypesSource.SetValues(DiagramSocketType.AllTypes);

	/// <summary>
	/// Initializes a new instance of the <see cref="CombinationDiagramElement"/>.
	/// </summary>
	public CombinationDiagramElement()
		: base(LocalizedStrings.Combination)
	{
		InputSockets.First().LinkableMaximum = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void TypeChanged()
	{
		UpdateOutputSocketType();
	}

	/// <inheritdoc />
	protected override void OnProcess(DiagramSocketValue value)
	{
		RaiseProcessOutput(OutputSocket, value.Time, value.Value, value);
	}
}