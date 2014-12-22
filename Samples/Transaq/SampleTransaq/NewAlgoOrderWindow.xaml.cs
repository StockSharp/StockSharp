namespace SampleTransaq
{
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Transaq;

	public partial class NewAlgoOrderWindow
	{
		public NewAlgoOrderWindow()
		{
			InitializeComponent();
			Portfolio.Connector = MainWindow.Instance.Trader;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Portfolio.Connector = null;
			base.OnClosing(e);
		}

		public Security Security { get; set; }

		private void SendClick(object sender, RoutedEventArgs e)
		{
			var algoOrder = new Order
			{
				Portfolio = Portfolio.SelectedPortfolio,
				Security = Security,
				Direction = IsBuy.IsChecked == true ? Sides.Buy : Sides.Sell,
				Price = OrderPrice.Text.To<decimal>(),
				Volume = OrderVolume.Text.To<decimal>()
			};

			var condition = new TransaqAlgoOrderCondition {Type = _selectedTrigger, Value = OrderPrice.Text.To<decimal>()};

			if (ValidAfterImmediately.IsChecked == true)
			{
				condition.ValidAfterType = TransaqAlgoOrderValidTypes.Immediately;
			}
			else
			{
				condition.ValidAfterType = TransaqAlgoOrderValidTypes.Date;
				condition.ValidAfter = ValidAfterDate.Value;
			}

			if (ValidBeforeTillCancelled.IsChecked == true)
			{
				condition.ValidBeforeType = TransaqAlgoOrderValidTypes.TillCancelled;
			}
			else
			{
				condition.ValidBeforeType = TransaqAlgoOrderValidTypes.Date;
				condition.ValidBefore = ValidBefore.Value;
			}

			algoOrder.Condition = condition;

			MainWindow.Instance.Trader.RegisterOrder(algoOrder);
			DialogResult = true;
		}

		private TransaqAlgoOrderConditionTypes _selectedTrigger = TransaqAlgoOrderConditionTypes.None;

		private void Trigger_OnChecked(object sender, RoutedEventArgs e)
		{
			_selectedTrigger = ((RadioButton)sender).Content.To<TransaqAlgoOrderConditionTypes>();
		}
	}
}
