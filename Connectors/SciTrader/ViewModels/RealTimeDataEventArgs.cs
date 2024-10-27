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
using SciTrader.Model;

namespace SciTrader.ViewModels
{
    public class RealTimeDataEventArgs : EventArgs
    {
        public PriceBar PriceBar { get; set; }
        public ChartDataInfo ChartDataInfo { get; set; }
        public PriceBarAction Action { get; set; }

        public RealTimeDataEventArgs(PriceBar priceBar, ChartDataInfo chartDataInfo, PriceBarAction action)
        {
            PriceBar = priceBar;
            ChartDataInfo = chartDataInfo;
            Action = action;
        }
    }

    public enum PriceBarAction
    {
        None,
        Add,
        Update
    }
}
