namespace StockSharp.Diagram;

/// <summary>
/// An enumeration of the predefined ways in which models may be changed.
/// </summary>
public enum ModelChange
{
	/// <summary>
	/// Changes to data properties and extended model properties, and other non-predefined property changes
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PropertyKey)]
	Property,
	/// <summary>
	/// Changed (replaced) the NodesSource collection property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodesSourceKey)]
	ChangedNodesSource,
	/// <summary>
	/// Changed the NodeKeyPath property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeKeyPathKey)]
	ChangedNodeKeyPath,
	/// <summary>
	/// Changed the NodeCategoryPath property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeCategoryPathKey)]
	ChangedNodeCategoryPath,
	/// <summary>
	/// Changed the NodeIsGroupPath property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeIsGroupPathKey)]
	ChangedNodeIsGroupPath,
	/// <summary>
	/// Changed the GroupNodePath property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedGroupNodePathKey)]
	ChangedGroupNodePath,
	/// <summary>
	/// Changed the MemberNodesPath property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedMemberNodesPathKey)]
	ChangedMemberNodesPath,
	/// <summary>
	/// Changed the NodeIsLinkLabelPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeIsLinkLabelPathKey)]
	ChangedNodeIsLinkLabelPath,
	/// <summary>
	/// Changed (replaced) the LinksSource collection property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinksSourceKey)]
	ChangedLinksSource,
	/// <summary>
	/// Changed the LinkFromPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkFromPathKey)]
	ChangedLinkFromPath,
	/// <summary>
	/// Changed the LinkToPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkToPathKey)]
	ChangedLinkToPath,
	/// <summary>
	/// Changed the FromNodesPath property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedFromNodesPathKey)]
	ChangedFromNodesPath,
	/// <summary>
	/// Changed the ToNodesPath property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedToNodesPathKey)]
	ChangedToNodesPath,
	/// <summary>
	/// Changed the LinkLabelNodePath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkLabelNodePathKey)]
	ChangedLinkLabelNodePath,
	/// <summary>
	/// Changed the LinkFromParameterPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkFromParameterPathKey)]
	ChangedLinkFromParameterPath,
	/// <summary>
	/// Changed the LinkToParameterPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkToParameterPathKey)]
	ChangedLinkToParameterPath,
	/// <summary>
	/// Changed the LinkCategoryPath property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkCategoryPathKey)]
	ChangedLinkCategoryPath,
	/// <summary>
	/// Changed the Name property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNameKey)]
	ChangedName,
	/// <summary>
	/// Changed the DataFormat property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedDataFormatKey)]
	ChangedDataFormat,
	/// <summary>
	/// Changed the Modifiable property (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedModifiableKey)]
	ChangedModifiable,
	/// <summary>
	/// Changed the CopyingGroupCopiesMembers property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedCopyingGroupCopiesMembersKey)]
	ChangedCopyingGroupCopiesMembers,
	/// <summary>
	/// Changed the CopyingLinkCopiesLabel property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedCopyingLinkCopiesLabelKey)]
	ChangedCopyingLinkCopiesLabel,
	/// <summary>
	/// Changed the RemovingGroupRemovesMembers property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedRemovingGroupRemovesMembersKey)]
	ChangedRemovingGroupRemovesMembers,
	/// <summary>
	/// Changed the RemovingLinkRemovesLabel property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedRemovingLinkRemovesLabelKey)]
	ChangedRemovingLinkRemovesLabel,
	/// <summary>
	/// Changed the ValidCycle property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedValidCycleKey)]
	ChangedValidCycle,
	/// <summary>
	/// Changed the ValidUnconnectedLinks property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedValidUnconnectedLinksKey)]
	ChangedValidUnconnectedLinks,
	/// <summary>
	/// Added a node data to NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedNodeKey)]
	AddedNode,
	/// <summary>
	/// About to remove a node data from NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovingNodeKey)]
	RemovingNode,
	/// <summary>
	/// Removed a node data from NodesSource (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedNodeKey)]
	RemovedNode,
	/// <summary>
	/// Changed the node key for a node data (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeKeyKey)]
	ChangedNodeKey,
	/// <summary>
	/// Added a link data to LinksSource (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedLinkKey)]
	AddedLink,
	/// <summary>
	/// About to remove a link data from LinksSource (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovingLinkKey)]
	RemovingLink,
	/// <summary>
	/// Removed a link data from LinksSource (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedLinkKey)]
	RemovedLink,
	/// <summary>
	/// Changed the LinkFromPort property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkFromPortKey)]
	ChangedLinkFromPort,
	/// <summary>
	/// Changed the LinkToPort property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkToPortKey)]
	ChangedLinkToPort,
	/// <summary>
	/// Changed the LinkLabelKey property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkLabelKeyKey)]
	ChangedLinkLabelKey,
	/// <summary>
	/// Changed (replaced) the FromNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedFromNodeKeysKey)]
	ChangedFromNodeKeys,
	/// <summary>
	/// Added a node key to the FromNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedFromNodeKeyKey)]
	AddedFromNodeKey,
	/// <summary>
	/// Removed a node key from the FromNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedFromNodeKeyKey)]
	RemovedFromNodeKey,
	/// <summary>
	/// Changed (replaced) the ToNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedToNodeKeysKey)]
	ChangedToNodeKeys,
	/// <summary>
	/// Added a node key to the ToNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedToNodeKeyKey)]
	AddedToNodeKey,
	/// <summary>
	/// Removed a node key from the ToNodeKeys collection property (GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedToNodeKeyKey)]
	RemovedToNodeKey,
	/// <summary>
	/// Changed the GroupNodeKey property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedGroupNodeKeyKey)]
	ChangedGroupNodeKey,
	/// <summary>
	/// Changed the LinkGroupKey property (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkGroupNodeKeyKey)]
	ChangedLinkGroupNodeKey,
	/// <summary>
	/// Changed (replaced) the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedMemberNodeKeysKey)]
	ChangedMemberNodeKeys,
	/// <summary>
	/// Added a node key to the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedMemberNodeKeyKey)]
	AddedMemberNodeKey,
	/// <summary>
	/// Removed a node key from the MemberNodeKeys collection property (GraphLinksModel, GraphModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedMemberNodeKeyKey)]
	RemovedMemberNodeKey,
	/// <summary>
	/// Changed the ParentNodeKey property (TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedParentNodeKeyKey)]
	ChangedParentNodeKey,
	/// <summary>
	/// Changed (replaced) the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedChildNodeKeysKey)]
	ChangedChildNodeKeys,
	/// <summary>
	/// Added a node key to the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeAddedChildNodeKeyKey)]
	AddedChildNodeKey,
	/// <summary>
	/// Removed a node key from the ChildNodeKeys collection property (TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRemovedChildNodeKeyKey)]
	RemovedChildNodeKey,
	/// <summary>
	/// Changed the value of the Category for a node data (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedNodeCategoryKey)]
	ChangedNodeCategory,
	/// <summary>
	/// Changed the value of the Category for a link data (GraphLinksModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeChangedLinkCategoryKey)]
	ChangedLinkCategory,
	/// <summary>
	/// Started a transaction.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeStartedTransactionKey)]
	StartedTransaction = -1,
	/// <summary>
	/// Committed the changes for a transaction.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeCommittedTransactionKey)]
	CommittedTransaction = -2,
	/// <summary>
	/// Rolled back the changes of a transaction and aborted it.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeRolledBackTransactionKey)]
	RolledBackTransaction = -3,
	/// <summary>
	/// Starting an undo operation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeStartingUndoKey)]
	StartingUndo = -4,
	/// <summary>
	/// Starting a redo operation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeStartingRedoKey)]
	StartingRedo = -5,
	/// <summary>
	/// Finished an undo operation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeFinishedUndoKey)]
	FinishedUndo = -13,
	/// <summary>
	/// Finished a redo operation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeFinishedRedoKey)]
	FinishedRedo = -14,
	/// <summary>
	/// Changed the shape or bounds of a node or one or more of its ports (GraphLinksModel, GraphModel, TreeModel)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeInvalidateRelationshipsKey)]
	InvalidateRelationships = -20,
	/// <summary>
	/// (for internal use)
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeReplacedReferenceKey)]
	ReplacedReference = -21,
	/// <summary>
	/// Cleared the contents of the UndoManager (for all models).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ModelChangeClearedUndoManagerKey)]
	ClearedUndoManager = -22,
	/// <summary>
	/// A transient data change that should not be considered a change to the model;
	/// such changes are ignored by the UndoManager and by the Diagram.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoneKey)]
	None = -100
}