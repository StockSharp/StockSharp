using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.ViewportManagers;
using SciChart.Charting.Visuals.TradeChart;
using SciChart.Examples.ExternalDependencies.Common;
using SciChart.Examples.ExternalDependencies.Data;
using Newtonsoft.Json.Linq;
using System.Timers;
using SciTrader;
using System.Windows;
using SciTrader.ViewModels;
using SciTrader.Network;

//using System.Windows.Automation.Provider;
namespace SciTrader.Model
{
    /// <summary>
    /// Manages chart data, handles WebSocket communication, and processes incoming price data.
    /// </summary>
    public class ChartDataManager
    {
        private static readonly Lazy<ChartDataManager> _instance = new Lazy<ChartDataManager>(() => new ChartDataManager("ws://localhost:9002"));

        public static ChartDataManager Instance => _instance.Value;

        // Dictionary to store chart data using generated key.
        private readonly Dictionary<string, ChartData> _chartData;

        // WebSocket client to receive real-time data.
        private WebSocketClient _webSocketClient;
        private Timer _timer; // Add timer field
                              // Events triggered when price series or real-time data is received.
        public event EventHandler<PriceSeriesEventArgs> PriceSeriesReceived;
        public event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;

        /// <summary>
        /// Initializes a new instance of the ChartDataManager class.
        /// </summary>
        /// <param name="websocketUrl">The URL of the WebSocket server.</param>
        public ChartDataManager(string websocketUrl)
        {
            _chartData = new Dictionary<string, ChartData>();
            _webSocketClient = new WebSocketClient(websocketUrl);
            _webSocketClient.OnConnected += OnConnected;
            _webSocketClient.OnPriceSeriesReceived += OnPriceSeriesReceived;
            _webSocketClient.OnRealTimeDataReceived += OnRealTimeDataReceived;
            _webSocketClient.OnDisconnected += OnDisconnected;
            _webSocketClient.Connect();
            //InitializeTimer(); // Initialize and start the timer
        }

        private void InitializeTimer()
        {
            _timer = new Timer(500); // Set interval to 10 seconds (10000 milliseconds)
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Handle the timer tick event here
            Console.WriteLine("Timer ticked at: " + DateTime.Now);
            var chartDataInfo = new ChartDataInfo("NQU24", "MIN", 1);
            // Example: Perform some periodic task
            var key = chartDataInfo.GetKey();
            double close = 2041950;
            var tickData = new PriceBar
            {
                DateTime = DateTime.Now,
                Open = close,
                Close = close,
                High = close,
                Low = close,
                Volume = 0
            };

            if (_chartData.ContainsKey(key))
            {
                _chartData[key].HandleTickData(chartDataInfo, tickData);
            }
            else
            {
                // Optionally handle the case where real-time data is received before historical data is initialized
                Console.WriteLine($"Real-time data received for {key} before historical data initialization.");
            }
        }


        /// <summary>
        /// Handles the WebSocket connected event.
        /// Sends a request for chart data upon connection.
        /// </summary>
        private void OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket connected at: " + DateTime.Now);
        }

