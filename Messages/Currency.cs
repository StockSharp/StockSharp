namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using NetDataContract = System.Runtime.Serialization.DataContractAttribute;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Валюта.
	/// </summary>
	[NetDataContract]
	[Serializable]
	[Ignore(FieldName = "IsDisposed")]
	public class Currency : Equatable<Currency>
	{
		/// <summary>
		/// Создать объект валюты.
		/// </summary>
		public Currency()
		{
			Type = CurrencyTypes.RUB;
		}

		/// <summary>
		/// Тип валюты. По умолчанию, стоит значение <see cref="CurrencyTypes.RUB"/>.
		/// </summary>
		[DataMember]
		public CurrencyTypes Type { get; set; }

		/// <summary>
		/// Значение в единицах <see cref="CurrencyTypes"/>.
		/// </summary>
		[DataMember]
		public decimal Value { get; set; }

		/// <summary>
		/// Создать копию валюты (копирование параметров валюты).
		/// </summary>
		/// <returns>Копия валюты.</returns>
		public override Currency Clone()
		{
			return new Currency { Type = Type, Value = Value };
		}

		/// <summary>
		/// Сравнить на равенство.
		/// </summary>
		/// <param name="other">Валюта, с которой нужно сравнить.</param>
		/// <returns>Результат сравнения.</returns>
		protected override bool OnEquals(Currency other)
		{
			return Type == other.Type && Value == other.Value;
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="Currency"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Value.GetHashCode();
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} {1}".Put(Value, Type);
		}

		/// <summary>
		/// Привести <see cref="decimal"/> значение к объекту <see cref="Currency"/>.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> значение.</param>
		/// <returns>Объект <see cref="Currency"/>.</returns>
		public static implicit operator Currency(decimal value)
		{
			return new Currency { Value = value };
		}

		/// <summary>
		/// Привести объект <see cref="Currency"/> к <see cref="decimal"/> значению.
		/// </summary>
		/// <param name="unit">Объект <see cref="Currency"/>.</param>
		/// <returns><see cref="decimal"/> значение.</returns>
		public static explicit operator decimal(Currency unit)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			return unit.Value;
		}

		/// <summary>
		/// Сложить два объекта <see cref="Currency"/>.
		/// </summary>
		/// <remarks>
		/// Величины должны иметь одинаковый <see cref="Type"/>.
		/// </remarks>
		/// <param name="c1">Первый объект <see cref="Currency"/>.</param>
		/// <param name="c2">Второй объект <see cref="Currency"/>.</param>
		/// <returns>Результат сложения.</returns>
		public static Currency operator +(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 + (decimal)c2;
		}

		/// <summary>
		/// Вычесть одну величину из другой величины.
		/// </summary>
		/// <param name="c1">Первый объект <see cref="Currency"/>.</param>
		/// <param name="c2">Второй объект <see cref="Currency"/>.</param>
		/// <returns>Результат вычитания.</returns>
		public static Currency operator -(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 - (decimal)c2;
		}

		/// <summary>
		/// Умножить одну величину на другую.
		/// </summary>
		/// <param name="c1">Первый объект <see cref="Currency"/>.</param>
		/// <param name="c2">Второй объект <see cref="Currency"/>.</param>
		/// <returns>Результат умножения.</returns>
		public static Currency operator *(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 * (decimal)c2;
		}

		/// <summary>
		/// Поделить одну величину на другую.
		/// </summary>
		/// <param name="c1">Первый объект <see cref="Currency"/>.</param>
		/// <param name="c2">Второй объект <see cref="Currency"/>.</param>
		/// <returns>Результат деления.</returns>
		public static Currency operator /(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 / (decimal)c2;
		}
	}

	/// <summary>
	/// Вспомогательный класс для работы с <see cref="Currency"/>.
	/// </summary>
	public static class CurrencyHelper
	{
		/// <summary>
		/// Привести объект типа <see cref="Decimal"/> к типу <see cref="Currency"/>.
		/// </summary>
		/// <param name="value">Значение валюты.</param>
		/// <param name="type">Тип валюты.</param>
		/// <returns>Валюта.</returns>
		public static Currency ToCurrency(this decimal value, CurrencyTypes type)
		{
			return new Currency { Type = type, Value = value };
		}
	}
}