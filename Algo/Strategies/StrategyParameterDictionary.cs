namespace StockSharp.Algo.Strategies;

/// <summary>
/// <see cref="IStrategyParam"/> dictionary.
/// </summary>
public class StrategyParameterDictionary : CachedSynchronizedDictionary<string, IStrategyParam>, IDisposable
{
	private class SecurityParam(Strategy strategy) : StrategyParam<Security>(nameof(Security))
	{
		public override Security Value
		{
			get => strategy.Security;
			set => strategy.Security = value;
		}
	}

	private readonly Strategy _strategy;
	private readonly SecurityParam _secParam;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParameterDictionary"/>.
	/// </summary>
	/// <param name="strategy"><see cref="Strategy"/></param>
	public StrategyParameterDictionary(Strategy strategy)
		: base(StringComparer.InvariantCultureIgnoreCase)
    {
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
		_secParam = new(strategy);
	}

	/// <summary>
	/// Try get parameter by the specified <see cref="IStrategyParam.Id"/>.
	/// </summary>
	/// <param name="id"><see cref="IStrategyParam.Id"/></param>
	/// <param name="param"><see cref="IStrategyParam"/> or <see langword="null"/> if parameter not exist.</param>
	/// <returns><see langword="true"/> if parameter exist.</returns>
	public bool TryGetById(string id, out IStrategyParam param)
	{
		if (TryGetValue(id, out param))
			return true;
		else if (id == _secParam.Id)
		{
			param = _secParam;
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public override IStrategyParam this[string key]
		=> TryGetById(key, out var param) ? param : throw new KeyNotFoundException($"Parameter with '{key}' doesn't exist.");

	private static string GetKey(IStrategyParam p) => p.CheckOnNull(nameof(p)).Id;

	/// <summary>
	/// Add parameter.
	/// </summary>
	/// <param name="p"><see cref="IStrategyParam"/></param>
	public void Add(IStrategyParam p) => Add(GetKey(p), p);

	/// <inheritdoc/>
	public override void Add(string key, IStrategyParam value)
	{
		base.Add(key, value);
		value.PropertyChanged += OnPropertyChanged;
	}

	/// <summary>
	/// Remove parameter.
	/// </summary>
	/// <param name="p"><see cref="IStrategyParam"/></param>
	public bool Remove(IStrategyParam p) => Remove(GetKey(p));

	/// <inheritdoc/>
	public override bool Remove(string key)
	{
		lock (SyncRoot)
		{
			if (!TryGetValue(key, out var p))
				return false;

			base.Remove(key);
			p.PropertyChanged -= OnPropertyChanged;
		}

		return true;
	}

	/// <inheritdoc/>
	public override void Clear()
	{
		Unsubscribe();
		base.Clear();
	}

	private void Unsubscribe() => CachedValues.ForEach(p => p.PropertyChanged -= OnPropertyChanged);

	/// <inheritdoc/>
	public void Dispose()
	{
		Unsubscribe();
		GC.SuppressFinalize(this);
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		var p = (IStrategyParam)sender;
		_strategy.RaiseParametersChanged(p.Id);
	}
}