namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The statistics manager.
/// </summary>
public class StatisticManager : IStatisticManager
{
	private class EquityParameterList : CachedSynchronizedSet<IStatisticParameter>
	{
		private IPnLStatisticParameter[] _pnlParams;

		public IPnLStatisticParameter[] PnLParams
		{
			get
			{
				lock (SyncRoot)
					return _pnlParams ??= [.. this.OfType<IPnLStatisticParameter>()];
			}
		}

		private ITradeStatisticParameter[] _tradeParams;

		public ITradeStatisticParameter[] TradeParams
		{
			get
			{
				lock (SyncRoot)
					return _tradeParams ??= [.. this.OfType<ITradeStatisticParameter>()];
			}
		}

		private IPositionStatisticParameter[] _positionParams;

		public IPositionStatisticParameter[] PositionParams
		{
			get
			{
				lock (SyncRoot)
					return _positionParams ??= [.. this.OfType<IPositionStatisticParameter>()];
			}
		}

		private IOrderStatisticParameter[] _orderParams;

		public IOrderStatisticParameter[] OrderParams
		{
			get
			{
				lock (SyncRoot)
					return _orderParams ??= [.. this.OfType<IOrderStatisticParameter>()];
			}
		}

		private readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = [];

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
		_parameters.AddRange(StatisticParameterRegistry.CreateAll());
	}

	private readonly EquityParameterList _parameters = [];

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