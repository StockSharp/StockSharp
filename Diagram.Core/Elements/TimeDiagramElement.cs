namespace StockSharp.Diagram.Elements;

/// <summary>
/// The element is used to provide current time.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TimeKey,
	Description = LocalizedStrings.CurrentTimeElementKey,
	GroupName = LocalizedStrings.TimeKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/time/current_time.html")]
public class TimeDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "2DA039CC-F6E0-47E3-B997-3A24CCFD62EE".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Clock2";

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeDiagramElement"/>.
	/// </summary>
	public TimeDiagramElement()
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.Date);
	}

	/// <inheritdoc/>
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		FlushPriority = FlushNormal;

		RaiseTime(time);
	}

	/// <inheritdoc/>
	public override void Flush(DateTimeOffset time)
	{
		RaiseTime(time);
	}

	private void RaiseTime(DateTimeOffset time)
		=> RaiseProcessOutput(_outputSocket, time, time);
}