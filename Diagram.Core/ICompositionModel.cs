namespace StockSharp.Diagram;

using System.Drawing;

/// <summary>
/// <see cref="CompositionDiagramElement"/> model.
/// </summary>
public interface ICompositionModel : ICloneable<ICompositionModel>
{
	/// <summary>
	/// <see cref="ICompositionModelBehavior{TNode, TLink}"/>
	/// </summary>
	object Behavior { get; }

	/// <summary>
	/// To check the composite element for errors in diagram.
	/// </summary>
	bool HasErrors { get; }

	/// <summary>
	/// Is it possible to edit a composite element diagram.
	/// </summary>
	bool Modifiable { get; set; }

	/// <summary>
	/// <see cref="IUndoManager"/>
	/// </summary>
	IUndoManager UndoManager { get; set; }

	/// <summary>
	/// Undo manager is suspended if this property is set to true.
	/// </summary>
	bool IsUndoManagerSuspended { get; set; }

	/// <summary>
	/// Child elements.
	/// </summary>
	IEnumerable<DiagramElement> Elements { get; }

	/// <summary>
	/// Add element.
	/// </summary>
	/// <param name="element">The diagram element.</param>
	/// <param name="location">Element position.</param>
	void AddElement(DiagramElement element, PointF location = default);

	/// <summary>
	/// Get connected sockets.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	/// <param name="socket"><see cref="DiagramSocket"/></param>
	/// <returns>Connected sockets.</returns>
	IEnumerable<DiagramSocket> GetConnectedSocketsFor(DiagramElement element, DiagramSocket socket);

	/// <summary>
	/// Get disconnected sockets.
	/// </summary>
	/// <returns>Disconnected sockets.</returns>
	IEnumerable<(string nodeKey, DiagramSocket socket)> GetDisconnectedSockets();

	/// <summary>
	/// Get element unique key.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	/// <returns>Key.</returns>
	string GetElementKey(DiagramElement element);

	/// <summary>
	/// Find element by unique key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns><see cref="DiagramElement"/></returns>
	DiagramElement FindElementByKey(string key);

	/// <summary>
	/// Changed event.
	/// </summary>
	event Action ModelChanged;

	/// <summary>
	/// Child element added event.
	/// </summary>
	event Action<DiagramElement> ElementAdded;

	/// <summary>
	/// Child element removed event.
	/// </summary>
	event Action<DiagramElement> ElementRemoved;
}

