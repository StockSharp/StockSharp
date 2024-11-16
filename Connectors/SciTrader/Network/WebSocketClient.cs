using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Timers;
using SciChart.Examples.ExternalDependencies.Data;
using SciTrader.ViewModels;
using SciTrader.Model;

namespace SciTrader.Network
{
    public class WebSocketClient
    {
        private WebSocket _webSocket;
        private Timer _reconnectTimer;
        private string _url;
        private bool _connected;

        public event EventHandler<PriceSeriesEventArgs> OnPriceSeriesReceived; // Update event handler type
        public event EventHandler<RealTimeDataEventArgs> OnRealTimeDataReceived; // Update event handler type
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;

        public WebSocketClient(string url)
        {
            _url = url;
            _webSocket = new WebSocket(url);

            _webSocket.OnMessage += WebSocket_OnMessage;
            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnClose += WebSocket_OnClose;

            _reconnectTimer = new Timer(5000); // 5 seconds interval
            _reconnectTimer.Elapsed += (sender, e) => Reconnect();
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            var jsonData = JObject.Parse(e.Data);
            string protocol = jsonData["protocol"].ToString();

            switch (protocol)
            {
                case "response":
                    HandleResponse(jsonData);
                    break;

                case "subscribe_item":
                    HandleSubscribe(jsonData);
                    break;

                case "request":
                    HandleRequest(jsonData);
                    break;

                default:
                    Console.WriteLine("Unknown protocol: " + protocol);
                    break;
            }
        }

        private void HandleResponse(JObject jsonData)
        {
            string message = jsonData["message"].ToString();
            Console.WriteLine(message);
        }

        private void HandleSubscribe(JObject jsonData)
        {
            var chartDataManager = ChartDataManager.Instance;
            var itemCode = jsonData["itemCode"].ToString();
            var chartDataList = chartDataManager.GetChartDataByItemCode(itemCode);
            if (chartDataList.Count == 0) return;
            var priceBar = ParsePriceBar(jsonData);
            foreach(var chartData in chartDataList)
                OnRealTimeDataReceived?.Invoke(this, new RealTimeDataEventArgs(priceBar, chartData.Info, PriceBarAction.None));
        }

        private void HandleRequest(JObject jsonData)
        {
            var chartDataInfo = ParseChartDataInfo(jsonData);
            var priceSeries = new PriceSeries();
            var candles = jsonData["candles"] as JArray;
            if (candles != null)
            {
                foreach (var candle in candles)
                {
                    var priceBar = ParsePriceBar(candle);
                    priceSeries.Add(priceBar);
                }
            }

            OnPriceSeriesReceived?.Invoke(this, new PriceSeriesEventArgs(priceSeries, chartDataInfo));
            var itemCode = jsonData["itemCode"];

            var json = new JObject();
            json.Add("protocol", "register_item");
            json.Add("itemCode", itemCode);
            string jsonMessage = json.ToString();
            Console.WriteLine(jsonMessage);

            Send(jsonMessage);
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            _connected = true;
            OnConnected?.Invoke(this, EventArgs.Empty);
            StopReconnectTimer();
        }

        private void WebSocket_OnClose(object sender, EventArgs e)
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
            StartReconnectTimer();
        }

        public void Connect()
        {
            _webSocket.Connect();
        }

        public void Disconnect()
        {
            StopReconnectTimer();
            _webSocket.Close();
        }

        public void Send(string message)
        {
            if (!_connected) return;
            _webSocket.Send(message);
        }

        private void StartReconnectTimer()
        {
            if (!_reconnectTimer.Enabled)
            {
                _reconnectTimer.Start();
            }
        }

        private void StopReconnectTimer()
        {
            if (_reconnectTimer.Enabled)
            {
                _reconnectTimer.Stop();
            }
        }

        private void Reconnect()
        {
            if (!_webSocket.IsAlive)
            {
                _webSocket.Connect();
            }
        }

        private PriceBar ParsePriceBar(JToken jsonData)
        {
            string timestamp = jsonData["timestamp"].ToString();
            DateTime dateTime;

            if (timestamp.Length == 17) // Length of "yyyyMMddHHmmssfff"
            {
                dateTime = DateTime.ParseExact(timestamp, "yyyyMMddHHmmssfff", null);
            }
            else if (timestamp.Length == 14) // Length of "yyyyMMddHHmmss"
            {
                dateTime = DateTime.ParseExact(timestamp, "yyyyMMddHHmmss", null);
            }
            else
            {
                throw new FormatException("Unexpected timestamp format");
            }

            return new PriceBar
            {
                DateTime = dateTime,
                Open = (double)jsonData["open"],
                High = (double)jsonData["high"],
                Low = (double)jsonData["low"],
                Close = (double)jsonData["close"],
                Volume = (long)jsonData["volume"]
            };
        }

        private ChartDataInfo ParseChartDataInfo(JToken jsonData)
        {
            return new ChartDataInfo(
                jsonData["itemCode"].ToString(),
                jsonData["chartType"].ToString(),
                (int)jsonData["cycle"]
            );
        }
    }
}
