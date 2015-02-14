namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Единицы измерения.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum UnitTypes
	{
		/// <summary>
		/// Абсолютное значение. Шаг изменения равен заданному числу.
		/// </summary>
		[EnumMember]
		Absolute,

		/// <summary>
		/// Проценты. Шаг изменения – одна сотая процента.
		/// </summary>
		[EnumMember]
		Percent,

		/// <summary>
		/// Пункт цены инструмента.
		/// </summary>
		[EnumMember]
		Point,

		/// <summary>
		/// Шаг цены инструмента.
		/// </summary>
		[EnumMember]
		Step,

		/// <summary>
		/// Лимитированное значение. Данная единица изменения позволяет задавать конкретное число,
		/// которое не может быть использовано в арифметических операциях <see cref="Unit"/>.
		/// </summary>
		[EnumMember]
		Limit,
	}

	/// <summary>
	/// Специальный класс, позволяющий задавать величины в процентном, абсолютном, пунктовым и пипсовых значениях.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[Ignore(FieldName = "IsDisposed")]
	public class Unit : Equatable<Unit>, IOperable<Unit>
	{
		/// <summary>
		/// Создать величину.
		/// </summary>
		public Unit()
		{
		}

		/// <summary>
		/// Создать абсолютную величину <see cref="UnitTypes.Absolute"/>.
		/// </summary>
		/// <param name="value">Значение.</param>
		public Unit(decimal value)
			: this(value, UnitTypes.Absolute)
		{
		}

		/// <summary>
		/// Создать величину типа <see cref="UnitTypes.Absolute"/> или <see cref="UnitTypes.Percent"/>.
		/// </summary>
		/// <param name="value">Значение.</param>
		/// <param name="type">Единица измерения.</param>
		public Unit(decimal value, UnitTypes type)
			: this(value, type, null)
		{
		}

		/// <summary>
		/// Создать величину типа <see cref="UnitTypes.Point"/> или <see cref="UnitTypes.Step"/>.
		/// </summary>
		/// <param name="value">Значение.</param>
		/// <param name="type">Единица измерения.</param>
		/// <param name="getTypeValue">Обработчик, возвращающие значение, ассоциированное с <see cref="Type"/> (шаг цены или объема).</param>
		public Unit(decimal value, UnitTypes type, Func<UnitTypes, decimal?> getTypeValue)
		{
			// mika Данную проверку лучше делать при арифметических действиях
			//
			//if (type == UnitTypes.Point || type == UnitTypes.Step)
			//{
			//    if (security == null)
			//        throw new ArgumentException("Единица измерения не может быть '{0}' так как не передана информация об инструменте.".Put(type), "type");
			//}

			Value = value;
			Type = type;
			GetTypeValue = getTypeValue;
		}

		/// <summary>
		/// Единица измерения.
		/// </summary>
		[DataMember]
		public UnitTypes Type { get; set; }

		/// <summary>
		/// Значение.
		/// </summary>
		[DataMember]
		public decimal Value { get; set; }

		[field: NonSerialized]
		private Func<UnitTypes, decimal?> _getTypeValue;

		/// <summary>
		/// Обработчик, возвращающие значение, ассоциированное с <see cref="Type"/> (шаг цены или объема).
		/// </summary>
		[Ignore]
		public Func<UnitTypes, decimal?> GetTypeValue
		{
			get { return _getTypeValue; }
			set { _getTypeValue = value; }
		}

		///<summary>
		/// Создать копию объекта <see cref="Unit" />.
		///</summary>
		///<returns>Копия.</returns>
		public override Unit Clone()
		{
			return new Unit
			{
				Type = Type,
				Value = Value,
				GetTypeValue = GetTypeValue,
			};
		}

		/// <summary>
		/// Сравнить величину с другой.
		/// </summary>
		/// <param name="other">Другая величина, с которой необходимо сравнивать.</param>
		/// <returns>Результат сравнения величин.</returns>
		public override int CompareTo(Unit other)
		{
			if (this == other)
				return 0;

			if (this < other)
				return -1;

			return 1;
		}

		/// <summary>
		/// Привести <see cref="decimal"/> значение к объекту <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> значение.</param>
		/// <returns>Объект <see cref="Unit"/>.</returns>
		public static implicit operator Unit(decimal value)
		{
			return new Unit(value);
		}

		/// <summary>
		/// Привести <see cref="int"/> значение к объекту <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="int"/> значение.</param>
		/// <returns>Объект <see cref="Unit"/>.</returns>
		public static implicit operator Unit(int value)
		{
			return new Unit(value);
		}

		/// <summary>
		/// Привести объект <see cref="Unit"/> к <see cref="decimal"/> значению.
		/// </summary>
		/// <param name="unit">Объект <see cref="Unit"/>.</param>
		/// <returns><see cref="decimal"/> значение.</returns>
		public static explicit operator decimal(Unit unit)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			switch (unit.Type)
			{
				case UnitTypes.Limit:
				case UnitTypes.Absolute:
					return unit.Value;
				case UnitTypes.Percent:
					throw new ArgumentException(LocalizedStrings.PercentagesConvert, "unit");
				case UnitTypes.Point:
					return unit.Value * unit.SafeGetTypeValue(null);
				case UnitTypes.Step:
					return unit.Value * unit.SafeGetTypeValue(null);
				default:
					throw new ArgumentOutOfRangeException("unit");
			}
		}

		/// <summary>
		/// Привести <see cref="double"/> значение к объекту <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="double"/> значение.</param>
		/// <returns>Объект <see cref="Unit"/>.</returns>
		public static implicit operator Unit(double value)
		{
			return (decimal)value;
		}

		/// <summary>
		/// Привести объект <see cref="Unit"/> к <see cref="double"/> значению.
		/// </summary>
		/// <param name="unit">Объект <see cref="Unit"/>.</param>
		/// <returns><see cref="double"/> значение.</returns>
		public static explicit operator double(Unit unit)
		{
			return (double)(decimal)unit;
		}

		///// <summary>
		///// Привести <see cref="string"/> значение к объекту <see cref="Unit"/>.
		///// </summary>
		///// <param name="value"><see cref="string"/> значение.</param>
		///// <returns>Объект <see cref="Unit"/>.</returns>
		//public static implicit operator Unit(string value)
		//{
		//    return value.ToUnit(null);
		//}

		private decimal SafeGetTypeValue(Func<UnitTypes, decimal?> getTypeValue)
		{
			var func = GetTypeValue ?? getTypeValue;

			if (func == null)
				throw new InvalidOperationException("Обработчик получения значения не установлен.");

			var value = func(Type);

			if (value != null && value != 0)
				return value.Value;

			if (getTypeValue == null)
				throw new ArgumentNullException("getTypeValue");

			value = getTypeValue(Type);

			if (value == null || value == 0)
				throw new InvalidOperationException(LocalizedStrings.Str1291);

			return value.Value;
		}

		private static Unit CreateResult(Unit u1, Unit u2, Func<decimal, decimal, decimal> operation, Func<decimal, decimal, decimal> percentOperation)
		{
			//  предовратить вызов переопределенного оператора
			//if (u1 == null)
			if (u1.IsNull())
				throw new ArgumentNullException("u1");

			//if (u2 == null)
			if (u2.IsNull())
				throw new ArgumentNullException("u2");

			if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.LimitedValueNotMath);

			if (operation == null)
				throw new ArgumentNullException("operation");

			if (percentOperation == null)
				throw new ArgumentNullException("percentOperation");

			//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
			//	throw new ArgumentException("У одной из величин не установлено получение значения.");

			//if (u1.GetTypeValue != null && u2.GetTypeValue != null && u1.GetTypeValue != u2.GetTypeValue)
			//	throw new ArgumentException(LocalizedStrings.Str614Params.Put(u1.Security.Id, u2.Security.Id));

			var result = new Unit
			{
				Type = u1.Type,
				GetTypeValue = u1.GetTypeValue ?? u2.GetTypeValue
			};

			if (u1.Type == u2.Type)
			{
				result.Value = operation(u1.Value, u2.Value);
			}
			else
			{
				if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
				{
					result.Type = u1.Type == UnitTypes.Percent ? u2.Type : u1.Type;

					var nonPerValue = u1.Type == UnitTypes.Percent ? u2.Value : u1.Value;
					var perValue = u1.Type == UnitTypes.Percent ? u1.Value : u2.Value;

					result.Value = percentOperation(nonPerValue, perValue * nonPerValue.Abs() / 100.0m);
				}
				else
				{
					var value = operation((decimal)u1, (decimal)u2);

					switch (result.Type)
					{
						case UnitTypes.Absolute:
							break;
						case UnitTypes.Point:
							value /= u1.SafeGetTypeValue(result.GetTypeValue);
							break;
						case UnitTypes.Step:
							value /= u1.SafeGetTypeValue(result.GetTypeValue);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					result.Value = value;
				}
			}

			return result;
		}

		/// <summary>
		/// Сложить два объекта <see cref="Unit"/>.
		/// </summary>
		/// <param name="u1">Первый объект <see cref="Unit"/>.</param>
		/// <param name="u2">Второй объект <see cref="Unit"/>.</param>
		/// <returns>Результат сложения.</returns>
		public static Unit operator +(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 + v2, (nonPer, per) => nonPer + per);
		}

		/// <summary>
		/// Перемножить два объекта <see cref="Unit"/>.
		/// </summary>
		/// <param name="u1">Первый объект <see cref="Unit"/>.</param>
		/// <param name="u2">Второй объект <see cref="Unit"/>.</param>
		/// <returns>Результат перемножения.</returns>
		public static Unit operator *(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 * v2, (nonPer, per) => nonPer * per);
		}

		/// <summary>
		/// Вычесть одну величину <see cref="Unit"/> из другой.
		/// </summary>
		/// <param name="u1">Первый объект <see cref="Unit"/>.</param>
		/// <param name="u2">Второй объект <see cref="Unit"/>.</param>
		/// <returns>Результат вычитания.</returns>
		public static Unit operator -(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 - v2, (nonPer, per) => (u1.Type == UnitTypes.Percent ? (per - nonPer) : (nonPer - per)));
		}

		/// <summary>
		/// Поделить одну величину <see cref="Unit"/> на другую.
		/// </summary>
		/// <param name="u1">Первый объект <see cref="Unit"/>.</param>
		/// <param name="u2">Второй объект <see cref="Unit"/>.</param>
		/// <returns>Результат деления.</returns>
		public static Unit operator /(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 / v2, (nonPer, per) => u1.Type == UnitTypes.Percent ? per / nonPer : nonPer / per);
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="Unit"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Value.GetHashCode();
		}

		/// <summary>
		/// Сравнить величину на эквивалентность с другой.
		/// </summary>
		/// <param name="other">Другая величина, с которой необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другая величина равна текущей, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(Unit other)
		{
			//var retVal = Type == other.Type && Value == other.Value;

			//if (Type == UnitTypes.Percent || Type == UnitTypes.Absolute || Type == UnitTypes.Limit)
			//	return retVal;

			//return retVal && CheckGetTypeValue(true) == other.CheckGetTypeValue(true);

			if (Type == other.Type)
				return Value == other.Value;

			if (Type == UnitTypes.Percent || other.Type == UnitTypes.Percent)
				return false;

			var curr = this;

			if (other.Type == UnitTypes.Absolute)
				curr = Convert(other.Type);
			else
				other = other.Convert(Type);

			return curr.Value == other.Value;
		}

		/// <summary>
		/// Сравнить величину на эквивалентность с другой.
		/// </summary>
		/// <param name="obj">Другая величина, с которой необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другая величина равна текущей, иначе, <see langword="false"/>.</returns>
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		/// <summary>
		/// Сравнить две величины на неравенство (если величины разных типов, то для сравнения будет применена конвертация).
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если величины не равны, иначе, <see langword="false"/>.</returns>
		public static bool operator !=(Unit u1, Unit u2)
		{
			return !(u1 == u2);
		}

		/// <summary>
		/// Сравнить две величины на равенство (если величины разных типов, то для сравнения будет применена конвертация).
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если величины равны, иначе, <see langword="false"/>.</returns>
		public static bool operator ==(Unit u1, Unit u2)
		{
			if (ReferenceEquals(u1, null))
				return u2.IsNull();

			if (ReferenceEquals(u2, null))
				return false;

			return u1.OnEquals(u2);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			switch (Type)
			{
				case UnitTypes.Percent:
					return Value + "%";
				case UnitTypes.Absolute:
					return Value.To<string>();
				case UnitTypes.Step:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "ш" : "s");
				case UnitTypes.Point:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "п" : "p");
				case UnitTypes.Limit:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "л" : "l");
				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownUnitMeasurement.Put(Type));
			}
		}

		/// <summary>
		/// Перевести величину в другой тип измерения.
		/// </summary>
		/// <param name="destinationType">Тип измерения, в который необходимо перевести.</param>
		/// <returns>Сконвертированная величина.</returns>
		public Unit Convert(UnitTypes destinationType)
		{
			return Convert(destinationType, GetTypeValue);
		}

		/// <summary>
		/// Перевести величину в другой тип измерения.
		/// </summary>
		/// <param name="destinationType">Тип измерения, в который необходимо перевести.</param>
		/// <param name="getTypeValue">Обработчик, возвращающие значение, ассоциированное с <see cref="Type"/> (шаг цены или объема).</param>
		/// <returns>Сконвертированная величина.</returns>
		public Unit Convert(UnitTypes destinationType, Func<UnitTypes, decimal?> getTypeValue)
		{
			if (Type == destinationType)
				return Clone();

			if (Type == UnitTypes.Percent || destinationType == UnitTypes.Percent)
				throw new InvalidOperationException(LocalizedStrings.PercentagesConvert);

			var value = (decimal)this;

			if (destinationType == UnitTypes.Point || destinationType == UnitTypes.Step)
			{
				if (getTypeValue == null)
					throw new ArgumentException(LocalizedStrings.UnitHandlerNotSet, "destinationType");

				switch (destinationType)
				{
					case UnitTypes.Point:
						var point = getTypeValue(UnitTypes.Point);

						if (point == null || point == 0)
							throw new InvalidOperationException("Price step cost is equal to zero.".Translate());

						value /= point.Value;
						break;
					case UnitTypes.Step:
						var step = getTypeValue(UnitTypes.Step);

						if (step == null || step == 0)
							throw new InvalidOperationException(LocalizedStrings.Str1546);

						value /= step.Value;
						break;
				}
			}

			return new Unit(value, destinationType, getTypeValue);
		}

		/// <summary>
		/// Проверить, является ли первая величина больше второй.
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если первая величина больше второй, иначе, <see langword="false"/>.</returns>
		public static bool operator >(Unit u1, Unit u2)
		{
			if (u1.IsNull())
				throw new ArgumentNullException("u1");

			if (u2.IsNull())
				throw new ArgumentNullException("u2");

			//if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
			//	throw new ArgumentException("Лимитированное значение не может участвовать в арифметических операциях.");

			//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
			//	throw new ArgumentException("У одной из величин не установлено получение значения.");

			if (u1.Type != u2.Type)
			{
				if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
					throw new ArgumentException(LocalizedStrings.PercentagesCannotCompare.Put(u1, u2));

				if (u2.Type == UnitTypes.Absolute)
					u1 = u1.Convert(u2.Type);
				else
					u2 = u2.Convert(u1.Type);
			}

			return u1.Value > u2.Value;
		}

		/// <summary>
		/// Проверить, является ли первая величина больше или равной второй.
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если первая величина больше или равной второй, иначе, <see langword="false"/>.</returns>
		public static bool operator >=(Unit u1, Unit u2)
		{
			return u1 == u2 || u1 > u2;
		}

		/// <summary>
		/// Проверить, является ли первая величина меньше второй.
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если первая величина меньше второй, иначе, <see langword="false"/>.</returns>
		public static bool operator <(Unit u1, Unit u2)
		{
			return u1 != u2 && !(u1 > u2);
		}

		/// <summary>
		/// Проверить, является ли первая величина меньше или равной второй.
		/// </summary>
		/// <param name="u1">Первая величина.</param>
		/// <param name="u2">Вторая величина.</param>
		/// <returns><see langword="true"/>, если первая величина меньше или равной второй, иначе, <see langword="false"/>.</returns>
		public static bool operator <=(Unit u1, Unit u2)
		{
			return !(u1 > u2);
		}

		/// <summary>
		/// Получить величину с противоположным знаком у значения <see cref="Value"/>.
		/// </summary>
		/// <param name="u">Величина.</param>
		/// <returns>Обратная величина.</returns>
		public static Unit operator -(Unit u)
		{
			if (u == null)
				throw new ArgumentNullException("u");

			return new Unit
			{
				GetTypeValue = u.GetTypeValue,
				Type = u.Type,
				Value = -u.Value
			};
		}

		Unit IOperable<Unit>.Add(Unit other)
		{
			return this + other;
		}

		Unit IOperable<Unit>.Subtract(Unit other)
		{
			return this - other;
		}

		Unit IOperable<Unit>.Multiply(Unit other)
		{
			return this * other;
		}

		Unit IOperable<Unit>.Divide(Unit other)
		{
			return this / other;
		}
	}

	/// <summary>
	/// Вспомогательный класс для <see cref="Unit"/>.
	/// </summary>
	public static class UnitHelper
	{
		/// <summary>
		/// Создать из <see cref="int"/> значения проценты.
		/// </summary>
		/// <param name="value"><see cref="int"/> значение.</param>
		/// <returns>Проценты.</returns>
		public static Unit Percents(this int value)
		{
			return Percents((decimal)value);
		}

		/// <summary>
		/// Создать из <see cref="double"/> значения проценты.
		/// </summary>
		/// <param name="value"><see cref="double"/> значение.</param>
		/// <returns>Проценты.</returns>
		public static Unit Percents(this double value)
		{
			return Percents((decimal)value);
		}

		/// <summary>
		/// Создать из <see cref="decimal"/> значения проценты.
		/// </summary>
		/// <param name="value"><see cref="decimal"/> значение.</param>
		/// <returns>Проценты.</returns>
		public static Unit Percents(this decimal value)
		{
			return new Unit(value, UnitTypes.Percent);
		}

		/// <summary>
		/// Пробразовать строку в <see cref="Unit"/>.
		/// </summary>
		/// <param name="str">Строковое представление <see cref="Unit"/>.</param>
		/// <param name="getTypeValue">Обработчик, возвращающие значение, ассоциированное с <see cref="Type"/> (шаг цены или объема).</param>
		/// <returns>Объект <see cref="Unit"/>.</returns>
		public static Unit ToUnit(this string str, Func<UnitTypes, decimal?> getTypeValue = null)
		{
			if (str.IsEmpty())
				throw new ArgumentNullException("str");

			var lastSymbol = str.Last();

			if (char.IsDigit(lastSymbol))
				return new Unit(str.To<decimal>(), UnitTypes.Absolute);

			var value = str.Substring(0, str.Length - 1).To<decimal>();

			UnitTypes type;

			switch (lastSymbol)
			{
				case 'ш':
				case 's':
					if (getTypeValue == null)
						throw new ArgumentNullException("getTypeValue");

					type = UnitTypes.Step;
					break;
				case 'п':
				case 'p':
					if (getTypeValue == null)
						throw new ArgumentNullException("getTypeValue");
			
					type = UnitTypes.Point;
					break;
				case '%':
					type = UnitTypes.Percent;
					break;
				case 'л':
				case 'l':
					type = UnitTypes.Limit;
					break;
				default:
					throw new ArgumentException(LocalizedStrings.UnknownUnitMeasurement.Put(lastSymbol), "str");
			}

			return new Unit(value, type, getTypeValue);
		}
	}
}