namespace StockSharp.BitStamp.Native
{
	using System;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;

	using Ecng.Common;

	using Newtonsoft.Json;

	using StockSharp.Logging;
	using StockSharp.Localization;

	class PusherClient : BaseLogReceiver
	{
		private class ChannelData
		{
			[JsonProperty("event")]
			public string Event { get; set; }

			[JsonProperty("data")]
			public string Data { get; set; }

			[JsonProperty("channel")]
			public string Channel { get; set; }
		}

		public event Action<Trade> NewTrade;
		public event Action<OrderBook> NewOrderBook;
		public event Action<Exception> PusherError;
		public event Action PusherConnected;
		public event Action PusherDisconnected;
		public event Action TradesSubscribed;
		public event Action OrderBooksSubscribed;

		private ClientWebSocket _ws;
		private CancellationTokenSource _source = new CancellationTokenSource();
		private bool _connected;

		public void ConnectPusher()
		{
			_ws = new ClientWebSocket();
			_ws.ConnectAsync(new Uri("wss://ws.pusherapp.com/app/de504dc5763aeef9ff52?client=PC-Orbit&version=1.0&protocol=7"), _source.Token).Wait();
			
			_connected = true;

			ThreadingHelper.Thread(OnReceive).Launch();
		}

		public void DisconnectPusher()
		{
			_connected = false;
			_source.Cancel();
			_source = new CancellationTokenSource();
		}

		private void OnReceive()
		{
			try
			{
				var buf = new byte[1024 * 1024];
				var pos = 0;

				var errorCount = 0;
				const int maxErrorCount = 10;

				while (!IsDisposed && _connected)
				{
					try
					{
						var task = _ws.ReceiveAsync(new ArraySegment<byte>(buf, pos, buf.Length - pos), _source.Token);
						task.Wait();

						var result = task.Result;

						if (result.CloseStatus != null)
						{
							if (task.Exception != null)
								PusherError.SafeInvoke(task.Exception);

							break;
						}

						pos += result.Count;

						if (!result.EndOfMessage)
							continue;

						var recv = Encoding.UTF8.GetString(buf, 0, pos);
						this.AddDebugLog(recv);

						pos = 0;

						var obj = JsonConvert.DeserializeObject<ChannelData>(recv);

						switch (obj.Event)
						{
							case "pusher:connection_established":
								PusherConnected.SafeInvoke();
								break;

							case "pusher_internal:subscription_succeeded":
							{
								switch (obj.Channel)
								{
									case "live_trades":
										TradesSubscribed.SafeInvoke();
										break;

									case "order_book":
										OrderBooksSubscribed.SafeInvoke();
										break;

									default:
										this.AddErrorLog(LocalizedStrings.Str3311Params, obj.Event);
										break;
								}

								break;
							}

							case "trade":
								NewTrade.SafeInvoke(JsonConvert.DeserializeObject<Trade>(obj.Data));
								break;

							case "data":
								NewOrderBook.SafeInvoke(JsonConvert.DeserializeObject<OrderBook>(obj.Data));
								break;

							default:
								this.AddErrorLog(LocalizedStrings.Str3312Params, obj.Event);
								break;
						}

						errorCount = 0;
					}
					catch (AggregateException ex)
					{
						PusherError.SafeInvoke(ex);

						var socketError = ex.InnerExceptions.FirstOrDefault() as WebSocketException;

						if (socketError != null)
							break;

						if (++errorCount >= maxErrorCount)
						{
							this.AddErrorLog("Max error {0} limit reached.", maxErrorCount);
							break;
						}
					}
					catch (Exception ex)
					{
						PusherError.SafeInvoke(ex);
					}
				}

				_ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, _source.Token).Wait();
				_ws.Dispose();

				PusherDisconnected.SafeInvoke();
			}
			catch (Exception ex)
			{
				PusherError.SafeInvoke(ex);
			}
		}

		private void Send(string command)
		{
			var sendBuf = Encoding.UTF8.GetBytes(command);
			_ws.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, _source.Token).Wait();
		}

		public void SubscribeTrades()
		{
			Send("{\"event\":\"pusher:subscribe\",\"data\":{\"channel\":\"live_trades\"}}");
		}

		public void UnSubscribeTrades()
		{
			Send("{\"event\":\"pusher:unsubscribe\",\"data\":{\"channel\":\"live_trades\"}}");
		}

		public void SubscribeDepths()
		{
			Send("{\"event\":\"pusher:subscribe\",\"data\":{\"channel\":\"order_book\"}}");
		}

		public void UnSubscribeDepths()
		{
			Send("{\"event\":\"pusher:unsubscribe\",\"data\":{\"channel\":\"order_book\"}}");
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			DisconnectPusher();
			base.DisposeManaged();
		}
	}
}