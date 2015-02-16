namespace StockSharp.Quik.Lua
{
	using System;

	using QuickFix.Fields;
	using QuickFix.FIX44;

	class StopOrderExecutionReport : ExecutionReport
	{
		public StopOrderExecutionReport()
		{
			Header.SetField(new MsgType(MsgType));
		}

		public new const string MsgType = "StockSharp8";

		#region Type

		private const int _typeFieldTag = 5020;

		public class TypeField : IntField
		{
			public TypeField()
				: base(_typeFieldTag)
			{
			}

			public TypeField(int value)
				: base(_typeFieldTag, value)
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

		private const int _resultFieldTag = 5021;

		public class ResultField : IntField
		{
			public ResultField()
				: base(_resultFieldTag)
			{
			}

			public ResultField(int value)
				: base(_resultFieldTag, value)
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
			return IsSetField(_resultFieldTag);
		}

		#endregion

		#region OtherSecurity

		private const int _otherSecurityCodeFieldTag = 5022;

		public class OtherSecurityCodeField : StringField
		{
			public OtherSecurityCodeField()
				: base(_otherSecurityCodeFieldTag)
			{
			}

			public OtherSecurityCodeField(string value)
				: base(_otherSecurityCodeFieldTag, value)
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
			return IsSetField(_otherSecurityCodeFieldTag);
		}

		#endregion

		#region StopPriceCondition

		private const int _stopPriceConditionFieldTag = 5023;

		public class StopPriceConditionField : IntField
		{
			public StopPriceConditionField()
				: base(_stopPriceConditionFieldTag)
			{
			}

			public StopPriceConditionField(int value)
				: base(_stopPriceConditionFieldTag, value)
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

		private const int _stopLimitPriceFieldTag = 5024;

		public class StopLimitPriceField : DecimalField
		{
			public StopLimitPriceField()
				: base(_stopLimitPriceFieldTag)
			{
			}

			public StopLimitPriceField(decimal value)
				: base(_stopLimitPriceFieldTag, value)
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
			return IsSetField(_stopLimitPriceFieldTag);
		}

		#endregion

		#region IsMarketStopLimit

		private const int _isMarketStopLimitFieldTag = 5025;

		public class IsMarketStopLimitField : BooleanField
		{
			public IsMarketStopLimitField()
				: base(_isMarketStopLimitFieldTag)
			{
			}

			public IsMarketStopLimitField(bool value)
				: base(_isMarketStopLimitFieldTag, value)
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
			return IsSetField(_isMarketStopLimitFieldTag);
		}

		#endregion

		#region ActiveTime

		private const int _activeTimeFromFieldTag = 5026;

		public class ActiveTimeFromField : DateTimeField
		{
			public ActiveTimeFromField()
				: base(_activeTimeFromFieldTag)
			{
			}

			public ActiveTimeFromField(DateTime value)
				: base(_activeTimeFromFieldTag, value)
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
			return IsSetField(_activeTimeFromFieldTag);
		}

		private const int _activeTimeToFieldTag = 5027;

		public class ActiveTimeToField : DateTimeField
		{
			public ActiveTimeToField()
				: base(_activeTimeToFieldTag)
			{
			}

			public ActiveTimeToField(DateTime value)
				: base(_activeTimeToFieldTag, value)
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
			return IsSetField(_activeTimeToFieldTag);
		}

		#endregion

		#region ConditionOrderId

		private const int _conditionOrderIdFieldTag = 5028;

		public class ConditionOrderIdField : IntField
		{
			public ConditionOrderIdField()
				: base(_conditionOrderIdFieldTag)
			{
			}

			public ConditionOrderIdField(int value)
				: base(_conditionOrderIdFieldTag, value)
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
			return IsSetField(_conditionOrderIdFieldTag);
		}

		#endregion

		#region ConditionOrderSide

		private const int _conditionOrderSideFieldTag = 5029;

		public class ConditionOrderSideField : IntField
		{
			public ConditionOrderSideField()
				: base(_conditionOrderSideFieldTag)
			{
			}

			public ConditionOrderSideField(int value)
				: base(_conditionOrderSideFieldTag, value)
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

		private const int _conditionOrderPartiallyMatchedFieldTag = 5030;

		public class ConditionOrderPartiallyMatchedField : BooleanField
		{
			public ConditionOrderPartiallyMatchedField()
				: base(_conditionOrderPartiallyMatchedFieldTag)
			{
			}

			public ConditionOrderPartiallyMatchedField(bool value)
				: base(_conditionOrderPartiallyMatchedFieldTag, value)
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
			return IsSetField(_conditionOrderPartiallyMatchedFieldTag);
		}

		#endregion

		#region ConditionOrderUseMatchedBalance

		private const int _conditionOrderUseMatchedBalanceFieldTag = 5031;

		public class ConditionOrderUseMatchedBalanceField : BooleanField
		{
			public ConditionOrderUseMatchedBalanceField()
				: base(_conditionOrderUseMatchedBalanceFieldTag)
			{
			}

			public ConditionOrderUseMatchedBalanceField(bool value)
				: base(_conditionOrderUseMatchedBalanceFieldTag, value)
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
			return IsSetField(_conditionOrderUseMatchedBalanceFieldTag);
		}

		#endregion

		#region LinkedOrderPrice

		private const int _linkedOrderPriceFieldTag = 5032;

		public class LinkedOrderPriceField : DecimalField
		{
			public LinkedOrderPriceField()
				: base(_linkedOrderPriceFieldTag)
			{
			}

			public LinkedOrderPriceField(decimal value)
				: base(_linkedOrderPriceFieldTag, value)
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
			return IsSetField(_linkedOrderPriceFieldTag);
		}

		#endregion

		#region LinkedOrderCancel

		private const int _linkedOrderCancelFieldTag = 5033;

		public class LinkedOrderCancelField : BooleanField
		{
			public LinkedOrderCancelField()
				: base(_linkedOrderCancelFieldTag)
			{
			}

			public LinkedOrderCancelField(bool value)
				: base(_linkedOrderCancelFieldTag, value)
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

		private const int _offsetFieldTag = 5034;

		public class OffsetField : StringField
		{
			public OffsetField()
				: base(_offsetFieldTag)
			{
			}

			public OffsetField(string value)
				: base(_offsetFieldTag, value)
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
			return IsSetField(_offsetFieldTag);
		}

		#endregion

		#region Spread

		private const int _spreadFieldTag = 5035;

		public class SpreadField : StringField
		{
			public SpreadField()
				: base(_spreadFieldTag)
			{
			}

			public SpreadField(string value)
				: base(_spreadFieldTag, value)
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
			return IsSetField(_spreadFieldTag);
		}

		#endregion

		#region IsMarketTakeProfit

		private const int _isMarketTakeProfitFieldTag = 5036;

		public class IsMarketTakeProfitField : BooleanField
		{
			public IsMarketTakeProfitField()
				: base(_isMarketTakeProfitFieldTag)
			{
			}

			public IsMarketTakeProfitField(bool value)
				: base(_isMarketTakeProfitFieldTag, value)
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
			return IsSetField(_isMarketTakeProfitFieldTag);
		}

		#endregion
	}
}