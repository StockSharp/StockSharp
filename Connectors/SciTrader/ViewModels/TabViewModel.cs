using System;
using System.ComponentModel;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using SciTrader.Data;
using SciTrader.DataSources;
using SciTrader.Views;
using DevExpress.Charts;

namespace SciTrader.ViewModels {
    public class TabViewModel : IDocumentContent {
        public static TabViewModel Create(MarketDataProvider symbolProvider, SymbolData symbol) {
            return ViewModelSource.Create(() => new TabViewModel(symbolProvider, symbol));
        }

        readonly OrderBookViewModel orderBookModel;
        readonly TradesViewModel tradesModel;
        readonly StockChartViewModel stockChartModel;
        readonly MarketDepthViewModel marketDepthModel;
        readonly OpenOrdersViewModel openOrdersModel;
        readonly OrderHistoryViewModel orderHistoryModel;
        readonly TradeHistoryViewModel tradeHistoryModel;
        readonly MarketDataProvider dataProvider;
        readonly AddIndicatorCustomCommand addIndicatorCommand;

        TradingDataSource tradingSource;        

        public virtual object Title { get; protected set; }
        public OrderBookViewModel OrderBookModel { get { return orderBookModel; } }
        public TradesViewModel TradesModel { get { return tradesModel; } }
        public StockChartViewModel StockChartModel { get { return stockChartModel; } }
        public MarketDepthViewModel MarketDepthModel { get { return marketDepthModel; } }
        public OpenOrdersViewModel OpenOrdersModel { get { return openOrdersModel; } }
        public OrderHistoryViewModel OrderHistoryModel { get { return orderHistoryModel; } }
        public TradeHistoryViewModel TradeHistoryModel { get { return tradeHistoryModel; } }
        public ICommand AddIndicatorCommand { get { return addIndicatorCommand; } }

        public virtual SymbolData Symbol { get; set; }
        public IDocumentOwner DocumentOwner { get; set; }

        public TabViewModel(MarketDataProvider symbolProvider, SymbolData symbol) {
            addIndicatorCommand = new AddIndicatorCustomCommand();
            dataProvider = symbolProvider;
            orderBookModel = OrderBookViewModel.Create();
            tradesModel = TradesViewModel.Create();
            stockChartModel = StockChartViewModel.Create();
            marketDepthModel = MarketDepthViewModel.Create();
            Symbol = symbol;
            stockChartModel.Init(Symbol);
            dataProvider.OnSymbolChanged(null, Symbol, stockChartModel.CurrentPrice);
            tradingSource = dataProvider.GetDataSource(Symbol);
            tradesModel.Init(tradingSource.ClosedOrders);
            orderBookModel.Init(tradingSource.SellOrders, tradingSource.BuyOrders);
            openOrdersModel = OpenOrdersViewModel.Create(dataProvider.OpenOrdersDataSource);
            orderHistoryModel = OrderHistoryViewModel.Create(dataProvider.OrdersHistoryDataSource);
            tradeHistoryModel = TradeHistoryViewModel.Create(dataProvider.TradesDataSource);
            Title = Symbol.Symbol;
        }

        public void UpdateData() {        
            orderBookModel.UpdateData(tradingSource.OpenedTransactionInfo, tradingSource.ClosedTransactionInfo);
            tradesModel.UpdateData(tradingSource.ClosedOrders[0]);
            stockChartModel.UpdateData(tradingSource.ClosedOrders[0]);
            marketDepthModel.UpdateData(tradingSource.SellOrders, tradingSource.BuyOrders);
            Title = Symbol.Symbol + " " + string.Format("{0:f2}", tradingSource.CurrentPrice);            
        }
        public void StockChartViewLoaded(StockChartView view) {
            addIndicatorCommand.Chart = view.Chart;
        }
        public void OnClose(CancelEventArgs e) {            
        }
        public void OnDestroy() {
            dataProvider.OnSymbolChanged(Symbol, null, 0.0);
        }
    }
}
