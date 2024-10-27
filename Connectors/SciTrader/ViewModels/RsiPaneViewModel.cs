// *************************************************************************************
// SCICHART® Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// RsiPaneViewModel.cs is part of the SCICHART® Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART® examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System;
using System.Diagnostics;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Examples.ExternalDependencies.Data;

namespace SciTrader.ViewModels
{
    public class RsiPaneViewModel : BaseChartPaneViewModel
    {
        private XyDataSeries<DateTime, double> _rsiSeries;
        public RsiPaneViewModel(CreateMultiPaneStockChartsViewModel parentViewModel/*, PriceSeries prices*/)
            : base(parentViewModel)
        {
            _rsiSeries = new XyDataSeries<DateTime, double>() { SeriesName = "RSI" };
            //rsiSeries.Append(prices.TimeData, prices.Rsi(14));
            ChartSeriesViewModels.Add(new LineRenderableSeriesViewModel {DataSeries = _rsiSeries});

            YAxisTextFormatting = "0.0";

            Height = 100;
        }

        public override void UpdatePriceSeries(PriceSeries priceSeries)
        {
            _rsiSeries.Clear();
            _rsiSeries.Append(priceSeries.TimeData, priceSeries.Rsi(14));
        }

        public override void UpdateRealTimeData(PriceBar price)
        {
            // Override in derived classes to update specific chart data
        }

        public override int GetDataCount()
        {
            return _rsiSeries.Count;
        }
    }
}