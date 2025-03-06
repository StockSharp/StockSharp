namespace StockSharp.Messages;

/// <summary>
/// The interface describing an message with <see cref="StrategyId"/> property.
/// </summary>
public interface IStrategyIdMessage
{
	/// <summary>
	/// Strategy id.
	/// </summary>
	string StrategyId { get; set; }
}