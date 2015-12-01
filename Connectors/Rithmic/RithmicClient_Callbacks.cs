namespace StockSharp.Rithmic
{
	using System;

	using com.omnesys.rapi;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class RithmicClient
	{
		private class AdmCallbacksImpl : AdmCallbacks
		{
			private readonly RithmicClient _client;
			private readonly ILogReceiver _receiver;

			public AdmCallbacksImpl(RithmicClient client, ILogReceiver receiver)
			{
				if (client == null)
					throw new ArgumentNullException(nameof(client));

				if (receiver == null)
					throw new ArgumentNullException(nameof(receiver));

				_client = client;
				_receiver = receiver;
			}

			public override void Alert(AlertInfo info)
			{
				_client.Alert.WithDump(_receiver).SafeInvoke(info);
				//_sessionHolder.AddWarningLog("Received unexpected AdmCallbacks.Alert():\n{0}", oInfo.DumpableToString());
			}
		}

		private class RCallbacksImpl : RCallbacks
		{
			private readonly RithmicClient _client;
			private readonly ILogReceiver _receiver;

			public RCallbacksImpl(RithmicClient client, ILogReceiver receiver)
			{
				if (client == null)
					throw new ArgumentNullException(nameof(client));

				if (receiver == null)
					throw new ArgumentNullException(nameof(receiver));

				_client = client;
				_receiver = receiver;
			}

			private void MarketDataError(Exception ex)
			{
				_client.MarketDataError.SafeInvoke(ex);
			}

			private void TransactionError(Exception ex)
			{
				_client.TransactionError.SafeInvoke(ex);
			}

			#region connection

			public override void Alert(AlertInfo info)
			{
				_client.Alert.WithDump(_receiver).SafeInvoke(info);
			}

			public override void PasswordChange(PasswordChangeInfo info)
			{
				_client.PasswordChange.WithDump(_receiver).SafeInvoke(info);
			}

			#endregion

			#region orders

			public override void ExecutionReplay(ExecutionReplayInfo info)
			{
				_client.Execution.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void OpenOrderReplay(OrderReplayInfo info)
			{
				_client.OrderInfo.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void OrderReplay(OrderReplayInfo info)
			{
				_client.OrderInfo.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void LineUpdate(LineInfo info)
			{
				_client.OrderLineUpdate.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void BustReport(OrderBustReport report)
			{
				_client.OrderBust.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void CancelReport(OrderCancelReport report)
			{
				_client.OrderCancel.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void FailureReport(OrderFailureReport report)
			{
				_client.OrderFailure.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void FillReport(OrderFillReport report)
			{
				_client.OrderFill.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void ModifyReport(OrderModifyReport report)
			{
				_client.OrderModify.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void NotCancelledReport(OrderNotCancelledReport report)
			{
				_client.OrderCancelFailure.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void NotModifiedReport(OrderNotModifiedReport report)
			{
				_client.OrderModifyFailure.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void OtherReport(OrderReport report)
			{
				_client.OrderReport.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void RejectReport(OrderRejectReport report)
			{
				_client.OrderReject.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void StatusReport(OrderStatusReport report)
			{
				_client.OrderStatus.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			public override void SingleOrderReplay(SingleOrderReplayInfo info)
			{
				_client.OrderReplay.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void TradeRoute(TradeRouteInfo info)
			{
			}

			public override void TradeRouteList(TradeRouteListInfo info)
			{
			}

			public override void TradeCorrectReport(OrderTradeCorrectReport report)
			{
			}

			public override void TriggerPulledReport(OrderTriggerPulledReport report)
			{
			}

			public override void TriggerReport(OrderTriggerReport report)
			{
			}

			#endregion

			#region market data

			public override void BestAskQuote(AskInfo info)
			{
				_client.BestAskQuote.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void BestBidAskQuote(BidInfo oBid, AskInfo oAsk)
			{
			}

			public override void BestBidQuote(BidInfo info)
			{
				_client.BestBidQuote.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void AskQuote(AskInfo info)
			{
				_client.AskQuote.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void BidQuote(BidInfo info)
			{
				_client.BidQuote.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void EndQuote(EndQuoteInfo info)
			{
				_client.EndQuote.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void OpenPrice(OpenPriceInfo info)
			{
				_receiver.AddLog(LogLevels.Debug, info.DumpableToString);
				_client.Level1.SafeInvoke(info.Symbol, info.Exchange, Level1Fields.OpenPrice, (decimal)info.Price, RithmicUtils.ToTime(info.Ssboe, info.Usecs));
			}

			public override void HighPrice(HighPriceInfo info)
			{
				_receiver.AddLog(LogLevels.Debug, info.DumpableToString);
				_client.Level1.SafeInvoke(info.Symbol, info.Exchange, Level1Fields.HighPrice, (decimal)info.Price, RithmicUtils.ToTime(info.Ssboe, info.Usecs));
			}

			public override void LowPrice(LowPriceInfo info)
			{
				_receiver.AddLog(LogLevels.Debug, info.DumpableToString);
				_client.Level1.SafeInvoke(info.Symbol, info.Exchange, Level1Fields.LowPrice, (decimal)info.Price, RithmicUtils.ToTime(info.Ssboe, info.Usecs));
			}

			public override void ClosePrice(ClosePriceInfo info)
			{
				_receiver.AddLog(LogLevels.Debug, info.DumpableToString);
				_client.Level1.SafeInvoke(info.Symbol, info.Exchange, Level1Fields.ClosePrice, (decimal)info.Price, RithmicUtils.ToTime(info.Ssboe, info.Usecs));
			}

			public override void OpenInterest(OpenInterestInfo info)
			{
				if (!info.QuantityFlag)
					return;

				_client.Level1.SafeInvoke(info.Symbol, info.Exchange, Level1Fields.OpenInterest, info.Quantity, RithmicUtils.ToTime(info.Ssboe, info.Usecs));
			}

			public override void LimitOrderBook(OrderBookInfo info)
			{
				_client.OrderBook.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void SettlementPrice(SettlementPriceInfo info)
			{
				_client.SettlementPrice.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TradeCondition(TradeInfo info)
			{
				_client.TradeCondition.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TradePrint(TradeInfo info)
			{
				_client.TradePrint.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TradeVolume(TradeVolumeInfo info)
			{
				_client.TradeVolume.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TradeReplay(TradeReplayInfo info)
			{
				_client.TradeReplay.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TimeBar(TimeBarInfo info)
			{
				_client.TimeBar.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void TimeBarReplay(TimeBarReplayInfo info)
			{
				_client.TimeBarReplay.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			#endregion

			#region portfolio info

			public override void AccountList(AccountListInfo info)
			{
				_client.Accounts.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void PnlUpdate(PnlInfo info)
			{
				_client.AccountPnLUpdate.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void PnlReplay(PnlReplayInfo info)
			{
				_client.AccountPnL.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void ProductRmsList(ProductRmsListInfo info)
			{
				_client.AccountRms.WithDump(_receiver).WithError(TransactionError).SafeInvoke(info);
			}

			public override void SodUpdate(SodReport report)
			{
				_client.AccountSodUpdate.WithDump(_receiver).WithError(TransactionError).SafeInvoke(report);
			}

			#endregion

			#region security info

			public override void BinaryContractList(BinaryContractListInfo info)
			{
				_client.SecurityBinaryContracts.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void OptionList(OptionListInfo info)
			{
				_client.SecurityOptions.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void InstrumentByUnderlying(InstrumentByUnderlyingInfo info)
			{
				_client.SecurityInstrumentByUnderlying.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void PriceIncrUpdate(PriceIncrInfo info)
			{
			}

			public override void RefData(RefDataInfo info)
			{
				_client.SecurityRefData.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			public override void MarketMode(MarketModeInfo info)
			{
			}

			public override void ExchangeList(ExchangeListInfo info)
			{
				_client.Exchanges.WithDump(_receiver).WithError(MarketDataError).SafeInvoke(info);
			}

			#endregion

			#region other

			public override void ClosingIndicator(ClosingIndicatorInfo info)
			{
			}

			public override void OpeningIndicator(OpeningIndicatorInfo info)
			{
			}

			public override void EquityOptionStrategyList(EquityOptionStrategyListInfo info)
			{
			}

			public override void Strategy(StrategyInfo info)
			{
			}

			public override void StrategyList(StrategyListInfo info)
			{
			}

			#endregion
		}
	}
}