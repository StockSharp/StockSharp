namespace StockSharp.Btce.Native
{
	using System;
	using System.Collections.Generic;

	using Ecng.Net;

	using Newtonsoft.Json;

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

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal struct Rights
	{
		[JsonProperty(PropertyName = "info")]
		public bool CanGetInfo { get; set; }

		[JsonProperty(PropertyName = "trade")]
		public bool CanTrade { get; set; }

		// на BTCE вроде никак не используется
		[JsonProperty(PropertyName = "withdraw")]
		public bool CanWithdraw { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class AccountState
	{
		[JsonProperty(PropertyName = "funds")]
		private readonly Dictionary<string, double> _funds = new Dictionary<string, double>();

		[JsonIgnore]
		public IDictionary<string, double> Funds
		{
			get { return _funds; }
		}

		[JsonProperty(PropertyName = "rights")]
		public Rights Rights;

		[JsonProperty(PropertyName = "transaction_count")]
		public long TransactionCount { get; set; }

		[JsonProperty(PropertyName = "open_orders")]
		public long ActiveOrderCount { get; set; }

		[JsonProperty(PropertyName = "server_time")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class InfoReply
	{
		[JsonProperty(PropertyName = "success")]
		public bool Success { get; set; }

		[JsonProperty(PropertyName = "error")]
		public string ErrorText { get; set; }

		[JsonProperty(PropertyName = "return")]
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

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Transaction
	{
		[JsonProperty(PropertyName = "tid")]
		public long Id { get; internal set; }

		// 1 - redeem
		// 4 - забрали по сделке
		// 5 - заблокировали на сделку
		// другие пока не нашел
		[JsonProperty(PropertyName = "type")]
		public int Type { get; set; }

		[JsonProperty(PropertyName = "amount")]
		public double Volume { get; set; }

		[JsonProperty(PropertyName = "currency")]
		public string Currency { get; set; }

		[JsonProperty(PropertyName = "desc")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "status")]
		public int Status { get; set; }

		[JsonProperty(PropertyName = "timestamp")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class TransactionsReply
	{
		[JsonProperty(PropertyName = "success")]
		public bool Success { get; set; }

		[JsonProperty(PropertyName = "error")]
		public string ErrorText { get; set; }

		[JsonProperty(PropertyName = "return")]
		private readonly IDictionary<long, Transaction> _items = new SortedDictionary<long, Transaction>();

		[JsonIgnore]
		// так удобнее получить инфу по конкретной заявке
		public IDictionary<long, Transaction> Items
		{
			get { return _items; }
		}

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

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	//internal enum Directions
	//{
	//	Sell,
	//	Buy,

	//	sell = Sell,
	//	buy = Buy,

	//	// указывается в public-API https://btc-e.com/api/3/trades/btc_usd
	//	ask = sell,
	//	bid = buy
	//}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Trade
	{
		[JsonProperty(PropertyName = "pair")]
		public string Instrument { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Side { get; set; }

		[JsonProperty(PropertyName = "amount")]
		public double Volume { get; set; }

		// используется в private-API
		[JsonProperty(PropertyName = "rate")]
		private double _rate;

		// указывается в public-API https://btc-e.com/api/3/trades/btc_usd
		[JsonProperty(PropertyName = "price")]
		public double Price
		{
			get { return _rate; }
			set { _rate = value; }
		}

		[JsonProperty(PropertyName = "order_id")]
		public long OrderId { get; set; }

		[JsonProperty(PropertyName = "is_your_order")]
		public bool IsMyOrder { get; set; }

		[JsonProperty(PropertyName = "tid")]
		public long Id { get; internal set; }

		[JsonProperty(PropertyName = "timestamp")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class MyTradesReply
	{
		[JsonProperty(PropertyName = "success")]
		public bool Success { get; set; }

		[JsonProperty(PropertyName = "error")]
		public string ErrorText { get; set; }

		[JsonProperty(PropertyName = "return")]
		private readonly IDictionary<long, Trade> _items = new SortedDictionary<long, Trade>();

		[JsonIgnore]
		// так удобнее получить инфу по конкретной заявке
		public IDictionary<long, Trade> Items
		{
			get { return _items; }
		}

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

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Order
	{
		[JsonProperty(PropertyName = "pair")]
		public string Instrument { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Side { get; set; }

		[JsonProperty(PropertyName = "amount")]
		public double Volume { get; set; }

		[JsonProperty(PropertyName = "rate")]
		public double Price { get; set; }

		// 0 - активна (висит в стакане)
		// остальные пока не нашел
		[JsonProperty(PropertyName = "status")]
		public int Status { get; set; }

		[JsonProperty(PropertyName = "order_id")]
		public long Id { get; set; }

		[JsonProperty(PropertyName = "timestamp_created")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class OrdersReply
	{
		[JsonProperty(PropertyName = "success")]
		public bool Success { get; set; }

		[JsonProperty(PropertyName = "error")]
		public string ErrorText { get; set; }

		[JsonProperty(PropertyName = "return")]
		private readonly IDictionary<long, Order> _items = new SortedDictionary<long, Order>();

		[JsonIgnore]
		// так удобнее получить инфу по конкретной заявке
		public IDictionary<long, Order> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	#endregion

	#region make/cancel order

	// в командах у нас пока создать/отменить заявку

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Command
	{
		//// для создания заявки указываются эти аргументы
		//[JsonProperty(PropertyName = "instrument")]
		//public string Instrument { get; set; }

		//[JsonProperty(PropertyName = "direction")]
		//public string Side { get; set; }

		//[JsonProperty(PropertyName = "volume")]
		//public double Volume { get; set; }

		//[JsonProperty(PropertyName = "price")]
		//public double Price { get; set; }

		// после создания заявки здесь можно узнать ее номер
		// для отмены заявки указывавается ее номер
		[JsonProperty(PropertyName = "order_id")]
		public long OrderId { get; set; }

		[JsonProperty(PropertyName = "received")]
		public double Received { get; set; }

		[JsonProperty(PropertyName = "remains")]
		public double Remains { get; set; }

		[JsonProperty(PropertyName = "funds")]
		private readonly Dictionary<string, double> _funds = new Dictionary<string, double>();

		[JsonIgnore]
		public IDictionary<string, double> Funds
		{
			get { return _funds; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class CommandReply
	{
		[JsonProperty(PropertyName = "success")]
		public bool Success { get; set; }

		[JsonProperty(PropertyName = "error")]
		public string ErrorText { get; set; }

		[JsonProperty(PropertyName = "return")]
		public Command Command { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	#endregion

	#region instruments

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	// https://btc-e.com/api/3/documentation#info
	// "btc_usd":{"decimal_places":3,"min_price":0.1,"max_price":3200,"min_amount":0.01,"hidden":0,"fee":0.2}
	internal class InstrumentInfo
	{
		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "decimal_places")]
		public int DecimalDigits { get; set; }

		[JsonProperty(PropertyName = "min_price")]
		public double MinPrice { get; set; }

		[JsonProperty(PropertyName = "max_price")]
		public double MaxPrice { get; set; }

		[JsonProperty(PropertyName = "min_amount")]
		public double MinVolume { get; set; }

		[JsonProperty(PropertyName = "hidden")]
		public bool IsHidden { get; set; }

		[JsonProperty(PropertyName = "fee")]
		public double Fee { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class InstrumentsReply
	{
		[JsonProperty(PropertyName = "server_time")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		[JsonProperty(PropertyName = "pairs")]
		private readonly IDictionary<string, InstrumentInfo> _items = new SortedDictionary<string, InstrumentInfo>();

		[JsonIgnore]
		// так удобнее получить инфу по конкретному инструменту
		public IDictionary<string, InstrumentInfo> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	#endregion

	#region tickers

	// https://btc-e.com/api/3/documentation#ticker
	// {"btc_usd":{"high":637.90002,"low":610,"avg":623.95001,"vol":4049518.45004,"vol_cur":6521.73554,"last":625.5,"buy":625.5,"sell":623.56,"updated":1394529316}}
	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Ticker
	{
		[JsonProperty(PropertyName = "instrument")]
		public string Instrument { get; set; }

		[JsonProperty(PropertyName = "high")]
		public double HighPrice { get; set; }

		[JsonProperty(PropertyName = "low")]
		public double LowPrice { get; set; }

		[JsonProperty(PropertyName = "avg")]
		public double AveragePrice { get; set; }

		// Для btc_usd объем в BTC
		[JsonProperty(PropertyName = "vol_cur")]
		public double Volume { get; set; }

		// Для btc_usd объем в USD
		[JsonProperty(PropertyName = "vol")]
		public double MoneyVolume { get; set; }

		[JsonProperty(PropertyName = "last")]
		public double LastPrice { get; set; }

		[JsonProperty(PropertyName = "sell")]
		public double Ask { get; set; }

		[JsonProperty(PropertyName = "buy")]
		public double Bid { get; set; }

		[JsonProperty(PropertyName = "updated")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class TickersReply
	{
		private readonly IDictionary<string, Ticker> _items = new SortedDictionary<string, Ticker>();

		[JsonIgnore]
		public IDictionary<string, Ticker> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(_items, Formatting.Indented);
		}
	}

	#endregion

	#region depth

	// https://btc-e.com/api/3/documentation#depth
	// {"btc_usd":{"asks":[[624.001,0.9591],[624.002,1.60614105],[625.3,0.33259]],"bids":[[623,32.35859875],[622.85,0.05],[622.835,0.01]]}}
	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class Depth
	{
		[JsonProperty(PropertyName = "bids")]
		public readonly List<double[]> Bids = new List<double[]>();

		[JsonProperty(PropertyName = "asks")]
		public readonly List<double[]> Asks = new List<double[]>();

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class DepthsReply
	{
		private readonly IDictionary<string, Depth> _items = new SortedDictionary<string, Depth>();

		[JsonIgnore]
		public IDictionary<string, Depth> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(_items, Formatting.Indented);
		}
	}

	#endregion

	#region last trades for all

	// https://btc-e.com/api/3/documentation#trades
	// {"btc_usd":[{"type":"ask","price":625.35,"amount":0.0104049,"tid":32254816,"timestamp":1394532697},{"type":"bid","price":627.87,"amount":0.0216,"tid":32254810,"timestamp":1394532683}]}
	//[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	internal class TradesReply
	{
		private readonly IDictionary<string, List<Trade>> _items = new SortedDictionary<string, List<Trade>>();

		[JsonIgnore]
		public IDictionary<string, List<Trade>> Items
		{
			get { return _items; }
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(_items, Formatting.Indented);
		}
	}

	#endregion
}