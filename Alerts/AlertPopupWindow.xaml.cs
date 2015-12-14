#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alerts.Alerts
File: AlertPopupWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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