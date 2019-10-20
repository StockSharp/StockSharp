namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		private Position ProcessPositionInfo(Tuple<SecurityId, string> key, decimal value)
		{
			var security = SafeGetConnector().GetSecurity(key.Item1);
			var pf = SafeGetConnector().GetPortfolio(key.Item2);
			var position = _positions.SafeAdd(Tuple.Create(security, pf), k => new Position
			{
				Security = security,
				Portfolio = pf,
			});
			position.LocalTime = position.LastChangeTime = CurrentTime;
			position.CurrentValue = value;
			return position;
		}

		private readonly Dictionary<Tuple<Security, Portfolio>, Position> _positions = new Dictionary<Tuple<Security, Portfolio>, Position>();

		IEnumerable<Position> IPositionProvider.Positions => _positions.Values.ToArray();

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

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string clientCode, string depoName)
		{
			return _positions.TryGetValue(Tuple.Create(security, portfolio));
		}

		void IPositionProvider.SubscribePositions(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
			SafeGetConnector().SubscribePositions(security, from, to, count, adapter);
		}

		void IPositionProvider.UnSubscribePositions()
		{
			SafeGetConnector().UnSubscribePositions();
		}

		Portfolio IPortfolioProvider.GetPortfolio(string name)
		{
			return SafeGetConnector().GetPortfolio(name);
		}

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios => Portfolio == null ? Enumerable.Empty<Portfolio>() : new[] { Portfolio };

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

		IdGenerator ITransactionProvider.TransactionIdGenerator => SafeGetConnector().TransactionIdGenerator;

		private Action<Order> _newOrder;

		event Action<Order> ITransactionProvider.NewOrder
		{
			add => _newOrder += value;
			remove => _newOrder -= value;
		}

		private Action<long> _massOrderCanceled;

		event Action<long> ITransactionProvider.MassOrderCanceled
		{
			add => _massOrderCanceled += value;
			remove => _massOrderCanceled -= value;
		}

		private Action<long, DateTimeOffset> _massOrderCanceled2;

		event Action<long, DateTimeOffset> ITransactionProvider.MassOrderCanceled2
		{
			add => _massOrderCanceled2 += value;
			remove => _massOrderCanceled2 -= value;
		}

		private Action<long, Exception> _massOrderCancelFailed;

		event Action<long, Exception> ITransactionProvider.MassOrderCancelFailed
		{
			add => _massOrderCancelFailed += value;
			remove => _massOrderCancelFailed -= value;
		}

		private Action<long, Exception, DateTimeOffset> _massOrderCancelFailed2;

		event Action<long, Exception, DateTimeOffset> ITransactionProvider.MassOrderCancelFailed2
		{
			add => _massOrderCancelFailed2 += value;
			remove => _massOrderCancelFailed2 -= value;
		}

		private Action<long, Exception> _orderStatusFailed;

		event Action<long, Exception> ITransactionProvider.OrderStatusFailed
		{
			add => _orderStatusFailed += value;
			remove => _orderStatusFailed -= value;
		}

		private Action<long, Exception, DateTimeOffset> _orderStatusFailed2;

		event Action<long, Exception, DateTimeOffset> ITransactionProvider.OrderStatusFailed2
		{
			add => _orderStatusFailed2 += value;
			remove => _orderStatusFailed2 -= value;
		}

		private Action<Order> _newStopOrder;

		event Action<Order> ITransactionProvider.NewStopOrder
		{
			add => _newStopOrder += value;
			remove => _newStopOrder -= value;
		}

		private Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> _lookupPortfoliosResult;

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult
		{
			add => _lookupPortfoliosResult += value;
			remove => _lookupPortfoliosResult -= value;
		}

		private Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> _lookupPortfoliosResult2;

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult2
		{
			add => _lookupPortfoliosResult2 += value;
			remove => _lookupPortfoliosResult2 -= value;
		}

		void ITransactionProvider.LookupPortfolios(PortfolioLookupMessage criteria)
		{
#pragma warning disable 618
			SafeGetConnector().LookupPortfolios(criteria);
#pragma warning restore 618
		}

		void ITransactionProvider.LookupOrders(OrderStatusMessage criteria)
		{
#pragma warning disable 618
			SafeGetConnector().LookupOrders(criteria);
#pragma warning restore 618
		}

		void ITransactionProvider.SubscribeOrders(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
			SafeGetConnector().SubscribeOrders(security, from, to, count, adapter);
		}

		void ITransactionProvider.UnSubscribeOrders()
		{
			SafeGetConnector().UnSubscribeOrders();
		}

		void ITransactionProvider.CancelOrders(bool? isStopOrder, Portfolio portfolio, Sides? direction, ExchangeBoard board, Security security, SecurityTypes? securityType, long? transactionId)
		{
			SafeGetConnector().CancelOrders(isStopOrder, portfolio, direction, board, security, securityType, transactionId);
		}

		void ITransactionProvider.RegisterPortfolio(Portfolio portfolio)
		{
			SafeGetConnector().RegisterPortfolio(portfolio);
		}

		void ITransactionProvider.UnRegisterPortfolio(Portfolio portfolio)
		{
			SafeGetConnector().UnRegisterPortfolio(portfolio);
		}

		private void OnConnectorLookupPortfoliosResult2(PortfolioLookupMessage criteria, IEnumerable<Portfolio> portfolios, IEnumerable<Portfolio> newPortfolios, Exception error)
		{
			_lookupPortfoliosResult2?.Invoke(criteria, portfolios, newPortfolios, error);
		}

		private void OnConnectorLookupPortfoliosResult(PortfolioLookupMessage criteria, IEnumerable<Portfolio> portfolios, Exception error)
		{
			_lookupPortfoliosResult?.Invoke(criteria, portfolios, error);
		}

		private void OnConnectorMassOrderCancelFailed(long transactionId, Exception error)
		{
			_massOrderCancelFailed?.Invoke(transactionId, error);
		}

		private void OnConnectorMassOrderCancelFailed2(long transactionId, Exception error, DateTimeOffset time)
		{
			_massOrderCancelFailed2?.Invoke(transactionId, error, time);
		}

		private void OnConnectorMassOrderCanceled(long transactionId)
		{
			_massOrderCanceled?.Invoke(transactionId);
		}

		private void OnConnectorMassOrderCanceled2(long transactionId, DateTimeOffset time)
		{
			_massOrderCanceled2?.Invoke(transactionId, time);
		}

		private void OnConnectorPortfolioChanged(Portfolio portfolio)
		{
			_portfolioChanged?.Invoke(portfolio);
		}

		private void OnConnectorNewPortfolio(Portfolio portfolio)
		{
			_newPortfolio?.Invoke(portfolio);
		}

		private void OnConnectorOrderStatusFailed(long transactionId, Exception error)
		{
			_orderStatusFailed?.Invoke(transactionId, error);
		}

		private void OnConnectorOrderStatusFailed2(long transactionId, Exception error, DateTimeOffset time)
		{
			_orderStatusFailed2?.Invoke(transactionId, error, time);
		}
	}
}