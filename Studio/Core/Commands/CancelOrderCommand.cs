namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	public class CancelOrderCommand : BaseStudioCommand
	{
		public IEnumerable<Order> Orders { get; private set; }

		public Order Mask { get; private set; }

		public CancelOrderCommand(Order mask)
		{
			if (mask == null)
				throw new ArgumentNullException("mask");

			Mask = mask;
		}

		public CancelOrderCommand(IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException("orders");

			Orders = orders;
		}
	}
}