#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: OrdersPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	/// <summary>
	/// Визуальный контрол-таблица, отображающая заявки (коллекцию объектов класса <see cref="Order"/>).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.OrdersKey)]
	[DescriptionLoc(LocalizedStrings.Str3268Key)]
	[Icon("images/order_16x16.png")]
	public partial class OrdersPanel
	{
		/// <summary>
		/// Создать <see cref="OrdersPanel"/>.
		/// </summary>
		public OrdersPanel()
		{
			InitializeComponent();

			Security prevSecurity = null;
			Portfolio prevPortfolio = null;
			OrderTypes? prevType = null;

			Func<OrderTypes?, Order> createOrder = type => new Order
			{
				Type = type,
				Security = prevSecurity,
				Portfolio = prevPortfolio,
			};

			OrderGrid.OrderRegistering += () =>
			{
				var wnd = new OrderWindow
				{
					SecurityProvider = ConfigManager.GetService<ISecurityProvider>(),
					MarketDataProvider = ConfigManager.GetService<IMarketDataProvider>(),
					Order = createOrder(prevType),
					Portfolios = ConfigManager.GetService<PortfolioDataSource>()
				};

				if (!wnd.ShowModal(this))
					return;

				var order = wnd.Order;

				prevType = order.Type;
				prevSecurity = order.Security;
				prevPortfolio = order.Portfolio;

				new RegisterOrderCommand(order).Process(this);
			};
			OrderGrid.OrderReRegistering += order =>
			{
				var window = new OrderWindow
				{
					Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
					Order = order.ReRegisterClone(newVolume: order.Balance),
				};

				if (window.ShowModal(this))
				{
					new ReRegisterOrderCommand(order, window.Order).Process(this);
				}
			};
			OrderGrid.OrderCanceling += orders => new CancelOrderCommand(orders).Process(this);
			OrderGrid.SelectionChanged += (s, e) => new SelectCommand<Order>(OrderGrid.SelectedOrder, false).Process(this);
			OrderGrid.PropertyChanged += (s, e) => new ControlChangedCommand(this).Process(this);

			GotFocus += (s, e) => new SelectCommand<Order>(OrderGrid.SelectedOrder, false).Process(this);

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ResetedCommand>(this, false, cmd => OrderGrid.Orders.Clear());

			//TODO: получение команд из ICommandService
			cmdSvc.Register<OrderCommand>(this, false, cmd =>
			{
				if (cmd.Order.Type != OrderTypes.Conditional && !OrderGrid.Orders.Contains(cmd.Order))
					OrderGrid.Orders.Add(cmd.Order);
			});
			cmdSvc.Register<ReRegisterOrderCommand>(this, false, cmd => OrderGrid.Orders.Add(cmd.NewOrder));
			cmdSvc.Register<OrderFailCommand>(this, false, cmd =>
			{
				if(cmd.Fail.Order.Type == OrderTypes.Conditional)
					return;

				if(!OrderGrid.Orders.Contains(cmd.Fail.Order))
					OrderGrid.Orders.Add(cmd.Fail.Order);
				else if(cmd.Action == OrderActions.Registering)
					OrderGrid.AddRegistrationFail(cmd.Fail);
			});
			cmdSvc.Register<BindStrategyCommand>(this, false, cmd =>
			{
				prevSecurity = cmd.Source.Security;
				prevPortfolio = cmd.Source.Portfolio;
			});

			WhenLoaded(() => new RequestBindSource(this).SyncProcess(this));
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.UnRegister<ResetedCommand>(this);
			cmdSvc.UnRegister<OrderCommand>(this);
			cmdSvc.UnRegister<ReRegisterOrderCommand>(this);
			cmdSvc.UnRegister<OrderFailCommand>(this);
		}

		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("OrderGrid", OrderGrid.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			OrderGrid.Load(settings.GetValue<SettingsStorage>("OrderGrid"));
		}
	}
}