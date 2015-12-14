#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: StrategyParam.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The startegy parameter.
	/// </summary>
	public interface IStrategyParam : IPersistable
	{
		/// <summary>
		/// Parameter name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The parameter value.
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// The From value at optimization.
		/// </summary>
		object OptimizeFrom { get; set; }

		/// <summary>
		/// The To value at optimization.
		/// </summary>
		object OptimizeTo { get; set; }

		/// <summary>
		/// The Increment value at optimization.
		/// </summary>
		object OptimizeStep { get; set; }
	}

	/// <summary>
	/// Wrapper for typified access to the strategy parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	public class StrategyParam<T> : IStrategyParam
	{
		private readonly Strategy _strategy;

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="name">Parameter name.</param>
		public StrategyParam(Strategy strategy, string name)
			: this(strategy, name, default(T))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		public StrategyParam(Strategy strategy, string name, T initialValue)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			_strategy = strategy;
			Name = name;
			_value = initialValue;

			_strategy.Parameters.Add(this);
		}

		/// <summary>
		/// Parameter name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Is it possible to store in <see cref="Value"/> a value, equal to <see langword="null" />.
		/// </summary>
		public bool AllowNull { get; set; } = typeof(T).IsNullable();

		private T _value;

		/// <summary>
		/// The parameter value.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (!AllowNull && value.IsNull())
					throw new ArgumentNullException(nameof(value));

				if (EqualityComparer<T>.Default.Equals(_value, value))
					return;

				var propChange = _value as INotifyPropertyChanged;
				if (propChange != null)
					propChange.PropertyChanged -= OnValueInnerStateChanged;

				_value = value;
				_strategy.RaiseParametersChanged(Name);

				propChange = _value as INotifyPropertyChanged;
				if (propChange != null)
					propChange.PropertyChanged += OnValueInnerStateChanged;
			}
		}

		/// <summary>
		/// The From value at optimization.
		/// </summary>
		public object OptimizeFrom { get; set; }

		/// <summary>
		/// The To value at optimization.
		/// </summary>
		public object OptimizeTo { get; set; }

		/// <summary>
		/// The Increment value at optimization.
		/// </summary>
		public object OptimizeStep { get; set; }

		private void OnValueInnerStateChanged(object sender, PropertyChangedEventArgs e)
		{
			_strategy.RaiseParametersChanged(Name);
		}

		object IStrategyParam.Value
		{
			get { return Value; }
			set { Value = (T)value; }
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>("Name");
			Value = storage.GetValue<T>("Value");
			OptimizeFrom = storage.GetValue<T>("OptimizeFrom");
			OptimizeTo = storage.GetValue<T>("OptimizeTo");
			OptimizeStep = storage.GetValue<object>("OptimizeStep");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Name", Name);
			storage.SetValue("Value", Value);
			storage.SetValue("OptimizeFrom", OptimizeFrom);
			storage.SetValue("OptimizeTo", OptimizeTo);
			storage.SetValue("OptimizeStep", OptimizeStep);
		}
	}

	/// <summary>
	/// The auxiliary class for <see cref="StrategyParam{T}"/>.
	/// </summary>
	public static class StrategyParamHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="strategy">Strategy.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <returns>The startegy parameter.</returns>
		public static StrategyParam<T> Param<T>(this Strategy strategy, string name, T initialValue = default(T))
		{
			return new StrategyParam<T>(strategy, name, initialValue);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="param">The startegy parameter.</param>
		/// <param name="optimizeFrom">The From value at optimization.</param>
		/// <param name="optimizeTo">The To value at optimization.</param>
		/// <param name="optimizeStep">The Increment value at optimization.</param>
		/// <returns>The startegy parameter.</returns>
		public static StrategyParam<T> Optimize<T>(this StrategyParam<T> param, T optimizeFrom = default(T), T optimizeTo = default(T), T optimizeStep = default(T))
		{
			if (param == null)
				throw new ArgumentNullException(nameof(param));

			param.OptimizeFrom = optimizeFrom;
			param.OptimizeTo = optimizeTo;
			param.OptimizeStep = optimizeStep;

			return param;
		}
	}
}