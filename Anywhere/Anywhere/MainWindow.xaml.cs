using System;
using System.Windows;
using System.Linq;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Xaml;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Quik;
using StockSharp.Messages;
using StockSharp.Logging;
using StockSharp.Xaml;
using StockSharp.Quik.Lua;
using StockSharp.Fix;
using StockSharp.Localization;


namespace StockSharp.Anywhere
{

    public class UserSubscription
    {
        public Security Security { set; get; }

        public string SecurityId
        {
            get { return Security.Id; }
        }

        public bool Trades { set; get; }
        public bool MarketDepth { set; get; }
        public bool Level1 { set; get; }
        public bool Orders { set; get; }
        public bool MyTrades { set; get; }

    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //private Connector _connector;
        private LogManager _logManager = new LogManager();

        LuaFixTransactionMessageAdapter _transAdapter;
        FixMessageAdapter _messAdapter;

        private List<Security> _securities;
        private List<Portfolio> _portfolios;

        private class SecurityList : SynchronizedList<Security>, ISecurityList
        {
        }

        private bool _isUnloading;    // flag - data loading ...
        private bool _isConnectClick; // flag - click on connect button

        public MainWindow()
        {
            InitializeComponent();

            ConnectCommand = new DelegateCommand(OnConnect, o => CanOnConnect(o));
            UnloadingCommand = new DelegateCommand(Unloading, o => CanUnloading(o));
            DeleteSubscriptionCommand = new DelegateCommand(DeleteSubscription, o => CanDeleteSubscription(o));

            _securities = new List<Security>();
            _portfolios = new List<Portfolio>();

            _transAdapter = new LuaFixTransactionMessageAdapter(new MillisecondIncrementalIdGenerator())
            {
                Login = "quik",
                Password = "quik".To<SecureString>(),
                Address = QuikTrader.DefaultLuaAddress,
                TargetCompId = "StockSharpTS",
                SenderCompId = "quik",
                ExchangeBoard = ExchangeBoard.Forts,
                Version = FixVersions.Fix44_Lua,
                RequestAllPortfolios = true,
                MarketData = FixMarketData.None
            };

            _messAdapter = new FixMessageAdapter(new MillisecondIncrementalIdGenerator())
            {
                Login = "quik",
                Password = "quik".To<SecureString>(),
                Address = QuikTrader.DefaultLuaAddress,
                TargetCompId = "StockSharpMD",
                SenderCompId = "quik",
                ExchangeBoard = ExchangeBoard.Forts,
                Version = FixVersions.Fix44_Lua,
                RequestAllSecurities = true,
                RequestAllPortfolios = false,
                MarketData = FixMarketData.MarketData
            };

            _messAdapter.AddSupportedMessage(MessageTypes.Connect);

            ((IMessageAdapter)_messAdapter).NewOutMessage += OnNewOutMessage;

        }

        private ObservableCollectionEx<UserSubscription> subscriptions;

        /// <summary>
        ///  Information about a market data subscribing
        /// </summary>
        public ObservableCollectionEx<UserSubscription> Subscriptions
        {
            get
            {
                if (subscriptions == null) Subscriptions = new ObservableCollectionEx<UserSubscription>();
                return subscriptions;

            }
            set { subscriptions = value; }
        }


        private bool isConnected = false;
        /// <summary>
        /// Connection status
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #region Commands

        public DelegateCommand ConnectCommand { set; get; }

        private void OnConnect(Object obj)
        {
            if (!IsConnected)
            {
                _messAdapter.SendInMessage(new ConnectMessage());
            }
            else
            {
                _messAdapter.SendInMessage(new DisconnectMessage());
            }

            _isConnectClick = true;
        }

        private bool CanOnConnect(Object obj)
        {
            return !_isConnectClick;
        }

        public DelegateCommand UnloadingCommand { set; get; }

