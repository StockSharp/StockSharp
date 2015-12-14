#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Statistics.Algo
File: StatisticManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// The statistics manager.
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
		/// Initializes a new instance of the <see cref="StatisticManager"/>.
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
		/// Calculated parameters.
		/// </summary>
		public ISynchronizedCollection<IStatisticParameter> Parameters => _parameters;

		/// <summary>
		/// To add the new profit-loss value.
		/// </summary>
		/// <param name="time">The change time <paramref name="pnl" />.</param>
		/// <param name="pnl">New profit-loss value.</param>
		public virtual void AddPnL(DateTimeOffset time, decimal pnl)
		{
			_parameters.PnLParams.ForEach(p => p.Add(time, pnl));
		}

		/// <summary>
		/// To add the new position value.
		/// </summary>
		/// <param name="time">The change time <paramref name="position" />.</param>
		/// <param name="position">The new position value.</param>
		public virtual void AddPosition(DateTimeOffset time, decimal position)
		{
			_parameters.PositionParams.ForEach(p => p.Add(time, position));
		}

		/// <summary>
		/// To add information about new trade.
		/// </summary>
		/// <param name="info">Information on new trade.</param>
		public virtual void AddMyTrade(PnLInfo info)
		{
			_parameters.TradeParams.ForEach(p => p.Add(info));
		}

		/// <summary>
		/// To add new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public virtual void AddNewOrder(Order order)
		{
			_parameters.OrderParams.ForEach(p => p.New(order));
		}

		/// <summary>
		/// To add the changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		public virtual void AddChangedOrder(Order order)
		{
			_parameters.OrderParams.ForEach(p => p.Changed(order));
		}

		/// <summary>
		/// To add the order registration error.
		/// </summary>
		/// <param name="fail">Error registering order.</param>
		public virtual void AddRegisterFailedOrder(OrderFail fail)
		{
			_parameters.OrderParams.ForEach(p => p.RegisterFailed(fail));
		}

		/// <summary>
		/// To add the order cancelling error.
		/// </summary>
		/// <param name="fail">The order error.</param>
		public virtual void AddFailedOrderCancel(OrderFail fail)
		{
			_parameters.OrderParams.ForEach(p => p.CancelFailed(fail));
		}

		/// <summary>
		/// To clear data on equity.
		/// </summary>
		public virtual void Reset()
		{
			_parameters.SyncDo(c => c.ForEach(p => p.Reset()));
		}
	}
}