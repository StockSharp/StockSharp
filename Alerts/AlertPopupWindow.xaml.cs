namespace StockSharp.Alerts
{
	using System;

	/// <summary>
	/// Popup window for alert type <see cref="AlertTypes.Popup"/>.
	/// </summary>
	public partial class AlertPopupWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AlertPopupWindow"/>.
		/// </summary>
		public AlertPopupWindow()
		{
			InitializeComponent();
		}

		private DateTime _time;

		/// <summary>
		/// Alert creation time.
		/// </summary>
		public DateTime Time
		{
			get { return _time; }
			set
			{
				_time = value;
				TimeCtrl.Text = value.TimeOfDay.ToString(@"hh\:mm\:ss");
			}
		}

		/// <summary>
		/// Alert text.
		/// </summary>
		public string Message
		{
			get { return MessageCtrl.Text; }
			set { MessageCtrl.Text = value; }
		}
	}
}