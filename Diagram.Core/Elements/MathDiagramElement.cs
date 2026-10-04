namespace StockSharp.Diagram.Elements;

using Ecng.Compilation;
using Ecng.Compilation.Expressions;

using StockSharp.Configuration;

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

				// The inputs are named by the text itself. The formula is compiled when the element is
				// prepared, and a text that cannot be read is reported there; until then it leaves the
				// inputs as they are.
				string[] variables;

				try
				{
					variables = ExpressionHelper.GetVariables(value);
				}
				catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
				{
					return;
				}

				var actualSocketIds = new List<string>();

				foreach (var v in variables)
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

				// A wired input stays until the text is known to compile: an unfinished formula names fewer
				// inputs than the finished one, and removing an input removes its link with it.
				InputSockets.Where(s => !actualSocketIds.Contains(s.Id) && !s.IsConnected).ToArray().ForEach(RemoveSocket);
			});

		_validation = AddParam<string>(nameof(Validation))
			.SetDisplay(LocalizedStrings.Formula, LocalizedStrings.Validation, LocalizedStrings.ValidationInputValues, 11)
			.SetOnValueChangedHandler(_ => _validator = null);
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
	protected override async ValueTask OnPrepareAsync(CancellationToken cancellationToken)
	{
		if (Expression.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Formula));

		// The text can be replaced while it is being compiled; what is kept is the formula of the text
		// that is there once the compiler is done.
		while (_formula is null)
		{
			var expression = Expression;
			var formula = await expression.CompileAsync(Paths.FileSystem, _formulaCtx, cancellationToken);

			if (!formula.Error.IsEmpty())
				throw new InvalidOperationException(formula.Error);

			if (expression == Expression)
				_formula = formula;
		}

		var variables = _formula.Variables.Select(GenerateSocketId).ToHashSet();
		InputSockets.Where(s => !variables.Contains(s.Id)).ToArray().ForEach(RemoveSocket);

		while (_validator is null && !Validation.IsEmpty())
		{
			var validation = Validation;
			var validator = await validation.CompileAsync<bool>(Paths.FileSystem, _validatorCtx, cancellationToken);

			if (!validator.Error.IsEmpty())
				throw new InvalidOperationException(validator.Error);

			if (validation == Validation)
				_validator = validator;
		}

		await base.OnPrepareAsync(cancellationToken);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_formula = null;
		_validator = null;

		// The compiled formulas live in assemblies of their own, which stay loaded until unloaded.
		_formulaCtx.Dispose();
		_validatorCtx.Dispose();

		base.DisposeManaged();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_formula = null;
		_validator = null;

		try
		{
			_formulaCtx.Unload();
		}
		catch { }

		try
		{
			_validatorCtx.Unload();
		}
		catch { }
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTime time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		// A text replaced since the element was prepared has no compiled form until it is prepared again.
		var formula = _formula ?? throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Formula));
		var validator = _validator;

		if (validator is null && !Validation.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Validation));

		var valuesByName = values.ToDictionary(p => p.Key.Name, p => p.Value.GetValue<decimal>());

		if (validator is not null)
		{
			var validatorValues = validator
				.Variables
				.Select(v =>
				{
					if (valuesByName.TryGetValue(v, out var value))
						return value;

					throw new InvalidOperationException(LocalizedStrings.ValueForWasNotPassed.Put(v));
				})
				.ToArray();

			if (!validator.Calculate(validatorValues))
				return;
		}

		var inputValues = formula
			.Variables
			.Select(v =>
			{
				if (valuesByName.TryGetValue(v, out var value))
					return value;

				throw new InvalidOperationException(LocalizedStrings.ValueForWasNotPassed.Put(v));
			})
			.ToArray();

		var result = formula.Calculate(inputValues);

		if (_outputSocket.Type == DiagramSocketType.Date)
			RaiseProcessOutput(_outputSocket, time, new DateTime(result.To<long>()).UtcKind(), source);
		else if (_outputSocket.Type == DiagramSocketType.Time)
			RaiseProcessOutput(_outputSocket, time, new TimeSpan(result.To<long>()), source);
		else
			RaiseProcessOutput(_outputSocket, time, result, source);
	}
}