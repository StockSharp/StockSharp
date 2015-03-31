namespace StockSharp.Quik.Lua
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Fix.Native;
	using StockSharp.Messages;

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

		public static bool ReadOrderCondition(this IFixReader reader, FixTags tag, TimeSpan dateTimeOffset, QuikOrderCondition condition)
		{
			switch ((QuikFixTags)tag)
			{
				case QuikFixTags.Type:
					condition.Type = (QuikOrderConditionTypes)reader.ReadInt();
					return true;
				case QuikFixTags.StopPriceCondition:
					condition.StopPriceCondition = (QuikStopPriceConditions)reader.ReadInt();
					return true;
				case QuikFixTags.ConditionOrderSide:
					condition.ConditionOrderSide = (Sides)reader.ReadInt();
					return true;
				case QuikFixTags.LinkedOrderCancel:
					condition.LinkedOrderCancel = reader.ReadBool();
					return true;
				case QuikFixTags.Result:
					condition.Result = (QuikOrderConditionResults)reader.ReadInt();
					return true;
				case QuikFixTags.OtherSecurityCode:
					condition.OtherSecurityId = new SecurityId { SecurityCode = reader.ReadString() };
					return true;
				case QuikFixTags.StopPrice:
					condition.StopPrice = reader.ReadDecimal();
					return true;
				case QuikFixTags.StopLimitPrice:
					condition.StopLimitPrice = reader.ReadDecimal();
					return true;
				case QuikFixTags.IsMarketStopLimit:
					condition.IsMarketStopLimit = reader.ReadBool();
					return true;
				case QuikFixTags.ActiveTimeFrom:
					if (condition.ActiveTime == null)
						condition.ActiveTime = new Range<DateTimeOffset>();

					condition.ActiveTime.Min = reader.ReadDateTime().ApplyTimeZone(dateTimeOffset);
					return true;
				case QuikFixTags.ActiveTimeTo:
					if (condition.ActiveTime == null)
						condition.ActiveTime = new Range<DateTimeOffset>();

					condition.ActiveTime.Max = reader.ReadDateTime().ApplyTimeZone(dateTimeOffset);
					return true;
				case QuikFixTags.ConditionOrderId:
					condition.ConditionOrderId = reader.ReadLong();
					return true;
				case QuikFixTags.ConditionOrderPartiallyMatched:
					condition.ConditionOrderPartiallyMatched = reader.ReadBool();
					return true;
				case QuikFixTags.ConditionOrderUseMatchedBalance:
					condition.ConditionOrderUseMatchedBalance = reader.ReadBool();
					return true;
				case QuikFixTags.LinkedOrderPrice:
					condition.LinkedOrderPrice = reader.ReadDecimal();
					return true;
				case QuikFixTags.Offset:
					condition.Offset = reader.ReadString().ToUnit();
					return true;
				case QuikFixTags.StopSpread:
					condition.Spread = reader.ReadString().ToUnit();
					return true;
				case QuikFixTags.IsMarketTakeProfit:
					condition.IsMarketTakeProfit = reader.ReadBool();
					return true;
				default:
					return false;
			}
		}
	}
}