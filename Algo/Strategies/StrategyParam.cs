namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Параметр стратегии.
	/// </summary>
	public interface IStrategyParam : IPersistable
	{
		/// <summary>
		/// Название параметра.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Значение параметра.
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// Значение От при оптимизации.
		/// </summary>
		object OptimizeFrom { get; set; }

		/// <summary>
		/// Значение До при оптимизации.
		/// </summary>
		object OptimizeTo { get; set; }

		/// <summary>
		/// Значение Шаг при оптимизации.
		/// </summary>
		object OptimizeStep { get; set; }
	}

	/// <summary>
	/// Обертка для типизированного доступа к параметру стратегии.
	/// </summary>
	/// <typeparam name="T">Тип значения параметра.</typeparam>
	public class StrategyParam<T> : IStrategyParam
	{
		private readonly Strategy _strategy;

		/// <summary>
		/// Создать <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="name">Название параметра.</param>
		public StrategyParam(Strategy strategy, string name)
			: this(strategy, name, default(T))
		{
		}

		/// <summary>
		/// Создать <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="name">Название параметра.</param>
		/// <param name="initialValue">Первоначальное значение.</param>
		public StrategyParam(Strategy strategy, string name, T initialValue)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			_strategy = strategy;
			Name = name;
			_value = initialValue;

			_strategy.Parameters.Add(this);
		}

		/// <summary>
		/// Название параметра.
		/// </summary>
		public string Name { get; private set; }

		private bool _allowNull = typeof(T).IsNullable();

		/// <summary>
		/// Возможно ли в <see cref="Value"/> хранить значение, равное null.
		/// </summary>
		public bool AllowNull
		{
			get { return _allowNull; }
			set { _allowNull = value; }
		}

		private T _value;

		/// <summary>
		/// Значение параметра.
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
					throw new ArgumentNullException("value");

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
		/// Значение От при оптимизации.
		/// </summary>
		public object OptimizeFrom { get; set; }

		/// <summary>
		/// Значение До при оптимизации.
		/// </summary>
		public object OptimizeTo { get; set; }

		/// <summary>
		/// Значение Шаг при оптимизации.
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
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>("Name");
			Value = storage.GetValue<T>("Value");
			OptimizeFrom = storage.GetValue<T>("OptimizeFrom");
			OptimizeTo = storage.GetValue<T>("OptimizeTo");
			OptimizeStep = storage.GetValue<object>("OptimizeStep");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
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
	/// Вспомогательный класс для с <see cref="StrategyParam{T}"/>.
	/// </summary>
	public static class StrategyParamHelper
	{
		/// <summary>
		/// Создать <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">Тип значения параметра.</typeparam>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="name">Название параметра.</param>
		/// <param name="initialValue">Первоначальное значение.</param>
		/// <returns>Параметр стратегии.</returns>
		public static StrategyParam<T> Param<T>(this Strategy strategy, string name, T initialValue = default(T))
		{
			return new StrategyParam<T>(strategy, name, initialValue);
		}

		/// <summary>
		/// Создать <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">Тип значения параметра.</typeparam>
		/// <param name="param">Параметр стратегии.</param>
		/// <param name="optimizeFrom">Значение От при оптимизации.</param>
		/// <param name="optimizeTo">Значение До при оптимизации.</param>
		/// <param name="optimizeStep">Значение Шаг при оптимизации.</param>
		/// <returns>Параметр стратегии.</returns>
		public static StrategyParam<T> Optimize<T>(this StrategyParam<T> param, T optimizeFrom = default(T), T optimizeTo = default(T), T optimizeStep = default(T))
		{
			if (param == null)
				throw new ArgumentNullException("param");

			param.OptimizeFrom = optimizeFrom;
			param.OptimizeTo = optimizeTo;
			param.OptimizeStep = optimizeStep;

			return param;
		}
	}
}