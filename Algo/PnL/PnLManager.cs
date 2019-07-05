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

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The profit-loss manager.
	/// </summary>
	public class PnLManager : IPnLManager
	{
		private readonly CachedSynchronizedDictionary<string, PortfolioPnLManager> _managersByPf = new CachedSynchronizedDictionary<string, PortfolioPnLManager>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<long, PortfolioPnLManager> _managersByTransId = new Dictionary<long, PortfolioPnLManager>();
		private readonly Dictionary<long, long> _orderIds = new Dictionary<long, long>();
		private readonly HashSet<long> _orderTransactions = new HashSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PnLManager"/>.
		/// </summary>
		public PnLManager()
		{
		}

		/// <inheritdoc />
		public decimal PnL => RealizedPnL + UnrealizedPnL ?? 0;

		private decimal _realizedPnL;

		/// <inheritdoc />
		public decimal RealizedPnL => _realizedPnL;

		/// <inheritdoc />
		public decimal? UnrealizedPnL
		{
			get
			{
				decimal? retVal = null;

				foreach (var manager in _managersByPf.CachedValues)
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
			lock (_managersByPf.SyncRoot)
			{
				_realizedPnL = 0;
				_managersByPf.Clear();
				_managersByTransId.Clear();
				_orderIds.Clear();
				_orderTransactions.Clear();
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

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					lock (_managersByPf.SyncRoot)
					{
						var manager = _managersByPf.SafeAdd(regMsg.PortfolioName, pf => new PortfolioPnLManager(pf));
						_managersByTransId.Add(regMsg.TransactionId, manager);

						_orderTransactions.Add(regMsg.TransactionId);
					}
					
					return null;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					var transId = execMsg.OriginalTransactionId;
					var orderId = execMsg.OrderId;

					if (transId != 0 && execMsg.HasOrderInfo())
					{
						lock (_managersByPf.SyncRoot)
						{
							if (orderId != null && _orderTransactions.Contains(transId))
								_orderIds[orderId.Value] = transId;
						}
					}

					if (execMsg.HasTradeInfo())
					{
						lock (_managersByPf.SyncRoot)
						{
							if (transId == 0)
							{
								if (orderId == null || !_orderIds.TryGetValue(orderId.Value, out transId))
									return null;
							}

							if (!_managersByTransId.TryGetValue(transId, out var manager))
								return null;

							if (!manager.ProcessMyTrade(execMsg, out var info))
								return null;

							_realizedPnL += info.PnL;
							changedPortfolios?.Add(manager);
							return info;
						}
					}

					break;
				}

				case MessageTypes.Level1Change:
				case MessageTypes.QuoteChange:
				case MessageTypes.PortfolioChange:
				case MessageTypes.PositionChange:
				{
					break;
				}

				default:
					return null;
			}

			foreach (var manager in _managersByPf.CachedValues)
			{
				if (manager.ProcessMessage(message))
					changedPortfolios?.Add(manager);
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