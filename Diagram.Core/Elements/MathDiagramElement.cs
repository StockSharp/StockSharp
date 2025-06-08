namespace StockSharp.Diagram.Elements;

using Ecng.Compilation;
using Ecng.Compilation.Expressions;

/// <summary>
/// Formula with two arguments element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FormulaKey,
	Description = LocalizedStrings.MathFormulaDescKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/formula.html")]
public class MathDiagramElement : DiagramElement
{
	private class FormulaSource : ItemsSourceBase<string>
	{
		private static readonly HashSet<string> _functions = new()
		{
			{ "a + b" },
			{ "a - b" },
			{ "a * b" },
			{ "a / b" },
			{ "pow(a, b)" },
			{ "log(a, b)" },
			{ "max(a, b)" },
			{ "min(a, b)" },
			{ "round(a, b)" },

			{ "abs(a)" },
			{ "sign(a)" },

			{ "cos(a)" },
			{ "sin(a)" },
			{ "tan(a)" },

			{ "acos(a)" },
			{ "asin(a)" },
			{ "atan(a)" },

			{ "floor(a)" },
			{ "ceiling(a)" },
			{ "truncate(a)" },

			{ "exp(a)" },
			{ "sqrt(a)" },
		};

		protected override IEnumerable<string> GetValues() => _functions;
	}

	private readonly AssemblyLoadContextTracker _formulaCtx = new();
	private readonly AssemblyLoadContextTracker _validatorCtx = new();

	private readonly DiagramSocket _outputSocket;

	private ExpressionFormula<decimal> _formula;
	private ExpressionFormula<bool> _validator;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "F0EDCBDF-41CB-442B-896C-332DAC3CAAE3".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Function";

	private readonly DiagramElementParam<string> _expression;

	/// <summary>
	/// Expression.
	/// </summary>
	public string Expression
	{
		get => _expression.Value;
		set => _expression.Value = value;
	}

	private readonly DiagramElementParam<string> _validation;

	/// <summary>
	/// Validation.
	/// </summary>
	public string Validation
	{
		get => _validation.Value;
		set => _validation.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MathDiagramElement"/>.
	/// </summary>
	public MathDiagramElement()
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Result, DiagramSocketType.Any);

		_expression = AddParam<string>(nameof(Expression))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Formula, LocalizedStrings.Formula, LocalizedStrings.MathFormulaDesc, 10)
			.SetEditor(new ItemsSourceAttribute(typeof(FormulaSource)) { IsEditable = true })
			//.SetOnValueChangingHandler((oldValue, newValue) =>
			//{
			//	var formula = ExpressionFormula.Compile(newValue, false);

			//	if (!formula.Error.IsEmpty())
			//		throw new InvalidOperationException(formula.Error);
			//})
			.SetOnValueChangedHandler(value =>
			{
				SetElementName(value);

				_formula = null;

				if (value.IsEmpty())
				{
					foreach (var socket in InputSockets.ToArray())
					{
						socket.Connected -= OnInputConnected;
						RemoveSocket(socket);
					}

					return;
				}

				_formula = value.Compile(_formulaCtx);

				if (!_formula.Error.IsEmpty())
					return;

				var actualSocketIds = new List<string>();

				foreach (var v in _formula.Variables)
				{
					var socketId = GenerateSocketId(v);
					actualSocketIds.Add(socketId);

					if (InputSockets.FirstOrDefault(s => s.Id == socketId) != null)
						continue;

					var socket = AddInput(socketId, v, DiagramSocketType.Any);

					socket.AllowConvertToNumeric();
					socket.AvailableTypes.Add(DiagramSocketType.Date);
					socket.AvailableTypes.Add(DiagramSocketType.Time);

					socket.Connected += OnInputConnected;
				}

				InputSockets.Where(s => !actualSocketIds.Contains(s.Id)).ToArray().ForEach(RemoveSocket);
			});

		_validation = AddParam<string>(nameof(Validation))
			.SetDisplay(LocalizedStrings.Formula, LocalizedStrings.Validation, LocalizedStrings.ValidationInputValues, 11)
			.SetOnValueChangedHandler(value =>
			{
				_validator = null;

				if (value.IsEmpty())
					return;

				_validator = value.Compile<bool>(_validatorCtx);

				if (!_validator.Error.IsEmpty())
					return;
			});
	}

	private void OnInputConnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.Type == source.Type)
			return;

		socket.Type = source.Type;

		DiagramSocketType newOutputType;

		if (InputSockets.Any(s => s.Type == DiagramSocketType.Date))
			newOutputType = DiagramSocketType.Date;
		else if (InputSockets.Any(s => s.Type == DiagramSocketType.Time))
			newOutputType = DiagramSocketType.Time;
		else
			newOutputType = DiagramSocketType.Unit;

		if (_outputSocket.Type != newOutputType)
			_outputSocket.Type = newOutputType;
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (_formula is null)
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Formula));
		else if (!_formula.Error.IsEmpty())
			throw new InvalidOperationException(_formula.Error);

		if (_validator?.Error.IsEmpty() == false)
			throw new InvalidOperationException(_validator.Error);

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		var valuesByName = values.ToDictionary(p => p.Key.Name, p => p.Value.GetValue<decimal>());

		if (_validator is not null)
		{
			var validatorValues = _validator
				.Variables
				.Select(v =>
				{
					if (valuesByName.TryGetValue(v, out var value))
						return value;

					throw new InvalidOperationException(LocalizedStrings.ValueForWasNotPassed.Put(v));
				})
				.ToArray();

			if (!_validator.Calculate(validatorValues))
				return;
		}

		var inputValues = _formula
			.Variables
			.Select(v =>
			{
				if (valuesByName.TryGetValue(v, out var value))
					return value;

				throw new InvalidOperationException(LocalizedStrings.ValueForWasNotPassed.Put(v));
			})
			.ToArray();

		var result = _formula.Calculate(inputValues);

		if (_outputSocket.Type == DiagramSocketType.Date)
		{
			var inputValue = values.Values.Select(v => v.Value).OfType<DateTimeOffset>().FirstOrDefault();
			var offset = inputValue == default ? TimeSpan.Zero : inputValue.Offset;

			RaiseProcessOutput(_outputSocket, time, new DateTimeOffset(result.To<long>(), TimeSpan.Zero).ToOffset(offset), source);
		}
		else if (_outputSocket.Type == DiagramSocketType.Time)
			RaiseProcessOutput(_outputSocket, time, new TimeSpan(result.To<long>()), source);
		else
			RaiseProcessOutput(_outputSocket, time, result, source);
	}
}