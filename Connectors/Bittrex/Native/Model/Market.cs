namespace StockSharp.Bittrex.Native.Model;

class Market
{
	public string MarketCurrency { get; set; }
	public string BaseCurrency { get; set; }
	public string MarketCurrencyLong { get; set; }
	public string BaseCurrencyLong { get; set; }
	public decimal MinTradeSize { get; set; }
	public string MarketName { get; set; }
	public bool IsActive { get; set; }
	public DateTime Created { get; set; }
}