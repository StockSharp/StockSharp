namespace SpeedTest
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	public class SpeedTestStrategy : Strategy
	{
		private readonly int _nummberofTests;
		public string TraderName { get; set; }

		public TimeSpan? MaxLatency
		{
			get { return Orders.Count() != 0 ? Orders.Max(o => o.LatencyRegistration) : null; }
		}

		public TimeSpan? MinLatency
		{
			get { return Orders.Count() != 0 ? Orders.Min(o => o.LatencyRegistration) : null; }
		}

		public SpeedTestStrategy(int nummberofTests)
		{
			_nummberofTests = nummberofTests;
		}

		private readonly object _locker = new object();
		private TimeSpan _holeTime;

		public TimeSpan AverageTime
		{
			get
			{
				return NumberOfOrders != 0
				       	? TimeSpan.FromMilliseconds((int) Math.Round(_holeTime.TotalMilliseconds/NumberOfOrders))
				       	: TimeSpan.FromMilliseconds(0);
			}
		}

		public int NumberOfOrders { get; set; }

		private void DeleteOrder(Order order)
		{
			if (order != null)
			{
				try
				{
					Connector.CancelOrder(order);
				}
				catch (Exception g)
				{
					Debug.Print("{0}", g);
				}
			}
		}

		public event Action OrderTimeChanged;

		protected override void OnStarted()
		{
			base.OnStarted();
			
			_holeTime = TimeSpan.FromMilliseconds(0);
			NumberOfOrders = 0;
			OrderRegistered += SpeedTestStrategyOrderRegistered;

			RegisterOrder(SendNewOrder());
		}

		private void SpeedTestStrategyOrderRegistered(Order order)
		{
			lock (_locker)
			{
				if (order.LatencyRegistration != null)
					_holeTime += order.LatencyRegistration.Value;

				NumberOfOrders++;

				if (OrderTimeChanged != null)
					OrderTimeChanged();

				DeleteOrder(order);

				if (NumberOfOrders < _nummberofTests)
				{
					RegisterOrder(SendNewOrder());
				}
				else
				{
					Stop();
				}
			}
		}

		private Order SendNewOrder()
		{
			var price = this.GetSecurityValue<decimal>(Level1Fields.BestBidPrice) * (1 - 0.01M);
			var testorder = this.CreateOrder(Sides.Buy, Security.ShrinkPrice(price));
			return testorder;
		}

		protected override void OnStopping()
		{
			OrderRegistered -= SpeedTestStrategyOrderRegistered;
			CancelActiveOrders();
			base.OnStopping();
		}
	}
}