namespace StockSharp.Algo.Indicators
{
	using System;

	/// <summary>
	/// Смещенное значение индикатора.
	/// </summary>
	public class ShiftedIndicatorValue : SingleIndicatorValue<IIndicatorValue>
	{
		/// <summary>
		/// Создать <see cref="ShiftedIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		public ShiftedIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
		}

		/// <summary>
		/// Создать <see cref="ShiftedIndicatorValue"/>.
		/// </summary>
		/// <param name="shift">Смещение значения индикатора.</param>
		/// <param name="value">Значение индикатора.</param>
		/// <param name="indicator">Индикатор.</param>
		public ShiftedIndicatorValue(IIndicator indicator, int shift, IIndicatorValue value)
			: base(indicator, value)
		{
			Shift = shift;
		}

		/// <summary>
		/// Смещение значения индикатора.
		/// </summary>
		public int Shift { get; private set; }

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		public override bool IsSupport(Type valueType)
		{
			return !IsEmpty && Value.IsSupport(valueType);
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public override T GetValue<T>()
		{
			return base.GetValue<IIndicatorValue>().GetValue<T>();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Измененная копия входного значения.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			throw new NotSupportedException();
		}
	}
}