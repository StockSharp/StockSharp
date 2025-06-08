namespace StockSharp.Diagram.Elements;

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
	private interface IPositionAlgo
	{
		void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume);
		void Cancel();
	}

	private abstract class BasePositionAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume) : IPositionAlgo
	{
		protected PositionModifyElement Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));
		protected Security Security { get; } = security ?? throw new ArgumentNullException(nameof(security));
		protected Portfolio Portfolio { get; } = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
		protected Sides Side { get; } = side;
		protected decimal Volume { get; } = volume;

		protected DiagramStrategy Strategy => Parent.Strategy;
		protected DiagramSocket OrderSocket => Parent._orderSocket;
		protected DiagramSocket TradeSocket => Parent._tradeSocket;
		protected bool IsTradeSocket => Parent._isTradeSocket;

		protected void ResetAlgo(DateTimeOffset time, decimal volume)
		{
			Parent._currAlgo = null;
			RaiseProcessOutput(Parent._remainVolumeSocket, time, volume);
		}

		protected void RaiseProcessOutput(DiagramSocket socket, DateTimeOffset time, object value)
			=> Parent.RaiseProcessOutput(socket, time, value);

		public abstract void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume);
		public abstract void Cancel();
	}

	private class MarketOrderAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume)
		: BasePositionAlgo(parent, security, portfolio, side, volume)
	{
		public override void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume)
		{
			var order = new Order
			{
				Portfolio = Portfolio,
				Security = Security,
				Side = Side,
				Type = OrderTypes.Market,
				Volume = Volume,
				ClientCode = Parent.ClientCode,
				BrokerCode = Parent.BrokerCode,
				IsManual = Parent.IsManual,
				MarginMode = Parent.MarginMode,
				IsMarketMaker = Parent.IsMarketMaker,
				TimeInForce = Parent.TimeInForce,
				Comment = Parent.Comment,
			};

			var strategy = Strategy;

			order
				.WhenMatched(strategy)
				.Do(ord =>
				{
					var time = ord.MatchedTime ?? ord.ServerTime;

					if (OrderSocket is not null)
					{
						RaiseProcessOutput(OrderSocket, time, ord);
						strategy.Flush(ord);
					}

					ResetAlgo(time, 0);
					strategy.Flush(ord);
				})
				.Apply(strategy);

			order
				.WhenRegisterFailed(strategy)
				.Do(fail =>
				{
					ResetAlgo(fail.ServerTime, fail.Order.Balance);
					strategy.Flush(fail);
				})
				.Apply(strategy);

			order
				.WhenCanceled(strategy)
				.Do(ord =>
				{
					ResetAlgo(ord.CancelledTime ?? ord.ServerTime, ord.Balance);
					strategy.Flush(ord);
				})
				.Apply(strategy);

			if (IsTradeSocket)
			{
				order
					.WhenNewTrade(strategy)
					.Do(trade =>
					{
						RaiseProcessOutput(TradeSocket, trade.Trade.ServerTime, trade);
						strategy.Flush(trade.Trade);
					})
					.Apply(strategy);
			}

			strategy.RegisterOrder(order);
		}

		public override void Cancel() => throw new NotSupportedException(LocalizedStrings.MarketOrderCannotCancel);
	}

	private abstract class BaseSlicePositionAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume, Unit volumePart)
		: BasePositionAlgo(parent, security, portfolio, side, volume)
	{
		protected Unit VolumePart { get; } = volumePart ?? throw new ArgumentNullException(nameof(volumePart));
		protected Order CurrOrder { get; private set; }
		protected decimal RemainingVolume { get; private set; } = volume;

		protected abstract decimal Price { get; }

		protected void RegisterOrder()
		{
			var orderVolume = VolumePart.Type switch
			{
				UnitTypes.Absolute/* or UnitTypes.Point or UnitTypes.Step*/
					=> RemainingVolume.Min((decimal)VolumePart),

				UnitTypes.Percent => RemainingVolume.Min(RemainingVolume - (decimal)(RemainingVolume - VolumePart)),
				UnitTypes.Limit => VolumePart.Value,

				_ => throw new InvalidOperationException(VolumePart.Type.To<string>())
			};

			var order = new Order
			{
				Portfolio = Portfolio,
				Security = Security,
				Side = Side,
				Type = OrderTypes.Limit,
				Price = Price,
				Volume = orderVolume,
				ClientCode = Parent.ClientCode,
				BrokerCode = Parent.BrokerCode,
				IsManual = Parent.IsManual,
				MarginMode = Parent.MarginMode,
				IsMarketMaker = Parent.IsMarketMaker,
				TimeInForce = Parent.TimeInForce,
				Comment = Parent.Comment,
			};

			var strategy = Strategy;

			order
				.WhenMatched(strategy)
				.Do(ord =>
				{
					CurrOrder = null;

					var time = ord.MatchedTime ?? ord.ServerTime;

					if (OrderSocket is not null)
					{
						RaiseProcessOutput(OrderSocket, time, ord);
						strategy.Flush(ord);
					}

					RemainingVolume -= order.Volume;

					if (RemainingVolume > 0)
					{
						RegisterOrder();
						return;
					}

					ResetAlgo(time, 0);
					strategy.Flush(ord);
				})
				.Apply(strategy);

			order
				.WhenRegisterFailed(strategy)
				.Do(fail =>
				{
					CurrOrder = null;

					if (fail.Order.GetMatchedVolume() is decimal matchedVolume)
						ResetAlgo(fail.ServerTime, RemainingVolume - matchedVolume);

					strategy.Flush(fail);
				})
				.Apply(strategy);

			order
				.WhenCanceled(strategy)
				.Do(ord =>
				{
					CurrOrder = null;

					if (ord.GetMatchedVolume() is decimal matchedVolume)
						ResetAlgo(ord.CancelledTime ?? ord.ServerTime, RemainingVolume - matchedVolume);

					strategy.Flush(ord);
				})
				.Apply(strategy);

			if (IsTradeSocket)
			{
				order
					.WhenNewTrade(strategy)
					.Do(trade =>
					{
						RaiseProcessOutput(TradeSocket, trade.Trade.ServerTime, trade);
						strategy.Flush(trade.Trade);
					})
					.Apply(strategy);
			}

			CurrOrder = order;
			strategy.RegisterOrder(order);
		}

		public override void Cancel()
		{
			if (CurrOrder is not null)
				Strategy.CancelOrder(CurrOrder);
		}
	}

	private class VWAPOrderAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume, Unit volumePart)
		: BaseSlicePositionAlgo(parent, security, portfolio, side, volume, volumePart)
	{
		private decimal _cumulativePriceVolume;
		private decimal _cumulativeVolume;

		private decimal _currentVwap;

		public override void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume)
		{
			if (price is not decimal decPrice || volume is not decimal decVol)
				return;

			_cumulativePriceVolume += decPrice * decVol;
			_cumulativeVolume += decVol;

			_currentVwap = _cumulativePriceVolume / _cumulativeVolume;

			if (CurrOrder is null)
				RegisterOrder();
		}

		protected override decimal Price => _currentVwap;
	}

	private class TWAPOrderAlgo : BaseSlicePositionAlgo
	{
		private readonly CircularBuffer<decimal> _prices = new(10);
		private readonly TimeSpan _timeInterval;
		private DateTimeOffset? _lastOrderTime;

		public TWAPOrderAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume, Unit volumePart, TimeSpan timeInterval)
			: base(parent, security, portfolio, side, volume, volumePart)
		{
			if (timeInterval <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeInterval), timeInterval, LocalizedStrings.IntervalMustBePositive);

			_timeInterval = timeInterval;
		}

		public override void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume)
		{
			if (price is not decimal decPrice)
				return;

			_prices.PushBack(decPrice);

			if (_lastOrderTime is null || (time - _lastOrderTime) >= _timeInterval)
			{
				_lastOrderTime = time;
				RegisterOrder();
			}
		}

		protected override decimal Price
		{
			get
			{
				if (_prices.Count == 0)
					throw new InvalidOperationException("_prices.Count == 0");

				return _prices.Sum() / _prices.Count;
			}
		}
	}

	private class IcebergOrderAlgo(PositionModifyElement parent, Security security, Portfolio portfolio, Sides side, decimal volume, Unit volumePart)
		: BaseSlicePositionAlgo(parent, security, portfolio, side, volume, volumePart)
	{
		private decimal _lastPrice;

		public override void UpdateLast(DateTimeOffset time, decimal? price, decimal? volume)
		{
			if (price is not decimal decPrice)
				return;

			_lastPrice = decPrice;

			if (CurrOrder is null)
				RegisterOrder();
		}

		protected override decimal Price => _lastPrice;
	}

	private readonly DiagramSocket _securitySocket;
	private readonly DiagramSocket _portfolioSocket;
	//private readonly DiagramSocket _cancelSocket;
	private DiagramSocket _volumeSocket;

	private DiagramSocket _lastPriceSocket;
	private DiagramSocket _lastVolumeSocket;

	private readonly DiagramSocket _remainVolumeSocket;
	private readonly DiagramSocket _orderSocket;
	private readonly DiagramSocket _tradeSocket;

	private bool _isTradeSocket;

	private decimal? _lastPrice;
	private decimal? _lastVolume;

	private IPositionAlgo _currAlgo;

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
	
	private bool NeedLastPrice => Algorithm != PositionModifyAlgorithms.MarketOrder;
	private bool NeedLastVolume => Algorithm is PositionModifyAlgorithms.VWAP;

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (Direction is null && NeedDirection)
			throw new InvalidOperationException(LocalizedStrings.OrderSideNotSpecified);

		if (NeedLastPrice && _lastPriceSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

		if (NeedLastVolume && _lastVolumeSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

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

		updateLastPriceSocket();
		updateLastVolumeSocket();
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
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
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
				if (posDir is null || (direction is not null && posDir != direction))
				{
					raiseInvalid();
					return;
				}

				operationDir = posDir.Value;
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

		_currAlgo = Algorithm switch
		{
			PositionModifyAlgorithms.MarketOrder => new MarketOrderAlgo(this, security, portfolio, operationDir, operationVol),
			PositionModifyAlgorithms.VWAP => new VWAPOrderAlgo(this, security, portfolio, operationDir, operationVol, VolumePart),
			PositionModifyAlgorithms.TWAP => new TWAPOrderAlgo(this, security, portfolio, operationDir, operationVol, VolumePart, TimeInterval),
			PositionModifyAlgorithms.Iceberg => new IcebergOrderAlgo(this, security, portfolio, operationDir, operationVol, VolumePart),
			_ => throw new InvalidOperationException(Algorithm.ToString())
		};

		_currAlgo.UpdateLast(time, _lastPrice, _lastVolume);
	}

	private void OnProcessLastPrice(DiagramSocketValue value)
	{
		var lastPrice = value.GetValue<decimal>();

		_currAlgo?.UpdateLast(value.Time, lastPrice, _lastVolume);
		_lastPrice = lastPrice;
	}

	private void OnProcessLastVolume(DiagramSocketValue value)
	{
		var lastVol = value.GetValue<decimal>();

		_currAlgo?.UpdateLast(value.Time, _lastPrice, lastVol);
		_lastVolume = lastVol;
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