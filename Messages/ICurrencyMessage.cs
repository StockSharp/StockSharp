namespace StockSharp.Messages;

/// <summary>
/// Interface describes an currency based message.
/// </summary>
public interface ICurrencyMessage
{
	/// <summary>
	/// Trading security currency.
	/// </summary>
	CurrencyTypes? Currency { get; set; }
}
