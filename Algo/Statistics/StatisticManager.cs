namespace StockSharp.Algo.Statistics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.PnL;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Менеджер статистики.
	/// </summary>
	public class StatisticManager
	{
		private sealed class EquityParameterList : CachedSynchronizedSet<IStatisticParameter>
		{
			private IPnLStatisticParameter[] _pnlParams;

			public IEnumerable<IPnLStatisticParameter> PnLParams
			{
				get
				{
					lock (SyncRoot)
						return _pnlParams ?? (_pnlParams = this.OfType<IPnLStatisticParameter>().ToArray());
				}
			}

			private ITradeStatisticParameter[] _tradeParams;

			public IEnumerable<ITradeStatisticParameter> TradeParams
			{
				get
				{
					lock (SyncRoot)
						return _tradeParams ?? (_tradeParams = this.OfType<ITradeStatisticParameter>().ToArray());
				}
			}

			private IPositionStatisticParameter[] _positionParams;

			public IEnumerable<IPositionStatisticParameter> PositionParams
			{
				get
				{
					lock (SyncRoot)
						return _positionParams ?? (_positionParams = this.OfType<IPositionStatisticParameter>().ToArray());
				}
			}

			private IOrderStatisticParameter[] _orderParams;

			public IEnumerable<IOrderStatisticParameter> OrderParams
			{
				get
				{
					lock (SyncRoot)
						return _orderParams ?? (_orderParams = this.OfType<IOrderStatisticParameter>().ToArray());
				}
			}

			protected override bool OnRemoving(IStatisticParameter item)
			{
				item.DoDispose();
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in ToArray())
					Remove(item);

				return true;
			}

			protected override void OnChanged()
			{
				_pnlParams = null;
				_orderParams = null;
				_positionParams = null;
				_tradeParams = null;

				base.OnChanged();
			}
		}

		/// <summary>
		/// Создать <see cref="StatisticManager"/>.
		/// </summary>
		public StatisticManager()
		{
			Parameters.Add(new MaxProfitParameter());
			Parameters.Add(new MaxDrawdownParameter());
			Parameters.Add(new MaxRelativeDrawdownParameter());
			Parameters.Add(new ReturnParameter());
			Parameters.Add(new RecoveryFactorParameter());
			Parameters.Add(new NetProfitParameter());
			Parameters.Add(new WinningTradesParameter());
			Parameters.Add(new AverageWinTradeParameter());
			Parameters.Add(new LossingTradesParameter());
			Parameters.Add(new AverageLossTradeParameter());
			Parameters.Add(new RoundtripCountParameter());
			Parameters.Add(new AverageTradeParameter());
			Parameters.Add(new TradeCountParameter());
			Parameters.Add(new MaxLongPositionParameter());
			Parameters.Add(new MaxShortPositionParameter());
			Parameters.Add(new MaxLatencyRegistrationParameter());
			Parameters.Add(new MinLatencyRegistrationParameter());
			Parameters.Add(new MaxLatencyCancellationParameter());
			Parameters.Add(new MinLatencyCancellationParameter());
			Parameters.Add(new OrderCountParameter());
			//Parameters.Add(new MaxSlippageParameter());
			//Parameters.Add(new MinSlippageParameter());
		}

		private readonly EquityParameterList _parameters = new EquityParameterList();

		/// <summary>
		/// Вычисляемые параметры.
		/// </summary>
		public ISynchronizedCollection<IStatisticParameter> Parameters
		{
			get { return _parameters; }
		}

		/// <summary>
		/// Добавить новое значение прибыли-убытка.
		/// </summary>
		/// <param name="time">Время изменения <paramref name="pnl"/>.</param>
		/// <param name="pnl">Новое значение прибыли-убытка.</param>
		public virtual void AddPnL(DateTimeOffset time, decimal pnl)
		{
			_parameters.PnLParams.ForEach(p => p.Add(time, pnl));
		}

		/// <summary>
		/// Добавить новое значение позиции.
		/// </summary>
		/// <param name="time">Время изменения <paramref name="position"/>.</param>
		/// <param name="position">Новое значение позиции.</param>
		public virtual void AddPosition(DateTimeOffset time, decimal position)
		{
			_parameters.PositionParams.ForEach(p => p.Add(time, position));
		}

		/// <summary>
		/// Добавить информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public virtual void AddMyTrade(PnLInfo info)
		{
			_parameters.TradeParams.ForEach(p => p.Add(info));
		}

		/// <summary>
		/// Добавить новую заявку.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		public virtual void AddNewOrder(Order order)
		{
			_parameters.OrderParams.ForEach(p => p.New(order));
		}

		/// <summary>
		/// Добавить измененную заявку.
		/// </summary>
		/// <param name="order">Измененная заявка.</param>
		public virtual void AddChangedOrder(Order order)
		{
			_parameters.OrderParams.ForEach(p => p.Changed(order));
		}

		/// <summary>
		/// Добавить ошибку регистрации заявки.
		/// </summary>
		/// <param name="fail">Ошибка регистрации заявки.</param>
		public virtual void AddRegisterFailedOrder(OrderFail fail)
		{
			_parameters.OrderParams.ForEach(p => p.RegisterFailed(fail));
		}

		/// <summary>
		/// Добавить ошибку отмены заявки.
		/// </summary>
		/// <param name="fail">Ошибка заявки.</param>
		public virtual void AddFailedOrderCancel(OrderFail fail)
		{
			_parameters.OrderParams.ForEach(p => p.CancelFailed(fail));
		}

		/// <summary>
		/// Очистить данные по эквити.
		/// </summary>
		public virtual void Reset()
		{
			_parameters.SyncDo(c => c.ForEach(p => p.Reset()));
		}
	}
}