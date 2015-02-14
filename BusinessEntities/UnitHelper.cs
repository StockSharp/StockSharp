namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Вспомогательный класс для <see cref="Unit"/>.
	/// </summary>
	public static class UnitHelper2
	{
		/// <summary>
		/// Создать из <see cref="int"/> значения пипсы.
		/// </summary>
		/// <param name="value"><see cref="int"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по шагу цены.</param>
		/// <returns>Пипсы.</returns>
		public static Unit Pips(this int value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// Создать из <see cref="double"/> значения пипсы.
		/// </summary>
		/// <param name="value"><see cref="double"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по шагу цены.</param>
		/// <returns>Пипсы.</returns>
		public static Unit Pips(this double value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// Создать из <see cref="decimal"/> значения пипсы.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по шагу цены.</param>
		/// <returns>Пипсы.</returns>
		public static Unit Pips(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return new Unit(value, UnitTypes.Step, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// Создать из <see cref="int"/> значения пункты.
		/// </summary>
		/// <param name="value"><see cref="int"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по стоимости шага цены.</param>
		/// <returns>Пункты.</returns>
		public static Unit Points(this int value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// Создать из <see cref="double"/> значения пункты.
		/// </summary>
		/// <param name="value"><see cref="double"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по стоимости шага цены.</param>
		/// <returns>Пункты.</returns>
		public static Unit Points(this double value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// Создать из <see cref="decimal"/> значения пункты.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> значение.</param>
		/// <param name="security">Инструмент, из которого берется информация по стоимости шага цены.</param>
		/// <returns>Пункты.</returns>
		public static Unit Points(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return new Unit(value, UnitTypes.Point, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// Пробразовать строку в <see cref="Unit"/>.
		/// </summary>
		/// <param name="str">Строковое представление <see cref="Unit"/>.</param>
		/// <param name="security">Информация по инструменту. Необходима при использовании <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Объект <see cref="Unit"/>.</returns>
		public static Unit ToUnit2(this string str, Security security = null)
		{
			return str.ToUnit(t => GetTypeValue(security, t));
		}

		/// <summary>
		/// Перевести величину в другой тип измерения.
		/// </summary>
		/// <param name="unit">Исходная величина.</param>
		/// <param name="destinationType">Тип измерения, в который необходимо перевести.</param>
		/// <param name="security">Информация по инструменту. Необходима при использовании <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Сконвертированная величина.</returns>
		public static Unit Convert(this Unit unit, UnitTypes destinationType, Security security)
		{
			return unit.Convert(destinationType, type => GetTypeValue(security, type));
		}

		internal static decimal ShrinkPrice(this Security security, decimal price)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (security.PriceStep == 0)
				throw new ArgumentException(LocalizedStrings.Str1546, "security");

			return price.Round(security.PriceStep, security.Decimals, null);
		}

		/// <summary>
		/// Установить для величины свойство <see cref="Unit.GetTypeValue"/>.
		/// </summary>
		/// <param name="unit">Величина.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Величина.</returns>
		public static Unit SetSecurity(this Unit unit, Security security)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			unit.GetTypeValue = type => GetTypeValue(security, type);

			return unit;
		}

		private static decimal? GetTypeValue(Security security, UnitTypes type)
		{
			switch (type)
			{
				case UnitTypes.Point:
					if (security == null)
						throw new ArgumentNullException("security");

					return security.StepPrice;
				case UnitTypes.Step:
					if (security == null)
						throw new ArgumentNullException("security");

					return security.PriceStep;
				default:
					throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.Str1291);
			}
		}
	}
}