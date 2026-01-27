namespace StockSharp.Algo.Basket;

/// <summary>
/// Maps parent subscription IDs to child subscription IDs (for multi-adapter routing).
/// </summary>
public interface IParentChildMap
{
	/// <summary>
	/// Add parent-child mapping.
	/// </summary>
	/// <param name="childId">Child subscription ID.</param>
	/// <param name="parentMsg">Parent subscription message.</param>
	/// <param name="adapter">The adapter assigned to this child.</param>
	void AddMapping(long childId, ISubscriptionMessage parentMsg, IMessageAdapter adapter);

	/// <summary>
	/// Get all active child subscriptions for a parent.
	/// </summary>
	/// <param name="parentId">Parent subscription ID.</param>
	/// <returns>Dictionary of childId → adapter.</returns>
	IDictionary<long, IMessageAdapter> GetChild(long parentId);

	/// <summary>
	/// Process a child subscription response.
	/// </summary>
	/// <param name="childId">Child subscription ID.</param>
	/// <param name="error">Error if any.</param>
	/// <param name="needParentResponse">Whether a response should be sent for the parent.</param>
	/// <param name="allError">Whether all children have errors.</param>
	/// <param name="innerErrors">All collected errors.</param>
	/// <returns>Parent subscription ID, or null if not found.</returns>
	long? ProcessChildResponse(long childId, Exception error, out bool needParentResponse, out bool allError, out IEnumerable<Exception> innerErrors);

	/// <summary>
	/// Process a child subscription finish.
	/// </summary>
	/// <param name="childId">Child subscription ID.</param>
	/// <param name="needParentResponse">Whether a response should be sent for the parent.</param>
	/// <returns>Parent subscription ID, or null if not found.</returns>
	long? ProcessChildFinish(long childId, out bool needParentResponse);

	/// <summary>
	/// Process a child subscription online notification.
	/// </summary>
	/// <param name="childId">Child subscription ID.</param>
	/// <param name="needParentResponse">Whether a response should be sent for the parent.</param>
	/// <returns>Parent subscription ID, or null if not found.</returns>
	long? ProcessChildOnline(long childId, out bool needParentResponse);

	/// <summary>
	/// Try get parent ID for a child subscription.
	/// </summary>
	/// <param name="childId">Child subscription ID.</param>
	/// <param name="parentId">Parent subscription ID if found.</param>
	/// <returns><see langword="true"/> if found.</returns>
	bool TryGetParent(long childId, out long parentId);

	/// <summary>
	/// Clear all mappings.
	/// </summary>
	void Clear();
}
