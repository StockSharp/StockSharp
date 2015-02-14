namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Интерфейс, описывающий параметр статистики.
	/// </summary>
	public interface IStatisticParameter : IPersistable
	{
		/// <summary>
		/// Название параметра.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Текущее значение параметра.
		/// </summary>
		object Value { get; }

		/// <summary>
		/// Отображаемое название параметра.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Описание параметра.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Категория.
		/// </summary>
		string Category { get; }

		/// <summary>
		/// Событие изменения <see cref="Value"/>.
		/// </summary>
		event Action ValueChanged;

		/// <summary>
		/// Сбросить значение параметра.
		/// </summary>
		void Reset();
	}

	/// <summary>
	/// Интерфейс, описывающий параметр статистики.
	/// </summary>
	/// <typeparam name="TValue">Тип значения параметра.</typeparam>
	public interface IStatisticParameter<TValue> : IStatisticParameter
	{
		/// <summary>
		/// Текущее значение параметра.
		/// </summary>
		new TValue Value { get; }
	}

	/// <summary>
	/// Базовый параметр статистики.
	/// </summary>
	/// <typeparam name="TValue">Тип значения параметра.</typeparam>
	public abstract class BaseStatisticParameter<TValue> : NotifiableObject, IStatisticParameter<TValue>
		where TValue : IComparable<TValue>
	{
		/// <summary>
		/// Инициализировать <see cref="BaseStatisticParameter{TValue}"/>.
		/// </summary>
		protected BaseStatisticParameter()
		{
			var type = GetType();
			_name = type.Name.Replace("Parameter", string.Empty);

			DisplayName = type.GetDisplayName(GetReadableName(_name));
			Description = type.GetDescription(DisplayName);
			Category = type.GetCategory();
		}

		private string _name;

		/// <summary>
		/// Название параметра.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set
			{
				if (_name == value)
					return;

				_name = value;
				this.Notify("Name");
			}
		}

		/// <summary>
		/// Отображаемое название параметра.
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Описание параметра.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Категория.
		/// </summary>
		public string Category { get; private set; }

		private TValue _value;

		/// <summary>
		/// Текущее значение параметра.
		/// </summary>
		public virtual TValue Value
		{
			get { return _value; }
			protected set
			{
				if (_value.CompareTo(value) == 0)
					return;

				_value = value;
				RaiseValueChanged();
			}
		}

		private static string GetReadableName(string name)
		{
			var index = 1;

			while (index < (name.Length - 1))
			{
				if (char.IsUpper(name[index]))
				{
					name = name.Insert(index, " ");
					index += 2;
				}
				else
					index++;
			}

			return name;
		}

		/// <summary>
		/// Текущее значение параметра.
		/// </summary>
		object IStatisticParameter.Value
		{
			get { return Value; }
		}

		/// <summary>
		/// Событие изменения <see cref="Value"/>.
		/// </summary>
		public virtual event Action ValueChanged;

		/// <summary>
		/// Сбросить значение параметра.
		/// </summary>
		public virtual void Reset()
		{
			Value = default(TValue);
		}

		/// <summary>
		/// Вызвать событие <see cref="ValueChanged"/>.
		/// </summary>
		private void RaiseValueChanged()
		{
			ValueChanged.SafeInvoke();
			this.Notify("Value");
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Value = storage.GetValue("Value", default(TValue));
		}
	
		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Value", Value);
		}
	}
}