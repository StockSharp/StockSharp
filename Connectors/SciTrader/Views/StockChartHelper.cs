// *************************************************************************************
// SCICHART® Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// StockChartHelper.cs is part of the SCICHART® Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART® examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System.Linq;
using System.Windows;
using SciChart.Charting;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Visuals.TradeChart;

namespace SciTrader.Views
{
    public static class StockChartHelper
    {
        public static readonly DependencyProperty ShowTooltipLabelProperty = DependencyProperty.RegisterAttached("ShowTooltipLabel", typeof (bool), typeof (StockChartHelper), new PropertyMetadata(default(bool), ShowTooltipLabelPropertyChanged));

        public static void SetShowTooltipLabel(DependencyObject element, bool value)
        {
            element.SetValue(ShowTooltipLabelProperty, value);
        }

        public static bool GetShowTooltipLabel(DependencyObject element)
        {
            return (bool) element.GetValue(ShowTooltipLabelProperty);
        }
 
        private static void ShowTooltipLabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var stockChart = d as SciChart.Charting.Visuals.TradeChart.SciStockChart;
            if (stockChart != null)
            {
                var group = (ModifierGroup) stockChart.ChartModifier;
                var cursor = group.ChildModifiers.SingleOrDefault(x => x is CursorModifier) as CursorModifier;
                if (cursor != null)
                {
                    cursor.ShowTooltip = (bool)args.NewValue;
                }
            }
        }
    }
}