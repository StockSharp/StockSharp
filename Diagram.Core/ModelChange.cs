namespace StockSharp.Diagram;

/// <summary>
/// An enumeration of the predefined ways in which models may be changed.
/// </summary>
public enum ModelChange
{
	/// <summary>
	/// Changes to data properties and extended model properties, and other non-predefined property changes
	/// </summary>
	Property,
	/// <summary>
	/// Changed (replaced) the NodesSource collection property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedNodesSource,
	/// <summary>
	/// Changed the NodeKeyPath property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedNodeKeyPath,
	/// <summary>
	/// Changed the NodeCategoryPath property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedNodeCategoryPath,
	/// <summary>
	/// Changed the NodeIsGroupPath property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedNodeIsGroupPath,
	/// <summary>
	/// Changed the GroupNodePath property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedGroupNodePath,
	/// <summary>
	/// Changed the MemberNodesPath property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedMemberNodesPath,
	/// <summary>
	/// Changed the NodeIsLinkLabelPath property (GraphLinksModel)
	/// </summary>
	ChangedNodeIsLinkLabelPath,
	/// <summary>
	/// Changed (replaced) the LinksSource collection property (GraphLinksModel)
	/// </summary>
	ChangedLinksSource,
	/// <summary>
	/// Changed the LinkFromPath property (GraphLinksModel)
	/// </summary>
	ChangedLinkFromPath,
	/// <summary>
	/// Changed the LinkToPath property (GraphLinksModel)
	/// </summary>
	ChangedLinkToPath,
	/// <summary>
	/// Changed the FromNodesPath property (GraphModel)
	/// </summary>
	ChangedFromNodesPath,
	/// <summary>
	/// Changed the ToNodesPath property (GraphModel)
	/// </summary>
	ChangedToNodesPath,
	/// <summary>
	/// Changed the LinkLabelNodePath property (GraphLinksModel)
	/// </summary>
	ChangedLinkLabelNodePath,
	/// <summary>
	/// Changed the LinkFromParameterPath property (GraphLinksModel)
	/// </summary>
	ChangedLinkFromParameterPath,
	/// <summary>
	/// Changed the LinkToParameterPath property (GraphLinksModel)
	/// </summary>
	ChangedLinkToParameterPath,
	/// <summary>
	/// Changed the LinkCategoryPath property (GraphLinksModel)
	/// </summary>
	ChangedLinkCategoryPath,
	/// <summary>
	/// Changed the Name property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedName,
	/// <summary>
	/// Changed the DataFormat property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedDataFormat,
	/// <summary>
	/// Changed the Modifiable property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedModifiable,
	/// <summary>
	/// Changed the CopyingGroupCopiesMembers property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedCopyingGroupCopiesMembers,
	/// <summary>
	/// Changed the CopyingLinkCopiesLabel property (GraphLinksModel)
	/// </summary>
	ChangedCopyingLinkCopiesLabel,
	/// <summary>
	/// Changed the RemovingGroupRemovesMembers property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedRemovingGroupRemovesMembers,
	/// <summary>
	/// Changed the RemovingLinkRemovesLabel property (GraphLinksModel)
	/// </summary>
	ChangedRemovingLinkRemovesLabel,
	/// <summary>
	/// Changed the ValidCycle property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedValidCycle,
	/// <summary>
	/// Changed the ValidUnconnectedLinks property (GraphLinksModel)
	/// </summary>
	ChangedValidUnconnectedLinks,
	/// <summary>
	/// Added a node data to NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	AddedNode,
	/// <summary>
	/// About to remove a node data from NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	RemovingNode,
	/// <summary>
	/// Removed a node data from NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	RemovedNode,
	/// <summary>
	/// Changed the node key for a node data (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedNodeKey,
	/// <summary>
	/// Added a link data to LinksSource (GraphLinksModel)
	/// </summary>
	AddedLink,
	/// <summary>
	/// About to remove a link data from LinksSource (GraphLinksModel)
	/// </summary>
	RemovingLink,
	/// <summary>
	/// Removed a link data from LinksSource (GraphLinksModel)
	/// </summary>
	RemovedLink,
	/// <summary>
	/// Changed the LinkFromPort property (GraphLinksModel)
	/// </summary>
	ChangedLinkFromPort,
	/// <summary>
	/// Changed the LinkToPort property (GraphLinksModel)
	/// </summary>
	ChangedLinkToPort,
	/// <summary>
	/// Changed the LinkLabelKey property (GraphLinksModel)
	/// </summary>
	ChangedLinkLabelKey,
	/// <summary>
	/// Changed (replaced) the FromNodeKeys collection property (GraphModel)
	/// </summary>
	ChangedFromNodeKeys,
	/// <summary>
	/// Added a node key to the FromNodeKeys collection property (GraphModel)
	/// </summary>
	AddedFromNodeKey,
	/// <summary>
	/// Removed a node key from the FromNodeKeys collection property (GraphModel)
	/// </summary>
	RemovedFromNodeKey,
	/// <summary>
	/// Changed (replaced) the ToNodeKeys collection property (GraphModel)
	/// </summary>
	ChangedToNodeKeys,
	/// <summary>
	/// Added a node key to the ToNodeKeys collection property (GraphModel)
	/// </summary>
	AddedToNodeKey,
	/// <summary>
	/// Removed a node key from the ToNodeKeys collection property (GraphModel)
	/// </summary>
	RemovedToNodeKey,
	/// <summary>
	/// Changed the GroupNodeKey property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedGroupNodeKey,
	/// <summary>
	/// Changed the LinkGroupKey property (GraphLinksModel)
	/// </summary>
	ChangedLinkGroupNodeKey,
	/// <summary>
	/// Changed (replaced) the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	ChangedMemberNodeKeys,
	/// <summary>
	/// Added a node key to the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	AddedMemberNodeKey,
	/// <summary>
	/// Removed a node key from the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	RemovedMemberNodeKey,
	/// <summary>
	/// Changed the ParentNodeKey property (TreeModel)
	/// </summary>
	ChangedParentNodeKey,
	/// <summary>
	/// Changed (replaced) the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	ChangedChildNodeKeys,
	/// <summary>
	/// Added a node key to the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	AddedChildNodeKey,
	/// <summary>
	/// Removed a node key from the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	RemovedChildNodeKey,
	/// <summary>
	/// Changed the value of the Category for a node data (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	ChangedNodeCategory,
	/// <summary>
	/// Changed the value of the Category for a link data (GraphLinksModel)
	/// </summary>
	ChangedLinkCategory,
	/// <summary>
	/// Started a transaction.
	/// </summary>
	StartedTransaction = -1,
	/// <summary>
	/// Committed the changes for a transaction.
	/// </summary>
	CommittedTransaction = -2,
	/// <summary>
	/// Rolled back the changes of a transaction and aborted it.
	/// </summary>
	RolledBackTransaction = -3,
	/// <summary>
	/// Starting an undo operation.
	/// </summary>
	StartingUndo = -4,
	/// <summary>
	/// Starting a redo operation.
	/// </summary>
	StartingRedo = -5,
	/// <summary>
	/// Finished an undo operation.
	/// </summary>
	FinishedUndo = -13,
	/// <summary>
	/// Finished a redo operation.
	/// </summary>
	FinishedRedo = -14,
	/// <summary>
	/// Changed the shape or bounds of a node or one or more of its ports (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	InvalidateRelationships = -20,
	/// <summary>
	/// (for internal use)
	/// </summary>
	ReplacedReference = -21,
	/// <summary>
	/// Cleared the contents of the UndoManager (for all models).
	/// </summary>
	ClearedUndoManager = -22,
	/// <summary>
	/// A transient data change that should not be considered a change to the model;
	/// such changes are ignored by the UndoManager and by the Diagram.
	/// </summary>
	None = -100
}