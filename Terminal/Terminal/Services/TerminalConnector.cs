using System;
using System.Collections.Generic;
using Ecng.Collections;
using MoreLinq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Terminal.Services {
	class TerminalConnector : Connector {

		readonly SynchronizedSet<CandleSeries> _subscribersBuffer = new CachedSynchronizedSet<CandleSeries>();  
		readonly SynchronizedDictionary<SubscriptionKey, CandleSubscription> _candleSubscriptions = new SynchronizedDictionary<SubscriptionKey, CandleSubscription>();

		public event Action<CandleSeries, IEnumerable<TimeFrameCandle>> Candles;

		bool IsConnected => ConnectionState == ConnectionStates.Connected;

		public TerminalConnector(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry) : base(entityRegistry, storageRegistry)
		{
			Connected += OnConnected;
			StorageAdapter.Format = StorageFormats.Csv;
		}

		private void OnConnected()
		{
			var subscribers = _subscribersBuffer.CopyAndClear();
			subscribers.ForEach(SubscribeCandles);
		}

		public void SubscribeCandles(CandleSeries series)
		{
			if (!IsConnected)
			{
				_subscribersBuffer.Add(series);
				return;
			}

			var key = new SubscriptionKey(series.Security.Id, (TimeSpan) series.Arg);
			var transId = TransactionIdGenerator.GetNextId();
			
			var subscription = _candleSubscriptions.TryGetValue(key);

			if (subscription != null)
			{
				Candles?.Invoke(series, subscription.CandleBuilder.AllCandles);
				subscription.AddSubscriber(series);

				return;
			}

			subscription = new CandleSubscription(series.Security, series.From, (TimeSpan) series.Arg, transId);
			subscription.AddSubscriber(series);
			_candleSubscriptions.Add(key, subscription);

			subscription.CandleBuilder.Candle += CandleBuilderOnCandle;

			var msg = new MarketDataMessage().FillSecurityInfo(this, series.Security);
			msg.TransactionId = transId;
			msg.SecurityId = GetSecurityId(series.Security);
			msg.DataType = MarketDataTypes.CandleTimeFrame;
			msg.IsSubscribe = true;
			msg.From = series.From;
			msg.Arg = series.Arg;

			SendInMessage(msg);

			SubscribeMarketData(series.Security, MarketDataTypes.CandleTimeFrame);
		}

		public void UnsubscribeCandles(CandleSeries series)
		{
			var key = new SubscriptionKey(series.Security.Id, (TimeSpan) series.Arg);
			var subscription = _candleSubscriptions.TryGetValue(key);
			if(subscription == null)
				return;

			subscription.RemoveSubscriber(series);
			if(subscription.NumSubscribers > 0)
				return;

			subscription.CandleBuilder.Candle -= CandleBuilderOnCandle;
			_candleSubscriptions.Remove(key);

			var msg = new MarketDataMessage().FillSecurityInfo(this, series.Security);
			msg.TransactionId = TransactionIdGenerator.GetNextId();
			msg.OriginalTransactionId = subscription.SubscribeTransactionId;
			msg.SecurityId = GetSecurityId(series.Security);
			msg.DataType = MarketDataTypes.CandleTimeFrame;
			msg.IsSubscribe = false;
			msg.From = subscription.From;
			msg.Arg = subscription.TimeFrame;

			SendInMessage(msg);

			UnSubscribeMarketData(series.Security, MarketDataTypes.CandleTimeFrame);
		}

		private void CandleBuilderOnCandle(TimeFrameCandle candle)
		{
			var sub = _candleSubscriptions.TryGetValue(new SubscriptionKey(candle.Security.Id, (TimeSpan) candle.Arg));
			var candles = new [] {candle};

			sub?.Subscribers.ForEach(s => Candles?.Invoke(s, candles));
		}

		protected override void OnProcessMessage(Message message)
		{
			switch(message.Type)
			{
				case MessageTypes.CandleTimeFrame:
					var msg = (TimeFrameCandleMessage)message;
					var key = new SubscriptionKey(msg.SecurityId.ToStringId(), msg.TimeFrame);

					var subscription = _candleSubscriptions.TryGetValue(key);
					if(subscription == null)
						return;

					subscription.CandleBuilder.ProcessMessage(msg);

					break;
			}

			base.OnProcessMessage(message);
		}

		class CandleSubscription
		{
			private readonly HashSet<CandleSeries> _subscribers = new HashSet<CandleSeries>();

			public long SubscribeTransactionId {get;}
			public SubscriptionKey Key {get;}
			public Security Security {get;}
			public DateTimeOffset From {get;}
			public TimeSpan TimeFrame {get;}
			public TerminalCandleBuilder CandleBuilder {get;}
			public int NumSubscribers => _subscribers.Count;

			public IEnumerable<CandleSeries> Subscribers => _subscribers; 

			public CandleSubscription(Security sec, DateTimeOffset from, TimeSpan tf, long transId)
			{
				Security = sec;
				From = from;
				TimeFrame = tf;
				Key = new SubscriptionKey(sec.Id, tf);
				SubscribeTransactionId = transId;
				CandleBuilder = new TerminalCandleBuilder(Security, TimeFrame);
			}

			public void AddSubscriber(CandleSeries series)
			{
				_subscribers.Add(series);
			}

			public void RemoveSubscriber(CandleSeries series)
			{
				_subscribers.Remove(series);
			}
		}

		struct SubscriptionKey
		{
			public string SecurityId {get;}
			public TimeSpan TimeFrame {get;}

			public SubscriptionKey(string secId, TimeSpan timeframe)
			{
				SecurityId = secId;
				TimeFrame = timeframe;
			}
		}
	}
}