        private void Unloading(Object obj)
        {
            if (obj.ToString() == LocalizedStrings.StartUnloading)
            {
                _isUnloading = true;

                //SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_connector);
                //SecurityPicker.MarketDataProvider = _connector;
                //var securities =    SecurityPicker.FilteredSecurities.ToArray();

                foreach (var subscr in Subscriptions)
                {
                    SecurityId secId = subscr.Security.ToSecurityId();

                    if (subscr.MarketDepth)
                    {
                        SubscribeMarketData(secId, MarketDataTypes.MarketDepth);
                    };

                    if (subscr.Trades)
                    {
                        SubscribeMarketData(secId, MarketDataTypes.Trades);
                    };

                    if (subscr.Level1)
                    {
                        SubscribeMarketData(secId, MarketDataTypes.Level1);
                    };

                }

                UnloadingButton.Content = LocalizedStrings.StoptUnloading;

            }
            else if (obj.ToString() == LocalizedStrings.StoptUnloading)
            {

                foreach (var subscr in Subscriptions)
                {

                    SecurityId secId = subscr.Security.ToSecurityId();

                    if (subscr.MarketDepth)
                    {
                        UnSubscribeMarketData(secId, MarketDataTypes.MarketDepth);
                    };

                    if (subscr.Trades)
                    {
                        UnSubscribeMarketData(secId, MarketDataTypes.Trades);
                    };

                    if (subscr.Level1)
                    {
                        UnSubscribeMarketData(secId, MarketDataTypes.Level1);
                    };

                }

                //SecurityPicker.SecurityProvider = null;
                //SecurityPicker.MarketDataProvider = null;
                //SecurityPicker.ExcludeSecurities.Clear();

                UnloadingButton.Content = LocalizedStrings.StartUnloading;

                _isUnloading = false;

            }
        }

        private bool CanUnloading(Object obj)
        {
            return IsConnected && Subscriptions.Any(s => s.MarketDepth == true || s.Level1 == true || s.Trades == true);
        }

        /// <summary>
        /// Удалает элемент подписки
        /// </summary>
        public DelegateCommand DeleteSubscriptionCommand { set; get; }

        private void DeleteSubscription(Object obj)
        {
            if (Subscriptions.Contains((UserSubscription)obj))
            {
                Subscriptions.Remove((UserSubscription)obj);
                SecurityPicker.ExcludeSecurities.Add(((UserSubscription)obj).Security);
            }
        }

        private bool CanDeleteSubscription(Object obj)
        {
            return (obj != null && !_isUnloading);
        }

        #endregion

        private void Connect()
        {
            //if (_connector == null)
            //{
            //    _connector = new QuikTrader();

            //    _connector.LogLevel = LogLevels.Debug;

            //    _logManager.Sources.Add(_connector);

            //    _logManager.Listeners.Add(new GuiLogListener(Monitor));

            //    _connector.Connected += () =>
            //    {
            //        _isConnectClick = false;
            //        this.GuiAsync(() => ConnectButton.Content = LocalizedStrings.Disconnect);

            //    };

            //    _connector.Disconnected += () =>
            //    {
            //        _isConnectClick = false;
            //        this.GuiAsync(() => ConnectButton.Content = LocalizedStrings.Connect);
            //    };

            //    SecurityEditor.SecurityProvider = new FilterableSecurityProvider(_connector);

            //    _connector.NewSecurities += securities => { };

            //    _connector.NewMyTrades += trades =>
            //                {
            //                    trades.ForEach(t =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == t.Trade.Security && s.Trades))
            //                        {
            //                            MyTradeGrid.Trades.Add(t);
            //                            SaveToFile(MyTradeToString(t), _myTradesFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.NewTrades += trades =>
            //                {
            //                    TradeGrid.Trades.AddRange(trades);
            //                    trades.ForEach(t => SaveToFile(TradeToString(t), _tradesFilePath));
            //                };

            //    _connector.NewOrders += orders =>
            //                {
            //                    orders.ForEach(o =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
            //                        {
            //                            OrderGrid.Orders.Add(o);
            //                            SaveToFile(OrderToString(o), _ordersFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.OrdersChanged += orders =>
            //                {
            //                    orders.ForEach(o =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
            //                        {
            //                            OrderGrid.Orders.Add(o);
            //                            SaveToFile(OrderToString(o), _ordersFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.NewStopOrders += orders =>
            //                {
            //                    orders.ForEach(o =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
            //                        {
            //                            OrderGrid.Orders.Add(o);
            //                            SaveToFile(OrderToString(o), _ordersFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.StopOrdersChanged += orders =>
            //                {
            //                    orders.ForEach(o =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
            //                        {
            //                            OrderGrid.Orders.Add(o);
            //                            SaveToFile(OrderToString(o), _ordersFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.NewPortfolios += portfolios => PortfolioGrid.Portfolios.AddRange(portfolios);

            //    _connector.NewPositions += positions =>
            //                {
            //                    positions.ForEach(p =>
            //                    {
            //                        if (Subscriptions.Any(s => s.Security == p.Security))
            //                        {
            //                            PortfolioGrid.Positions.Add(p);
            //                            SaveToFile(PositionToString(p), _positionsFilePath);
            //                        }
            //                    });
            //                };

            //    _connector.PositionsChanged += positions =>
            //    {
            //        positions.ForEach(p =>
            //        {
            //            if (Subscriptions.Any(s => s.Security == p.Security))
            //            {
            //                PortfolioGrid.Positions.Add(p);
            //                SaveToFile(PositionToString(p), _positionsFilePath);
            //            }
            //        });
            //    };


            //    _connector.SecuritiesChanged += securities =>
            //                {
            //                    securities.ForEach(s => SaveToFile(Level1ToString(s), _level1FilePath));
            //                };

            //    _connector.MarketDepthsChanged += depths =>
            //                {
            //                    depths.ForEach(d => DepthToFile(d, Path.Combine(_outputFolder, string.Format("{0}_depth.txt", d.Security.Code))));
            //                };

            //    //_connector.ValuesChanged += (a, b, c, d) => { };

            //}

            //_connector.Connect();
        }

