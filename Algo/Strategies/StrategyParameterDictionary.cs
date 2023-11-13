namespace StockSharp.Algo.Strategies;

using System;
using System.ComponentModel;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.Localization;

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

	/// <summary>
	/// Add parameter.
	/// </summary>
	/// <param name="p"><see cref="IStrategyParam"/></param>
	public void Add(IStrategyParam p)
	{
		if (p is null)
			throw new ArgumentNullException(nameof(p));

		if (!_strategy.Parameters.TryAdd2(p.Id, p))
			throw new ArgumentException(LocalizedStrings.CompositionAlreadyExistParams.Put(p.Name, string.Empty), nameof(p));
	}

	/// <inheritdoc/>
	public override void Add(string key, IStrategyParam value)
	{
		base.Add(key, value);
		value.PropertyChanged += OnPropertyChanged;
	}

	/// <inheritdoc/>
	public override bool Remove(string key)
	{
		if (!this.TryGetAndRemove(key, out var p))
			return false;

		p.PropertyChanged -= OnPropertyChanged;
		return true;
	}

	/// <inheritdoc/>
	public override void Clear()
	{
		CachedValues.ForEach(p => p.PropertyChanged -= OnPropertyChanged);

		base.Clear();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		CachedValues.ForEach(p => p.PropertyChanged -= OnPropertyChanged);
		GC.SuppressFinalize(this);
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		var p = (IStrategyParam)sender;
		_strategy.RaiseParametersChanged(p.Name);
	}
}