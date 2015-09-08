namespace StockSharp.Anywhere
{
	using System;
	using System.Linq;
	using System.IO;
	using System.Reflection;
	using System.Text;

	using Ecng.Common;
	using Ecng.Xaml;
	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Quik;
	using StockSharp.Messages;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;

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
    
    public partial class MainWindow 
    {
        private Connector _connector;
        private readonly LogManager _logManager = new LogManager();

        private bool _isUnloading;    // флаг - идет загрузка данных   
        private bool _isConnectClick; // флаг - выполнен щелчок по кнопке соединения

		private readonly static string _outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OUTPUT"); // путь к папке в выгружаемыми данными 

		private readonly string _tradesFilePath = Path.Combine(_outputFolder, "trades.txt");
		private readonly string _ordersFilePath = Path.Combine(_outputFolder, "orders.txt");
		private readonly string _myTradesFilePath = Path.Combine(_outputFolder, "mytrades.txt");
		private readonly string _level1FilePath = Path.Combine(_outputFolder, "level1.txt");
		private readonly string _positionsFilePath = Path.Combine(_outputFolder, "positions.txt");

        public MainWindow()
        {
            InitializeComponent();

			Title = TypeHelper.ApplicationNameWithVersion;

            ConnectCommand = new DelegateCommand(OnConnect, CanOnConnect);
            UnloadingCommand = new DelegateCommand(Unloading, CanUnloading);
            DeleteSubscriptionCommand = new DelegateCommand(DeleteSubscription, CanDeleteSubscription);

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
        }

        private ObservableCollectionEx<UserSubscription> _subscriptions;

        /// <summary>
        /// Содержит информацию о подписке на данные
        /// </summary>
        public ObservableCollectionEx<UserSubscription> Subscriptions
        {
            get
            {
                if (_subscriptions == null) Subscriptions = new ObservableCollectionEx<UserSubscription>();
                return _subscriptions;

            }
            set { _subscriptions = value; }
        }

        #region Commands

        /// <summary>
        /// Устанавливает/разрывает соединение
        /// </summary>
        public DelegateCommand ConnectCommand { set; get; }

        private void OnConnect(object obj)
        {
            if ((string)obj == LocalizedStrings.Connect)
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

		private void Unloading(object obj)
        {
			if ((string)obj == "Начать выгрузку")
            {
                _isUnloading = true;
             
                SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_connector);
                SecurityPicker.MarketDataProvider = _connector;

                var securities = SecurityPicker.FilteredSecurities.ToArray();

                foreach (var security in securities)
                {
                    if (_subscriptions.All(s => s.Security != security))
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

        private bool CanUnloading(object obj)
        {
            return _connector != null && 
                   _connector.ConnectionState == ConnectionStates.Connected && 
                   Subscriptions.Any(s => s.MarketDepth || s.Level1 || s.Trades);
        }


        /// <summary>
        /// Удалает элемент подписки
        /// </summary>
        public DelegateCommand DeleteSubscriptionCommand { set; get; }

        private void DeleteSubscription(object obj)
        {
            if (Subscriptions.Contains((UserSubscription)obj))
            {
                Subscriptions.Remove((UserSubscription)obj);
                SecurityPicker.ExcludeSecurities.Add(((UserSubscription)obj).Security);
            }
        }

        private bool CanDeleteSubscription(object obj)
        {
            return (obj != null && !_isUnloading);
        }

        #endregion

        private void Connect()
        {
            if (_connector == null)
            {
	            _connector = new QuikTrader { LogLevel = LogLevels.Debug };

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
		private static string TradeToString(Trade trade)
        {
            //securityId;tradeId;time;price;volume;orderdirection 
            return "{0};{1};{2};{3};{4};{5}".Put(
                                   trade.Security.Id,
                                   trade.Id,
                                   trade.Time,
                                   trade.Price,
                                   trade.Volume,
                                   trade.OrderDirection);
        }

        // возвращает строку своей сделки
        private static string MyTradeToString(MyTrade trade)
        {
            //securityId;tradeId;time;volume;price;orderdirection;orderId 
            return "{0};{1};{2};{3};{4};{5};{6}".Put(
                                   trade.Trade.Security.Id,
                                   trade.Trade.Id,
                                   trade.Trade.Time,
                                   trade.Trade.Volume,
                                   trade.Trade.Price,
                                   trade.Trade.OrderDirection,
                                   trade.Order.Id);

        }

        // возвращает строку заявки
		private static string OrderToString(Order order)
        {
            //orderId;transactionId;time;securityId;portfolioName;volume;balance;price;direction;type;localTime 
            return "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}".Put(
                                   order.Id,
                                   order.TransactionId,
                                   order.Time,
                                   order.Security.Id,
                                   order.Portfolio.Name,
                                   order.Volume,
                                   order.Balance,
                                   order.Price,
                                   order.Direction,
                                   order.Type,
                                   order.State,
                                   order.LocalTime);
        }

        // возвращает строку позиции
		private static string PositionToString(Position position)
        {
            return "{0};{1};{2};{3}".Put(
                                position.Security.Id,
                                position.Portfolio.Name,
                                position.CurrentValue,
                                position.AveragePrice);
        }

        // возвращает строку Level1
		private static string Level1ToString(Security security)
        {
			return "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13}".Put(
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
        private static string QuoteToString(Quote quote)
        {
			return "{0};{1};{2}{3}".Put(
                                quote.OrderDirection,
                                quote.Price,
                                quote.Volume,
                                Environment.NewLine);
        }

        // записывает данные в файл
        private static void SaveToFile(string line, string filePath)
        {
            using (var file = new StreamWriter(filePath, true))
            {
                file.WriteLine(line);
            }
        }

        // преобразует стакан в MemoryStream и записывает в файл
        private static void DepthToFile(MarketDepth depth, string filePath)
        {
            using (var mem = new MemoryStream(200))
            {
                for (var i = depth.Asks.GetUpperBound(0); i >= 0; i--)
                {
                    var bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Asks[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                for (var i = 0; i <= depth.Bids.GetUpperBound(0); i++)
                {
                    var bytes = Encoding.UTF8.GetBytes(QuoteToString(depth.Bids[i]));
                    mem.Write(bytes, 0, bytes.Length);
                }

                using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    mem.WriteTo(file);
                }
            }

        }

        #endregion


    }
}