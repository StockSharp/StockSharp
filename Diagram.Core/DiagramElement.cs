namespace StockSharp.Diagram;

using System.Threading;
using System.Runtime.CompilerServices;

using Ecng.Configuration;

/// <summary>
/// The diagram element.
/// </summary>
public abstract class DiagramElement : BaseLogReceiver, INotifyPropertyChanging, INotifyPropertyChanged, ICustomTypeDescriptor, INotifyPropertiesChanged, IPersistable
{
	private readonly ObservableCollection<DiagramSocket> _inputSockets = [];
	private readonly ObservableCollection<DiagramSocket> _outputSockets = [];
	private readonly List<(DiagramSocket from, DiagramSocket to)> _activeConnections = [];
	private readonly Dictionary<DiagramSocket, DiagramSocketValue> _socketValues = [];
	private readonly HashSet<IDiagramElementParam> _parameters = [];
	private readonly HashSet<DiagramElement> _subscribedElements = [];

	private readonly DiagramElementParam<bool> _showParameters;
	private readonly DiagramElementParam<bool> _showSockets;
	private readonly DiagramElementParam<bool> _processNullValues;

	private int _emptyHandlerSocketCount;

	private bool _suppressSocketEvents;

	/// <summary>
	/// Generate socket identifier.
	/// </summary>
	/// <param name="suffix">Suffix.</param>
	/// <returns>Identifier.</returns>
	public static string GenerateSocketId(string suffix) => "dynsock_" + suffix;

	/// <summary>
	/// The unique identifier of the diagram element type.
	/// </summary>
	public abstract Guid TypeId { get; }

	/// <summary>
	/// Is the element contains external code.
	/// </summary>
	public virtual bool IsExternalCode { get; } = false;

	/// <summary>
	/// Incoming connections.
	/// </summary>
	[Browsable(false)]
	public IReadOnlyCollection<DiagramSocket> InputSockets => _inputSockets;

	/// <summary>
	/// Outgoing connections.
	/// </summary>
	[Browsable(false)]
	public IReadOnlyCollection<DiagramSocket> OutputSockets => _outputSockets;

	/// <summary>
	/// Parent composition this element belongs to.
	/// </summary>
	public CompositionDiagramElement ParentComposition { get; private set; }

	/// <summary>
	/// Whether undo/redo operation is in progress.
	/// </summary>
	public virtual bool IsUndoRedoing => ParentComposition?.IsUndoRedoing == true;

	/// <summary>
	/// Check if undo manager is defined
	/// </summary>
	public virtual bool HasUndoManager => ParentComposition?.HasUndoManager == true;

