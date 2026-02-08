namespace StockSharp.Diagram.Elements;

using StockSharp.Algo.PositionManagement;
using StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Additional order condition based on current position.
/// </summary>
public enum PositionConditions
{
	/// <summary>
	/// No additional condition.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NoneKey,
		Description = LocalizedStrings.PosConditionNoneKey)]
	NoCondition,

	/// <summary>
	/// Open position only. Only send order if the current position is zero.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionOpenKey,
		Description = LocalizedStrings.PosConditionOpenDetailsKey)]
	OpenPosition,

	/// <summary>
	/// Increase position only. Only send order if it is of the same direction as the current position or if the current position is zero.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionIncreaseOnlyKey,
		Description = LocalizedStrings.PosConditionIncreaseOnlyDetailsKey)]
	IncreaseOnly,

	/// <summary>
	/// Reduce position only. Only send order if it is of the opposite direction of the current non-zero position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionReduceOnlyKey,
		Description = LocalizedStrings.PosConditionReduceOnlyDetailsKey)]
	ReduceOnly,

	/// <summary>
	/// Close position. Order volume is calculated automatically based on current position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionCloseKey,
		Description = LocalizedStrings.PosConditionCloseDetailsKey)]
	ClosePosition,

	/// <summary>
	/// Invert position to the opposite of the current one. Order volume is calculated automatically based on current position.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PosConditionInvertKey,
		Description = LocalizedStrings.PosConditionInvertDetailsKey)]
	InvertPosition,
}

/// <summary>
/// Position modification algorithms.
/// </summary>
public enum PositionModifyAlgorithms
{
	/// <summary>
	/// Change position using market order.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarketOrdersKey,
		Description = LocalizedStrings.PosModifyMarketOrdersKey)]
	MarketOrder,

	/// <summary>
	/// Change position using the VWAP algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VWAPKey,
		Description = LocalizedStrings.PosModifyVWAPKey)]
	VWAP,

	/// <summary>
	/// Change position using the TWAP algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TWAPKey,
		Description = LocalizedStrings.PosModifyTWAPKey)]
	TWAP,

	/// <summary>
	/// Change position using the Iceberg algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IcebergKey,
		Description = LocalizedStrings.PosModifyIcebergKey)]
	Iceberg,

	/// <summary>
	/// Change position using quoting-based accumulation.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.QuotingKey,
		Description = LocalizedStrings.PosModifyQuotingKey)]
	Quoting,
}

/// <summary>
/// Element that changes position (open, close, reduce, reverse).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PositionModifyKey,
	Description = LocalizedStrings.PositionModifyDescKey,
	GroupName = LocalizedStrings.PositionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/positions/modify.html")]
public class PositionModifyElement : OrderRegisterBaseDiagramElement
{
	/// <summary>
	/// Wrapper that adapts IPositionModifyAlgo to the diagram element's order lifecycle.
	/// </summary>
	private sealed class AlgoWrapper(PositionModifyElement parent, Security security, Portfolio portfolio, IPositionModifyAlgo algo)
	{
		private readonly PositionModifyElement _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private readonly Security _security = security ?? throw new ArgumentNullException(nameof(security));
		private readonly Portfolio _portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
		private readonly IPositionModifyAlgo _algo = algo ?? throw new ArgumentNullException(nameof(algo));

		private Order _currOrder;

		public IPositionModifyAlgo Algo => _algo;

		public void UpdateMarketData(DateTime time, decimal? price, decimal? volume)
		{
			_algo.UpdateMarketData(time, price, volume);
			ProcessNextAction(time);
		}

		public void UpdateOrderBook(DateTime time, IOrderBookMessage depth)
		{
			_algo.UpdateOrderBook(depth);
			ProcessNextAction(time);
		}

		public void Cancel()
		{
			_algo.Cancel();

			if (_currOrder is not null && !_currOrder.State.IsFinal())
				_parent.Strategy.CancelOrder(_currOrder);
		}

