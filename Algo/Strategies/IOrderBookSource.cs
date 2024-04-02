namespace StockSharp.Algo.Strategies;

using Ecng.Common;

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

/// <summary>
/// Default implementation of <see cref="IOrderBookSource"/>.
/// </summary>
public class OrderBookSource : IOrderBookSource
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookSource"/>.
	/// </summary>
	/// <param name="name"><see cref="IOrderBookSource.Name"/></param>
	public OrderBookSource(string name)
    {
		_name = name.ThrowIfEmpty(nameof(name));
	}

	private readonly string _name;
	string IOrderBookSource.Name => _name;
}