        private void OnNewOutMessage(Message message)
        {
            try
            {
                Security security = null;
                switch (message.Type)
                {
                    case MessageTypes.QuoteChange:
                        //Debug.WriteLine("QuoteChange");

                        QuoteChangeMessage qtCngMsg = (QuoteChangeMessage)message;

                        security = Subscriptions.FirstOrDefault(s => s.Security.Code == qtCngMsg.SecurityId.SecurityCode && s.Security.Type == qtCngMsg.SecurityId.SecurityType).Security;

                        if (security == null) return;

                        var depth = qtCngMsg.ToMarketDepth(security);

                        depth.WriteMarketDepth();

                        break;

                    case MessageTypes.Board:
                        //Debug.WriteLine("Board");
                        break;

                    case MessageTypes.Security:
                        //Debug.WriteLine("Security");
                        _securities.Add(((SecurityMessage)message).ToSecurity());
                        break;

                    case MessageTypes.SecurityLookupResult:
                        //Debug.WriteLine("SecurityLookupResult");
                        var lst = new SecurityList();
                        lst.AddRange(_securities);
                        this.GuiSync(() => SecurityEditor.SecurityProvider = new FilterableSecurityProvider(lst));
                        break;
                    case MessageTypes.PortfolioLookupResult:
                        //Debug.WriteLine("PortfolioLookupResult");
                        break;

                    case MessageTypes.Level1Change:
                        //Debug.WriteLine("Level1Change");
                        Level1ChangeMessage lvCngMsg = (Level1ChangeMessage)message;

                        security = _securities.FirstOrDefault(s => s.Code == lvCngMsg.SecurityId.SecurityCode && s.Type == lvCngMsg.SecurityId.SecurityType);

                        if (security == null) return;

                        security.ApplyChanges(lvCngMsg);

                        security.WriteLevel1();

                        break;

                    case MessageTypes.News:
                        break;

                    case MessageTypes.Execution:
                        //Debug.WriteLine("Execution");

                        if (_securities.Count == 0) return;

                        ExecutionMessage execMsg = (ExecutionMessage)message;

                        security = _securities.FirstOrDefault(s => s.Code == execMsg.SecurityId.SecurityCode && s.Type == execMsg.SecurityId.SecurityType);

                        if (security == null) return;

                        switch (execMsg.ExecutionType)
                        {
                            case ExecutionTypes.Tick:
                                {
                                    var trade = execMsg.ToTrade(security);
                                    trade.WriteTrade();
                                    break;
                                }
                            case ExecutionTypes.Trade:
                                {
                                    //Debug.WriteLine("MyTrade");

                                    var trade = execMsg.ToTrade(security);
                                    var order = execMsg.ToOrder(security);
                                    var mytrade = new MyTrade() { Trade = trade, Order = order };

                                    mytrade.WriteMyTrade();

                                    break;
                                }
                            case ExecutionTypes.Order:
                                {
                                    //Debug.WriteLine("Order");

                                    var order = execMsg.ToOrder(security);

                                    order.WriteOrder();

                                    break;
                                }
                            default:
                                break;
                        }


                        break;

                    case MessageTypes.Portfolio:
                        //Debug.WriteLine("Portfolio");
                        break;
                    case MessageTypes.PortfolioLookup:
                        // Debug.WriteLine("PortfolioLookup");
                        break;
                    case MessageTypes.PortfolioChange:
                        //Debug.WriteLine("PortfolioChange");
                        break;
                    case MessageTypes.Position:
                        //Debug.WriteLine("Position");

                        if (_securities.Count == 0) return;

                        PositionMessage posMsg = (PositionMessage)message;

                        security = _securities.FirstOrDefault(s => s.Code == posMsg.SecurityId.SecurityCode && s.Type == posMsg.SecurityId.SecurityType);

                        if (security == null) return;

                        var position = GetPosition(new Portfolio() { Name = posMsg.PortfolioName }, security);

                        posMsg.CopyExtensionInfo(position);

                        position.WritePosition();

                        break;

                    case MessageTypes.PositionChange:
                        //Debug.WriteLine("PositionChange");

                        if (_securities.Count == 0) return;

                        PositionChangeMessage posChMsg = (PositionChangeMessage)message;

                        if (_securities.Count == 0) return;

                        security = _securities.FirstOrDefault(s => s.Code == posChMsg.SecurityId.SecurityCode && s.Type == posChMsg.SecurityId.SecurityType);

                        if (security == null) return;

                        //var position = posChMsg.Changes.
                        var chgPosition = GetPosition(new Portfolio() { Name = posChMsg.PortfolioName }, security);

                        chgPosition.ApplyChanges(posChMsg);

                        chgPosition.WritePosition();

                        break;

                    case MessageTypes.MarketData:
                        {
                            //Debug.WriteLine("MarketData");
                            //var mdMsg = (MarketDataMessage)message;
                            break;
                        }

                    case MessageTypes.Error:
                        Debug.WriteLine(((ErrorMessage)message).Error.Message);
                        break;
                    case MessageTypes.Connect:
                        {
                            if (((ConnectMessage)message).Error == null)
                            {
                                if (_messAdapter.PortfolioLookupRequired)

                                    _messAdapter.SendInMessage(new PortfolioLookupMessage { IsBack = true, IsSubscribe = true, TransactionId = _messAdapter.TransactionIdGenerator.GetNextId() });

                                if (_messAdapter.OrderStatusRequired)
                                {
                                    var transactionId = _messAdapter.TransactionIdGenerator.GetNextId();
                                    _messAdapter.SendInMessage(new OrderStatusMessage { TransactionId = transactionId });
                                }

                                if (_messAdapter.SecurityLookupRequired)
                                {
                                    _messAdapter.SendInMessage(new SecurityLookupMessage { TransactionId = _messAdapter.TransactionIdGenerator.GetNextId() });
                                }

                                IsConnected = true;
                            }
                            else
                            {
                                Debug.WriteLine(((ConnectMessage)message).Error.Message);
                            }

                            _isConnectClick = false;

                            break;
                        }
                    case MessageTypes.Disconnect:
                        //Debug.WriteLine("Disconnect");
                        IsConnected = false;
                        _isConnectClick = false;

                        break;

                    case MessageTypes.SecurityLookup:
                        {
                            // Debug.WriteLine("SecurityLookup");
                            break;
                        }

                    case MessageTypes.Session:
                        //Debug.WriteLine("Session");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Message type {0} not suppoted.".Put(message.Type));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " " + ex.StackTrace);
            }


        }

        private void SecurityEditor_SecuritySelected()
        {
            if (Subscriptions.Any(s => s.Security == SecurityEditor.SelectedSecurity)) return;
            Subscriptions.Add(new UserSubscription() { Security = SecurityEditor.SelectedSecurity });
        }

        private void SubscribeMarketData(SecurityId securityId, MarketDataTypes type)
        {
            var message = new MarketDataMessage
            {
                DataType = type,
                IsSubscribe = true,
                SecurityId = securityId,
                From = DateTimeOffset.MinValue,
                To = DateTimeOffset.MaxValue,
                TransactionId = _messAdapter.TransactionIdGenerator.GetNextId()
            };

            switch (type)
            {
                case MarketDataTypes.MarketDepth:
                    message.MaxDepth = MarketDataMessage.DefaultMaxDepth;
                    break;
                case MarketDataTypes.Trades:
                    message.Arg = ExecutionTypes.Tick;
                    break;
                case MarketDataTypes.OrderLog:
                    message.Arg = ExecutionTypes.OrderLog;
                    break;
            }

            _messAdapter.SendInMessage(message);

        }

        private void UnSubscribeMarketData(SecurityId securityId, MarketDataTypes type)
        {
            var message = new MarketDataMessage
            {
                DataType = type,
                SecurityId = securityId,
                IsSubscribe = false,
                TransactionId = _messAdapter.TransactionIdGenerator.GetNextId()
            };

            switch (type)
            {
                case MarketDataTypes.Trades:
                    message.Arg = ExecutionTypes.Tick;
                    break;
                case MarketDataTypes.OrderLog:
                    message.Arg = ExecutionTypes.OrderLog;
                    break;
            }

            _messAdapter.SendInMessage(message);
        }

        #region INotifyPropertyChanged releases

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private Position GetPosition(Portfolio portfolio, Security security)
        {
            if (portfolio == null)
                throw new ArgumentNullException("portfolio");

            if (security == null)
                throw new ArgumentNullException("security");

            Position position;

            var ef = new EntityFactory();

            position = ef.CreatePosition(portfolio, security);

            position.LimitType = null;
            position.Description = "";

            return position;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsConnected)
            {
                _messAdapter.SendInMessage(new DisconnectMessage());
            }

        }
    }
}
