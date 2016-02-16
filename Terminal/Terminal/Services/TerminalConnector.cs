using System;
using Ecng.Collections;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.Messages;

namespace StockSharp.Terminal.Services {
	class TerminalConnector : Connector {

		readonly SynchronizedDictionary<string, Tuple<DateTimeOffset, long>> _subscriptionDates = new SynchronizedDictionary<string, Tuple<DateTimeOffset, long>>();

		// todo rework
		readonly SynchronizedDictionary<Tuple<SecurityId, object>, TimeFrameCandle> _lastCandles = new SynchronizedDictionary<Tuple<SecurityId, object>, TimeFrameCandle>();
		public event Action<TimeFrameCandle> Candle;

		public TerminalConnector()
		{
			
		}

		public TerminalConnector(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry) : base(entityRegistry, storageRegistry)
		{
			
		}

		public void SubscribeCandles(CandleSeries series)
		{
			var transId = TransactionIdGenerator.GetNextId();
			
			_subscriptionDates[series.Security.Id] = Tuple.Create(series.From, transId);

			var msg = new MarketDataMessage().FillSecurityInfo(this, series.Security);
			msg.TransactionId = transId;
			msg.SecurityId = GetSecurityId(series.Security);
			msg.DataType = MarketDataTypes.CandleTimeFrame;
			msg.IsSubscribe = true;
			msg.From = series.From;
			msg.Arg = (TimeSpan)series.Arg;

			_lastCandles.Remove(Tuple.Create(msg.SecurityId, msg.Arg));

			SendInMessage(msg);

			SubscribeMarketData(series.Security, MarketDataTypes.CandleTimeFrame);
		}

		public void UnsubscribeCandles(CandleSeries series)
		{
			var tuple = _subscriptionDates.TryGetValue(series.Security.Id);
			if(tuple == null)
				return;

			var msg = new MarketDataMessage().FillSecurityInfo(this, series.Security);
			msg.TransactionId = TransactionIdGenerator.GetNextId();
			msg.OriginalTransactionId = tuple.Item2;
			msg.SecurityId = GetSecurityId(series.Security);
			msg.DataType = MarketDataTypes.CandleTimeFrame;
			msg.IsSubscribe = false;
			msg.From = tuple.Item1;
			msg.Arg = series.Arg;

			SendInMessage(msg);

			UnSubscribeMarketData(series.Security, MarketDataTypes.CandleTimeFrame);
		}

		protected override void OnProcessMessage(Message message)
		{
			switch(message.Type)
			{
				case MessageTypes.CandleTimeFrame:
					Candle?.Invoke(GetCandle((TimeFrameCandleMessage)message));

					break;
			}

			base.OnProcessMessage(message);
		}

		TimeFrameCandle GetCandle(TimeFrameCandleMessage msg)
		{
			var key = Tuple.Create(msg.SecurityId, msg.Arg);
			
			var candle = _lastCandles.TryGetValue(key);
			if (candle == null)
			{
				candle = NewCandleFromMessage(msg);
			}
			else if (candle.OpenTime != msg.OpenTime)
			{
				candle.State = CandleStates.Finished;
				Candle?.Invoke(candle);
				candle = NewCandleFromMessage(msg);
			}

			_lastCandles[key] = candle;

			candle.OpenPrice = msg.OpenPrice;
			candle.HighPrice = msg.HighPrice;
			candle.LowPrice = msg.LowPrice;
			candle.ClosePrice = msg.ClosePrice;
			candle.TotalVolume = msg.TotalVolume;
			candle.State = msg.State;

			return candle;
		}

		TimeFrameCandle NewCandleFromMessage(TimeFrameCandleMessage msg)
		{
			return new TimeFrameCandle
			{
				Security = GetSecurity(msg.SecurityId),
				OpenTime = msg.OpenTime,
				CloseTime = msg.CloseTime,
				OpenPrice = msg.OpenPrice,
				HighPrice = msg.HighPrice,
				LowPrice = msg.LowPrice,
				ClosePrice = msg.ClosePrice,
				TotalVolume = msg.TotalVolume,
				State = msg.State,
			};
		}
	}
}
