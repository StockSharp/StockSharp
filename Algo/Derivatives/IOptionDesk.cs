namespace StockSharp.Algo.Derivatives;

/// <summary>
/// Interface describing options desk control.
/// </summary>
public interface IOptionDesk
{
	/// <summary>
	/// <see cref="BasketBlackScholes"/>
	/// </summary>
	BasketBlackScholes Model { get; set; }
}