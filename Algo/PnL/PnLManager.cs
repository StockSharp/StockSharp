#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.PnL.Algo
File: PnLManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.PnL
{
	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The profit-loss manager.
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
		public virtual decimal PnL => RealizedPnL + UnrealizedPnL;

		private decimal _realizedPnL;

		/// <summary>
		/// The relative value of profit-loss without open position accounting.
		/// </summary>
		public virtual decimal RealizedPnL => _realizedPnL;

		/// <summary>
		/// The value of unrealized profit-loss.
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
		/// To process the message, containing market data or trade. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="message">The message, containing market data or trade.</param>
		/// <returns>Information on new trade.</returns>
		public PnLInfo ProcessMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					return null;
				}

				case MessageTypes.Execution:
				{
					var trade = (ExecutionMessage)message;

					if (trade.HasTradeInfo())
					{
						// TODO
						if (trade.PortfolioName.IsEmpty())
							return null;

						lock (_portfolioManagers.SyncRoot)
						{
							var manager = _portfolioManagers.SafeAdd(trade.PortfolioName, pf => new PortfolioPnLManager(pf));

							PnLInfo info;

							if (manager.ProcessMyTrade(trade, out info))
								_realizedPnL += info.PnL;

							return info;
						}
					}

					break;
				}
			}

			foreach (var pnLManager in _portfolioManagers.CachedValues)
				pnLManager.ProcessMessage(message);

			return null;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Load(SettingsStorage storage)
		{
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Save(SettingsStorage storage)
		{
		}
	}
}