namespace StockSharp.Algo.Strategies
{
	using System;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

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

			if (message.StrategyId != EnsureGetId())
				return;

			var connector = SafeGetConnector();

			var security = connector.LookupById(message.SecurityId);
			var portfolio = connector.LookupByPortfolioName(message.PortfolioName);

			var position = _positions.SafeAdd(CreateKey(security, portfolio), k => new Position
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

			RaisePositionChanged();

			foreach (var id in message.GetSubscriptionIds())
			{
				if (_subscriptionsById.TryGetValue(id, out var subscription))
					PositionReceived?.Invoke(subscription, position);
			}
		}

		private void OnConnectorPositionReceived(Subscription subscription, Position position)
		{
			if (_pfSubscription != subscription)
				return;

			if (position.StrategyId != EnsureGetId())
			{
				this.AddWarningLog("Position {0} has StrategyId '{1}' instead of '{2}'.", position, position.StrategyId, EnsureGetId());
				return;
			}

			_positions.SafeAdd(CreateKey(position.Security, position.Portfolio), k => position, out var isNew);

			if (isNew)
				_newPosition?.Invoke(position);
			else
				_positionChanged?.Invoke(position);

			RaisePositionChanged();

			PositionReceived?.Invoke(subscription, position);
		}

		private void RaisePositionChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str1399Params, _positions.CachedPairs.Select(pos => pos.Key + "=" + pos.Value).JoinCommaSpace());

			this.Notify(nameof(Position));
			PositionChanged?.Invoke();

			StatisticManager.AddPosition(CurrentTime, Position);
			StatisticManager.AddPnL(CurrentTime, PnL);

			RaiseNewStateMessage(nameof(Position), Position);
		}

		private readonly CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position> _positions = new CachedSynchronizedDictionary<Tuple<Security, Portfolio>, Position>();

		private Tuple<Security, Portfolio> CreateKey(Security security, Portfolio portfolio)
			=> Tuple.Create(security ?? throw new ArgumentNullException(nameof(security)), portfolio ?? throw new ArgumentNullException(nameof(portfolio)));

		/// <summary>
		/// Get position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Position.</returns>
		protected decimal? GetPositionValue(Security security, Portfolio portfolio)
			=> _positions.TryGetValue(CreateKey(security, portfolio))?.CurrentValue;

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
			=> _positions.TryGetValue(Tuple.Create(security, portfolio));

		Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
			=> SafeGetConnector().LookupByPortfolioName(name);

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios
			=> Portfolio == null ? Enumerable.Empty<Portfolio>() : new[] { Portfolio };

		//private Action<Portfolio> _newPortfolio;

		event Action<Portfolio> IPortfolioProvider.NewPortfolio
		{
			add { }
			remove { }
		}

		//private Action<Portfolio> _portfolioChanged;

		event Action<Portfolio> IPortfolioProvider.PortfolioChanged
		{
			add { }
			remove { }
		}

		//private void OnConnectorPortfolioChanged(Portfolio portfolio)
		//{
		//	_portfolioChanged?.Invoke(portfolio);
		//}

		//private void OnConnectorNewPortfolio(Portfolio portfolio)
		//{
		//	_newPortfolio?.Invoke(portfolio);
		//}
	}
}