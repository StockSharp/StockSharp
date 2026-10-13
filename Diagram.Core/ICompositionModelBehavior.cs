namespace StockSharp.Diagram;

/// <summary>
/// <see cref="ICompositionModel"/> behavior.
/// </summary>
/// <typeparam name="TNode">Node type.</typeparam>
/// <typeparam name="TLink">Link type.</typeparam>
public interface ICompositionModelBehavior<TNode, TLink> : ICloneable
	where TNode : ICompositionModelNode
	where TLink : ICompositionModelLink
{
	/// <summary>
	/// Parent.
	/// </summary>
	ICompositionModel Parent { get; set; }

	/// <summary>
	/// <see cref="IUndoManager"/>
	/// </summary>
	IUndoManager UndoManager { get; set; }

	/// <summary>
	/// Undo manager is suspended if this property is set to true.
	/// </summary>
	bool IsUndoManagerSuspended { get; set; }

	/// <summary>
	/// Is it possible to edit a composite element diagram.
	/// </summary>
	bool Modifiable { get; set; }

	/// <summary>
	/// Nodes.
	/// </summary>
	IEnumerable<TNode> Nodes { get; set; }

	/// <summary>
	/// Links.
	/// </summary>
	IEnumerable<TLink> Links { get; set; }

	/// <summary>
	/// Find node by key.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <returns><typeparamref name="TNode"/></returns>
	TNode FindNodeByKey(string key);

	/// <summary>
	/// Add node.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	void AddNode(TNode node);

	/// <summary>
	/// Remove node.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	void RemoveNode(TNode node);

	/// <summary>
	/// Add link.
	/// </summary>
	/// <param name="link"><typeparamref name="TLink"/></param>
	void AddLink(TLink link);

	/// <summary>
	/// Remove link.
	/// </summary>
	/// <param name="link"><typeparamref name="TLink"/></param>
	void RemoveLink(TLink link);

	/// <summary>
	/// Add link.
	/// </summary>
	/// <param name="from">From node.</param>
	/// <param name="fromPort"><see cref="ICompositionModelLink.FromPort"/></param>
	/// <param name="to">To node.</param>
	/// <param name="toPort"><see cref="ICompositionModelLink.ToPort"/></param>
	/// <returns><typeparamref name="TLink"/></returns>
	TLink AddLink(TNode from, string fromPort, TNode to, string toPort);

	/// <summary>
	/// Remove link.
	/// </summary>
	/// <param name="from">From node.</param>
	/// <param name="fromPort"><see cref="ICompositionModelLink.FromPort"/></param>
	/// <param name="to">To node.</param>
	/// <param name="toPort"><see cref="ICompositionModelLink.ToPort"/></param>
	void RemoveLink(TNode from, string fromPort, TNode to, string toPort);

	/// <summary>
	/// Get all links for the specified node.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	/// <returns>Links.</returns>
	IEnumerable<TLink> GetLinksForNode(TNode node);

	/// <summary>
	/// Start transaction.
	/// </summary>
	/// <param name="name">Operation name.</param>
	/// <returns>Operation result.</returns>
	bool StartTransaction(string name);

	/// <summary>
	/// Commit transaction.
	/// </summary>
	/// <param name="name">Operation name.</param>
	/// <returns>Operation result.</returns>
	bool CommitTransaction(string name);

	/// <summary>
	/// Rollback transaction.
	/// </summary>
	/// <returns>Operation result.</returns>
	bool RollbackTransaction();

	/// <summary>
	/// Changed event.
	/// </summary>
	event Action<(ModelChange change, object data, string propName, object oldValue, object oldParam, object newValue, object newParam)> BehaviorChanged;

	/// <summary>
	/// Raise socket added event.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	void RaiseSocketAdded(TNode node);

	/// <summary>
	/// Raise links removed event.
	/// </summary>
	/// <param name="node"><typeparamref name="TNode"/></param>
	void RaiseLinksRemoved(TNode node);

	/// <summary>
	/// Raise commited event.
	/// </summary>
	/// <param name="name">Opeation name.</param>
	/// <param name="node"><typeparamref name="TNode"/></param>
	/// <param name="op"><see cref="IUndoableEdit"/></param>
	void RaiseCommited(string name, TNode node, IUndoableEdit op);
}

/// <summary>
/// In-memory implementation of <see cref="ICompositionModelBehavior{TNode,TLink}"/>: keeps nodes and links
/// in observable collections and raises <see cref="BehaviorChanged"/> notifications, without any visual or
/// built-in undo backing of its own (an <see cref="IUndoManager"/> can be attached externally). Suitable for
/// headless hosts and for UI front-ends that render the model directly (e.g. the Avalonia diagram editor).
/// </summary>
public class InMemoryCompositionModelBehavior : ICompositionModelBehavior<InMemoryCompositionModelNode, InMemoryCompositionModelLink>
{
	/// <inheritdoc/>
	public ICompositionModel Parent { get; set; }
	/// <inheritdoc/>
	public IUndoManager UndoManager { get; set; }
	/// <inheritdoc/>
	public bool IsUndoManagerSuspended { get; set; }

	private bool _modifiable;

	/// <inheritdoc/>
	public bool Modifiable
	{
		get => _modifiable;
		set
		{
			if (_modifiable != value)
			{
				_modifiable = value;
				BehaviorChanged?.Invoke((ModelChange.Property, nameof(Modifiable), default, !value, default, value, default));
			}
		}
	}

