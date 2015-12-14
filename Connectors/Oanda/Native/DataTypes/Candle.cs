#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.DataTypes.Oanda
File: Candle.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Candle
	{
		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("openMid")]
		public double Open { get; set; }

		[JsonProperty("highMid")]
		public double High { get; set; }

		[JsonProperty("lowMid")]
		public double Low { get; set; }

		[JsonProperty("closeMid")]
		public double Close { get; set; }

		[JsonProperty("volume")]
		public double Volume { get; set; }

		[JsonProperty("complete")]
		public bool Complete { get; set; }
	}
}