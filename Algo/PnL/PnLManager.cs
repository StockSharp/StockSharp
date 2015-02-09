namespace StockSharp.Algo.PnL
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Менеджер прибыли-убытка.
	/// </summary>
	public class PnLManager : IPnLManager
	{
		private readonly CachedSynchronizedDictionary<string, PortfolioPnLManager> _portfolioManagers = new CachedSynchronizedDictionary<string, PortfolioPnLManager>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Создать <see cref="PnLManager"/>.
		/// </summary>
		public PnLManager()
		{
		}

		/// <summary>
		/// Суммарное значение прибыли-убытка.
		/// </summary>
		public virtual decimal PnL
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
		/// Значение нереализованной прибыли-убытка.
		/// </summary>
		public virtual decimal UnrealizedPnL
		{
			get { return _portfolioManagers.CachedValues.Sum(p => p.UnrealizedPnL); }
		}

		/// <summary>
		/// Обнулить <see cref="PnL"/>.
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
		/// Рассчитать прибыльность сделки. Если сделка уже ранее была обработана, то возвращается предыдущая информация.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Информация о новой сделке.</returns>
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
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		public void ProcessMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			foreach (var pnLManager in _portfolioManagers.CachedValues)
				pnLManager.ProcessMessage(message);
		}
	}
}