namespace StockSharp.Diagram;

/// <summary>
/// The debugger of the diagram composite element.
/// </summary>
public class DiagramDebugger : Disposable, IDebugger
{
	private readonly CachedSynchronizedDictionary<DiagramSocket, DiagramSocketBreakpoint> _breakpoints = [];
	private readonly SynchronizedDictionary<DebuggerSyncObject, (CompositionDiagramElement composition, CompositionDiagramElement parent)> _compositions = [];
	private DebuggerSyncObject _syncObject;
	private CompositionDiagramElement _composition;

	/// <summary>
	/// Breakpoints (sockets, on which the data transmission will be stopped).
	/// </summary>
	public IEnumerable<DiagramSocketBreakpoint> Breakpoints => _breakpoints.CachedValues;

	/// <inheritdoc />
	public bool IsWaitingOnInput => _syncObject.IsWaitingOnInput || _compositions.Any(p => p.Key.IsWaitingOnInput);

	/// <inheritdoc />
	public bool IsWaitingOnOutput => _syncObject.IsWaitingOnOutput || _compositions.Any(p => p.Key.IsWaitingOnOutput);

	/// <inheritdoc />
	public bool CanStepInto => IsWaitingOnInput && HoverBlock is not null;

	/// <inheritdoc />
	public bool CanStepOut => _compositions[_syncObject].parent != null;

	/// <inheritdoc />
	public bool IsWaitingOnError => _syncObject.CurrentError != null || _compositions.Any(p => p.Key.CurrentError != null);

	/// <summary>
	/// <see langword="false" />, if the debugger is used. Otherwise, <see langword="true" />.
	/// </summary>
	public bool IsDisabled { get; set; }

	/// <summary>
	/// Composite element.
	/// </summary>
	public CompositionDiagramElement Composition
	{
		get => _composition;
		private set
		{
			if (_composition == value)
				return;

			_composition = value;
			CompositionChanged?.Invoke(_composition);
			_blockChanged?.Invoke(_composition);
		}
	}

	/// <summary>
	/// The diagram composite element change event.
	/// </summary>
	public event Action<CompositionDiagramElement> CompositionChanged;

	private Action<object> _blockChanged;

	event Action<object> IDebugger.BlockChanged
	{
		add => _blockChanged += value;
		remove => _blockChanged -= value;
	}

	private Action<object> _break;

	event Action<object> IDebugger.Break
	{
		add => _break += value;
		remove => _break -= value;
	}

	/// <summary>
	/// The event of the stop at the breakpoint.
	/// </summary>
	public event Action<DiagramSocket> Break;

	private Action<object> _error;

	event Action<object> IDebugger.Error
	{
		add => _error += value;
		remove => _error -= value;
	}

	/// <summary>
	/// The event of the stop at the error.
	/// </summary>
	public event Action<DiagramElement> Error;

	/// <inheritdoc />
	public event Action Changed;

	/// <inheritdoc />
	public event Action Continued;

	/// <summary>
	/// Add breakpoint event.
	/// </summary>
	public event Action<DiagramSocket> Added;

	/// <summary>
	/// Remove breakpoint event.
	/// </summary>
	public event Action<DiagramSocket> Removed;

	private void RaiseChanged() => Changed?.Invoke();

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramDebugger"/>.
	/// </summary>
	/// <param name="composition">Composite element.</param>
	public DiagramDebugger(CompositionDiagramElement composition)
	{
		Root = Composition = composition ?? throw new ArgumentNullException(nameof(composition));
		Root.ElementAdded += OnCompositionElementAdded;
		Root.ElementRemoved += OnCompositionElementRemoved;

		Root.DebuggerSyncObject = _syncObject = SetSyncObject(Composition);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		Root.ElementAdded -= OnCompositionElementAdded;
		Root.ElementRemoved -= OnCompositionElementRemoved;
	}

	/// <summary>
	/// The root diagram element.
	/// </summary>
	public CompositionDiagramElement Root { get; }

	/// <inheritdoc />
	public object ExecBlock => Composition;

	/// <inheritdoc />
	public object HoverBlock => _syncObject.CurrentElement as CompositionDiagramElement;

	private DebuggerSyncObject SetSyncObject(CompositionDiagramElement composition, CompositionDiagramElement parent = null)
	{
		var syncObject = new DebuggerSyncObject(this, parent ?? composition, OnIsBreak, OnSyncObjectBreak, OnSyncObjectError);

		_compositions.Add(syncObject, (composition, parent));

		foreach (var element in composition.Elements)
		{
			SetSyncObject(composition, element, syncObject);
		}

		return syncObject;
	}

