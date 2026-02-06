namespace StockSharp.Algo.Basket;

/// <summary>
/// Default implementation of <see cref="IParentChildMap"/>.
/// Maps parent subscription IDs to child subscription IDs.
/// </summary>
public class ParentChildMap : IParentChildMap
{
	private readonly Lock _syncObject = new();
	private readonly Dictionary<long, RefQuadruple<long, SubscriptionStates, IMessageAdapter, Exception>> _childToParentIds = [];

	/// <inheritdoc />
	public void AddMapping(long childId, ISubscriptionMessage parentMsg, IMessageAdapter adapter)
	{
		if (childId <= 0)
			throw new ArgumentOutOfRangeException(nameof(childId));

		if (parentMsg == null)
			throw new ArgumentNullException(nameof(parentMsg));

		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		using (_syncObject.EnterScope())
			_childToParentIds.Add(childId, RefTuple.Create(parentMsg.TransactionId, SubscriptionStates.Stopped, adapter, default(Exception)));
	}

	/// <inheritdoc />
	public IDictionary<long, IMessageAdapter> GetChild(long parentId)
	{
		if (parentId <= 0)
			throw new ArgumentOutOfRangeException(nameof(parentId));

		using (_syncObject.EnterScope())
			return FilterByParent(parentId).Where(p => p.Value.Second.IsActive()).ToDictionary(p => p.Key, p => p.Value.Third);
	}

	private IEnumerable<KeyValuePair<long, RefQuadruple<long, SubscriptionStates, IMessageAdapter, Exception>>> FilterByParent(long parentId)
		=> _childToParentIds.Where(p => p.Value.First == parentId);

	/// <inheritdoc />
	public long? ProcessChildResponse(long childId, Exception error, out bool needParentResponse, out bool allError, out IEnumerable<Exception> innerErrors)
	{
		allError = true;
		needParentResponse = true;
		innerErrors = [];

		if (childId == 0)
			return null;

		using (_syncObject.EnterScope())
		{
			if (!_childToParentIds.TryGetValue(childId, out var tuple))
				return null;

			var parentId = tuple.First;
			tuple.Second = error == null ? SubscriptionStates.Active : SubscriptionStates.Error;
			tuple.Fourth = error;

			var errors = new List<Exception>();

			foreach (var pair in FilterByParent(parentId))
			{
				var t = pair.Value;

				// one of adapter still not yet response.
				if (t.Second == SubscriptionStates.Stopped)
				{
					needParentResponse = false;
					break;
				}

				if (t.Second != SubscriptionStates.Error)
					allError = false;
				else if (t.Fourth != null)
					errors.Add(t.Fourth);
			}

			innerErrors = errors;
			return parentId;
		}
	}

	/// <inheritdoc />
	public long? ProcessChildFinish(long childId, out bool needParentResponse)
		=> ProcessChild(childId, SubscriptionStates.Finished, out needParentResponse);

	/// <inheritdoc />
	public long? ProcessChildOnline(long childId, out bool needParentResponse)
		=> ProcessChild(childId, SubscriptionStates.Online, out needParentResponse);

	private long? ProcessChild(long childId, SubscriptionStates state, out bool needParentResponse)
	{
		needParentResponse = true;

		using (_syncObject.EnterScope())
		{
			if (!_childToParentIds.TryGetValue(childId, out var tuple))
				return null;

			var parentId = tuple.First;
			tuple.Second = state;

			foreach (var pair in FilterByParent(parentId))
			{
				var t = pair.Value;

				if (t.Second != SubscriptionStates.Error && t.Second != state)
				{
					needParentResponse = false;
					break;
				}
			}

			return parentId;
		}
	}

	/// <inheritdoc />
	public bool TryGetParent(long childId, out long parentId)
	{
		parentId = default;

		using (_syncObject.EnterScope())
		{
			if (!_childToParentIds.TryGetValue(childId, out var t))
				return false;

			parentId = t.First;
			return true;
		}
	}

	/// <inheritdoc />
	public bool RemoveMapping(long childId)
	{
		using (_syncObject.EnterScope())
			return _childToParentIds.Remove(childId);
	}

	/// <inheritdoc />
	public void Clear()
	{
		using (_syncObject.EnterScope())
			_childToParentIds.Clear();
	}
}
