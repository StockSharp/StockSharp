namespace StockSharp.Algo.PnL
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	class PortfolioPnLManager
	{
		private readonly Dictionary<long, PnLInfo> _tradeInfos = new Dictionary<long, PnLInfo>();
		private readonly CachedSynchronizedDictionary<SecurityId, PnLQueue> _securityPnLs = new CachedSynchronizedDictionary<SecurityId, PnLQueue>();

		public PortfolioPnLManager(string portfolioName)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException("portfolioName");

			PortfolioName = portfolioName;
		}

		public string PortfolioName { get; private set; }

		/// <summary>
		/// Суммарное значение прибыли-убытка.
		/// </summary>
		public decimal PnL
		{
			get { return RealizedPnL + UnrealizedPnL; }
		}

		private decimal _realizedPnL;

		/// <summary>
		/// Относительное значение прибыли-убытка без учета открытой позиции.
		/// </summary>
		public virtual decimal RealizedPnL
		{
			get { return _realizedPnL; }
		}

		/// <summary>
		/// Обнулить <see cref="PnL"/>.
		/// </summary>
		public void Reset()
		{
			_realizedPnL = 0;
			_securityPnLs.Clear();
		}

		public decimal UnrealizedPnL
		{
			get { return _securityPnLs.CachedValues.Sum(q => q.UnrealizedPnL); }
		}

		/// <summary>
		/// Рассчитать прибыльность сделки. Если сделка уже ранее была обработана, то возвращается предыдущая информация.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <param name="info">Информация о новой сделке.</param>
		/// <returns><see langword="true"/>, если получена новая сделка, иначе, <see langword="false"/>.</returns>
		public bool ProcessMyTrade(ExecutionMessage trade, out PnLInfo info)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			if (_tradeInfos.TryGetValue(trade.TradeId, out info))
				return false;

			var queue = _securityPnLs.SafeAdd(trade.SecurityId, security => new PnLQueue(security));

			info = queue.Process(trade);

			_tradeInfos.Add(trade.TradeId, info);
			_realizedPnL += info.PnL;

			return true;
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
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

					if (queue != null)
						queue.ProcessExecution(execMsg);

					break;
				}

				case MessageTypes.Level1Change:
				{
					var levelMsg = (Level1ChangeMessage)message;
					var queue = _securityPnLs.TryGetValue(levelMsg.SecurityId);

					if (queue != null)
						queue.ProcessLevel1(levelMsg);

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;
					var queue = _securityPnLs.TryGetValue(quoteMsg.SecurityId);

					if (queue != null)
						queue.ProcessQuotes(quoteMsg);

					break;
				}
			}
		}
	}
}