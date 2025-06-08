namespace StockSharp.Diagram.Elements;

/// <summary>
/// Two values comparison element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ComparisonKey,
	Description = LocalizedStrings.TwoValuesComparisonElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/comparison.html")]
public sealed class ComparisonDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "95CA0E17-8579-48D9-9228-63E50B7D78F6".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Workflow";

	private readonly DiagramElementParam<DiagramSocket> _leftValue;

	/// <summary>
	/// Left operand.
	/// </summary>
	public DiagramSocket LeftValue
	{
		get => _leftValue.Value;
		set => _leftValue.Value = value;
	}

	private readonly DiagramElementParam<ComparisonOperator?> _operator;

	/// <summary>
	/// Operator.
	/// </summary>
	public ComparisonOperator? Operator
	{
		get => _operator.Value;
		set => _operator.Value = value;
	}

	private readonly DiagramElementParam<DiagramSocket> _rightValue;

	/// <summary>
	/// Right operand.
	/// </summary>
	public DiagramSocket RightValue
	{
		get => _rightValue.Value;
		set => _rightValue.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ComparisonDiagramElement"/>.
	/// </summary>
	public ComparisonDiagramElement()
	{
		var left = AddInput(StaticSocketIds.Input, LocalizedStrings.Value + " 1", DiagramSocketType.Comparable);
		var right = AddInput(StaticSocketIds.SecondInput, LocalizedStrings.Value + " 2", DiagramSocketType.Comparable);

		_outputSocket = AddOutput(StaticSocketIds.Signal, LocalizedStrings.Signal, DiagramSocketType.Bool);

		_leftValue = AddParam(nameof(LeftValue), left)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Condition, LocalizedStrings.Left, LocalizedStrings.LeftOperand, 10)
			.SetEditor(new TemplateEditorAttribute { TemplateKey = "InputSocketsEditorTemplate" })
			.SetSaveLoadHandlers(SaveSocket, LoadSocket);

		_operator = AddParam<ComparisonOperator?>(nameof(Operator))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Condition, LocalizedStrings.Operator, LocalizedStrings.EqualityOperator, 20)
			.SetOnValueChangedHandler(value => SetElementName(value?.GetDisplayName()));

		_rightValue = AddParam(nameof(RightValue), right)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Condition, LocalizedStrings.Right, LocalizedStrings.RightOperand, 30)
			.SetEditor(new TemplateEditorAttribute { TemplateKey = "InputSocketsEditorTemplate" })
			.SetSaveLoadHandlers(SaveSocket, LoadSocket);
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (Operator == null)
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Comparison));

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		if (Operator == null)
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Comparison));

		if (!values.ContainsKey(_leftValue.Value))
			throw new InvalidOperationException(LocalizedStrings.ParameterIsEmptyParams.Put(_leftValue.Value.Name));

		if (!values.ContainsKey(_rightValue.Value))
			throw new InvalidOperationException(LocalizedStrings.ParameterIsEmptyParams.Put(_rightValue.Value.Name));

		var leftValue = values[_leftValue.Value];
		var rightValue = values[_rightValue.Value];

		var left = leftValue.GetValue<Unit>(true);
		var right = rightValue.GetValue<Unit>(true);

		bool isMatched;

		if (left is null || right is null)
			isMatched = false;
		else
		{
			isMatched = Operator switch
			{
				ComparisonOperator.Equal => left.Compare(right) == 0,
				ComparisonOperator.NotEqual => left.Compare(right) != 0,
				ComparisonOperator.Greater => left.Compare(right) == 1,
				ComparisonOperator.GreaterOrEqual => left.Compare(right) >= 0,
				ComparisonOperator.Less => left.Compare(right) == -1,
				ComparisonOperator.LessOrEqual => left.Compare(right) <= 0,
				ComparisonOperator.Any => true,
				_ => throw new InvalidOperationException(Operator.To<string>()),
			};
		}

		RaiseProcessOutput(_outputSocket, time, isMatched, source);
	}

	private static SettingsStorage SaveSocket(DiagramSocket socket)
	{
		if (socket == null)
			return null;

		var settings = new SettingsStorage();
		settings.SetValue(nameof(DiagramSocket.Id), socket.Id);
		return settings;
	}

	private DiagramSocket LoadSocket(SettingsStorage settings)
	{
		var id = settings.GetValue<string>(nameof(DiagramSocket.Id));
		return id != null ? InputSockets.FindById(id) : null;
	}
}