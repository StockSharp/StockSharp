namespace StockSharp.Studio.Controls
{
	using System;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	/// <summary>
	/// Визуальный контрол-таблица, отображающая условные заявки (коллекцию объектов класса <see cref="Order"/>).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str3242Key)]
	[DescriptionLoc(LocalizedStrings.Str3243Key)]
	[Icon("images/order_16x16.png")]
	public partial class OrdersConditionalPanel
	{
		public OrdersConditionalPanel()
		{
			InitializeComponent();

			Security prevSecurity = null;
			Portfolio prevPortfolio = null;

			Func<Order> createOrder = () => new Order
			{
				Type = OrderTypes.Conditional,
				Security = prevSecurity,
				Portfolio = prevPortfolio,
			};

			OrderGrid.OrderRegistering += () =>
			{
				var wnd = new OrderConditionalWindow
				{
					SecurityProvider = ConfigManager.TryGetService<FilterableSecurityProvider>(),
					Order = createOrder()
				};

				if (!wnd.ShowModal(this))
					return;

				var order = wnd.Order;

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
			cmdSvc.Register<OrderCommand>(this, false, cmd =>
			{
				if (cmd.Action == OrderActions.Registering && cmd.Order.Type == OrderTypes.Conditional)
					OrderGrid.Orders.Add(cmd.Order);
			});
			cmdSvc.Register<ReRegisterOrderCommand>(this, false, cmd => OrderGrid.Orders.Add(cmd.NewOrder));
			cmdSvc.Register<OrderFailCommand>(this, false, cmd =>
			{
				if (cmd.Action == OrderActions.Registering)
					OrderGrid.AddRegistrationFail(cmd.Fail);
			});
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
			settings.SetValue("OrderGrid", OrderGrid.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			OrderGrid.Load(settings.GetValue<SettingsStorage>("OrderGrid"));
		}
	}
}
