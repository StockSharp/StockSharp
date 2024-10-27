using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using SciChart.Charting;
using SciChart.Charting.Common.Helpers;
using SciChart.Charting.ViewportManagers;
using SciChart.Charting.Visuals.TradeChart;
using SciChart.Data.Model;
using SciChart.Examples.ExternalDependencies.Common;
using SciChart.Examples.ExternalDependencies.Data;
using SciTrader.Model;
using SciTrader;

namespace SciTrader.ViewModels
{
    public class CreateMultiPaneStockChartsViewModel : BaseViewModel
    {
        #region Fields

        private IndexRange _xVisibleRange;
        private ObservableCollection<BaseChartPaneViewModel> _chartPaneViewModels = new ObservableCollection<BaseChartPaneViewModel>();
        private bool _isPanEnabled;
        private bool _isZoomEnabled;
        private string _verticalChartGroupId;
        private IViewportManager _viewportManager;
        private bool _DataReceived = false;
        private Timer _timer;

        private ObservableCollection<StockItem> _stockItems;
        private StockItem _selectedItem;

        private ObservableCollection<int> _cycles;
        private int _selectedCycle;

        private ObservableCollection<string> _chartTypes;
        private string _selectedChartType;

        #endregion

        #region Properties

        public ObservableCollection<StockItem> StockItems
        {
            get { return _stockItems; }
            set
            {
                _stockItems = value;
                OnPropertyChanged(nameof(StockItems));
            }
        }

        public StockItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public ObservableCollection<int> Cycles
        {
            get { return _cycles; }
            set
            {
                _cycles = value;
                OnPropertyChanged(nameof(Cycles));
            }
        }

        public int SelectedCycle
        {
            get { return _selectedCycle; }
            set
            {
                _selectedCycle = value;
                OnPropertyChanged(nameof(SelectedCycle));
            }
        }

        public ObservableCollection<string> ChartTypes
        {
            get { return _chartTypes; }
            set
            {
                _chartTypes = value;
                OnPropertyChanged(nameof(ChartTypes));
            }
        }

        public string SelectedChartType
        {
            get { return _selectedChartType; }
            set
            {
                _selectedChartType = value;
                OnPropertyChanged(nameof(SelectedChartType));
            }
        }

        public string VerticalChartGroupId
        {
            get { return _verticalChartGroupId; }
            set
            {
                if (_verticalChartGroupId == value) return;
                _verticalChartGroupId = value;
                OnPropertyChanged("VerticalChartGroupId");
            }
        }

        public IndexRange XVisibleRange
        {
            get => _xVisibleRange;
            set
            {
                if (!Equals(_xVisibleRange, value))
                {
                    _xVisibleRange = value;
                    OnPropertyChanged("XVisibleRange");
                }
            }
        }

        public ObservableCollection<BaseChartPaneViewModel> ChartPaneViewModels
        {
            get { return _chartPaneViewModels; }
            set
            {
                if (_chartPaneViewModels == value) return;

                _chartPaneViewModels = value;
                OnPropertyChanged("ChartPaneViewModels");
            }
        }

        public bool IsPanEnabled
        {
            get { return _isPanEnabled; }
            set
            {
                _isPanEnabled = value;
                OnPropertyChanged("IsPanEnabled");
            }
        }

        public bool IsZoomEnabled
        {
            get { return _isZoomEnabled; }
            set
            {
                _isZoomEnabled = value;
                OnPropertyChanged("IsZoomEnabled");
            }
        }

        #endregion

        #region Commands

        public ICommand ZoomModeCommand { get; private set; }
        public ICommand PanModeCommand { get; private set; }
        public ICommand ZoomExtentsCommand { get; private set; }
        public ICommand ItemSelectedCommand { get; private set; }
        public ICommand CycleSelectedCommand { get; private set; }
        public ICommand ChartTypeSelectedCommand { get; private set; }

        #endregion

        #region Constructor

        public CreateMultiPaneStockChartsViewModel()
        {
            InitializeCommands();
            InitializeCollections();
            InitializeViewportManager();
            InitializeChartPaneViewModels();
            InitializeChartDataManager();
            LoadStockItems();
            SetZoomMode();
        }

        #endregion

        #region Initialization Methods

        private void InitializeCommands()
        {
            ItemSelectedCommand = new RelayCommand(OnItemSelected);
            CycleSelectedCommand = new RelayCommand(OnCycleSelected);
            ChartTypeSelectedCommand = new RelayCommand(OnChartTypeSelected);
            ZoomModeCommand = new ActionCommand(SetZoomMode);
            PanModeCommand = new ActionCommand(SetPanMode);
            ZoomExtentsCommand = new ActionCommand(ZoomExtends);
        }

