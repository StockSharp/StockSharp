namespace StockSharp.Diagram.Elements;

/// <summary>
/// Random value element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RandomKey,
	Description = LocalizedStrings.RandomElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/random.html")]
public class RandomDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;
	private readonly HashSet<DiagramSocket> _triggerLinks = [];

	/// <inheritdoc />
	public override Guid TypeId { get; } = "F75E5DA0-36C4-44A0-9DA1-9E0174E30729".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Dices";

	private readonly DiagramElementParam<decimal> _min;

	/// <summary>
	/// Minimum.
	/// </summary>
	public decimal Min
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	private readonly DiagramElementParam<decimal> _max;

	/// <summary>
	/// Maximum.
	/// </summary>
	public decimal Max
	{
		get => _max.Value;
		set => _max.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RandomDiagramElement"/>.
	/// </summary>
	public RandomDiagramElement()
	{
		var triggerSocket = AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, OnProcessTrigger, int.MaxValue);

		triggerSocket.Connected += OnTriggerSocketConnected;
		triggerSocket.Disconnected += OnTriggerSocketDisconnected;

		_min = AddParam<decimal>(nameof(Min), 0)
			.SetBasic(true)
			.SetCanOptimize()
			.SetDisplay(LocalizedStrings.Random, LocalizedStrings.Min, LocalizedStrings.Minimum, 11);

		_max = AddParam<decimal>(nameof(Max), 100)
			.SetBasic(true)
			.SetCanOptimize()
			.SetDisplay(LocalizedStrings.Random, LocalizedStrings.Max, LocalizedStrings.Maximum, 12);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Value, DiagramSocketType.Unit);

		ShowParameters = true;
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		FlushPriority = _triggerLinks.Any() ? FlushDisabled : FlushNormal;

		if (FlushPriority == FlushNormal)
			RaiseRandomOuput(time);
	}

	/// <inheritdoc />
	public override void Flush(DateTimeOffset time)
	{
		RaiseRandomOuput(time);
	}

	private void RaiseRandomOuput(DateTimeOffset time)
	{
		var value = RandomGen.GetDecimal(Min, Max, 2);
		RaiseProcessOutput(_outputSocket, time, (Unit)value);
	}

	private void OnProcessTrigger(DiagramSocketValue value)
	{
		if (value.GetValue<bool?>() == false)
			return;

		RaiseRandomOuput(value.Time);
	}

	private void OnTriggerSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.IsOutput)
			return;

		_triggerLinks.Add(source);
	}

	private void OnTriggerSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.IsOutput)
			return;

		if (source != null)
			_triggerLinks.Remove(source);
	}
}