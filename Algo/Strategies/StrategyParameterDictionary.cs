namespace StockSharp.Algo.Strategies;

using System;
using System.ComponentModel;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// <see cref="IStrategyParam"/> dictionary.
/// </summary>
public class StrategyParameterDictionary : CachedSynchronizedDictionary<string, IStrategyParam>, IDisposable
{
	private readonly Strategy _strategy;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParameterDictionary"/>.
	/// </summary>
	/// <param name="strategy"><see cref="Strategy"/></param>
	public StrategyParameterDictionary(Strategy strategy)
		: base(StringComparer.InvariantCultureIgnoreCase)
    {
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
	}

	/// <summary>
	/// Get parameter by the specified name.
	/// </summary>
	/// <param name="name"><see cref="IStrategyParam.Name"/></param>
	/// <returns><see cref="IStrategyParam"/></returns>
	public IStrategyParam TryGetByName(string name)
		=> CachedValues.FirstOrDefault(p => p.Name == name);

	/// <summary>
	/// Try get parameter by the specified name.
	/// </summary>
	/// <param name="name"><see cref="IStrategyParam.Name"/></param>
	/// <returns><see cref="IStrategyParam"/> or <see langword="null"/> if parameter not exist.</returns>
	public IStrategyParam GetByName(string name)
		=> TryGetByName(name) ?? throw new ArgumentException($"Parameter {name} doesn't exist.");

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
		_strategy.RaiseParametersChanged(p.Name);
	}
}