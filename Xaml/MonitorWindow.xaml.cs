#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: MonitorWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// The window for trading strategies work monitoring.
	/// </summary>
	public partial class MonitorWindow : ILogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MonitorWindow"/>.
		/// </summary>
		public MonitorWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// To display the window on top of screen when an error occurred.
		/// </summary>
		public bool BringToFrontOnError { get; set; }

		/// <summary>
		/// To delete all messages.
		/// </summary>
		public void Clear()
		{
			_monitor.Clear();
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			((ILogListener)_monitor).WriteMessages(messages);

			if (BringToFrontOnError && messages.Any(message => message.Level == LogLevels.Error))
				this.BringToFront();
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			_monitor.Load(storage);
			BringToFrontOnError = storage.GetValue<bool>(nameof(BringToFrontOnError));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			_monitor.Save(storage);
			storage.SetValue(nameof(BringToFrontOnError), BringToFrontOnError);
		}

		void IDisposable.Dispose()
		{
		}
	}
}