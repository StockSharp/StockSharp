namespace StockSharp.Algo.Strategies
{
	using System;
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
		private Position GetPosition(Security security, Portfolio portfolio, out bool isNew)
		{
			return _positions.SafeAdd(CreateKey(security, portfolio), k => new Position
			{
				Security = security,
				Portfolio = portfolio,
			}, out isNew);
		}

		private void ProcessPositionChange(PositionChangeMessage message)
		{
			if (message.StrategyId != EnsureGetId())
				return;

			var security = SafeGetConnector().LookupById(message.SecurityId);
			var pf = SafeGetConnector().LookupByPortfolioName(message.PortfolioName);

			var position = GetPosition(security, pf, out var isNew);

			position.ApplyChanges(message);

			if (isNew)
				_newPosition?.Invoke(position);
			else
				_positionChanged?.Invoke(position);
		}

		private void RaisePositionChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str1399Params, _positions.CachedPairs.Select(pos => pos.Key + "=" + pos.Value).Join(", "));

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
		protected decimal? GetPositionValue(Security security, Portfolio portfolio) => _positions.TryGetValue(CreateKey(security, portfolio))?.CurrentValue;

		/// <summary>
		/// Set position.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="position">Position.</param>
		protected void SetPositionValue(Security security, Portfolio portfolio, decimal position) => GetPosition(security, portfolio, out _).CurrentValue = position;

		/// <inheritdoc />
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

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string clientCode, string depoName, TPlusLimits? limitType)
			=> _positions.TryGetValue(Tuple.Create(security, portfolio));

		Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
			=> SafeGetConnector().LookupByPortfolioName(name);

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios
			=> Portfolio == null ? Enumerable.Empty<Portfolio>() : new[] { Portfolio };

		private Action<Portfolio> _newPortfolio;

		event Action<Portfolio> IPortfolioProvider.NewPortfolio
		{
			add => _newPortfolio += value;
			remove => _newPortfolio -= value;
		}

		private Action<Portfolio> _portfolioChanged;

		event Action<Portfolio> IPortfolioProvider.PortfolioChanged
		{
			add => _portfolioChanged += value;
			remove => _portfolioChanged -= value;
		}
	}
}