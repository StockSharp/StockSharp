// *************************************************************************************
// SCICHART� Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// VolumePaneViewModel.cs is part of the SCICHART� Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART� examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System;
using System.Diagnostics;
using System.Linq;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Examples.ExternalDependencies.Data;

namespace SciTrader.ViewModels
{
    public class VolumePaneViewModel : BaseChartPaneViewModel
    {
        private XyDataSeries<DateTime, double> _volumePrices;
        public VolumePaneViewModel(CreateMultiPaneStockChartsViewModel parentViewModel/*, PriceSeries prices*/)
            : base(parentViewModel)
        {
            _volumePrices = new XyDataSeries<DateTime, double>() { SeriesName = "Volume" };
            //volumePrices.Append(prices.TimeData, prices.VolumeData.Select(x => (double) x));
            ChartSeriesViewModels.Add(new ColumnRenderableSeriesViewModel {DataSeries = _volumePrices});

            YAxisTextFormatting = "###E+0";
        }

        public override void UpdatePriceSeries(PriceSeries priceSeries)
        {
            _volumePrices.Clear();
            _volumePrices.Append(priceSeries.TimeData, priceSeries.VolumeData.Select(x => (double)x));
        }

        public override void UpdateRealTimeData(PriceBar price)
        {
            // Override in derived classes to update specific chart data
        }

        public override int GetDataCount()
        {
            return _volumePrices.Count;
        }
    }
}