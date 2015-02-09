namespace StockSharp.Studio.Services.Alerts
{
	using System;

	public partial class AlertPopupWindow
	{
		public AlertPopupWindow()
		{
			InitializeComponent();
		}

		private DateTime _time;

		public DateTime Time
		{
			get { return _time; }
			set
			{
				_time = value;
				TimeCtrl.Text = value.TimeOfDay.ToString(@"hh\:mm\:ss");
			}
		}

		public string Message
		{
			get { return MessageCtrl.Text; }
			set { MessageCtrl.Text = value; }
		}
	}
}