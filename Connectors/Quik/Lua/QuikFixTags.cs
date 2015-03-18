namespace StockSharp.Quik.Lua
{
	enum QuikFixTags
	{
		Type = 5020,
		StopPriceCondition = 5021,
		ConditionOrderSide = 5022,
		LinkedOrderCancel = 5023,
		Result = 5024,
		OtherSecurityCode = 5025,
		StopPrice = 5026,
		StopLimitPrice = 5027,
		IsMarketStopLimit = 5028,
		ActiveTimeFrom = 5029,
		ActiveTimeTo = 5030,
		ConditionOrderId = 5031,
		ConditionOrderPartiallyMatched = 5032,
		ConditionOrderUseMatchedBalance = 5033,
		LinkedOrderPrice = 5034,
		Offset = 5035,
		StopSpread = 5036,
		IsMarketTakeProfit = 5037
	}

	static class QuikFixMessages
	{
		public const string NewStopOrderSingle = "DD";
		public const string StopOrderExecutionReport = "88";
	}
}