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
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The strategy parameter.
	/// </summary>
	public interface IStrategyParam : IPersistable
	{
		/// <summary>
		/// Parameter identifier.
		/// </summary>
		string Id { get; }

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
			: this(strategy, name, name, default(T))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="id">Parameter identifier.</param>
		/// <param name="name">Parameter name.</param>
		public StrategyParam(Strategy strategy, string id, string name)
			: this(strategy, id, name, default(T))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		public StrategyParam(Strategy strategy, string name, T initialValue)
			: this(strategy, name, name, initialValue)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="id">Parameter identifier.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		public StrategyParam(Strategy strategy, string id, string name, T initialValue)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			Id = id;
			Name = name;
			_value = initialValue;

			if (!_strategy.Parameters.TryAdd(id, this))
				throw new ArgumentException(LocalizedStrings.CompositionAlreadyExistParams.Put(name, string.Empty), nameof(name));
		}

		/// <summary>
		/// Parameter identifier.
		/// </summary>
		public string Id { get; private set; }

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
			get => _value;
			set
			{
				if (!AllowNull && value.IsNull())
					throw new ArgumentNullException(nameof(value));

				if (EqualityComparer<T>.Default.Equals(_value, value))
					return;

				if (_value is INotifyPropertyChanged propChange)
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
			get => Value;
			set => Value = (T)value;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Id = storage.GetValue<string>(nameof(Id));
			Name = storage.GetValue<string>(nameof(Name));
			Value = storage.GetValue<T>(nameof(Value));
			OptimizeFrom = storage.GetValue<T>(nameof(OptimizeFrom));
			OptimizeTo = storage.GetValue<T>(nameof(OptimizeTo));
			OptimizeStep = storage.GetValue<object>(nameof(OptimizeStep));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Id), Id);
			storage.SetValue(nameof(Name), Name);
			storage.SetValue(nameof(Value), Value);
			storage.SetValue(nameof(OptimizeFrom), OptimizeFrom);
			storage.SetValue(nameof(OptimizeTo), OptimizeTo);
			storage.SetValue(nameof(OptimizeStep), OptimizeStep);
		}
	}
}