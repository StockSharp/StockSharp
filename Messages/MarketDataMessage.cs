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
	using System.Xml.Serialization;

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
	public class MarketDataMessage : SecurityMessage, ISubscriptionMessage
	{
		/// <summary>
		/// Start date, from which data needs to be retrieved.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str343Key)]
		[DescriptionLoc(LocalizedStrings.Str344Key)]
		[MainCategory]
		public DateTimeOffset? From { get; set; }

		/// <summary>
		/// End date, until which data needs to be retrieved.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str345Key)]
		[DescriptionLoc(LocalizedStrings.Str346Key)]
		[MainCategory]
		public DateTimeOffset? To { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[Browsable(false)]
		[DataMember]
		public MarketDataTypes DataType { get; set; }

		/// <summary>
		/// Additional argument for market data request.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str347Key)]
		[DescriptionLoc(LocalizedStrings.Str348Key)]
		[MainCategory]
		public object Arg { get; set; }

		/// <inheritdoc />
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// The message is not supported by adapter. To be set if the answer.
		/// </summary>
		[DataMember]
		public bool IsNotSupported { get; set; }

		/// <summary>
		/// Subscribe or unsubscribe error info. To be set if the answer.
		/// </summary>
		[DataMember]
		[XmlIgnore]
		public Exception Error { get; set; }

		/// <summary>
		/// Market-data count.
		/// </summary>
		[DataMember]
		public long? Count { get; set; }

		/// <summary>
		/// Max depth of requested order book. Uses in case <see cref="MarketDataMessage.DataType"/> = <see cref="MarketDataTypes.MarketDepth"/>.
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

		/// <summary>
		/// Which market-data type is used as a source value.
		/// </summary>
		[DataMember]
		public MarketDataTypes? BuildFrom { get; set; }

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

		/// <inheritdoc />
		[DataMember]
		public bool IsHistory { get; set; }

		/// <summary>
		/// Use only the regular trading hours for which data will be requested.
		/// </summary>
		[DataMember]
		public bool IsRegularTradingHours { get; set; }

		/// <summary>
		/// Request <see cref="CandleStates.Finished"/> only candles.
		/// </summary>
		[DataMember]
		public bool IsFinished { get; set; }

		/// <summary>
		/// Board code.
		/// </summary>
		[DataMember]
		public string BoardCode { get; set; }

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

			destination.Arg = Arg;
			destination.DataType = DataType;
			destination.Error = Error;
			destination.From = From;
			destination.To = To;
			destination.IsSubscribe = IsSubscribe;
			destination.TransactionId = TransactionId;
			destination.Count = Count;
			destination.MaxDepth = MaxDepth;
			destination.NewsId = NewsId;
			destination.IsNotSupported = IsNotSupported;
			destination.BuildMode = BuildMode;
			destination.BuildFrom = BuildFrom;
			destination.BuildField = BuildField;
			destination.IsCalcVolumeProfile = IsCalcVolumeProfile;
			destination.IsHistory = IsHistory;
			destination.AllowBuildFromSmallerTimeFrame = AllowBuildFromSmallerTimeFrame;
			destination.IsRegularTradingHours = IsRegularTradingHours;
			destination.IsFinished = IsFinished;
			destination.BoardCode = BoardCode;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Sec={SecurityId},Type={DataType},IsSubscribe={IsSubscribe}";

			if (Arg != null)
				str += $",Arg={Arg}";

			if (TransactionId != default)
				str += $",TransId={TransactionId}";

			if (OriginalTransactionId != default)
				str += $",OrigId={OriginalTransactionId}";

			if (MaxDepth != null)
				str += $",MaxDepth={MaxDepth}";

			if (Count != null)
				str += $",Cnt={Count}";

			if (From != null)
				str += $",From={From}";

			if (To != null)
				str += $",To={To}";

			if (BuildMode == MarketDataBuildModes.Build)
				str += $",Build={BuildMode}/{BuildFrom}/{BuildField}";

			if (AllowBuildFromSmallerTimeFrame)
				str += $",SmallTF={AllowBuildFromSmallerTimeFrame}";

			if (IsRegularTradingHours)
				str += $",RTH={IsRegularTradingHours}";

			if (IsFinished)
				str += $",Fin={IsFinished}";

			if (IsHistory)
				str += $",Hist={IsHistory}";

			if (IsCalcVolumeProfile)
				str += $",Profile={IsCalcVolumeProfile}";

			if (!BoardCode.IsEmpty())
				str += $",BoardCode={BoardCode}";

			if (Error != null)
				str += $",Error={Error.Message}";

			return str;
		}
	}
}