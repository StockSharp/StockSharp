namespace StockSharp.Algo.Risk;

using Ecng.Reflection;

/// <summary>
/// The <see cref="IRiskRule"/> provider.
/// </summary>
public interface IRiskRuleProvider
{
	/// <summary>
	/// Rules.
	/// </summary>
	IEnumerable<Type> Rules { get; }

	/// <summary>
	/// Add rule.
	/// </summary>
	/// <param name="rule">Type of <see cref="IRiskRule"/>.</param>
	void AddRule(Type rule);

	/// <summary>
	/// Remove rule.
	/// </summary>
	/// <param name="rule">Type of <see cref="IRiskRule"/>.</param>
	void RemoveRule(Type rule);
}

/// <summary>
/// The <see cref="IRiskRule"/> provider.
/// </summary>
public class InMemoryRiskRuleProvider : IRiskRuleProvider
{
	private readonly List<Type> _rules = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRiskRuleProvider"/>.
	/// </summary>
    public InMemoryRiskRuleProvider()
    {
		_rules.AddRange(GetType().Assembly.FindImplementations<IRiskRule>(extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null));
	}

    IEnumerable<Type> IRiskRuleProvider.Rules => _rules;

	void IRiskRuleProvider.AddRule(Type rule)
	{
		_rules.Add(rule);
	}

	void IRiskRuleProvider.RemoveRule(Type rule)
	{
		_rules.Remove(rule);
	}
}