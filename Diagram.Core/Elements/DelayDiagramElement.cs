namespace StockSharp.Diagram.Elements;

/// <summary>
/// The diagram element to delay signal for specified number of input values.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.DelaySignalKey,
	Description = LocalizedStrings.DelaySignalDescKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/delay_value.html")]
public class DelayDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private int? _counter;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "823A7255-F6E1-47B6-8186-4B58FFDE0A78".To<Guid>();

	/// <inheritdoc />
	public override string IconName => "Alert";

	private readonly DiagramElementParam<int> _n;

	/// <summary>
	/// Number of value to delay signal for.
	/// </summary>
	public int N
	{
		get => _n.Value;
		set => _n.Value = value;
	}

	private readonly DiagramElementParam<bool> _isFinishedOnly;

	/// <summary>
	/// Send only finished values.
	/// </summary>
	public bool IsFinishedOnly
	{
		get => _isFinishedOnly.Value;
		set => _isFinishedOnly.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DelayDiagramElement"/>.
	/// </summary>
	public DelayDiagramElement()
	{
		AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, OnProcessTrigger, int.MaxValue);
		var input = AddInput(StaticSocketIds.Input, LocalizedStrings.Input, DiagramSocketType.Any, OnProcessInput);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Signal, DiagramSocketType.Bool);

		_n = AddParam(nameof(N), 3)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Delay, LocalizedStrings.DelayLength, LocalizedStrings.DelayLengthDetails, 30)
			.SetCanOptimize()
			.SetOnValueChangingHandler((_, newValue) =>
			{
				if (newValue < 1)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.InvalidValue);
			})
			.SetOnValueChangedHandler(v => SetElementName(LocalizedStrings.DelayParams.Put(v)));

		_isFinishedOnly = AddParam(nameof(IsFinishedOnly), true)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Delay, LocalizedStrings.Finished, LocalizedStrings.ProcessOnlyFormed, 40);

		SetElementName(LocalizedStrings.DelayParams.Put("N"));

		ShowParameters = true;
	}

	/// <inheritdoc/>
	protected override void OnReseted()
	{
		base.OnReseted();

		_counter = default;
	}

	private void OnProcessTrigger(DiagramSocketValue value)
	{
		if (_counter is not null || value.GetValue<bool?>() == false)
			return;

		_counter = N;

		LogDebug($"Delay activated: N={N}");
	}

	private void OnProcessInput(DiagramSocketValue value)
	{
		if (_counter is null)
			return;

		if (IsFinishedOnly && value.IsFinal() == false)
			return;

		if (--_counter > 0)
			return;

		_counter = null;

		RaiseProcessOutput(_outputSocket, value.Time, true, value);
	}
}