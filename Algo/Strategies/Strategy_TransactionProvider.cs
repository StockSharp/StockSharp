namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		IdGenerator ITransactionProvider.TransactionIdGenerator => SafeGetConnector().TransactionIdGenerator;

		private Action<Order> _newOrder;

		event Action<Order> ITransactionProvider.NewOrder
		{
			add => _newOrder += value;
			remove => _newOrder -= value;
		}

		//private Action<long> _massOrderCanceled;

		event Action<long> ITransactionProvider.MassOrderCanceled
		{
			add { }
			remove { }
		}

		//private Action<long, DateTimeOffset> _massOrderCanceled2;

		event Action<long, DateTimeOffset> ITransactionProvider.MassOrderCanceled2
		{
			add { }
			remove { }
		}

		//private Action<long, Exception> _massOrderCancelFailed;

		event Action<long, Exception> ITransactionProvider.MassOrderCancelFailed
		{
			add { }
			remove { }
		}

		//private Action<long, Exception, DateTimeOffset> _massOrderCancelFailed2;

		event Action<long, Exception, DateTimeOffset> ITransactionProvider.MassOrderCancelFailed2
		{
			add { }
			remove { }
		}

		//private Action<long, Exception> _orderStatusFailed;

		event Action<long, Exception> ITransactionProvider.OrderStatusFailed
		{
			add { }
			remove { }
		}

		//private Action<long, Exception, DateTimeOffset> _orderStatusFailed2;

		event Action<long, Exception, DateTimeOffset> ITransactionProvider.OrderStatusFailed2
		{
			add { }
			remove { }
		}

		event Action<Order> ITransactionProvider.NewStopOrder
		{
			add { }
			remove { }
		}

		//private Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> _lookupPortfoliosResult;

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult
		{
			add { }
			remove { }
		}

		//private Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> _lookupPortfoliosResult2;

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult2
		{
			add { }
			remove { }
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

		//private void OnConnectorLookupPortfoliosResult2(PortfolioLookupMessage criteria, IEnumerable<Portfolio> portfolios, IEnumerable<Portfolio> newPortfolios, Exception error)
		//{
		//	_lookupPortfoliosResult2?.Invoke(criteria, portfolios, newPortfolios, error);
		//}

		//private void OnConnectorLookupPortfoliosResult(PortfolioLookupMessage criteria, IEnumerable<Portfolio> portfolios, Exception error)
		//{
		//	_lookupPortfoliosResult?.Invoke(criteria, portfolios, error);
		//}

		//private void OnConnectorMassOrderCancelFailed(long transactionId, Exception error)
		//{
		//	_massOrderCancelFailed?.Invoke(transactionId, error);
		//}

		//private void OnConnectorMassOrderCancelFailed2(long transactionId, Exception error, DateTimeOffset time)
		//{
		//	_massOrderCancelFailed2?.Invoke(transactionId, error, time);
		//}

		//private void OnConnectorMassOrderCanceled(long transactionId)
		//{
		//	_massOrderCanceled?.Invoke(transactionId);
		//}

		//private void OnConnectorMassOrderCanceled2(long transactionId, DateTimeOffset time)
		//{
		//	_massOrderCanceled2?.Invoke(transactionId, time);
		//}

		//private void OnConnectorOrderStatusFailed(long transactionId, Exception error)
		//{
		//	_orderStatusFailed?.Invoke(transactionId, error);
		//}

		//private void OnConnectorOrderStatusFailed2(long transactionId, Exception error, DateTimeOffset time)
		//{
		//	_orderStatusFailed2?.Invoke(transactionId, error, time);
		//}
	}
}