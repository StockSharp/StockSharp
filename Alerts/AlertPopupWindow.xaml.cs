namespace StockSharp.Alerts
{
	using System;

	/// <summary>
	/// Всплывающее окно для типа сигнала <see cref="AlertTypes.Popup"/>.
	/// </summary>
	public partial class AlertPopupWindow
	{
		/// <summary>
		/// Создать <see cref="AlertPopupWindow"/>.
		/// </summary>
		public AlertPopupWindow()
		{
			InitializeComponent();
		}

		private DateTime _time;

		/// <summary>
		/// Время формирования сигнала.
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
		/// Текст сигнала.
		/// </summary>
		public string Message
		{
			get { return MessageCtrl.Text; }
			set { MessageCtrl.Text = value; }
		}
	}
}