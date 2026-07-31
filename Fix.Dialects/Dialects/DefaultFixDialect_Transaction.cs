namespace StockSharp.Fix.Dialects;

using TimeInForce = StockSharp.Fix.Native.FixTimeInForce;

partial class DefaultFixDialect
{
	private async ValueTask WriteAccountAndPartiesAsync(IFixWriter writer, OrderMessage orderMsg, CancellationToken cancellationToken)
	{
		await WriteAccountAsync(writer, orderMsg, cancellationToken);

		var hasClientCode = !orderMsg.ClientCode.IsEmpty();
		var hasBrokerCode = !orderMsg.BrokerCode.IsEmpty();

		var partyCount = 0 + (hasClientCode ? 1 : 0) + (hasBrokerCode ? 1 : 0);

		if (partyCount == 0)
			return;

		await writer.WriteAsync(FixTags.NoPartyIDs, cancellationToken);
		await writer.WriteAsync(partyCount, cancellationToken);

		if (hasClientCode)
		{
			await writer.WriteAsync(FixTags.PartyID, cancellationToken);
			await writer.WriteAsync(orderMsg.ClientCode, cancellationToken);

			await writer.WriteAsync(FixTags.PartyIDSource, cancellationToken);
			await writer.WriteAsync(PartyIDSource.GenerallyAcceptedMarketParticipantIdentifier, cancellationToken);

			await writer.WriteAsync(FixTags.PartyRole, cancellationToken);
			await writer.WriteAsync((int)PartyRole.ClientId, cancellationToken);
		}

		if (hasBrokerCode)
		{
			await writer.WriteAsync(FixTags.PartyID, cancellationToken);
			await writer.WriteAsync(orderMsg.BrokerCode, cancellationToken);

			await writer.WriteAsync(FixTags.PartyIDSource, cancellationToken);
			await writer.WriteAsync(PartyIDSource.GenerallyAcceptedMarketParticipantIdentifier, cancellationToken);

			await writer.WriteAsync(FixTags.PartyRole, cancellationToken);
			await writer.WriteAsync((int)PartyRole.EnteringFirm, cancellationToken);
		}
	}

	private async ValueTask<string> WriteNewOrderSingleAsync(IFixWriter writer, OrderRegisterMessage regMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.ClOrdID, cancellationToken);
		await writer.WriteAsync(regMsg.TransactionId.To<string>(), cancellationToken);

		var securityId = regMsg.SecurityId;

		await WriteSecurityIdAsync(writer, regMsg, cancellationToken);

		await writer.WriteAsync(FixTags.ExDestination, cancellationToken);
		await writer.WriteAsync(securityId.BoardCode, cancellationToken);

		await writer.WriteTransactTimeAsync(TimeStampParser, cancellationToken);

		await writer.WriteSideAsync(regMsg.Side, cancellationToken);

		await writer.WriteAsync(FixTags.OrdType, cancellationToken);
		await writer.WriteAsync(regMsg.GetFixType(), cancellationToken);

		await WriteAccountAndPartiesAsync(writer, regMsg, cancellationToken);

		await writer.WriteAsync(FixTags.OrderQty, cancellationToken);
		await writer.WriteAsync(regMsg.Volume, cancellationToken);

		await writer.WriteHandlInstAsync(regMsg, HandlInst.AutomatedExecutionOrderPrivate, cancellationToken);

		// An absent tag 59 reads as Day, so order entry always states it.
		var tif = regMsg.GetFixTimeInForce();

		var secType = regMsg.ToSecurityType();

		if (secType != null)
		{
			await writer.WriteAsync(FixTags.SecurityType, cancellationToken);
			await writer.WriteAsync(secType, cancellationToken);
		}

		if (regMsg.VisibleVolume != null)
		{
			await writer.WriteAsync(FixTags.MaxFloor, cancellationToken);
			await writer.WriteAsync(regMsg.VisibleVolume.Value, cancellationToken);
		}

		if (regMsg.Condition != null)
			await WriteOrderConditionAsync(writer, regMsg.Condition, cancellationToken);

		if (regMsg.OrderType != OrderTypes.Market)
		{
			await writer.WriteAsync(FixTags.Price, cancellationToken);
			await writer.WriteAsync(regMsg.Price, cancellationToken);
		}

		await writer.WriteAsync(FixTags.TimeInForce, cancellationToken);
		await writer.WriteAsync(tif, cancellationToken);

