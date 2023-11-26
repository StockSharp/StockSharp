namespace StockSharp.Algo.Strategies
{
	using System;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Positions;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	partial class Strategy
	{
		private void ProcessPositionChangeMessage(PositionChangeMessage message)
		{
			if (Connector.KeepStrategiesPositions)
				return;

			if (message.StrategyId == EnsureGetId())
				ProcessPositionChangeMessageImpl(message);
		}

		private void ProcessPositionChangeMessageImpl(PositionChangeMessage message)
		{
			if(message == null)
				return;

			ProcessRisk(() => message);

			var connector = SafeGetConnector();

			var security = connector.LookupById(message.SecurityId);
			var portfolio = connector.LookupByPortfolioName(message.PortfolioName);

			var position = _positions.SafeAdd(CreatePositionKey(security, portfolio), _ => new Position
			{
				Security = security,
				Portfolio = portfolio,
				StrategyId = message.StrategyId,
			}, out var isNew);

			position.ApplyChanges(message);

			if (isNew)
				_newPosition?.Invoke(position);
			else
				_positionChanged?.Invoke(position);

			RaisePositionChanged(position.LocalTime);

			foreach (var id in message.GetSubscriptionIds())
			{
				if (_subscriptionsById.TryGetValue(id, out var subscription))
					PositionReceived?.Invoke(subscription, position);
			}
		}

		private void OnConnectorPositionReceived(Subscription subscription, Position position)
		{
			if (_pfSubscription != subscription || IsDisposeStarted)
				return;

			if (position.StrategyId != EnsureGetId())
			{
				this.AddWarningLog("Position {0} has StrategyId '{1}' instead of '{2}'.", position, position.StrategyId, EnsureGetId());
				return;
			}

			ProcessRisk(() => position.ToChangeMessage());

			_positions.SafeAdd(CreatePositionKey(position.Security, position.Portfolio), k => position, out var isNew);

			if (isNew)
				_newPosition?.Invoke(position);
			else
				_positionChanged?.Invoke(position);

			RaisePositionChanged(position.LocalTime);

			PositionReceived?.Invoke(subscription, position);
		}

		private void RaisePositionChanged(DateTimeOffset time)
		{
			this.AddInfoLog(LocalizedStrings.NewPosition, _positions.CachedPairs.Select(pos => pos.Key + "=" + pos.Value.CurrentValue).JoinCommaSpace());

			this.Notify(nameof(Position));
			PositionChanged?.Invoke();

			StatisticManager.AddPosition(time, Position);
			StatisticManager.AddPnL(time, PnL, Commission);
		}

		private readonly CachedSynchronizedDictionary<(Security, Portfolio), Position> _positions = new();

		private static (Security, Portfolio) CreatePositionKey(Security security, Portfolio portfolio)
			=> (security ?? throw new ArgumentNullException(nameof(security)), portfolio ?? throw new ArgumentNullException(nameof(portfolio)));

		/// <summary>
		/// Get position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Position.</returns>
		protected decimal? GetPositionValue(Security security, Portfolio portfolio)
			=> _positions.TryGetValue(CreatePositionKey(security, portfolio))?.CurrentValue;

		// All strategy orders are sent with StrategyId=rootStrategy.StrategyId
		// Also, child strategies do not subscribe for positions
		// So, child strategies do not recieve and position updates.
		// This manager allows child strategies to track their own position based on order state/balance.
		private class ChildStrategyPositionManager : BaseLogReceiver, IPositionManager
		{
			readonly StrategyPositionManager _inner;
			readonly CachedSynchronizedDictionary<(SecurityId secId, string portName), decimal> _positions = new();

			public ChildStrategyPositionManager() => _inner = new StrategyPositionManager(true) { Parent = this };

			public decimal? GetPositionValue(SecurityId securityId, string portfolioName) => _positions.TryGetValue((securityId, portfolioName));

			public PositionChangeMessage ProcessMessage(Message message)
			{
				PositionChangeMessage result = null;

				switch (message.Type)
				{
					case MessageTypes.Reset:
						_positions.Clear();
						break;

					case MessageTypes.PositionChange:
						this.AddWarningLog("ignored: {0}", message);
						break;

					default:
						result = _inner.ProcessMessage(message);
						break;
				}


				if (result == null)
					return null;

				if (!result.Changes.TryGetValue(PositionChangeTypes.CurrentValue, out var curValue))
				{
					this.AddWarningLog("no changes for {0}/{1}", result.SecurityId, result.PortfolioName);
					return result;
				}

				var key = (result.SecurityId, result.PortfolioName);

				lock(_positions.SyncRoot)
					_positions[key] = (decimal)curValue;

				return result;
			}

			public void Reset() => ProcessMessage(new ResetMessage());
		}

		private readonly ChildStrategyPositionManager _positionManager;

		/// <summary>
		/// The position aggregate value.
		/// </summary>
		[Browsable(false)]
		public decimal Position
		{
			get => Security == null || Portfolio == null ? 0m : GetPositionValue(Security, Portfolio) ?? 0;
			[Obsolete]
			set	{ }
		}

		/// <summary>
		/// <see cref="Position"/> change event.
		/// </summary>
		public event Action PositionChanged;

		/// <inheritdoc />
		[Browsable(false)]
		public IEnumerable<Position> Positions => _positions.CachedValues;

		private Action<Position> _newPosition;

		event Action<Position> IPositionProvider.NewPosition
		{
			add => _newPosition += value;
			remove => _newPosition -= value;
		}

		private Action<Position> _positionChanged;

		event Action<Position> IPositionProvider.PositionChanged
		{
			add => _positionChanged += value;
			remove => _positionChanged -= value;
		}

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType)
			=> _positions.TryGetValue((security, portfolio));

		Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
			=> SafeGetConnector().LookupByPortfolioName(name);

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios
			=> Portfolio == null ? Enumerable.Empty<Portfolio>() : new[] { Portfolio };

		event Action<Portfolio> IPortfolioProvider.NewPortfolio
		{
			add { }
			remove { }
		}

		event Action<Portfolio> IPortfolioProvider.PortfolioChanged
		{
			add { }
			remove { }
		}
	}
}