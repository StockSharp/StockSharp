namespace StockSharp.Messages
{
	/// <summary>
	/// The interface describing an message with <see cref="PortfolioName"/> property.
	/// </summary>
	public interface IPortfolioNameMessage : IMessage
	{
		/// <summary>
		/// Portfolio code name.
		/// </summary>
		string PortfolioName { get; set; }
	}
}