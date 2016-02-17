namespace StockSharp.Quik.Lua
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Fix.Native;
	using StockSharp.Messages;

	static class LuaFixExtensions
	{
		public static void WriteOrderCondition(this IFixWriter writer, QuikOrderCondition condition, string dateTimeFormat)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			if (condition == null)
				throw new ArgumentNullException(nameof(condition));

			if (condition.Type != null)
			{
				writer.Write((FixTags)LuaFixTags.Type);
				writer.Write((int)condition.Type.Value);
			}

			if (condition.StopPriceCondition != null)
			{
				writer.Write((FixTags)LuaFixTags.StopPriceCondition);
				writer.Write((int)condition.StopPriceCondition.Value);
			}

			if (condition.ConditionOrderSide != null)
			{
				writer.Write((FixTags)LuaFixTags.ConditionOrderSide);
				writer.Write((int)condition.ConditionOrderSide.Value);
			}

			if (condition.LinkedOrderCancel != null)
			{
				writer.Write((FixTags)LuaFixTags.LinkedOrderCancel);
				writer.Write(condition.LinkedOrderCancel.Value);
			}

			if (condition.Result != null)
			{
				writer.Write((FixTags)LuaFixTags.Result);
				writer.Write((int)condition.Result.Value);
			}

			if (condition.OtherSecurityId != null)
			{
				writer.Write((FixTags)LuaFixTags.OtherSecurityCode);
				writer.Write(condition.OtherSecurityId.Value.SecurityCode);
			}

			if (condition.StopPrice != null)
			{
				writer.Write((FixTags)LuaFixTags.StopPrice);
				writer.Write(condition.StopPrice.Value);
			}

			if (condition.StopLimitPrice != null)
			{
				writer.Write((FixTags)LuaFixTags.StopLimitPrice);
				writer.Write(condition.StopLimitPrice.Value);
			}

			if (condition.IsMarketStopLimit != null)
			{
				writer.Write((FixTags)LuaFixTags.IsMarketStopLimit);
				writer.Write(condition.IsMarketStopLimit.Value);
			}

			if (condition.ActiveTime != null)
			{
				writer.Write((FixTags)LuaFixTags.ActiveTimeFrom);
				writer.Write(condition.ActiveTime.Min.UtcDateTime, dateTimeFormat);

				writer.Write((FixTags)LuaFixTags.ActiveTimeTo);
				writer.Write(condition.ActiveTime.Max.UtcDateTime, dateTimeFormat);
			}

			if (condition.ConditionOrderId != null)
			{
				writer.Write((FixTags)LuaFixTags.ConditionOrderId);
				writer.Write(condition.ConditionOrderId.Value);
			}

			if (condition.ConditionOrderPartiallyMatched != null)
			{
				writer.Write((FixTags)LuaFixTags.ConditionOrderPartiallyMatched);
				writer.Write(condition.ConditionOrderPartiallyMatched.Value);
			}

			if (condition.ConditionOrderUseMatchedBalance != null)
			{
				writer.Write((FixTags)LuaFixTags.ConditionOrderUseMatchedBalance);
				writer.Write(condition.ConditionOrderUseMatchedBalance.Value);
			}

			if (condition.LinkedOrderPrice != null)
			{
				writer.Write((FixTags)LuaFixTags.LinkedOrderPrice);
				writer.Write(condition.LinkedOrderPrice.Value);
			}

			if (condition.Offset != null)
			{
				writer.Write((FixTags)LuaFixTags.Offset);
				writer.Write(condition.Offset.ToString());
			}

			if (condition.Spread != null)
			{
				writer.Write((FixTags)LuaFixTags.StopSpread);
				writer.Write(condition.Spread.ToString());
			}

			if (condition.IsMarketTakeProfit != null)
			{
				writer.Write((FixTags)LuaFixTags.IsMarketTakeProfit);
				writer.Write(condition.IsMarketTakeProfit.Value);
			}
		}

		public static bool ReadOrderCondition(this IFixReader reader, FixTags tag, TimeZoneInfo timeZone, string dateTimeFormat, Func<QuikOrderCondition> getCondition)
		{
			if (getCondition == null)
				throw new ArgumentNullException(nameof(getCondition));

			switch ((LuaFixTags)tag)
			{
				case LuaFixTags.Type:
					getCondition().Type = (QuikOrderConditionTypes)reader.ReadInt();
					return true;
				case LuaFixTags.StopPriceCondition:
					getCondition().StopPriceCondition = (QuikStopPriceConditions)reader.ReadInt();
					return true;
				case LuaFixTags.ConditionOrderSide:
					getCondition().ConditionOrderSide = (Sides)reader.ReadInt();
					return true;
				case LuaFixTags.LinkedOrderCancel:
					getCondition().LinkedOrderCancel = reader.ReadBool();
					return true;
				case LuaFixTags.Result:
					getCondition().Result = (QuikOrderConditionResults)reader.ReadInt();
					return true;
				case LuaFixTags.OtherSecurityCode:
					getCondition().OtherSecurityId = new SecurityId { SecurityCode = reader.ReadString() };
					return true;
				case LuaFixTags.StopPrice:
					getCondition().StopPrice = reader.ReadDecimal();
					return true;
				case LuaFixTags.StopLimitPrice:
					getCondition().StopLimitPrice = reader.ReadDecimal();
					return true;
				case LuaFixTags.IsMarketStopLimit:
					getCondition().IsMarketStopLimit = reader.ReadBool();
					return true;
				case LuaFixTags.ActiveTimeFrom:
					if (getCondition().ActiveTime == null)
						getCondition().ActiveTime = new Range<DateTimeOffset>();

					getCondition().ActiveTime.Min = reader.ReadDateTime(dateTimeFormat).ToDateTimeOffset(timeZone);
					return true;
				case LuaFixTags.ActiveTimeTo:
					if (getCondition().ActiveTime == null)
						getCondition().ActiveTime = new Range<DateTimeOffset>();

					getCondition().ActiveTime.Max = reader.ReadDateTime(dateTimeFormat).ToDateTimeOffset(timeZone);
					return true;
				case LuaFixTags.ConditionOrderId:
					getCondition().ConditionOrderId = reader.ReadLong();
					return true;
				case LuaFixTags.ConditionOrderPartiallyMatched:
					getCondition().ConditionOrderPartiallyMatched = reader.ReadBool();
					return true;
				case LuaFixTags.ConditionOrderUseMatchedBalance:
					getCondition().ConditionOrderUseMatchedBalance = reader.ReadBool();
					return true;
				case LuaFixTags.LinkedOrderPrice:
					getCondition().LinkedOrderPrice = reader.ReadDecimal();
					return true;
				case LuaFixTags.Offset:
					getCondition().Offset = reader.ReadString().ToUnit();
					return true;
				case LuaFixTags.StopSpread:
					getCondition().Spread = reader.ReadString().ToUnit();
					return true;
				case LuaFixTags.IsMarketTakeProfit:
					getCondition().IsMarketTakeProfit = reader.ReadBool();
					return true;
				default:
					return false;
			}
		}
	}
}