namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class MarketDepthQuote : NotifiableObject
	{
		public class OrderRegistry
		{
			/// <summary>
			/// Контейнер заявок, сгруппированных по цене <see cref="Order.Price"/>.
			/// </summary>
			public sealed class OrderContainer
			{
				private readonly CachedSynchronizedList<Order> _orders = new CachedSynchronizedList<Order>();

				/// <summary>
				/// Суммарный баланс на покупку. Рассчет идет по внутренним заявкам <see cref="Orders"/>.
				/// </summary>
				public decimal TotalBuyBalance { get; private set; }

				/// <summary>
				/// Суммарный баланс на продажу. Рассчет идет по внутренним заявкам <see cref="Orders"/>.
				/// </summary>
				public decimal TotalSellBalance { get; private set; }

				///// <summary>
				///// Активные заявки, цена которых равна <see cref="Price"/>.
				///// </summary>
				public IEnumerable<Order> Orders
				{
					get { return _orders.Cache; }
				}

				public void RefreshTotals()
				{
					_orders.SyncDo(c =>
					{
						TotalBuyBalance = TotalSellBalance = 0;

						foreach (var order in c)
						{
							if (order.Direction == Sides.Buy)
								TotalBuyBalance += order.Balance;
							else
								TotalSellBalance += order.Balance;
						}
					});
				}

				public void Add(Order order)
				{
					if (order == null)
						throw new ArgumentNullException("order");

					_orders.Add(order);
				}

				public void Remove(Order order)
				{
					if (order == null)
						throw new ArgumentNullException("order");

					_orders.Remove(order);
				}
			}

			private readonly SynchronizedDictionary<decimal, OrderContainer> _orders = new SynchronizedDictionary<decimal, OrderContainer>();

			public void ProcessNewOrder(Order order)
			{
				var container = GetContainer(order);

				if (order.State == OrderStates.Active)
				{
					container.Add(order);
					container.RefreshTotals();
				}
			}

			public void ProcessChangedOrder(Order order)
			{
				var container = GetContainer(order);

				if (order.State == OrderStates.Done)
					container.Remove(order);

				container.RefreshTotals();
			}

			private OrderContainer GetContainer(Order order)
			{
				if (order == null)
					throw new ArgumentNullException("order");

				return _orders.SafeAdd(order.Price);
			}

			public OrderContainer GetContainer(decimal price)
			{
				return _orders.TryGetValue(price);
			}
		}

		private readonly MarketDepthControl _depthControl;

		public MarketDepthQuote(MarketDepthControl depthControl)
		{
			if (depthControl == null)
				throw new ArgumentNullException("depthControl");

			_depthControl = depthControl;
		}

		public Quote Quote { get; set; }

		private bool _isBest;

		public bool IsBest
		{
			get { return _isBest; }
			set
			{
				if (value == IsBest)
					return;

				_isBest = value;
				NotifyChanged("IsBest");
			}
		}

		private string _buy;

		public string Buy
		{
			get { return _buy; }
			private set
			{
				if (value == Buy)
					return;

				_buy = value;
				NotifyChanged("Buy");
			}
		}

		private string _sell;

		public string Sell
		{
			get { return _sell; }
			private set
			{
				if (value == Sell)
					return;

				_sell = value;
				NotifyChanged("Sell");
			}
		}

		private string _price;

		public string Price
		{
			get { return _price; }
			private set
			{
				if (value == Price)
					return;

				_price = value;
				NotifyChanged("Price");
			}
		}

		private string _ownBuy;

		public string OwnBuy
		{
			get { return _ownBuy; }
			private set
			{
				if (value == OwnBuy)
					return;

				_ownBuy = value;
				NotifyChanged("OwnBuy");
			}
		}

		private string _ownSell;

		public string OwnSell
		{
			get { return _ownSell; }
			private set
			{
				if (value == OwnSell)
					return;

				_ownSell = value;
				NotifyChanged("OwnSell");
			}
		}

		public void Init(Quote quote = null, OrderRegistry.OrderContainer orders = null, OrderRegistry.OrderContainer stopOrders = null, Quote trades = null, Quote myTrade = null)
		{
			Quote = quote;

			if (quote == null && trades == null)
			{
				Buy = Sell = OwnBuy = OwnSell = string.Empty;
				Price = string.Empty;
				IsBest = false;
				return;
			}

			if (quote != null)
			{
				Buy = (Quote.OrderDirection == Sides.Buy) ? quote.Volume.ToString(_depthControl.VolumeTextFormat) : string.Empty;
				Sell = (Quote.OrderDirection == Sides.Sell) ? quote.Volume.ToString(_depthControl.VolumeTextFormat) : string.Empty;

				Price = quote.Price.ToString(_depthControl.PriceTextFormat);

				var buyPosition = orders != null ? orders.TotalBuyBalance : 0;
				var stopBuyPosition = stopOrders != null ? stopOrders.TotalBuyBalance : 0;

				var sellPosition = orders != null ? orders.TotalSellBalance : 0;
				var stopSellPosition = stopOrders != null ? stopOrders.TotalSellBalance : 0;

				var ownBuy = buyPosition + stopBuyPosition;
				var ownSell = sellPosition + stopSellPosition;
				OwnBuy = ownBuy == 0 ? string.Empty : ownBuy.To<string>();
				OwnSell = ownSell == 0 ? string.Empty : ownSell.To<string>();
			}

			if (trades != null)
			{
				if (trades.OrderDirection == Sides.Buy)
					Buy = "[" + trades.Volume + "]" + Buy;
				else
					Sell = "[" + trades.Volume + "]" + Sell;
			}

			if (myTrade != null)
			{
				if (myTrade.OrderDirection == Sides.Buy)
					OwnBuy = "[" + myTrade.Volume + "]" + OwnBuy;
				else
					OwnSell = "[" + myTrade.Volume + "]" + OwnSell;
			}
		}
	}
}