namespace StockSharp.Btce.Native
{
	using System;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Localization;

	class PusherClient : BaseLogReceiver
	{
		// to get readable name after obfuscation
		public override string Name => nameof(Btce) + "_" + nameof(PusherClient);

		public event Action<string, PusherTransaction[]> NewTrades;
		public event Action<string, OrderBook> OrderBookChanged;
		public event Action<Exception> Error;
		public event Action Connected;
		public event Action<bool> Disconnected;

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
						this.AddErrorLog(LocalizedStrings.ErrorConnection);

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
			this.AddInfoLog(LocalizedStrings.Connecting);
			_client.Connect("wss://ws-eu.pusher.com/app/ee987526a24ba107824c?client=stocksharp&version=1.0&protocol=7", false);
		}

		public void Disconnect()
		{
			this.AddInfoLog(LocalizedStrings.Disconnecting);
			_client.Disconnect();
		}

		private void OnProcess(dynamic obj)
		{
			var evt = (string)obj.@event;

			switch (evt)
			{
				case "pusher:connection_established":
					Connected?.Invoke();
					break;

				case "pusher_internal:subscription_succeeded":
					break;

				case "connection_established":
					break;

				case Channels.OrderBook:
					OrderBookChanged?.Invoke(((string)obj.channel).Remove("." + Channels.OrderBook), ((string)obj.data).DeserializeObject<OrderBook>());
					break;

				case Channels.Trades:
					NewTrades?.Invoke(((string)obj.channel).Remove("." + Channels.Trades), ((string)obj.data).DeserializeObject<PusherTransaction[]>());
					break;

				default:
					this.AddErrorLog(LocalizedStrings.UnknownEvent, evt);
					break;
			}
		}

		private static class Channels
		{
			public const string Trades = "trades";
			public const string OrderBook = "depth";
		}

		public void SubscribeTrades(string currency)
		{
			Process("subscribe", Channels.Trades, currency);
		}

		public void UnSubscribeTrades(string currency)
		{
			Process("unsubscribe", Channels.Trades, currency);
		}

		public void SubscribeOrderBook(string currency)
		{
			Process("subscribe", Channels.OrderBook, currency);
		}

		public void UnSubscribeOrderBook(string currency)
		{
			Process("unsubscribe", Channels.OrderBook, currency);
		}

		private void Process(string action, string channel, string currency)
		{
			if (action.IsEmpty())
				throw new ArgumentNullException(nameof(action));

			if (currency.IsEmpty())
				throw new ArgumentNullException(nameof(currency));

			if (channel.IsEmpty())
				throw new ArgumentNullException(nameof(channel));

			_client.Send(new
			{
				@event = "pusher:" + action,
				data = new
				{
					channel = currency + "." + channel,
				},
			});
		}
	}
}