	/// <summary>
	/// Help url.
	/// </summary>
	public virtual string DocUrl
	{
		get => GetType().GetDocUrl();
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Diagram element settings.
	/// </summary>
	[Browsable(false)]
	public virtual IEnumerable<IDiagramElementParam> Parameters => _parameters;

	/// <summary>
	/// The name of the group which includes a diagram element.
	/// </summary>
	public virtual string Category { get; private set; }

	/// <summary>
	/// Use auto naming.
	/// </summary>
	public bool CanAutoName { get; set; } = true;

	private readonly DiagramElementParam<string> _elementName;

	private string ElementName
	{
		get => _elementName.Value;
		set => _elementName.Value = value;
	}

	private readonly DiagramElementParam<string> _name;

	/// <inheritdoc />
	public override string Name
	{
		get => _name.Value;
		set => _name.Value = value;
	}

	/// <summary>
	/// The diagram element description.
	/// </summary>
	public virtual string Description { get; private set; }

	private readonly DiagramElementParam<LogLevels> _logLevel;

	/// <inheritdoc />
	public override LogLevels LogLevel
	{
		get => _logLevel.Value;
		set => _logLevel.Value = value;
	}

	/// <summary>
	/// Show element parameters in higher order elements.
	/// </summary>
	public bool ShowParameters
	{
		get => _showParameters.Value;
		set => _showParameters.Value = value;
	}

	/// <summary>
	/// Show element sockets in higher order elements.
	/// </summary>
	public bool ShowSockets
	{
		get => _showSockets.Value;
		set => _showSockets.Value = value;
	}

	/// <summary>
	/// Process null values.
	/// </summary>
	public bool ProcessNullValues
	{
		get => _processNullValues.Value;
		set => _processNullValues.Value = value;
	}

	/// <summary>
	/// Icon resource name.
	/// </summary>
	[Browsable(false)]
	public abstract string IconName { get; }

	/// <summary>
	/// The strategy to which the element is attached.
	/// </summary>
	public virtual DiagramStrategy Strategy
	{
		get => ParentComposition?.Strategy;
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Connector.
	/// </summary>
	protected IConnector Connector => Strategy.SafeGetConnector();

	private DebuggerSyncObject _debuggerSyncObject;

	/// <summary>
	/// The synchronization object for the debugger.
	/// </summary>
	public DebuggerSyncObject DebuggerSyncObject
	{
		get => _debuggerSyncObject;
		set
		{
			_debuggerSyncObject = value;
			RaisePropertyChanged(nameof(DebuggerSyncObject));
		}
	}

	/// <summary>
	/// New data occurring event.
	/// </summary>
	public event Action<DiagramSocketValue> ProcessOutput;

	///// <summary>
	///// The diagram element name change event.
	///// </summary>
	//public event Action<string> NameChanging;

	/// <summary>
	/// The diagram element connection added event.
	/// </summary>
	public event Action<DiagramSocket> SocketAdded;

	/// <summary>
	/// The diagram element connection removed event.
	/// </summary>
	public event Action<DiagramSocket> SocketRemoved;

	/// <summary>
	/// The diagram element connection changed event.
	/// </summary>
	public event Action<DiagramSocket> SocketChanged;

	/// <summary>
	/// Started undoable operation.
	/// </summary>
	public event Action StartedUndoableOperation;

	/// <summary>
	/// Committed undoable operation.
	/// </summary>
	public event Action<DiagramElement, IUndoableEdit> CommittedUndoableOperation;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramElement"/>.
	/// </summary>
	protected DiagramElement()
	{
		var type = GetType();

		var defaultName = type.GetDisplayName();
		Category = type.GetCategory(LocalizedStrings.Common);
		Description = type.GetDescription(defaultName);

		_name = AddParam(nameof(Name), defaultName)
			.SetNonBrowsable()
			.SetOnValueChangedHandler(SetElementName);

		_elementName = AddParam(nameof(ElementName), defaultName)
			.SetBasic()
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.Name, LocalizedStrings.DiagramElemName, 0)
			.SetEditor(new EditorAttribute(typeof(INameEditor), typeof(INameEditor)))
			.SetOnValueChangedHandler(v =>
			{
				_elementName.IgnoreOnSave = true;

				if (CanAutoName)
				{
					if (v == INameEditorConstants.ResetName)
					{
						ElementName = defaultName;
						CanAutoName = true;
					}

					if (Name == ElementName)
						return;

					Name = ElementName;
					CanAutoName = false;
				}
				else
				{
					if (v == INameEditorConstants.ResetName)
					{
						Name = string.Empty;
						ElementName = string.Empty;
					}
					else
						Name = ElementName;
				}
			});

		_logLevel = AddParam(nameof(LogLevel), LogLevels.Inherit)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.LogLevel, LocalizedStrings.DiagramElemLogLevel, 1);

		_showParameters = AddParam(nameof(ShowParameters), false)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.Parameters, LocalizedStrings.DiagramElemShowParams, 2)
			.SetOnValueChangedHandler(v => RaisePropertiesChanged());

