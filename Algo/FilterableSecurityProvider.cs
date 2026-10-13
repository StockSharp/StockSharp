namespace StockSharp.Algo;

/// <summary>
/// Provider of information about instruments supporting search using <see cref="SecurityTrie"/>.
/// </summary>
public class FilterableSecurityProvider : Disposable, ISecurityProvider
{
	private readonly SecurityTrie _trie = [];

	private readonly ISecurityProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="FilterableSecurityProvider"/>.
	/// </summary>
	/// <param name="provider">Security meta info provider.</param>
	[Obsolete("Use CreateAsync method instead.")]
	public FilterableSecurityProvider(ISecurityProvider provider)
		: this(provider, true)
	{
		AddSecurities(_provider.LookupAllAsync().ToBlockingEnumerable());
	}

	private FilterableSecurityProvider(ISecurityProvider provider, bool subscribe)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));

		if (!subscribe)
			return;

		_provider.Added += AddSecurities;
		_provider.Removed += RemoveSecurities;
		_provider.Cleared += ClearSecurities;
	}

	/// <summary>
	/// Creates the provider over everything <paramref name="provider"/> holds now, kept in step with it afterwards.
	/// </summary>
	/// <param name="provider">Security meta info provider.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The provider.</returns>
	public static async ValueTask<FilterableSecurityProvider> CreateAsync(ISecurityProvider provider, CancellationToken cancellationToken)
	{
		var filterable = new FilterableSecurityProvider(provider, true);

		filterable.AddSecurities(await provider.LookupAllAsync().ToArrayAsync(cancellationToken));

		return filterable;
	}

	/// <inheritdoc />
	public int Count => _trie.Count;

	/// <inheritdoc />
	public event Action<IEnumerable<Security>> Added;

	/// <inheritdoc />
	public event Action<IEnumerable<Security>> Removed;

	/// <inheritdoc />
	public event Action Cleared;

	/// <inheritdoc />
	public ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> new(_trie.GetById(id));

	/// <inheritdoc />
	public IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		var secId = criteria.SecurityId.ToStringId(nullIfEmpty: true);

		var filter = secId.IsEmpty()
			? (criteria.IsLookupAll() ? string.Empty : criteria.SecurityId.SecurityCode)
			: secId;

		var securities = _trie.Retrieve(filter);

		if (!secId.IsEmpty())
			securities = securities.Where(s => s.Id.EqualsIgnoreCase(secId));

		return new SyncAsyncEnumerable<Security>(securities.Filter(criteria).TryLimitByCount(criteria));
	}

	async ValueTask<SecurityMessage> ISecurityMessageProvider.LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> (await LookupByIdAsync(id, cancellationToken))?.ToMessage();

	IAsyncEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessagesAsync(SecurityLookupMessage criteria)
	{
		return Impl(this, criteria);

		static async IAsyncEnumerable<SecurityMessage> Impl(FilterableSecurityProvider provider, SecurityLookupMessage criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var s in provider.LookupAsync(criteria).WithEnforcedCancellation(cancellationToken))
				yield return s.ToMessage();
		}
	}

	private void AddSecurities(IEnumerable<Security> securities)
	{
		securities.ForEach(_trie.Add);
            Added?.Invoke(securities);
	}

	private void RemoveSecurities(IEnumerable<Security> securities)
	{
		_trie.RemoveRange(securities);
            Removed?.Invoke(securities);
	}

	private void ClearSecurities()
	{
		_trie.Clear();
		Cleared?.Invoke();
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_provider.Added -= AddSecurities;
		_provider.Removed -= RemoveSecurities;
		_provider.Cleared -= ClearSecurities;

		base.DisposeManaged();
	}
}
