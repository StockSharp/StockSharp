#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.InteractiveBrokers
File: IBTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	static class ExtendedMarketDataTypes
	{
		public const MarketDataTypes Scanner = (MarketDataTypes)(-1);
		public const MarketDataTypes FundamentalReport = (MarketDataTypes)(-2);
		public const MarketDataTypes OptionCalc = (MarketDataTypes)(-3);
	}

	static class ExtendedMessageTypes
	{
		public const MessageTypes Scanner = (MessageTypes)(-1);
		public const MessageTypes FundamentalReport = (MessageTypes)(-2);
		public const MessageTypes ScannerParameters = (MessageTypes)(-3);
		public const MessageTypes FinancialAdvise = (MessageTypes)(-4);
	}

	/// <summary>
	/// The message about start of the instruments scanner based on specified parameters. The results will come through the <see cref="IBTrader.NewScannerResults"/> event.
	/// </summary>
	public class ScannerMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Filter.
		/// </summary>
		public ScannerFilter Filter { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ScannerMarketDataMessage"/>.
		/// </summary>
		/// <param name="filter">Filter.</param>
		public ScannerMarketDataMessage(ScannerFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			Filter = filter;
			DataType = ExtendedMarketDataTypes.Scanner;
		}
	}

	/// <summary>
	/// The message with the results of scanner starting by the message <see cref="ScannerMarketDataMessage"/>.
	/// </summary>
	public class ScannerResultMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScannerResultMessage"/>.
		/// </summary>
		public ScannerResultMessage()
			: base(ExtendedMessageTypes.Scanner)
		{
		}

		/// <summary>
		/// The results.
		/// </summary>
		public IEnumerable<ScannerResult> Results { get; set; }

		/// <summary>
		/// The query identifier <see cref="ScannerMarketDataMessage"/>.
		/// </summary>
		public long OriginalTransactionId { get; set; }
	}

	/// <summary>
	/// The message with scanner parameters.
	/// </summary>
	public class ScannerParametersMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScannerParametersMessage"/>.
		/// </summary>
		public ScannerParametersMessage()
			: base(ExtendedMessageTypes.ScannerParameters)
		{
		}

		/// <summary>
		/// The parameters in the xml format.
		/// </summary>
		public string Parameters { get; set; }
	}

	/// <summary>
	/// The messgae with financial advice.
	/// </summary>
	public class FinancialAdviseMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FinancialAdviseMessage"/>.
		/// </summary>
		public FinancialAdviseMessage()
			: base(ExtendedMessageTypes.FinancialAdvise)
		{
		}

		/// <summary>
		/// Type.
		/// </summary>
		public int AdviseType { get; set; }

		/// <summary>
		/// Data in the xml format.
		/// </summary>
		public string Data { get; set; }
	}

	/// <summary>
	/// The message to receive market reports for the specified instrument. The results will come through the <see cref="IBTrader.NewFundamentalReport"/> event.
	/// </summary>
	public class FundamentalReportMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// The report type.
		/// </summary>
		public FundamentalReports Report { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FundamentalReportMarketDataMessage"/>.
		/// </summary>
		/// <param name="report">The report type.</param>
		public FundamentalReportMarketDataMessage(FundamentalReports report)
		{
			Report = report;
			DataType = ExtendedMarketDataTypes.FundamentalReport;
		}
	}

	/// <summary>
	/// The message with the market report initiated by the message <see cref="FundamentalReportMarketDataMessage"/>.
	/// </summary>
	public class FundamentalReportMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FundamentalReportMessage"/>.
		/// </summary>
		public FundamentalReportMessage()
			: base(ExtendedMessageTypes.FundamentalReport)
		{
		}

		/// <summary>
		/// Text of report.
		/// </summary>
		public string Data { get; set; }

		/// <summary>
		/// The query identifier <see cref="FundamentalReportMarketDataMessage"/>.
		/// </summary>
		public long OriginalTransactionId { get; set; }
	}

	/// <summary>
	/// The message about subscription to the estimated option values getting.
	/// </summary>
	public class OptionCalcMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OptionCalcMarketDataMessage"/>.
		/// </summary>
		/// <param name="impliedVolatility">The implied volatility.</param>
		/// <param name="optionPrice">The option price.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		public OptionCalcMarketDataMessage(decimal impliedVolatility, decimal optionPrice, decimal assetPrice)
		{
			AssetPrice = assetPrice;
			OptionPrice = optionPrice;
			ImpliedVolatility = impliedVolatility;

			DataType = ExtendedMarketDataTypes.OptionCalc;
		}

		/// <summary>
		/// The implied volatility.
		/// </summary>
		public decimal ImpliedVolatility { get; private set; }

		/// <summary>
		/// The option price.
		/// </summary>
		public decimal OptionPrice { get; private set; }

		/// <summary>
		/// Underlying asset price.
		/// </summary>
		public decimal AssetPrice { get; private set; }
	}

	/// <summary>
	/// The implementation of the <see cref="IConnector"/> interface which provides a connection to Interactive Brokers via the IB Gateway.
	/// </summary>
	[Icon("InteractiveBrokers_logo.png")]
	public class IBTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();
		private readonly SynchronizedDictionary<long, object> _states = new SynchronizedDictionary<long, object>();

		/// <summary>
		/// Initializes a new instance of the <see cref="IBTrader"/>.
		/// </summary>
		public IBTrader()
		{
			CreateAssociatedSecurity = true;

			Adapter.InnerAdapters.Add(new InteractiveBrokersMessageAdapter(TransactionIdGenerator));
		}

		private InteractiveBrokersMessageAdapter NativeAdapter
		{
			get { return Adapter.InnerAdapters.OfType<InteractiveBrokersMessageAdapter>().First(); }
		}

		/// <summary>
		/// Address.
		/// </summary>
		public EndPoint Address
		{
			get { return NativeAdapter.Address; }
			set { NativeAdapter.Address = value; }
		}

		/// <summary>
		/// Unique ID. Used when several clients are connected to one terminal or gateway.
		/// </summary>
		public int ClientId
		{
			get { return NativeAdapter.ClientId; }
			set { NativeAdapter.ClientId = value; }
		}

		/// <summary>
		/// The server messages logging level. The default is <see cref="ServerLogLevels.Detail"/>.
		/// </summary>
		public ServerLogLevels ServerLogLevel
		{
			get { return NativeAdapter.ServerLogLevel; }
			set { NativeAdapter.ServerLogLevel = value; }
		}

		/// <summary>
		/// Whether to use real-time data or 'frozen' on the broker server. By default, the 'frozen' data is used.
		/// </summary>
		public bool IsRealTimeMarketData
		{
			get { return NativeAdapter.IsRealTimeMarketData; }
			set { NativeAdapter.IsRealTimeMarketData = value; }
		}

		/// <summary>
		/// The new results occurring event of the scanner started previously via <see cref="SubscribeScanner"/>.
		/// </summary>
		public event Action<ScannerFilter, IEnumerable<ScannerResult>> NewScannerResults;

		/// <summary>
		/// Event of new candles occurring, that are received after the subscription by <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// The new report occurring event obtained by subscription <see cref="SubscribeFundamentalReport"/>.
		/// </summary>
		public event Action<Security, FundamentalReports, string> NewFundamentalReport;

		/// <summary>
		/// The event of occurring of new scanner parameters which are applied via <see cref="ScannerFilter"/>. Parameters are passed in the xml format.
		/// </summary>
		public event Action<string> NewScannerParameters;

		/// <summary>
		/// The new financial advice occurring event. Parameters are passed in the xml format.
		/// </summary>
		public event Action<int, string> NewFinancialAdvise;

		///// <summary>
		///// Событие о появлении отчета о комиссии по сделке.
		///// </summary>
		//public event Action<IBCommission> NewCommission;

		/// <summary>
		/// Cancel orders by filter.
		/// </summary>
		/// <param name="transactionId">Order cancellation transaction id.</param>
		/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
		/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
		/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
		/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
		/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
		protected override void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (isStopOrder == null && portfolio == null && direction == null && board == null && security == null)
				base.OnCancelOrders(transactionId);
			else
				this.CancelOrders(Orders, isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// To start or stop the instruments scanner based on specified parameters. The results will come through the <see cref="IBTrader.NewScannerResults"/> event.
		/// </summary>
		/// <param name="filter">Filter.</param>
		/// <param name="isSubscribe"><see langword="true" /> if you need to subscribe, otherwise <see langword="false" />.</param>
		public void SubscribeScanner(ScannerFilter filter, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_states.Add(transactionId, filter);

			SendInMessage(new ScannerMarketDataMessage(filter)
			{
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			});
		}

		/// <summary>
		/// To subscribe or unsubscribe to receive market reports for the specified instrument. The results will come through the <see cref="IBTrader.NewFundamentalReport"/> event.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="report">The report type.</param>
		/// <param name="isSubscribe"><see langword="true" /> if you need to subscribe, otherwise <see langword="false" />.</param>
		public void SubscribeFundamentalReport(Security security, FundamentalReports report, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_states.Add(transactionId, Tuple.Create(security, report));

			SendInMessage(new FundamentalReportMarketDataMessage(report)
			{
				//SecurityId = GetSecurityId(security),
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// To subscribe or unsubscribe to receive the estimated option values. The results will come through the <see cref="IConnector.SecuritiesChanged"/> event.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="impliedVolatility">The implied volatility.</param>
		/// <param name="optionPrice">The option price.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="isSubscribe"><see langword="true" /> if you need to subscribe, otherwise <see langword="false" />.</param>
		public void SubscribeOptionCalc(Security security, decimal impliedVolatility, decimal optionPrice, decimal assetPrice, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			SendInMessage(new OptionCalcMarketDataMessage(impliedVolatility, optionPrice, assetPrice)
			{
				//SecurityId = GetSecurityId(security),
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) &&
				series.Arg is TimeSpan &&
				IBTimeFrames.CanConvert((TimeSpan)series.Arg))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_candleSeries.Add(transactionId, series);

			SendInMessage(new MarketDataMessage
			{
				TransactionId = transactionId,
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = true,
				From = from,
				To = to,
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = false,
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case ExtendedMessageTypes.Scanner:
				{
					var scannerMsg = (ScannerResultMessage)message;
					var state = (ScannerFilter)_states[scannerMsg.OriginalTransactionId];
					NewScannerResults.SafeInvoke(state, scannerMsg.Results);

					break;
				}
				case ExtendedMessageTypes.ScannerParameters:
				{
					var scannerMsg = (ScannerParametersMessage)message;
					NewScannerParameters.SafeInvoke(scannerMsg.Parameters);

					break;
				}
				case ExtendedMessageTypes.FinancialAdvise:
				{
					var adviseMsg = (FinancialAdviseMessage)message;
					NewFinancialAdvise.SafeInvoke(adviseMsg.AdviseType, adviseMsg.Data);

					break;
				}
				case ExtendedMessageTypes.FundamentalReport:
				{
					var reportMsg = (FundamentalReportMessage)message;
					var state = (Tuple<Security, FundamentalReports>)_states[reportMsg.OriginalTransactionId];
					NewFundamentalReport.SafeInvoke(state.Item1, state.Item2, reportMsg.Data);

					break;
				}
				default:
				{
					var candleMsg = message as CandleMessage;

					if (candleMsg == null)
						break;

					var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

					if (series != null)
					{
						var candle = candleMsg.ToCandle(series);
						NewCandles.SafeInvoke(series, new[] { candle });

						if (candleMsg.IsFinished)
							Stopped.SafeInvoke(series);
					}

					return;
				}
			}

			base.OnProcessMessage(message);
		}
	}
}