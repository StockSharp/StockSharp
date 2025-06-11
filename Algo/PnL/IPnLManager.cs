namespace StockSharp.Algo.PnL;

/// <summary>
/// The interface of the profit-loss calculation manager.
/// </summary>
public interface IPnLManager : IPersistable
{
	/// <summary>
	/// The value of realized profit-loss.
	/// </summary>
	decimal RealizedPnL { get; }

	/// <summary>
	/// The value of unrealized profit-loss.
	/// </summary>
	decimal UnrealizedPnL { get; }

	/// <summary>
	/// To zero <see cref="PnL"/>.
	/// </summary>
	void Reset();

	/// <summary>
	/// Update the security information.
	/// </summary>
	/// <param name="l1Msg"><see cref="Level1ChangeMessage"/></param>
	void UpdateSecurity(Level1ChangeMessage l1Msg);

	/// <summary>
	/// To process the message, containing market data or trade. If the trade was already processed earlier, previous information returns.
	/// </summary>
	/// <param name="message">The message, containing market data or trade.</param>
	/// <param name="changedPortfolios">Changed <see cref="PortfolioPnLManager"/> list.</param>
	/// <returns>Information on new trade.</returns>
	PnLInfo ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios = null);
}