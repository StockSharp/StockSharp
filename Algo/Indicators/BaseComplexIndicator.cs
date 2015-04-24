namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using MoreLinq;

	/// <summary>
	/// Режимы обработки вложенных индикаторов.
	/// </summary>
	public enum ComplexIndicatorModes
	{
		/// <summary>
		/// Последовательно. Результат выполнения предыдущего индикатора передается в следующий.
		/// </summary>
		Sequence,

		/// <summary>
		/// Параллельно. Результаты выполнения индикаторов не зависят друг от друга.
		/// </summary>
		Parallel,
	}

	/// <summary>
	/// Базовый индикатор, который строится ввиде комбинации нескольких индикаторов.
	/// </summary>
	public abstract class BaseComplexIndicator : BaseIndicator, IComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="BaseComplexIndicator"/>.
		/// </summary>
		/// <param name="innerIndicators">Вложенные индикаторы.</param>
		protected BaseComplexIndicator(params IIndicator[] innerIndicators)
		{
			if (innerIndicators == null)
				throw new ArgumentNullException("innerIndicators");

			if (innerIndicators.Any(i => i == null))
				throw new ArgumentException("innerIndicators");

			InnerIndicators = new List<IIndicator>(innerIndicators);

			Mode = ComplexIndicatorModes.Parallel;
		}

		/// <summary>
		/// Режим обработки вложенных индикаторов. По умолчаннию равно <see cref="ComplexIndicatorModes.Parallel"/>.
		/// </summary>
		[Browsable(false)]
		public ComplexIndicatorModes Mode { get; protected set; }

		/// <summary>
		/// Вложенные индикаторы.
		/// </summary>
		[Browsable(false)]
		protected IList<IIndicator> InnerIndicators { get; private set; }

		IEnumerable<IIndicator> IComplexIndicator.InnerIndicators
		{
			get { return InnerIndicators; }
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return InnerIndicators.All(i => i.IsFormed); }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = new ComplexIndicatorValue(this);

			foreach (var indicator in InnerIndicators)
			{
				var result = indicator.Process(input);

				value.InnerValues.Add(indicator, result);

				if (Mode == ComplexIndicatorModes.Sequence)
				{
					if (!indicator.IsFormed)
					{
						break;
					}

					input = result;
				}
			}

			return value;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			InnerIndicators.ForEach(i => i.Reset());
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			var index = 0;

			foreach (var indicator in InnerIndicators)
			{
				var innerSettings = new SettingsStorage();
				indicator.Save(innerSettings);
				settings.SetValue(indicator.Name + index, innerSettings);
				index++;
			}
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			var index = 0;

			foreach (var indicator in InnerIndicators)
			{
				indicator.Load(settings.GetValue<SettingsStorage>(indicator.Name + index));
				index++;
			}
		}
	}
}
