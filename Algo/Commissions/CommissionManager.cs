namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating manager.
/// </summary>
public class CommissionManager : ICommissionManager
{
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
		Commission = 0;
		_rules.Cache.ForEach(r => r.Reset());
	}

	/// <inheritdoc />
	public virtual decimal? Process(Message message)
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
					var ruleCom = rule.Process(execMsg);

					if (ruleCom != null)
						commission = (commission ?? 0) + ruleCom.Value;
				}

				if (commission != null)
					Commission += commission.Value;

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
	public void Load(SettingsStorage storage)
	{
		Rules.Clear();
		Rules.AddRange(storage.GetValue<SettingsStorage[]>(nameof(Rules)).Select(s => s.LoadEntire<ICommissionRule>()));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Rules), Rules.Select(r => r.SaveEntire(false)).ToArray());
	}
}