namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public enum OrderActions
	{
		Registering,
		Registered,
		Changed,
		ReRegistering,
		Canceling,
	}

	public class OrderCommand : BaseStudioCommand
	{
		public OrderCommand(Order order, OrderActions action)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			Order = order;
			Action = action;
		}

		public Order Order { get; private set; }
		public OrderActions Action { get; private set; }
	}
}