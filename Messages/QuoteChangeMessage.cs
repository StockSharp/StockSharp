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
	using System.Linq;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Order book states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum QuoteChangeStates
	{
		/// <summary>
		/// Snapshot started.
		/// </summary>
		[EnumMember]
		SnapshotStarted,

		/// <summary>
		/// Snapshot building.
		/// </summary>
		[EnumMember]
		SnapshotBuilding,

		/// <summary>
		/// Snapshot complete.
		/// </summary>
		[EnumMember]
		SnapshotComplete,

		/// <summary>
		/// Incremental.
		/// </summary>
		[EnumMember]
		Increment,
	}

	/// <summary>
	/// Messages containing quotes.
	/// </summary>
	[DataContract]
	[Serializable]
	public sealed class QuoteChangeMessage : BaseSubscriptionIdMessage<QuoteChangeMessage>,
		IServerTimeMessage, ISecurityIdMessage, IGeneratedMessage, ISeqNumMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		private QuoteChange[] _bids = ArrayHelper.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to buy.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str281Key)]
		[DescriptionLoc(LocalizedStrings.Str282Key)]
		[MainCategory]
		public QuoteChange[] Bids
		{
			get => _bids;
			set => _bids = value ?? throw new ArgumentNullException(nameof(value));
		}

		private QuoteChange[] _asks = ArrayHelper.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to sell.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str283Key)]
		[DescriptionLoc(LocalizedStrings.Str284Key)]
		[MainCategory]
		public QuoteChange[] Asks
		{
			get => _asks;
			set => _asks = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Flag sorted by price quotes (<see cref="Bids"/> by descending, <see cref="Asks"/> by ascending).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str285Key)]
		[DescriptionLoc(LocalizedStrings.Str285Key, true)]
		[MainCategory]
		public bool IsSorted { get; set; } = true;

		/// <inheritdoc />
		[DataMember]
		public DataType BuildFrom { get; set; }

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
		[Ecng.Serialization.Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Order book state.
		/// </summary>
		[DataMember]
		public QuoteChangeStates? State { get; set; }

		/// <summary>
		/// Determines a <see cref="QuoteChange.StartPosition"/> initialized.
		/// </summary>
		[DataMember]
		public bool HasPositions { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long SeqNum { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.MarketDepth;

		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteChangeMessage"/>.
		/// </summary>
		public QuoteChangeMessage()
			: base(MessageTypes.QuoteChange)
		{
		}

		/// <inheritdoc />
		public override void CopyTo(QuoteChangeMessage destination)
		{
			base.CopyTo(destination);

			destination.SecurityId = SecurityId;
			destination.Bids = Bids.ToArray();
			destination.Asks = Asks.ToArray();
			destination.ServerTime = ServerTime;
			destination.IsSorted = IsSorted;
			destination.Currency = Currency;
			destination.BuildFrom = BuildFrom;
			destination.IsFiltered = IsFiltered;
			destination.State = State;
			destination.HasPositions = HasPositions;
			destination.SeqNum = SeqNum;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Sec={SecurityId},T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff},B={Bids.Length},A={Asks.Length}";

			if (State != default)
				str += $",State={State.Value}";

			if (SeqNum != default)
				str += $",SQ={SeqNum}";

			return str;
		}
	}
}