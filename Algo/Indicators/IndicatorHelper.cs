namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;

	using StockSharp.Localization;

	/// <summary>
	/// Вспомогательный класс для работы с исдникеаторами.
	/// </summary>
	public static class IndicatorHelper
	{
		/// <summary>
		/// Получить текущее значение индикатора.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <returns>Текущее значение.</returns>
		public static T GetCurrentValue<T>(this BaseIndicator<T> indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			return ((IIndicator)indicator).GetCurrentValue<T>();
		}

		/// <summary>
		/// Получить текущее значение индикатора.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <returns>Текущее значение.</returns>
		public static T GetCurrentValue<T>(this IIndicator indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			return indicator.GetValue<T>(0);
		}

		/// <summary>
		/// Получить значение индикатора по индексу (0 - последнее значение).
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="index">Индекс значения.</param>
		/// <returns>Значение индикатора.</returns>
		public static T GetValue<T>(this BaseIndicator<T> indicator, int index)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			return ((IIndicator)indicator).GetValue<T>(index);
		}

		/// <summary>
		/// Получить значение индикатора по индексу (0 - последнее значение).
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="index">Индекс значения.</param>
		/// <returns>Значение индикатора.</returns>
		public static T GetValue<T>(this IIndicator indicator, int index)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			if (index >= indicator.Container.Count)
			{
				if (index == 0 && typeof(decimal) == typeof(T))
					return 0m.To<T>();
				else
					throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.Str914Params.Put(indicator.Name));
			}

			var value = indicator.Container.GetValue(index).Item2;
			return typeof(IIndicatorValue).IsAssignableFrom(typeof(T)) ? value.To<T>() : value.GetValue<T>();
		}

		/// <summary>
		/// Обновить индикатор ценой закрытия свечи <see cref="Candle.ClosePrice"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="candle">Свеча.</param>
		/// <returns>Новое значение индикатора.</returns>
		public static IIndicatorValue Process(this IIndicator indicator, Candle candle)
		{
			return indicator.Process(new CandleIndicatorValue(indicator, candle));
		}

		/// <summary>
		/// Обновить индикатор числовым значением.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Числовое значение.</param>
		/// <param name="isFinal">Является ли значение окончательным (индикатор окончательно формирует свое значение и более не будет изменяться в данной точке времени). По-умолчанию true.</param>
		/// <returns>Новое значение индикатора.</returns>
		public static IIndicatorValue Process(this IIndicator indicator, decimal value, bool isFinal = true)
		{
			return indicator.Process(new DecimalIndicatorValue(indicator, value) { IsFinal = isFinal });
		}

		internal static void LoadNotNull(this IPersistable obj, SettingsStorage settings, string name)
		{
			var value = settings.GetValue<SettingsStorage>(name);
			if (value != null)
				obj.Load(value);
		}

		/// <summary>
		/// Получить входное значение для <see cref="IIndicatorValue"/>.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="indicatorValue">Значение индикатора.</param>
		/// <returns>Входное значение указанного типа.</returns>
		public static T GetInputValue<T>(this IIndicatorValue indicatorValue)
		{
			var input = indicatorValue.InputValue;

			while (input != null && !input.IsSupport(typeof(T)))
			{
				input = input.InputValue;
			}

			return input == null ? default(T) : input.GetValue<T>();
		}
	}
}