namespace StockSharp.Diagram.Elements;

/// <summary>
/// Crossing element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CrossingKey,
	Description = LocalizedStrings.CrossingElementDescriptionKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/crossing.html")]
public class CrossingDiagramElement : DiagramElement
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "DA1D00FC-3CCC-4769-A692-5938E8ACA201".To<Guid>();

	/// <inheritdoc />
	public override string IconName => "Shuffle";

	private readonly DiagramSocket _input1;

	private class ValuePair
	{
		public Unit Value1 { get; private set; }
		public Unit Value2 { get; private set; }

		public bool IsFilled => Value1 is not null && Value2 is not null;

		public bool IsUp
		{
			get
			{
				var v1 = Value1;
				var v2 = Value2;

				if (v1 is null || v2 is null)
					throw new InvalidOperationException();

				return v1 >= v2;
			}
		}

		public void Set(Unit val, bool isFirst)
		{
			if (isFirst)
				Value1 = val;
			else
				Value2 = val;
		}

		public void SetFrom(ValuePair other)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			Value1 = other.Value1;
			Value2 = other.Value2;
		}

		public void Reset() => Value1 = Value2 = null;
	}

	private readonly ValuePair _currentValues = new();
	private readonly ValuePair _previousValues = new();
	private readonly DiagramSocket _outputSocket;

	/// <summary>
	/// Initializes a new instance of the <see cref="CrossingDiagramElement"/>.
	/// </summary>
	public CrossingDiagramElement()
	{
		var input1Name = $"{LocalizedStrings.Input} {LocalizedStrings.Up}";
		var input2Name = $"{LocalizedStrings.Input} {LocalizedStrings.Down}";

		_input1 = AddInput("Input Up", input1Name, DiagramSocketType.Any, OnProcess, isDynamic: false);
		var input2 = AddInput("Input Down", input2Name, DiagramSocketType.Any, OnProcess, isDynamic: false);

		_input1.AvailableTypes.Clear();
		_input1.AvailableTypes.Add(DiagramSocketType.IndicatorValue);
		_input1.AvailableTypes.Add(DiagramSocketType.Unit);
		_input1.AvailableTypes.Add(DiagramSocketType.Comparable);

		input2.AvailableTypes.Clear();
		input2.AvailableTypes.Add(DiagramSocketType.IndicatorValue);
		input2.AvailableTypes.Add(DiagramSocketType.Unit);
		input2.AvailableTypes.Add(DiagramSocketType.Comparable);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.Bool);
	}

	private void OnProcess(DiagramSocketValue sock)
	{
		if (sock is null)
			throw new ArgumentNullException(nameof(sock));

		_currentValues.Set(sock.GetValue<Unit>(true), sock.Socket == _input1);

		if (!_currentValues.IsFilled)
			return;

		void shift()
		{
			_previousValues.SetFrom(_currentValues);
			_currentValues.Reset();
		}

		if (!_previousValues.IsFilled)
		{
			shift();
			return;
		}

		var isCurrUp = _currentValues.IsUp;
		var isCrossing = isCurrUp == !_previousValues.IsUp;

		shift();

		if (isCrossing)
			RaiseProcessOutput(_outputSocket, sock.Time, isCurrUp, sock);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentValues.Reset();
		_previousValues.Reset();
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source) { }
}