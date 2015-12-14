#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.DataTypes.Oanda
File: Calendar.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Calendar
	{
		[JsonProperty("title")]
		public string Title { get; set; }

		[JsonProperty("timeStamp")]
		public long TimeStamp { get; set; }

		[JsonProperty("unit")]
		public string Unit { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("forecast")]
		public double Forecast { get; set; }

		[JsonProperty("previous")]
		public double Previous { get; set; }

		[JsonProperty("actual")]
		public double Actual { get; set; }

		[JsonProperty("market")]
		public double Market { get; set; }
	}
}