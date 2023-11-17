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

		event Action<long> ITransactionProvider.MassOrderCanceled
		{
			add { }
			remove { }
		}

		event Action<long, DateTimeOffset> ITransactionProvider.MassOrderCanceled2
		{
			add { }
			remove { }
		}

		event Action<long, Exception> ITransactionProvider.MassOrderCancelFailed
		{
			add { }
			remove { }
		}

		event Action<long, Exception, DateTimeOffset> ITransactionProvider.MassOrderCancelFailed2
		{
			add { }
			remove { }
		}

		event Action<long, Exception> ITransactionProvider.OrderStatusFailed
		{
			add { }
			remove { }
		}

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

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult
		{
			add { }
			remove { }
		}

		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult2
		{
			add { }
			remove { }
		}

		void ITransactionProvider.CancelOrders(bool? isStopOrder, Portfolio portfolio, Sides? direction, ExchangeBoard board, Security security, SecurityTypes? securityType, long? transactionId)
		{
			SafeGetConnector().CancelOrders(isStopOrder, portfolio, direction, board, security, securityType, transactionId);
		}

		[Obsolete("Use Subscribe method.")]
		void ITransactionProvider.RegisterPortfolio(Portfolio portfolio)
		{
			SafeGetConnector().RegisterPortfolio(portfolio);
		}

		[Obsolete("Use UnSubscribe method.")]
		void ITransactionProvider.UnRegisterPortfolio(Portfolio portfolio)
		{
			SafeGetConnector().UnRegisterPortfolio(portfolio);
		}
	}
}