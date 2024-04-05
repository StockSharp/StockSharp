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
	using Ecng.ComponentModel;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// The strategy parameter.
	/// </summary>
	public interface IStrategyParam : IPersistable, INotifyPropertyChanged
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
		/// The type of the parameter value.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// The parameter value.
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// Check can optimize parameter.
		/// </summary>
		bool CanOptimize { get; set; }

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

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage"><see cref="SettingsStorage"/></param>
		/// <param name="addDescription">Add description info.</param>
		void Save(SettingsStorage storage, bool addDescription);
	}

	/// <summary>
	/// Wrapper for typified access to the strategy parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	public class StrategyParam<T> : NotifiableObject, IStrategyParam
	{
		private readonly IEqualityComparer<T> _comparer;

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		public StrategyParam(string name)
			: this(name, name)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="id">Parameter identifier.</param>
		/// <param name="name">Parameter name.</param>
		public StrategyParam(string id, string name)
			: this(id, name, default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		public StrategyParam(string name, T initialValue)
			: this(name, name, initialValue)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="id">Parameter identifier.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		public StrategyParam(string id, string name, T initialValue)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			Id = id;
			Name = name;
			_value = initialValue;

			CanOptimize = typeof(T).CanOptimize();

			_comparer = EqualityComparer<T>.Default;
		}

		/// <inheritdoc />
		public string Id { get; private set; }

		/// <inheritdoc />
		public string Name { get; private set; }

		private T _value;

		/// <inheritdoc />
		public virtual T Value
		{
			get => _value;
			set
			{
				if (Validator?.Invoke(value) == false)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				if (_comparer.Equals(_value, value))
					return;

				if (_value is INotifyPropertyChanged propChange)
					propChange.PropertyChanged -= OnValueInnerStateChanged;

				_value = value;
				NotifyChanged(nameof(Value));

				if (_value is INotifyPropertyChanged propChange2)
					propChange2.PropertyChanged += OnValueInnerStateChanged;
			}
		}

		/// <summary>
		/// <see cref="Value"/> validator.
		/// </summary>
		public Func<T, bool> Validator { get; set; }

		Type IStrategyParam.Type => typeof(T);

		object IStrategyParam.Value
		{
			get => Value;
			set => Value = (T)value;
		}

		/// <inheritdoc />
		public bool CanOptimize { get; set; }

		/// <inheritdoc />
		public object OptimizeFrom { get; set; }

		/// <inheritdoc />
		public object OptimizeTo { get; set; }

		/// <inheritdoc />
		public object OptimizeStep { get; set; }

		private void OnValueInnerStateChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyChanged(nameof(Value));
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Id = storage.GetValue<string>(nameof(Id));
			Name = storage.GetValue<string>(nameof(Name));

			try
			{
				Value = storage.GetValue<T>(nameof(Value));
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			CanOptimize = storage.GetValue(nameof(CanOptimize), CanOptimize);
			OptimizeFrom = storage.GetValue<SettingsStorage>(nameof(OptimizeFrom))?.FromStorage();
			OptimizeTo = storage.GetValue<SettingsStorage>(nameof(OptimizeTo))?.FromStorage();
			OptimizeStep = storage.GetValue<SettingsStorage>(nameof(OptimizeStep))?.FromStorage();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			Save(storage, false);
		}

		/// <inheritdoc />
		public void Save(SettingsStorage storage, bool addDescription)
		{
			storage
				.Set(nameof(Id), Id)
				.Set(nameof(Name), Name)
				.Set(nameof(Value), Value)
			;

			if (addDescription)
			{
				if (typeof(T).IsEnum)
					storage.Set("Description", Enumerator.GetNames<T>().JoinPipe());
			}
			else
			{
				storage
					.Set(nameof(CanOptimize), CanOptimize)
					.Set(nameof(OptimizeFrom), OptimizeFrom?.ToStorage())
					.Set(nameof(OptimizeTo), OptimizeTo?.ToStorage())
					.Set(nameof(OptimizeStep), OptimizeStep?.ToStorage())
				;
			}
		}

		/// <inheritdoc />
		public override string ToString() => $"{Name}={Value}";
	}
}