	private IEnumerable<InMemoryCompositionModelNode> _nodes = new ObservableCollection<InMemoryCompositionModelNode>();

	/// <inheritdoc/>
	public IEnumerable<InMemoryCompositionModelNode> Nodes
	{
		get => _nodes;
		set
		{
			var prev = _nodes;
			_nodes = value;
			BehaviorChanged?.Invoke((ModelChange.ChangedNodesSource, default, default, prev, default, value, default));
		}
	}

	private IEnumerable<InMemoryCompositionModelLink> _links = new ObservableCollection<InMemoryCompositionModelLink>();

	/// <inheritdoc/>
	public IEnumerable<InMemoryCompositionModelLink> Links
	{
		get => _links;
		set
		{
			var prev = _links;
			_links = value;
			BehaviorChanged?.Invoke((ModelChange.ChangedLinksSource, default, default, prev, default, value, default));
		}
	}

	/// <inheritdoc/>
	public event Action<(ModelChange change, object data, string propName, object oldValue, object oldParam, object newValue, object newParam)> BehaviorChanged;

	/// <inheritdoc/>
	public InMemoryCompositionModelNode FindNodeByKey(string key)
		=> Nodes.FirstOrDefault(n => n.Key == key);

	/// <inheritdoc/>
	public IEnumerable<InMemoryCompositionModelLink> GetLinksForNode(InMemoryCompositionModelNode node)
		=> Links.Where(l => l.From == node.Key || l.To == node.Key);

	/// <inheritdoc/>
	public void RaiseCommited(string name, InMemoryCompositionModelNode node, IUndoableEdit op)
		=> BehaviorChanged?.Invoke((ModelChange.Property, node, name, op, default, default, default));
	/// <inheritdoc/>
	public void RaiseLinksRemoved(InMemoryCompositionModelNode node)
		=> BehaviorChanged?.Invoke((ModelChange.InvalidateRelationships, node, default, default, default, default, default));
	/// <inheritdoc/>
	public void RaiseSocketAdded(InMemoryCompositionModelNode node)
		=> BehaviorChanged?.Invoke((ModelChange.InvalidateRelationships, node, default, default, default, default, default));

	/// <inheritdoc/>
	public void AddLink(InMemoryCompositionModelLink link)
	{
		((IList<InMemoryCompositionModelLink>)Links).Add(link);
		BehaviorChanged?.Invoke((ModelChange.AddedLink, link, default, default, default, default, default));
	}
	/// <inheritdoc/>
	public void RemoveLink(InMemoryCompositionModelLink link)
	{
		((IList<InMemoryCompositionModelLink>)Links).Remove(link);
		BehaviorChanged?.Invoke((ModelChange.RemovedLink, link, default, default, default, default, default));
	}

	/// <inheritdoc/>
	public InMemoryCompositionModelLink AddLink(InMemoryCompositionModelNode from, string fromPort, InMemoryCompositionModelNode to, string toPort)
	{
		var link = new InMemoryCompositionModelLink
		{
			From = from.Key,
			FromPort = fromPort,
			To = to.Key,
			ToPort = toPort
		};
		AddLink(link);
		return link;
	}
	/// <inheritdoc/>
	public void RemoveLink(InMemoryCompositionModelNode from, string fromPort, InMemoryCompositionModelNode to, string toPort)
	{
		var links = ((IList<InMemoryCompositionModelLink>)Links).RemoveWhere(e => e.From == from.Key && e.FromPort == fromPort && e.To == to.Key && e.ToPort == toPort);

		foreach (var link in links)
			BehaviorChanged?.Invoke((ModelChange.RemovedLink, link, default, default, default, default, default));
	}

	/// <inheritdoc/>
	public void AddNode(InMemoryCompositionModelNode node)
	{
		// A freshly added node (e.g. from the palette) carries no key; consumers index nodes/links and render
		// by key, so assign a unique one. A key supplied by deserialization is preserved.
		if (node.Key.IsEmpty())
			node.Key = Guid.NewGuid().To<string>();

		((IList<InMemoryCompositionModelNode>)Nodes).Add(node);
		BehaviorChanged?.Invoke((ModelChange.AddedNode, node, default, default, default, default, default));
	}
	/// <inheritdoc/>
	public void RemoveNode(InMemoryCompositionModelNode node)
	{
		((IList<InMemoryCompositionModelNode>)Nodes).Remove(node);
		BehaviorChanged?.Invoke((ModelChange.RemovedNode, node, default, default, default, default, default));
	}

	/// <inheritdoc/>
	public bool StartTransaction(string name)
	{
		BehaviorChanged?.Invoke((ModelChange.StartedTransaction, name, default, default, default, default, default));
		return true;
	}
	/// <inheritdoc/>
	public bool RollbackTransaction()
	{
		BehaviorChanged?.Invoke((ModelChange.RolledBackTransaction, default, default, default, default, default, default));
		return true;
	}
	/// <inheritdoc/>
	public bool CommitTransaction(string name)
	{
		BehaviorChanged?.Invoke((ModelChange.CommittedTransaction, name, default, default, default, default, default));
		return true;
	}

	object ICloneable.Clone() => new InMemoryCompositionModelBehavior();
}