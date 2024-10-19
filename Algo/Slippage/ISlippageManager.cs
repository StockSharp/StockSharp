namespace StockSharp.Algo.Slippage;

/// <summary>
/// The interface for the slippage calculation manager.
/// </summary>
public interface ISlippageManager : IPersistable
{
	/// <summary>
	/// Total slippage.
	/// </summary>
	decimal Slippage { get; }

	/// <summary>
	/// To reset the state.
	/// </summary>
	void Reset();

	/// <summary>
	/// To calculate slippage.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>The slippage. If it is impossible to calculate slippage, <see langword="null" /> will be returned.</returns>
	decimal? ProcessMessage(Message message);
}