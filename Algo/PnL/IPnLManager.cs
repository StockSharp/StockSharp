namespace StockSharp.Algo.PnL;

using System.Collections.Generic;

using Ecng.Serialization;

using StockSharp.Messages;

/// <summary>
/// The interface of the profit-loss calculation manager.
/// </summary>
public interface IPnLManager : IPersistable
{
	/// <summary>
	/// Total profit-loss.
	/// </summary>
	decimal PnL { get; }

	/// <summary>
	/// The value of realized profit-loss.
	/// </summary>
	decimal RealizedPnL { get; }

	/// <summary>
	/// The value of unrealized profit-loss.
	/// </summary>
	decimal? UnrealizedPnL { get; }

	/// <summary>
	/// To zero <see cref="PnL"/>.
	/// </summary>
	void Reset();

	/// <summary>
	/// To process the message, containing market data or trade. If the trade was already processed earlier, previous information returns.
	/// </summary>
	/// <param name="message">The message, containing market data or trade.</param>
	/// <param name="changedPortfolios">Changed <see cref="PortfolioPnLManager"/> list.</param>
	/// <returns>Information on new trade.</returns>
	PnLInfo ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios = null);
}