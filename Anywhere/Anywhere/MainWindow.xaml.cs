#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Anywhere.AnywherePublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Anywhere
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Security;

    using Ecng.Common;
    using Ecng.Xaml;

    using Algo;
    using BusinessEntities;
    using Fix;
    using Messages;
    using Quik;
    using Quik.Lua;

    public class UserSubscription
    {
        public Security Security { set; get; }

        public string SecurityId => Security.Id;

	    public bool Trades { set; get; }
        public bool MarketDepth { set; get; }
        public bool Level1 { set; get; }
        public bool Orders { set; get; }
        public bool MyTrades { set; get; }
    }

    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly FixMessageAdapter _messAdapter;

        private readonly List<Security> _securities;

        private readonly LuaFixTransactionMessageAdapter _transAdapter;
        private bool _isConnectClick; // flag - click on connect button

        private bool _isConnected;

        private bool _isUnloading; // flag - data loading ...
        private bool _marketDataStarted;

        private InputTranParser _parser;

        private ObservableCollectionEx<UserSubscription> _subscriptions;
        private bool _transactionsStarted;

        public MainWindow()
        {
            InitializeComponent();

            ConnectCommand = new DelegateCommand(OnConnect, CanOnConnect);
            UnloadingCommand = new DelegateCommand(Unloading, CanUnloading);
            DeleteSubscriptionCommand = new DelegateCommand(DeleteSubscription, CanDeleteSubscription);
            ParsingCommand = new DelegateCommand(Parsing, CanParsing);

            _securities = new List<Security>();

            _transAdapter = new LuaFixTransactionMessageAdapter(new MillisecondIncrementalIdGenerator())
            {
                Login = "quik",
                Password = "quik".To<SecureString>(),
                Address = QuikTrader.DefaultLuaAddress,
                TargetCompId = "StockSharpTS",
                SenderCompId = "quik",
                RequestAllPortfolios = true,
            };

            _messAdapter = new LuaFixMarketDataMessageAdapter(new MillisecondIncrementalIdGenerator())
            {
                Login = "quik",
                Password = "quik".To<SecureString>(),
                Address = QuikTrader.DefaultLuaAddress,
                TargetCompId = "StockSharpMD",
                SenderCompId = "quik",
                RequestAllSecurities = true,
                RequestAllPortfolios = false,
            };

            _messAdapter.AddSupportedMessage(MessageTypes.Connect);

            ((IMessageAdapter)_messAdapter).NewOutMessage += OnNewOutMessage;
        }

        /// <summary>
        ///     Information about a market data subscribing
        /// </summary>
        public ObservableCollectionEx<UserSubscription> Subscriptions
        {
            get
            {
                if (_subscriptions == null)
                    Subscriptions = new ObservableCollectionEx<UserSubscription>();
                return _subscriptions;
            }
            set { _subscriptions = value; }
        }

        /// <summary>
        ///     Connection status
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void OnNewOutMessage(Message message)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageTypes.QuoteChange:
                        //Debug.WriteLine("QuoteChange");

                        ((QuoteChangeMessage)message).WriteMarketDepth();

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

                        break;
                    case MessageTypes.PortfolioLookupResult:
                        //Debug.WriteLine("PortfolioLookupResult");
                        break;

                    case MessageTypes.Level1Change:
                        //Debug.WriteLine("Level1Change");

                        ((Level1ChangeMessage)message).WriteLevel1();

                        break;
                    case MessageTypes.News:
                        break;

                    case MessageTypes.Execution:
                        //Debug.WriteLine("Execution");

                        var execMsg = (ExecutionMessage)message;

		                if (execMsg.ExecutionType == ExecutionTypes.Tick)
			                execMsg.WriteTrade();
		                else
		                {
							if (execMsg.HasOrderInfo())
								execMsg.WriteOrder();

							if (execMsg.HasTradeInfo())
								execMsg.WriteMyTrade();
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
                        //position.WritePosition();

                        break;

                    case MessageTypes.PositionChange:
                        //Debug.WriteLine("PositionChange");

                        ((PositionChangeMessage)message).WritePosition();

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
                                _messAdapter.SendInMessage(new SecurityLookupMessage { TransactionId = _messAdapter.TransactionIdGenerator.GetNextId() });

                            IsConnected = true;
                        }
                        else
                            Debug.WriteLine(((ConnectMessage)message).Error.Message);

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
            if (Subscriptions.Any(s => s.Security == SecurityEditor.SelectedSecurity))
                return;
            Subscriptions.Add(new UserSubscription { Security = SecurityEditor.SelectedSecurity });
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsConnected)
                _messAdapter.SendInMessage(new DisconnectMessage());
        }

       #region Commands

        public DelegateCommand ConnectCommand { set; get; }

        private void OnConnect(object obj)
        {
            if (!IsConnected)
                _messAdapter.SendInMessage(new ConnectMessage());
            else
                _messAdapter.SendInMessage(new DisconnectMessage());

            _isConnectClick = true;
        }

        private bool CanOnConnect(object obj)
        {
            return !_isConnectClick;
        }

        public DelegateCommand UnloadingCommand { set; get; }

        private void Unloading(object e)
        {
            if (!_marketDataStarted)
            {
                _isUnloading = true;

                //SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_connector);
                //SecurityPicker.MarketDataProvider = _connector;
                //var securities =    SecurityPicker.FilteredSecurities.ToArray();

                foreach (var subscr in Subscriptions)
                {
                    var secId = subscr.Security.ToSecurityId();

                    if (subscr.MarketDepth)
                        SubscribeMarketData(secId, MarketDataTypes.MarketDepth);
                    ;

                    if (subscr.Trades)
                        SubscribeMarketData(secId, MarketDataTypes.Trades);
                    ;

                    if (subscr.Level1)
                        SubscribeMarketData(secId, MarketDataTypes.Level1);
                    ;
                }

                _marketDataStarted = true;
            }
            else
            {
                foreach (var subscr in Subscriptions)
                {
                    var secId = subscr.Security.ToSecurityId();

                    if (subscr.MarketDepth)
                        UnSubscribeMarketData(secId, MarketDataTypes.MarketDepth);
                    ;

                    if (subscr.Trades)
                        UnSubscribeMarketData(secId, MarketDataTypes.Trades);
                    ;

                    if (subscr.Level1)
                        UnSubscribeMarketData(secId, MarketDataTypes.Level1);
                    ;
                }

                //SecurityPicker.SecurityProvider = null;
                //SecurityPicker.MarketDataProvider = null;
                //SecurityPicker.ExcludeSecurities.Clear();

                _isUnloading = false;

                _marketDataStarted = false;
            }
        }

        private bool CanUnloading(object obj)
        {
            return IsConnected && Subscriptions.Any(s => s.MarketDepth || s.Level1 || s.Trades);
        }

        public DelegateCommand ParsingCommand { set; get; }

        private void Parsing(object e)
        {
            if (!_transactionsStarted)
            {
                _parser = new InputTranParser(_transAdapter, _messAdapter, _securities);
                _parser.Start();
                _transactionsStarted = true;
            }
            else
            {
                _parser.Stop();
                _transactionsStarted = false;
            }
        }

        private static bool CanParsing(object obj)
        {
            return true;
        }

        /// <summary>
        ///     Remove usersubscription
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

        #region INotifyPropertyChanged releases

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}