using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using SciChart.Examples.ExternalDependencies.Data;
using SciTrader.ViewModels;

namespace SciTrader.Model
{
    // Represents the data and behavior for chart data handling.
    public class ChartData
    {
        public int TickCount { get; set; } // Count of ticks received
        public ChartDataInfo Info { get; set; } // Metadata about the chart
        public List<PriceBar> PriceBars { get; set; } // List of price bars representing the chart data
        public DateTime LastUpdateTime { get; private set; } // Last time data was updated

        private Timer _timer; // Timer for handling periodic tasks

        // Event triggered when a cycle is completed (e.g., new price bar added)
        public event EventHandler<RealTimeDataEventArgs> OnCycleCompleted;

        // Constructor initializing the ChartData with item code, chart type, and cycle duration
        public ChartData(string itemCode, string chartType, int cycle)
        {
            Info = new ChartDataInfo(itemCode, chartType, cycle);
            PriceBars = new List<PriceBar>();
            StartTimer();
        }

        // Starts the timer to handle cycle-based tasks
        private void StartTimer()
        {
            DateTime now = DateTime.Now;
            DateTime nextCycle = GetNextCycleTime(now);
            TimeSpan dueTime = nextCycle - now;
            double dueTimeMs = dueTime.TotalMilliseconds;
            double periodMs = TimeSpan.FromMinutes(Info.Cycle).TotalMilliseconds;

            _timer = new Timer(dueTimeMs);
            _timer.Elapsed += (sender, args) =>
            {
                OnTimerElapsed(sender, args);
                _timer.Interval = periodMs; // Set the interval for subsequent cycles
                _timer.Start();
            };
            _timer.AutoReset = false; // Execute only once initially
            _timer.Start();
        }

        // Calculates the next cycle time based on the last bar time
        private DateTime GetNextCycleTime(DateTime lastBarTime)
        {
            if (lastBarTime == DateTime.MinValue) // If no bars exist yet, start from the current cycle
            {
                DateTime now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);
            }

            DateTime nextCycle;
            if (Info.ChartType == "MIN")
            {
                int minutes = Info.Cycle;
                nextCycle = lastBarTime.AddMinutes(minutes - (lastBarTime.Minute % minutes))
                                         .AddSeconds(-lastBarTime.Second)
                                         .AddMilliseconds(-lastBarTime.Millisecond);
                if (nextCycle <= lastBarTime) // Ensure we get the next cycle time
                {
                    nextCycle = nextCycle.AddMinutes(minutes);
                }
            }
            else
            {
                nextCycle = lastBarTime.AddMilliseconds(Info.Cycle * 1000); // Assuming cycle is in seconds for TICK
            }
            return nextCycle;
        }

        // Handles the timer elapsed event to add a new PriceBar on cycle completion
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var newPriceBar = new PriceBar
            {
                DateTime = GetNextCycleTime(DateTime.Now),
                Open = PriceBars.Last().Close,
                Close = PriceBars.Last().Close,
                High = PriceBars.Last().Close,
                Low = PriceBars.Last().Close,
                Volume = 0
            };
            AddPriceBar(newPriceBar);
            OnCycleCompleted?.Invoke(this, new RealTimeDataEventArgs(newPriceBar, Info, PriceBarAction.Add));
        }

        // Adds a new price bar to the list
        public void AddPriceBar(PriceBar priceBar)
        {
            PriceBars.Add(priceBar);
        }

        // Handles incoming tick data and updates or adds price bars accordingly
        public void HandleTickData(ChartDataInfo chartDataInfo, PriceBar tickData)
        {
            double close = tickData.Close;
            LastUpdateTime = DateTime.Now;

            if (Info.ChartType == "TICK")
            {
                TickCount++;
                if (TickCount == Info.Cycle)
                {
                    TickCount = 0;
                    var newPriceBar = new PriceBar
                    {
                        DateTime = DateTime.Now,
                        Open = close,
                        Close = close,
                        High = close,
                        Low = close,
                        Volume = tickData.Volume
                    };
                    AddPriceBar(newPriceBar);
                    OnCycleCompleted?.Invoke(this, new RealTimeDataEventArgs(newPriceBar, Info, PriceBarAction.Add));
                }
                else
                {
                    var lastPriceBar = PriceBars.Last();
                    lastPriceBar.Close = close;
                    if (close > lastPriceBar.High)
                    {
                        lastPriceBar.High = close;
                    }
                    if (close < lastPriceBar.Low)
                    {
                        lastPriceBar.Low = close;
                    }
                    lastPriceBar.Volume += tickData.Volume;
                    OnCycleCompleted?.Invoke(this, new RealTimeDataEventArgs(lastPriceBar, Info, PriceBarAction.Update));
                }
            }
            else if (Info.ChartType == "MIN")
            {
                DateTime lastBarTime = PriceBars.Count == 0 ? DateTime.MinValue : PriceBars.Last().DateTime;
                DateTime nextCycleTime = GetNextCycleTime(lastBarTime);

                if (PriceBars.Count == 0 || DateTime.Now >= nextCycleTime)
                {
                    var newPriceBar = new PriceBar
                    {
                        DateTime = nextCycleTime,
                        Open = close,
                        Close = close,
                        High = close,
                        Low = close,
                        Volume = tickData.Volume
                    };
                    AddPriceBar(newPriceBar);
                    OnCycleCompleted?.Invoke(this, new RealTimeDataEventArgs(newPriceBar, Info, PriceBarAction.Add));
                }
                else
                {
                    var lastPriceBar = PriceBars.Last();
                    lastPriceBar.Close = close;
                    if (close > lastPriceBar.High)
                    {
                        lastPriceBar.High = close;
                    }
                    if (close < lastPriceBar.Low)
                    {
                        lastPriceBar.Low = close;
                    }
                    lastPriceBar.Volume += tickData.Volume;
                    OnCycleCompleted?.Invoke(this, new RealTimeDataEventArgs(lastPriceBar, Info, PriceBarAction.Update));
                }
            }
        }
    }
}
