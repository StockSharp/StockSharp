namespace StockSharp.Algo;

using Ecng.Linq;

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
	public FilterableSecurityProvider(ISecurityProvider provider)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));

		_provider.Added += AddSecurities;
		_provider.Removed += RemoveSecurities;
		_provider.Cleared += ClearSecurities;

		AddSecurities(_provider.LookupAll());
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
	public IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria, CancellationToken cancellationToken)
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

		return securities.Filter(criteria).TryLimitByCount(criteria).ToAsyncEnumerable2(cancellationToken);
	}

	async ValueTask<SecurityMessage> ISecurityMessageProvider.LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		=> (await LookupByIdAsync(id, cancellationToken))?.ToMessage();

	async IAsyncEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessagesAsync(SecurityLookupMessage criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		await foreach (var s in LookupAsync(criteria, cancellationToken).WithEnforcedCancellation(cancellationToken))
			yield return s.ToMessage();
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
