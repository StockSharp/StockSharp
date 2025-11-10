namespace StockSharp.Algo.Storages;

using System.Runtime.CompilerServices;

/// <summary>
/// The interface for access to the storage of information on instruments.
/// </summary>
public interface ISecurityStorage : ISecurityProvider
{
	/// <summary>
	/// Sync object.
	/// </summary>
	SyncObject SyncRoot { get; }

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

	SyncObject ISecurityStorage.SyncRoot => _inner.SyncRoot;

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

		lock (_inner.SyncRoot)
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

		ISet<Security> toDelete = null;

		lock (_inner.SyncRoot)
		{
			foreach (var security in securities)
			{
				if (!_inner.Remove(security.ToSecurityId()))
				{
					toDelete ??= securities.ToSet();

					toDelete.Remove(security);
				}
			}
		}

		cancellationToken.ThrowIfCancellationRequested();
		Removed?.Invoke(toDelete);
		return default;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		await Task.Yield();

		foreach (var s in _inner.SyncGet(d => d.Values.Filter(criteria).ToArray()).Concat(_underlying.Lookup(criteria)).Distinct())
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return s;
		}
	}

	/// <inheritdoc />
	public ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> new(_inner.TryGetValue(id) ?? _underlying.LookupById(id));

	async ValueTask<SecurityMessage> ISecurityMessageProvider.LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> (await LookupByIdAsync(id, cancellationToken))?.ToMessage();

	async IAsyncEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessagesAsync(SecurityLookupMessage criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		await foreach (var s in LookupAsync(criteria, cancellationToken).WithEnforcedCancellation(cancellationToken))
			yield return s.ToMessage();
	}

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