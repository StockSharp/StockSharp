namespace StockSharp.Algo;

/// <summary>
/// The interface, describing the rules list.
/// </summary>
public interface IMarketRuleList : INotifyList<IMarketRule>, ISynchronizedCollection
{
	/// <summary>
	/// To get all active tokens of rules.
	/// </summary>
	IEnumerable<object> Tokens { get; }

	/// <summary>
	/// To get all rules, associated with tokens.
	/// </summary>
	/// <param name="token">Token rules.</param>
	/// <returns>All rules, associated with token.</returns>
	IEnumerable<IMarketRule> GetRulesByToken(object token);

	/// <summary>
	/// Delete all rules, for which <see cref="IMarketRule.Token"/> is equal to <paramref name="token" />.
	/// </summary>
	/// <param name="token">Token rules.</param>
	/// <param name="currentRule">The current rule that has initiated deletion. If it was passed, it will not be deleted.</param>
	void RemoveRulesByToken(object token, IMarketRule currentRule);
}

/// <summary>
/// Rule list.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MarketRuleList"/>.
/// </remarks>
/// <param name="container">The rules container.</param>
public class MarketRuleList(IMarketRuleContainer container) : SynchronizedSet<IMarketRule>(true), IMarketRuleList
{
	private readonly IMarketRuleContainer _container = container ?? throw new ArgumentNullException(nameof(container));
	private readonly Dictionary<object, HashSet<IMarketRule>> _rulesByToken = [];

	/// <summary>
	/// Adding the element.
	/// </summary>
	/// <param name="item">Element.</param>
	protected override void OnAdded(IMarketRule item)
	{
		if (item.Token != null)
			_rulesByToken.SafeAdd(item.Token).Add(item);

		item.Container = _container;
		base.OnAdded(item);
	}

	/// <summary>
	/// Deleting the element.
	/// </summary>
	/// <param name="item">Element.</param>
	protected override void OnRemoved(IMarketRule item)
	{
		item.Container.AddRuleLog(LogLevels.Debug, item, LocalizedStrings.Deleting);

		if (item.Token != null)
		{
			var set = _rulesByToken[item.Token];
			set.Remove(item);

			if (set.IsEmpty())
				_rulesByToken.Remove(item.Token);
		}

		item.Dispose();

		base.OnRemoved(item);
	}

	/// <summary>
	/// Clearing elements.
	/// </summary>
	/// <returns>The sign of possible action.</returns>
	protected override bool OnClearing()
	{
		foreach (var item in this.ToArray())
			Remove(item);

		return base.OnClearing();
	}

	IEnumerable<object> IMarketRuleList.Tokens
	{
		get
		{
			lock (SyncRoot)
				return [.. _rulesByToken.Keys];
		}
	}

	/// <summary>
	/// To get all rules, associated with tokens.
	/// </summary>
	/// <param name="token">Token rules.</param>
	/// <returns>All rules, associated with token.</returns>
	public IEnumerable<IMarketRule> GetRulesByToken(object token)
	{
		lock (SyncRoot)
		{
			return _rulesByToken.TryGetValue(token, out var set)
				? [.. set]
				: [];
		}
	}

	void IMarketRuleList.RemoveRulesByToken(object token, IMarketRule currentRule)
	{
		lock (SyncRoot)
		{
			foreach (var rule in GetRulesByToken(token))
			{
				if (currentRule != rule)
					Remove(rule);
			}
		}
	}
}