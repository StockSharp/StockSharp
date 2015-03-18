namespace StockSharp.Quik.Lua
{
	using System;

	using QuickFix.Fields;
	using QuickFix.FIX44;

	class NewStopOrderSingle : NewOrderSingle
	{
		public NewStopOrderSingle()
		{
			Header.SetField(new MsgType(QuikFixMessages.NewStopOrderSingle));
		}

		#region Type

		public class TypeField : IntField
		{
			public TypeField()
				: base((int)QuikFixTags.Type)
			{
			}

			public TypeField(int value)
				: base((int)QuikFixTags.Type, value)
			{
			}
		}

		public TypeField Type
		{
			get
			{
				var val = new TypeField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		#endregion

		#region Result

		public class ResultField : IntField
		{
			public ResultField()
				: base((int)QuikFixTags.Result)
			{
			}

			public ResultField(int value)
				: base((int)QuikFixTags.Result, value)
			{
			}
		}

		public ResultField Result
		{
			get
			{
				var val = new ResultField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetResult()
		{
			return IsSetField((int)QuikFixTags.Result);
		}

		#endregion

		#region OtherSecurity

		public class OtherSecurityCodeField : StringField
		{
			public OtherSecurityCodeField()
				: base((int)QuikFixTags.OtherSecurityCode)
			{
			}

			public OtherSecurityCodeField(string value)
				: base((int)QuikFixTags.OtherSecurityCode, value)
			{
			}
		}

		public OtherSecurityCodeField OtherSecurityCode
		{
			get
			{
				var val = new OtherSecurityCodeField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetOtherSecurityCode()
		{
			return IsSetField((int)QuikFixTags.OtherSecurityCode);
		}

		#endregion

		#region StopPriceCondition

		public class StopPriceConditionField : IntField
		{
			public StopPriceConditionField()
				: base((int)QuikFixTags.StopPriceCondition)
			{
			}

			public StopPriceConditionField(int value)
				: base((int)QuikFixTags.StopPriceCondition, value)
			{
			}
		}

		public StopPriceConditionField StopPriceCondition
		{
			get
			{
				var val = new StopPriceConditionField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		#endregion

		#region StopLimitPrice

		public class StopLimitPriceField : DecimalField
		{
			public StopLimitPriceField()
				: base((int)QuikFixTags.StopLimitPrice)
			{
			}

			public StopLimitPriceField(decimal value)
				: base((int)QuikFixTags.StopLimitPrice, value)
			{
			}
		}

		public StopLimitPriceField StopLimitPrice
		{
			get
			{
				var val = new StopLimitPriceField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetStopLimitPrice()
		{
			return IsSetField((int)QuikFixTags.StopLimitPrice);
		}

		#endregion

		#region IsMarketStopLimit

		public class IsMarketStopLimitField : BooleanField
		{
			public IsMarketStopLimitField()
				: base((int)QuikFixTags.IsMarketStopLimit)
			{
			}

			public IsMarketStopLimitField(bool value)
				: base((int)QuikFixTags.IsMarketStopLimit, value)
			{
			}
		}

		public IsMarketStopLimitField IsMarketStopLimit
		{
			get
			{
				var val = new IsMarketStopLimitField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetIsMarketStopLimit()
		{
			return IsSetField((int)QuikFixTags.IsMarketStopLimit);
		}

		#endregion

		#region ActiveTime

		public class ActiveTimeFromField : DateTimeField
		{
			public ActiveTimeFromField()
				: base((int)QuikFixTags.ActiveTimeFrom)
			{
			}

			public ActiveTimeFromField(DateTime value)
				: base((int)QuikFixTags.ActiveTimeFrom, value)
			{
			}
		}

		public ActiveTimeFromField ActiveTimeFrom
		{
			get
			{
				var val = new ActiveTimeFromField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetActiveTimeFrom()
		{
			return IsSetField((int)QuikFixTags.ActiveTimeFrom);
		}

		public class ActiveTimeToField : DateTimeField
		{
			public ActiveTimeToField()
				: base((int)QuikFixTags.ActiveTimeTo)
			{
			}

			public ActiveTimeToField(DateTime value)
				: base((int)QuikFixTags.ActiveTimeTo, value)
			{
			}
		}

		public ActiveTimeToField ActiveTimeTo
		{
			get
			{
				var val = new ActiveTimeToField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetActiveTimeTo()
		{
			return IsSetField((int)QuikFixTags.ActiveTimeTo);
		}

		#endregion

		#region ConditionOrderId

		public class ConditionOrderIdField : IntField
		{
			public ConditionOrderIdField()
				: base((int)QuikFixTags.ConditionOrderId)
			{
			}

			public ConditionOrderIdField(int value)
				: base((int)QuikFixTags.ConditionOrderId, value)
			{
			}
		}

		public ConditionOrderIdField ConditionOrderId
		{
			get
			{
				var val = new ConditionOrderIdField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetConditionOrderId()
		{
			return IsSetField((int)QuikFixTags.ConditionOrderId);
		}

		#endregion

		#region ConditionOrderSide

		public class ConditionOrderSideField : IntField
		{
			public ConditionOrderSideField()
				: base((int)QuikFixTags.ConditionOrderSide)
			{
			}

			public ConditionOrderSideField(int value)
				: base((int)QuikFixTags.ConditionOrderSide, value)
			{
			}
		}

		public ConditionOrderSideField ConditionOrderSide
		{
			get
			{
				var val = new ConditionOrderSideField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		#endregion

		#region ConditionOrderPartiallyMatched

		public class ConditionOrderPartiallyMatchedField : BooleanField
		{
			public ConditionOrderPartiallyMatchedField()
				: base((int)QuikFixTags.ConditionOrderPartiallyMatched)
			{
			}

			public ConditionOrderPartiallyMatchedField(bool value)
				: base((int)QuikFixTags.ConditionOrderPartiallyMatched, value)
			{
			}
		}

		public ConditionOrderPartiallyMatchedField ConditionOrderPartiallyMatched
		{
			get
			{
				var val = new ConditionOrderPartiallyMatchedField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetConditionOrderPartiallyMatched()
		{
			return IsSetField((int)QuikFixTags.ConditionOrderPartiallyMatched);
		}

		#endregion

		#region ConditionOrderUseMatchedBalance

		public class ConditionOrderUseMatchedBalanceField : BooleanField
		{
			public ConditionOrderUseMatchedBalanceField()
				: base((int)QuikFixTags.ConditionOrderUseMatchedBalance)
			{
			}

			public ConditionOrderUseMatchedBalanceField(bool value)
				: base((int)QuikFixTags.ConditionOrderUseMatchedBalance, value)
			{
			}
		}

		public ConditionOrderUseMatchedBalanceField ConditionOrderUseMatchedBalance
		{
			get
			{
				var val = new ConditionOrderUseMatchedBalanceField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetConditionOrderUseMatchedBalance()
		{
			return IsSetField((int)QuikFixTags.ConditionOrderUseMatchedBalance);
		}

		#endregion

		#region LinkedOrderPrice

		public class LinkedOrderPriceField : DecimalField
		{
			public LinkedOrderPriceField()
				: base((int)QuikFixTags.LinkedOrderPrice)
			{
			}

			public LinkedOrderPriceField(decimal value)
				: base((int)QuikFixTags.LinkedOrderPrice, value)
			{
			}
		}

		public LinkedOrderPriceField LinkedOrderPrice
		{
			get
			{
				var val = new LinkedOrderPriceField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetLinkedOrderPrice()
		{
			return IsSetField((int)QuikFixTags.LinkedOrderPrice);
		}

		#endregion

		#region LinkedOrderCancel

		public class LinkedOrderCancelField : BooleanField
		{
			public LinkedOrderCancelField()
				: base((int)QuikFixTags.LinkedOrderCancel)
			{
			}

			public LinkedOrderCancelField(bool value)
				: base((int)QuikFixTags.LinkedOrderCancel, value)
			{
			}
		}

		public LinkedOrderCancelField LinkedOrderCancel
		{
			get
			{
				var val = new LinkedOrderCancelField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		#endregion

		#region Offset

		public class OffsetField : StringField
		{
			public OffsetField()
				: base((int)QuikFixTags.Offset)
			{
			}

			public OffsetField(string value)
				: base((int)QuikFixTags.Offset, value)
			{
			}
		}

		public OffsetField Offset
		{
			get
			{
				var val = new OffsetField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetOffset()
		{
			return IsSetField((int)QuikFixTags.Offset);
		}

		#endregion

		#region Spread

		public class SpreadField : StringField
		{
			public SpreadField()
				: base((int)QuikFixTags.StopSpread)
			{
			}

			public SpreadField(string value)
				: base((int)QuikFixTags.StopSpread, value)
			{
			}
		}

		public SpreadField StopSpread
		{
			get
			{
				var val = new SpreadField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetStopSpread()
		{
			return IsSetField((int)QuikFixTags.StopSpread);
		}

		#endregion

		#region IsMarketTakeProfit

		public class IsMarketTakeProfitField : BooleanField
		{
			public IsMarketTakeProfitField()
				: base((int)QuikFixTags.IsMarketTakeProfit)
			{
			}

			public IsMarketTakeProfitField(bool value)
				: base((int)QuikFixTags.IsMarketTakeProfit, value)
			{
			}
		}

		public IsMarketTakeProfitField IsMarketTakeProfit
		{
			get
			{
				var val = new IsMarketTakeProfitField();
				GetField(val);
				return val;
			}
			set { SetField(value); }
		}

		public bool IsSetIsMarketTakeProfit()
		{
			return IsSetField((int)QuikFixTags.IsMarketTakeProfit);
		}

		#endregion
	}
}