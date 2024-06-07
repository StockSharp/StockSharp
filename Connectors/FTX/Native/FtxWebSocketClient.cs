namespace StockSharp.FTX.Native
{
	using System.Security;
	using System.Security.Cryptography;
	using System.Text;

	using Newtonsoft.Json.Linq;

	/// <summary>
	/// Subscriber for Websocket "trade" channel
	/// </summary>
	[Flags]
	internal enum WsTradeChannelSubscriber
	{
		None = 0,
		Trade = 1 << 0,
		Candles = 1 << 1
	}

	class FtxWebSocketClient : BaseLogReceiver
	{
		private readonly SecureString _key;
		private readonly HMACSHA256 _hasher;

		public event Action<string, List<Trade>> NewTrade;
		public event Action<string, OrderBook, QuoteChangeStates> NewOrderBook;
		public event Action<string, Level1> NewLevel1;
		public event Action<Order> NewOrder;
		public event Action<Fill> NewFill;
		public event Action Connected;
		public event Action<Exception> Error;
		public event Action<bool> Disconnected;

		private DateTime? _nextPing;

		private readonly WebSocketClient _client;

		public FtxWebSocketClient(SecureString key, SecureString secret, string subaccountName)
		{
			_key = key;
			_hasher = secret.IsEmpty() ? null : new(Encoding.UTF8.GetBytes(secret.UnSecure()));
			_client = new WebSocketClient(
				() =>
				{
					this.AddInfoLog(LocalizedStrings.Connected);
					try
					{
						SendAuthRequest(subaccountName);
						SendPingRequest();
						SubscribeFills();
						SubscribeOrders();
					}
					catch (Exception ex)
					{
						Error?.Invoke(ex);
					}

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
				exception =>
				{
					this.AddErrorLog(exception);
					Error?.Invoke(exception);
				},
				OnProcess,
				(s, a) => this.AddInfoLog(s, a),
				(s, a) =>
				{
					this.AddErrorLog(s, a);
				},
				(s, a) => this.AddVerboseLog(s, a),
				(s) => this.AddVerboseLog(s));
		}


		protected override void DisposeManaged()
		{
			_hasher?.Dispose();
			_client.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// Connect to the websocket
		/// </summary>
		public void Connect()
		{
			_nextPing = null;
			this.AddInfoLog(LocalizedStrings.Connecting);
			_client.Connect("wss://ftx.com/ws", true);
		}

		/// <summary>
		/// Closes the WebSocket connection
		/// </summary>
		public void Disconnect()
		{
			this.AddInfoLog(LocalizedStrings.Disconnecting);
			_client.Disconnect();
		}

		/// <summary>
		/// Market ping request
		/// </summary>
		public void ProcessPing()
		{
			if (_nextPing == null || DateTime.UtcNow < _nextPing.Value)
			{
				return;
			}
			SendPingRequest();
		}

		/// <summary>
		/// Fills subscribing
		/// </summary>
		public void SubscribeFills()
		{
			if (_client == null || !_client.IsConnected)
				return;

			_client?.Send(new
			{
				op = "subscribe",
				channel = "fills"
			});

		}

		/// <summary>
		/// Orders subscribing
		/// </summary>
		public void SubscribeOrders()
		{
			if (_client == null || !_client.IsConnected)
				return;

			_client?.Send(new
			{
				op = "subscribe",
				channel = "orders"
			});
		}

		private readonly SynchronizedList<string> _level1Subs = new();

		/// <summary>
		/// Level1 subscribing
		/// </summary>
		/// <param name="market">Currency</param>
		public void SubscribeLevel1(string market)
		{
			var subs = _level1Subs.ToList();

			if (subs.All(f => f != market))
			{
				if (_client == null || !_client.IsConnected)
					return;

				_client?.Send(new
				{
					op = "subscribe",
					channel = "ticker",
					market
				});
			}

			_level1Subs.Add(market);
		}

		/// <summary>
		/// Level1 unsubscribing
		/// </summary>
		/// <param name="market">Currency</param>
		public void UnsubscribeLevel1(string market)
		{

			lock (_level1Subs.SyncRoot)
			{
				var subs = _level1Subs.ToList();

				var subToDel = subs.FirstOrDefault(d => d == market);

				if (subToDel == null)
					return;

				_level1Subs.Remove(market);

				if (_level1Subs.All(d => d != market))
				{
					if (_client == null || !_client.IsConnected)
						return;

					_client?.Send(new
					{
						op = "unsubscribe",
						channel = "ticker",
						market
					});

				}
			}
		}

		private WsTradeChannelSubscriber _tradesMarketChannelSubFlags;

		/// <summary>
		/// Market trade channel subscribing
		/// </summary>
		/// <param name="market">Currency</param>
		/// <param name="subscriber">Subscriber for "trades" channel</param>
		public void SubscribeTradesChannel(string market, WsTradeChannelSubscriber subscriber)
		{

			if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
			{
				if (_client == null || !_client.IsConnected)
					return;

				_client?.Send(new
				{
					op = "subscribe",
					channel = "trades",
					market
				});
			}
			_tradesMarketChannelSubFlags |= subscriber;
		}

		/// <summary>
		/// Market trade channel unsubscribing
		/// </summary>
		/// <param name="market">Currency</param>
		/// <param name="subscriber">Subscriber for "trades" channel</param>
		public void UnsubscribeTradesChannel(string market, WsTradeChannelSubscriber subscriber)
		{
			_tradesMarketChannelSubFlags = Enumerator.Remove(_tradesMarketChannelSubFlags, subscriber);

			if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
			{
				if (_client == null || !_client.IsConnected)
					return;

				_client?.Send(new
				{
					op = "unsubscribe",
					channel = "trades",
					market
				});


			}
		}

		/// <summary>
		/// Market orderbook channel subscribing
		/// </summary>
		/// <param name="market">Currency</param>
		public void SubscribeOrderBook(string market)
		{
			if (_client == null || !_client.IsConnected)
				return;

			_client?.Send(new
			{
				op = "subscribe",
				channel = "orderbook",
				market
			});

		}

		/// <summary>
		/// Market Order book channel unsubscribing
		/// </summary>
		/// <param name="market">Currency</param>
		public void UnsubscribeOrderBook(string market)
		{
			if (_client == null || !_client.IsConnected)
				return;

			_client?.Send(new
			{
				op = "unsubscribe",
				channel = "orderbook",
				market
			});

		}


		private void OnProcess(dynamic obj)
		{
			var channel = (string)obj.channel;
			var type = (string)obj.type;

			if (channel == "ticker")
			{
				if (type != "update") return;

				WebSocketResponse<Level1> level1 = Parse<WebSocketResponse<Level1>>(obj);
				if (level1 != null && level1.Data != null)
				{
					NewLevel1?.Invoke(level1.Market, level1.Data);
				}
			}
			else if (channel == "trades")
			{
				if (type != "update") return;

				WebSocketResponse<List<Trade>> trade = Parse<WebSocketResponse<List<Trade>>>(obj);

				if (trade != null && trade.Data != null)
				{
					NewTrade?.Invoke(trade.Market, trade.Data);
				}
			}
			else if (channel == "orderbook")
			{
				if (type != "update" && type != "partial") return;

				WebSocketResponse<OrderBook> ob = Parse<WebSocketResponse<OrderBook>>(obj);
				if (ob != null && ob.Data != null)
				{
					NewOrderBook?.Invoke(ob.Market, ob.Data, type == "partial" ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment);
				}
			}
			else if (channel == "orders")
			{
				if (type != "update") return;

				WebSocketResponse<Order> order = Parse<WebSocketResponse<Order>>(obj);
				if (order != null && order.Data != null)
				{
					NewOrder?.Invoke(order.Data);
				}
			}
			else if (channel == "fills")
			{
				if (type != "update") return;

				WebSocketResponse<Fill> fill = Parse<WebSocketResponse<Fill>>(obj);
				if (fill != null && fill.Data != null)
				{
					NewFill?.Invoke(fill.Data);
				}
			}
		}

		#region Utils
		private static long GetMillisecondsFromEpochStart()
		{
			return GetMillisecondsFromEpochStart(DateTime.UtcNow);
		}

		private static long GetMillisecondsFromEpochStart(DateTime time)
		{
			return (long)time.ToUnix(false);
		}

		private void SendAuthRequest(string subaccountName)
		{
			long time = GetMillisecondsFromEpochStart();
			string sign = GenerateSignature(time);

			if (_client == null || !_client.IsConnected)
				return;

			if (subaccountName == null)
			{


				_client?.Send(new
				{
					op = "login",
					args = new
					{
						key = _key.UnSecure(),
						sign,
						time
					}
				});
			}
			else
			{
				_client?.Send(new
				{
					op = "login",
					args = new
					{
						key = _key.UnSecure(),
						sign,
						time,
						subaccount = subaccountName
					}
				});
			}
		}

		public void SendPingRequest()
		{
			if (_client == null || !_client.IsConnected)
				return;

			_client?.Send(new
			{
				op = "ping"
			});

			_nextPing = DateTime.UtcNow.AddSeconds(5);
		}

		private string GenerateSignature(long time)
		{
			var signature = $"{time}websocket_login";
			var hash = _hasher.ComputeHash(Encoding.UTF8.GetBytes(signature));
			var hashStringBase64 = BitConverter.ToString(hash).Replace("-", string.Empty);
			return hashStringBase64.ToLower();
		}

		private static T Parse<T>(dynamic obj)
		{
			if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
				throw new InvalidOperationException((string)obj.reason.ToString());
			return ((JToken)obj).DeserializeObject<T>();
		}
		#endregion
	}
}