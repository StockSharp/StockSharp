// *************************************************************************************
// SCICHART® Copyright SciChart Ltd. 2011-2023. All rights reserved.
//  
// Web: http://www.scichart.com
//   Support: support@scichart.com
//   Sales:   sales@scichart.com
// 
// BaseChartPaneViewModel.cs is part of the SCICHART® Examples. Permission is hereby granted
// to modify, create derivative works, distribute and publish any part of this source
// code whether for commercial, private or personal use. 
// 
// The SCICHART® examples are distributed in the hope that they will be useful, but
// without any warranty. It is provided "AS IS" without warranty of any kind, either
// expressed or implied. 
// *************************************************************************************
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SciChart.Charting.Model.ChartSeries;
using SciChart.Charting.ViewportManagers;
using SciChart.Charting.Visuals.TradeChart;
using SciChart.Examples.ExternalDependencies.Common;
using SciChart.Examples.ExternalDependencies.Data;

namespace SciTrader.ViewModels
{    
    public abstract class BaseChartPaneViewModel : BaseViewModel, IChildPane
    {
        private readonly CreateMultiPaneStockChartsViewModel _parentViewModel;
        private ObservableCollection<IRenderableSeriesViewModel> _chartSeriesViewModels;
        private string _title;
        private string _yAxisTextFormatting;
        private bool _isFirstChartPane;
        private bool _isLastChartPane;
        private double _height = double.NaN;

        protected BaseChartPaneViewModel(CreateMultiPaneStockChartsViewModel parentViewModel)
        {
            _chartSeriesViewModels = new ObservableCollection<IRenderableSeriesViewModel>();
            _parentViewModel = parentViewModel;

            ViewportManager = new DefaultViewportManager();
        }

        public virtual void UpdatePriceSeries(PriceSeries priceSeries)
        {
            // Override in derived classes to update specific chart data
        }

        public virtual void UpdateRealTimeData(PriceBar price)
        {
            // Override in derived classes to update specific chart data
        }

        public virtual int GetDataCount()
        {
            return 0;
        }

        public CreateMultiPaneStockChartsViewModel ParentViewModel
        {
            get { return _parentViewModel; }
        }

        public ObservableCollection<IRenderableSeriesViewModel> ChartSeriesViewModels
        {
            //get { return _chartSeriesViewModels; }
            get => _chartSeriesViewModels;
            set
            {
                _chartSeriesViewModels = value;
                OnPropertyChanged("ChartSeriesViewModels");
            }
        }

        public IViewportManager ViewportManager { get; set; }

        public string YAxisTextFormatting
        {
            get { return _yAxisTextFormatting; }
            set
            {
                if (_yAxisTextFormatting == value)
                    return;

                _yAxisTextFormatting = value;
                OnPropertyChanged("YAxisTextFormatting");
            }
        }
        
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;

                _title = value;
                OnPropertyChanged("Title");
            }
        }
        
        public bool IsFirstChartPane
        {
            get { return _isFirstChartPane; }
            set
            {
                if (_isFirstChartPane == value) return;
                _isFirstChartPane = value;
                OnPropertyChanged("IsFirstChartPane");
            }
        }

        public bool IsLastChartPane
        {
            get { return _isLastChartPane; }
            set
            {
                if (_isLastChartPane == value) return;
                _isLastChartPane = value;
                OnPropertyChanged("IsLastChartPane");
            }
        }   

        public double Height
        {
            get { return _height; }
            set
            {
                if (Math.Abs(_height - value) < double.Epsilon) return;
                _height = value;
                OnPropertyChanged("Height");
            }
        }
                        
        public void ZoomExtents()
        {
        }

        public ICommand ClosePaneCommand
        {
            get; set;
        }
    }
}
