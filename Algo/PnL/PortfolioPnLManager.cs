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
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The profit-loss manager, related for specified <see cref="PortfolioName"/>.
	/// </summary>
	public class PortfolioPnLManager : IPnLManager
	{
		private readonly Dictionary<string, PnLInfo> _tradeByStringIdInfos = new Dictionary<string, PnLInfo>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<long, PnLInfo> _tradeByIdInfos = new Dictionary<long, PnLInfo>();
		private readonly CachedSynchronizedDictionary<SecurityId, PnLQueue> _securityPnLs = new CachedSynchronizedDictionary<SecurityId, PnLQueue>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioPnLManager"/>.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		public PortfolioPnLManager(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			PortfolioName = portfolioName;
		}

		/// <summary>
		/// Portfolio name.
		/// </summary>
		public string PortfolioName { get; }

		/// <inheritdoc />
		public decimal PnL => RealizedPnL + UnrealizedPnL ?? 0;

		private decimal _realizedPnL;

		/// <inheritdoc />
		public virtual decimal RealizedPnL => _realizedPnL;

		/// <inheritdoc />
		public void Reset()
		{
			_realizedPnL = 0;
			_securityPnLs.Clear();

			_tradeByStringIdInfos.Clear();
			_tradeByIdInfos.Clear();
		}

		PnLInfo IPnLManager.ProcessMessage(Message message, ICollection<PortfolioPnLManager> changedPortfolios)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public decimal? UnrealizedPnL => _securityPnLs.CachedValues.Sum(q => q.UnrealizedPnL);

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

			info = null;

			var tradeId = trade.TradeId;
			var tradeStringId = trade.TradeStringId;

			if (tradeId != null)
			{
				if (_tradeByIdInfos.TryGetValue(tradeId.Value, out info))
					return false;

				var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

				info = queue.Process(trade);

				_tradeByIdInfos.Add(tradeId.Value, info);
				_realizedPnL += info.PnL;
				return true;
			}
			else if (!tradeStringId.IsEmpty())
			{
				if (_tradeByStringIdInfos.TryGetValue(tradeStringId, out info))
					return false;

				var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

				info = queue.Process(trade);

				_tradeByStringIdInfos.Add(tradeStringId, info);
				_realizedPnL += info.PnL;
				return true;
			}

			return false;
		}

		/// <summary>
		/// To process the message, containing market data.
		/// </summary>
		/// <param name="message">The message, containing market data.</param>
		/// <returns><see cref="PnL"/> was changed.</returns>
		public bool ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType != ExecutionTypes.Tick)
						break;

					var queue = _securityPnLs.TryGetValue(execMsg.SecurityId);

					if (queue == null)
						break;

					queue.ProcessExecution(execMsg);
					return true;
				}

				case MessageTypes.Level1Change:
				{
					var levelMsg = (Level1ChangeMessage)message;

					var queue = _securityPnLs.TryGetValue(levelMsg.SecurityId);

					if (queue == null)
						break;

					queue.ProcessLevel1(levelMsg);
					return true;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					if (quoteMsg.State != null)
						break;

					var queue = _securityPnLs.TryGetValue(quoteMsg.SecurityId);

					if (queue == null)
						break;

					queue.ProcessQuotes(quoteMsg);
					return true;
				}

				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;

					var leverage = posMsg.TryGetDecimal(PositionChangeTypes.Leverage);
					if (leverage != null)
					{
						if (posMsg.IsMoney())
							_securityPnLs.CachedValues.ForEach(q => q.Leverage = leverage.Value);
						else
							_securityPnLs.SafeAdd(posMsg.SecurityId, security => new PnLQueue(security)).Leverage = leverage.Value;
					}

					break;
				}
			}

			return false;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			
		}
	}
}