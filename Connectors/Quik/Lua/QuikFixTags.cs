namespace StockSharp.Quik.Lua
{
	using StockSharp.Fix.Native;

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

	static class QuikFixExtensions
	{
		public static void WriteOrderCondition(this IFixWriter writer, QuikOrderCondition condition)
		{
			if (condition.Type != null)
			{
				writer.Write((FixTags)QuikFixTags.Type);
				writer.Write((int)condition.Type.Value);
			}

			if (condition.StopPriceCondition != null)
			{
				writer.Write((FixTags)QuikFixTags.StopPriceCondition);
				writer.Write((int)condition.StopPriceCondition.Value);
			}

			if (condition.ConditionOrderSide != null)
			{
				writer.Write((FixTags)QuikFixTags.ConditionOrderSide);
				writer.Write((int)condition.ConditionOrderSide.Value);
			}

			if (condition.LinkedOrderCancel != null)
			{
				writer.Write((FixTags)QuikFixTags.LinkedOrderCancel);
				writer.Write(condition.LinkedOrderCancel.Value);
			}

			if (condition.Result != null)
			{
				writer.Write((FixTags)QuikFixTags.Result);
				writer.Write((int)condition.Result.Value);
			}

			if (condition.OtherSecurityId != null)
			{
				writer.Write((FixTags)QuikFixTags.OtherSecurityCode);
				writer.Write(condition.OtherSecurityId.Value.SecurityCode);
			}

			if (condition.StopPrice != null)
			{
				writer.Write((FixTags)QuikFixTags.StopPrice);
				writer.Write(condition.StopPrice.Value);
			}

			if (condition.StopLimitPrice != null)
			{
				writer.Write((FixTags)QuikFixTags.StopLimitPrice);
				writer.Write(condition.StopLimitPrice.Value);
			}

			if (condition.IsMarketStopLimit != null)
			{
				writer.Write((FixTags)QuikFixTags.IsMarketStopLimit);
				writer.Write(condition.IsMarketStopLimit.Value);
			}

			if (condition.ActiveTime != null)
			{
				writer.Write((FixTags)QuikFixTags.ActiveTimeFrom);
				writer.Write(condition.ActiveTime.Min.UtcDateTime);

				writer.Write((FixTags)QuikFixTags.ActiveTimeTo);
				writer.Write(condition.ActiveTime.Max.UtcDateTime);
			}

			if (condition.ConditionOrderId != null)
			{
				writer.Write((FixTags)QuikFixTags.ConditionOrderId);
				writer.Write(condition.ConditionOrderId.Value);
			}

			if (condition.ConditionOrderPartiallyMatched != null)
			{
				writer.Write((FixTags)QuikFixTags.ConditionOrderPartiallyMatched);
				writer.Write(condition.ConditionOrderPartiallyMatched.Value);
			}

			if (condition.ConditionOrderUseMatchedBalance != null)
			{
				writer.Write((FixTags)QuikFixTags.ConditionOrderUseMatchedBalance);
				writer.Write(condition.ConditionOrderUseMatchedBalance.Value);
			}

			if (condition.LinkedOrderPrice != null)
			{
				writer.Write((FixTags)QuikFixTags.LinkedOrderPrice);
				writer.Write(condition.LinkedOrderPrice.Value);
			}

			if (condition.Offset != null)
			{
				writer.Write((FixTags)QuikFixTags.Offset);
				writer.Write(condition.Offset.ToString());
			}

			if (condition.Spread != null)
			{
				writer.Write((FixTags)QuikFixTags.StopSpread);
				writer.Write(condition.Spread.ToString());
			}

			if (condition.IsMarketTakeProfit != null)
			{
				writer.Write((FixTags)QuikFixTags.IsMarketTakeProfit);
				writer.Write(condition.IsMarketTakeProfit.Value);
			}
		}
	}
}