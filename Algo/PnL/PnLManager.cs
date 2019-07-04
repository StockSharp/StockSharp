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
	using System.Collections.Generic;

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

		/// <inheritdoc />
		public virtual decimal PnL => RealizedPnL + UnrealizedPnL ?? 0;

		private decimal _realizedPnL;

		/// <inheritdoc />
		public virtual decimal RealizedPnL => _realizedPnL;

		/// <inheritdoc />
		public virtual decimal? UnrealizedPnL
		{
			get
			{
				decimal? retVal = null;

				foreach (var manager in _portfolioManagers.CachedValues)
				{
					var pnl = manager.UnrealizedPnL;

					if (pnl != null)
					{
						if (retVal == null)
							retVal = 0;

						retVal += pnl.Value;
					}
				}

				return retVal;
			}
		}

		/// <inheritdoc />
		public void Reset()
		{
			lock (_portfolioManagers.SyncRoot)
			{
				_realizedPnL = 0;
				_portfolioManagers.Clear();	
			}
		}

		/// <inheritdoc />
		public PnLInfo ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios)
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

							if (manager.ProcessMyTrade(trade, out var info))
							{
								_realizedPnL += info.PnL;

								changedPortfolios?.Add(manager);
							}

							return info;
						}
					}

					break;
				}
			}

			foreach (var pnLManager in _portfolioManagers.CachedValues)
			{
				if (pnLManager.ProcessMessage(message))
					changedPortfolios?.Add(pnLManager);
			}

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