namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for access to the storage of information on instruments.
/// </summary>
public interface ISecurityStorage : ISecurityProvider
{
	/// <summary>
	/// Enter sync scope.
	/// </summary>
	/// <returns>Sync scope.</returns>
	Lock.Scope EnterScope();

	/// <summary>
	/// Save security.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="forced">Forced update.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation task.</returns>
	ValueTask SaveAsync(Security security, bool forced, CancellationToken cancellationToken);

	/// <summary>
	/// Delete security.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation task.</returns>
	ValueTask DeleteAsync(Security security, CancellationToken cancellationToken);

	/// <summary>
	/// Delete securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation task.</returns>
	ValueTask DeleteRangeAsync(IEnumerable<Security> securities, CancellationToken cancellationToken);

	/// <summary>
	/// To delete instruments by the criterion.
	/// </summary>
	/// <param name="criteria">The criterion.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Operation task.</returns>
	ValueTask DeleteByAsync(SecurityLookupMessage criteria, CancellationToken cancellationToken);
}

/// <summary>
/// In memory implementation of <see cref="ISecurityStorage"/>.
/// </summary>
public class InMemorySecurityStorage : ISecurityStorage
{
	private readonly ISecurityProvider _underlying;
	private readonly SynchronizedDictionary<SecurityId, Security> _inner = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemorySecurityStorage"/>.
	/// </summary>
	public InMemorySecurityStorage()
		: this(new CollectionSecurityProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemorySecurityStorage"/>.
	/// </summary>
	/// <param name="underlying">Underlying provider.</param>
	public InMemorySecurityStorage(ISecurityProvider underlying)
	{
		_underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	}

	Lock.Scope ISecurityStorage.EnterScope() => _inner.EnterScope();

	int ISecurityProvider.Count => _inner.Count;

	/// <inheritdoc />
	public event Action<IEnumerable<Security>> Added;

	/// <inheritdoc />
	public event Action<IEnumerable<Security>> Removed;

	/// <inheritdoc />
	public event Action Cleared;

	/// <inheritdoc />
	public ValueTask DeleteAsync(Security security, CancellationToken cancellationToken)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		cancellationToken.ThrowIfCancellationRequested();

		if (_inner.Remove(security.ToSecurityId()))
			Removed?.Invoke([security]);

		return default;
	}

	/// <inheritdoc />
	public ValueTask DeleteByAsync(SecurityLookupMessage criteria, CancellationToken cancellationToken)
	{
		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		if (criteria.IsLookupAll())
		{
			_inner.Clear();
			Cleared?.Invoke();
			return default;
		}

		Security[] toDelete;

		using (_inner.EnterScope())
		{
			toDelete = [.. _inner.Values.Filter(criteria)];

			foreach (var security in toDelete)
				_inner.Remove(security.ToSecurityId());
		}

		cancellationToken.ThrowIfCancellationRequested();
		Removed?.Invoke(toDelete);
		return default;
	}

	/// <inheritdoc />
	public ValueTask DeleteRangeAsync(IEnumerable<Security> securities, CancellationToken cancellationToken)
	{
		if (securities is null)
			throw new ArgumentNullException(nameof(securities));

		HashSet<Security> toDelete = null;

		using (_inner.EnterScope())
		{
			foreach (var security in securities)
			{
				if (!_inner.Remove(security.ToSecurityId()))
					continue;

				toDelete ??= [];
				toDelete.Add(security);
			}
		}

		cancellationToken.ThrowIfCancellationRequested();

		if (toDelete is { Count: > 0 })
			Removed?.Invoke(toDelete);

		return default;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria)
		=> _inner.SyncGet(d => d.Values.Filter(criteria).ToArray()).ToAsyncEnumerable().Concat(_underlying.LookupAsync(criteria)).Distinct();

	/// <inheritdoc />
	public async ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> _inner.TryGetValue(id) ?? await _underlying.LookupByIdAsync(id, cancellationToken);

	async ValueTask<SecurityMessage> ISecurityMessageProvider.LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> (await LookupByIdAsync(id, cancellationToken))?.ToMessage();

	IAsyncEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessagesAsync(SecurityLookupMessage criteria)
		=> LookupAsync(criteria).Select(s => s.ToMessage());

	/// <inheritdoc />
	public ValueTask SaveAsync(Security security, bool forced, CancellationToken cancellationToken)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		cancellationToken.ThrowIfCancellationRequested();

		if (_inner.TryAdd2(security.ToSecurityId(), security))
			Added?.Invoke([security]);

		return default;
	}
}