#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicClient.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.IO;

	using com.omnesys.rapi;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class RithmicClient
	{
		public RithmicClient(ILogReceiver receiver, string adminConnectionPoint, string certFile, string domainServerAddress, string domainName, string licenseServerAddress, string localBrokerAddress, string loggerAddress, string logFileName)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			if (!File.Exists(certFile))
				throw new FileNotFoundException(LocalizedStrings.Str3457Params.Put(certFile));

			Callbacks = new RCallbacksImpl(this, receiver);

			var eParams = new REngineParams
			{
				AdmCnnctPt = adminConnectionPoint,
				AdmCallbacks = new AdmCallbacksImpl(this, receiver),
				AppName = "StockSharp",
				AppVersion = GetType().Assembly.GetName().Version.ToString(),
				CertFile = certFile,
				DmnSrvrAddr = domainServerAddress,
				DomainName = domainName,
				LicSrvrAddr = licenseServerAddress,
				LocBrokAddr = localBrokerAddress,
				LoggerAddr = loggerAddress,
				LogFilePath = logFileName,
			};

			Session = new REngine(eParams);
		}

		public RCallbacks Callbacks { get; private set; }
		public REngine Session { get; private set; }
		
		public event Action<Exception> MarketDataError;
		public event Action<Exception> TransactionError;

		public event Action<AlertInfo> Alert;

		public event Action<OrderReplayInfo> OrderInfo;
		public event Action<OrderCancelReport> OrderCancel;
		public event Action<OrderFailureReport> OrderFailure;
		public event Action<OrderFillReport> OrderFill;
		public event Action<OrderModifyReport> OrderModify;
		public event Action<OrderNotCancelledReport> OrderCancelFailure;
		public event Action<OrderNotModifiedReport> OrderModifyFailure;
		public event Action<OrderRejectReport> OrderReject;
		public event Action<OrderStatusReport> OrderStatus;
		public event Action<SingleOrderReplayInfo> OrderReplay;
		public event Action<LineInfo> OrderLineUpdate;
		public event Action<OrderBustReport> OrderBust;
		public event Action<OrderReport> OrderReport;

		public event Action<ExecutionReplayInfo> Execution;

		public event Action<AccountListInfo> Accounts;
		public event Action<PnlInfo> AccountPnLUpdate;
		public event Action<PnlReplayInfo> AccountPnL;
		public event Action<ProductRmsListInfo> AccountRms;
		public event Action<SodReport> AccountSodUpdate;

		public event Action<BinaryContractListInfo> SecurityBinaryContracts;
		public event Action<OptionListInfo> SecurityOptions;
		public event Action<InstrumentByUnderlyingInfo> SecurityInstrumentByUnderlying;
		public event Action<RefDataInfo> SecurityRefData;
		public event Action<ExchangeListInfo> Exchanges;

		public event Action<AskInfo> BestAskQuote;
		public event Action<BidInfo> BestBidQuote;
		public event Action<AskInfo> AskQuote;
		public event Action<BidInfo> BidQuote;
		public event Action<EndQuoteInfo> EndQuote;
		public event Action<string, string, Level1Fields, decimal, DateTimeOffset> Level1;
		public event Action<OrderBookInfo> OrderBook;
		public event Action<SettlementPriceInfo> SettlementPrice;
		public event Action<TradeInfo> TradeCondition;
		public event Action<TradeInfo> TradePrint;
		public event Action<TradeVolumeInfo> TradeVolume;
		public event Action<TradeReplayInfo> TradeReplay;
		public event Action<TimeBarInfo> TimeBar;
		public event Action<TimeBarReplayInfo> TimeBarReplay;

		public event Action<PasswordChangeInfo> PasswordChange;
	}
}