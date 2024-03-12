namespace StockSharp.Algo.Strategies;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.BusinessEntities;

/// <summary>
/// <see cref="IStrategyParam"/> dictionary.
/// </summary>
public class StrategyParameterDictionary : CachedSynchronizedDictionary<string, IStrategyParam>, IDisposable
{
	private class SecurityParam : StrategyParam<Security>
	{
		private readonly Strategy _strategy;

		public SecurityParam(Strategy strategy)
			: base(nameof(Security))
		{
			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
		}

		public override Security Value
		{
			get => _strategy.Security;
			set => _strategy.Security = value;
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
	/// Get parameter by the specified name.
	/// </summary>
	/// <param name="name"><see cref="IStrategyParam.Name"/></param>
	/// <param name="param"><see cref="IStrategyParam"/> or <see langword="null"/> if parameter not exist.</param>
	/// <returns><see langword="true"/> if parameter exist.</returns>
	public bool TryGetByName(string name, out IStrategyParam param)
	{
		param = CachedValues.FirstOrDefault(p => p.Name == name) ?? (name == _secParam.Name ? _secParam : null);
		return param is not null;
	}

	/// <summary>
	/// Try get parameter by the specified <see cref="IStrategyParam.Name"/>.
	/// </summary>
	/// <param name="name"><see cref="IStrategyParam.Name"/></param>
	/// <returns><see cref="IStrategyParam"/></returns>
	public IStrategyParam GetByName(string name)
		=> TryGetByName(name, out var param) ? param : throw new ArgumentException($"Parameter {name} doesn't exist.");

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
		else if (id == _secParam.Name)
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
		_strategy.RaiseParametersChanged(p.Name);
	}
}