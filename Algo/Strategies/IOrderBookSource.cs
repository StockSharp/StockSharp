namespace StockSharp.Algo.Strategies;

using StockSharp.Messages;

/// <summary>
/// <see cref="IOrderBookMessage"/> source.
/// </summary>
public interface IOrderBookSource
{
	/// <summary>
	/// Name.
	/// </summary>
	string Name { get; }
}