namespace StockSharp.Algo;

/// <summary>
/// The supplier of information on instruments, getting data from the collection.
/// </summary>
public class CollectionSecurityProvider : ISecurityProvider
{
	private readonly SynchronizedDictionary<SecurityId, Security> _inner = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
	/// </summary>
	public CollectionSecurityProvider()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
	/// </summary>
	/// <param name="securities">The instruments collection.</param>
	public CollectionSecurityProvider(IEnumerable<Security> securities)
		=> AddRange(securities);

	/// <inheritdoc />
	public int Count => _inner.Count;

	private Action<IEnumerable<Security>> _added;

	event Action<IEnumerable<Security>> ISecurityProvider.Added
	{
		add => _added += value;
		remove => _added -= value;
	}

	private Action<IEnumerable<Security>> _removed;

	event Action<IEnumerable<Security>> ISecurityProvider.Removed
	{
		add => _removed += value;
		remove => _removed -= value;
	}

	private Action _cleared;

	event Action ISecurityProvider.Cleared
	{
		add => _cleared += value;
		remove => _cleared -= value;
	}

	/// <inheritdoc />
	public ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> new(_inner.TryGetValue(id));

	/// <inheritdoc />
	public IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria)
		=> new SyncAsyncEnumerable<Security>(_inner.SyncGet(d => d.Values.Filter(criteria)));

	async ValueTask<SecurityMessage> ISecurityMessageProvider.LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> (await LookupByIdAsync(id, cancellationToken))?.ToMessage();

	IAsyncEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessagesAsync(SecurityLookupMessage criteria)
	{
		return Impl(this, criteria);

		static async IAsyncEnumerable<SecurityMessage> Impl(CollectionSecurityProvider provider, SecurityLookupMessage criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var s in provider.LookupAsync(criteria).WithEnforcedCancellation(cancellationToken))
				yield return s.ToMessage();
		}
	}

	/// <summary>
	/// Add security.
	/// </summary>
	/// <param name="security">Security.</param>
	public void Add(Security security)
	{
		if (_inner.TryAdd2(security.ToSecurityId(), security))
			_added?.Invoke([security]);
	}

	/// <summary>
	/// Remove security.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns><see langword="true"/> if security was removed, otherwise <see langword="false"/>.</returns>
	public bool Remove(Security security)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		if (!_inner.Remove(security.ToSecurityId()))
			return false;

		_removed?.Invoke([security]);
		return true;
	}

	/// <summary>
	/// Add securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	public void AddRange(IEnumerable<Security> securities)
	{
		if (securities is null)
			throw new ArgumentNullException(nameof(securities));

		HashSet<Security> added = null;

		foreach (var security in securities)
		{
			if (!_inner.TryAdd2(security.ToSecurityId(), security))
				continue;

			added ??= [];
			added.Add(security);
		}

		if (added is { Count: > 0 })
			_added?.Invoke(added);
	}

	/// <summary>
	/// Remove securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	public void RemoveRange(IEnumerable<Security> securities)
	{
		if (securities is null)
			throw new ArgumentNullException(nameof(securities));

		HashSet<Security> removed = null;

		foreach (var security in securities)
		{
			if (!_inner.Remove(security.ToSecurityId()))
				continue;

			removed ??= [];
			removed.Add(security);
		}

		if (removed is { Count: > 0 })
			_removed?.Invoke(removed);
	}

	/// <summary>
	/// Clear.
	/// </summary>
	public void Clear()
	{
		using (_inner.EnterScope())
		{
			if (_inner.Count == 0)
				return;

			_inner.Clear();
		}

		_cleared?.Invoke();
	}
}