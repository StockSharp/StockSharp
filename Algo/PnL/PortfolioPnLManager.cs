#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.PnL.Algo
File: PortfolioPnLManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.PnL
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Messages;

	class PortfolioPnLManager
	{
		private readonly Dictionary<long, PnLInfo> _tradeInfos = new Dictionary<long, PnLInfo>();
		private readonly CachedSynchronizedDictionary<SecurityId, PnLQueue> _securityPnLs = new CachedSynchronizedDictionary<SecurityId, PnLQueue>();

		public PortfolioPnLManager(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			PortfolioName = portfolioName;
		}

		public string PortfolioName { get; }

		/// <summary>
		/// Total profit-loss.
		/// </summary>
		public decimal PnL => RealizedPnL + UnrealizedPnL ?? 0;

		private decimal _realizedPnL;

		/// <summary>
		/// The relative value of profit-loss without open position accounting.
		/// </summary>
		public virtual decimal RealizedPnL => _realizedPnL;

		/// <summary>
		/// To zero <see cref="PnL"/>.
		/// </summary>
		public void Reset()
		{
			_realizedPnL = 0;
			_securityPnLs.Clear();
		}

		public decimal? UnrealizedPnL
		{
			get { return _securityPnLs.CachedValues.Sum(q => q.UnrealizedPnL); }
		}

		/// <summary>
		/// To calculate trade profitability. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="trade">Trade.</param>
		/// <param name="info">Information on new trade.</param>
		/// <returns><see langword="true" />, if new trade received, otherwise, <see langword="false" />.</returns>
		public bool ProcessMyTrade(ExecutionMessage trade, out PnLInfo info)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			var tradeId = trade.GetTradeId();

			if (_tradeInfos.TryGetValue(tradeId, out info))
				return false;

			var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

			info = queue.Process(trade);

			_tradeInfos.Add(tradeId, info);
			_realizedPnL += info.PnL;

			return true;
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		public void ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.Tick)
						break;

					var queue = _securityPnLs.TryGetValue(execMsg.SecurityId);
					queue?.ProcessExecution(execMsg);

					break;
				}

				case MessageTypes.Level1Change:
				{
					var levelMsg = (Level1ChangeMessage)message;

					var queue = _securityPnLs.TryGetValue(levelMsg.SecurityId);
					queue?.ProcessLevel1(levelMsg);

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					var queue = _securityPnLs.TryGetValue(quoteMsg.SecurityId);
					queue?.ProcessQuotes(quoteMsg);

					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var pfMsg = (PortfolioChangeMessage)message;

					var leverage = pfMsg.Changes.TryGetValue(PositionChangeTypes.Leverage).To<decimal?>();
					if (leverage != null)
					{
						_securityPnLs.CachedValues.ForEach(q => q.Leverage = leverage.Value);
					}

					break;
				}

				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;

					var leverage = posMsg.Changes.TryGetValue(PositionChangeTypes.Leverage).To<decimal?>();
					if (leverage != null)
					{
						_securityPnLs.SafeAdd(posMsg.SecurityId, security => new PnLQueue(security)).Leverage = leverage.Value;
					}

					break;
				}
			}
		}
	}
}