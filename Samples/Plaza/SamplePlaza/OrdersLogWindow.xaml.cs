#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SamplePlaza.SamplePlazaPublic
File: OrdersLogWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace SamplePlaza
{
	using System;
	using System.Windows.Threading;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;

	public partial class OrdersLogWindow
	{
		private readonly DispatcherTimer _timer;

		private int _operationCount;
		private int _orderCount;
		private int _tradeCount;

		private int _operationCountPerSecond;
		private int _orderCountPerSecond;
		private int _tradeCountPerSecond;

		private DateTimeOffset? _lastOperationTime;

		public OrdersLogWindow()
		{
			InitializeComponent();

			_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			_timer.Tick += OnTick;
			_timer.Start();
		}

		private void OnTick(object sender, EventArgs eventArgs)
		{
			OperationCount.Text = _operationCount.ToString();
			OrderCount.Text = _orderCount.ToString();
			TradeCount.Text = _tradeCount.ToString();

			OperationCountPerSecond.Text = _operationCountPerSecond.ToString();
			OrderCountPerSecond.Text = _orderCountPerSecond.ToString();
			TradeCountPerSecond.Text = _tradeCountPerSecond.ToString();

			_operationCountPerSecond = _orderCountPerSecond = _tradeCountPerSecond = 0;

			if (_lastOperationTime != null)
				LastOperationTime.Text = _lastOperationTime.Value.ToString("yyyy/MM/dd/ HH:mm:ss.fff");
		}

		protected override void OnClosed(EventArgs e)
		{
			_timer.Stop();
			base.OnClosed(e);
		}

		public void AddOperation(OrderLogItem item)
		{
			_lastOperationTime = item.Order.Time;

			_operationCount++;
			_operationCountPerSecond++;

			if (item.Order.State == OrderStates.Active && item.Order.IsMatchedEmpty())
			{
				_orderCount++;
				_orderCountPerSecond++;
			}
			else if (item.Trade != null)
			{
				_tradeCount++;
				_tradeCountPerSecond++;
			}
		}
	}
}