		_showSockets = AddParam(nameof(ShowSockets), false)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.Sockets, LocalizedStrings.ShowSockets, 3);

		_processNullValues = AddParam(nameof(ProcessNullValues), false)
			.SetDisplay(LocalizedStrings.Common, LocalizedStrings.ProcessNullValues, LocalizedStrings.ProcessNullValues, 10);

		PropertyChanging += OnPropertyChanging;
		PropertyChanged += OnPropertyChanged;

		LocalizedStrings.ActiveLanguageChanged += OnActiveLanguageChanged;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		PropertyChanging -= OnPropertyChanging;
		PropertyChanged -= OnPropertyChanged;

		LocalizedStrings.ActiveLanguageChanged -= OnActiveLanguageChanged;
	}

	private void OnActiveLanguageChanged()
	{
		Description = GetDescription();
		Category = GetCategory();

		RaisePropertyChanged(nameof(Description));
		RaisePropertyChanged(nameof(Category));
	}

	/// <summary>
	/// Get display name.
	/// </summary>
	/// <returns>Name.</returns>
	public virtual string GetDisplayName() => GetType().GetDisplayName();

	/// <summary>
	/// Get description.
	/// </summary>
	/// <returns>Description.</returns>
	public virtual string GetDescription() => GetType().GetDescription().IsEmpty(GetDisplayName());

	/// <summary>
	/// Get category.
	/// </summary>
	/// <returns>Category.</returns>
	public virtual string GetCategory() => GetType().GetCategory(LocalizedStrings.Common);

	/// <summary>
	/// Set element name.
	/// </summary>
	/// <param name="name">Name.</param>
	protected void SetElementName(string name)
	{
		if (!CanAutoName)
			return;

		ElementName = name ?? GetType().GetDisplayName();
		CanAutoName = true;
	}

	/// <summary>
	/// To add a parameter.
	/// </summary>
	/// <typeparam name="T">Parameter type.</typeparam>
	/// <param name="name">Name.</param>
	/// <param name="value">Value.</param>
	/// <returns>Parameter.</returns>
	protected DiagramElementParam<T> AddParam<T>(string name, T value = default)
	{
		var param = new DiagramElementParam<T>
		{
			Name = name,
			Value = value,
		};

		AddParam(param);

		return param;
	}

	/// <summary>
	/// To add a parameter.
	/// </summary>
	/// <param name="param">Parameter.</param>
	protected void AddParam(IDiagramElementParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (_parameters.Any(p => p.Name == param.Name))
			throw new ArgumentException($"Parameter '{param.Name}' already exist.");

		param.PropertyChanging += ParameterPropertyChanging;
		param.PropertyChanged += ParameterPropertyChanged;

		_parameters.Add(param);
	}

	/// <summary>
	/// To remove a parameter.
	/// </summary>
	/// <param name="param">Parameter.</param>
	protected void RemoveParam(IDiagramElementParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		param.PropertyChanging -= ParameterPropertyChanging;
		param.PropertyChanged -= ParameterPropertyChanged;

		_parameters.Remove(param);
	}

	private void ParameterPropertyChanging(object sender, PropertyChangingEventArgs args)
	{
		if (args.PropertyName != "Value")
			return;

		RaisePropertyChanging(((IDiagramElementParam)sender).Name);
	}

	private void ParameterPropertyChanged(object sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName != "Value")
			return;

		var parameter = (IDiagramElementParam)sender;

		RaisePropertyChanged(parameter.Name);
		RaisePropertyChanged(parameter.Name + parameter.GetHashCode()); // fot upd in propgrid
		RaiseParameterValueChanged(parameter.Name);
	}

	#region Sockets

	private ObservableCollection<DiagramSocket> GetSockets(DiagramSocketDirection dir)
		=> dir == DiagramSocketDirection.In ? _inputSockets : _outputSockets;

	/// <summary>
	/// Create new socket instance.
	/// </summary>
	protected virtual DiagramSocket CreateSocketInstance(DiagramSocketDirection dir, string socketId)
		=> new(dir, socketId);

	/// <summary>
	/// To add or get an outgoing connection.
	/// </summary>
	/// <param name="socketId">The connection identifier.</param>
	/// <param name="dir"><see cref="DiagramSocketDirection"/></param>
	/// <param name="name">The connection name.</param>
	/// <param name="type">Connection type.</param>
	/// <param name="process">The action is called at the processing of the new incoming value for socket.</param>
	/// <param name="linkableMax">The maximum number of connections.</param>
	/// <param name="index">Index in sockets list.</param>
	/// <param name="isDynamic">Dynamic sockets are removed during <see cref="Load"/>.</param>
	/// <param name="allowGet">Return existing socket if it's already exist.</param>
	/// <returns>Connection.</returns>
	protected (DiagramSocket socket, bool isNew) GetOrAddSocket(string socketId, DiagramSocketDirection dir, string name, DiagramSocketType type, Action<DiagramSocketValue> process, int linkableMax, int index, bool isDynamic, bool allowGet)
	{
		if (socketId.IsEmptyOrWhiteSpace())
			throw new ArgumentNullException(nameof(socketId));

		var sockets = GetSockets(dir);
		var socket = sockets.FindById(socketId);
		var isNew = false;

		if (socket == null)
		{
			socket = CreateSocketInstance(dir, socketId);
			isNew = true;
		}
		else if (!allowGet)
			throw new InvalidOperationException($"Socket with Id '{socketId}' already exists.");

		socket.Name = name;
		socket.LinkableMaximum = linkableMax;
		socket.Type = type;
		socket.Action = process;
		socket.IsDynamic = isDynamic;

		if (dir == DiagramSocketDirection.In && type == DiagramSocketType.Unit)
		{
			socket.AllowConvertToNumeric();
			socket.AvailableTypes.Remove(DiagramSocketType.Unit);
		}

		if (isNew)
		{
			if (sockets.FindById(socket.Id) != null)
				throw new InvalidOperationException($"Socket with Id '{socket.Id}' already exists.");

			socket.Parent = this;
			socket.Connected += OnSocketConnected;
			socket.Disconnected += OnSocketDisconnected;

			using var _ = SaveUndoState();

			sockets.Insert(index.Min(sockets.Count), socket);

			if (!_suppressSocketEvents)
				SocketAdded?.Invoke(socket);
		}

		return (socket, isNew);
	}

	/// <summary>
	/// To add or get existing incoming connection. isDynamic is false by default.
	/// </summary>
	/// <param name="id">The connection identifier.</param>
	/// <param name="name">The connection name.</param>
	/// <param name="type">Connection type.</param>
	/// <param name="linkableMax">The maximum number of connections.</param>
	/// <param name="process">The action is called at the processing of the new incoming value for socket.</param>
	/// <param name="index">Index in sockets list.</param>
	/// <param name="isDynamic">Socket will be saved with the element. Default is true for sockets with explicit id.</param>
	/// <returns>Connection.</returns>
	protected DiagramSocket AddInput(StaticSocketIds id, string name, DiagramSocketType type, Action<DiagramSocketValue> process = null, int linkableMax = 1, int index = int.MaxValue, bool? isDynamic = null)
		=> GetOrAddSocket(id.ToString(), DiagramSocketDirection.In, name, type, process, linkableMax, index, isDynamic ?? false, false).socket;

	/// <summary>
	/// To add or get existing incoming connection. isDynamic is true by default.
	/// </summary>
	/// <param name="id">The connection identifier.</param>
	/// <param name="name">The connection name.</param>
	/// <param name="type">Connection type.</param>
	/// <param name="linkableMax">The maximum number of connections.</param>
	/// <param name="process">The action is called at the processing of the new incoming value for socket.</param>
	/// <param name="index">Index in sockets list.</param>
	/// <param name="isDynamic">Socket will be saved with the element. Default is true for sockets with explicit id.</param>
	/// <returns>Connection.</returns>
	protected DiagramSocket AddInput(string id, string name, DiagramSocketType type, Action<DiagramSocketValue> process = null, int linkableMax = 1, int index = int.MaxValue, bool? isDynamic = null)
		=> GetOrAddSocket(id, DiagramSocketDirection.In, name, type, process, linkableMax, index, isDynamic ?? true, false).socket;

	/// <summary>
	/// To add or get an outgoing connection.
	/// </summary>
	/// <param name="id">The connection identifier.</param>
	/// <param name="name">The connection name.</param>
	/// <param name="type">Connection type.</param>
	/// <param name="linkableMax">The maximum number of connections.</param>
	/// <param name="index">Index in sockets list.</param>
	/// <param name="isDynamic">Dynamic sockets are removed during Load().</param>
	/// <returns>Connection.</returns>
	protected DiagramSocket AddOutput(StaticSocketIds id, string name, DiagramSocketType type, int linkableMax = int.MaxValue, int index = int.MaxValue, bool isDynamic = false)
		=> GetOrAddSocket(id.ToString(), DiagramSocketDirection.Out, name, type, null, linkableMax, index, isDynamic, false).socket;

	/// <summary>
	/// To add or get an outgoing connection.
	/// </summary>
	/// <param name="id">The connection identifier.</param>
	/// <param name="name">The connection name.</param>
	/// <param name="type">Connection type.</param>
	/// <param name="linkableMax">The maximum number of connections.</param>
	/// <param name="index">Index in sockets list.</param>
	/// <param name="isDynamic">Dynamic sockets are removed during Load().</param>
	/// <returns>Connection.</returns>
	protected DiagramSocket AddOutput(string id, string name, DiagramSocketType type, int linkableMax = int.MaxValue, int index = int.MaxValue, bool isDynamic = true)
		=> GetOrAddSocket(id, DiagramSocketDirection.Out, name, type, null, linkableMax, index, isDynamic, false).socket;

	/// <summary>
	/// To remove a connection.
	/// </summary>
	/// <param name="socket">Connection.</param>
	protected void RemoveSocket(DiagramSocket socket) => RemoveSocket(socket, true);

	private void RemoveSocket(DiagramSocket socket, bool raiseSocketRemoved)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		using var _ = SaveUndoState();

		if(!GetSockets(socket.Directon).Remove(socket))
			return;

		socket.Connected -= OnSocketConnected;
		socket.Disconnected -= OnSocketDisconnected;

		var socket2 = socket;

		_activeConnections
			.Where(c => c.to == socket2)
			.ToArray()
			.ForEach(c => OnSocketDisconnected(socket2, c.from));

		if(raiseSocketRemoved && !_suppressSocketEvents)
			SocketRemoved?.Invoke(socket);

		socket.Dispose();
		socket.Parent = null;
	}

	/// <summary>
	/// To remove all incoming and outgoing connections.
	/// </summary>
	/// <param name="raiseSocketRemoved">Raise <see cref="SocketRemoved"/> event.</param>
	protected virtual void RemoveSockets(bool raiseSocketRemoved = true) => RemoveSockets(_ => true, raiseSocketRemoved);

	/// <summary>
	/// To remove multiple sockets.
	/// </summary>
	/// <param name="predicate"></param>
	/// <param name="raiseSocketRemoved">Raise <see cref="SocketRemoved"/> event.</param>
	protected void RemoveSockets(Func<DiagramSocket, bool> predicate, bool raiseSocketRemoved = true)
	{
		var sockets = _inputSockets.Concat(_outputSockets).Where(s => s.IsDynamic && predicate(s)).ToArray();

		using var _ = SaveUndoState();

		sockets.ForEach(s => RemoveSocket(s, raiseSocketRemoved));
	}

	private void CalcEmptyHandlerSocketCount()
		=> _emptyHandlerSocketCount = _activeConnections.Select(c => c.to).Where(c => c.Action is null).Distinct().Count();

	/// <summary>
	/// The method is called at subscription to the processing of diagram element output values.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	protected virtual void OnSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (source == null)
			throw new ArgumentNullException(nameof(source));

		if (socket.IsOutput)
			return;

		var conn = (from: source, to: socket);

		var connection = _activeConnections.FirstOrDefault(c => c.from == conn.from && c.to == conn.to);
		if(connection != default)
			throw new InvalidOperationException(LocalizedStrings.ElemAlreadyBinded.Put(source, this, socket));

		_activeConnections.Add(conn);
		CalcEmptyHandlerSocketCount();

		UpdateSubscribedElements();
	}

	/// <summary>
	/// The method is called at unsubscription from the processing of diagram element output values.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	protected virtual void OnSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (socket.IsOutput)
			return;

		var connections = _activeConnections.Where(c => c.to == socket).ToArray();

		if(connections.Length == 0)
			return;

		(DiagramSocket from, DiagramSocket to) conn = default;

		if (connections.Length == 1)
			conn = connections[0];
		else if(source != null)
			conn = connections.SingleOrDefault(c => c.from == source);

		if (conn == default)
			return;

		_activeConnections.Remove(conn);
		CalcEmptyHandlerSocketCount();

		UpdateSubscribedElements();
	}

	private void UpdateSubscribedElements()
	{
		var need = _activeConnections.Select(c => c.from.Parent).Distinct().ToSet();

		var toUnsubscribe = new HashSet<DiagramElement>(_subscribedElements);
		toUnsubscribe.ExceptWith(need);

		var toSubscribe = new HashSet<DiagramElement>(need);
		toSubscribe.ExceptWith(_subscribedElements);

		toUnsubscribe.ForEach(e => e.ProcessOutput -= Process);
		toSubscribe.ForEach(e => e.ProcessOutput += Process);

		_subscribedElements.Clear();
		_subscribedElements.AddRange(need);
	}

	/// <summary>
	/// To call the event <see cref="SocketChanged"/>.
	/// </summary>
	/// <param name="socket">Socket.</param>
	protected void RaiseSocketChanged(DiagramSocket socket)
	{
		if (!_suppressSocketEvents)
			SocketChanged?.Invoke(socket);
	}

	#endregion

	#region Process

	/// <summary>
	/// To call the event <see cref="ProcessOutput"/>.
	/// </summary>
	/// <param name="socket">Output socket.</param>
	/// <param name="time">Time.</param>
	/// <param name="value">Value.</param>
	/// <param name="source">Source value.</param>
	/// <param name="subscription">Subscription.</param>
	protected void RaiseProcessOutput(DiagramSocket socket, DateTimeOffset time, object value, DiagramSocketValue source = null, Subscription subscription = null)
	{
		var dsv = new DiagramSocketValue(socket, time, value, source, subscription ?? source?.Subscription);

		LogDebug("'{0}' output value: '{1}'", socket, value);

		socket.Value = value;
		Strategy.ClearStateRequired(socket, true);

		DebuggerSyncObject?.TryWait(socket, false);
		ProcessOutput?.Invoke(dsv);
	}

	/// <summary>
	/// Disabled value for <see cref="FlushPriority"/>.
	/// </summary>
	protected const int FlushDisabled = -1;

	/// <summary>
	/// Normal value for <see cref="FlushPriority"/>.
	/// </summary>
	protected const int FlushNormal = 0;

	private int _flushPriority = FlushDisabled;

	/// <summary>
	/// Is need flush state (<see cref="FlushDisabled"/> means No).
	/// </summary>
	public int FlushPriority
	{
		get => _flushPriority;
		protected set
		{
			if (_flushPriority == value)
				return;

			_flushPriority = value;

			var composition = ParentComposition;

			if (composition is null)
				return;

			if (value == FlushDisabled)
				composition.RemoveFlushElement(this);
			else
				composition.AddFlushElement(this);
		}
	}

	/// <summary>
	/// Reset <see cref="FlushPriority"/>.
	/// </summary>
	protected void ResetFlushPriority()
		=> FlushPriority = FlushDisabled;

	/// <summary>
	/// Element processing level. How many times <see cref="Process"/> is reentered.
	/// </summary>
	protected int ProcessingLevel { get; private set; }

	/// <summary>
	/// Flush non trigger (root) elements.
	/// </summary>
	public virtual void Flush(DateTimeOffset time)
	{
	}

	private void CompositionOnStrategyChanged() => RaisePropertyChanged(nameof(Strategy));

	private bool _initialized;

	/// <summary>
	/// To initialize the element.
	/// </summary>
	/// <param name="parent">Parent composition or strategy.</param>
	public void Init(ILogSource parent)
	{
		if(parent == null)
			throw new ArgumentNullException(nameof(parent));

		if(ParentComposition == parent)
			return;

		if(_initialized)
			throw new InvalidOperationException("already initialized");

		_initialized = true;

		ParentComposition = parent as CompositionDiagramElement;
		Parent = parent;
		DebuggerSyncObject = ParentComposition?.DebuggerSyncObject;

		if(ParentComposition != null)
			ParentComposition.StrategyChanged += CompositionOnStrategyChanged;

		OnInit();
	}

	/// <summary>
	/// The deinitialization of the element.
	/// </summary>
	public void UnInit()
	{
		if(!_initialized)
			return;

		_initialized = false;

		OnUnInit();

		if(ParentComposition != null)
			ParentComposition.StrategyChanged -= CompositionOnStrategyChanged;

		Parent = null;
		ParentComposition = null;
		DebuggerSyncObject = null;
	}

	/// <summary>
	/// The method is called at initialization of the diagram element.
	/// </summary>
	protected virtual void OnInit() { }

	/// <summary>
	/// The method is called at deinitialization of the diagram element.
	/// </summary>
	protected virtual void OnUnInit() { }

	/// <summary>
	/// Wait all parameters before invoke method.
	/// </summary>
	protected virtual bool WaitAllInput => true;

	/// <summary>
	/// To handle the incoming value.
	/// </summary>
	/// <param name="value">Value.</param>
	public void Process(DiagramSocketValue value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		try
		{
			if (++ProcessingLevel > Strategy.OverflowLimit || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
			{
				Strategy.Stop(new InvalidOperationException($"!!!{LocalizedStrings.Overflow}!!!"));
				return;
			}

			var sockets = _activeConnections.Where(c => c.from == value.Socket).Select(c => c.to).ToArray();

			foreach (var socket in sockets)
			{
				LogDebug("'{0}' input from: '{1}' value: '{2}'", Name, value.Socket, value.Value);

				socket.Value = value.Value;
				Strategy.ClearStateRequired(socket, true);

				DebuggerSyncObject?.TryWait(socket, true);

				if (value.Value == null && !ProcessNullValues)
					return;

				var socketValue = new DiagramSocketValue(socket, value.Time, value.Value, value, value.Subscription);

				if (socket.Action != null)
				{
					socket.Action(socketValue);
				}
				else
				{
					_socketValues[socket] = socketValue;
					Strategy.ClearStateRequired(this, true);
				}
			}

			if (_socketValues.Count == 0 || (WaitAllInput && _socketValues.Count < _emptyHandlerSocketCount))
				return;

			OnProcess(value.Time, _socketValues, value);

			_socketValues.Clear();
			Strategy.ClearStateRequired(this, false);
		}
		catch (Exception excp)
		{
			SaveErrorInfo(excp);
			throw;
		}
		finally
		{
			--ProcessingLevel;
		}
	}

	private void SaveErrorInfo(Exception excp, bool waitDebugger = true)
	{
		if (excp is null)
			throw new ArgumentNullException(nameof(excp));

		if (waitDebugger)
			DebuggerSyncObject?.TryWaitOnError(this, excp);

		// в Runner дебаггер не установлен (решил и не устанавливать ради производительности)
		// поэтому сделал отдельные свойства
		if (ParentComposition is not null && ParentComposition.LastError is null)
		{
			ParentComposition.LastError = excp;
			ParentComposition.LastErrorElement = this;
		}
	}

	/// <summary>
	/// Get connection count.
	/// </summary>
	/// <param name="socket">Socket.</param>
	/// <returns>Count.</returns>
	public int GetNumConnections(DiagramSocket socket) => _activeConnections.Count(c => c.to == socket);

	/// <summary>
	/// Get connected source sockets.
	/// </summary>
	/// <param name="targetInputSocket"></param>
	public DiagramSocket[] GetConnectedSourceSockets(DiagramSocket targetInputSocket)
		=> [.. _activeConnections.Where(c => c.to == targetInputSocket).Select(c => c.from)];

	/// <summary>
	/// The method is called at the processing of the new incoming values.
	/// </summary>
	/// <param name="time">Time.</param>
	/// <param name="values">Values.</param>
	/// <param name="source">Source value.</param>
	protected virtual void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// To prepare for starting the diagram element algorithm.
	/// </summary>
	public void Prepare()
	{
		try
		{
			OnPrepare();
		}
		catch (Exception excp)
		{
			SaveErrorInfo(excp);
			throw;
		}
	}

	/// <summary>
	/// To prepare for starting the diagram element algorithm.
	/// </summary>
	protected virtual void OnPrepare()
	{
	}

	/// <summary>
	/// To start for start the diagram element algorithm.
	/// </summary>
	public void Start(DateTimeOffset time)
	{
		try
		{
			OnStart(time);
		}
		catch (Exception excp)
		{
			SaveErrorInfo(excp);
			throw;
		}
	}

	/// <summary>
	/// The method is called at the start of the diagram element algorithm.
	/// </summary>
	protected virtual void OnStart(DateTimeOffset time)
	{
	}

	/// <summary>
	/// To stop the diagram element algorithm.
	/// </summary>
	public void Stop()
	{
		try
		{
			OnStop();
		}
		catch (Exception excp)
		{
			SaveErrorInfo(excp, false);
			throw;
		}
	}

	/// <summary>
	/// The method is called at the stop of the diagram element algorithm.
	/// </summary>
	protected virtual void OnStop()
	{
	}

	/// <summary>
	/// To reinitialize the diagram element state.
	/// </summary>
	public void Reset()
	{
		try
		{
			OnReseted();
		}
		catch (Exception excp)
		{
			SaveErrorInfo(excp);
			throw;
		}
	}

	/// <summary>
	/// The method is called at re-initialisation of the diagram element state.
	/// </summary>
	protected virtual void OnReseted()
	{
		ResetFlushPriority();
	}

	/// <summary>
	/// Clear socket values.
	/// </summary>
	public virtual void ClearSocketValues()
	{
		if (_socketValues.Count <= 0)
			return;

		foreach (var pair in _socketValues)
		{
			pair.Key.Value = null;
			Strategy.ClearStateRequired(pair.Key, false);
		}

		_socketValues.Clear();
	}

	#endregion

	#region INotifyPropertyChanging

	/// <summary>
	/// The diagram element properties value changing event.
	/// </summary>
	public event PropertyChangingEventHandler PropertyChanging;

	/// <summary>
	/// To call the <see cref="PropertyChanging"/> event.
	/// </summary>
	/// <param name="propertyName">Property name.</param>
	protected virtual void RaisePropertyChanging(string propertyName)
	{
		RaisePropertyChanging(this, new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// To call the <see cref="PropertyChanging"/> event.
	/// </summary>
	/// <param name="sender">Sender.</param>
	/// <param name="args">Arguments.</param>
	protected virtual void RaisePropertyChanging(object sender, PropertyChangingEventArgs args)
	{
		PropertyChanging?.Invoke(sender, args);
	}

	#endregion

	#region INotifyPropertyChanged

	/// <summary>
	/// The diagram element properties value change event.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// To call the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="propertyName">Property name.</param>
	protected virtual void RaisePropertyChanged(string propertyName)
	{
		RaisePropertyChanged(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// To call the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="sender">Sender.</param>
	/// <param name="args">Arguments.</param>
	protected virtual void RaisePropertyChanged(object sender, PropertyChangedEventArgs args)
	{
		PropertyChanged?.Invoke(sender, args);
	}

	#endregion

	#region Parameter value changed event

	/// <summary>
	/// The diagram element parameter value change event.
	/// </summary>
	public event Action<string> ParameterValueChanged;

	/// <summary>
	/// To call the <see cref="ParameterValueChanged"/> event.
	/// </summary>
	/// <param name="parameterName">Parameter name.</param>
	protected virtual void RaiseParameterValueChanged(string parameterName)
	{
		ParameterValueChanged?.Invoke(parameterName);
	}

	#endregion

	#region Implementation of ICustomTypeDescriptor

	private PropertyDescriptorCollection GetAllProperties()
	{
		var paramProperties = Parameters
			.Select(p => (PropertyDescriptor)new DiagramElementParamPropertyDescriptor(p))
			.ToArray();

		return new PropertyDescriptorCollection(paramProperties);
	}

	AttributeCollection ICustomTypeDescriptor.GetAttributes()
	{
		return TypeDescriptor.GetAttributes(this, true);
	}

	string ICustomTypeDescriptor.GetClassName()
	{
		return TypeDescriptor.GetClassName(this, true);
	}

	string ICustomTypeDescriptor.GetComponentName()
	{
		return TypeDescriptor.GetComponentName(this, true);
	}

	TypeConverter ICustomTypeDescriptor.GetConverter()
	{
		return TypeDescriptor.GetConverter(this, true);
	}

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
	{
		return TypeDescriptor.GetDefaultEvent(this, true);
	}

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
	{
		return TypeDescriptor.GetDefaultProperty(this, true);
	}

	object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
	{
		return TypeDescriptor.GetEditor(this, editorBaseType, true);
	}

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
	{
		return TypeDescriptor.GetEvents(this, attributes, true);
	}

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
	{
		return TypeDescriptor.GetEvents(this, true);
	}

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
	{
		return GetAllProperties();
	}

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
	{
		return GetAllProperties();
	}

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
	{
		return this;
	}

	#endregion

	/// <summary>
	/// </summary>
	public virtual void InitializeCopy(DiagramElement copiedFrom)
	{
		if (copiedFrom is null)
			throw new ArgumentNullException(nameof(copiedFrom));

		Id = Guid.NewGuid();
	}

	/// <summary>
	/// Create a copy of <see cref="DiagramElement"/>.
	/// </summary>
	/// <returns><see cref="DiagramElement"/> copy.</returns>
	protected virtual DiagramElement CreateCopy()
		=> GetType().CreateInstance<DiagramElement>();

	/// <summary>
	/// Create a copy of <see cref="DiagramElement"/>.
	/// </summary>
	/// <param name="cloneSockets">To create copies of connections.</param>
	/// <returns>Copy.</returns>
	public virtual DiagramElement Clone(bool cloneSockets = true)
	{
		var clone = CreateCopy();

		var settings = this.Save();

		if (!cloneSockets)
		{
			settings.Remove(nameof(InputSockets));
			settings.Remove(nameof(OutputSockets));

			settings.Remove(nameof(Id));
		}

		clone!.Load(settings);

		return clone;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Id), Id);

		storage.SetValue(nameof(CanAutoName), CanAutoName);

		var paramStorage = new SettingsStorage();

		foreach (var param in Parameters.Where(p => !p.IgnoreOnSave).ToArray())
			paramStorage.SetValue(param.Name, param.Save());

		storage.SetValue(nameof(Parameters), paramStorage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_suppressSocketEvents = true;

		try
		{
			Id = storage.GetValue(nameof(Id), Id);
			CanAutoName = storage.GetValue(nameof(CanAutoName), CanAutoName);

			RemoveSockets(false);

			storage.SafeGetValue<SettingsStorage>(nameof(Parameters), parameters =>
			{
				foreach (var pair in parameters)
				{
					// набор параметров может изменяться в процессе загрузки
					var param = Parameters.FirstOrDefault(p => p.Name.EqualsIgnoreCase(pair.Key));

					if (param == null)
						continue;

					try
					{
						param.Load((SettingsStorage)pair.Value);
					}
					catch (Exception excp)
					{
						LogError("Load {0} param error:\n{1}", param.Name, excp);
					}
				}
			});

			if (!CanAutoName)
				ElementName = Name;
		}
		finally
		{
			_suppressSocketEvents = false;
		}
	}

	#region INotifyPropertiesChanged

	/// <summary>
	/// The available properties change event.
	/// </summary>
	public event Action PropertiesChanged;

	/// <summary>
	/// To call the <see cref="PropertiesChanged"/> event.
	/// </summary>
	protected virtual void RaisePropertiesChanged()
	{
		foreach (var p in GetAllProperties().Typed())
			RaisePropertyChanged(p.Name);

		PropertiesChanged?.Invoke();
	}

	#endregion

	#region undo helper

	private int _undoStateLevel;
	private SettingsStorage _savedUndoState;
	private readonly List<UndoHelper> _ongoingPropertyChanges = [];

	private class UndoHelper : Disposable
	{
		private readonly DiagramElement _parent;
		public object State {get;} // for debug purposes

		public UndoHelper(DiagramElement parent, object state = null)
		{
			_parent = parent ?? throw new ArgumentNullException(nameof(parent));

			State = state;

			if (parent.IsUndoRedoing || !parent.HasUndoManager)
				return;

			if (Interlocked.Increment(ref parent._undoStateLevel) == 1)
			{
				_parent._savedUndoState = _parent.Save();
				_parent.StartedUndoableOperation?.Invoke();
			}
		}

		protected override void DisposeManaged()
		{
			if (_parent != null && Interlocked.Decrement(ref _parent._undoStateLevel) == 0)
			{
				var oldState = _parent._savedUndoState;
				_parent._savedUndoState = null;
				_parent.CommittedUndoableOperation?.Invoke(_parent, new UndoData(_parent, oldState, _parent.Save()));
			}

			base.DisposeManaged();
		}
	}

	private readonly struct UndoData(IPersistable obj, SettingsStorage oldData, SettingsStorage newData) : IUndoableEdit
	{
		private IPersistable Object { get; } = obj;
		private SettingsStorage OldData { get; } = oldData;
		private SettingsStorage NewData { get; } = newData;

		void IUndoableEdit.Clear() { }
		bool IUndoableEdit.CanUndo() => OldData != null;
		bool IUndoableEdit.CanRedo() => NewData != null;

		void IUndoableEdit.Undo() => Object.Load(OldData);
		void IUndoableEdit.Redo() => Object.Load(NewData);
	}

	private void OnPropertyChanging(object _, PropertyChangingEventArgs e) => _ongoingPropertyChanges.Add((UndoHelper) SaveUndoState(e.PropertyName));

	private void OnPropertyChanged(object _, PropertyChangedEventArgs e)
	{
		var idx = _ongoingPropertyChanges.IndexOf(h => Equals(h.State, e.PropertyName));
		if(idx < 0)
			return;

		var helper = _ongoingPropertyChanges[idx];
		_ongoingPropertyChanges.RemoveAt(idx);

		helper.Dispose();
	}

	/// <summary>
	/// Save state to enable undo.
	/// </summary>
	protected IDisposable SaveUndoState(object debugState = null) => new UndoHelper(this, debugState);

	#endregion

	/// <summary>
	/// <see cref="IDispatcher"/>.
	/// </summary>
	protected virtual IDispatcher Dispatcher => ConfigManager.GetService<IDispatcher>();
}
