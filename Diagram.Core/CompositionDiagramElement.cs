namespace StockSharp.Diagram;

/// <summary>
/// Composite element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CompositeElementKey,
	Description = LocalizedStrings.CompositeElementKey,
	GroupName = LocalizedStrings.OwnElementsKey
)]
public class CompositionDiagramElement : DiagramElement
{
	private class InnerElementParam : Disposable, IDiagramElementParam
	{
		private readonly DiagramElement _element;
		private readonly IDiagramElementParam _param;

		public InnerElementParam(DiagramElement element, IDiagramElementParam param)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));
			_param = param ?? throw new ArgumentNullException(nameof(param));

			Attributes = [];
			Attributes.AddRange(_param.Attributes);

			TryUpdateDisplay();

			_element.PropertyChanged += OnElementPropertyChanged;
		}

		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			_element.PropertyChanged -= OnElementPropertyChanged;
		}

		private void TryUpdateDisplay()
		{
			var display = Attributes.OfType<DisplayAttribute>().FirstOrDefault();

			if (display is null)
				return;

			Attributes.Remove(display);
			Attributes.Add(new DisplayAttribute
			{
				Name = _element is VariableDiagramElement or CandlesDiagramElement ? _element.Name : $"{display.Name} ({_element.Name})",
				Description = display.Description,
				GroupName = LocalizedStrings.Schema,
			});
		}

		private void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_element.Name))
			{
				TryUpdateDisplay();
			}
		}

		string IDiagramElementParam.Name
		{
			get => _element.Id + _param.Name;
			set => throw new NotSupportedException();
		}

		Type IDiagramElementParam.Type => _param.Type;

		public IList<Attribute> Attributes { get; }

		object IDiagramElementParam.Value
		{
			get => _param.Value;
			set => _param.Value = value;
		}

		bool IDiagramElementParam.IsDefault => _param.IsDefault;

		bool IDiagramElementParam.CanOptimize
		{
			get => _param.CanOptimize;
			set => throw new NotSupportedException();
		}

		bool IDiagramElementParam.IgnoreOnSave
		{
			get => _param.IgnoreOnSave;
			set => _param.IgnoreOnSave = value;
		}

		bool IDiagramElementParam.NotifyOnChanged
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		void IDiagramElementParam.SetValueWithIgnoreOnSave(object value)
			=> _param.SetValueWithIgnoreOnSave(value);

		public void Load(SettingsStorage storage)
			=> _param.Load(storage);

		public void Save(SettingsStorage storage)
			=> _param.Save(storage);

		public override string ToString()
			=> _param.ToString();

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { }
			remove { }
		}

		event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
		{
			add { }
			remove { }
		}
	}

	private readonly List<InnerElementParam> _modelParams = [];

	/// <inheritdoc />
	public override IEnumerable<IDiagramElementParam> Parameters
		=> base.Parameters.Concat(_modelParams);

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositionDiagramElement"/>.
	/// </summary>
	/// <param name="model"><see cref="ICompositionModel"/></param>
	public CompositionDiagramElement(ICompositionModel model)
	{
		Model = model;
		ShowParameters = true;
		IsLoaded = true;
	}

	/// <inheritdoc />
	public override string GetDisplayName() => Name;

	/// <inheritdoc />
	public override string GetDescription() => Description;

	/// <inheritdoc />
	public override string GetCategory() => LocalizedStrings.OwnElements;

	private ICompositionModel _model;

	/// <summary>
	/// <see cref="ICompositionModel"/>
	/// </summary>
	[Browsable(false)]
	public ICompositionModel Model
	{
		get => _model;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (_model == value)
				return;

			if (_model != null)
				UnSubscribeModel();

			_model = value;

			if (_model == null)
				return;

			SubscribeModel();
		}
	}

	/// <summary>
	/// Other sockets if this one is connected.
	/// </summary>
	/// <param name="socket"><see cref="DiagramSocket"/></param>
	/// <returns>Connected sockets.</returns>
	public IEnumerable<DiagramSocket> ConnectedToSockets(DiagramSocket socket)
		=> Model.GetConnectedSocketsFor(this, socket) ?? [];

	private string _category;

	/// <summary>
	/// The name of the group which includes a diagram element.
	/// </summary>
	public new string Category
	{
		get => _category;
		set
		{
			_category = value;
			RaisePropertyChanged(nameof(Category));
		}
	}

	/// <summary>
	/// Schema version.
	/// </summary>
	public int SchemaVersion { get; set; } = 1;

	/// <inheritdoc/>
	public override string DocUrl { get; set; }

	private Guid _typeId = Guid.NewGuid();

	/// <inheritdoc />
	public override Guid TypeId => _typeId;

	/// <summary>
	/// <see cref="TypeId"/>
	/// </summary>
	/// <param name="typeId"><see cref="TypeId"/></param>
	protected void SetTypeId(Guid typeId) => _typeId = typeId;

	/// <summary>
	/// <see cref="ICompositionModel.Elements"/>
	/// </summary>
	public IEnumerable<DiagramElement> Elements => Model.Elements.WhereNotNull();

	/// <summary>
	/// <see cref="ICompositionModel.HasErrors"/>
	/// </summary>
	public bool HasErrors => Model.HasErrors;

	private bool _isLoaded;

	/// <summary>
	/// Is composite diagram element loaded.
	/// </summary>
	public bool IsLoaded
	{
		get => _isLoaded;
		set
		{
			_isLoaded = value;
			RaisePropertyChanged(nameof(IsLoaded));
		}
	}

	/// <inheritdoc />
	public override string IconName { get; } = "Puzzle";

	/// <inheritdoc/>
	public override bool IsUndoRedoing => Model?.UndoManager?.IsUndoingRedoing == true;

	/// <inheritdoc/>
	public override bool HasUndoManager => Model?.UndoManager != null;

	private DiagramStrategy _strategy;

	/// <summary>
	/// The strategy to which the element is attached.
	/// </summary>
	public override DiagramStrategy Strategy
	{
		get => _strategy ?? ParentComposition?.Strategy;
		set
		{
			if (_strategy == value)
				return;

			_strategy = value;
			RaisePropertyChanged(nameof(Strategy));
			RaiseStrategyChanged();
		}
	}

	/// <summary>
	/// The last error element.
	/// </summary>
	public DiagramElement LastErrorElement { get; internal set; }

	/// <summary>
	/// The last error.
	/// </summary>
	public Exception LastError { get; internal set; }

	/// <summary>
	/// Child element added.
	/// </summary>
	public event Action<DiagramElement> ElementAdded;

	/// <summary>
	/// Child element removed.
	/// </summary>
	public event Action<DiagramElement> ElementRemoved;

	/// <summary>
	/// Raised when strategy changed.
	/// </summary>
	public event Action StrategyChanged;

	/// <summary>
	/// The composite element diagram change event.
	/// </summary>
	public event Action Changed;

	/// <summary>
	/// Suspend undo/redo manager for <see cref="Model"/>.
	/// </summary>
	public void SuspendUndoManager()
	{
		Model.IsUndoManagerSuspended = true;

		Elements
			.OfType<CompositionDiagramElement>()
			.ForEach(e => e.SuspendUndoManager());
	}

	/// <summary>
	/// Resume undo/redo manager for <see cref="Model"/>.
	/// </summary>
	public void ResumeUndoManager()
	{
		Model.IsUndoManagerSuspended = false;

		Elements
			.OfType<CompositionDiagramElement>()
			.ForEach(e => e.ResumeUndoManager());
	}

	/// <inheritdoc />
	protected override void RaiseParameterValueChanged(string parameterName)
	{
		base.RaiseParameterValueChanged(parameterName);
		RaiseChanged();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_typeId = storage.GetValue<string>(nameof(TypeId)).To<Guid>();

		RefreshModelData();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(TypeId), _typeId)
		;
	}

	/// <inheritdoc />
	protected override void OnInit()
	{
		base.OnInit();
		Elements.ForEach(e => e.Init(this));
	}

	/// <inheritdoc />
	protected override void OnUnInit()
	{
		Elements.ForEach(e => e.UnInit());
		base.OnUnInit();
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		ThrowIfHasErrors();

		Elements.ForEach(e => e.Prepare());

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		Elements.ForEach(e => e.Start(time));

		base.OnStart(time);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		ThrowIfHasErrors();

		Elements.ForEach(e => e.Stop());

		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		LastError = null;
		LastErrorElement = null;

		_flushingElements.Clear();

		Elements.ForEach(e => e.Reset());
	}

	private void ThrowIfHasErrors()
	{
		if (HasErrors)
			throw new InvalidOperationException(LocalizedStrings.DiagramHasError);
	}

	private readonly HashSet<DiagramElement> _flushingElements = [];

	internal void AddFlushElement(DiagramElement element)
	{
		if (!_flushingElements.Add(element))
			return;

		if (FlushPriority < element.FlushPriority)
			FlushPriority = element.FlushPriority;
	}

	internal void RemoveFlushElement(DiagramElement element)
	{
		if (_flushingElements.Remove(element))
			FlushPriority = _flushingElements.Count == 0 ? FlushDisabled : _flushingElements.Max(e => e.FlushPriority);
	}

	/// <inheritdoc />
	public override void Flush(DateTimeOffset time)
		=> _flushingElements.OrderBy(e => e.FlushPriority).ToArray().ForEach(e => e.Flush(time));

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time,
		IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
	}

	/// <summary>
	/// Invoke <see cref="ElementAdded"/>.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	protected void RaiseElementAdded(DiagramElement element)
		=> ElementAdded?.Invoke(element);

	/// <summary>
	/// Invoke <see cref="ElementRemoved"/>.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	protected void RaiseElementRemoved(DiagramElement element)
		=> ElementRemoved?.Invoke(element);

	/// <summary>
	/// Invoke <see cref="Changed"/>.
	/// </summary>
	protected void RaiseChanged() => Changed?.Invoke();

	/// <summary>
	/// Invoke  <see cref="StrategyChanged"/>.
	/// </summary>
	protected void RaiseStrategyChanged() => StrategyChanged?.Invoke();

	#region Model

	private void SubscribeModel()
	{
		var model = Model;

		model.ModelChanged += ModelChanged;
		model.ElementAdded += ModelElementAdded;
		model.ElementRemoved += ModelElementRemoved;

		Elements.ForEach(OnElementAddded);

		RefreshModelData();
	}

	private void ModelChanged()
	{
		RefreshModelData();
		RaiseChanged();
	}

	private void UnSubscribeModel()
	{
		var model = Model;

		model.ModelChanged -= ModelChanged;
		model.ElementAdded -= ModelElementAdded;
		model.ElementRemoved -= ModelElementRemoved;

		Elements.ForEach(OnElementRemoved);

		RemoveSockets();
	}

	private void ClearModelParams()
	{
		foreach (var e in _modelParams.CopyAndClear())
			e.Dispose();
	}

	private void ModelElementAdded(DiagramElement element)
	{
		OnElementAddded(element);
		RefreshModelData();

		RaiseElementAdded(element);
		RaisePropertiesChanged();
	}

	private void ModelElementRemoved(DiagramElement element)
	{
		OnElementRemoved(element);
		RefreshModelData();

		RaiseElementRemoved(element);
		RaisePropertiesChanged();
	}

	private void OnElementAddded(DiagramElement element)
	{
		//element.NameChanging += CheckElementName;
		element.Init(this);
		element.PropertyChanged += RaisePropertyChanged;
		element.PropertiesChanged += OnChildElementPropertiesChanged;
		element.ParameterValueChanged += RaiseParameterValueChanged;
	}

	private void OnElementRemoved(DiagramElement element)
	{
		//element.NameChanging -= CheckElementName;
		element.PropertyChanged -= RaisePropertyChanged;
		element.PropertiesChanged -= OnChildElementPropertiesChanged;
		element.ParameterValueChanged -= RaiseParameterValueChanged;
		element.UnInit();
	}

	private class CompositionSocket(DiagramSocketDirection dir, string socketId = null)
		: DiagramSocket(dir, socketId)
	{
		public string InternalNodeKey { get; set; }
		public string InternalSocketId { get; set; }

		public DiagramElement InternalNode => (Parent as CompositionDiagramElement)?.Model.FindElementByKey(InternalNodeKey);

		public DiagramSocket InternalSocket
		{
			get
			{
				var node = InternalNode;
				return (Directon == DiagramSocketDirection.In ? node?.InputSockets : node?.OutputSockets)?.FindById(InternalSocketId);
			}
		}
	}

	/// <inheritdoc />
	protected override DiagramSocket CreateSocketInstance(DiagramSocketDirection dir, string socketId = null) => new CompositionSocket(dir, socketId);

	private static string GenerateSocketId(string nodeKey, string internalSocketId) => $"{nodeKey}_{internalSocketId}";

	private void RefreshModelData()
	{
		UnSubscribeChildOutput();

		var sockets = Model.GetDisconnectedSockets().ToArray();

		static (string nodeKey, DiagramSocketDirection dir, string socketId, DiagramSocketType sockType, int linkMax) CreateSocketKey(string nodeKey, DiagramSocket socket)
			=> (nodeKey, socket?.Directon ?? DiagramSocketDirection.In, socket?.Id, socket?.Type, socket?.LinkableMaximum ?? 0);

		var needKeys = sockets.Select(s => CreateSocketKey(s.nodeKey, s.socket)).ToSet();

		RemoveSockets(s =>
		{
			var s1 = (CompositionSocket)s;
			var internalSocket = s1.InternalSocket;
			var key = CreateSocketKey(s1.InternalNodeKey, internalSocket);
			return internalSocket == null || !needKeys.Contains(key);
		});

		// treat all disconnected ports for all elements in this composition
		// as inputs or outputs for the entire composition
		sockets
			.ForEach(s =>
			{
				var (nodeKey, socket) = s;
				var socketKey = CreateSocketKey(nodeKey, socket);

				var existing = (CompositionSocket)this.GetAllSockets().FirstOrDefault(s2 =>
				{
					var s2comp = (CompositionSocket)s2;
					var s2compKey = CreateSocketKey(s2comp.InternalNodeKey, s2comp.InternalSocket);
					return socketKey.Equals(s2compKey);
				});

				if (existing != null)
				{
					var sourceSockets = GetConnectedSourceSockets(existing);
					sourceSockets.ForEach(ss =>
					{
						var internalSocket = existing.InternalSocket;
						internalSocket.Disconnect(ss);
						internalSocket.Connect(ss);
					});
				}
				else
				{
					var newSocketId = GenerateSocketId(nodeKey, socket.Id);
					var newSocket = (CompositionSocket)(socket.Directon == DiagramSocketDirection.In ? AddInput(newSocketId, socket.Name, socket.Type, linkableMax: socket.LinkableMaximum) : AddOutput(newSocketId, socket.Name, socket.Type, linkableMax: socket.LinkableMaximum));

					newSocket.InternalNodeKey = nodeKey;
					newSocket.InternalSocketId = socket.Id;
				}
			});

		SubscribeChildOutput();
		UpdateModelParameters();
	}

	private DiagramElement[] _subscribedChildOutput;

	private void SubscribeChildOutput()
	{
		UnSubscribeChildOutput();

		_subscribedChildOutput = [.. GetOutputElements()];
		_subscribedChildOutput.ForEach(o => o.ProcessOutput += ProcessChildOutput);
	}

	private void UnSubscribeChildOutput()
	{
		if (_subscribedChildOutput == null)
			return;

		_subscribedChildOutput.ForEach(o => o.ProcessOutput -= ProcessChildOutput);
		_subscribedChildOutput = null;
	}

	private void UpdateModelParameters()
	{
		ClearModelParams();

		_modelParams.AddRange(Elements
			.Where(e => e.ShowParameters)
			.SelectMany(e => e.Parameters.Where(p => p.CanOptimize).Select(p => new InnerElementParam(e, p))));
	}

	private void OnChildElementPropertiesChanged()
	{
		UpdateModelParameters();
		RaisePropertiesChanged();
	}

	#endregion

	#region Init/Process

	/// <inheritdoc />
	protected override void OnSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		base.OnSocketConnected(socket, source);

		if (socket.IsOutput)
			return;

		((CompositionSocket)socket).InternalSocket?.Connect(source);
	}

	/// <inheritdoc />
	protected override void OnSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		base.OnSocketDisconnected(socket, source);

		if (socket.IsOutput)
			return;

		((CompositionSocket)socket).InternalSocket?.Disconnect(source);
	}

	private IEnumerable<DiagramElement> GetOutputElements()
	{
		if (Model == null)
			return [];

		return [.. OutputSockets
			.OfType<CompositionSocket>()
			.Select(s => s.InternalNode)
			.WhereNotNull()
			.Distinct()];
	}

	private void ProcessChildOutput(DiagramSocketValue value)
	{
		var nodeKey = Model?.GetElementKey(value.Socket.Parent);
		var socket = OutputSockets.OfType<CompositionSocket>().FirstOrDefault(s => s.InternalNodeKey.EqualsIgnoreCase(nodeKey) && s.InternalSocketId == value.Socket.Id);

		if (socket == null)
			return;

		RaiseProcessOutput(socket, value.Time, value.Value, value);
	}

	#endregion

	#region Undo/redo

	/// <summary>
	/// This predicate is true when one can call <see cref="Undo"/>.
	/// </summary>
	/// <returns>Check result.</returns>
	public bool CanUndo()
	{
		return Model != null && Model.UndoManager is not null && Model.UndoManager.CanUndo();
	}

	/// <summary>
	/// Restore the state of some models to before the current state.
	/// </summary>
	public void Undo()
	{
		Model?.UndoManager?.Undo();
	}

	/// <summary>
	/// This predicate is true when one can call <see cref="Redo"/>.
	/// </summary>
	/// <returns>Check result.</returns>
	public bool CanRedo()
	{
		return Model != null && Model.UndoManager is not null && Model.UndoManager.CanRedo();
	}

	/// <summary>
	/// Restore the state of some models to after the current state.
	/// </summary>
	public void Redo()
	{
		Model?.UndoManager?.Redo();
	}

	#endregion

	/// <summary>
	/// Find all portfolios in elements.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Portfolio> FindPortfolios()
	{
		return this
			.FindAllElements<VariableDiagramElement>()
			.Where(vde => vde.Type == DiagramSocketType.Portfolio)
			.Select(vde => vde.Value)
			.OfType<Portfolio>()
			.Distinct();
	}

	/// <summary>
	/// Find all <see cref="MarketDepthPanelDiagramElement"/>.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<MarketDepthPanelDiagramElement> FindMarketDepthPanels()
	{
		return this
			.FindAllElements<MarketDepthPanelDiagramElement>()
			.Distinct();
	}

	private void UpdateTypeIdImpl(List<(Guid oldTypeId, Guid newTypeId)> map, Guid? id = null)
	{
		var rename = map.FirstOrDefault(p => p.oldTypeId == TypeId);
		var found = rename != default;

		var newId = found ? rename.newTypeId : id ?? Guid.NewGuid();

		if (id != null && id != newId)
			throw new InvalidOperationException("rename conflict");

		if (!found)
			map.Add((oldTypeId: TypeId, newTypeId: newId));

		SetTypeId(newId);

		Elements
			.OfType<CompositionDiagramElement>()
			.ForEach(e => e.UpdateTypeIdImpl(map));
	}

	/// <summary>
	/// Update <see cref="DiagramElement.TypeId"/> for composition elements.
	/// </summary>
	/// <param name="id">New value for <see cref="DiagramElement.TypeId"/>. Can be <see langword="null"/>.</param>
	public void UpdateTypeId(Guid? id = null) => UpdateTypeIdImpl([], id);

	/// <summary>
	/// Create a copy of <see cref="CompositionDiagramElement"/>.
	/// </summary>
	/// <param name="cloneSockets">To create copies of connections.</param>
	/// <returns>Copy.</returns>
	public override DiagramElement Clone(bool cloneSockets = true)
	{
		var clone = new CompositionDiagramElement(Model.Clone())
		{
			Category = Category,
			DocUrl = DocUrl,
		};

		var settings = this.Save();

		if (!cloneSockets)
		{
			settings.Remove(nameof(InputSockets));
			settings.Remove(nameof(OutputSockets));
		}

		clone.Load(settings);

		return clone;
	}
}
