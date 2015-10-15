namespace StockSharp.Algo.PnL
{
	using StockSharp.Messages;

	/// <summary>
	/// The interface of the gain-loss calculation manager.
	/// </summary>
	public interface IPnLManager
	{
		/// <summary>
		/// Total profit-loss.
		/// </summary>
		decimal PnL { get; }

		/// <summary>
		/// The value of realized gain-loss.
		/// </summary>
		decimal RealizedPnL { get; }

		/// <summary>
		/// The value of unrealized gain-loss.
		/// </summary>
		decimal UnrealizedPnL { get; }

		/// <summary>
		/// To zero <see cref="IPnLManager.PnL"/>.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate trade profitability. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="trade">Trade.</param>
		/// <returns>Information on new trade.</returns>
		PnLInfo ProcessMyTrade(ExecutionMessage trade);

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		void ProcessMessage(Message message);
	}
}