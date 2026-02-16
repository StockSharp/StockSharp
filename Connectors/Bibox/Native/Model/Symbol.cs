namespace StockSharp.Bibox.Native.Model;

class Symbol
{
	[JsonProperty("symbol")]
	public string Code { get; set; }

	[JsonProperty("base")]
	public string Base { get; set; }

	[JsonProperty("quote")]
	public string Quote { get; set; }

	[JsonProperty("min_price")]
	public double? MinPrice { get; set; }

	[JsonProperty("max_price")]
	public double? MaxPrice { get; set; }

	[JsonProperty("quantity_min")]
	public double? MinQuantity { get; set; }

	[JsonProperty("quantity_max")]
	public double? MaxQuantity { get; set; }

	[JsonProperty("price_scale")]
	public int? PriceScale { get; set; }

	[JsonProperty("quantity_scale")]
	public int? QuantityScale { get; set; }

	[JsonProperty("price_increment")]
	public double? PriceIncrement { get; set; }

	[JsonProperty("quantity_increment")]
	public double? QuantityIncrement { get; set; }

	[JsonProperty("min_order_value")]
	public double? MinOrderValue { get; set; }
}