namespace StockSharp.Btce.Native;

#region getInfo

/*
	{
	 * "success":1,
	 * "error":"message",
	 * "return": {
	 *		"funds":{"usd":0,"btc":0,"ltc":0,"nmc":0,"rur":0,"eur":0,"nvc":0,"trc":0,"ppc":0,"ftc":0,"xpm":0},
	 *		"rights":{"info":1,"trade":1,"withdraw":0},
	 *		"transaction_count":0,
	 *		"open_orders":0,
	 *		"server_time":1394002785
	 *	}}
	*/

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal struct Rights
{
	[JsonProperty("info")]
	public bool CanGetInfo { get; set; }

	[JsonProperty("trade")]
	public bool CanTrade { get; set; }

	// BTCE looks like not used
	[JsonProperty("withdraw")]
	public bool CanWithdraw { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class AccountState
{
	[JsonProperty("funds")]
	private readonly Dictionary<string, double> _funds = new();

	[JsonIgnore]
	public IDictionary<string, double> Funds => _funds;

	[JsonProperty("rights")]
	public Rights Rights { get; set; }

	[JsonProperty("transaction_count")]
	public long TransactionCount { get; set; }

	[JsonProperty("open_orders")]
	public long ActiveOrderCount { get; set; }

	[JsonProperty("server_time")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class InfoReply
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error")]
	public string ErrorText { get; set; }

	[JsonProperty("return")]
	public AccountState State { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region transactions

/*
	{
 		"success":1,
 		"return": {
 			"384619137": {
 				"type":1,
 				"amount":0.02437489,
 				"currency":"BTC",
 				"desc":"BTC-E CODE redeemed",
 				"status":2,
 				"timestamp":1394099618
 			}
 		}
		"error":"no transactions"
	}	 
*/

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
[JsonConverter(typeof(JArrayToObjectConverter))]
class PusherTransaction
{
	public string Side { get; set; }
	public decimal Price { get; set; }
	public decimal Size { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Transaction
{
	[JsonProperty("tid")]
	public long Id { get; internal set; }

	// 1 - redeem
	// 4 - matched
	// 5 - blocked
	// other values not yet found
	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("amount")]
	public double Volume { get; set; }

	[JsonProperty("currency")]
	public string Currency { get; set; }

	[JsonProperty("desc")]
	public string Description { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class TransactionsReply
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error")]
	public string ErrorText { get; set; }

	[JsonProperty("return")]
	private readonly IDictionary<long, Transaction> _items = new SortedDictionary<long, Transaction>();

	[JsonIgnore]
	public IDictionary<long, Transaction> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region trades

/*
	{
		"success":1,
		"return": {
			"31883326": {
				"pair":"btc_rur",
				"type":"sell",
				"amount":0.01,
				"rate":23501.3,
				"order_id":166540940,
				"is_your_order":0,
				"timestamp":1394100096
			}
		}
	}
 */

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Trade
{
	[JsonProperty("pair")]
	public string Instrument { get; set; }

	[JsonProperty("type")]
	public string Side { get; set; }

	[JsonProperty("amount")]
	public double Volume { get; set; }

	// используется в private-API
	[JsonProperty("rate")]
	private double _rate;

	// указывается в public-API https://btc-e.com/api/3/trades/btc_usd
	[JsonProperty("price")]
	public double Price
	{
		get => _rate;
		set => _rate = value;
	}

	[JsonProperty("order_id")]
	public long OrderId { get; set; }

	[JsonProperty("is_your_order")]
	public bool IsMyOrder { get; set; }

	[JsonProperty("tid")]
	public long Id { get; internal set; }

	[JsonProperty("timestamp")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class MyTradesReply
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error")]
	public string ErrorText { get; set; }

	[JsonProperty("return")]
	private readonly IDictionary<long, Trade> _items = new SortedDictionary<long, Trade>();

	[JsonIgnore]
	public IDictionary<long, Trade> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region orders

/*
	{
		"success":1,
		"return": {
			"166542044": {
				"pair":"btc_rur",
				"type":"sell",
				"amount":0.01000000,
				"rate":90000.00000000,
				"timestamp_created":1394100225,
				"status":0
			}
		}
	}
 */

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Order
{
	[JsonProperty("pair")]
	public string Instrument { get; set; }

	[JsonProperty("type")]
	public string Side { get; set; }

	[JsonProperty("amount")]
	public double Volume { get; set; }

	[JsonProperty("rate")]
	public double Price { get; set; }

	// Deprecated
	// 0 - active, 1 – executed order, 2 - canceled, 3 – canceled, but was partially executed.
	[JsonProperty("status")]
	[Obsolete]
	public int Status { get; set; }

	[JsonProperty("order_id")]
	public long Id { get; set; }

	[JsonProperty("timestamp_created")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class OrdersReply
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error")]
	public string ErrorText { get; set; }

	[JsonProperty("return")]
	private readonly IDictionary<long, Order> _items = new SortedDictionary<long, Order>();

	[JsonIgnore]
	public IDictionary<long, Order> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region make/cancel order

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Command
{
	[JsonProperty("order_id")]
	public long OrderId { get; set; }

	[JsonProperty("received")]
	public double Received { get; set; }

	[JsonProperty("remains")]
	public double Remains { get; set; }

	[JsonProperty("funds")]
	private readonly Dictionary<string, double> _funds = new();

	[JsonIgnore]
	public IDictionary<string, double> Funds => _funds;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class CommandReply
{
	[JsonProperty("success")]
	public bool Success { get; set; }

	[JsonProperty("error")]
	public string ErrorText { get; set; }

	[JsonProperty("return")]
	public Command Command { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region instruments

// https://btc-e.com/api/3/documentation#info
// "btc_usd":{"decimal_places":3,"min_price":0.1,"max_price":3200,"min_amount":0.01,"hidden":0,"fee":0.2}
[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class InstrumentInfo
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("decimal_places")]
	public int DecimalDigits { get; set; }

	[JsonProperty("min_price")]
	public double MinPrice { get; set; }

	[JsonProperty("max_price")]
	public double MaxPrice { get; set; }

	[JsonProperty("min_amount")]
	public double MinVolume { get; set; }

	[JsonProperty("hidden")]
	public bool IsHidden { get; set; }

	[JsonProperty("fee")]
	public double Fee { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class InstrumentsReply
{
	[JsonProperty("server_time")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	[JsonProperty("pairs")]
	private readonly IDictionary<string, InstrumentInfo> _items = new SortedDictionary<string, InstrumentInfo>();

	[JsonIgnore]
	public IDictionary<string, InstrumentInfo> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

#endregion

#region tickers

// https://btc-e.com/api/3/documentation#ticker
// {"btc_usd":{"high":637.90002,"low":610,"avg":623.95001,"vol":4049518.45004,"vol_cur":6521.73554,"last":625.5,"buy":625.5,"sell":623.56,"updated":1394529316}}
[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Ticker
{
	[JsonProperty("instrument")]
	public string Instrument { get; set; }

	[JsonProperty("high")]
	public double? HighPrice { get; set; }

	[JsonProperty("low")]
	public double? LowPrice { get; set; }

	[JsonProperty("avg")]
	public double? AveragePrice { get; set; }

	[JsonProperty("vol_cur")]
	public double? Volume { get; set; }

	[JsonProperty("vol")]
	public double? MoneyVolume { get; set; }

	[JsonProperty("last")]
	public double? LastPrice { get; set; }

	[JsonProperty("sell")]
	public double? Ask { get; set; }

	[JsonProperty("buy")]
	public double? Bid { get; set; }

	[JsonProperty("updated")]
	[JsonConverter(typeof(JsonDateTimeConverter))]
	public DateTime Timestamp { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class TickersReply
{
	private readonly IDictionary<string, Ticker> _items = new SortedDictionary<string, Ticker>();

	[JsonIgnore]
	public IDictionary<string, Ticker> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(_items, Formatting.Indented);
	}
}

#endregion

#region depth

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
[JsonConverter(typeof(JArrayToObjectConverter))]
class OrderBookEntry
{
	public decimal Price { get; set; }
	public decimal Size { get; set; }
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class OrderBook
{
	[JsonProperty("bid")]
	public OrderBookEntry[] Bids { get; set; }

	[JsonProperty("ask")]
	public OrderBookEntry[] Asks { get; set; }

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

// https://btc-e.com/api/3/documentation#depth
// {"btc_usd":{"asks":[[624.001,0.9591],[624.002,1.60614105],[625.3,0.33259]],"bids":[[623,32.35859875],[622.85,0.05],[622.835,0.01]]}}
[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class Depth
{
	[JsonProperty("bids")]
	public readonly List<double[]> Bids = new();

	[JsonProperty("asks")]
	public readonly List<double[]> Asks = new();

	public override string ToString()
	{
		return JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}

[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class DepthsReply
{
	private readonly IDictionary<string, Depth> _items = new SortedDictionary<string, Depth>();

	[JsonIgnore]
	public IDictionary<string, Depth> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(_items, Formatting.Indented);
	}
}

#endregion

#region last trades for all

// https://btc-e.com/api/3/documentation#trades
// {"btc_usd":[{"type":"ask","price":625.35,"amount":0.0104049,"tid":32254816,"timestamp":1394532697},{"type":"bid","price":627.87,"amount":0.0216,"tid":32254810,"timestamp":1394532683}]}
[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
internal class TradesReply
{
	private readonly IDictionary<string, List<Trade>> _items = new SortedDictionary<string, List<Trade>>();

	[JsonIgnore]
	public IDictionary<string, List<Trade>> Items => _items;

	public override string ToString()
	{
		return JsonConvert.SerializeObject(_items, Formatting.Indented);
	}
}

#endregion
