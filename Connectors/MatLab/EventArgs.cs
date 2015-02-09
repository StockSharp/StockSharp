namespace StockSharp.MatLab
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Аргумент, передающий информацию об ошибке.
	/// </summary>
	public class ErrorEventArgs : EventArgs
	{
		internal ErrorEventArgs(Exception error)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			Error = error;
		}

		/// <summary>
		/// Информация об ошибке.
		/// </summary>
		public Exception Error { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию об инструментах.
	/// </summary>
	public class SecuritiesEventArgs : EventArgs
	{
		internal SecuritiesEventArgs(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			Securities = securities.ToArray();
		}

		/// <summary>
		/// Инструменты.
		/// </summary>
		public Security[] Securities { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о заявках.
	/// </summary>
	public class OrdersEventArgs : EventArgs
	{
		internal OrdersEventArgs(IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			Orders = orders.ToArray();
		}

		/// <summary>
		/// Заявки.
		/// </summary>
		public Order[] Orders { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию об ошибках заявок (регистрации, снятия).
	/// </summary>
	public class OrderFailsEventArgs : EventArgs
	{
		internal OrderFailsEventArgs(IEnumerable<OrderFail> orderFails)
		{
			if (orderFails == null)
				throw new ArgumentNullException("orderFails");

			OrderFails = orderFails.ToArray();
		}

		/// <summary>
		/// Ошибки.
		/// </summary>
		public OrderFail[] OrderFails { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о тиковых сделках.
	/// </summary>
	public class TradesEventArgs : EventArgs
	{
		internal TradesEventArgs(IEnumerable<Trade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			Trades = trades.ToArray();
		}

		/// <summary>
		/// Сделки.
		/// </summary>
		public Trade[] Trades { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о собственных сделках.
	/// </summary>
	public class MyTradesEventArgs : EventArgs
	{
		internal MyTradesEventArgs(IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			Trades = trades.ToArray();
		}

		/// <summary>
		/// Сделки.
		/// </summary>
		public MyTrade[] Trades { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о портфелях.
	/// </summary>
	public class PortfoliosEventArgs : EventArgs
	{
		internal PortfoliosEventArgs(IEnumerable<Portfolio> portfolios)
		{
			if (portfolios == null)
				throw new ArgumentNullException("portfolios");

			Portfolios = portfolios.ToArray();
		}

		/// <summary>
		/// Портфели.
		/// </summary>
		public Portfolio[] Portfolios { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о позициях.
	/// </summary>
	public class PositionsEventArgs : EventArgs
	{
		internal PositionsEventArgs(IEnumerable<Position> positions)
		{
			if (positions == null)
				throw new ArgumentNullException("positions");

			Positions = positions.ToArray();
		}

		/// <summary>
		/// Позиции.
		/// </summary>
		public Position[] Positions { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о стаканах.
	/// </summary>
	public class MarketDepthsEventArgs : EventArgs
	{
		internal MarketDepthsEventArgs(IEnumerable<MarketDepth> depths)
		{
			if (depths == null)
				throw new ArgumentNullException("depths");

			Depths = depths.ToArray();
		}

		/// <summary>
		/// Стаканы.
		/// </summary>
		public MarketDepth[] Depths { get; private set; }
	}

	/// <summary>
	/// Аргумент, передающий информацию о строчках лога заявок.
	/// </summary>
	public class OrderLogItemsEventArg : EventArgs
	{
		internal OrderLogItemsEventArg(IEnumerable<OrderLogItem> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			Items = items.ToArray();
		}

		/// <summary>
		/// Строчки.
		/// </summary>
		public OrderLogItem[] Items { get; private set; }
	}
}