namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Базовый индикатор.
	/// </summary>
	public abstract class BaseIndicator : Cloneable<IIndicator>, IIndicator
	{
		/// <summary>
		/// Инициализировать <see cref="BaseIndicator"/>, который работает с данными типа <see cref="decimal"/>.
		/// </summary>
		protected BaseIndicator()
		{
			_name = GetType().GetDisplayName();
		}

		private Guid _id = Guid.NewGuid();

		/// <summary>
		/// Уникальный идентификатор.
		/// </summary>
		[Browsable(false)]
		public Guid Id
		{
			get { return _id; }
		}

		private string _name;

		/// <summary>
		/// Название индикатора.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str908Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual string Name
		{
			get { return _name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_name = value;
			}
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public virtual void Reset()
		{
			IsFormed = false;
			Container.ClearValues();
			Reseted.SafeInvoke();
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Id", _id);
			storage.SetValue("Name", Name);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Load(SettingsStorage storage)
		{
			_id = storage.GetValue<Guid>("Id");
			Name = storage.GetValue<string>("Name");
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsFormed { get; protected set; }

		private readonly IIndicatorContainer _container = new IndicatorContainer();

		/// <summary>
		/// Контейнер, хранящий данные индикатора.
		/// </summary>
		[Browsable(false)]
		public IIndicatorContainer Container
		{
			get { return _container; }
		}

		/// <summary>
		/// Событие об изменении индикатора (например, добавлено новое значение).
		/// </summary>
		public event Action<IIndicatorValue, IIndicatorValue> Changed;

		/// <summary>
		/// Событие о сбросе состояния индикатора на первоначальное. Событие вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public event Action Reseted;

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		public virtual IIndicatorValue Process(IIndicatorValue input)
		{
			var result = OnProcess(input);

			result.InputValue = input;
			//var result = value as IIndicatorValue ?? input.SetValue(value);

			if (input.IsFinal)
			{
				result.IsFinal = input.IsFinal;
				Container.AddValue(input, result);
			}

			if (IsFormed && !result.IsEmpty)
				RaiseChangedEvent(input, result);

			return result;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected abstract IIndicatorValue OnProcess(IIndicatorValue input);

		/// <summary>
		/// Вызвать событие <see cref="Changed"/>.
		/// </summary>
		/// <param name="input">Входное значение индикатора.</param>
		/// <param name="result">Результирующее значение индикатора.</param>
		protected void RaiseChangedEvent(IIndicatorValue input, IIndicatorValue result)
		{
			if (input == null)
				throw new ArgumentNullException("input");

			if (result == null)
				throw new ArgumentNullException("result");

			Changed.SafeInvoke(input, result);
		}

		/// <summary>
		/// Создать копию <see cref="IIndicator"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override IIndicator Clone()
		{
			return PersistableHelper.Clone(this);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}
