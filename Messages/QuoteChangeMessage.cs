namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	using Ecng.Common;

	/// <summary>
	/// Сообщение, содержащее данные по котировкам.
	/// </summary>
	public sealed class QuoteChangeMessage : Message
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		private IEnumerable<QuoteChange> _bids = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Котировки на покупку.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str281Key)]
		[DescriptionLoc(LocalizedStrings.Str282Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Bids
		{
			get { return _bids; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_bids = value;
			}
		}

		private IEnumerable<QuoteChange> _asks = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Котировки на продажу.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str283Key)]
		[DescriptionLoc(LocalizedStrings.Str284Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Asks
		{
			get { return _asks; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_asks = value;
			}
		}

		/// <summary>
		/// Серверное время изменения.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Отсортированы ли котировки по цене (<see cref="Bids"/> по убыванию, <see cref="Asks"/> по возрастанию).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str285Key)]
		[DescriptionLoc(LocalizedStrings.Str285Key, true)]
		[MainCategory]
		public bool IsSorted { get; set; }

		/// <summary>
		/// Создать <see cref="QuoteChangeMessage"/>.
		/// </summary>
		public QuoteChangeMessage()
			: base(MessageTypes.QuoteChange)
		{
		}

		/// <summary>
		/// Создать копию объекта.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			var clone = new QuoteChangeMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				Bids = Bids.Select(q => q.Clone()).ToArray(),
				Asks = Asks.Select(q => q.Clone()).ToArray(),
				ServerTime = ServerTime,
				IsSorted = IsSorted
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",T(S)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(ServerTime);
		}
	}
}