namespace StockSharp.Algo.PnL
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The gain-loss manager.
	/// </summary>
	public class PnLManager : IPnLManager
	{
		private readonly CachedSynchronizedDictionary<string, PortfolioPnLManager> _portfolioManagers = new CachedSynchronizedDictionary<string, PortfolioPnLManager>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="PnLManager"/>.
		/// </summary>
		public PnLManager()
		{
		}

		/// <summary>
		/// Total profit-loss.
		/// </summary>
		public virtual decimal PnL
		{
			get { return RealizedPnL + UnrealizedPnL; }
		}

		private decimal _realizedPnL;

		/// <summary>
		/// The relative value of profit-loss without open position accounting.
		/// </summary>
		public virtual decimal RealizedPnL
		{
			get { return _realizedPnL; }
		}

		/// <summary>
		/// The value of unrealized gain-loss.
		/// </summary>
		public virtual decimal UnrealizedPnL
		{
			get { return _portfolioManagers.CachedValues.Sum(p => p.UnrealizedPnL); }
		}

		/// <summary>
		/// To zero <see cref="PnLManager.PnL"/>.
		/// </summary>
		public void Reset()
		{
			lock (_portfolioManagers.SyncRoot)
			{
				_realizedPnL = 0;
				_portfolioManagers.Clear();	
			}
		}

		/// <summary>
		/// To calculate trade profitability. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="trade">Trade.</param>
		/// <returns>Information on new trade.</returns>
		public virtual PnLInfo ProcessMyTrade(ExecutionMessage trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			lock (_portfolioManagers.SyncRoot)
			{
				var manager = _portfolioManagers.SafeAdd(trade.PortfolioName, pf => new PortfolioPnLManager(pf));
				
				PnLInfo info;

				if (manager.ProcessMyTrade(trade, out info))
					_realizedPnL += info.PnL;

				return info;
			}
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		public void ProcessMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			foreach (var pnLManager in _portfolioManagers.CachedValues)
				pnLManager.ProcessMessage(message);
		}
	}
}