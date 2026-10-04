namespace StockSharp.Algo.Risk;

/// <summary>
/// The risks control manager.
/// </summary>
public class RiskManager : BaseLogReceiver, IRiskManager
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RiskManager"/>.
	/// </summary>
	public RiskManager()
	{
	}

	private readonly CachedSynchronizedList<IRiskRule> _rules = [];

	/// <inheritdoc />
	public INotifyList<IRiskRule> Rules => _rules;

	/// <inheritdoc />
	public virtual void Reset()
	{
		_rules.Cache.ForEach(r => r.Reset());
	}

	/// <inheritdoc />
	public IEnumerable<IRiskRule> ProcessRules(Message message)
	{
		if (message.Type == MessageTypes.Reset)
		{
			Reset();
			return [];
		}

		var rules = _rules.Cache;

		if (rules.Length == 0)
			return [];

		return [.. rules.Where(r => r.ProcessMessage(message))];
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var rules = new List<IRiskRule>();

		foreach (var s in storage.GetValue<SettingsStorage[]>(nameof(Rules)))
			rules.Add(await s.LoadEntireAsync<IRiskRule>(cancellationToken));

		Rules.Clear();
		Rules.AddRange(rules);

		await base.LoadAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var rules = new List<SettingsStorage>();

		foreach (var rule in Rules)
			rules.Add(await rule.SaveEntireAsync(false, cancellationToken));

		storage.SetValue(nameof(Rules), rules.ToArray());

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<IRiskManager> CloneAsync(CancellationToken cancellationToken)
	{
		var clone = new RiskManager();
		await clone.LoadAsync(await this.SaveAsync(cancellationToken), cancellationToken);
		return clone;
	}
}