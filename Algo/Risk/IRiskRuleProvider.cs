namespace StockSharp.Algo.Risk;

using Ecng.Reflection;

/// <summary>
/// The <see cref="IRiskRule"/> provider.
/// </summary>
public interface IRiskRuleProvider : ICustomProvider<Type>
{
}

/// <summary>
/// The <see cref="IRiskRule"/> provider.
/// </summary>
public class InMemoryRiskRuleProvider : IRiskRuleProvider
{
	private readonly List<Type> _all = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRiskRuleProvider"/>.
	/// </summary>
	public InMemoryRiskRuleProvider()
		=> _all.AddRange(GetType().Assembly.FindImplementations<IRiskRule>(extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null));

	IEnumerable<Type> ICustomProvider<Type>.All => _all;

	void ICustomProvider<Type>.Add(Type rule) => _all.Add(rule);
	void ICustomProvider<Type>.Remove(Type rule) => _all.Remove(rule);
}