using System;
using System.Windows;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Ecng.Xaml;
using MoreLinq;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Quik;
using StockSharp.Messages;
using StockSharp.Logging;
using StockSharp.Xaml;
using StockSharp.Localization;

namespace Anywhere
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
    
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Connector _connector;
        private LogManager _logManager = new LogManager();

        private bool _isUnloading;    // флаг - идет загрузка данных   
        private bool _isConnectClick; // флаг - выполнен щелчок по кнопке соединения

        static private string _outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OUTPUT"); // путь к папке в выгружаемыми данными 

        private string _tradesFilePath = Path.Combine(_outputFolder, "trades.txt");
        private string _ordersFilePath = Path.Combine(_outputFolder, "orders.txt");
        private string _myTradesFilePath = Path.Combine(_outputFolder, "mytrades.txt");
        private string _level1FilePath = Path.Combine(_outputFolder, "level1.txt");
        private string _positionsFilePath = Path.Combine(_outputFolder, "positions.txt");

        public MainWindow()
        {
            InitializeComponent();

            ConnectCommand = new DelegateCommand(OnConnect, o => CanOnConnect(o));
            UnloadingCommand = new DelegateCommand(Unloading, o => CanUnloading(o));
            DeleteSubscriptionCommand = new DelegateCommand(DeleteSubscription, o => CanDeleteSubscription(o));

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
        }

        private ObservableCollectionEx<UserSubscription> subscriptions;

        /// <summary>
        /// Содержит информацию о подписке на данные
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

        #region Commands

        /// <summary>
        /// Устанавливает/разрывает соединение
        /// </summary>
        public DelegateCommand ConnectCommand { set; get; }

        private void OnConnect(Object obj)
        {
            if (obj.ToString() == "Подключиться")
            {
                 Connect();
            }
            else
            {
                _connector.Disconnect();
            }

            _isConnectClick = true;
        }

        private bool CanOnConnect(Object obj)
        {
            return !_isConnectClick;
        }

        /// <summary>
        /// Запускает/останавливает выгрузку данных
        /// </summary>
        public DelegateCommand UnloadingCommand { set; get; }

        private void Unloading(Object obj)
        {
            if (obj.ToString() == "Начать выгрузку")
            {
                _isUnloading = true;
             
                SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_connector);
                SecurityPicker.MarketDataProvider = _connector;

                var securities =    SecurityPicker.FilteredSecurities.ToArray();

                foreach (var security in securities)
                {
                    if (!subscriptions.Any(s => s.Security == security))
                    {
                        SecurityPicker.ExcludeSecurities.Add(security);
                    }
                }
                
                foreach (var subscr in Subscriptions)
                {
                    if (subscr.Level1) _connector.RegisterSecurity(subscr.Security);
                    if (subscr.MarketDepth) _connector.RegisterMarketDepth(subscr.Security);
                    if (subscr.Trades) _connector.RegisterTrades(subscr.Security);
                }

                foreach (var portfolio in _connector.Portfolios)
                {
                    _connector.RegisterPortfolio(portfolio);
                }

                UnloadingButton.Content = "Остановить выгрузку";

            }
            else if (obj.ToString() == "Остановить выгрузку")
            {

                foreach (var subscr in Subscriptions)
                {
                    if (subscr.Level1) _connector.UnRegisterSecurity(subscr.Security);
                    if (subscr.MarketDepth) _connector.UnRegisterMarketDepth(subscr.Security);
                    if (subscr.Trades) _connector.UnRegisterTrades(subscr.Security);
                }

                SecurityPicker.SecurityProvider = null;
                SecurityPicker.MarketDataProvider = null;
                SecurityPicker.ExcludeSecurities.Clear();

                foreach (var portfolio in _connector.Portfolios)
                {
                    _connector.UnRegisterPortfolio(portfolio);
                }

                UnloadingButton.Content = "Начать выгрузку";

                _isUnloading = false;

            }
        }

        private bool CanUnloading(Object obj)
        {
            return _connector != null && 
                   _connector.ConnectionState == ConnectionStates.Connected && 
                   Subscriptions.Any(s => s.MarketDepth == true || s.Level1 == true || s.Trades == true);
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
            if (_connector == null)
            {
                _connector = new QuikTrader();

                _connector.LogLevel = LogLevels.Debug;

                _logManager.Sources.Add(_connector);

                _logManager.Listeners.Add(new GuiLogListener(Monitor));

                _connector.Connected += () => 
                {
                    _isConnectClick = false;
                    this.GuiAsync(() => ConnectButton.Content = LocalizedStrings.Disconnect);

                };

                _connector.Disconnected += () => 
                {
                    _isConnectClick = false;
                    this.GuiAsync(() => ConnectButton.Content = LocalizedStrings.Connect);
                };

                SecurityEditor.SecurityProvider = new FilterableSecurityProvider(_connector);
               
                _connector.NewSecurities += securities => { };

                _connector.NewMyTrades += trades =>
                            {
                                trades.ForEach(t =>
                                {
                                    if (Subscriptions.Any(s => s.Security == t.Trade.Security && s.Trades))
                                    {
                                        MyTradeGrid.Trades.Add(t);
                                        SaveToFile(MyTradeToString(t), _myTradesFilePath);
                                    }
                                });
                            };

                _connector.NewTrades += trades =>
                            {
                                TradeGrid.Trades.AddRange(trades);
                                trades.ForEach(t => SaveToFile(TradeToString(t), _tradesFilePath));
                            };

                _connector.NewOrders += orders =>
                            {
                                orders.ForEach(o =>
                                {
                                    if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
                                    {
                                        OrderGrid.Orders.Add(o);
                                        SaveToFile(OrderToString(o), _ordersFilePath);
                                    }
                                });
                            };

                _connector.OrdersChanged += orders =>
                            {
                                orders.ForEach(o =>
                                {
                                    if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
                                    {
                                        OrderGrid.Orders.Add(o);
                                        SaveToFile(OrderToString(o), _ordersFilePath);
                                    }
                                });
                            };

                _connector.NewStopOrders += orders =>
                            {
                                orders.ForEach(o =>
                                {
                                    if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
                                    {
                                        OrderGrid.Orders.Add(o);
                                        SaveToFile(OrderToString(o), _ordersFilePath);
                                    }
                                });
                            };

                _connector.StopOrdersChanged += orders =>
                            {
                                orders.ForEach(o =>
                                {
                                    if (Subscriptions.Any(s => s.Security == o.Security && s.Orders))
                                    {
                                        OrderGrid.Orders.Add(o);
                                        SaveToFile(OrderToString(o), _ordersFilePath);
                                    }
                                });
                            };

                _connector.NewPortfolios += portfolios => PortfolioGrid.Portfolios.AddRange(portfolios);

                _connector.NewPositions += positions =>
                            {
                                positions.ForEach(p => 
                                {
                                    if (Subscriptions.Any(s => s.Security == p.Security ))
                                    {
                                        PortfolioGrid.Positions.Add(p);
                                        SaveToFile(PositionToString(p), _positionsFilePath);
                                    }
                                });
                            };

                _connector.PositionsChanged += positions =>
                {
                    positions.ForEach(p =>
                    {
                        if (Subscriptions.Any(s => s.Security == p.Security))
                        {
                            PortfolioGrid.Positions.Add(p);
                            SaveToFile(PositionToString(p), _positionsFilePath);
                        }
                    });
                };


                _connector.SecuritiesChanged += securities =>
                            {
                                securities.ForEach(s => SaveToFile(Level1ToString(s), _level1FilePath));
                            };

                _connector.MarketDepthsChanged += depths =>
                            {
                                depths.ForEach(d => DepthToFile(d, Path.Combine(_outputFolder, string.Format("{0}_depth.txt", d.Security.Code))));
                            };

                //_connector.ValuesChanged += (a, b, c, d) => { };

            }

            _connector.Connect();
        }

        private void SecurityEditor_SecuritySelected()
        {
            if (Subscriptions.Any(s => s.Security == SecurityEditor.SelectedSecurity)) return;
            Subscriptions.Add(new UserSubscription() { Security = SecurityEditor.SelectedSecurity });
        }
        
        #region Функции записи данных в текстовый файл 

        // возвращает строку сделки
        private string TradeToString(Trade trade)
        {
            //securityId;tradeId;time;price;volume;orderdirection 
            return string.Format("{0};{1};{2};{3};{4};{5}",
                                   trade.Security.Id,
                                   trade.Id.ToString(),
                                   trade.Time.ToString(),
                                   trade.Price.ToString(),
                                   trade.Volume.ToString(),
                                   trade.OrderDirection.ToString());
        }

        // возвращает строку своей сделки
        private string MyTradeToString(MyTrade trade)
        {
            //securityId;tradeId;time;volume;price;orderdirection;orderId 
            return string.Format("{0};{1};{2};{3};{4};{5};{6}",
                                   trade.Trade.Security.Id,
                                   trade.Trade.Id,
                                   trade.Trade.Time,
                                   trade.Trade.Volume,
                                   trade.Trade.Price,
                                   trade.Trade.OrderDirection.ToString(),
                                   trade.Order.Id);

        }

        // возвращает строку заявки
        private string OrderToString(Order order)
        {
            //orderId;transactionId;time;securityId;portfolioName;volume;balance;price;direction;type;localTime 
            return string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
                                   order.Id,
                                   order.TransactionId,
                                   order.Time,
                                   order.Security.Id,
                                   order.Portfolio.Name,
                                   order.Volume,
                                   order.Balance,
                                   order.Price,
                                   order.Direction.ToString(),
                                   order.Type.ToString(),
                                   order.State.ToString(),
                                   order.LocalTime);
        }

        // возвращает строку позиции
        private string PositionToString(Position position)
        {
            return string.Format("{0};{1};{2};{3}",
                                position.Security.Id,
                                position.Portfolio.Name,
                                position.CurrentValue,
                                position.AveragePrice);
        }

        // возвращает строку Level1
        private string Level1ToString(Security security)
        {
                return string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13}",
                                    security.Id,
                                    security.Board.Code,
                                    security.PriceStep,
                                    security.VolumeStep,
                                    security.Type,
                                    security.LastTrade.Price,
                                    security.LastTrade.Volume,
                                    security.LastTrade.Time.TimeOfDay,
                                    security.LastTrade.Time.Date,
                                    security.LastTrade.Time.Date,
                                    security.BestBid.Price,
                                    security.BestBid.Volume,
                                    security.BestAsk.Price,
                                    security.BestAsk.Volume
                                    );

        }

        // возвращает строку котировки стакана
        private string QuoteToString(Quote quote)
        {
            return string.Format("{0};{1};{2}{3}",
                                quote.OrderDirection.ToString(),
                                quote.Price,
                                quote.Volume,
                                Environment.NewLine);
        }

        // записывает данные в файл
        private void SaveToFile(string line, string filePath)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(line);
            }
        }

        // преобразует стакан в MemoryStream и записывает в файл
        private void DepthToFile(MarketDepth depth, string filePath)
        {
            using (MemoryStream mem = new MemoryStream(200))
            {
                for (var i = depth.Asks.GetUpperBound(0); i >= 0; i--)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Asks[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                for (var i = 0; i <= depth.Bids.GetUpperBound(0); i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Bids[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    mem.WriteTo(file);
                }
            }

        }

        #endregion


    }
}
