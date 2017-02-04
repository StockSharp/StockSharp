#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMultiConnection.SampleMultiConnectionPublic
File: PortfoliosWindow.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Protective;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleStrategies
{
	public partial class PortfoliosWindow
	{

        private List<Strategy> _strategies;
        public PortfoliosWindow()
        {
            InitializeComponent();
            _strategies = new List<Strategy>();
        }

        private void ProtectionClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var position = (Position)PortfolioGrid.SelectedPosition;

            if (position == null || position.Security == null || position.CurrentValue == 0)
            {
                ProtectionButton.IsEnabled = false;
                return;
            }

            if (ProtectionButton.Content.ToString() == "Protection on")
            {
                // Определяем направление стратегии
                var side = position.CurrentValue > 0 ? Sides.Buy : Sides.Sell;

                var trades = MainWindow.Instance.Connector.MyTrades.Where(t => t.Trade.Security.Code == position.Security.Code && t.Order.Direction == side).ToList();
                List<decimal> prices = new List<decimal>();

                // Выбираем цены последних сделок, которые будут использованы для расчета средневзвешенной цены позиции.
                for (var i = trades.Count() - 1; i >= 0; i--)
                {
                    var t = trades[i];
                    for (var j = 1; j <= t.Trade.Volume; j++)
                    {
                        prices.Add(t.Trade.Price);
                        if (prices.Count == Math.Abs(position.CurrentValue.Value))
                            break;
                    }
                }

                if (!prices.Any())
                    return;

                var security = trades.First().Trade.Security;

                // Цена, которую будем защищать. Равна средневзвешенно цене позиции, расчитанной по последним сделкам.
                var protecPrice = security.ShrinkPrice(prices.Average());

                // Защищаемый уровень. В данном случае определяетя, как смешение от защищаемой цены, на заданное число тиков.
                var protectLevel = new Unit(Convert.ToInt32(SpinEdit.EditValue), UnitTypes.Step);
                protectLevel.SetSecurity(security);

                if (!MainWindow.Instance.Connector.RegisteredMarketDepths.Contains(security))
                    MainWindow.Instance.Connector.RegisterFilteredMarketDepth(security);

                // Создаем защитную стратегию.
                var strategy = new StopLossStrategy(side, protecPrice, Math.Abs(position.CurrentValue.Value), protectLevel)
                {
                    Connector = MainWindow.Instance.Connector,
                    Security = security,
                    Portfolio = position.Portfolio,
                    CommentOrders = true,
                    UseMarketOrders = true,
                    UseQuoting = false
                };

                // Добавляем стратегию в список.
                _strategies.Add(strategy);
                MainWindow.Instance.LogManager.Sources.Add(strategy);

                // Задаем правило для остановки стратегии. В момент остановки удаляем стратегию из списка.
                strategy.WhenStopped().Do(s =>
                {
                    if (_strategies.Contains(s))
                        _strategies.Remove(s);

                    if (MainWindow.Instance.LogManager.Sources.Contains(s))
                        MainWindow.Instance.LogManager.Sources.Remove(s);

                }).Apply();

                // Запускаем стратегию.
                strategy.Start();

                ProtectionButton.Content = "Protection off";
            }
            else
            {
                var strategy = _strategies.FirstOrDefault(s => s.Security == position.Security);

                if (strategy == null)
                    return;

                if (strategy.ProcessState == ProcessStates.Started)
                {
                    strategy.Stop();
                }
                else
                {
                    _strategies.Remove(strategy);
                }

                ProtectionButton.Content = "Protection on";
            }

        }

        private void PortfolioGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var position = PortfolioGrid.SelectedPosition as Position;

            if (position == null || position.Security == null || position.CurrentValue == 0)
            {
                ProtectionButton.IsEnabled = false;
            }
            else
            {
                ProtectionButton.IsEnabled = true;

                var strategy = _strategies.FirstOrDefault(s => s.Security.Code == position.Security.Code);

                if (strategy != null && strategy.ProcessState == ProcessStates.Started)
                {
                    ProtectionButton.Content = "Protection off";
                }
                else
                {
                    ProtectionButton.Content = "Protection on";
                }

            }
        }

    }
}