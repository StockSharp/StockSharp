namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="PortfolioName"/> property.
/// </summary>
public interface IPortfolioNameMessage
{
	/// <summary>
	/// Portfolio code name.
	/// </summary>
	string PortfolioName { get; set; }
}