	private bool OnIsBreak(DiagramSocket socket)
		=> !IsDisabled && Breakpoints.FirstOrDefault(i => i.Socket == socket)?.NeedBreak() == true;

	private void OnSyncObjectBreak(DebuggerSyncObject sync)
	{
		var socket = sync.CurrentSocket;
		Break?.Invoke(socket);
		_break?.Invoke(socket);
		SelectComposition(sync);
	}

	private void OnSyncObjectError(DebuggerSyncObject sync)
	{
		var elem = sync.CurrentElement;
		Error?.Invoke(elem);
		_error?.Invoke(elem);
		SelectComposition(sync);
	}

	private void SelectComposition(DebuggerSyncObject sync)
	{
		var composition = _compositions[sync];

		if (Composition == composition.composition)
			return;

		_syncObject = sync;
		Composition = composition.composition;
	}

	private void OnCompositionElementAdded(DiagramElement element)
	{
		var composition = Composition;
		var syncObject = _compositions.First(p => p.Value.composition == composition).Key;

		SetSyncObject(composition, element, syncObject);
	}

	private void OnCompositionElementRemoved(DiagramElement element)
	{
		foreach (var socket in element.GetAllSockets())
			RemoveBreak(socket);
	}

	private void SetSyncObject(CompositionDiagramElement composition, DiagramElement element, DebuggerSyncObject syncObject)
	{
		element.DebuggerSyncObject = syncObject ?? throw new ArgumentNullException(nameof(syncObject));

		foreach (var socket in element.GetAllSockets().BreakOnly())
			AddBreak(socket);

		if (element is CompositionDiagramElement c)
			SetSyncObject(c, composition);
	}

	/// <summary>
	/// To get the breakpoint by the socket.
	/// </summary>
	/// <param name="socket"><see cref="DiagramSocket"/></param>
	/// <param name="breakpoint"><see cref="DiagramSocketBreakpoint"/></param>
	/// <returns>Operation result.</returns>
	public bool TryGetBreakpoint(DiagramSocket socket, out DiagramSocketBreakpoint breakpoint)
		=> _breakpoints.TryGetValue(socket, out breakpoint);

	/// <summary>
	/// To add a breakpoint in the socket.
	/// </summary>
	/// <param name="socket">Socket.</param>
	/// <returns>Operation result.</returns>
	public bool AddBreak(DiagramSocket socket)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (_breakpoints.ContainsKey(socket))
			return false;

		var breakpoint = CreateSocketBreakpoint(socket);

		AddBreak(breakpoint);

		Added?.Invoke(socket);

		RaiseChanged();

