namespace StockSharp.Quik.Lua
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Fix;
	using StockSharp.Fix.Native;
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
		/// Отправить данные по условию заявки.
		/// </summary>
		/// <param name="writer">Писатель FIX данных.</param>
		/// <param name="regMsg">Сообщение, содержащее информацию для регистрации заявки.</param>
		protected override void SendFixOrderCondition(IFixWriter writer, OrderRegisterMessage regMsg)
		{
			var condition = (QuikOrderCondition)regMsg.Condition;

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

		/// <summary>
		/// Метод вызывается при обработке полученного сообщения.
		/// </summary>
		/// <param name="msgType">Тип FIX сообщения.</param>
		/// <param name="reader">Читатель данных, записанных в формате FIX протокола.</param>
		/// <returns>Успешно ли обработаны данные.</returns>
		protected override bool? ProcessTransactionMessage(string msgType, IFixReader reader)
		{
			switch (msgType)
			{
				case QuikFixMessages.StopOrderExecutionReport:
				{
					int? type = null;
					int? stopPriceCondition = null;
					int? conditionOrderSide = null;
					bool? linkedOrderCancel = null;
					int? result = null;
					string otherSecurityCode = null;
					decimal? stopPrice = null;
					decimal? stopLimitPrice = null;
					bool? isMarketStopLimit = null;
					DateTimeOffset? activeTimeFrom = null;
					DateTimeOffset? activeTimeTo = null;
					long? conditionOrderId = null;
					bool? conditionOrderPartiallyMatched = null;
					bool? conditionOrderUseMatchedBalance = null;
					decimal? linkedOrderPrice = null;
					string offset = null;
					string stopSpread = null;
					bool? isMarketTakeProfit = null;

					var executions = reader.ReadExecutionMessage(Session, SessionHolder.UtcOffset, tag =>
					{
						switch ((QuikFixTags)tag)
						{
							case QuikFixTags.Type:
								type = reader.ReadInt();
								return true;
							case QuikFixTags.StopPriceCondition:
								stopPriceCondition = reader.ReadInt();
								return true;
							case QuikFixTags.ConditionOrderSide:
								conditionOrderSide = reader.ReadInt();
								return true;
							case QuikFixTags.LinkedOrderCancel:
								linkedOrderCancel = reader.ReadBool();
								return true;
							case QuikFixTags.Result:
								result = reader.ReadInt();
								return true;
							case QuikFixTags.OtherSecurityCode:
								otherSecurityCode = reader.ReadString();
								return true;
							case QuikFixTags.StopPrice:
								stopPrice = reader.ReadDecimal();
								return true;
							case QuikFixTags.StopLimitPrice:
								stopLimitPrice = reader.ReadDecimal();
								return true;
							case QuikFixTags.IsMarketStopLimit:
								isMarketStopLimit = reader.ReadBool();
								return true;
							case QuikFixTags.ActiveTimeFrom:
								activeTimeFrom = reader.ReadDateTime().ApplyTimeZone(SessionHolder.UtcOffset);
								return true;
							case QuikFixTags.ActiveTimeTo:
								activeTimeTo = reader.ReadDateTime().ApplyTimeZone(SessionHolder.UtcOffset);
								return true;
							case QuikFixTags.ConditionOrderId:
								conditionOrderId = reader.ReadLong();
								return true;
							case QuikFixTags.ConditionOrderPartiallyMatched:
								conditionOrderPartiallyMatched = reader.ReadBool();
								return true;
							case QuikFixTags.ConditionOrderUseMatchedBalance:
								conditionOrderUseMatchedBalance = reader.ReadBool();
								return true;
							case QuikFixTags.LinkedOrderPrice:
								linkedOrderPrice = reader.ReadDecimal();
								return true;
							case QuikFixTags.Offset:
								offset = reader.ReadString();
								return true;
							case QuikFixTags.StopSpread:
								stopSpread = reader.ReadString();
								return true;
							case QuikFixTags.IsMarketTakeProfit:
								isMarketTakeProfit = reader.ReadBool();
								return true;
							default:
								return false;
						}
					});

					if (executions == null)
						return null;

					var exec = executions.First();

					var condition = new QuikOrderCondition
					{
						Type = (QuikOrderConditionTypes?)type,
						Result = (QuikOrderConditionResults?)result,
						StopPriceCondition = (QuikStopPriceConditions?)stopPriceCondition,
						StopPrice = stopPrice,
						StopLimitPrice = stopLimitPrice,
						IsMarketStopLimit = isMarketStopLimit,
						ConditionOrderId = conditionOrderId,
						ConditionOrderSide = (Sides?)conditionOrderSide,
						ConditionOrderPartiallyMatched = conditionOrderPartiallyMatched,
						ConditionOrderUseMatchedBalance = conditionOrderUseMatchedBalance,
						LinkedOrderPrice = linkedOrderPrice,
						LinkedOrderCancel = linkedOrderCancel,
						Offset = offset == null ? null : offset.ToUnit(),
						Spread = stopSpread == null ? null : stopSpread.ToUnit(),
						IsMarketTakeProfit = isMarketTakeProfit,
					};

					if (otherSecurityCode != null)
						condition.OtherSecurityId = new SecurityId { SecurityCode = otherSecurityCode };

					if (activeTimeFrom != null && activeTimeTo != null)
						condition.ActiveTime = new Range<DateTimeOffset>(activeTimeFrom.Value, activeTimeTo.Value);

					exec.Condition = condition;

					SendOutMessage(exec);

					return true;
				}
			}

			return base.ProcessTransactionMessage(msgType, reader);
		}
	}
}