/// <summary>
/// Default implementation of <see cref="ICompositionModel"/>.
/// </summary>
/// <typeparam name="TNode">Node type.</typeparam>
/// <typeparam name="TLink">Link type.</typeparam>
public class CompositionModel<TNode, TLink> : ICompositionModel
	where TNode : ICompositionModelNode, new()
	where TLink : ICompositionModelLink, new()
{
	private const string _transactionName = "AddRemoveTransaction";

	private readonly ICompositionModelBehavior<TNode, TLink> _behavior;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositionModel{TNode,TLink}"/>.
	/// </summary>
	/// <param name="behavior"><see cref="ICompositionModelBehavior{TNode,TLink}"/></param>
	public CompositionModel(ICompositionModelBehavior<TNode, TLink> behavior)
	{
		_behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
		_behavior.Modifiable = true;
		_behavior.Parent = this;

		_behavior.BehaviorChanged += OnBehaviorChanged;
	}

	object ICompositionModel.Behavior => _behavior;
	bool ICompositionModel.HasErrors => Elements.Any(s => s is null);

	/// <inheritdoc/>
	public bool Modifiable
	{
		get => _behavior.Modifiable;
		set => _behavior.Modifiable = value;
	}

	/// <inheritdoc/>
	public IUndoManager UndoManager
	{
		get => _behavior.UndoManager;
		set => _behavior.UndoManager = value;
	}

	/// <inheritdoc/>
	public bool IsUndoManagerSuspended
	{
		get => _behavior.IsUndoManagerSuspended;
		set => _behavior.IsUndoManagerSuspended = value;
	}

	/// <inheritdoc/>
	public event Action ModelChanged;

	/// <inheritdoc/>
	public event Action<DiagramElement> ElementAdded;

	/// <inheritdoc/>
	public event Action<DiagramElement> ElementRemoved;

	/// <summary>
	/// Nodes.
	/// </summary>
	public IEnumerable<TNode> Nodes
	{
		get => _behavior.Nodes;
		set => _behavior.Nodes = value;
	}

	/// <summary>
	/// Links.
	/// </summary>
	public IEnumerable<TLink> Links
	{
		get => _behavior.Links;
		set => _behavior.Links = value;
	}

	/// <inheritdoc/>
	public IEnumerable<DiagramElement> Elements => Nodes.Select(e => e.Element);

	DiagramElement ICompositionModel.FindElementByKey(string key)
		=> _behavior.FindNodeByKey(key)?.Element;

	string ICompositionModel.GetElementKey(DiagramElement element)
		=> GetNode(element)?.Key;

	private TNode GetNode(DiagramElement element)
		=> Nodes.FirstOrDefault(e => e.Element == element);

	private void OnBehaviorChanged((ModelChange change, object data, string propertyName, object oldValue, object oldParam, object newValue, object newParam) t)
	{
		//private bool _isReconnecting;

		void Disconnect(TLink link)
		{
			if (link.IsReconnecting)
				return;

			var (from, to) = (link.GetFromSocket(_behavior), link.GetToSocket(_behavior));
			from?.Disconnect(to);
			to?.Disconnect(from);

			link.IsConnected = false;
		}

		bool Connect(TLink link, bool checkTypes = false)
		{
			if (link.IsReconnecting)
				return true;

			try
			{
				link.IsReconnecting = true;
				link.IsConnected = true;

				var (from, to) = (link.GetFromSocket(_behavior), link.GetToSocket(_behavior));

				if (from == null || to == null || (checkTypes && !from.CanConnect(to)))
					return false;

				from.Connect(to);
				to.Connect(from);

				link.FromPort = from.Id;
				link.ToPort = to.Id;
			}
			finally
			{
				link.IsReconnecting = false;
			}

			return true;
		}

		var changed = false;

		switch (t.change)
		{
			case ModelChange.AddedNode:
				OnElementAdded((TNode)t.data);
				changed = true;
				break;

			case ModelChange.RemovedNode:
				OnElementRemoved((TNode)t.data);
				changed = true;
				break;

			case ModelChange.ChangedNodesSource:
			{
				var oldValues = (IEnumerable<TNode>)t.oldValue;
				oldValues?.ForEach(e => OnElementAdded(e));

				var newValues = (IEnumerable<TNode>)t.newValue;
				newValues?.ForEach(e => OnElementAdded(e));

				changed = true;
				break;
			}

			case ModelChange.AddedLink:
				Connect((TLink)t.data);
				changed = true;
				break;

			case ModelChange.RemovedLink:
				Disconnect((TLink)t.data);
				changed = true;
				break;

			case ModelChange.ChangedLinkToPort:
			{
				var newLink = (TLink)t.data;

				var oldLink = newLink.TypedClone();
				oldLink.To = (string)t.oldValue;
				oldLink.ToPort = (string)t.oldParam;

				Disconnect(oldLink);
				Connect(newLink);

				changed = true;
				break;
			}

			case ModelChange.ChangedLinkFromPort:
			{
				var newLink = (TLink)t.data;

				var oldLink = newLink.TypedClone();
				oldLink.From = (string)t.oldValue;
				oldLink.FromPort = (string)t.oldParam;

				Disconnect(oldLink);
				Connect(newLink);

				changed = true;
				break;
			}

			case ModelChange.ChangedLinksSource:
			{
				var oldValues = ((IEnumerable<TLink>)t.oldValue)?.ToArray();
				oldValues?.ForEach(l => Disconnect(l));

				var newValues = ((IEnumerable<TLink>)t.newValue)?.ToArray();
				newValues?.ForEach(l => Connect(l));

				changed = true;
				break;
			}

			case ModelChange.FinishedRedo:
			case ModelChange.FinishedUndo:
				changed = true;
				break;

			case ModelChange.Property:
			{
				if (!t.propertyName.IsEmpty() && !t.propertyName.EqualsIgnoreCase(nameof(Modifiable)))
					changed = true;

				break;
			}
		}

		if (changed)
			ModelChanged?.Invoke();
	}

	private void OnElementAdded(TNode baseElement)
	{
		if (baseElement.Element == null)
			return;

		var element = baseElement.Element;

		element.StartedUndoableOperation += OnStartedUndoableOperation;
		element.CommittedUndoableOperation += OnCommittedUndoableOperation;
		element.SocketAdded += OnElementSocketAdded;
		element.SocketRemoved += OnElementSocketRemoved;
		element.SocketChanged += OnElementSocketChanged;

		ElementAdded?.Invoke(element);
	}

	private void OnElementRemoved(TNode baseElement)
	{
		if (baseElement.Element == null)
			return;

		var element = baseElement.Element;

		element.StartedUndoableOperation -= OnStartedUndoableOperation;
		element.CommittedUndoableOperation -= OnCommittedUndoableOperation;
		element.SocketAdded -= OnElementSocketAdded;
		element.SocketRemoved -= OnElementSocketRemoved;
		element.SocketChanged -= OnElementSocketChanged;

		ElementRemoved?.Invoke(element);
	}

	private void OnElementSocketAdded(DiagramSocket socket)
	{
		var node = Nodes.FirstOrDefault(n => n.Element == socket.Parent);

		if (node == null)
			return;

		foreach (var link in GetLinks(node, socket))
		{
			if (!IsConnected(link))
				_behavior.RemoveLink(link);
		}

		_behavior.RaiseSocketAdded(node);
	}

	private void OnElementSocketRemoved(DiagramSocket socket)
	{
		RemoveSocketLinks(socket, false);
	}

	private void OnElementSocketChanged(DiagramSocket socket)
	{
		RemoveSocketLinks(socket, true);
	}

	private void RemoveSocketLinks(DiagramSocket socket, bool checkCanConnect)
	{
		var node = Nodes.FirstOrDefault(n => n.Element == socket.Parent);

		if (node == null)
			return;

		GetLinks(node, socket)
			.Where(l => !checkCanConnect || !IsConnected(l) || !CanConnect(l))
			.ForEach(_behavior.RemoveLink);

		_behavior.RaiseLinksRemoved(node);
	}

	private bool CanConnect(ICompositionModelLink link)
	{
		var (from, to) = (link.GetFromSocket(_behavior), link.GetToSocket(_behavior));
		return from != null && to != null && from.CanConnect(to);
	}

	private bool IsConnected(ICompositionModelLink link)
		=> link.GetFromSocket(_behavior) != null && link.GetToSocket(_behavior) != null;

	private bool HasUndoManager => UndoManager is not null;

	private void OnStartedUndoableOperation()
	{
		if (HasUndoManager)
			_behavior.StartTransaction(DiagramConstants.ElementDataName);
	}

	private void OnCommittedUndoableOperation(DiagramElement element, IUndoableEdit op)
	{
		if (!HasUndoManager)
			return;

		var node = Nodes.FirstOrDefault(n => n.Element == element);

		if (node != null)
		{
			_behavior.RaiseCommited(DiagramConstants.ElementDataName, node, op);
		}

		_behavior.CommitTransaction(DiagramConstants.ElementDataName);
	}

	private IEnumerable<TLink> GetLinks(TNode node, DiagramSocket socket)
	{
		var links = _behavior.GetLinksForNode(node)
			.Where(l => socket.IsInput ? l.To == node.Key && l.ToPort.EqualsIgnoreCase(socket.Id) : l.From == node.Key && l.FromPort.EqualsIgnoreCase(socket.Id))
			.ToArray();

		return links;
	}

	private IEnumerable<TLink> GetLinks(DiagramElement element, DiagramSocket socket) => GetLinks(GetNode(element), socket);

	IEnumerable<DiagramSocket> ICompositionModel.GetConnectedSocketsFor(DiagramElement element, DiagramSocket socket)
	{
		var links = GetLinks(element, socket);

		return socket.IsInput ? links.Select(l => l.GetFromSocket(_behavior)) : links.Select(l => l.GetToSocket(_behavior));
	}

	void ICompositionModel.AddElement(DiagramElement element, PointF location)
	{
		if (element == null)
			throw new ArgumentNullException(nameof(element));

		ExecuteTransaction(_transactionName, m => _behavior.AddNode(new() { Element = element, Location = location }));
	}

	/// <summary>
	/// Add node.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	public void AddNode(TNode node) => _behavior.AddNode(node);

	/// <summary>
	/// Remove node.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	public void RemoveNode(TNode node) => _behavior.RemoveNode(node);

	/// <summary>
	/// Add link.
	/// </summary>
	/// <param name="from">From node.</param>
	/// <param name="fromPort"><see cref="ICompositionModelLink.FromPort"/></param>
	/// <param name="to">To node.</param>
	/// <param name="toPort"><see cref="ICompositionModelLink.ToPort"/></param>
	public void AddLink(TNode from, string fromPort, TNode to, string toPort)
		=> _behavior.AddLink(from, fromPort, to, toPort);

	/// <summary>
	/// Remove link.
	/// </summary>
	/// <param name="from">From node.</param>
	/// <param name="fromPort"><see cref="ICompositionModelLink.FromPort"/></param>
	/// <param name="to">To node.</param>
	/// <param name="toPort"><see cref="ICompositionModelLink.ToPort"/></param>
	public void RemoveLink(TNode from, string fromPort, TNode to, string toPort)
		=> _behavior.RemoveLink(from, fromPort, to, toPort);

	//public void RemoveElement(DiagramElement element)
	//{
	//	if (element == null)
	//		throw new ArgumentNullException(nameof(element));

	//	ExecuteTransaction(_transactionName, m =>
	//	{
	//		var tmp = GetNode(element);

	//		if (tmp != null)
	//			_behavior.RemoveNode(tmp);
	//	});
	//}

	IEnumerable<(string nodeKey, DiagramSocket socket)> ICompositionModel.GetDisconnectedSockets()
	{
		var sockets = new List<(string nodeKey, DiagramSocket socket)>();

		foreach (var n in Nodes)
		{
			var node = n;

			if (node.Element == null || !node.Element.ShowSockets)
				continue;

			var ports = _behavior.GetLinksForNode(node)
				.Select(l => l.From == node.Key ? l.FromPort : l.ToPort)
				.ToArray();

			node.Element
				.GetAllSockets()
				.Where(s => ports.All(p => !p.EqualsIgnoreCase(s.Id)))
				.Select(s =>
				{
					var socket = new DiagramSocket(s.Directon, s.Id)
					{
						Name = $"{s.Name} - {node.Element.Name}",
						Type = s.Type,
						LinkableMaximum = s.LinkableMaximum,
					};
					return (node.Key, socket);
				})
				.ForEach(sockets.Add);
		}

		return sockets;
	}

	//private void Replace(IEnumerable<DiagramElement> elements, DiagramElement element)
	//{
	//	ExecuteTransaction(_transactionName, m =>
	//	{
	//		_behavior.AddNode(new() { Element = element });

	//		var elementNode = Nodes.First(e => e.Element == element);
	//		var nodes = Nodes.Where(e => elements.Any(t => t == e.Element)).ToArray();

	//		var nodeLinks = nodes
	//			.Select(node => new
	//			{
	//				Node = node,
	//				InputLinks = _behavior.GetLinksForNode(node).Where(l => l.To == node.Key && nodes.All(n => n.Key != l.From)).ToArray(),
	//				OutputLinks = _behavior.GetLinksForNode(node).Where(l => l.From == node.Key && nodes.All(n => n.Key != l.To)).ToArray()
	//			})
	//			.ToArray();

	//		nodes.ForEach(_behavior.RemoveNode);

	//		var allNodes = Nodes.ToDictionary(s => s.Key);

	//		foreach (var nodeLink in nodeLinks)
	//		{
	//			nodeLink.InputLinks.ForEach(l => _behavior.AddLink(allNodes[l.From], l.FromPort, elementNode, l.ToPort));
	//			nodeLink.OutputLinks.ForEach(l => _behavior.AddLink(elementNode, l.FromPort, allNodes[l.To], l.ToPort));
	//		}
	//	});
	//}

	/// <summary>
	/// Execute the specified action in transaction scope.
	/// </summary>
	/// <param name="name">Transaction name.</param>
	/// <param name="action">Action.</param>
	public void ExecuteTransaction(string name, Action<CompositionModel<TNode, TLink>> action)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		if (action is null)
			throw new ArgumentNullException(nameof(action));

		_behavior.StartTransaction(name);

		try
		{
			action(this);
			_behavior.CommitTransaction(name);
		}
		catch (Exception)
		{
			_behavior.RollbackTransaction();
			throw;
		}
	}

	private CompositionModel<TNode, TLink> Clone()
	{
		var model = new CompositionModel<TNode, TLink>((ICompositionModelBehavior<TNode, TLink>)_behavior.Clone());

		model.ExecuteTransaction("Clone", m =>
		{
			m.Nodes = new ObservableCollection<TNode>(Nodes.Select(n => (TNode)n.Clone()));
			m.Links = new ObservableCollection<TLink>(Links.Select(l => l.TypedClone()));
		});

		return model;
	}

	ICompositionModel ICloneable<ICompositionModel>.Clone() => Clone();
	object ICloneable.Clone() => Clone();
}