		public void ProcessNextAction(DateTime time)
		{
			if (_algo.IsFinished)
			{
				ResetAlgo(time, _algo.RemainingVolume);
				return;
			}

			if (_currOrder is not null)
				return;

			var action = _algo.GetNextAction();

			switch (action.ActionType)
			{
				case PositionModifyAction.ActionTypes.None:
					break;

				case PositionModifyAction.ActionTypes.Register:
					RegisterOrder(action);
					break;

				case PositionModifyAction.ActionTypes.Cancel:
					if (_currOrder is not null && !_currOrder.State.IsFinal())
						_parent.Strategy.CancelOrder(_currOrder);
					break;

				case PositionModifyAction.ActionTypes.Finished:
					ResetAlgo(time, _algo.RemainingVolume);
					break;
			}
		}

		private void RegisterOrder(PositionModifyAction action)
		{
			var order = new Order
			{
				Portfolio = _portfolio,
				Security = _security,
				Side = action.Side!.Value,
				Type = action.OrderType ?? OrderTypes.Market,
				Volume = action.Volume!.Value,
				ClientCode = _parent.ClientCode,
				BrokerCode = _parent.BrokerCode,
				IsManual = _parent.IsManual,
				MarginMode = _parent.MarginMode,
				IsMarketMaker = _parent.IsMarketMaker,
				TimeInForce = _parent.TimeInForce,
				Comment = _parent.Comment,
			};

			if (action.Price is decimal price)
				order.Price = price;

			var strategy = _parent.Strategy;

			order
				.WhenMatched(strategy)
				.Do(ord =>
				{
					_currOrder = null;
					var matchedVol = ord.Volume;
					_algo.OnOrderMatched(matchedVol);

					var time = ord.MatchedTime ?? ord.ServerTime;

					if (_parent._orderSocket is not null)
					{
						_parent.RaiseProcessOutput(_parent._orderSocket, time, ord);
						strategy.Flush(ord);
					}

					if (_algo.IsFinished)
					{
						ResetAlgo(time, 0);
						strategy.Flush(ord);
					}
					else
					{
						ProcessNextAction(time);
					}
				})
				.Apply(strategy);

			order
				.WhenRegisterFailed(strategy)
				.Do(fail =>
				{
					_currOrder = null;
					_algo.OnOrderFailed();

					ResetAlgo(fail.ServerTime, _algo.RemainingVolume);
					strategy.Flush(fail);
				})
				.Apply(strategy);

			order
				.WhenCanceled(strategy)
				.Do(ord =>
				{
					_currOrder = null;
					var matchedVol = ord.GetMatchedVolume() ?? 0;
					_algo.OnOrderCanceled(matchedVol);

					var time = ord.CancelledTime ?? ord.ServerTime;

					if (_algo.IsFinished)
					{
						ResetAlgo(time, _algo.RemainingVolume);
						strategy.Flush(ord);
					}
					else
					{
						ProcessNextAction(time);
					}
				})
				.Apply(strategy);

			if (_parent._isTradeSocket)
			{
				order
					.WhenNewTrade(strategy)
					.Do(trade =>
					{
						_parent.RaiseProcessOutput(_parent._tradeSocket, trade.Trade.ServerTime, trade);
						strategy.Flush(trade.Trade);
					})
					.Apply(strategy);
			}

			_currOrder = order;
			strategy.RegisterOrder(order);
		}

		private void ResetAlgo(DateTime time, decimal remainingVolume)
		{
			_parent._currAlgo = null;
			_parent.RaiseProcessOutput(_parent._remainVolumeSocket, time, remainingVolume);
		}
	}

	private readonly DiagramSocket _securitySocket;
	private readonly DiagramSocket _portfolioSocket;
	//private readonly DiagramSocket _cancelSocket;
	private DiagramSocket _volumeSocket;

	private DiagramSocket _lastPriceSocket;
	private DiagramSocket _lastVolumeSocket;
	private DiagramSocket _orderBookSocket;

	private readonly DiagramSocket _remainVolumeSocket;
	private readonly DiagramSocket _orderSocket;
	private readonly DiagramSocket _tradeSocket;

	private bool _isTradeSocket;

	private decimal? _lastPrice;
	private decimal? _lastVolume;

	private AlgoWrapper _currAlgo;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "953961CD-A9BA-4AFE-AC38-E8B61F84B3BE".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Notepad";

