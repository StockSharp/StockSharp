namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Идентификатор инструмента.
	/// </summary>
	public struct SecurityId : IEquatable<SecurityId>
	{
		private string _securityCode;

		/// <summary>
		/// Код инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str349Key)]
		[DescriptionLoc(LocalizedStrings.Str349Key, true)]
		[MainCategory]
		public string SecurityCode
		{
			get { return _securityCode; }
			set { _securityCode = value; }
		}

		private string _boardCode;

		/// <summary>
		/// Код электронной площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode
		{
			get { return _boardCode; }
			set { _boardCode = value; }
		}

		private object _native;

		/// <summary>
		/// Внутренний идентификатор торговой системы.
		/// </summary>
		public object Native
		{
			get { return _native; }
			set { _native = value; }
		}

		private SecurityTypes? _securityType;

		/// <summary>
		/// Тип инструмента.
		/// </summary>
		public SecurityTypes? SecurityType
		{
			get { return _securityType; }
			set { _securityType = value; }
		}

		/// <summary>
		/// Идентификатор в формате SEDOL (Stock Exchange Daily Official List).
		/// </summary>
		[DataMember]
		[DisplayName("SEDOL")]
		[DescriptionLoc(LocalizedStrings.Str351Key)]
		public string Sedol { get; set; }

		/// <summary>
		/// Идентификатор в формате CUSIP (Committee on Uniform Securities Identification Procedures).
		/// </summary>
		[DataMember]
		[DisplayName("CUSIP")]
		[DescriptionLoc(LocalizedStrings.Str352Key)]
		public string Cusip { get; set; }

		/// <summary>
		/// Идентификатор в формате ISIN (International Securities Identification Number).
		/// </summary>
		[DataMember]
		[DisplayName("ISIN")]
		[DescriptionLoc(LocalizedStrings.Str353Key)]
		public string Isin { get; set; }

		/// <summary>
		/// Идентификатор в формате RIC (Reuters Instrument Code).
		/// </summary>
		[DataMember]
		[DisplayName("RIC")]
		[DescriptionLoc(LocalizedStrings.Str354Key)]
		public string Ric { get; set; }

		/// <summary>
		/// Идентификатор в формате Bloomberg.
		/// </summary>
		[DataMember]
		[DisplayName("Bloomberg")]
		[DescriptionLoc(LocalizedStrings.Str355Key)]
		public string Bloomberg { get; set; }

		/// <summary>
		/// Идентификатор в формате IQFeed.
		/// </summary>
		[DataMember]
		[DisplayName("IQFeed")]
		[DescriptionLoc(LocalizedStrings.Str356Key)]
		public string IQFeed { get; set; }

		/// <summary>
		/// Идентификатор в формате Interactive Brokers.
		/// </summary>
		[DataMember]
		[DisplayName("InteractiveBrokers")]
		[DescriptionLoc(LocalizedStrings.Str357Key)]
		[Nullable]
		public int? InteractiveBrokers { get; set; }

		/// <summary>
		/// Идентификатор в формате Plaza.
		/// </summary>
		[DataMember]
		[DisplayName("Plaza")]
		[DescriptionLoc(LocalizedStrings.Str358Key)]
		public string Plaza { get; set; }

		private int _hashCode;
		
		/// <summary>
		/// Рассчитать хеш-код объекта.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return EnsureGetHashCode();
		}

		private int EnsureGetHashCode()
		{
			if (_hashCode == 0)
			{
				_hashCode = _native != null
					? _native.GetHashCode()
					: (_securityCode + _boardCode).ToLowerInvariant().GetHashCode();
			}

			return _hashCode;
		}

		/// <summary>
		/// Сравнить идентификатор инструмента на эквивалентность.
		/// </summary>
		/// <param name="other">Другой идентификатор для сравнения.</param>
		/// <returns><see langword="true"/>, если идентификаторы эквивалентны, иначе, <see langword="false"/>.</returns>
		public override bool Equals(object other)
		{
			return Equals((SecurityId)other);
		}

		/// <summary>
		/// Сравнить идентификатор инструмента на эквивалентность.
		/// </summary>
		/// <param name="other">Другой идентификатор для сравнения.</param>
		/// <returns><see langword="true"/>, если идентификаторы эквивалентны, иначе, <see langword="false"/>.</returns>
		public bool Equals(SecurityId other)
		{
			if (EnsureGetHashCode() != other.EnsureGetHashCode())
				return false;

			if (_native == null)
				return _securityCode.CompareIgnoreCase(other._securityCode) && _boardCode.CompareIgnoreCase(other._boardCode);

			return _native.Equals(other.Native);
		}

		/// <summary>
		/// Сравнить на неравенство два идентификатора.
		/// </summary>
		/// <param name="left">Левый операнд.</param>
		/// <param name="right">Правый операнд.</param>
		/// <returns><see langword="true"/>, если идентификаторы не эквивалентны, иначе, <see langword="false"/>.</returns>
		public static bool operator !=(SecurityId left, SecurityId right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Сравнить на равенство два идентификатора.
		/// </summary>
		/// <param name="left">Левый операнд.</param>
		/// <param name="right">Правый операнд.</param>
		/// <returns><see langword="true"/>, если идентификаторы эквивалентны, иначе, <see langword="false"/>.</returns>
		public static bool operator ==(SecurityId left, SecurityId right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "S#:{0}@{1}, Native:{2},Type:{3}".Put(SecurityCode, BoardCode, Native, SecurityType);
		}
	}
}