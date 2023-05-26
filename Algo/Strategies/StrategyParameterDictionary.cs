namespace StockSharp.Algo.Strategies;

using System;
using System.Linq;

using Ecng.Collections;

/// <summary>
/// <see cref="IStrategyParam"/> dictionary.
/// </summary>
public class StrategyParameterDictionary : CachedSynchronizedDictionary<string, IStrategyParam>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParameterDictionary"/>.
	/// </summary>
	public StrategyParameterDictionary()
		: base(StringComparer.InvariantCultureIgnoreCase)
    {
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
}