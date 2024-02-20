namespace StockSharp.Algo.Strategies.Protective
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Positions;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The strategy of the automatic position protection.
	/// </summary>
	/// <remarks>
	/// New trades come in strategy via <see cref="ProcessNewMyTrade"/>. They are automatically protected by <see cref="TakeProfitStopLossStrategy"/>. Also, <see cref="AutoProtectiveStrategy"/> turns over stops in case of position flipping.
	/// </remarks>
	[Obsolete("Use ProtectiveController class.")]
	public class AutoProtectiveStrategy : Strategy
	{
		private class AutoProtectivePositionManager : PositionManager
		{
			public AutoProtectivePositionManager()
				: base(false)
			{
			}

			public decimal Position { get; set; }

			public override PositionChangeMessage ProcessMessage(Message message)
			{
				var change = base.ProcessMessage(message);

				if (change != null)
				{
					var currValue = change.TryGetDecimal(PositionChangeTypes.CurrentValue);

					if (currValue != null)
						Position = currValue.Value;
				}

				return change;
			}
		}

		private readonly SynchronizedDictionary<Security, AutoProtectivePositionManager> _positionManagers = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="AutoProtectiveStrategy"/>.
		/// </summary>
		public AutoProtectiveStrategy()
		{
			_takeProfitLevel = this.Param(nameof(TakeProfitLevel), new Unit());
			_stopLossLevel = this.Param(nameof(StopLossLevel), new Unit());
			_isTrailingStopLoss = this.Param<bool>(nameof(IsTrailingStopLoss));
			_isTrailingTakeProfit = this.Param<bool>(nameof(IsTrailingTakeProfit));
			_takeProfitTimeOut = this.Param<TimeSpan>(nameof(TakeProfitTimeOut));
			_stopLossTimeOut = this.Param<TimeSpan>(nameof(StopLossTimeOut));
			_useMarketOrders = this.Param<bool>(nameof(UseMarketOrders));
		}

		private readonly StrategyParam<Unit> _takeProfitLevel;

		/// <summary>
		/// The protective level for the take profit. The default level is 0, which means the disabled.
		/// </summary>
		public Unit TakeProfitLevel
		{
			get => _takeProfitLevel.Value;
			set => _takeProfitLevel.Value = value;
		}

		private readonly StrategyParam<Unit> _stopLossLevel;

		/// <summary>
		/// The protective level for the stop loss. The default level is 0, which means the disabled.
		/// </summary>
		public Unit StopLossLevel
		{
			get => _stopLossLevel.Value;
			set => _stopLossLevel.Value = value;
		}

		private readonly StrategyParam<bool> _isTrailingStopLoss;

		/// <summary>
		/// Whether to use a trailing technique for <see cref="StopLossStrategy"/>. The default is off.
		/// </summary>
		public bool IsTrailingStopLoss
		{
			get => _isTrailingStopLoss.Value;
			set
			{
				if (value && StopLossLevel.Type == UnitTypes.Limit)
					throw new InvalidOperationException(LocalizedStrings.UnsupportedType.Put(UnitTypes.Limit));

				_isTrailingStopLoss.Value = value;
			}
		}

		private readonly StrategyParam<bool> _isTrailingTakeProfit;

		/// <summary>
		/// Whether to use a trailing technique for <see cref="TakeProfitStrategy"/>. The default is off.
		/// </summary>
		public bool IsTrailingTakeProfit
		{
			get => _isTrailingTakeProfit.Value;
			set
			{
				if (value && TakeProfitLevel.Type == UnitTypes.Limit)
					throw new InvalidOperationException(LocalizedStrings.UnsupportedType.Put(UnitTypes.Limit));

				_isTrailingTakeProfit.Value = value;
			}
		}

		private readonly StrategyParam<TimeSpan> _stopLossTimeOut;

		/// <summary>
		/// Time limit for <see cref="StopLossStrategy"/>. If protection has not worked by this time, the position will be closed on the market. The default is off.
		/// </summary>
		public TimeSpan StopLossTimeOut
		{
			get => _stopLossTimeOut.Value;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_stopLossTimeOut.Value = value;
			}
		}

		private readonly StrategyParam<TimeSpan> _takeProfitTimeOut;

		/// <summary>
		/// Time limit for <see cref="TakeProfitStrategy"/>. If protection has not worked by this time, the position will be closed on the market. The default is off.
		/// </summary>
		public TimeSpan TakeProfitTimeOut
		{
			get => _takeProfitTimeOut.Value;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_takeProfitTimeOut.Value = value;
			}
		}

		private readonly StrategyParam<bool> _useMarketOrders;

		/// <summary>
		/// Whether to use market orders.
		/// </summary>
		public bool UseMarketOrders
		{
			get => _useMarketOrders.Value;
			set => _useMarketOrders.Value = value;
		}

		/// <summary>
		/// To get or set the initial position for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Position.</returns>
		public decimal this[Security security]
		{
			get => GetPositionManager(security).Position;
			set
			{
				var prevPos = this[security];

				if (prevPos != value)
				{
					// закрытие или переворот позы
					if (prevPos.Sign() != value.Sign())
					{
						CloseProtective(security);

						// переворот
						if (value != 0)
						{
							IncreasePosition(value);
						}
					}
					else
					{
						var diffVolume = value.Abs() - prevPos.Abs();

						if (diffVolume == 0)
							return;

						// увеличение позы
						if (diffVolume > 0)
						{
							IncreasePosition(diffVolume * value.Sign());
						}
						// уменьшение позы
						else
						{
							DecreasePosition(security, diffVolume.Abs());
						}
					}

					GetPositionManager(security).Position = value;
				}
			}
		}

		private IMarketRule _myTradesRule;
		private Strategy _myTradesStrategy;

		/// <summary>
		/// The strategy which new trades are automatically passed to <see cref="ProcessNewMyTrade"/>.
		/// </summary>
		public Strategy MyTradesStrategy
		{
			get => _myTradesStrategy;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (_myTradesStrategy == value)
					return;

				if (_myTradesRule != null)
					Rules.Remove(_myTradesRule);

				_myTradesStrategy = value;

				if (ProcessState == ProcessStates.Started)
					ApplyRule();
			}
		}

		private void ApplyRule()
		{
			_myTradesRule = MyTradesStrategy
					.WhenNewMyTrade()
					.Do(ProcessNewMyTrade)
					.Apply(this);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (MyTradesStrategy != null)
				ApplyRule();
		}

		/// <summary>
		/// To protect the position that has been updated via <see cref="this[Security]"/>.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <returns>The protective strategy. If <see langword="null" /> will be returned then the position protection is ignored.</returns>
		protected virtual IProtectiveStrategy Protect(decimal position)
		{
			return null;
		}

		/// <summary>
		/// Protect position.
		/// </summary>
		/// <param name="trade">The protected trade.</param>
		/// <param name="volume">Volume to be protected is specified by the value.</param>
		/// <returns>Protective strategy.</returns>
		protected virtual Strategy Protect(MyTrade trade, decimal volume)
		{
			Strategy protectiveStrategy;

			var takeProfit = TakeProfitLevel == 0 ? null : new TakeProfitStrategy(trade, TakeProfitLevel)
			{
				UseMarketOrders = UseMarketOrders,
				IsTrailing = IsTrailingTakeProfit,
				TimeOut = TakeProfitTimeOut,
				WaitAllTrades = WaitAllTrades
			};

			var stopLoss = StopLossLevel == 0 ? null : new StopLossStrategy(trade, StopLossLevel)
			{
				UseMarketOrders = UseMarketOrders,
				IsTrailing = IsTrailingStopLoss,
				TimeOut = StopLossTimeOut,
				WaitAllTrades = WaitAllTrades
			};

			if (takeProfit != null && stopLoss != null)
			{
				protectiveStrategy = new TakeProfitStopLossStrategy(takeProfit, stopLoss)
				{
					WaitAllTrades = WaitAllTrades
				};
			}
			else if (takeProfit != null)
				protectiveStrategy = takeProfit;
			else
				protectiveStrategy = stopLoss;

			if (protectiveStrategy is IProtectiveStrategy p)
			{
				//protectiveStrategy.DisposeOnStop = true;
				p.ProtectiveVolume = volume;
			}

			return protectiveStrategy;
		}

		/// <summary>
		/// To sort protective strategies to define the worst and the best ones by market prices (when position is partially closed the worst ones are cancelled firstly).
		/// </summary>
		/// <param name="strategies">Protective strategies in unsorted order.</param>
		/// <returns>Protective strategies in sorted order.</returns>
		protected virtual IEnumerable<IGrouping<Tuple<Sides, decimal>, IProtectiveStrategy>> Sort(IEnumerable<IGrouping<Tuple<Sides, decimal>, IProtectiveStrategy>> strategies)
		{
			var side = strategies.First().Key.Item1;
			return strategies.OrderBy(s => s.Key.Item2 * (side == Sides.Buy ? 1 : -1));
		}

		/// <summary>
		/// To process trade to correct the protective strategies volume.
		/// </summary>
		/// <param name="trade">Trade.</param>
		public void ProcessNewMyTrade(MyTrade trade)
		{
			var security = trade.Trade.Security;

			var manager = GetPositionManager(security);

			var prevPos = manager.Position;

			manager.ProcessMessage(trade.ToMessage());

			this.AddInfoLog(LocalizedStrings.PrevPosNewPos, security.Id, prevPos, manager.Position);

			if (prevPos == 0)
			{
				Protect(manager, trade, trade.Trade.Volume);
				return;
			}

			// закрытие или переворот позы
			if (prevPos.Sign() != manager.Position.Sign())
			{
				// закрываем старые стопы
				CloseProtective(security);

				// переворот позы
				if (manager.Position != 0)
				{
					this.AddInfoLog(LocalizedStrings.ReversePos);
					Protect(manager, trade, manager.Position.Abs());
				}
			}
			else
			{
				var diffVolume = manager.Position.Abs() - prevPos.Abs();

				if (diffVolume == 0)
					return;

				// увеличение позы
				if (diffVolume > 0)
				{
					this.AddInfoLog(LocalizedStrings.PosIncreased, diffVolume);
					Protect(manager, trade, diffVolume);
				}
				// уменьшение позы
				else
				{
					DecreasePosition(security, diffVolume.Abs());
				}
			}
		}

		private void CloseProtective(Security security)
		{
			foreach (var strategies in ChildStrategies
						.Where(s => s.Security == security)
						.OfType<IProtectiveStrategy>()
						.GroupBy(s => Tuple.Create(s.ProtectiveSide, s.ProtectivePrice))
						.ToArray())
			{
				strategies.OfType<Strategy>().ForEach(s => s.Stop());
			}
		}

		private void IncreasePosition(decimal position)
		{
			var strategy = Protect(position);

			if (strategy != null)
			{
				this.AddInfoLog(LocalizedStrings.PosChanged, position);
				ChildStrategies.Add((Strategy)strategy);
			}
		}

		private void DecreasePosition(Security security, decimal diffVolume)
		{
			this.AddInfoLog(LocalizedStrings.PosDecreased, diffVolume);

			var groups = ChildStrategies
				.Where(s => s.Security == security)
				.OfType<IProtectiveStrategy>()
				.GroupBy(s => Tuple.Create(s.ProtectiveSide, s.ProtectivePrice))
				.ToArray();

			if (groups.Length == 0)
			{
				this.AddWarningLog(LocalizedStrings.StopsNotFound);
				return;
			}

			foreach (var strategies in Sort(groups))
			{
				this.AddInfoLog(LocalizedStrings.StopsDecreased, diffVolume);

				diffVolume = ChangeVolume(strategies.ToArray(), diffVolume);

				if (diffVolume <= 0)
					break;
			}
		}

		private AutoProtectivePositionManager GetPositionManager(Security security)
		{
			return _positionManagers.SafeAdd(security, key => new AutoProtectivePositionManager { Parent = this });
		}

		private void Protect(AutoProtectivePositionManager positionManager, MyTrade trade, decimal volume)
		{
			var strategy = Protect(trade, volume);

			if (strategy == null)
			{
				this.AddWarningLog(LocalizedStrings.NoProtectiveStrategies);
				return;
			}

			strategy.NewMyTrade += protectiveTrade =>
			{
				var prevPos = positionManager.Position;

				positionManager.ProcessMessage(protectiveTrade.ToMessage());

				this.AddInfoLog(protectiveTrade.ToString());
				this.AddInfoLog(LocalizedStrings.PrevPosNewPos, protectiveTrade.Trade.Security.Id, prevPos, positionManager.Position);
			};

			ChildStrategies.Add(strategy);
		}

		private decimal ChangeVolume(IProtectiveStrategy[] strategies, decimal removableVolume)
		{
			if (removableVolume <= 0)
				throw new ArgumentOutOfRangeException(nameof(removableVolume), removableVolume, LocalizedStrings.InvalidValue);

			// старый котируемый объем
			var volume = strategies.First().ProtectiveVolume;

			// новый котируемый объем
			var newVolume = Math.Max(0, volume - removableVolume);

			if (newVolume == 0)
			{
				this.AddInfoLog(LocalizedStrings.Stop);

				strategies.Cast<Strategy>().ForEach(s => s.Stop());
			}
			else
			{
				foreach (var strategy in strategies)
					strategy.ProtectiveVolume = newVolume;
			}

			// оставшийся объем для обновления стратегий
			return removableVolume - volume;
		}
	}
}