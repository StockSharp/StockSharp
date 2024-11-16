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
    public class PriceSeriesEventArgs : EventArgs
    {
        public PriceSeries PriceSeries { get; set; }
        public ChartDataInfo ChartDataInfo { get; set; }

        public PriceSeriesEventArgs(PriceSeries priceSeries, ChartDataInfo chartDataInfo)
        {
            PriceSeries = priceSeries;
            ChartDataInfo = chartDataInfo;
        }
    }
}
