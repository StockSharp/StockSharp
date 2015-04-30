namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class CalendarsResponse
	{
		[JsonProperty("calendars")]
		public IEnumerable<Calendar> Calendars { get; set; }
	}
}