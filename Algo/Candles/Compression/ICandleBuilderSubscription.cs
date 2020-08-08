#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: ICandleBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using StockSharp.Messages;

	/// <summary>
	/// Interface described candles subscription.
	/// </summary>
	public interface ICandleBuilderSubscription
	{
		/// <summary>
		/// Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
		/// </summary>
		MarketDataMessage Message { get; }

		/// <summary>
		/// Volume profile.
		/// </summary>
		VolumeProfileBuilder VolumeProfile { get; set; }

		/// <summary>
		/// The current candle.
		/// </summary>
		CandleMessage CurrentCandle { get; set; }
	}

	///// <summary>
	///// Interface described candles subscription.
	///// </summary>
	///// <typeparam name="TCandleMessage">Candle type.</typeparam>
	//public interface ICandleBuilderSubscription<TCandleMessage> : ICandleBuilderSubscription
	//	where TCandleMessage : CandleMessage
	//{
	//	/// <summary>
	//	/// The current candle.
	//	/// </summary>
	//	new TCandleMessage CurrentCandle { get; set; }
	//}
}