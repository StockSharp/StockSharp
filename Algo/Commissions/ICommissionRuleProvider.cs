namespace StockSharp.Algo.Commissions;

using Ecng.Reflection;

/// <summary>
/// The <see cref="ICommissionRule"/> provider.
/// </summary>
public interface ICommissionRuleProvider : ICustomProvider<Type>
{
}

/// <summary>
/// The <see cref="ICommissionRule"/> provider.
/// </summary>
public class InMemoryCommissionRuleProvider : ICommissionRuleProvider
{
	private readonly CachedSynchronizedSet<Type> _all = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryCommissionRuleProvider"/>.
	/// </summary>
	public InMemoryCommissionRuleProvider()
		=> _all.AddRange(GetType().Assembly.FindImplementations<ICommissionRule>(extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null));

	IEnumerable<Type> ICustomProvider<Type>.All
		=> _all.Cache;

	void ICustomProvider<Type>.Add(Type rule) => _all.Add(rule);
	void ICustomProvider<Type>.Remove(Type rule) => _all.Remove(rule);
}