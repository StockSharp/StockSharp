namespace StockSharp.Diagram.Elements;

using System.Linq.Expressions;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

using Ecng.Compilation;

/// <summary>
/// The element which is using <see cref="DiagramExternalElement"/>.
/// </summary>
public abstract class BaseExternalDiagramElement : DiagramElement
{
	private class InputMethod(string id)
	{
		private readonly Dictionary<DiagramSocket, object> _values = [];

		public string Id { get; } = id.ThrowIfEmpty(nameof(id));
		public MethodInfo Method { get; set; }
		public ParameterInfo[] Parameters { get; set; }
		public DiagramExternalElement Target { get; set; }
		public List<DiagramSocket> Sockets { get; } = [];

		public void Process(DiagramSocketValue value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (Target is null)
				throw new InvalidOperationException("Target is null.");

			_values[value.Socket] = value.Value;

			if (Target.WaitAllInput && _values.Count != Sockets.Count)
				return;

			var paramValues = Sockets.Select(_values.TryGetValue).ToArray();

			_values.Clear();

			if (paramValues.Length != Parameters.Length)
				throw new InvalidOperationException($"paramValues={paramValues.Length} != parameters={Parameters.Length}");

			for (var i = 0; i < paramValues.Length; i++)
			{
				var param = Parameters[i];
				var paramVal = paramValues[i];

				var paramName = param.Name;
				var paramType = param.ParameterType;

				if (paramVal is null)
				{
					if (paramType.IsValueType && !paramType.IsNullable())
						throw new InvalidOperationException($"Parameter {paramName} has type '{paramType}' and must has value.");
				}
				else
				{
					if (!paramVal.GetType().Is(paramType))
						paramValues[i] = paramVal.ConvertValue(paramType, true);
				}
			}

			Method.Invoke(Target, paramValues);
		}
	}

	private class ResolveSocketParameterVisitor(DiagramSocket socket) : ExpressionVisitor
	{
		private readonly DiagramSocket _socket = socket;

		protected override Expression VisitParameter(ParameterExpression node)
		{
			return node.Type == typeof(DiagramSocket) ? Expression.Constant(_socket) : base.VisitParameter(node);
		}
	}

	private const string _invokeMethodName = nameof(Action.Invoke);

	private readonly List<InputMethod> _inputMethods = [];
	private readonly Dictionary<DiagramSocket, (EventInfo evt, Delegate handler)> _outputEvents = [];

	private DiagramExternalElement _element;

	private readonly HashSet<IDiagramElementParam> _baseParams = [];

	/// <summary>
	/// Get <see cref="DiagramExternalElement"/> instance.
	/// </summary>
	/// <returns><see cref="DiagramExternalElement"/></returns>
	protected abstract DiagramExternalElement Get();

