namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating manager.
/// </summary>
public class CommissionManager : ICommissionManager
{
	private readonly Lock _syncRoot = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="CommissionManager"/>.
	/// </summary>
	public CommissionManager()
	{
	}

	private readonly CachedSynchronizedSet<ICommissionRule> _rules = new(true);

	/// <inheritdoc />
	public ISynchronizedCollection<ICommissionRule> Rules => _rules;

	/// <inheritdoc />
	public virtual decimal Commission { get; private set; }

	/// <inheritdoc />
	public virtual void Reset()
	{
		using (_syncRoot.EnterScope())
			Commission = 0;

		_rules.Cache.ForEach(r => r.Reset());
	}

	/// <inheritdoc />
	public virtual async ValueTask<decimal?> ProcessAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Reset();
				return null;
			}
			case MessageTypes.Execution:
			{
				if (_rules.Count == 0)
					return null;

				var execMsg = (ExecutionMessage)message;

				decimal? commission = null;

				foreach (var rule in _rules.Cache)
				{
					var ruleCom = await rule.ProcessAsync(execMsg, cancellationToken);

					if (ruleCom != null)
						commission = (commission ?? 0) + ruleCom.Value;
				}

				if (commission != null)
				{
					using (_syncRoot.EnterScope())
						Commission += commission.Value;
				}

				return commission;
			}
			default:
				return null;
		}
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var rules = new List<ICommissionRule>();

		foreach (var s in storage.GetValue<SettingsStorage[]>(nameof(Rules)))
			rules.Add(await s.LoadEntireAsync<ICommissionRule>(cancellationToken));

		Rules.Clear();
		Rules.AddRange(rules);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var rules = new List<SettingsStorage>();

		foreach (var rule in Rules)
			rules.Add(await rule.SaveEntireAsync(false, cancellationToken));

		storage.SetValue(nameof(Rules), rules.ToArray());
	}

	/// <inheritdoc />
	public async ValueTask<ICommissionManager> CloneAsync(CancellationToken cancellationToken)
	{
		var clone = new CommissionManager();
		await clone.LoadAsync(await this.SaveAsync(cancellationToken), cancellationToken);
		return clone;
	}
}