	private readonly DiagramElementParam<Sides?> _direction;

	/// <summary>
	/// Direction.
	/// </summary>
	public Sides? Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	private readonly DiagramElementParam<PositionConditions> _posCondition;

	/// <summary>
	/// <see cref="PositionConditions"/>
	/// </summary>
	public PositionConditions PosCondition
	{
		get => _posCondition.Value;
		set => _posCondition.Value = value;
	}

	private readonly DiagramElementParam<PositionModifyAlgorithms> _algorithm;

	/// <summary>
	/// <see cref="PositionModifyAlgorithms"/>
	/// </summary>
	public PositionModifyAlgorithms Algorithm
	{
		get => _algorithm.Value;
		set => _algorithm.Value = value;
	}

	private readonly DiagramElementParam<Unit> _volumePart;

	/// <summary>
	/// Volume part that <see cref="PositionModifyAlgorithms.VWAP"/> uses for split target volume.
	/// </summary>
	public Unit VolumePart
	{
		get => _volumePart.Value;
		set => _volumePart.Value = value;
	}

	private readonly DiagramElementParam<TimeSpan> _timeInterval;

	/// <summary>
	/// Time interval that <see cref="PositionModifyAlgorithms.TWAP"/> uses for split time range.
	/// </summary>
	public TimeSpan TimeInterval
	{
		get => _timeInterval.Value;
		set => _timeInterval.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionModifyElement"/>.
	/// </summary>
	public PositionModifyElement()
	{
		_securitySocket		= AddInput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security, index: 0);
		_portfolioSocket	= AddInput(StaticSocketIds.Portfolio, LocalizedStrings.Portfolio, DiagramSocketType.Portfolio, index: 4);
		/*_cancelSocket		= */AddInput(StaticSocketIds.Flag, LocalizedStrings.Cancel, DiagramSocketType.Bool, OnProcessCancel, index: 5);
		UpdateLastSockets(true);
		UpdateVolumeSocket(true);

		_remainVolumeSocket = AddOutput(StaticSocketIds.Volume, LocalizedStrings.Balance, DiagramSocketType.Unit);
		_orderSocket = AddOutput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order);
		_tradeSocket = AddOutput(StaticSocketIds.MyTrade, LocalizedStrings.OwnTrade, DiagramSocketType.MyTrade);

