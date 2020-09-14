#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: MarketDataMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Market-data types.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum MarketDataTypes
	{
		/// <summary>
		/// Level 1.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Level1Key)]
		Level1,

		/// <summary>
		/// Market depth (order book).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketDepthKey)]
		MarketDepth,

		/// <summary>
		/// Tick trades.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TicksKey)]
		Trades,

		/// <summary>
		/// Order log.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderLogKey)]
		OrderLog,

		/// <summary>
		/// News.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NewsKey)]
		News,

		/// <summary>
		/// Candles (time-frame).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeFrameCandleKey)]
		CandleTimeFrame,

		/// <summary>
		/// Candle (tick).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TickCandleKey)]
		CandleTick,

		/// <summary>
		/// Candle (volume).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolumeCandleKey)]
		CandleVolume,

		/// <summary>
		/// Candle (range).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RangeCandleKey)]
		CandleRange,

		/// <summary>
		/// Candle (X&amp;0).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PnFCandleKey)]
		CandlePnF,

		/// <summary>
		/// Candle (renko).
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RenkoCandleKey)]
		CandleRenko,

		/// <summary>
		/// Board info.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardInfoKey)]
		Board,

		/// <summary>
		/// Heikin Ashi.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HeikinAshiKey)]
		CandleHeikinAshi
	}

	/// <summary>
	/// Build modes.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum MarketDataBuildModes
	{
		/// <summary>
		/// Request built data and build the missing data from trades, depths etc.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadAndBuildKey)]
		LoadAndBuild,

		/// <summary>
		/// Request only built data.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LoadKey)]
		Load,

		/// <summary>
		/// Build from trades, depths etc.
		/// </summary>
		[EnumMember]
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BuildKey)]
		Build
	}

	/// <summary>
	/// Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
	/// </summary>
	[DataContract]
	[Serializable]
	public class MarketDataMessage : SecurityMessage, ISubscriptionMessage, IGeneratedMessage
	{
		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset? From { get; set; }

		/// <inheritdoc />
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset? To { get; set; }

		DataType ISubscriptionMessage.DataType => DataType2;

		private DataType _dataType2 = Messages.DataType.Level1;

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public DataType DataType2
		{
			get => _dataType2;
			set => _dataType2 = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Market data type.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		//[Obsolete("Use DataType2 property.")]
		public new MarketDataTypes DataType
		{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
			get => DataType2.ToMarketDataType();
			set => DataType2 = value.ToDataType(Arg);
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <summary>
		/// Additional argument for market data request.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str347Key)]
		[DescriptionLoc(LocalizedStrings.Str348Key)]
		[MainCategory]
		[Obsolete("Use DataType2 property.")]
		public object Arg
		{
			get => DataType2.Arg;
			set => DataType2 = Messages.DataType.Create(DataType2.MessageType, value);
		}

		/// <inheritdoc />
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Skip { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long? Count { get; set; }

		/// <summary>
		/// Max depth of requested order book. Uses in case <see cref="DataType2"/> = <see cref="DataType.MarketDepth"/>.
		/// </summary>
		[DataMember]
		public int? MaxDepth { get; set; }

		/// <summary>
		/// News id. Uses in case of request news text.
		/// </summary>
		[DataMember]
		public string NewsId { get; set; }

		/// <summary>
		/// To perform the calculation <see cref="CandleMessage.PriceLevels"/>. By default, it is disabled.
		/// </summary>
		[DataMember]
		public bool IsCalcVolumeProfile { get; set; }

		/// <summary>
		/// Build mode.
		/// </summary>
		[DataMember]
		public MarketDataBuildModes BuildMode { get; set; }

		/// <inheritdoc />
		[DataMember]
		public DataType BuildFrom { get; set; }

		/// <summary>
		/// Extra info for the <see cref="BuildFrom"/>.
		/// </summary>
		[DataMember]
		public Level1Fields? BuildField { get; set; }

		/// <summary>
		/// Allow build candles from smaller timeframe.
		/// </summary>
		/// <remarks>
		/// Available only for <see cref="TimeFrameCandleMessage"/>.
		/// </remarks>
		[DataMember]
		public bool AllowBuildFromSmallerTimeFrame { get; set; } = true;

		/// <summary>
		/// Use only the regular trading hours for which data will be requested.
		/// </summary>
		[DataMember]
		public bool IsRegularTradingHours { get; set; }

		/// <summary>
		/// Request <see cref="CandleStates.Finished"/> only candles.
		/// </summary>
		[DataMember]
		public bool IsFinishedOnly { get; set; }

		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		public string BoardCode { get; set; }

		/// <summary>
		/// Interval for data refresh.
		/// </summary>
		[DataMember]
		public TimeSpan? RefreshSpeed { get; set; }

		/// <summary>
		/// Order log to market depth builder.
		/// </summary>
		public IOrderLogMarketDepthBuilder DepthBuilder { get; set; }
		
		/// <summary>
		/// Try fill gaps.
		/// </summary>
		[DataMember]
		public bool FillGaps { get; set; }

		/// <summary>
		/// Pass through incremental <see cref="QuoteChangeMessage"/>.
		/// </summary>
		[DataMember]
		public bool DoNotBuildOrderBookInrement { get; set; }

		bool ISubscriptionMessage.FilterEnabled => false;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDataMessage"/>.
		/// </summary>
		public MarketDataMessage()
			: base(MessageTypes.MarketData)
		{
		}

		/// <summary>
		/// Initialize <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected MarketDataMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new MarketDataMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(MarketDataMessage destination)
		{
			base.CopyTo(destination);

			destination.DataType2 = DataType2.TypedClone();
			destination.From = From;
			destination.To = To;
			destination.IsSubscribe = IsSubscribe;
			destination.TransactionId = TransactionId;
			destination.Skip = Skip;
			destination.Count = Count;
			destination.MaxDepth = MaxDepth;
			destination.NewsId = NewsId;
			destination.BuildMode = BuildMode;
			destination.BuildFrom = BuildFrom?.TypedClone();
			destination.BuildField = BuildField;
			destination.IsCalcVolumeProfile = IsCalcVolumeProfile;
			destination.AllowBuildFromSmallerTimeFrame = AllowBuildFromSmallerTimeFrame;
			destination.IsRegularTradingHours = IsRegularTradingHours;
			destination.IsFinishedOnly = IsFinishedOnly;
			destination.BoardCode = BoardCode;
			destination.RefreshSpeed = RefreshSpeed;
			destination.DepthBuilder = DepthBuilder;
			destination.FillGaps = FillGaps;
			destination.DoNotBuildOrderBookInrement = DoNotBuildOrderBookInrement;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",DataType={DataType2},IsSubscribe={IsSubscribe}";

			if (TransactionId != default)
				str += $",TransId={TransactionId}";

			if (OriginalTransactionId != default)
				str += $",OrigId={OriginalTransactionId}";

			if (MaxDepth != default)
				str += $",MaxDepth={MaxDepth}";

			if (Skip != default)
				str += $",Skip={Skip}";

			if (Count != default)
				str += $",Cnt={Count}";

			if (From != default)
				str += $",From={From}";

			if (To != default)
				str += $",To={To}";

			if (BuildMode == MarketDataBuildModes.Build)
				str += $",Build={BuildMode}/{BuildFrom}/{BuildField}";

			if (AllowBuildFromSmallerTimeFrame)
				str += $",SmallTF={AllowBuildFromSmallerTimeFrame}";

			if (IsRegularTradingHours)
				str += $",RTH={IsRegularTradingHours}";

			if (IsFinishedOnly)
				str += $",FinOnly={IsFinishedOnly}";

			if (IsCalcVolumeProfile)
				str += $",Profile={IsCalcVolumeProfile}";

			if (!BoardCode.IsEmpty())
				str += $",BoardCode={BoardCode}";

			if (RefreshSpeed != null)
				str += $",Speed={RefreshSpeed}";

			if (FillGaps)
				str += $",Gaps={FillGaps}";

			if (DoNotBuildOrderBookInrement)
				str += $",NotBuildInc={DoNotBuildOrderBookInrement}";

			return str;
		}
	}
}