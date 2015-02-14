namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Сообщение поиска инструментов по заданному критерию.
	/// </summary>
	public class SecurityLookupMessage : SecurityMessage, IEquatable<SecurityLookupMessage>
	{
		/// <summary>
		/// Номер транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str230Key)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Типы инструментов.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		public IEnumerable<SecurityTypes> SecurityTypes { get; set; }

		/// <summary>
		/// Создать <see cref="SecurityLookupMessage"/>.
		/// </summary>
		public SecurityLookupMessage()
			: base(MessageTypes.SecurityLookup)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new SecurityLookupMessage
			{
				TransactionId = TransactionId,
				SecurityTypes = SecurityTypes
			};
			
			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Проверить критерии поиска на эквивалентность.
		/// </summary>
		/// <param name="other">Другой критерий поиска, с которым необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если критерии поиска равны, иначе, <see langword="false"/>.</returns>
		public bool Equals(SecurityLookupMessage other)
		{
			if (SecurityId.Equals(other.SecurityId))
				return true;

			if (Name == other.Name && 
				ShortName == other.ShortName && 
				Currency == other.Currency && 
				ExpiryDate == other.ExpiryDate && 
				OptionType == other.OptionType &&
				((SecurityTypes == null && other.SecurityTypes == null) ||
				(SecurityTypes != null && other.SecurityTypes != null && SecurityTypes.SequenceEqual(other.SecurityTypes))) && 
				SettlementDate == other.SettlementDate &&
				Strike == other.Strike &&
				UnderlyingSecurityCode == other.UnderlyingSecurityCode)
				return true;

			return false;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",TransId={0}".Put(TransactionId);
		}
	}
}