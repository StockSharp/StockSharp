namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class OrderFailCommand : BaseStudioCommand
	{
		public OrderFailCommand(OrderFail fail, OrderActions action)
		{
			if (fail == null)
				throw new ArgumentNullException("fail");

			Fail = fail;
			Action = action;
		}

		public OrderFail Fail { get; private set; }
		public OrderActions Action { get; private set; }
	}
}