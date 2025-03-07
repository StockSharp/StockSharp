namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private ISecurityProvider SecurityProvider => SafeGetConnector();

	int ISecurityProvider.Count => SecurityProvider.Count;

	event Action<IEnumerable<Security>> ISecurityProvider.Added
	{
		add => SecurityProvider.Added += value;
		remove => SecurityProvider.Added -= value;
	}

	event Action<IEnumerable<Security>> ISecurityProvider.Removed
	{
		add => SecurityProvider.Removed += value;
		remove => SecurityProvider.Removed -= value;
	}

	event Action ISecurityProvider.Cleared
	{
		add => SecurityProvider.Cleared += value;
		remove => SecurityProvider.Cleared -= value;
	}

	/// <inheritdoc />
	public Security LookupById(SecurityId id)
		=> SecurityProvider.LookupById(id);
	
	IEnumerable<Security> ISecurityProvider.Lookup(SecurityLookupMessage criteria)
		=> SecurityProvider.Lookup(criteria);

	SecurityMessage ISecurityMessageProvider.LookupMessageById(SecurityId id)
		=> SecurityProvider.LookupMessageById(id);

	IEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessages(SecurityLookupMessage criteria)
		=> SecurityProvider.LookupMessages(criteria);
}