		return true;
	}

	/// <summary>
	/// To remove the breakpoint from the socket.
	/// </summary>
	/// <param name="socket">Socket.</param>
	/// <returns>Operation result.</returns>
	public bool RemoveBreak(DiagramSocket socket)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		if (!_breakpoints.Remove(socket))
			return false;

		socket.IsBreak = false;
		socket.PropertyChanged -= OnSocketPropertyChanged;

		Removed?.Invoke(socket);

		RaiseChanged();

		return true;
	}

	/// <summary>
	/// Remove all breakpoints from the scheme.
	/// </summary>
	public void RemoveAllBreaks()
	{
		foreach (var breakpoint in Breakpoints)
		{
			var socket = breakpoint.Socket;

			socket.IsBreak = false;
			socket.PropertyChanged -= OnSocketPropertyChanged;

			Removed?.Invoke(socket);
		}

		_breakpoints.Clear();

		RaiseChanged();
	}

	/// <summary>
	/// Whether the socket is the breakpoint.
	/// </summary>
	/// <param name="socket">Socket.</param>
	/// <returns><see langword="true" />, if the socket is the breakpoint, otherwise, <see langword="false" />.</returns>
	public bool IsBreak(DiagramSocket socket)
	{
		if (socket == null)
			throw new ArgumentNullException(nameof(socket));

		return Breakpoints.Any(i => i.Socket == socket);
	}

	/// <inheritdoc />
	public void StepNext()
	{
		var syncObject = GetCurrentSyncObject();

		if (syncObject == null)
			return;

		if (syncObject != _syncObject)
			OnSyncObjectBreak(syncObject);

		syncObject.ContinueAndWaitOnNext();
	}

	/// <inheritdoc />
	public void StepInto()
	{
		if (!CanStepInto)
			return;

		var composition = (CompositionDiagramElement)_syncObject.CurrentElement;

		var newSync = _compositions.First(p => p.Value.composition == composition).Key;
		var oldSync = _syncObject;

		_syncObject = newSync;

		Composition = composition;

		if (oldSync.CurrentElement != composition)
			return;

		newSync.SetWaitOnNext();
		oldSync.ContinueAndWaitOnNext();
	}

	/// <inheritdoc />
	public void StepOut()
	{
		if (!CanStepOut)
			return;

		var (composition, parent) = _compositions[_syncObject];

		var newSync = _compositions.First(c => c.Value.composition == parent).Key;
		var oldSync = _syncObject;

		_syncObject = newSync;

		Composition = parent;

		if (composition != null && newSync.CurrentElement != composition)
			return;

		newSync.SetWaitOnNext();
		oldSync.Continue();
	}

	/// <summary>
	/// Continue.
	/// </summary>
	public void Continue()
	{
		var syncObject = GetCurrentSyncObject();

		if (syncObject == null)
			return;

		//if (syncObject != _syncObject)
		//	OnSyncObjectBreak(syncObject);

		syncObject.Continue();

		if (!IsDisabled)
			Continued?.Invoke();
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		var breakPoints = storage.GetValue<SettingsStorage[]>(nameof(Breakpoints));

		if (breakPoints is null || Composition is null)
			return;

		foreach (var breakPoint in breakPoints)
		{
			var elementIds = breakPoint.GetValue<IEnumerable<Guid>>("ElementIds");
			var socketId = breakPoint.GetValue<string>("SocketId");

			if (socketId.IsEmpty() || elementIds is null)
				continue;

			DiagramElement element = null;

			var elements = Composition.Elements;

			foreach (var id in elementIds)
			{
				element = elements.FirstOrDefault(e => e.Id == id);

				if (element is CompositionDiagramElement composition)
					elements = composition.Elements;
			}

			var socket = element?.InputSockets.FindById(socketId) ?? element?.OutputSockets.FindById(socketId);

			if (socket is null)
				continue;

			var obj = CreateSocketBreakpoint(socket);
			obj.Load(breakPoint);

			AddBreak(obj);
		}
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		static Guid[] getElementIds(DiagramElement element)
		{
			var names = new List<Guid>();

			while (element != null)
			{
				names.Insert(0, element.Id);
				element = element.ParentComposition;
			}

			return [.. names];
		}

		storage.SetValue(nameof(Breakpoints), Breakpoints.Select(breakpoint =>
		{
			var storage = breakpoint.Save();

			storage.SetValue("SocketId", breakpoint.Socket.Id);
			storage.SetValue("ElementIds", getElementIds(breakpoint.Socket.Parent));

			return storage;
		}).ToArray());
	}

	private void AddBreak(DiagramSocketBreakpoint breakPoint)
	{
		if (breakPoint is null)
			throw new ArgumentNullException(nameof(breakPoint));

		var socket = breakPoint.Socket;

		socket.IsBreak = true;
		socket.PropertyChanged += OnSocketPropertyChanged;

		_breakpoints.Add(socket, breakPoint);
	}

	private void OnSocketPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		var socket = (DiagramSocket)sender;

		if (e.PropertyName != nameof(socket.Type))
			return;

		RemoveBreak(socket);
		AddBreak(socket);
	}

	private DebuggerSyncObject GetCurrentSyncObject()
	{
		if (_syncObject.IsWaitingOnInput || _syncObject.IsWaitingOnOutput)
			return _syncObject;

		return _compositions.Keys.FirstOrDefault(s => s.IsWaitingOnInput || s.IsWaitingOnOutput);
	}

	private static DiagramSocketBreakpoint CreateSocketBreakpoint(DiagramSocket socket)
	{
		DiagramSocketBreakpoint breakpoint;

		var type = socket.Type;

		if (type == DiagramSocketType.Bool)
		{
			breakpoint = new BooleanDiagramSocketBreakpoint(socket);
		}
		else if (type == DiagramSocketType.IndicatorValue || type == DiagramSocketType.Unit)
		{
			breakpoint = new RangeDiagramSocketBreakpoint<decimal>(socket);
		}
		else if (type == DiagramSocketType.Date)
		{
			breakpoint = new RangeDiagramSocketBreakpoint<DateTimeOffset>(socket);
		}
		else if (type == DiagramSocketType.Time)
		{
			breakpoint = new RangeDiagramSocketBreakpoint<TimeSpan>(socket);
		}
		else if (type.Type.IsEnum)
		{
			breakpoint = typeof(EnumDiagramSocketBreakpoint<>).Make(type.Type).CreateInstance<DiagramSocketBreakpoint>(socket);
		}
		else
			breakpoint = new DiagramSocketBreakpoint(socket);

		return breakpoint;
	}
}