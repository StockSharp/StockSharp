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

	using StockSharp.Algo.PnL;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The statistics manager.
	/// </summary>
	public class StatisticManager
	{
		private sealed class EquityParameterList : CachedSynchronizedSet<IStatisticParameter>
		{
			private IPnLStatisticParameter[] _pnlParams;

			public IPnLStatisticParameter[] PnLParams
			{
				get
				{
					lock (SyncRoot)
						return _pnlParams ??= this.OfType<IPnLStatisticParameter>().ToArray();
				}
			}

			private ITradeStatisticParameter[] _tradeParams;

			public ITradeStatisticParameter[] TradeParams
			{
				get
				{
					lock (SyncRoot)
						return _tradeParams ??= this.OfType<ITradeStatisticParameter>().ToArray();
				}
			}

			private IPositionStatisticParameter[] _positionParams;

			public IPositionStatisticParameter[] PositionParams
			{
				get
				{
					lock (SyncRoot)
						return _positionParams ??= this.OfType<IPositionStatisticParameter>().ToArray();
				}
			}

			private IOrderStatisticParameter[] _orderParams;

			public IOrderStatisticParameter[] OrderParams
			{
				get
				{
					lock (SyncRoot)
						return _orderParams ??= this.OfType<IOrderStatisticParameter>().ToArray();
				}
			}

			protected override bool OnRemoving(IStatisticParameter item)
			{
				item.DoDispose();
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this.ToArray())
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

		private readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="StatisticManager"/>.
		/// </summary>
		public StatisticManager()
		{
			var maxPf = new MaxProfitParameter();
			var maxDd = new MaxDrawdownParameter();
			var netPf = new NetProfitParameter();

			_parameters = new()
			{
				maxPf,
				new MaxProfitDateParameter(maxPf),
				maxDd,
				new MaxDrawdownDateParameter(maxDd),
				new MaxRelativeDrawdownParameter(),
				new MaxDrawdownPercentParameter(maxDd),
				new ReturnParameter(),
				netPf,
				new NetProfitPercentParameter(),
				new RecoveryFactorParameter(maxDd, netPf),
				new CommissionParameter(),

				new WinningTradesParameter(),
				new AverageWinTradeParameter(),
				new LossingTradesParameter(),
				new AverageLossTradeParameter(),
				new PerMonthTradeParameter(),
				new PerDayTradeParameter(),
				new RoundtripCountParameter(),
				new AverageTradeProfitParameter(),
				new TradeCountParameter(),

				new MaxLongPositionParameter(),
				new MaxShortPositionParameter(),

				new MaxLatencyRegistrationParameter(),
				new MinLatencyRegistrationParameter(),
				new MaxLatencyCancellationParameter(),
				new MinLatencyCancellationParameter(),
				new OrderCountParameter(),
				new OrderErrorCountParameter(),
				new OrderInsufficientFundErrorCountParameter(),
				//new MaxSlippageParameter(),
				//new MinSlippageParameter()
			};

			foreach (var p in _parameters.Cache)
				_dict.Add(p.Type, p);
		}

		private readonly EquityParameterList _parameters;

		/// <summary>
		/// Calculated parameters.
		/// </summary>
		public IStatisticParameter[] Parameters => _parameters.Cache;

		/// <summary>
		/// Get <see cref="IStatisticParameter"/> by the specified type.
		/// </summary>
		/// <param name="type"><see cref="StatisticParameterTypes"/></param>
		/// <returns><see cref="IStatisticParameter"/></returns>
		public IStatisticParameter GetByType(StatisticParameterTypes type)
			=> _dict[type];

		/// <summary>
		/// Init by initial value.
		/// </summary>
		/// <param name="beginValue">Initial value.</param>
		public virtual void Init(decimal beginValue)
			=> _parameters.PnLParams.ForEach(p => p.Init(beginValue));

		/// <summary>
		/// To add the new profit-loss value.
		/// </summary>
		/// <param name="time">The change time <paramref name="pnl" />.</param>
		/// <param name="pnl">New profit-loss value.</param>
		/// <param name="commission">Commission.</param>
		public virtual void AddPnL(DateTimeOffset time, decimal pnl, decimal? commission)
			=> _parameters.PnLParams.ForEach(p => p.Add(time, pnl, commission));

		/// <summary>
		/// To add the new position value.
		/// </summary>
		/// <param name="time">The change time <paramref name="position" />.</param>
		/// <param name="position">The new position value.</param>
		public virtual void AddPosition(DateTimeOffset time, decimal position)
			=> _parameters.PositionParams.ForEach(p => p.Add(time, position));

		/// <summary>
		/// To add information about new trade.
		/// </summary>
		/// <param name="info">Information on new trade.</param>
		public virtual void AddMyTrade(PnLInfo info)
			=> _parameters.TradeParams.ForEach(p => p.Add(info));

		/// <summary>
		/// To add new order.
		/// </summary>
		/// <param name="order">New order.</param>
		public virtual void AddNewOrder(Order order)
			=> _parameters.OrderParams.ForEach(p => p.New(order));

		/// <summary>
		/// To add the changed order.
		/// </summary>
		/// <param name="order">The changed order.</param>
		public virtual void AddChangedOrder(Order order)
			=> _parameters.OrderParams.ForEach(p => p.Changed(order));

		/// <summary>
		/// To add the order registration error.
		/// </summary>
		/// <param name="fail">Error registering order.</param>
		public virtual void AddRegisterFailedOrder(OrderFail fail)
			=> _parameters.OrderParams.ForEach(p => p.RegisterFailed(fail));

		/// <summary>
		/// To add the order cancelling error.
		/// </summary>
		/// <param name="fail">The order error.</param>
		public virtual void AddFailedOrderCancel(OrderFail fail)
			=> _parameters.OrderParams.ForEach(p => p.CancelFailed(fail));

		/// <summary>
		/// To clear data on equity.
		/// </summary>
		public virtual void Reset()
			=> _parameters.SyncDo(c => c.ForEach(p => p.Reset()));

		/// <summary>
		/// Return all available parameters.
		/// </summary>
		public static IStatisticParameter[] GetAllParameters() => new StatisticManager().Parameters;
	}
}
