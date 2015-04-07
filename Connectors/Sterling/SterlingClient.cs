namespace StockSharp.Sterling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using SterlingLib;

	class SterlingClient
	{
		private readonly STIApp _stiApp = new STIApp();
		private readonly STIQuote _stiQuote = new STIQuote();
		private readonly STIEvents _stiEvents = new STIEvents();
		private readonly STIPosition _stiPosition = new STIPosition();
		private readonly STIAcctMaint _stiAcctMaint = new STIAcctMaint();
		private readonly STIOrderMaint _stiOrderMaint = new STIOrderMaint();

		public SterlingClient()
		{
			_stiApp.SetModeXML(false);
			_stiPosition.RegisterForPositions();
			TraderName = _stiApp.GetTraderName();
		}

		public string TraderName { get; private set; }

		#region STIEvents
		internal event _ISTIEventsEvents_OnSTITradeUpdateMsgEventHandler OnStiTradeUpdate
		{
			add { _stiEvents.OnSTITradeUpdateMsg += value; }
			remove { _stiEvents.OnSTITradeUpdateMsg -= value; }
		}

		public event _ISTIEventsEvents_OnSTIOrderUpdateMsgEventHandler OnStiOrderUpdate
		{
			add { _stiEvents.OnSTIOrderUpdateMsg += value; }
			remove { _stiEvents.OnSTIOrderUpdateMsg -= value; }
		}

		public event _ISTIEventsEvents_OnSTIOrderRejectMsgEventHandler OnStiOrderReject
		{
			add { _stiEvents.OnSTIOrderRejectMsg += value; }
			remove { _stiEvents.OnSTIOrderRejectMsg -= value; }
		}

		public event _ISTIEventsEvents_OnSTIOrderConfirmMsgEventHandler OnStiOrderConfirm
		{
			add { _stiEvents.OnSTIOrderConfirmMsg += value; }
			remove { _stiEvents.OnSTIOrderConfirmMsg -= value; }
		}

		public event _ISTIEventsEvents_OnSTIShutdownEventHandler OnStiShutdown
		{
			add { _stiEvents.OnSTIShutdown += value; }
			remove { _stiEvents.OnSTIShutdown -= value; }
		}
		#endregion

		#region STIQuote
		public event _ISTIQuoteEvents_OnSTIQuoteUpdateEventHandler OnStiQuoteUpdate
		{
			add { _stiQuote.OnSTIQuoteUpdate += value; }
			remove { _stiQuote.OnSTIQuoteUpdate -= value; }
		}

		public event _ISTIQuoteEvents_OnSTIQuoteSnapEventHandler OnStiQuoteSnap
		{
			add { _stiQuote.OnSTIQuoteSnap += value; }
			remove { _stiQuote.OnSTIQuoteSnap -= value; }
		}

		public event _ISTIQuoteEvents_OnSTIQuoteRqstEventHandler OnStiQuoteRqst
		{
			add { _stiQuote.OnSTIQuoteRqst += value; }
			remove { _stiQuote.OnSTIQuoteRqst -= value; }
		}

		public event _ISTIQuoteEvents_OnSTIL2UpdateEventHandler OnStil2Update
		{
			add { _stiQuote.OnSTIL2Update += value; }
			remove { _stiQuote.OnSTIL2Update -= value; }
		}

		public event _ISTIQuoteEvents_OnSTIL2ReplyEventHandler OnStil2Reply
		{
			add { _stiQuote.OnSTIL2Reply += value; }
			remove { _stiQuote.OnSTIL2Reply -= value; }
		}

		public event _ISTIQuoteEvents_OnSTIGreeksUpdateEventHandler OnStiGreeksUpdate
		{
			add { _stiQuote.OnSTIGreeksUpdate += value; }
			remove { _stiQuote.OnSTIGreeksUpdate -= value; }
		}

		public event _ISTIQuoteEvents_OnSTINewsUpdateEventHandler OnStiNewsUpdate;
		#endregion

		public event _ISTIAcctMaintEvents_OnSTIAcctUpdateEventHandler OnStiAcctUpdate
		{
			add { _stiAcctMaint.OnSTIAcctUpdate += value; }
			remove { _stiAcctMaint.OnSTIAcctUpdate -= value; }
		}

		public event _ISTIPositionEvents_OnSTIPositionUpdateEventHandler OnStiPositionUpdate
		{
			add { _stiPosition.OnSTIPositionUpdate += value; }
			remove { _stiPosition.OnSTIPositionUpdate -= value; }
		}

		public IEnumerable<structSTIAcctUpdate> GetPortfolios()
		{
			Array arrayAccts = null;
			_stiAcctMaint.GetAccountList(ref arrayAccts);

			return arrayAccts.Cast<string>().Select(a => _stiAcctMaint.GetAccountInfo(a));
		}

		public IEnumerable<structSTITradeUpdate> GetMyTrades()
		{
			Array arrayEquityTrade = null;
			_stiOrderMaint.GetEquityTradeList(ref arrayEquityTrade);

			Array arrayForexTrade = null;
			_stiOrderMaint.GetForexTradeList(ref arrayForexTrade);

			Array arrayFuturesTrade = null;
			_stiOrderMaint.GetFuturesTradeList(ref arrayFuturesTrade);

			Array arrayOptionsTrade = null;
			_stiOrderMaint.GetOptionsTradeList(ref arrayOptionsTrade);

			return new List<Array> { arrayEquityTrade, arrayForexTrade, arrayFuturesTrade, arrayOptionsTrade }.SelectMany(a => a.Cast<structSTITradeUpdate>());
		}

		public IEnumerable<structSTIOrderUpdate> GetOrders()
		{
			Array arrayOrder = null;
			_stiOrderMaint.GetOrderList(false, ref arrayOrder);

			return arrayOrder.Cast<structSTIOrderUpdate>();
		}

		public IEnumerable<structSTIPositionUpdate> GetPositions()
		{
			Array arrayPos = null;
			_stiPosition.GetPositionList(ref arrayPos);

			return arrayPos.Cast<structSTIPositionUpdate>();
		}

		public void SubscribeQuote(string symbol, string exch)
		{
			_stiQuote.RegisterQuote(symbol, "");
		}

		public void UnsubsribeQuote(string symbol, string exch)
		{
			_stiQuote.DeRegisterQuote(symbol, "");
		}

		public void UnsubscribeAllQuotes()
		{
			_stiQuote.DeRegisterAllQuotes();
		}

		public void SubscribeLevel2(string symbol, string exch)
		{
			var pL2 = new structSTIL2Reg // как использовать непонятно, методом тыка
			{
				bstrSymbol = symbol,
				bARCA = 1,
				bBATS = 1,
				bEDGA = 1,
				bEDGX = 1,
				bNasdaq = 1,
				bNyOpen = 1,
				bReg = 1
			};

			_stiQuote.RegisterL2(ref pL2);
		}

		public void UnsubsribeLevel2(string symbol, string exch)
		{
			var pL2 = new structSTIL2Reg // как использовать непонятно, методом тыка
			{
				bstrSymbol = symbol,
				bARCA = 1,
				bBATS = 1,
				bEDGA = 1,
				bEDGX = 1,
				bNasdaq = 1,
				bNyOpen = 1,
				bReg = 0
			};

			_stiQuote.RegisterL2(ref pL2);
		}

		public void SubscribeNews()
		{
			_stiQuote.OnSTINewsUpdate += OnStiNewsUpdate;
		}

		public void UnsubscribeNews()
		{
			_stiQuote.OnSTINewsUpdate -= OnStiNewsUpdate;
		}
	}
}