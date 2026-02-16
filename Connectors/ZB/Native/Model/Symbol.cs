namespace StockSharp.ZB.Native.Model;

class Statistic
{
	[JsonProperty("period")]
	public string Period { get; set; }

	[JsonProperty("price_open")]
	public double PriceOpen { get; set; }

	[JsonProperty("price_close")]
	public double PriceClose { get; set; }

	[JsonProperty("price_low")]
	public double PriceLow { get; set; }

	[JsonProperty("price_high")]
	public double PriceHigh { get; set; }

	[JsonProperty("price_bid")]
	public double PriceBid { get; set; }

	[JsonProperty("price_ask")]
	public double PriceAsk { get; set; }

	[JsonProperty("volume_quoted")]
	public double VolumeQuoted { get; set; }

	[JsonProperty("volume_base")]
	public double VolumeBase { get; set; }

	[JsonProperty("orders_buy")]
	public int OrdersBuy { get; set; }

	[JsonProperty("orders_sell")]
	public int OrdersSell { get; set; }
}

class Symbol
{
	[JsonProperty("symbol")]
	public string Code { get; set; }

	[JsonProperty("alias")]
	public string Alias { get; set; }

	[JsonProperty("currency_base")]
	public string CurrencyBase { get; set; }

	[JsonProperty("currency_base_name")]
	public string CurrencyBaseName { get; set; }

	[JsonProperty("currency_quoted")]
	public string CurrencyQuoted { get; set; }

	[JsonProperty("currency_quoted_name")]
	public string CurrencyQuotedName { get; set; }

	[JsonProperty("statistic")]
	public Statistic[] Statistic { get; set; }
}