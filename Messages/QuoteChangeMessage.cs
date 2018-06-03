#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: QuoteChangeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Messages containing quotes.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public sealed class QuoteChangeMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		private IEnumerable<QuoteChange> _bids = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to buy.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str281Key)]
		[DescriptionLoc(LocalizedStrings.Str282Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Bids
		{
			get => _bids;
			set => _bids = value ?? throw new ArgumentNullException(nameof(value));
		}

		private IEnumerable<QuoteChange> _asks = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to sell.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str283Key)]
		[DescriptionLoc(LocalizedStrings.Str284Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Asks
		{
			get => _asks;
			set => _asks = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Change server time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Flag sorted by price quotes (<see cref="QuoteChangeMessage.Bids"/> by descending, <see cref="QuoteChangeMessage.Asks"/> by ascending).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str285Key)]
		[DescriptionLoc(LocalizedStrings.Str285Key, true)]
		[MainCategory]
		public bool IsSorted { get; set; }

		/// <summary>
		/// The quote change was built by level1.
		/// </summary>
		[Browsable(false)]
		public bool IsByLevel1 { get; set; }

		/// <summary>
		/// The quote change contains filtered quotes.
		/// </summary>
		[Browsable(false)]
		public bool IsFiltered { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteChangeMessage"/>.
		/// </summary>
		public QuoteChangeMessage()
			: base(MessageTypes.QuoteChange)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new QuoteChangeMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				Bids = Bids.Select(q => q.Clone()).ToArray(),
				Asks = Asks.Select(q => q.Clone()).ToArray(),
				ServerTime = ServerTime,
				IsSorted = IsSorted,
				Currency = Currency,
				IsByLevel1 = IsByLevel1,
				IsFiltered = IsFiltered,
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff}";
		}
	}
}