		_posCondition = AddParam<PositionConditions>(nameof(PosCondition))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Condition, LocalizedStrings.PosCondition, 51)
			.SetOnValueChangedHandler(_ =>
			{
				UpdateVolumeSocket();
				UpdateElementName();
			});

		_direction = AddParam<Sides?>(nameof(Direction))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Direction, LocalizedStrings.PosSide, 52)
			.SetOnValueChangedHandler(_ => UpdateElementName());

		_algorithm = AddParam(nameof(Algorithm), PositionModifyAlgorithms.MarketOrder)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Algo, LocalizedStrings.PosModifyAlgo, 53)
			.SetOnValueChangedHandler(_ => UpdateLastSockets());

		_volumePart = AddParam(nameof(VolumePart), new Unit(1, UnitTypes.Percent))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Part, LocalizedStrings.VolumePart, 54)
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (newValue is null || newValue.Value <= 0)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.InvalidValue);
			});

		_timeInterval = AddParam(nameof(TimeInterval), TimeSpan.FromMinutes(1))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Position, LocalizedStrings.Interval, LocalizedStrings.TWAPInterval, 55)
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (newValue <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.IntervalMustBePositive);
			});
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_currAlgo = default;
		_isTradeSocket = default;
		_lastPrice = default;
		_lastVolume = default;

		base.OnReseted();
	}

	private bool NeedDirection => PosCondition is PositionConditions.NoCondition or PositionConditions.OpenPosition;
	private bool NeedVolume => PosCondition is not (PositionConditions.ClosePosition or PositionConditions.InvertPosition);
	
	private bool NeedLastPrice => Algorithm is not (PositionModifyAlgorithms.MarketOrder or PositionModifyAlgorithms.Quoting);
	private bool NeedLastVolume => Algorithm is PositionModifyAlgorithms.VWAP;
	private bool NeedOrderBook => Algorithm is PositionModifyAlgorithms.Quoting;

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (Direction is null && NeedDirection)
			throw new InvalidOperationException(LocalizedStrings.OrderSideNotSpecified);

		if (NeedLastPrice && _lastPriceSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.PriceNotSpecified);

		if (NeedLastVolume && _lastVolumeSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

		if (NeedOrderBook && _orderBookSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.MarketDepthNotSpecified);

		if (NeedVolume && _volumeSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

		_isTradeSocket = _tradeSocket.IsConnected;

		base.OnPrepare();
	}

	private void UpdateLastSockets(bool force = false)
	{
		void updateLastPriceSocket()
		{
			var needPrice = force || NeedLastPrice;

			if (needPrice == (_lastPriceSocket != null))
				return;

			if (needPrice)
				_lastPriceSocket = AddInput(GenerateSocketId("price"), LocalizedStrings.LastTradePrice, DiagramSocketType.Unit, OnProcessLastPrice, index: 10);
			else
			{
				RemoveSocket(_lastPriceSocket);
				_lastPriceSocket = null;
			}
		}

		void updateLastVolumeSocket()
		{
			var needVol = force || NeedLastVolume;

			if (needVol == (_lastVolumeSocket != null))
				return;

			if (needVol)
				_lastVolumeSocket = AddInput(GenerateSocketId("last_volume"), LocalizedStrings.LastTradeVolume, DiagramSocketType.Unit, OnProcessLastVolume, index: 11);
			else
			{
				RemoveSocket(_lastVolumeSocket);
				_lastVolumeSocket = null;
			}
		}

		void updateOrderBookSocket()
		{
			var needOrderBook = force || NeedOrderBook;

			if (needOrderBook == (_orderBookSocket != null))
				return;

			if (needOrderBook)
				_orderBookSocket = AddInput(GenerateSocketId("order_book"), LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth, OnProcessOrderBook, index: 12);
			else
			{
				RemoveSocket(_orderBookSocket);
				_orderBookSocket = null;
			}
		}

		updateLastPriceSocket();
		updateLastVolumeSocket();
		updateOrderBookSocket();
	}

	private void UpdateVolumeSocket(bool force = false)
	{
		var needVolume = force || NeedVolume;

		if (needVolume == (_volumeSocket != null))
			return;

		if (needVolume)
			_volumeSocket = AddInput(StaticSocketIds.Volume, LocalizedStrings.Volume, DiagramSocketType.Unit, index: 3);
		else
		{
			RemoveSocket(_volumeSocket);
			_volumeSocket = null;
		}
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTime time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		if (_currAlgo is not null || !CanProcess(values))
			return;

		var security = Strategy.Security;

		if (values.TryGetValue(_securitySocket, out var securityValue))
			security = securityValue.GetValue<Security>();

		if (security == null || security.IsAllSecurity())
			throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

		var portfolio = Strategy.Portfolio;

		if (values.TryGetValue(_portfolioSocket, out var portfolioValue))
			portfolio = portfolioValue.GetValue<Portfolio>();

		if (portfolio is null)
			throw new InvalidOperationException(LocalizedStrings.PortfolioNotSpecified);

		decimal? volume = null;

		if (NeedVolume)
		{
			if (_volumeSocket is null || !values.TryGetValue(_volumeSocket, out var volumeValue))
				throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

			volume = volumeValue.GetValue<decimal>();

			if (volume <= 0)
				throw new InvalidOperationException(LocalizedStrings.VolumeMustBeGreaterThanZero);
		}

		var direction = Direction;
		var currentPos = Strategy.GetPositionValue(security, portfolio) ?? default;
		var posDir = currentPos.GetDirection();

		Sides operationDir;
		decimal operationVol;

		void raiseInvalid()
			=> RaiseProcessOutput(_remainVolumeSocket, time, -1, source);

		switch (PosCondition)
		{
			case PositionConditions.NoCondition:
			{
				operationDir = direction ?? throw new InvalidOperationException(LocalizedStrings.OrderSideNotSpecified);
				operationVol = volume ?? throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);
				break;
			}
			case PositionConditions.OpenPosition:
			{
				if (posDir is not null)
				{
					raiseInvalid();
					return;
				}

				operationDir = direction ?? throw new InvalidOperationException(LocalizedStrings.OrderSideNotSpecified);
				operationVol = volume ?? throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);
				break;
			}
			case PositionConditions.ClosePosition:
			{
				if (posDir is null || (direction is not null && posDir == direction))
				{
					raiseInvalid();
					return;
				}

				operationDir = posDir.Value.Invert();
				operationVol = currentPos.Abs();
				break;
			}
			case PositionConditions.IncreaseOnly:
			{
				// Allow if position is zero (posDir is null) or same direction as current position
				if (posDir is not null && direction is not null && posDir != direction)
				{
					raiseInvalid();
					return;
				}

				operationDir = posDir ?? direction ?? throw new InvalidOperationException(LocalizedStrings.OrderSideNotSpecified);
				operationVol = volume ?? throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);
				break;
			}
			case PositionConditions.ReduceOnly:
			{
				if (posDir is null || (direction is not null && posDir == direction))
				{
					raiseInvalid();
					return;
				}

				operationDir = posDir.Value.Invert();
				operationVol = (volume ?? 0m).Abs().Min(currentPos.Abs());
				break;
			}
			case PositionConditions.InvertPosition:
			{
				if (posDir is null)
				{
					raiseInvalid();
					return;
				}

				operationDir = posDir.Value.Invert();
				operationVol = (2 * currentPos).Abs();
				break;
			}
			default:
				throw new InvalidOperationException(PosCondition.To<string>());
		}

		LogVerbose("Pos modify dir={0}, vol={1}, cond={2}", operationDir, operationVol, PosCondition);

		IPositionModifyAlgo algo = Algorithm switch
		{
			PositionModifyAlgorithms.MarketOrder => new MarketOrderAlgo(operationDir, operationVol),
			PositionModifyAlgorithms.VWAP => new QuotingBehaviorAlgo(
				new VWAPQuotingBehavior(new Unit(0)), operationDir, operationVol, VolumePart),
			PositionModifyAlgorithms.TWAP => new QuotingBehaviorAlgo(
				new TWAPQuotingBehavior(TimeInterval), operationDir, operationVol, VolumePart),
			PositionModifyAlgorithms.Iceberg => new QuotingBehaviorAlgo(
				new LastTradeQuotingBehavior(new Unit(0)), operationDir, operationVol, VolumePart),
			PositionModifyAlgorithms.Quoting => new QuotingBehaviorAlgo(
				new BestByPriceQuotingBehavior(new Unit(0)), operationDir, operationVol, VolumePart),
			_ => throw new InvalidOperationException(Algorithm.ToString())
		};

		_currAlgo = new AlgoWrapper(this, security, portfolio, algo);
		_currAlgo.UpdateMarketData(time, _lastPrice, _lastVolume);
	}

	private void OnProcessLastPrice(DiagramSocketValue value)
	{
		var lastPrice = value.GetValue<decimal>();

		_currAlgo?.UpdateMarketData(value.Time, lastPrice, _lastVolume);
		_lastPrice = lastPrice;
	}

	private void OnProcessLastVolume(DiagramSocketValue value)
	{
		var lastVol = value.GetValue<decimal>();

		_currAlgo?.UpdateMarketData(value.Time, _lastPrice, lastVol);
		_lastVolume = lastVol;
	}

	private void OnProcessOrderBook(DiagramSocketValue value)
	{
		var depth = value.GetValue<IOrderBookMessage>();
		_currAlgo?.UpdateOrderBook(value.Time, depth);
	}

	private void OnProcessCancel(DiagramSocketValue value)
	{
		if (!value.GetValue<bool>())
			return;

		_currAlgo?.Cancel();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		UpdateLastSockets();
		UpdateVolumeSocket();
	}

	private void UpdateElementName()
	{
		var name = PosCondition != PositionConditions.NoCondition ? PosCondition.GetFieldDisplayName() : string.Empty;

		if (Direction is Sides side)
		{
			if (name.IsEmpty())
				name = side.GetFieldDisplayName();
			else
				name += $" ({side.GetFieldDisplayName()})";
		}

		SetElementName(name);
	}
}