namespace StockSharp.Sterling
{
	using System;

	using Ecng.Common;

	using SterlingLib;

	using StockSharp.Messages;

	partial class SterlingMessageAdapter
	{
		private void SessionOnStiOrderConfirmMsg(STIOrderConfirmMsg msg)
		{
			
		}

		private void SessionOnStiTradeUpdateMsg(STITradeUpdateMsg msg)
		{

		}

		private void SessionOnStiOrderUpdateMsg(STIOrderUpdateMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				Balance = msg.LvsQuantity,
				OrderState = msg.OrderStatus.ToStockSharp(),
				Side = Extensions.ToSide(msg.Side),
				Volume = msg.Quantity,
				VisibleVolume = msg.Display,
				ExecutionType = ExecutionTypes.Order,
			});
		}

		private void SessionOnStiOrderRejectMsg(STIOrderRejectMsg msg)
		{
			SendOutMessage(new ExecutionMessage
			{
				OriginalTransactionId = msg.ClOrderID.To<long>(),
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(),
				ExecutionType = ExecutionTypes.Order,
			});
		}
	}
}