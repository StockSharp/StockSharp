namespace StockSharp.Algo.Commissions;

using Ecng.Reflection;

/// <summary>
/// The <see cref="ICommissionRule"/> provider.
/// </summary>
public interface ICommissionRuleProvider
{
	/// <summary>
	/// Rules.
	/// </summary>
	IEnumerable<Type> Rules { get; }

	/// <summary>
	/// Add rule.
	/// </summary>
	/// <param name="rule">Type of <see cref="ICommissionRule"/>.</param>
	void AddRule(Type rule);

	/// <summary>
	/// Remove rule.
	/// </summary>
	/// <param name="rule">Type of <see cref="ICommissionRule"/>.</param>
	void RemoveRule(Type rule);
}

/// <summary>
/// The <see cref="ICommissionRule"/> provider.
/// </summary>
public class InMemoryCommissionRuleProvider : ICommissionRuleProvider
{
	private readonly CachedSynchronizedSet<Type> _rules = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryCommissionRuleProvider"/>.
	/// </summary>
	public InMemoryCommissionRuleProvider()
		=> _rules.AddRange(GetType().Assembly.FindImplementations<ICommissionRule>(extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null));

    IEnumerable<Type> ICommissionRuleProvider.Rules
		=> _rules.Cache;

	void ICommissionRuleProvider.AddRule(Type rule)
		=> _rules.Add(rule);

	void ICommissionRuleProvider.RemoveRule(Type rule)
		=> _rules.Remove(rule);
}