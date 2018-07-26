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
	public class MarketDataMessage : SecurityMessage
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

		/// <summary>
		/// The message is market-data subscription.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// The message is not supported by adapter. To be setted if the answer.
		/// </summary>
		[DataMember]
		public bool IsNotSupported { get; set; }

		/// <summary>
		/// Subscribe or unsubscribe error info. Заполняется в случае ответа.
		/// </summary>
		[DataMember]
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
		/// Avaible only for <see cref="TimeFrameCandleMessage"/>.
		/// </remarks>
		[DataMember]
		public bool AllowBuildFromSmallerTimeFrame { get; set; } = true;

		/// <summary>
		/// Request history market data only.
		/// </summary>
		[DataMember]
		public bool IsHistory { get; set; }

		/// <summary>
		/// Use only the regular trading hours for which data will be requested.
		/// </summary>
		[DataMember]
		public bool IsRegularTradingHours { get; set; }

		/// <summary>
		/// The default depth of order book.
		/// </summary>
		public const int DefaultMaxDepth = 50;

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
			var clone = new MarketDataMessage
			{
				Arg = Arg,
				DataType = DataType,
				Error = Error,
				From = From,
				To = To,
				IsSubscribe = IsSubscribe,
				TransactionId = TransactionId,
				Count = Count,
				MaxDepth = MaxDepth,
				NewsId = NewsId,
				LocalTime = LocalTime,
				IsNotSupported = IsNotSupported,
				BuildMode = BuildMode,
				BuildFrom = BuildFrom,
				BuildField = BuildField,
				IsCalcVolumeProfile = IsCalcVolumeProfile,
				IsHistory = IsHistory,
				AllowBuildFromSmallerTimeFrame = AllowBuildFromSmallerTimeFrame,
				IsRegularTradingHours = IsRegularTradingHours,
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",Sec={SecurityId},Type={DataType},IsSubscribe={IsSubscribe},Arg={Arg},TransId={TransactionId},OrigId={OriginalTransactionId}";
		}
	}
}