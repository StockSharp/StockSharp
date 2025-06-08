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
/// Dummy implementation of <see cref="ICompositionModelBehavior{TNode,TLink}"/>.
/// </summary>
public class DummyCompositionModelBehavior : ICompositionModelBehavior<DummyCompositionModelNode, DummyCompositionModelLink>
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

	private IEnumerable<DummyCompositionModelNode> _nodes = new ObservableCollection<DummyCompositionModelNode>();

	/// <inheritdoc/>
	public IEnumerable<DummyCompositionModelNode> Nodes
	{
		get => _nodes;
		set
		{
			var prev = _nodes;
			_nodes = value;
			BehaviorChanged?.Invoke((ModelChange.ChangedNodesSource, default, default, prev, default, value, default));
		}
	}

	private IEnumerable<DummyCompositionModelLink> _links = new ObservableCollection<DummyCompositionModelLink>();

	/// <inheritdoc/>
	public IEnumerable<DummyCompositionModelLink> Links
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
	public DummyCompositionModelNode FindNodeByKey(string key)
		=> Nodes.FirstOrDefault(n => n.Key == key);

	/// <inheritdoc/>
	public IEnumerable<DummyCompositionModelLink> GetLinksForNode(DummyCompositionModelNode node)
		=> Links.Where(l => l.From == node.Key || l.To == node.Key);

	/// <inheritdoc/>
	public void RaiseCommited(string name, DummyCompositionModelNode node, IUndoableEdit op) => throw new NotSupportedException();
	/// <inheritdoc/>
	public void RaiseLinksRemoved(DummyCompositionModelNode node) => throw new NotSupportedException();
	/// <inheritdoc/>
	public void RaiseSocketAdded(DummyCompositionModelNode node) => throw new NotSupportedException();

	/// <inheritdoc/>
	public void AddLink(DummyCompositionModelLink link)
	{
		((IList<DummyCompositionModelLink>)Links).Add(link);
		BehaviorChanged?.Invoke((ModelChange.AddedLink, link, default, default, default, default, default));
	}
	/// <inheritdoc/>
	public void RemoveLink(DummyCompositionModelLink link)
	{
		((IList<DummyCompositionModelLink>)Links).Remove(link);
		BehaviorChanged?.Invoke((ModelChange.RemovedLink, link, default, default, default, default, default));
	}

	/// <inheritdoc/>
	public DummyCompositionModelLink AddLink(DummyCompositionModelNode from, string fromPort, DummyCompositionModelNode to, string toPort)
	{
		var link = new DummyCompositionModelLink
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
	public void RemoveLink(DummyCompositionModelNode from, string fromPort, DummyCompositionModelNode to, string toPort)
	{
		var links = ((IList<DummyCompositionModelLink>)Links).RemoveWhere(e => e.From == from.Key && e.FromPort == fromPort && e.To == to.Key && e.ToPort == toPort);

		foreach (var link in links)
			BehaviorChanged?.Invoke((ModelChange.RemovedLink, link, default, default, default, default, default));
	}

	/// <inheritdoc/>
	public void AddNode(DummyCompositionModelNode node)
	{
		((IList<DummyCompositionModelNode>)Nodes).Add(node);
		BehaviorChanged?.Invoke((ModelChange.AddedNode, node, default, default, default, default, default));
	}
	/// <inheritdoc/>
	public void RemoveNode(DummyCompositionModelNode node)
	{
		((IList<DummyCompositionModelNode>)Nodes).Remove(node);
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

	object ICloneable.Clone() => new DummyCompositionModelBehavior();
}