        public void requestChartData()
        {
            try
            {
                ItemManager itemManager = ItemManager.Instance;
                var item = itemManager.GetFirstFavoriteItemsByProductCode();
                if (item != null)
                {
                    var requestData = new ChartDataRequest(item.ItemCode, "MIN", 1, 0, 1500);
                    var jsonMessage = requestData.ToJson();
                    Console.WriteLine(jsonMessage);

                    _webSocketClient.Send(jsonMessage);
                }
                else
                {
                    var requestData = new ChartDataRequest("NQU24", "MIN", 1, 0, 1500);
                    var jsonMessage = requestData.ToJson();
                    Console.WriteLine(jsonMessage);

                    _webSocketClient.Send(jsonMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error requestChartData: {ex.Message}");
            }
        }

        public void requestChartData(string itemCode, string chartType, int cycle, int count)
        {
            var requestData = new ChartDataRequest(itemCode, chartType, cycle, 0, count);
            var jsonMessage = requestData.ToJson();
            Console.WriteLine(jsonMessage);

            _webSocketClient.Send(jsonMessage);
        }

        /// <summary>
        /// Handles the WebSocket disconnected event.
        /// Logs the disconnection time.
        /// </summary>
        private void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("WebSocket disconnected at: " + DateTime.Now);
        }

        /// <summary>
        /// Handles the event when a series of price data is received.
        /// Saves the received data and raises the PriceSeriesReceived event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args containing the received price series data and ChartDataInfo.</param>
        private void OnPriceSeriesReceived(object sender, PriceSeriesEventArgs e)
        {
            foreach (var priceBar in e.PriceSeries)
            {
                SaveHistoricalTickData(e.ChartDataInfo, priceBar);
            }

            PriceSeriesReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the event when real-time price data is received.
        /// Saves the received data and raises the RealTimeDataReceived event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args containing the received real-time price data and ChartDataInfo.</param>
        private void OnRealTimeDataReceived(object sender, RealTimeDataEventArgs e)
        {
            SaveRealTimeTickData(e.ChartDataInfo, e.PriceBar);
        }

        /// <summary>
        /// Saves historical tick data for a specified item, chart type, and cycle.
        /// </summary>
        /// <param name="chartDataInfo">The chart data info object containing item code, chart type, and cycle.</param>
        /// <param name="tickData">The tick data to save.</param>
        public void SaveHistoricalTickData(ChartDataInfo chartDataInfo, PriceBar tickData)
        {
            var key = chartDataInfo.GetKey();

            if (!_chartData.ContainsKey(key))
            {
                var chartData = new ChartData(chartDataInfo.ItemCode, chartDataInfo.ChartType, chartDataInfo.Cycle);
                chartData.OnCycleCompleted += ChartData_CycleCompleted;
                _chartData[key] = chartData;
            }

            _chartData[key].AddPriceBar(tickData);
        }

        /// <summary>
        /// Saves real-time tick data for a specified item, chart type, and cycle.
        /// </summary>
        /// <param name="chartDataInfo">The chart data info object containing item code, chart type, and cycle.</param>
        /// <param name="tickData">The tick data to save.</param>
        public void SaveRealTimeTickData(ChartDataInfo chartDataInfo, PriceBar tickData)
        {
            var key = chartDataInfo.GetKey();

            if (_chartData.ContainsKey(key))
            {
                _chartData[key].HandleTickData(chartDataInfo, tickData);
            }
            else
            {
                // Optionally handle the case where real-time data is received before historical data is initialized
                Console.WriteLine($"Real-time data received for {key} before historical data initialization.");
            }
        }

        private void ChartData_CycleCompleted(object sender, RealTimeDataEventArgs e)
        {
            // Now `e` is the PriceBar instance that was passed when the event was invoked
            var newPriceBar = e;
            RealTimeDataReceived?.Invoke(this, e);
            // Do something with the new PriceBar, e.g., update UI or log the event
            //Console.WriteLine($"Cycle completed with new PriceBar: {newPriceBar.DateTime}, Open: {newPriceBar.Open}, Close: {newPriceBar.Close}");
        }

        public void HandleTickData(string itemCode, PriceBar tickData)
        {
            var chartDataList = GetChartDataByItemCode(itemCode);

            if (chartDataList.Count > 0)
            {
                foreach (var chartData in chartDataList)
                {
                    Console.WriteLine($"ChartType: {chartData.Info.ChartType}, Cycle: {chartData.Info.Cycle}, Data Points: {chartData.PriceBars.Count}");

                    chartData.HandleTickData(chartData.Info, tickData);
                }
            }
            else
            {
                Console.WriteLine("No data available for the specified item code.");
            }
        }

        /// <summary>
        /// Retrieves the chart data for a specified item, chart type, and cycle.
        /// </summary>
        /// <param name="itemCode">The item code of the data.</param>
        /// <param name="chartType">The chart type (e.g., "MIN").</param>
        /// <param name="cycle">The cycle interval for the data.</param>
        /// <returns>A list of PriceBar objects representing the chart data.</returns>
        public List<PriceBar> GetChartData(string itemCode, string chartType, int cycle)
        {
            var key = new ChartDataInfo(itemCode, chartType, cycle).GetKey();

            if (_chartData.ContainsKey(key))
            {
                return _chartData[key].PriceBars;
            }

            return new List<PriceBar>();
        }

        /// <summary>
        /// Retrieves all chart data for a specified item code.
        /// </summary>
        /// <param name="itemCode">The item code of the data.</param>
        /// <returns>A list of ChartData objects representing the chart data for the specified item code.</returns>
        public List<ChartData> GetChartDataByItemCode(string itemCode)
        {
            var chartDataList = new List<ChartData>();

            foreach (var key in _chartData.Keys)
            {
                if (key.StartsWith(itemCode))
                {
                    chartDataList.Add(_chartData[key]);
                }
            }

            return chartDataList;
        }
    }
}
