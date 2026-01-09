namespace StockSharp.Algo.Strategies;

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
/// <remarks>
/// Initializes a new instance of the <see cref="OrderBookSource"/>.
/// </remarks>
/// <param name="name"><see cref="IOrderBookSource.Name"/></param>
public class OrderBookSource(string name) : IOrderBookSource
{
	private readonly string _name = name.ThrowIfEmpty(nameof(name));
	string IOrderBookSource.Name => _name;
}