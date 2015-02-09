namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Комплексное значение индикатора <see cref="IComplexIndicator"/>, которое получается в результате вычисления.
	/// </summary>
	public class ComplexIndicatorValue : IIndicatorValue
	{
		/// <summary>
		/// Создать <see cref="ComplexIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		public ComplexIndicatorValue(IIndicator indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			Indicator = indicator;
			InnerValues = new Dictionary<IIndicator, IIndicatorValue>();
			IsFormed = indicator.IsFormed;
		}

		/// <summary>
		/// Индикатор.
		/// </summary>
		public IIndicator Indicator { get; private set; }

		/// <summary>
		/// Значение индикатора отсутствует.
		/// </summary>
		public bool IsEmpty { get; set; }

		/// <summary>
		/// Является ли значение окончательным (индикатор окончательно формирует свое значение и более не будет изменяться в данной точке времени).
		/// </summary>
		public bool IsFinal { get; set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public bool IsFormed { get; private set; }

		/// <summary>
		/// Входное значение.
		/// </summary>
		public IIndicatorValue InputValue { get; set; }

		/// <summary>
		/// Вложенные значения.
		/// </summary>
		public IDictionary<IIndicator, IIndicatorValue> InnerValues { get; private set; }

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		bool IIndicatorValue.IsSupport(Type valueType)
		{
			return InnerValues.Any(v => v.Value.IsSupport(valueType));
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public virtual T GetValue<T>()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Измененная копия входного значения.</returns>
		public virtual IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Сравнить с другим значением индикатора.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns>Код сравнения.</returns>
		public virtual int CompareTo(IIndicatorValue other)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Сравнить с другим значением индикатора.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns>Код сравнения.</returns>
		public int CompareTo(object other)
		{
			throw new NotSupportedException();
		}
	}
}