	/// <inheritdoc />
	public override bool IsExternalCode => true;

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseExternalDiagramElement"/>.
	/// </summary>
	protected BaseExternalDiagramElement()
	{
		_baseParams.AddRange(Parameters);

		if (Scope<CompositionLoadingContext>.Current?.Value.AllowCode == false)
			throw new InvalidOperationException($"{GetType()} element cannot be created.");
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		_element = Get();

		_element.Container = this;

		foreach (var method in _inputMethods)
		{
			method.Target = _element;
		}

		foreach (var (_, (evt, handler)) in _outputEvents)
		{
			evt.AddEventHandler(_element, handler);
		}

		var parameters = _element.Parameters.ToDictionary(p => p.Name);

		foreach (var parameter in Parameters.Except(_baseParams))
		{
			if (parameters.TryGetValue(parameter.Name, out var elemParam))
				elemParam.Value = parameter.Value;
		}

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		if (_element is null)
			throw new InvalidOperationException("_element is null");

		_element.Start();

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		base.OnStop();

		if (_element is not DiagramExternalElement elem)
			return;

		elem.Stop();

		foreach (var (_, (evt, handler)) in _outputEvents)
		{
			evt.RemoveEventHandler(elem, handler);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_element?.Reset();
		_element = null;
	}

	/// <summary>
	/// Update sockets.
	/// </summary>
	/// <param name="type">Element type.</param>
	/// <param name="parameters"><see cref="DiagramExternalElement.Parameters"/></param>
	protected void UpdateSockets(Type type, IEnumerable<IDiagramElementParam> parameters)
	{
		if (type is null)			throw new ArgumentNullException(nameof(type));
		if (parameters is null)		throw new ArgumentNullException(nameof(parameters));

		UpdateInputSockets(type);
		UpdateOutputSockets(type);

		UpdateParameters(parameters);
	}

	private void UpdateParameters(IEnumerable<IDiagramElementParam> parameters)
	{
		var newParams = parameters.ToDictionary(p => p.Name);

		var toRemove = Parameters
			.Where(p => !_baseParams.Contains(p))
			.ToDictionary(p => p.Name);

		foreach (var (id, newParam) in newParams)
		{
			if (toRemove.TryGetAndRemove(id, out var prevParam))
			{
				RemoveParam(prevParam);
				AddParam(newParam);

				if (prevParam.Type == newParam.Type)
					newParam.Value = prevParam.Value;
			}
			else
				AddParam(newParam);
		}

		foreach (var p in toRemove.Values)
			RemoveParam(p);
	}

	private void RaiseProcessOutput(DiagramSocket outputSocket, object value)
	{
		RaiseProcessOutput(outputSocket, Strategy.CurrentTime, value);
	}

	private const BindingFlags _flags = BindingFlags.Instance | BindingFlags.Public;

	private void UpdateInputSockets(Type type)
	{
		var methods = type
			.GetMethods(_flags)
			.Where(m => m.GetAttribute<DiagramExternalAttribute>() != null)
			.Where(m => !m.IsGenericMethodDefinition);

		var inputMethods = _inputMethods.CopyAndClear();

		foreach (var method in methods)
		{
			var parameters = method.GetParameters();
			var methodId = $"{method.Name}({parameters.Select(p => p.ParameterType.Name).JoinCommaSpace()})";

			var inputMethod = inputMethods.FirstOrDefault(i => i.Id == methodId) ?? new(methodId);
			inputMethod.Method = method;
			inputMethod.Parameters = parameters;

			var sockets = inputMethod.Sockets.CopyAndClear();

			foreach (var parameter in parameters)
			{
				var socketType = DiagramSocketType.GetSocketType(parameter.ParameterType);

				var uniqueKey = $"{methodId} - {parameter.Name}";
				var name = $"{method.Name} - {parameter.Name}";

				var (socket, _) = GetOrAddSocket(GenerateSocketId(uniqueKey), DiagramSocketDirection.In, name, socketType, inputMethod.Process, 1, int.MaxValue, true, true);

				RaiseSocketChanged(socket);

				inputMethod.Sockets.Add(socket);
			}

			foreach (var socket in sockets.Except(inputMethod.Sockets))
				RemoveSocket(socket);

			_inputMethods.Add(inputMethod);
		}

		foreach (var socket in inputMethods.Except(_inputMethods).SelectMany(info => info.Sockets))
			RemoveSocket(socket);
	}

	private void UpdateOutputSockets(Type type)
	{
		var eventInfos = type.GetEvents(_flags);
		var outputEvents = _outputEvents.CopyAndClear();

		foreach (var eventInfo in eventInfos.Where(e => e.GetAttribute<DiagramExternalAttribute>() is not null))
		{
			var handlerType = eventInfo.EventHandlerType;
			var methodInfo = handlerType.GetMethod(_invokeMethodName);

			if (methodInfo.ReturnType != typeof(void))
				continue;

			var parameters = methodInfo.GetParameters();

			var isFSharpHandler = handlerType.IsFSharpHandler();

			ParameterInfo handlerParameter;
			if (isFSharpHandler)
			{
				if (parameters.Length != 2)
					continue;

				handlerParameter = parameters[1];
			}
			else
			{
				if (parameters.Length != 1)
					continue;

				handlerParameter = parameters[0];
			}

			if (handlerParameter.IsOut)
				continue;

			var socketType = DiagramSocketType.GetSocketType(handlerParameter.ParameterType);
			var (socket, _) = GetOrAddSocket(GenerateSocketId(eventInfo.Name), DiagramSocketDirection.Out,
				eventInfo.Name, socketType, null, int.MaxValue, int.MaxValue, true, true);

			var action = (Action<DiagramSocket, object>)RaiseProcessOutput;
			var actType = action.GetType();

			static ParameterExpression createParam(Type type, string name)
				=> Expression.Parameter(type, name);

			var socketParameter = createParam(typeof(DiagramSocket), "socket");

			var paramsExpressions = new List<ParameterExpression>();
			if (isFSharpHandler)
			{
				paramsExpressions.Add(createParam(parameters[0].ParameterType, parameters[0].Name));
				paramsExpressions.Add(createParam(handlerParameter.ParameterType, handlerParameter.Name));
			}
			else
			{
				paramsExpressions.Add(createParam(handlerParameter.ParameterType, handlerParameter.Name));
			}

			var convertedParameter = Expression.Convert(paramsExpressions.Last(), typeof(object));
			var callExpression = Expression.Call(
				Expression.Constant(action),
				actType.GetMethod(_invokeMethodName),
				socketParameter,
				convertedParameter
			);

			var closureExpression = new ResolveSocketParameterVisitor(socket).Visit(callExpression);
			var handler = Expression.Lambda(handlerType, closureExpression, paramsExpressions).Compile();

			RaiseSocketChanged(socket);

			_outputEvents.Add(socket, (eventInfo, handler));
		}

		foreach (var socket in outputEvents.Select(p => p.Key).Except([.. _outputEvents.Keys]))
			RemoveSocket(socket);
	}

	private void RemoveAllSockets()
	{
		_inputMethods.Clear();
		_outputEvents.Clear();

		foreach (var socket in InputSockets.ToArray())
			RemoveSocket(socket);

		foreach (var socket in OutputSockets.ToArray())
		{
			RemoveSocket(socket);
		}
	}
}
