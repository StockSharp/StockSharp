namespace StockSharp.Diagram.Elements;

/// <summary>
/// Previous value receiving element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PreviousValueKey,
	Description = LocalizedStrings.PreviousValueElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/prev_value.html")]
public class PreviousValueDiagramElement : TypedDiagramElement<PreviousValueDiagramElement>
{
	private readonly Queue<object> _queue = [];

	/// <inheritdoc />
	public override Guid TypeId { get; } = "835C9AE0-906A-48D6-9E20-A332B6350FD0".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Back";

	private readonly DiagramElementParam<int> _shift;

	/// <summary>
	/// Shift.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
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

	static PreviousValueDiagramElement() => SocketTypesSource.SetValues(DiagramSocketType.AllTypes);

	/// <summary>
	/// Initializes a new instance of the <see cref="PreviousValueDiagramElement"/>.
	/// </summary>
	public PreviousValueDiagramElement()
		: base(LocalizedStrings.Previous)
	{
		_shift = AddParam(nameof(Shift), 1)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Previous, LocalizedStrings.Shift, LocalizedStrings.OffsetSize, 30)
			.SetCanOptimize()
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (newValue < 1)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.InvalidValue);
			})
			.SetOnValueChangedHandler(value => SetElementName(LocalizedStrings.ShiftIs.Put(value)));

		_isFinishedOnly = AddParam(nameof(IsFinishedOnly), true)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Previous, LocalizedStrings.Finished, LocalizedStrings.ProcessOnlyFormed, 40);

		ShowParameters = true;
	}

	/// <inheritdoc />
	protected override void TypeChanged()
	{
		UpdateOutputSocketType();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_queue.Clear();
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (Type == null)
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Connection));

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnProcess(DiagramSocketValue value)
	{
		if (IsFinishedOnly && value.IsFinal() == false)
			return;

		if (_queue.Count >= Shift)
			RaiseProcessOutput(OutputSocket, value.Time, _queue.Dequeue(), value);

		_queue.Enqueue(value.Value);
	}
}