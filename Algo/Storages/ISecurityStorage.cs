namespace StockSharp.Algo.Storages;

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
	void Save(Security security, bool forced);

	/// <summary>
	/// Delete security.
	/// </summary>
	/// <param name="security">Security.</param>
	void Delete(Security security);

	/// <summary>
	/// Delete securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	void DeleteRange(IEnumerable<Security> securities);

	/// <summary>
	/// To delete instruments by the criterion.
	/// </summary>
	/// <param name="criteria">The criterion.</param>
	void DeleteBy(SecurityLookupMessage criteria);
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
	public void Delete(Security security)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		if (_inner.Remove(security.ToSecurityId()))
			Removed?.Invoke([security]);
	}

	/// <inheritdoc />
	public void DeleteBy(SecurityLookupMessage criteria)
	{
		if (criteria.IsLookupAll())
		{
			_inner.Clear();
			Cleared?.Invoke();
			return;
		}

		Security[] toDelete;

		lock (_inner.SyncRoot)
		{
			toDelete = [.. _inner.Values.Filter(criteria)];

			foreach (var security in toDelete)
				_inner.Remove(security.ToSecurityId());
		}

		Removed?.Invoke(toDelete);
	}

	/// <inheritdoc />
	public void DeleteRange(IEnumerable<Security> securities)
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

		Removed?.Invoke(toDelete);
	}

	/// <inheritdoc />
	public IEnumerable<Security> Lookup(SecurityLookupMessage criteria)
		=> _inner.SyncGet(d => d.Values.Filter(criteria).ToArray()).Concat(_underlying.Lookup(criteria)).Distinct();

	/// <inheritdoc />
	public Security LookupById(SecurityId id)
		=> _inner.TryGetValue(id) ?? _underlying.LookupById(id);

	SecurityMessage ISecurityMessageProvider.LookupMessageById(SecurityId id)
		=> LookupById(id)?.ToMessage();

	IEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessages(SecurityLookupMessage criteria)
		=> Lookup(criteria).Select(s => s.ToMessage());

	/// <inheritdoc />
	public void Save(Security security, bool forced)
	{
		if (security is null)
			throw new ArgumentNullException(nameof(security));

		if (_inner.TryAdd2(security.ToSecurityId(), security))
			Added?.Invoke([security]);
	}
}