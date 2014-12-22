namespace StockSharp.Studio.Controls
{
	using System;
	using System.Windows.Controls;
	using System.Runtime.InteropServices;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	[Guid("F97DCB8B-2104-4DF3-B6C5-CBB2B8B3B704")]
	public class LogManagerPanel : UserControl, IStudioControl
	{
		private readonly Monitor _monitor = new Monitor();
		private readonly GuiLogListener _listener;

		public LogManagerPanel()
		{
			Content = _monitor;
			_listener = new GuiLogListener(_monitor);

			// загрузка контрола происходит только при открытии окна актипро
			// если окно свернуто, то событие загрузки не вызывается.
			ConfigManager.GetService<LogManager>().Listeners.Add(_listener);
			//Loaded += OnLoaded;
		}

		//private void OnLoaded(object sender, RoutedEventArgs e)
		//{
		//	Loaded -= OnLoaded;
		//	new AddLogListenerCommand(_listener).Process(this);
		//}

		void IPersistable.Load(SettingsStorage storage)
		{
			((IPersistable)_monitor).Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			((IPersistable)_monitor).Save(storage);
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Str3237; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		void IDisposable.Dispose()
		{
			new RemoveLogListenerCommand(_listener).Process(this);
		}
	}
}