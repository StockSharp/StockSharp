

using System;
using System.Security;
using System.Security.Cryptography;
using SciTrader.Model;
using Newtonsoft.Json.Linq;
using StockSharp.Logging;
using System.Collections.Generic;
using StockSharp.Messages;
using System.Threading;
using System.Threading.Tasks;
using Ecng.Net;
using Ecng.Collections;
using Ecng.Common;
using System.Linq;
using Ecng.Serialization;
using StockSharp.Localization;

namespace System.Network.Generic
{
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
    class DarkHorseWebSocketClient : BaseLogReceiver
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

		public DarkHorseWebSocketClient(SecureString key, SecureString secret, string subaccountName)
		{
			_key = key;
			_hasher = secret.IsEmpty() ? null : new(secret.UnSecure().UTF8());
			_client = new WebSocketClient(
				() =>
				{
					this.AddInfoLog(LocalizedStrings.Connected);
					try
					{
						_ = SendAuthRequest(subaccountName, default);
						_ = SendPingRequest(default);
						_ = SubscribeFills(default);
						_ = SubscribeOrders(default);
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
		public ValueTask Connect(CancellationToken cancellationToken)
		{
			_nextPing = null;
			this.AddInfoLog(LocalizedStrings.Connecting);
			return _client.ConnectAsync("ws://localhost:9002", true, cancellationToken: cancellationToken);
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
		public ValueTask ProcessPing(CancellationToken cancellationToken)
		{
			if (_nextPing == null || DateTime.UtcNow < _nextPing.Value)
			{
				return default;
			}
			return SendPingRequest(cancellationToken);
		}

		public ValueTask SubscribeFills(CancellationToken cancellationToken)
		{
			if (_client == null || !_client.IsConnected)
				return default;

			return SendAsync(new
			{
				op = "subscribe",
				channel = "fills"
			}, cancellationToken);

		}

		/// <summary>
		/// Orders subscribing
		/// </summary>
		public ValueTask SubscribeOrders(CancellationToken cancellationToken)
		{
			if (_client == null || !_client.IsConnected)
				return default;

			return SendAsync(new
			{
				op = "subscribe",
				channel = "orders"
			}, cancellationToken);
		}

		private readonly SynchronizedList<string> _level1Subs = new();

		public async ValueTask SubscribeLevel1(string market, CancellationToken cancellationToken)
		{
			var subs = _level1Subs.ToList();

			if (subs.All(f => f != market))
			{
				if (_client == null || !_client.IsConnected)
					return;

				await SendAsync(new
				{
					op = "subscribe",
					channel = "ticker",
					market
				}, cancellationToken);
			}

			_level1Subs.Add(market);
		}

		public async ValueTask UnsubscribeLevel1(string market, CancellationToken cancellationToken)
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

				await SendAsync(new
				{
					op = "unsubscribe",
					channel = "ticker",
					market
				}, cancellationToken);

			}
		}

		private WsTradeChannelSubscriber _tradesMarketChannelSubFlags;

		public async ValueTask SubscribeTradesChannel(string symbol, WsTradeChannelSubscriber subscriber, CancellationToken cancellationToken)
		{
			if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
			{
				if (_client == null || !_client.IsConnected)
					return;

				await SendAsync(new
				{
					op = "subscribe",
					channel = "trades",
					symbol
				}, cancellationToken);
			}
			_tradesMarketChannelSubFlags |= subscriber;
		}

		public ValueTask UnsubscribeTradesChannel(string symbol, WsTradeChannelSubscriber subscriber, CancellationToken cancellationToken)
		{
			_tradesMarketChannelSubFlags = Enumerator.Remove(_tradesMarketChannelSubFlags, subscriber);

			if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
			{
				if (_client == null || !_client.IsConnected)
					return default;

				return SendAsync(new
				{
					op = "unsubscribe",
					channel = "trades",
					symbol
				}, cancellationToken);


			}

			return default;
		}

		public ValueTask SubscribeOrderBook(string symbol, CancellationToken cancellationToken)
		{
			if (_client == null || !_client.IsConnected)
				return default;

			return SendAsync(new
			{
				op = "subscribe",
				channel = "orderbook",
				symbol
			}, cancellationToken);

		}

		public ValueTask UnsubscribeOrderBook(string symbol, CancellationToken cancellationToken)
		{
			if (_client == null || !_client.IsConnected)
				return default;

			return SendAsync(new
			{
				op = "unsubscribe",
				channel = "orderbook",
				symbol
			}, cancellationToken);
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
				try
				{
					if (type != "update" && type != "partial") return;

					WebSocketResponse<OrderBook> ob = Parse<WebSocketResponse<OrderBook>>(obj);

					if (ob != null && ob.Data != null)
					{
						NewOrderBook?.Invoke(ob.Market, ob.Data, type == "partial" ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment);
					}
				}
				catch (Exception ex)
				{
					// Log the exception, handle the error or report it appropriately
					Console.WriteLine($"Error processing orderbook update: {ex.Message}");
					// Optionally, you can log the full exception stack trace
					Console.WriteLine(ex.StackTrace);
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

		private ValueTask SendAuthRequest(string subaccountName, CancellationToken cancellationToken)
		{
			long time = GetMillisecondsFromEpochStart();
			string sign = GenerateSignature(time);

			if (_client == null || !_client.IsConnected)
				return default;

			if (subaccountName == null)
			{
				return SendAsync(new
				{
					op = "login",
					args = new
					{
						key = _key.UnSecure(),
						sign,
						time
					}
				}, cancellationToken);
			}
			else
			{
				return SendAsync(new
				{
					op = "login",
					args = new
					{
						key = _key.UnSecure(),
						sign,
						time,
						subaccount = subaccountName
					}
				}, cancellationToken);
			}
		}

		public async ValueTask SendPingRequest(CancellationToken cancellationToken)
		{
			if (_client == null || !_client.IsConnected)
				return;

			await SendAsync(new
			{
				op = "ping"
			}, cancellationToken);

			_nextPing = DateTime.UtcNow.AddSeconds(5);
		}

		private ValueTask SendAsync(object request, CancellationToken cancellationToken)
			=> _client.SendAsync(request, cancellationToken);

		private string GenerateSignature(long time)
		{
			var signature = $"{time}websocket_login";
			return _hasher.ComputeHash(signature.UTF8()).Digest();
		}

		private static T Parse<T>(dynamic obj)
		{
			try
			{
				// Check if the object has an error status
				if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
					throw new InvalidOperationException((string)obj.reason.ToString());

				// Attempt to deserialize the object to the specified type
				return ((JToken)obj).DeserializeObject<T>();
			}
			catch (InvalidOperationException ex)
			{
				// Handle InvalidOperationException specifically (e.g., status == "error")
				Console.WriteLine($"Error occurred: {ex.Message}");
				throw; // Re-throw the exception if you want it to propagate further
			}
			catch (Exception ex)
			{
				// Handle any other exceptions that might occur (e.g., deserialization failure)
				Console.WriteLine($"An unexpected error occurred: {ex.Message}");
				throw; // Re-throw the exception or handle it as needed
			}
		}

		#endregion
	}
}