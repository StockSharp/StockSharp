namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The statistics manager.
/// </summary>
public class StatisticManager : Disposable, IStatisticManager
{
	private class EquityParameterList : CachedSynchronizedSet<IStatisticParameter>
	{
		private IPnLStatisticParameter[] _pnlParams;

		public IPnLStatisticParameter[] PnLParams
		{
			get
			{
				using (EnterScope())
					return _pnlParams ??= [.. this.OfType<IPnLStatisticParameter>()];
			}
		}

		private ITradeStatisticParameter[] _tradeParams;

		public ITradeStatisticParameter[] TradeParams
		{
			get
			{
				using (EnterScope())
					return _tradeParams ??= [.. this.OfType<ITradeStatisticParameter>()];
			}
		}

		private IPositionStatisticParameter[] _positionParams;

		public IPositionStatisticParameter[] PositionParams
		{
			get
			{
				using (EnterScope())
					return _positionParams ??= [.. this.OfType<IPositionStatisticParameter>()];
			}
		}

		private IOrderStatisticParameter[] _orderParams;

		public IOrderStatisticParameter[] OrderParams
		{
			get
			{
				using (EnterScope())
					return _orderParams ??= [.. this.OfType<IOrderStatisticParameter>()];
			}
		}

		private readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = [];

		public bool TryGetValue(StatisticParameterTypes type, out IStatisticParameter parameter)
		{
			using (EnterScope())
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

	void IStatisticManager.AddPnL(DateTime time, decimal pnl, decimal? commission)
		=> _parameters.PnLParams.ForEach(p => p.Add(time, pnl, commission));

	void IStatisticManager.AddPosition(DateTime time, decimal position)
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
		=> _parameters.Cache.ForEach(p => p.Reset());

	async Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		foreach (var ps in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Parameters)))
		{
			var type = ps.GetValue<StatisticParameterTypes?>(nameof(IStatisticParameter.Type));

			if (type is null)
				continue;

			if (_parameters.TryGetValue(type.Value, out var p))
				await p.LoadAsync(ps, cancellationToken);
		}
	}

	async Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var parameters = new List<SettingsStorage>();

		foreach (var p in Parameters)
		{
			var s = await p.SaveAsync(cancellationToken);
			s.Set(nameof(p.Type), (int)p.Type);
			parameters.Add(s);
		}

		storage.Set(nameof(Parameters), parameters.ToArray());
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		foreach (var parameter in Parameters)
			parameter.Dispose();

		base.DisposeManaged();
	}
}