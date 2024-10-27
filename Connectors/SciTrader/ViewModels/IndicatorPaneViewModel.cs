// *************************************************************************************
// SCICHART® Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// IndicatorPaneViewModel.cs is part of the SCICHART® Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART® examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Examples.ExternalDependencies.Data;

namespace SciTrader.ViewModels
{
    public class MacdPaneViewModel : BaseChartPaneViewModel
    {
        private XyDataSeries<DateTime, double> _histogramDataSeries;
        private XyyDataSeries<DateTime, double> _macdDataSeries;
        public MacdPaneViewModel(CreateMultiPaneStockChartsViewModel parentViewModel/*, PriceSeries prices*/)
            : base(parentViewModel)
        {
            _histogramDataSeries = new XyDataSeries<DateTime, double>() { SeriesName = "Histogram" };
            //histogramDataSeries.Append(prices.TimeData, macdPoints.Select(x => x.Divergence));
            ChartSeriesViewModels.Add(new ColumnRenderableSeriesViewModel {DataSeries = _histogramDataSeries});

            _macdDataSeries = new XyyDataSeries<DateTime, double>() { SeriesName = "MACD" };
            //macdDataSeries.Append(prices.TimeData, macdPoints.Select(x => x.Macd), macdPoints.Select(x => x.Signal));
            ChartSeriesViewModels.Add(new BandRenderableSeriesViewModel
            {
                DataSeries = _macdDataSeries,
                StrokeThickness = 2,
            });

            YAxisTextFormatting = "0.00";

            Height = 100;
        }

        public override void UpdatePriceSeries(PriceSeries priceSeries)
        {
            _histogramDataSeries.Clear();
            _macdDataSeries.Clear();
            IEnumerable<MacdPoint> macdPoints = priceSeries.CloseData.Macd(12, 26, 9).ToList();
            _histogramDataSeries.Append(priceSeries.TimeData, macdPoints.Select(x => x.Divergence));
            _macdDataSeries.Append(priceSeries.TimeData, macdPoints.Select(x => x.Macd), macdPoints.Select(x => x.Signal));
        }

        public override void UpdateRealTimeData(PriceBar price)
        {
            // Override in derived classes to update specific chart data
        }

        public override int GetDataCount()
        {
            return _macdDataSeries.Count;
        }
    }
}
