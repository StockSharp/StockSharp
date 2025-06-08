namespace StockSharp.Diagram.Elements;

/// <summary>
/// A diagram element in the form of a Flag. It is set and reset based on the incoming sockets. The output value is transmitted only in the case of the initial setting of the flag.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FlagKey,
	Description = LocalizedStrings.FlagElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/flag.html")]
public class FlagDiagramElement : DiagramElement
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "D9273C2C-6789-4BA8-A5C5-2B1EB4C4F006".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Flag";

	private readonly DiagramSocket _outputSocket;

	private bool _isSet;

	/// <summary>
	/// Initializes a new instance of the <see cref="FlagDiagramElement"/>.
	/// </summary>
    public FlagDiagramElement()
    {
		AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, OnProcessTrigger, int.MaxValue);
		AddInput(StaticSocketIds.Flag, LocalizedStrings.Reset, DiagramSocketType.Any, OnProcessReset, int.MaxValue);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Signal, DiagramSocketType.Bool);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isSet = default;
	}

	private void OnProcessTrigger(DiagramSocketValue value)
	{
		if (_isSet || value.GetValue<bool?>() == false)
			return;

		_isSet = true;

		RaiseProcessOutput(_outputSocket, value.Time, true, value);
	}

	private void OnProcessReset(DiagramSocketValue value)
	{
		if (value.GetValue<bool?>() == false)
			return;

		_isSet = default;
	}
}