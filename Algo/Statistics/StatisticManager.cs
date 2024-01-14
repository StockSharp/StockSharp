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
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Algo.PnL;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The statistics manager.
	/// </summary>
	public class StatisticManager : IStatisticManager
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

			private readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = new();

			public bool TryGetValue(StatisticParameterTypes type, out IStatisticParameter parameter)
			{
				lock (SyncRoot)
					return _dict.TryGetValue(type, out parameter);
			}

			protected override bool OnAdding(IStatisticParameter item)
			{
				_dict.Add(item.Type, item);
				return base.OnAdding(item);
			}

			protected override bool OnRemoving(IStatisticParameter item)
			{
				_dict.Remove(item.Type);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				_dict.Clear();
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
			};
		}

		private readonly EquityParameterList _parameters;

		/// <inheritdoc />
		public IStatisticParameter[] Parameters => _parameters.Cache;

		void IStatisticManager.AddPnL(DateTimeOffset time, decimal pnl, decimal? commission)
			=> _parameters.PnLParams.ForEach(p => p.Add(time, pnl, commission));

		void IStatisticManager.AddPosition(DateTimeOffset time, decimal position)
			=> _parameters.PositionParams.ForEach(p => p.Add(time, position));

		void IStatisticManager.AddMyTrade(PnLInfo info)
			=> _parameters.TradeParams.ForEach(p => p.Add(info));

		void IStatisticManager.AddNewOrder(Order order)
			=> _parameters.OrderParams.ForEach(p => p.New(order));

		void IStatisticManager.AddChangedOrder(Order order)
			=> _parameters.OrderParams.ForEach(p => p.Changed(order));

		void IStatisticManager.AddRegisterFailedOrder(OrderFail fail)
			=> _parameters.OrderParams.ForEach(p => p.RegisterFailed(fail));

		void IStatisticManager.AddFailedOrderCancel(OrderFail fail)
			=> _parameters.OrderParams.ForEach(p => p.CancelFailed(fail));

		void IStatisticManager.Reset()
			=> _parameters.SyncDo(c => c.ForEach(p => p.Reset()));

		void IPersistable.Load(SettingsStorage storage)
		{
			foreach (var ps in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Parameters)))
			{
				var type = ps.GetValue<StatisticParameterTypes?>(nameof(IStatisticParameter.Type));

				if (type is null)
					continue;

				if (_parameters.TryGetValue(type.Value, out var p))
					p.Load(ps);
			}
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.Set(nameof(Parameters), Parameters.Select(p =>
			{
				var s = p.Save();
				s.Set(nameof(p.Type), (int)p.Type);
				return s;
			}).ToArray());
		}
	}
}