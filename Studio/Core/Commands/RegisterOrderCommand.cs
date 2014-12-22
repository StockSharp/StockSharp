namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class RegisterOrderCommand : BaseStudioCommand
	{
		public RegisterOrderCommand(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			Order = order;
		}

		public Order Order { get; private set; }
	}
}