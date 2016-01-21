#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

using StockSharp.Terminal.Layout;
using StockSharp.Terminal.Controls;
using StockSharp.Terminal.Logics;

using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock;

using StockSharp.Algo;
using StockSharp.Logging;
using StockSharp.Algo.Storages;

using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Common;
using Ecng.Xaml;

using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Configuration;
using StockSharp.Terminal.Services;

namespace StockSharp.Terminal
{
    public partial class MainWindow
    {
        #region Fields
        //-------------------------------------------------------------------

        private int _countWorkArea = 2;

        private ConnectorService _connectorService;

        //-------------------------------------------------------------------
        #endregion Fields

        #region Properties
        //-------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public LayoutManager LayoutManager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static MainWindow Instance { get; private set; }

        //-------------------------------------------------------------------
        #endregion Properties

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            LayoutManager = new LayoutManager(DockingManager);
            DockingManager.DocumentClosed += DockingManager_DocumentClosed;

            Title = Title.Put("Multi connection");

            var logManager = new LogManager();

            _connectorService = new ConnectorService();
            _connectorService.ChangeConnectStatusEvent += ChangeConnectStatusEvent;
            _connectorService.ErrorEvent += ConnectorServiceErrorEvent;

            logManager.Sources.Add(_connectorService.GetConnector());
            logManager.Listeners.Add(new FileLogListener("sample.log"));

            _connectorService.InitConnector();
        }

        #region Events
        //-------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            var newWorkArea = new LayoutDocument()
            {
                Title = "Work area #" + ++_countWorkArea,
                Content = new WorkAreaControl()
            };

            LayoutDocuments.Children.Add(newWorkArea);

            var offset = LayoutDocuments.Children.Count - 1;
            LayoutDocuments.SelectedContentIndex = (offset < 0) ? 0 : offset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            var manager = (DockingManager)sender;

            if (LayoutDocuments.Children.Count == 0 && manager.FloatingWindows.ToList().Count == 0)
                _countWorkArea = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewControlComboBox.SelectedIndex != -1)
            {
                var workArea = (WorkAreaControl)DockingManager.ActiveContent;
                workArea.AddControl(((ComboBoxItem)NewControlComboBox.SelectedItem).Content.ToString());
                NewControlComboBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
        {
            DockingManager.ActiveContent.DoIfElse<WorkAreaControl>(editor =>
            {
                var element = (DockingManager)sender;

            }, () =>
            {
                var element = (DockingManager)sender;
                new Connector().Configure(this);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            _connectorService.Configure(this);
            new XmlSerializer<SettingsStorage>().Serialize(_connectorService.Save(), ConnectorService.SETTINGS_FILE);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectClick(object sender, RoutedEventArgs e)
        {
            if (!_connectorService.IsConnected)
                _connectorService.Connect();
            else
                _connectorService.Disconnect();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isConnected"></param>
        private void ChangeConnectStatusEvent(bool isConnected)
        {
            ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        private void ConnectorServiceErrorEvent(string message, string caption)
        {
            MessageBox.Show(this, message, caption);
        }

        //-------------------------------------------------------------------
        #endregion Events

        #region Приватные методы
        //-------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        private static void ShowOrHide(Window window)
        {
            if (window == null)
                throw new ArgumentNullException("window");

            if (window.Visibility == Visibility.Visible)
                window.Hide();
            else
                window.Show();
        }

        //-------------------------------------------------------------------
        #endregion Приватные методы
    }
}