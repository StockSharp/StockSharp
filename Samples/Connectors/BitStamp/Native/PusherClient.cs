#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: PusherClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Common;
	using Ecng.Net;

	using Newtonsoft.Json.Linq;

	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.BitStamp.Native.Model;

	class PusherClient : BaseLogReceiver
	{
		public event Action<string, Trade> NewTrade;
		public event Action<string, OrderBook> NewOrderBook;
		public event Action<string, OrderStates, Order> NewOrderLog;
		public event Action<Exception> Error;
		public event Action Connected;
		public event Action<bool> Disconnected;
		public event Action<string> TradesSubscribed;
		public event Action<string> OrderBookSubscribed;
		public event Action<string> OrderLogSubscribed;
		public event Action<string> TradesUnSubscribed;
		public event Action<string> OrderBookUnSubscribed;
		public event Action<string> OrderLogUnSubscribed;

		private int _activityTimeout;
		private DateTime? _nextPing;

		private readonly WebSocketClient _client;

		public PusherClient()
		{
			_client = new WebSocketClient(
				() =>
				{
					this.AddInfoLog(LocalizedStrings.Connected);
					Connected?.Invoke();
				},
				expected =>
				{
					if (expected)
						this.AddInfoLog(LocalizedStrings.Disconnected);
					else
						this.AddErrorLog(LocalizedStrings.Str2959);

					Disconnected?.Invoke(expected);
				},
				error =>
				{
					this.AddErrorLog(error);
					Error?.Invoke(error);
				},
				OnProcess,
				(s, a) => this.AddInfoLog(s, a),
				(s, a) => this.AddErrorLog(s, a),
				(s, a) => this.AddVerboseLog(s, a),
				(s) => this.AddVerboseLog(s));
		}

		protected override void DisposeManaged()
		{
			_client.Dispose();
			base.DisposeManaged();
		}

		public void Connect()
		{
			_nextPing = null;
			_activityTimeout = 0;

			this.AddInfoLog(LocalizedStrings.Connecting);
			_client.Connect("wss://ws.bitstamp.net", true);
		}

		public void Disconnect()
		{
			this.AddInfoLog(LocalizedStrings.Disconnecting);
			_client.Disconnect();
		}

		private void OnProcess(dynamic obj)
		{
			var channel = (string)obj.channel;
			var evt = (string)obj.@event;
			var data = obj.data;

			//if (data != null && evt != "bts:error")
			//	data = ((string)data).DeserializeObject<object>();

			switch (evt)
			{
				//case "pusher:connection_established":
				//	_activityTimeout = (int)data.activity_timeout;
				//	_nextPing = DateTime.UtcNow.AddSeconds(_activityTimeout);
				//	Connected?.Invoke();
				//	break;

				case "bts:error":
					Error?.Invoke(new InvalidOperationException((string)data.message));
					break;

				case "ping":
					SendPingPong("pong");
					break;

				case "pong":
					break;

				case "bts:subscription_succeeded":
				{
					if (channel.StartsWith(ChannelNames.OrderBook))
						OrderBookSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderBook));
					else if (channel.StartsWith(ChannelNames.Trades))
						TradesSubscribed?.Invoke(GetPair(channel, ChannelNames.Trades));
					else if (channel.StartsWith(ChannelNames.OrderLog))
						OrderLogSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderLog));
					else
						this.AddErrorLog(LocalizedStrings.Str3311Params, channel);

					break;
				}

				case "bts:unsubscription_succeeded":
				{
					if (channel.StartsWith(ChannelNames.OrderBook))
						OrderBookUnSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderBook));
					else if (channel.StartsWith(ChannelNames.Trades))
						TradesUnSubscribed?.Invoke(GetPair(channel, ChannelNames.Trades));
					else if (channel.StartsWith(ChannelNames.OrderLog))
						OrderLogUnSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderLog));
					else
						this.AddErrorLog(LocalizedStrings.Str3311Params, channel);

					break;
				}

				case "trade":
					NewTrade?.Invoke(GetPair(channel, ChannelNames.Trades), ((JToken)data).DeserializeObject<Trade>());
					break;

				case "data":
					NewOrderBook?.Invoke(GetPair(channel, ChannelNames.OrderBook), ((JToken)data).DeserializeObject<OrderBook>());
					break;

				case "order_created":
				case "order_changed":
				case "order_deleted":
					NewOrderLog?.Invoke(GetPair(channel, ChannelNames.OrderLog), evt == "order_deleted" ? OrderStates.Done : OrderStates.Active, ((JToken)data).DeserializeObject<Order>());
					break;

				default:
					this.AddErrorLog(LocalizedStrings.Str3312Params, evt);
					break;
			}
		}

		private static string GetPair(string channel, string name)
		{
			channel = channel.Remove(name).Remove("_");

			if (channel.IsEmpty())
				channel = "btcusd";

			return channel;
		}

		private static class ChannelNames
		{
			public const string Trades = "live_trades_";
			public const string OrderBook = "order_book_";
			public const string OrderLog = "live_orders_";	
		}

		private static class Commands
		{
			public const string Subscribe = "subscribe";
			public const string UnSubscribe = "unsubscribe";
		}

		public void SubscribeTrades(string currency)
		{
			Process(Commands.Subscribe, ChannelNames.Trades + currency);
		}

		public void UnSubscribeTrades(string currency)
		{
			Process(Commands.UnSubscribe, ChannelNames.Trades + currency);
		}

		public void SubscribeOrderBook(string currency)
		{
			Process(Commands.Subscribe, ChannelNames.OrderBook + currency);
		}

		public void UnSubscribeOrderBook(string currency)
		{
			Process(Commands.UnSubscribe, ChannelNames.OrderBook + currency);
		}

		public void SubscribeOrderLog(string currency)
		{
			Process(Commands.Subscribe, ChannelNames.OrderLog + currency);
		}

		public void UnSubscribeOrderLog(string currency)
		{
			Process(Commands.UnSubscribe, ChannelNames.OrderLog + currency);
		}

		private void Process(string action, string channel)
		{
			if (action.IsEmpty())
				throw new ArgumentNullException(nameof(action));

			if (channel.IsEmpty())
				throw new ArgumentNullException(nameof(channel));

			_client.Send(new
			{
				@event = $"bts:{action}",
				data = new { channel }
			});
		}

		public void ProcessPing()
		{
			if (_nextPing == null || DateTime.UtcNow < _nextPing.Value)
				return;

			SendPingPong("ping");
		}

		private void SendPingPong(string action)
		{
			_client.Send(new { @event = $"{action}" });

			if (_activityTimeout > 0)
				_nextPing = DateTime.UtcNow.AddSeconds(_activityTimeout);
		}
	}
}