        private void InitializeCollections()
        {
            Cycles = new ObservableCollection<int>(Enumerable.Range(1, 240));
            SelectedCycle = Cycles.FirstOrDefault();

            ChartTypes = new ObservableCollection<string> { "MIN", "DAY", "WEEK", "MONTH", "YEAR", "TICK" };
            SelectedChartType = ChartTypes.FirstOrDefault();

            StockItems = new ObservableCollection<StockItem>();
        }

        private void InitializeViewportManager()
        {
            _verticalChartGroupId = Guid.NewGuid().ToString();
            _viewportManager = new DefaultViewportManager();
        }

        private void InitializeChartPaneViewModels()
        {
            var closePaneCommand = new ActionCommand<IChildPane>(pane => ChartPaneViewModels.Remove((BaseChartPaneViewModel)pane));

            _chartPaneViewModels.Add(new PricePaneViewModel(this) { IsFirstChartPane = true, ViewportManager = _viewportManager });
            _chartPaneViewModels.Add(new MacdPaneViewModel(this) { Title = "MACD", ClosePaneCommand = closePaneCommand });
            _chartPaneViewModels.Add(new RsiPaneViewModel(this) { Title = "RSI", ClosePaneCommand = closePaneCommand });
            _chartPaneViewModels.Add(new VolumePaneViewModel(this) { Title = "Volume", ClosePaneCommand = closePaneCommand, IsLastChartPane = true });
        }

        private void InitializeChartDataManager()
        {
            var chartDataManager = ChartDataManager.Instance;
            chartDataManager.PriceSeriesReceived += OnPriceSeriesReceived;
            chartDataManager.RealTimeDataReceived += OnRealTimeDataReceived;
            chartDataManager.requestChartData();
        }

        #endregion

        #region Timer Methods

        private void InitializeTimer()
        {
            _timer = new Timer(1000); // Set interval to 10 seconds (10000 milliseconds)
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var chartDataManager = ChartDataManager.Instance;
            if (chartDataManager != null)
            {
                chartDataManager.PriceSeriesReceived += OnPriceSeriesReceived;
                chartDataManager.RealTimeDataReceived += OnRealTimeDataReceived;
                StopTimer();
            }

            Console.WriteLine("Timer ticked at: " + DateTime.Now);
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        #endregion

        #region Event Handlers

        private void OnPriceSeriesReceived(object sender, PriceSeriesEventArgs e)
        {
            foreach (var paneViewModel in _chartPaneViewModels)
            {
                paneViewModel.UpdatePriceSeries(e.PriceSeries);
            }
            _DataReceived = true;
        }

        private void OnRealTimeDataReceived(object sender, RealTimeDataEventArgs e)
        {
            if (!_DataReceived) return;

            foreach (var paneViewModel in _chartPaneViewModels)
            {
                paneViewModel.UpdateRealTimeData(e.PriceBar);
            }

            if (e.Action == PriceBarAction.Add && XVisibleRange != null && XVisibleRange.Max > _chartPaneViewModels[0].GetDataCount())
            {
                var existingRange = _xVisibleRange;
                var newRange = new IndexRange(existingRange.Min + 1, existingRange.Max + 1);
                XVisibleRange = newRange;
            }
        }

        #endregion

        #region Command Methods

        private void OnItemSelected(object parameter)
        {
            if (parameter is StockItem selectedStockItem)
            {
                var chartDataManager = ChartDataManager.Instance;
                chartDataManager.requestChartData(SelectedItem.ItemCode, SelectedChartType, SelectedCycle, 1500);
                _DataReceived = false;
            }
        }

        private void OnCycleSelected(object parameter)
        {
            if (parameter is int selectedCycle)
            {
                var chartDataManager = ChartDataManager.Instance;
                chartDataManager.requestChartData(SelectedItem.ItemCode, SelectedChartType, SelectedCycle, 1500);
                _DataReceived = false;
            }
        }

        private void OnChartTypeSelected(object parameter)
        {
            if (parameter is string selectedChartType)
            {
                var chartDataManager = ChartDataManager.Instance;
                chartDataManager.requestChartData(SelectedItem.ItemCode, SelectedChartType, SelectedCycle, 1500);
                _DataReceived = false;
            }
        }

        private void ZoomExtends()
        {
            _viewportManager.AnimateZoomExtents(TimeSpan.FromMilliseconds(500));
        }

        private void SetPanMode()
        {
            IsPanEnabled = true;
            IsZoomEnabled = false;
        }

        private void SetZoomMode()
        {
            IsPanEnabled = false;
            IsZoomEnabled = true;
        }

        #endregion

        #region Helper Methods

        private void LoadStockItems()
        {
            var itemManager = ItemManager.Instance;
            var stockItemsList = itemManager.GetFavoriteItemsByProductCode();
            StockItems = new ObservableCollection<StockItem>(stockItemsList);

            if (StockItems.Any())
            {
                SelectedItem = StockItems.First();
            }
        }

        #endregion
    }
}