		if (tif == TimeInForce.GoodTillDate && regMsg.TillDate != null)
		{
			var tillDate = regMsg.TillDate.Value;

			if (tillDate.TimeOfDay == default)
			{
				await writer.WriteAsync(FixTags.ExpireDate, cancellationToken);
				await writer.WriteAsync(tillDate, DateParser, cancellationToken);
			}
			else
			{
				await writer.WriteAsync(FixTags.ExpireTime, cancellationToken);
				await writer.WriteAsync(tillDate, TimeStampParser, cancellationToken);
			}
		}

		if (!regMsg.Comment.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Text, cancellationToken);
			await writer.WriteAsync(Convert(regMsg.Comment), cancellationToken);
		}

		if (regMsg.IsMarketMaker == true)
		{
			await writer.WriteMarketMakerAsync(cancellationToken);
		}

		if (regMsg.MarginMode is not null)
		{
			await writer.WriteAsync(FixTags.CashMargin, cancellationToken);
			await writer.WriteAsync(1, cancellationToken);
		}

		if (regMsg.Slippage != null)
		{
			await writer.WriteAsync(FixTags.Slippage, cancellationToken);
			await writer.WriteAsync(regMsg.Slippage.Value, cancellationToken);
		}

		if (regMsg.IsManual != null)
		{
			await writer.WriteAsync(FixTags.ManualOrderIndicator, cancellationToken);
			await writer.WriteAsync(regMsg.IsManual.Value, cancellationToken);
		}

		await writer.WritePositionEffectAsync(regMsg.PositionEffect, cancellationToken);

		if (regMsg.PostOnly != null)
		{
			await writer.WriteAsync(FixTags.PostOnly, cancellationToken);
			await writer.WriteAsync(regMsg.PostOnly.Value, cancellationToken);
		}

		if (regMsg.Leverage != null)
		{
			await writer.WriteAsync(FixTags.Leverage, cancellationToken);
			await writer.WriteAsync(regMsg.Leverage.Value, cancellationToken);
		}

		return FixMessages.NewOrderSingle;
	}

	/// <summary>
	/// To record data by the order condition.
	/// </summary>
	/// <param name="writer">FIX data writer.</param>
	/// <param name="condition">Order condition (e.g., stop- and algo- orders parameters).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	protected virtual async ValueTask WriteOrderConditionAsync(IFixWriter writer, OrderCondition condition, CancellationToken cancellationToken)
	{
		var fixCondition = (FixOrderCondition)condition;

		if (fixCondition.StopLoss is decimal sl)
		{
			await writer.WriteAsync(FixTags.StopPx, cancellationToken);
			await writer.WriteAsync(sl, cancellationToken);
		}

		if (fixCondition.TakeProfit is decimal tp)
		{
			await writer.WriteAsync(FixTags.TakeProfit, cancellationToken);
			await writer.WriteAsync(tp, cancellationToken);
		}

		if (fixCondition.Offset != null)
		{
			await writer.WriteAsync(FixTags.PegOffsetValue, cancellationToken);
			await writer.WriteAsync(fixCondition.Offset.Value, cancellationToken);

			await writer.WriteAsync(FixTags.PegOffsetType, cancellationToken);
			await writer.WriteAsync(0, cancellationToken);
		}
	}

	/// <summary>
	/// To read the order condition <see cref="OrderCondition"/>.
	/// </summary>
	/// <param name="reader">Data reader.</param>
	/// <param name="tag">Tag.</param>
	/// <param name="getCondition">The function returning the order condition.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Whether the data was successfully processed.</returns>
	protected virtual ValueTask<bool> ReadOrderConditionAsync(IFixReader reader, FixTags tag, Func<OrderCondition> getCondition, CancellationToken cancellationToken)
	{
		return reader.ReadOrderConditionAsync(tag, getCondition, cancellationToken);
	}

	///// <summary>
	///// The final initialization of the order condition.
	///// </summary>
	///// <param name="ordType">Order type.</param>
	///// <param name="condition">The order condition.</param>
	//protected virtual void PostInitCondition(char ordType, OrderCondition condition)
	//{
	//	var fixCon = (FixOrderCondition)condition;
	//
	//	switch (ordType)
	//	{
	//		case OrdType.Stop:
	//		case OrdType.StopLimit:
	//			fixCon.Type = FixStopOrderType.StopLoss;
	//			break;
	//		case Native.Extensions.TakeProfit:
	//			fixCon.Type = FixStopOrderType.TakeProfit;
	//			break;
	//		case Native.Extensions.TakeProfitTrailing:
	//			fixCon.Type = FixStopOrderType.TakeProfitTrailing;
	//			break;
	//		case Native.Extensions.StopTrailing:
	//			fixCon.Type = FixStopOrderType.StopLossTrailing;
	//			break;
	//		default:
	//			throw new ArgumentOutOfRangeException(nameof(ordType));
	//	}
	//}

	private async ValueTask<string> WriteOrderCancelRequestAsync(IFixWriter writer, OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		await WriteAccountAndPartiesAsync(writer, cancelMsg, cancellationToken);

		await writer.WriteAsync(FixTags.OrigClOrdID, cancellationToken);
		await WriteClOrdIdAsync(writer, cancelMsg.OriginalTransactionId, cancellationToken);

		await writer.WriteAsync(FixTags.ClOrdID, cancellationToken);
		await writer.WriteAsync(cancelMsg.TransactionId.To<string>(), cancellationToken);

		if (cancelMsg.OrderId != null)
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(cancelMsg.OrderId.Value.To<string>(), cancellationToken);
		}
		else if (!cancelMsg.OrderStringId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(cancelMsg.OrderStringId, cancellationToken);
		}

		await WriteSecurityIdAsync(writer, cancelMsg, cancellationToken);

		if (cancelMsg.Side != null)
		{
			await writer.WriteSideAsync(cancelMsg.Side.Value, cancellationToken);
		}

		if (cancelMsg.OrderType != null)
		{
			await writer.WriteAsync(FixTags.OrdType, cancellationToken);
			await writer.WriteAsync(cancelMsg.GetFixType(), cancellationToken);
		}

		if (cancelMsg.Balance != null)
		{
			await writer.WriteAsync(FixTags.LeavesQty, cancellationToken);
			await writer.WriteAsync(cancelMsg.Balance.Value, cancellationToken);
		}

		if (cancelMsg.Volume != null)
		{
			await writer.WriteAsync(FixTags.OrderQty, cancellationToken);
			await writer.WriteAsync(cancelMsg.Volume.Value, cancellationToken);
		}

		if (!cancelMsg.Comment.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Text, cancellationToken);
			await writer.WriteAsync(Convert(cancelMsg.Comment), cancellationToken);
		}

		await writer.WriteTransactTimeAsync(TimeStampParser, cancellationToken);

		return FixMessages.OrderCancelRequest;
	}

	private async ValueTask<string> WriteOrderCancelReplaceRequestAsync(IFixWriter writer, OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
	{
		await WriteAccountAndPartiesAsync(writer, replaceMsg, cancellationToken);

		await writer.WriteAsync(FixTags.ClOrdID, cancellationToken);
		await writer.WriteAsync(replaceMsg.TransactionId.To<string>(), cancellationToken);

		if (replaceMsg.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.OrigClOrdID, cancellationToken);
			await WriteClOrdIdAsync(writer, replaceMsg.OriginalTransactionId, cancellationToken);
		}

		if (replaceMsg.OldOrderId != null)
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(replaceMsg.OldOrderId.Value.To<string>(), cancellationToken);
		}
		else if (!replaceMsg.OldOrderStringId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(replaceMsg.OldOrderStringId, cancellationToken);
		}

		await WriteSecurityIdAsync(writer, replaceMsg, cancellationToken);

		await writer.WriteSideAsync(replaceMsg.Side, cancellationToken);

		await writer.WriteAsync(FixTags.OrderQty, cancellationToken);
		await writer.WriteAsync(replaceMsg.Volume, cancellationToken);

		await writer.WriteAsync(FixTags.OrdType, cancellationToken);
		await writer.WriteAsync(replaceMsg.GetFixType(), cancellationToken);

		if (replaceMsg.OrderType != OrderTypes.Market)
		{
			await writer.WriteAsync(FixTags.Price, cancellationToken);
			await writer.WriteAsync(replaceMsg.Price, cancellationToken);
		}

		var condition = (FixOrderCondition)replaceMsg.Condition;

		if (condition?.StopLoss is decimal sl)
		{
			await writer.WriteAsync(FixTags.StopPx, cancellationToken);
			await writer.WriteAsync(sl, cancellationToken);
		}

		if (condition?.TakeProfit is decimal tp)
		{
			await writer.WriteAsync(FixTags.TakeProfit, cancellationToken);
			await writer.WriteAsync(tp, cancellationToken);
		}

		// An absent tag 59 reads as Day, so a replace states it rather than leave the lifetime to
		// the counterparty's default.
		var tif = replaceMsg.GetFixTimeInForce();

		await writer.WriteAsync(FixTags.TimeInForce, cancellationToken);
		await writer.WriteAsync(tif, cancellationToken);

		if (tif == FixTimeInForce.GoodTillDate)
		{
			await writer.WriteExpiryDateAsync(replaceMsg, DateParser, TimeZone, cancellationToken);
		}

		await writer.WriteTransactTimeAsync(TimeStampParser, cancellationToken);

		if (replaceMsg.MarginMode is not null)
		{
			await writer.WriteAsync(FixTags.CashMargin, cancellationToken);
			await writer.WriteAsync(1, cancellationToken);
		}

		if (replaceMsg.Slippage != null)
		{
			await writer.WriteAsync(FixTags.Slippage, cancellationToken);
			await writer.WriteAsync(replaceMsg.Slippage.Value, cancellationToken);
		}

		if (replaceMsg.IsManual != null)
		{
			await writer.WriteAsync(FixTags.ManualOrderIndicator, cancellationToken);
			await writer.WriteAsync(replaceMsg.IsManual.Value, cancellationToken);
		}

		await writer.WritePositionEffectAsync(replaceMsg.PositionEffect, cancellationToken);

		if (replaceMsg.PostOnly != null)
		{
			await writer.WriteAsync(FixTags.PostOnly, cancellationToken);
			await writer.WriteAsync(replaceMsg.PostOnly.Value, cancellationToken);
		}

		if (replaceMsg.OldOrderPrice != null)
		{
			await writer.WriteAsync(FixTags.OldPrice, cancellationToken);
			await writer.WriteAsync(replaceMsg.OldOrderPrice.Value, cancellationToken);
		}

		if (replaceMsg.OldOrderVolume != null)
		{
			await writer.WriteAsync(FixTags.OldVolume, cancellationToken);
			await writer.WriteAsync(replaceMsg.OldOrderVolume.Value, cancellationToken);
		}

		if (replaceMsg.Leverage != null)
		{
			await writer.WriteAsync(FixTags.Leverage, cancellationToken);
			await writer.WriteAsync(replaceMsg.Leverage.Value, cancellationToken);
		}

		return FixMessages.OrderCancelReplaceRequest;
	}

	private async ValueTask<string> WriteOrderMassCancelRequestAsync(IFixWriter writer, OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.ClOrdID, cancellationToken);
		await writer.WriteAsync(cancelMsg.TransactionId.To<string>(), cancellationToken);

		await writer.WriteTransactTimeAsync(TimeStampParser, cancellationToken);

		if (!cancelMsg.PortfolioName.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Account, cancellationToken);
			await writer.WriteAsync(cancelMsg.PortfolioName, cancellationToken);
		}

		var requestType = MassCancelRequestType.CancelAllOrders;

		if (!cancelMsg.SecurityId.SecurityCode.IsEmpty())
		{
			requestType = MassCancelRequestType.CancelOrdersForSecurity;

			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(cancelMsg.SecurityId.SecurityCode, cancellationToken);

			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(cancelMsg.SecurityId.BoardCode, cancellationToken);
		}

		if (!cancelMsg.SecurityId.BoardCode.IsEmpty())
		{
			requestType = MassCancelRequestType.CancelOrdersForMarket;

			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(cancelMsg.SecurityId.BoardCode, cancellationToken);
		}

		var secTypes = cancelMsg.GetSecurityTypes();

		if (secTypes.Count > 0)
		{
			requestType = MassCancelRequestType.CancelOrdersForSecurityType;

			await writer.WriteAsync(FixTags.NoSecurityTypes, cancellationToken);
			await writer.WriteAsync(secTypes.Count, cancellationToken);

			foreach (var secType in secTypes)
			{
				await writer.WriteAsync(FixTags.SecurityType, cancellationToken);
				await writer.WriteAsync(secType.ToFix(), cancellationToken);
			}
		}

		if (cancelMsg.Side != null)
		{
			await writer.WriteSideAsync(cancelMsg.Side.Value, cancellationToken);
		}

		await writer.WriteAsync(FixTags.CancelMode, cancellationToken);
		await writer.WriteAsync((int)cancelMsg.Mode, cancellationToken);

		await writer.WriteAsync(FixTags.MassCancelRequestType, cancellationToken);
		await writer.WriteAsync(requestType, cancellationToken);

		return FixMessages.OrderMassCancelRequest;
	}

	private static async ValueTask<string> WriteOrderMassStatusRequestAsync(IFixWriter writer, OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.MassStatusReqID, cancellationToken);
		await writer.WriteAsync(statusMsg.TransactionId.To<string>(), cancellationToken);

		if (statusMsg.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.OrigClOrdID, cancellationToken);
			await writer.WriteAsync(statusMsg.OriginalTransactionId.To<string>(), cancellationToken);
		}

		if (!statusMsg.SecurityId.SecurityCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Symbol, cancellationToken);
			await writer.WriteAsync(statusMsg.SecurityId.SecurityCode, cancellationToken);
		}

		if (!statusMsg.SecurityId.BoardCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.SecurityExchange, cancellationToken);
			await writer.WriteAsync(statusMsg.SecurityId.BoardCode, cancellationToken);
		}

		if (statusMsg.SecurityIds.Length > 0)
		{
			await writer.WriteAsync(FixTags.NoUnderlyings, cancellationToken);
			await writer.WriteAsync(statusMsg.SecurityIds.Length, cancellationToken);

			foreach (var secId in statusMsg.SecurityIds)
			{
				await writer.WriteAsync(FixTags.UnderlyingSymbol, cancellationToken);
				await writer.WriteAsync(secId.SecurityCode, cancellationToken);

				if (!secId.BoardCode.IsEmpty())
				{
					await writer.WriteAsync(FixTags.UnderlyingSecurityExchange, cancellationToken);
					await writer.WriteAsync(secId.BoardCode, cancellationToken);
				}
			}
		}

		if (!statusMsg.PortfolioName.IsEmpty())
		{
			await writer.WriteAsync(FixTags.Account, cancellationToken);
			await writer.WriteAsync(statusMsg.PortfolioName, cancellationToken);
		}

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(statusMsg.GetSubscriptionType(), cancellationToken);

		if (statusMsg.IsSubscribe)
		{
			await writer.WriteAsync(FixTags.MassStatusReqType, cancellationToken);
			await writer.WriteAsync((int)MassStatusReqType.StatusForAllOrders, cancellationToken);

			if (statusMsg.States.Length > 0)
			{
				await writer.WriteAsync(FixTags.OrdStatus, cancellationToken);
				await writer.WriteAsync(statusMsg.States.Select(s => s.To<string>()).JoinComma(), cancellationToken);
			}
		}

		if (statusMsg.IsIncremental)
		{
			await writer.WriteAsync(FixTags.IsIncremental, cancellationToken);
			await writer.WriteAsync(true, cancellationToken);
		}

		if (!statusMsg.UserId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.RequestUserId, cancellationToken);
			await writer.WriteAsync(statusMsg.UserId, cancellationToken);
		}

		return FixMessages.OrderMassStatusRequest;
	}

	private static async ValueTask<string> WriteOrderStatusRequestAsync(IFixWriter writer, OrderStatusMessage statusMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.OrdStatusReqID, cancellationToken);
		await writer.WriteAsync(statusMsg.TransactionId.To<string>(), cancellationToken);

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(statusMsg.GetSubscriptionType(), cancellationToken);

		if (statusMsg.OrderId != null)
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(statusMsg.OrderId.Value.To<string>(), cancellationToken);
		}
		else if (!statusMsg.OrderStringId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.OrderID, cancellationToken);
			await writer.WriteAsync(statusMsg.OrderStringId, cancellationToken);
		}

		if (statusMsg.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.ClOrdID, cancellationToken);
			await writer.WriteAsync(statusMsg.OriginalTransactionId.To<string>(), cancellationToken);
		}

		return FixMessages.OrderStatusRequest;
	}

	private async ValueTask<string> WriteRequestForPositionsAsync(IFixWriter writer, PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(FixTags.PosReqID, cancellationToken);
		await writer.WriteAsync(lookupMsg.TransactionId.To<string>(), cancellationToken);

		if (lookupMsg.OriginalTransactionId != 0)
		{
			await writer.WriteAsync(FixTags.MDResponseID, cancellationToken);
			await writer.WriteAsync(lookupMsg.OriginalTransactionId.To<string>(), cancellationToken);
		}

		await writer.WriteAsync(FixTags.SubscriptionRequestType, cancellationToken);
		await writer.WriteAsync(lookupMsg.GetSubscriptionType(), cancellationToken);

		await writer.WriteAsync(FixTags.PosReqType, cancellationToken);
		await writer.WriteAsync((int)PosReqType.Positions, cancellationToken);

		if (!lookupMsg.PortfolioName.IsEmpty())
		{
			await WriteAccountAsync(writer, lookupMsg, cancellationToken);

			await writer.WriteAsync(FixTags.AccountType, cancellationToken);
			await writer.WriteAsync((int)AccountType.AccountIsCarriedOnCustomerSideBooks, cancellationToken);
		}

		// ClientCode rides in the standard FIX Parties block (PartyRole.ClientId)
		// rather than packed into the Account string. PortfolioMessage has no
		// BrokerCode, so we only emit one party here.
		if (!lookupMsg.ClientCode.IsEmpty())
		{
			await writer.WriteAsync(FixTags.NoPartyIDs, cancellationToken);
			await writer.WriteAsync(1, cancellationToken);

			await writer.WriteAsync(FixTags.PartyID, cancellationToken);
			await writer.WriteAsync(lookupMsg.ClientCode, cancellationToken);

			await writer.WriteAsync(FixTags.PartyIDSource, cancellationToken);
			await writer.WriteAsync(PartyIDSource.GenerallyAcceptedMarketParticipantIdentifier, cancellationToken);

			await writer.WriteAsync(FixTags.PartyRole, cancellationToken);
			await writer.WriteAsync((int)PartyRole.ClientId, cancellationToken);
		}

		if (lookupMsg.SecurityIds.Length > 0)
		{
			await writer.WriteAsync(FixTags.NoUnderlyings, cancellationToken);
			await writer.WriteAsync(lookupMsg.SecurityIds.Length, cancellationToken);

			foreach (var secId in lookupMsg.SecurityIds)
			{
				await writer.WriteAsync(FixTags.UnderlyingSymbol, cancellationToken);
				await writer.WriteAsync(secId.SecurityCode, cancellationToken);

				if (!secId.BoardCode.IsEmpty())
				{
					await writer.WriteAsync(FixTags.UnderlyingSecurityExchange, cancellationToken);
					await writer.WriteAsync(secId.BoardCode, cancellationToken);
				}
			}
		}

		if (!lookupMsg.StrategyId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.StrategyTypeId, cancellationToken);
			await writer.WriteAsync(lookupMsg.StrategyId, cancellationToken);
		}

		if (lookupMsg.Side != null)
		{
			await writer.WriteAsync(FixTags.Side, cancellationToken);
			await writer.WriteAsync(lookupMsg.Side.Value.ToFix(), cancellationToken);
		}

		if (lookupMsg.IsIncremental)
		{
			await writer.WriteAsync(FixTags.IsIncremental, cancellationToken);
			await writer.WriteAsync(true, cancellationToken);
		}

		if (!lookupMsg.UserId.IsEmpty())
		{
			await writer.WriteAsync(FixTags.RequestUserId, cancellationToken);
			await writer.WriteAsync(lookupMsg.UserId, cancellationToken);
		}

		return FixMessages.RequestForPositions;
	}

	private async IAsyncEnumerable<Message> ReadExecutionReportExAsync(IFixReader reader, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		OrderCondition condition = null;

		var report = new ExecutionReport();

		var isOk = await ReadExecutionReportAsync(reader, report, TimeStampParser, (tag, r1, r2, ct) =>
			tag switch
			{
				_ => ReadOrderConditionAsync(reader, tag, () => (condition ??= OrderConditionType.CreateOrderCondition()), ct),
			}, cancellationToken);

		if (!isOk)
			yield break;

		var result = ProcessExecutionReportAsync(report, (r, execMsg, ct) =>
		{
			if (condition != null)
				execMsg.Condition = condition;

			return ProcessExecutionReportAsync(r, execMsg, ct);
		}, cancellationToken);

		await foreach (var msg in result.WithEnforcedCancellation(cancellationToken))
		{
			yield return msg;
		}
	}
}