namespace StockSharp.Quik.Lua
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using QuickFix.Fields;

	using StockSharp.Fix;
	using StockSharp.Messages;

	[DisplayName("LuaFixTransactionMessageAdapter")]
	class LuaFixTransactionMessageAdapter : FixMessageAdapter
	{
		/// <summary>
		/// Создать <see cref="LuaFixTransactionMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public LuaFixTransactionMessageAdapter(FixSessionHolder sessionHolder)
			: base(MessageAdapterTypes.Transaction, sessionHolder, sessionHolder.TransactionSession)
		{
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					if (regMsg.OrderType != OrderTypes.Conditional)
						break;

					ProcessOrderRegister(regMsg);
					return;
				}
			}

			base.OnSendInMessage(message);
		}

		private void ProcessOrderRegister(OrderRegisterMessage regMsg)
		{
			var fixMsg = regMsg.ToFixOrder<NewStopOrderSingle>(Session);

			var condition = (QuikOrderCondition)regMsg.Condition;

			fixMsg.Type = new NewStopOrderSingle.TypeField((int)condition.Type);
			fixMsg.StopPriceCondition = new NewStopOrderSingle.StopPriceConditionField((int)condition.StopPriceCondition);
			fixMsg.ConditionOrderSide = new NewStopOrderSingle.ConditionOrderSideField((int)condition.ConditionOrderSide);
			fixMsg.LinkedOrderCancel = new NewStopOrderSingle.LinkedOrderCancelField(condition.LinkedOrderCancel);

			if (condition.Result != null)
				fixMsg.Result = new NewStopOrderSingle.ResultField((int)condition.Result);
			if (condition.OtherSecurityId != null)
				fixMsg.OtherSecurityCode = new NewStopOrderSingle.OtherSecurityCodeField(condition.OtherSecurityId.Value.SecurityCode);
			if (condition.StopPrice != null)
				fixMsg.StopPx = new StopPx(condition.StopPrice.Value);
			if (condition.StopLimitPrice != null)
				fixMsg.StopLimitPrice = new NewStopOrderSingle.StopLimitPriceField(condition.StopLimitPrice.Value);
			if (condition.IsMarketStopLimit != null)
				fixMsg.IsMarketStopLimit = new NewStopOrderSingle.IsMarketStopLimitField(condition.IsMarketStopLimit.Value);
			if (condition.ActiveTime != null)
			{
				fixMsg.ActiveTimeFrom = new NewStopOrderSingle.ActiveTimeFromField(condition.ActiveTime.Min.UtcDateTime);
				fixMsg.ActiveTimeTo = new NewStopOrderSingle.ActiveTimeToField(condition.ActiveTime.Min.UtcDateTime);
			}
			if (condition.ConditionOrderId != null)
				fixMsg.ConditionOrderId = new NewStopOrderSingle.ConditionOrderIdField((int)condition.ConditionOrderId);
			if (condition.ConditionOrderPartiallyMatched != null)
				fixMsg.ConditionOrderPartiallyMatched = new NewStopOrderSingle.ConditionOrderPartiallyMatchedField(condition.ConditionOrderPartiallyMatched.Value);
			if (condition.ConditionOrderUseMatchedBalance != null)
				fixMsg.ConditionOrderUseMatchedBalance = new NewStopOrderSingle.ConditionOrderUseMatchedBalanceField(condition.ConditionOrderUseMatchedBalance.Value);
			if (condition.LinkedOrderPrice != null)
				fixMsg.LinkedOrderPrice = new NewStopOrderSingle.LinkedOrderPriceField(condition.LinkedOrderPrice.Value);
			if (condition.Offset != null)
				fixMsg.Offset = new NewStopOrderSingle.OffsetField(condition.Offset.ToString());
			if (condition.Spread != null)
				fixMsg.StopSpread = new NewStopOrderSingle.SpreadField(condition.Spread.ToString());
			if (condition.IsMarketTakeProfit != null)
				fixMsg.IsMarketTakeProfit = new NewStopOrderSingle.IsMarketTakeProfitField(condition.IsMarketTakeProfit.Value);

			SendMessage(fixMsg);
		}

		/// <summary>
		/// Метод вызывается при обработке полученного сообщения.
		/// </summary>
		/// <param name="fixMessage">Строка сообщения.</param>
		protected override bool ProcessTransactionMessage(string fixMessage)
		{
			var msgType = QuickFix.Message.GetMsgType(fixMessage);

			switch (msgType)
			{
				case StopOrderExecutionReport.MsgType:
				{
					var fixMsg = CreateMessage<StopOrderExecutionReport>(Session, fixMessage);
					var exec = fixMsg.ToExecutionMessage(Session, SessionHolder.UtcOffset);

					var condition = new QuikOrderCondition
					{
						Type = (QuikOrderConditionTypes)fixMsg.Type.Obj,
						Result = fixMsg.IsSetResult() ? (QuikOrderConditionResults?)fixMsg.Result.Obj : null,
						StopPriceCondition = (QuikStopPriceConditions)fixMsg.StopPriceCondition.Obj,
						StopPrice = fixMsg.IsSetStopPx() ? fixMsg.StopPx.Obj : (decimal?)null,
						StopLimitPrice = fixMsg.IsSetStopLimitPrice() ? fixMsg.StopLimitPrice.Obj : (decimal?)null,
						IsMarketStopLimit = fixMsg.IsSetIsMarketStopLimit() ? fixMsg.IsMarketStopLimit.Obj : (bool?)null,
						ConditionOrderId = fixMsg.IsSetConditionOrderId() ? fixMsg.ConditionOrderId.Obj : (long?)null,
						ConditionOrderSide = (Sides)fixMsg.ConditionOrderSide.Obj,
						ConditionOrderPartiallyMatched = fixMsg.IsSetConditionOrderPartiallyMatched() ? fixMsg.ConditionOrderPartiallyMatched.Obj : (bool?)null,
						ConditionOrderUseMatchedBalance = fixMsg.IsSetConditionOrderUseMatchedBalance() ? fixMsg.ConditionOrderUseMatchedBalance.Obj : (bool?)null,
						LinkedOrderPrice = fixMsg.IsSetLinkedOrderPrice() ? fixMsg.LinkedOrderPrice.Obj : (decimal?)null,
						LinkedOrderCancel = fixMsg.LinkedOrderCancel.Obj,
						Offset = fixMsg.IsSetOffset() ? fixMsg.Offset.Obj.ToUnit() : null,
						Spread = fixMsg.IsSetStopSpread() ? fixMsg.StopSpread.Obj.ToUnit() : null,
						IsMarketTakeProfit = fixMsg.IsSetIsMarketTakeProfit() ? fixMsg.IsMarketTakeProfit.Obj : (bool?)null,
					};

					if (fixMsg.IsSetOtherSecurityCode())
						condition.OtherSecurityId = new SecurityId { SecurityCode = fixMsg.OtherSecurityCode.Obj };
					if (fixMsg.IsSetActiveTimeFrom() && fixMsg.IsSetActiveTimeTo())
						condition.ActiveTime = new Range<DateTimeOffset>(fixMsg.ActiveTimeFrom.Obj.ToDateTimeOffset(SessionHolder.UtcOffset), fixMsg.ActiveTimeTo.Obj.ToDateTimeOffset(SessionHolder.UtcOffset));

					exec.Condition = condition;

					SendOutMessage(exec);

					return true;
				}
			}

			return base.ProcessTransactionMessage(fixMessage);
		}
	}
}
